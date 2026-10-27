using ChangaQuest.BusinessLogic.Reports;
using ChangaQuest.DataAccess.Accounts;
using ChangaQuest.DataAccess.Changas;
using ChangaQuest.DataAccess.Reports;
using ChangaQuest.Entities.Accounts;
using ChangaQuest.Entities.Changas;
using ChangaQuest.Entities.Reports;

namespace ChangaQuest.UnitTests.Reports;

public sealed class ReportManagerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-25T12:00:00Z");

    [Fact]
    public async Task Eligible_account_submits_a_pending_report_with_evidence_and_gets_no_private_data_back()
    {
        var fixture = new Fixture();

        var result = await fixture.Manager.SubmitAsync(
            "reporter", new ReportSubmitCommand(fixture.ChangaId, "  Contenido ofensivo  ", " https://example.test/captura "));

        Assert.Equal(ReportManagerStatus.Success, result.Status);
        Assert.Equal("pending", result.Submitted!.Status);
        Assert.Equal(Now, result.Submitted.CreatedAt);
        var saved = Assert.Single(fixture.Reports.Items);
        Assert.Equal(result.Submitted.Id, saved.Id);
        Assert.Equal("reporter", saved.ReporterId);
        Assert.Equal(fixture.ChangaId, saved.ChangaId);
        Assert.Equal("Contenido ofensivo", saved.Reason);
        Assert.Equal("https://example.test/captura", saved.Evidence);
        Assert.Equal(ReportStatus.Pending, saved.Status);
        Assert.DoesNotContain(typeof(ReportSubmittedView).GetProperties(), property => property.Name is "Reason" or "Evidence");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Report_without_evidence_is_registered_with_null_evidence(string? evidence)
    {
        var fixture = new Fixture();

        var result = await fixture.Manager.SubmitAsync(
            "reporter", new ReportSubmitCommand(fixture.ChangaId, "Motivo válido", evidence));

        Assert.Equal(ReportManagerStatus.Success, result.Status);
        Assert.Null(Assert.Single(fixture.Reports.Items).Evidence);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Empty_reason_is_rejected_and_nothing_is_stored(string? reason)
    {
        var fixture = new Fixture();

        var result = await fixture.Manager.SubmitAsync(
            "reporter", new ReportSubmitCommand(fixture.ChangaId, reason, null));

        Assert.Equal(ReportManagerStatus.Invalid, result.Status);
        Assert.True(result.Errors!.ContainsKey("reason"));
        Assert.Empty(fixture.Reports.Items);
    }

    [Fact]
    public async Task Reason_and_evidence_longer_than_the_limit_are_rejected()
    {
        var fixture = new Fixture();

        var longReason = await fixture.Manager.SubmitAsync(
            "reporter", new ReportSubmitCommand(fixture.ChangaId, new string('a', ReportPolicy.MaximumReasonLength + 1), null));
        var longEvidence = await fixture.Manager.SubmitAsync(
            "reporter", new ReportSubmitCommand(fixture.ChangaId, "Motivo", new string('b', ReportPolicy.MaximumEvidenceLength + 1)));
        var atLimit = await fixture.Manager.SubmitAsync(
            "reporter", new ReportSubmitCommand(
                fixture.ChangaId,
                new string('a', ReportPolicy.MaximumReasonLength),
                new string('b', ReportPolicy.MaximumEvidenceLength)));

        Assert.Equal(ReportManagerStatus.Invalid, longReason.Status);
        Assert.True(longReason.Errors!.ContainsKey("reason"));
        Assert.Equal(ReportManagerStatus.Invalid, longEvidence.Status);
        Assert.True(longEvidence.Errors!.ContainsKey("evidence"));
        Assert.Equal(ReportManagerStatus.Success, atLimit.Status);
        Assert.Single(fixture.Reports.Items);
    }

    [Fact]
    public async Task Unknown_changa_is_reported_as_not_found_and_nothing_is_stored()
    {
        var fixture = new Fixture();

        var result = await fixture.Manager.SubmitAsync(
            "reporter", new ReportSubmitCommand(Guid.NewGuid(), "Motivo", null));

        Assert.Equal(ReportManagerStatus.ChangaNotFound, result.Status);
        Assert.Empty(fixture.Reports.Items);
    }

    [Fact]
    public async Task Anonymous_or_ineligible_accounts_cannot_submit()
    {
        var fixture = new Fixture();
        var unverified = new Fixture(emailConfirmed: false);
        var minor = new Fixture(declaredAdult: false);
        var command = new ReportSubmitCommand(fixture.ChangaId, "Motivo", null);

        Assert.Equal(ReportManagerStatus.Unauthenticated, (await fixture.Manager.SubmitAsync(null, command)).Status);
        Assert.Equal(ReportManagerStatus.Unauthenticated, (await fixture.Manager.SubmitAsync(" ", command)).Status);
        Assert.Equal(ReportManagerStatus.Forbidden, (await unverified.Manager.SubmitAsync("reporter", command with { ChangaId = unverified.ChangaId })).Status);
        Assert.Equal(ReportManagerStatus.Forbidden, (await minor.Manager.SubmitAsync("reporter", command with { ChangaId = minor.ChangaId })).Status);
        Assert.Empty(fixture.Reports.Items);
        Assert.Empty(unverified.Reports.Items);
        Assert.Empty(minor.Reports.Items);
    }

    [Fact]
    public async Task Failed_persistence_is_not_presented_as_submitted()
    {
        var fixture = new Fixture();
        fixture.Reports.FailOnAdd = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Manager.SubmitAsync(
            "reporter", new ReportSubmitCommand(fixture.ChangaId, "Motivo", null)));
        Assert.Empty(fixture.Reports.Items);
    }

    [Fact]
    public async Task Administrator_reads_reason_and_evidence()
    {
        var fixture = new Fixture(administrators: ["admin"]);
        var report = fixture.Reports.Seed("reporter", fixture.ChangaId, "Motivo", "Evidencia");

        var result = await fixture.Manager.GetAsync("admin", report.Id);

        Assert.Equal(ReportManagerStatus.Success, result.Status);
        Assert.Equal("Motivo", result.Report!.Reason);
        Assert.Equal("Evidencia", result.Report.Evidence);
        Assert.Equal("reporter", result.Report.ReporterId);
        Assert.Equal("pending", result.Report.Status);
    }

    [Fact]
    public async Task Administrator_gets_not_found_for_an_unknown_report()
    {
        var fixture = new Fixture(administrators: ["admin"]);

        var result = await fixture.Manager.GetAsync("admin", Guid.NewGuid());

        Assert.Equal(ReportManagerStatus.ReportNotFound, result.Status);
    }

    [Fact]
    public async Task Non_administrators_including_the_reporter_are_denied_even_knowing_the_id()
    {
        var fixture = new Fixture(administrators: ["admin"]);
        var report = fixture.Reports.Seed("reporter", fixture.ChangaId, "Motivo", "Evidencia");

        var reporter = await fixture.Manager.GetAsync("reporter", report.Id);
        var other = await fixture.Manager.GetAsync("other", report.Id);
        var unknownId = await fixture.Manager.GetAsync("other", Guid.NewGuid());
        var anonymous = await fixture.Manager.GetAsync(null, report.Id);

        Assert.Equal(ReportManagerStatus.AdminForbidden, reporter.Status);
        Assert.Equal(ReportManagerStatus.AdminForbidden, other.Status);
        Assert.Equal(ReportManagerStatus.AdminForbidden, unknownId.Status);
        Assert.Equal(ReportManagerStatus.Unauthenticated, anonymous.Status);
        Assert.Null(reporter.Report);
        Assert.Null(other.Report);
    }

    [Fact]
    public async Task Case_index_returns_party_reports_and_the_full_administrative_queue_without_private_fields()
    {
        var fixture = new Fixture(administrators: ["admin"]);
        var report = fixture.Reports.Seed("reporter", fixture.ChangaId, "Motivo privado", "Evidencia privada");

        var partyCases = await fixture.Manager.ListCasesAsync("author");
        var adminCases = await fixture.Manager.ListCasesAsync("admin");
        var unrelatedCases = await fixture.Manager.ListCasesAsync("unrelated");
        var anonymousCases = await fixture.Manager.ListCasesAsync(null);

        Assert.Equal(ReportManagerStatus.Success, partyCases.Status);
        Assert.Equal(report.Id, Assert.Single(partyCases.Cases!).Id);
        Assert.Equal(report.Id, Assert.Single(adminCases.Cases!).Id);
        Assert.Empty(unrelatedCases.Cases!);
        Assert.Equal(ReportManagerStatus.Unauthenticated, anonymousCases.Status);
        Assert.DoesNotContain(
            typeof(ReportCaseListItem).GetProperties(),
            property => property.Name is "Reason" or "Evidence");
    }

    [Fact]
    public async Task Administrative_case_orders_and_identifies_responses_and_appeals_by_party()
    {
        var fixture = new Fixture(administrators: ["admin"]);
        var report = fixture.Reports.Seed("reporter", fixture.ChangaId, "Motivo", "Evidencia");
        report.Responses.Add(new ReportResponse
        {
            Id = Guid.NewGuid(), AuthorId = "worker", Text = "Respuesta de quien realiza",
            CreatedAt = Now.AddMinutes(2)
        });
        report.Responses.Add(new ReportResponse
        {
            Id = Guid.NewGuid(), AuthorId = "author", Text = "Respuesta de quien solicita",
            CreatedAt = Now.AddMinutes(1)
        });
        report.Appeals.Add(new ReportAppeal
        {
            Id = Guid.NewGuid(), AppellantId = "worker", Text = "Apelación de quien realiza",
            CreatedAt = Now.AddMinutes(4)
        });
        report.Appeals.Add(new ReportAppeal
        {
            Id = Guid.NewGuid(), AppellantId = "author", Text = "Apelación de quien solicita",
            CreatedAt = Now.AddMinutes(3)
        });

        var result = await fixture.Manager.GetAsync("admin", report.Id);

        Assert.Equal(new[] { "requester", "worker" }, result.Report!.Responses.Select(response => response.AuthorRole));
        Assert.Equal(new[] { "requester", "worker" }, result.Report.Appeals.Select(appeal => appeal.AppellantRole));
        Assert.Equal(
            [Now.AddMinutes(1), Now.AddMinutes(2)],
            result.Report.Responses.Select(response => response.CreatedAt));
        Assert.Equal(
            [Now.AddMinutes(3), Now.AddMinutes(4)],
            result.Report.Appeals.Select(appeal => appeal.CreatedAt));
    }

    [Fact]
    public async Task Parties_can_respond_until_decision_and_only_see_their_own_response()
    {
        var fixture = new Fixture(administrators: ["admin"]);
        var report = fixture.Reports.Seed("reporter", fixture.ChangaId, "Motivo privado", "Evidencia privada");

        var authorResponse = await fixture.Manager.SubmitResponseAsync("author", report.Id, new("Respuesta del solicitante"));
        var workerResponse = await fixture.Manager.SubmitResponseAsync("worker", report.Id, new("Respuesta de la realizadora"));
        var decision = await fixture.Manager.DecideAsync(
            "admin", report.Id, new("Conclusión", "Motivo de decisión", true));
        var lateResponse = await fixture.Manager.SubmitResponseAsync("author", report.Id, new("Respuesta tardía"));
        var partyCase = await fixture.Manager.GetCaseAsync("author", report.Id);

        Assert.Equal(ReportManagerStatus.Success, authorResponse.Status);
        Assert.Equal(ReportManagerStatus.Success, workerResponse.Status);
        Assert.Equal(ReportManagerStatus.Success, decision.Status);
        Assert.Equal(ReportManagerStatus.CaseClosed, lateResponse.Status);
        Assert.Equal(ReportStatus.Decided, report.Status);
        Assert.Equal("Conclusión", report.Conclusion);
        Assert.True(report.EvidenceConsidered);
        Assert.Equal(ReportManagerStatus.Success, partyCase.Status);
        Assert.Single(partyCase.Case!.MyResponses);
        Assert.Equal("Respuesta del solicitante", partyCase.Case.MyResponses[0].Text);
        Assert.Equal("Conclusión", partyCase.Case.Decision!.Conclusion);
    }

    [Fact]
    public async Task Unrelated_people_cannot_respond_or_appeal_and_appeal_requires_a_decision()
    {
        var fixture = new Fixture();
        var report = fixture.Reports.Seed("reporter", fixture.ChangaId, "Motivo", null);

        var unrelatedResponse = await fixture.Manager.SubmitResponseAsync("outsider", report.Id, new("No autorizado"));
        var prematureAppeal = await fixture.Manager.AppealAsync("author", report.Id, new("Apelación prematura"));

        Assert.Equal(ReportManagerStatus.PartyForbidden, unrelatedResponse.Status);
        Assert.Equal(ReportManagerStatus.DecisionMissing, prematureAppeal.Status);
        Assert.Empty(report.Responses);
        Assert.Empty(report.Appeals);
    }

    [Fact]
    public async Task Any_administrator_can_resolve_an_appeal()
    {
        var fixture = new Fixture(administrators: ["first-admin", "second-admin"]);
        var report = fixture.Reports.Seed("reporter", fixture.ChangaId, "Motivo", null);
        await fixture.Manager.DecideAsync(
            "first-admin", report.Id, new("Conclusión", "Motivo de decisión", false));
        var appeal = await fixture.Manager.AppealAsync("worker", report.Id, new("No estoy de acuerdo"));

        var resolution = await fixture.Manager.ResolveAppealAsync(
            "second-admin", report.Id, appeal.Appeal!.Id, new("Se mantiene la conclusión"));

        Assert.Equal(ReportManagerStatus.Success, resolution.Status);
        var savedAppeal = Assert.Single(report.Appeals);
        Assert.Equal("second-admin", savedAppeal.ResolverId);
        Assert.Equal("Se mantiene la conclusión", savedAppeal.Resolution);
    }

    private sealed class Fixture
    {
        public Fixture(bool emailConfirmed = true, bool declaredAdult = true, string[]? administrators = null)
        {
            var changa = new Changa
            {
                Id = Guid.NewGuid(),
                AuthorId = "author",
                Status = ChangaStatus.Assigned,
                Assignment = new ChangaAssignment { WorkerId = "worker" }
            };
            ChangaId = changa.Id;
            Reports = new FakeReportStore(changa);
            Manager = new ReportManager(
                Reports,
                new FakeChangaStore(changa),
                new FakeAccountStore(emailConfirmed, declaredAdult, administrators ?? []),
                new FixedTimeProvider(Now));
        }

        public Guid ChangaId { get; }
        public FakeReportStore Reports { get; }
        public ReportManager Manager { get; }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeReportStore(Changa changa) : IReportStore
    {
        public List<Report> Items { get; } = [];
        public bool FailOnAdd { get; set; }

        public Report Seed(string reporterId, Guid changaId, string reason, string? evidence)
        {
            var report = new Report
            {
                Id = Guid.NewGuid(), ReporterId = reporterId, ChangaId = changaId,
                Reason = reason, Evidence = evidence, CreatedAt = Now, Changa = changa
            };
            Items.Add(report);
            return report;
        }

        public Task AddAsync(Report report)
        {
            if (FailOnAdd)
            {
                throw new InvalidOperationException("Falla simulada de persistencia.");
            }

            Items.Add(report);
            return Task.CompletedTask;
        }

        public Task<Report?> FindByIdAsync(Guid id) => Task.FromResult(Items.SingleOrDefault(report => report.Id == id));

        public Task<IReadOnlyList<Report>> ListAsync(string? participantId) =>
            Task.FromResult<IReadOnlyList<Report>>(Items
                .Where(report => participantId is null ||
                    report.Changa.AuthorId == participantId ||
                    report.Changa.Assignment?.WorkerId == participantId)
                .ToArray());

        public Task<ReportMutationStatus> AddResponseAsync(ReportResponse response)
        {
            var report = Items.SingleOrDefault(item => item.Id == response.ReportId);
            if (report is null)
            {
                return Task.FromResult(ReportMutationStatus.NotFound);
            }

            if (report.Status != ReportStatus.Pending)
            {
                return Task.FromResult(ReportMutationStatus.Conflict);
            }

            report.Responses.Add(response);
            return Task.FromResult(ReportMutationStatus.Updated);
        }

        public Task<ReportMutationStatus> RecordDecisionAsync(
            Guid reportId, string reviewerId, string conclusion, string reason,
            bool evidenceConsidered, DateTimeOffset decidedAt)
        {
            var report = Items.SingleOrDefault(item => item.Id == reportId);
            if (report is null)
            {
                return Task.FromResult(ReportMutationStatus.NotFound);
            }

            if (report.Status != ReportStatus.Pending)
            {
                return Task.FromResult(ReportMutationStatus.Conflict);
            }

            report.Status = ReportStatus.Decided;
            report.DecisionMakerId = reviewerId;
            report.Conclusion = conclusion;
            report.DecisionReason = reason;
            report.EvidenceConsidered = evidenceConsidered;
            report.DecidedAt = decidedAt;
            return Task.FromResult(ReportMutationStatus.Updated);
        }

        public Task<ReportMutationStatus> AddAppealAsync(ReportAppeal appeal)
        {
            var report = Items.SingleOrDefault(item => item.Id == appeal.ReportId);
            if (report is null)
            {
                return Task.FromResult(ReportMutationStatus.NotFound);
            }

            if (report.Status != ReportStatus.Decided)
            {
                return Task.FromResult(ReportMutationStatus.Conflict);
            }

            report.Appeals.Add(appeal);
            return Task.FromResult(ReportMutationStatus.Updated);
        }

        public Task<ReportMutationStatus> ResolveAppealAsync(
            Guid reportId, Guid appealId, string resolverId, string resolution, DateTimeOffset resolvedAt) =>
            Task.FromResult(ResolveAppeal(reportId, appealId, resolverId, resolution, resolvedAt));

        private ReportMutationStatus ResolveAppeal(
            Guid reportId, Guid appealId, string resolverId, string resolution, DateTimeOffset resolvedAt)
        {
            var appeal = Items.SingleOrDefault(item => item.Id == reportId)?.Appeals
                .SingleOrDefault(item => item.Id == appealId);
            if (appeal is null)
            {
                return ReportMutationStatus.NotFound;
            }

            if (appeal.ResolvedAt is not null)
            {
                return ReportMutationStatus.Conflict;
            }

            appeal.ResolverId = resolverId;
            appeal.Resolution = resolution;
            appeal.ResolvedAt = resolvedAt;
            return ReportMutationStatus.Updated;
        }
    }

    private sealed class FakeChangaStore(Changa changa) : IChangaStore
    {
        public Task<Changa?> FindByIdAsync(Guid id) => Task.FromResult(id == changa.Id ? changa : null);

        public Task AddAsync(Changa value) => throw new NotSupportedException();
        public Task<IReadOnlyList<Changa>> ListAvailableAsync() => throw new NotSupportedException();
        public Task<IReadOnlyList<Changa>> ListAvailableForSearchAsync() => throw new NotSupportedException();
        public Task<IReadOnlyList<Changa>> ListMyUnassignedAsync(string authorId) => throw new NotSupportedException();
        public Task<Changa?> FindAvailableByIdAsync(Guid id) => throw new NotSupportedException();
        public Task<ChangaTakePersistenceResult> TakeAsync(Guid id, string workerId) => throw new NotSupportedException();
        public Task<ChangaCancelPersistenceResult> CancelAsync(Guid id) => throw new NotSupportedException();
        public Task<ChangaCompletionTransitionResult> TransitionCompletionAsync(
            Guid id, ChangaStatus expectedStatus, ChangaStatus newStatus,
            DateTimeOffset? requestedAt = null, DateTimeOffset? deadline = null,
            DateTimeOffset? respondedAt = null, string? reviewCause = null) => throw new NotSupportedException();
        public Task<ChangaPaymentDeclarationPersistenceResult> DeclarePaymentAsync(
            Guid id, bool requester, DateTimeOffset declaredAt) => throw new NotSupportedException();
        public Task SetPhoneConsentAsync(Guid id, bool isAuthor, PhoneConsentStatus status, string? phone) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<ChangaAssignment>> ListAssignmentsForAccountAsync(string userId) =>
            throw new NotSupportedException();
        public Task<bool> HasRatingAsync(Guid changaId, string raterId) => throw new NotSupportedException();
        public Task<ChangaRatingPersistenceResult> AddRatingAsync(ChangaRating rating) =>
            throw new NotSupportedException();
        public Task<ChangaRatingAggregate> GetRatingAggregateAsync(string rateeId) =>
            throw new NotSupportedException();
        public Task<ChangaCancellationPersistenceResult> CreateCancellationRequestAsync(
            Guid changaId, string requestedById, string targetId) => throw new NotSupportedException();
        public Task<ChangaCancellationPersistenceResult> RespondCancellationRequestAsync(
            Guid changaId, Guid requestId, string responderId, bool accept) => throw new NotSupportedException();
        public Task EnsureSamplesAsync(IReadOnlyCollection<Changa> samples) => throw new NotSupportedException();
    }

    private sealed class FakeAccountStore(bool emailConfirmed, bool declaredAdult, string[] administrators)
        : IAccountIdentityStore
    {
        public Task<AppUser?> FindByIdAsync(string userId) => Task.FromResult<AppUser?>(
            new AppUser { Id = userId, EmailConfirmed = emailConfirmed, DeclaredAdult = declaredAdult });

        public Task<bool> IsInRoleAsync(string userId, string role) =>
            Task.FromResult(role == ReportPolicy.AdministratorRole && administrators.Contains(userId));

        public Task<AccountCreationResult> CreateAdultAsync(string email, string password) => throw new NotSupportedException();
        public Task<AccountIdentityLoginStatus> SignInAsync(string email, string password) => throw new NotSupportedException();
        public Task<bool> ConfirmEmailAsync(string userId, string token) => throw new NotSupportedException();
        public Task SignOutAsync() => throw new NotSupportedException();
        public Task SaveSearchPreferenceAsync(AppUser user) => throw new NotSupportedException();
    }
}
