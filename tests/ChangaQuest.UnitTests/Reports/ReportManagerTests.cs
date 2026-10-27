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

    private sealed class Fixture
    {
        public Fixture(bool emailConfirmed = true, bool declaredAdult = true, string[]? administrators = null)
        {
            var changa = new Changa { Id = Guid.NewGuid(), AuthorId = "author", Status = ChangaStatus.Available };
            ChangaId = changa.Id;
            Reports = new FakeReportStore();
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

    private sealed class FakeReportStore : IReportStore
    {
        public List<Report> Items { get; } = [];
        public bool FailOnAdd { get; set; }

        public Report Seed(string reporterId, Guid changaId, string reason, string? evidence)
        {
            var report = new Report
            {
                Id = Guid.NewGuid(), ReporterId = reporterId, ChangaId = changaId,
                Reason = reason, Evidence = evidence, CreatedAt = Now
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
