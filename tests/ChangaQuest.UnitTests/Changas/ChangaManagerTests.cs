using ChangaQuest.BusinessLogic.Changas;
using ChangaQuest.DataAccess.Accounts;
using ChangaQuest.DataAccess.Changas;
using ChangaQuest.Entities.Accounts;
using ChangaQuest.Entities.Changas;

namespace ChangaQuest.UnitTests.Changas;

public sealed class ChangaManagerTests
{
    [Fact]
    public async Task Author_lists_only_own_unassigned_changas_without_private_address()
    {
        var available = AvailableChanga("account-id");
        available.ExactAddress = "Dirección privada 123";
        var cancelled = AvailableChanga("account-id");
        cancelled.Status = ChangaStatus.Cancelled;
        var assigned = AvailableChanga("account-id");
        assigned.Assignment = new ChangaAssignment { WorkerId = "worker-id", Changa = assigned };
        var store = new FakeChangaStore(available, cancelled, assigned, AvailableChanga("someone-else"));
        var manager = new ChangaManager(store, new FakeAccountStore(emailConfirmed: true, declaredAdult: true));

        var result = await manager.ListMyUnassignedAsync("account-id");

        Assert.Equal(ChangaManagerStatus.Success, result.Status);
        Assert.Equal(2, result.Changas!.Count);
        Assert.Contains(result.Changas, item => item.Id == available.Id && item.Status == "available");
        Assert.Contains(result.Changas, item => item.Id == cancelled.Id && item.Status == "cancelled");
        Assert.DoesNotContain(typeof(ChangaPublicView).GetProperties(), property => property.Name == "ExactAddress");
    }

    [Fact]
    public async Task Author_cancels_available_unassigned_changa_and_listing_keeps_cancelled_status()
    {
        var changa = AvailableChanga("account-id");
        var store = new FakeChangaStore(changa);
        var manager = new ChangaManager(store, new FakeAccountStore(emailConfirmed: true, declaredAdult: true));

        var result = await manager.CancelAsync("account-id", changa.Id);

        Assert.Equal(ChangaManagerStatus.Success, result.Status);
        Assert.Equal("cancelled", result.Changa!.Status);
        Assert.Empty(await store.ListAvailableAsync());
        Assert.Contains(await store.ListMyUnassignedAsync("account-id"), item => item.Id == changa.Id);
    }

    [Fact]
    public async Task Non_author_cannot_cancel_even_when_changa_has_an_assignment()
    {
        var changa = AvailableChanga("author-id");
        changa.Assignment = new ChangaAssignment { WorkerId = "worker-id", Changa = changa };
        var store = new FakeChangaStore(changa);
        var manager = new ChangaManager(store, new FakeAccountStore(emailConfirmed: true, declaredAdult: true));

        var result = await manager.CancelAsync("other-id", changa.Id);

        Assert.Equal(ChangaManagerStatus.CancelForbidden, result.Status);
        Assert.Equal(ChangaStatus.Available, changa.Status);
        Assert.NotNull(changa.Assignment);
    }

    [Fact]
    public async Task Author_gets_conflict_for_assigned_other_state_and_missing_changas()
    {
        var assigned = AvailableChanga("account-id");
        assigned.Status = ChangaStatus.Assigned;
        assigned.Assignment = new ChangaAssignment { WorkerId = "worker-id", Changa = assigned };
        var pending = AvailableChanga("account-id");
        pending.Status = ChangaStatus.PendingReview;
        var store = new FakeChangaStore(assigned, pending);
        var manager = new ChangaManager(store, new FakeAccountStore(emailConfirmed: true, declaredAdult: true));

        Assert.Equal(ChangaManagerStatus.AlreadyAssigned, (await manager.CancelAsync("account-id", assigned.Id)).Status);
        Assert.Equal(ChangaManagerStatus.NotAvailable, (await manager.CancelAsync("account-id", pending.Id)).Status);
        Assert.Equal(ChangaManagerStatus.NotFound, (await manager.CancelAsync("account-id", Guid.NewGuid())).Status);
        Assert.Equal(ChangaStatus.Assigned, assigned.Status);
        Assert.Equal(ChangaStatus.PendingReview, pending.Status);
    }

    [Fact]
    public async Task Eligible_account_creates_first_come_pending_changa_and_public_projection()
    {
        var store = new FakeChangaStore();
        var manager = new ChangaManager(store, new FakeAccountStore(emailConfirmed: true, declaredAdult: true));

        var result = await manager.CreateAsync("account-id", ValidCommand());

        Assert.Equal(ChangaManagerStatus.Success, result.Status);
        var saved = Assert.Single(store.Changas);
        Assert.Equal(ChangaStatus.PendingReview, saved.Status);
        Assert.Equal("limpieza-domestica", saved.CategoryId);
        Assert.Equal(AssignmentMode.FirstCome, saved.AssignmentMode);
        Assert.Equal("account-id", saved.AuthorId);
        Assert.Equal("Dirección privada 123", saved.ExactAddress);
        Assert.Equal("first_come", result.Changa?.AssignmentMode);
        Assert.DoesNotContain(typeof(ChangaPublicView).GetProperties(), property => property.Name == "ExactAddress");
    }

    [Fact]
    public async Task Unapproved_category_is_rejected_without_persisting()
    {
        var store = new FakeChangaStore();
        var manager = new ChangaManager(store, new FakeAccountStore(emailConfirmed: true, declaredAdult: true));

        var result = await manager.CreateAsync("account-id", ValidCommand() with { CategoryId = "trabajo-electrico" });

        Assert.Equal(ChangaManagerStatus.InvalidCategory, result.Status);
        Assert.Empty(store.Changas);
    }

    [Fact]
    public async Task Pending_changa_can_be_approved_or_rejected_by_review_operation()
    {
        var changa = new Changa
        {
            Id = Guid.NewGuid(),
            Description = "Descripción válida para una changa.",
            CategoryId = "limpieza-domestica",
            Status = ChangaStatus.PendingReview
        };
        var store = new FakeChangaStore(changa);
        var manager = new ChangaManager(store, new FakeAccountStore(emailConfirmed: true, declaredAdult: true));

        var approved = await manager.ApproveAsync("account-id", changa.Id);

        Assert.Equal(ChangaManagerStatus.Success, approved.Status);
        Assert.Equal(ChangaStatus.Available, changa.Status);

        changa.Status = ChangaStatus.PendingReview;
        var rejected = await manager.RejectAsync("account-id", changa.Id, "Fuera del alcance.");

        Assert.Equal(ChangaManagerStatus.Success, rejected.Status);
        Assert.Equal(ChangaStatus.Rejected, changa.Status);
        Assert.Equal("Fuera del alcance.", changa.ReviewReason);
        Assert.Empty(await store.ListAvailableAsync());
    }

    [Fact]
    public async Task Invalid_command_returns_field_errors_without_persisting()
    {
        var store = new FakeChangaStore();
        var manager = new ChangaManager(store, new FakeAccountStore(emailConfirmed: true, declaredAdult: true));

        var result = await manager.CreateAsync(
            "account-id",
            new ChangaCreateCommand(null, 0, null, null, null, null, false));

        Assert.Equal(ChangaManagerStatus.Invalid, result.Status);
        Assert.Contains("description", result.Errors!.Keys);
        Assert.Contains("reward", result.Errors.Keys);
        Assert.Empty(store.Changas);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Account_must_be_verified_and_adult_before_creating(bool emailConfirmed, bool declaredAdult)
    {
        var store = new FakeChangaStore();
        var manager = new ChangaManager(store, new FakeAccountStore(emailConfirmed, declaredAdult));

        var result = await manager.CreateAsync("account-id", ValidCommand());

        Assert.Equal(ChangaManagerStatus.Forbidden, result.Status);
        Assert.Empty(store.Changas);
    }

    [Fact]
    public async Task Missing_account_id_is_unauthenticated()
    {
        var store = new FakeChangaStore();
        var manager = new ChangaManager(store, new FakeAccountStore(emailConfirmed: true, declaredAdult: true));

        var result = await manager.CreateAsync(null, ValidCommand());

        Assert.Equal(ChangaManagerStatus.Unauthenticated, result.Status);
        Assert.Empty(store.Changas);
    }

    [Fact]
    public async Task Eligible_worker_takes_another_persons_available_changa_once()
    {
        var changa = AvailableChanga("author-id");
        var store = new FakeChangaStore(changa);
        var manager = new ChangaManager(store, new FakeAccountStore(emailConfirmed: true, declaredAdult: true));

        var result = await manager.TakeAsync("worker-id", changa.Id);
        var retry = await manager.TakeAsync("worker-id", changa.Id);

        Assert.Equal(ChangaManagerStatus.Success, result.Status);
        Assert.Equal(ChangaManagerStatus.Success, retry.Status);
        Assert.Equal(result.Assignment?.Id, retry.Assignment?.Id);
        Assert.Equal(ChangaStatus.Assigned, changa.Status);
        Assert.Single(store.Assignments);
    }

    [Fact]
    public async Task Author_cannot_take_own_changa()
    {
        var changa = AvailableChanga("author-id");
        var store = new FakeChangaStore(changa);
        var manager = new ChangaManager(store, new FakeAccountStore(emailConfirmed: true, declaredAdult: true));

        var result = await manager.TakeAsync("author-id", changa.Id);

        Assert.Equal(ChangaManagerStatus.SelfAssignment, result.Status);
        Assert.Equal(ChangaStatus.Available, changa.Status);
        Assert.Empty(store.Assignments);
    }

    [Fact]
    public async Task Different_worker_gets_conflict_after_assignment()
    {
        var changa = AvailableChanga("author-id");
        var store = new FakeChangaStore(changa);
        var manager = new ChangaManager(store, new FakeAccountStore(emailConfirmed: true, declaredAdult: true));

        await manager.TakeAsync("first-worker", changa.Id);
        var result = await manager.TakeAsync("second-worker", changa.Id);

        Assert.Equal(ChangaManagerStatus.AlreadyAssigned, result.Status);
        Assert.Single(store.Assignments);
        Assert.Equal("first-worker", store.Assignments[0].WorkerId);
    }

    [Fact]
    public async Task Cancelled_changa_cannot_be_taken()
    {
        var changa = AvailableChanga("author-id");
        changa.Status = ChangaStatus.Cancelled;
        var store = new FakeChangaStore(changa);
        var manager = new ChangaManager(store, new FakeAccountStore(emailConfirmed: true, declaredAdult: true));

        var result = await manager.TakeAsync("worker-id", changa.Id);

        Assert.Equal(ChangaManagerStatus.NotAvailable, result.Status);
        Assert.Empty(store.Assignments);
    }

    [Fact]
    public async Task Coordination_shares_phones_only_after_mutual_consent_and_only_worker_sees_address()
    {
        var changa = AvailableChanga("author-id");
        var store = new FakeChangaStore(changa);
        var manager = new ChangaManager(store, new FakeAccountStore(emailConfirmed: true, declaredAdult: true));
        await manager.TakeAsync("worker-id", changa.Id);

        var requesterPending = await manager.GetCoordinationAsync("author-id", changa.Id);
        Assert.Equal(ChangaManagerStatus.Success, requesterPending.Status);
        Assert.False(requesterPending.Coordination!.HasMutualConsent);
        Assert.Null(requesterPending.Coordination.OtherPhone);
        Assert.Null(requesterPending.Coordination.ExactAddress);

        Assert.Equal(ChangaManagerStatus.Success,
            (await manager.SetPhoneConsentAsync("author-id", changa.Id, true, "+1 (234) 567-8901")).Status);
        Assert.Null((await manager.GetCoordinationAsync("author-id", changa.Id)).Coordination!.OtherPhone);
        Assert.Equal(ChangaManagerStatus.Success,
            (await manager.SetPhoneConsentAsync("worker-id", changa.Id, true, "12345678901")).Status);

        var requester = (await manager.GetCoordinationAsync("author-id", changa.Id)).Coordination!;
        var worker = (await manager.GetCoordinationAsync("worker-id", changa.Id)).Coordination!;
        Assert.True(requester.HasMutualConsent);
        Assert.Equal("12345678901", requester.OtherPhone);
        Assert.Null(requester.ExactAddress);
        Assert.Equal("+1 (234) 567-8901", worker.OtherPhone);
        Assert.Equal(changa.ExactAddress, worker.ExactAddress);

        await manager.SetPhoneConsentAsync("worker-id", changa.Id, false, "12345678901");
        var afterRevocation = (await manager.GetCoordinationAsync("author-id", changa.Id)).Coordination!;
        Assert.False(afterRevocation.HasMutualConsent);
        Assert.Null(afterRevocation.OtherPhone);
        Assert.Null(changa.Assignment!.WorkerPhone);
    }

    [Fact]
    public async Task Coordination_rejects_invalid_phone_and_forbids_outsiders()
    {
        var changa = AvailableChanga("author-id");
        var store = new FakeChangaStore(changa);
        var manager = new ChangaManager(store, new FakeAccountStore(emailConfirmed: true, declaredAdult: true));
        await manager.TakeAsync("worker-id", changa.Id);

        var invalid = await manager.SetPhoneConsentAsync("author-id", changa.Id, true, "123-45");
        Assert.Equal(ChangaManagerStatus.InvalidPhone, invalid.Status);
        Assert.Contains("phone", invalid.Errors!.Keys);
        Assert.Equal(PhoneConsentStatus.Pending, changa.Assignment!.RequesterPhoneConsent);
        Assert.Null(changa.Assignment.RequesterPhone);

        Assert.Equal(ChangaManagerStatus.CoordinationForbidden,
            (await manager.GetCoordinationAsync("outsider-id", changa.Id)).Status);
        Assert.Equal(ChangaManagerStatus.CoordinationForbidden,
            (await manager.SetPhoneConsentAsync("outsider-id", changa.Id, true, "12345678901")).Status);
        Assert.Equal(ChangaManagerStatus.NotFound,
            (await manager.GetCoordinationAsync("author-id", Guid.NewGuid())).Status);

        changa.Status = ChangaStatus.Available;
        changa.Assignment = null;
        Assert.Equal(ChangaManagerStatus.NotAssigned,
            (await manager.GetCoordinationAsync("author-id", changa.Id)).Status);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("12345678", true)]
    [InlineData("+54 (351) 234-5678", true)]
    [InlineData("123456789012345", true)]
    [InlineData("1234567", false)]
    [InlineData("1234567890123456", false)]
    [InlineData("-+12345678", false)]
    [InlineData("12345678abc", false)]
    [InlineData("1234.5678", false)]
    [InlineData("12345+678", false)]
    [InlineData("+54 351 123-4567", true)]
    [InlineData("(0351) 123 4567", true)]
    public async Task Phone_consent_accepts_only_supported_digit_lengths(string? phone, bool valid)
    {
        var changa = AvailableChanga("author-id");
        var store = new FakeChangaStore(changa);
        var manager = new ChangaManager(store, new FakeAccountStore(emailConfirmed: true, declaredAdult: true));
        await manager.TakeAsync("worker-id", changa.Id);

        var result = await manager.SetPhoneConsentAsync("author-id", changa.Id, true, phone);

        Assert.Equal(valid ? ChangaManagerStatus.Success : ChangaManagerStatus.InvalidPhone, result.Status);
        Assert.Equal(valid ? PhoneConsentStatus.Accepted : PhoneConsentStatus.Pending,
            changa.Assignment!.RequesterPhoneConsent);
    }

    [Fact]
    public async Task Phone_consent_trims_whitespace_before_checking_the_storage_length_limit()
    {
        var changa = AvailableChanga("author-id");
        var store = new FakeChangaStore(changa);
        var manager = new ChangaManager(store, new FakeAccountStore(emailConfirmed: true, declaredAdult: true));
        await manager.TakeAsync("worker-id", changa.Id);
        var paddedPhone = $"{new string(' ', 20)}+1 (234) 567-8901{new string(' ', 20)}";

        var result = await manager.SetPhoneConsentAsync("author-id", changa.Id, true, paddedPhone);

        Assert.Equal(ChangaManagerStatus.Success, result.Status);
        Assert.Equal("+1 (234) 567-8901", changa.Assignment!.RequesterPhone);
    }

    [Fact]
    public async Task Coordination_list_contains_only_assignments_for_current_account()
    {
        var changa = AvailableChanga("author-id");
        var store = new FakeChangaStore(changa);
        var manager = new ChangaManager(store, new FakeAccountStore(emailConfirmed: true, declaredAdult: true));
        await manager.TakeAsync("worker-id", changa.Id);

        var workerList = await manager.ListCoordinationsAsync("worker-id");
        var requesterList = await manager.ListCoordinationsAsync("author-id");
        var outsiderList = await manager.ListCoordinationsAsync("outsider-id");

        Assert.Single(workerList.Coordinations!);
        Assert.Single(requesterList.Coordinations!);
        Assert.Empty(outsiderList.Coordinations!);
    }

    [Fact]
    public async Task Worker_marks_completion_and_requester_confirms_without_closing_changa()
    {
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 10, 25, 15, 0, 0, TimeSpan.Zero));
        var changa = AvailableChanga("author-id");
        var store = new FakeChangaStore(changa);
        var manager = new ChangaManager(store, new FakeAccountStore(true, true), clock);
        await manager.TakeAsync("worker-id", changa.Id);

        var marked = await manager.MarkDoneAsync("worker-id", changa.Id);
        var confirmed = await manager.ConfirmCompletionAsync("author-id", changa.Id);

        Assert.Equal(ChangaManagerStatus.Success, marked.Status);
        Assert.Equal("pending_response", marked.Completion!.Status);
        Assert.Equal(clock.GetUtcNow(), marked.Completion.MarkedDoneAt);
        Assert.Equal(clock.GetUtcNow().AddMinutes(20), marked.Completion.ResponseDeadline);
        Assert.Equal(ChangaManagerStatus.Success, confirmed.Status);
        Assert.Equal("confirmed", confirmed.Completion!.Status);
        Assert.Equal(ChangaStatus.Confirmed, changa.Status);
        Assert.NotNull(changa.Assignment);
        Assert.Equal(clock.GetUtcNow(), changa.Assignment!.CompletionRespondedAt);
    }

    [Fact]
    public async Task Requester_rejection_moves_completion_to_review_without_releasing_assignment()
    {
        var clock = new TestTimeProvider(DateTimeOffset.Parse("2026-10-25T15:00:00Z"));
        var changa = AvailableChanga("author-id");
        var store = new FakeChangaStore(changa);
        var manager = new ChangaManager(store, new FakeAccountStore(true, true), clock);
        await manager.TakeAsync("worker-id", changa.Id);
        await manager.MarkDoneAsync("worker-id", changa.Id);

        var rejected = await manager.RejectCompletionAsync("author-id", changa.Id);

        Assert.Equal(ChangaManagerStatus.Success, rejected.Status);
        Assert.Equal("completion_review", rejected.Completion!.Status);
        Assert.Equal("rejected", rejected.Completion.ReviewCause);
        Assert.Equal(ChangaStatus.CompletionReview, changa.Status);
        Assert.Equal("worker-id", changa.Assignment!.WorkerId);
    }

    [Fact]
    public async Task Completion_read_lazily_persists_expiration_and_late_action_conflicts()
    {
        var clock = new TestTimeProvider(DateTimeOffset.Parse("2026-10-25T15:00:00Z"));
        var changa = AvailableChanga("author-id");
        var store = new FakeChangaStore(changa);
        var manager = new ChangaManager(store, new FakeAccountStore(true, true), clock);
        await manager.TakeAsync("worker-id", changa.Id);
        await manager.MarkDoneAsync("worker-id", changa.Id);
        clock.Advance(TimeSpan.FromMinutes(20));

        var lateMark = await manager.MarkDoneAsync("worker-id", changa.Id);
        var lateAction = await manager.ConfirmCompletionAsync("author-id", changa.Id);
        var read = await manager.GetCompletionAsync("worker-id", changa.Id);

        Assert.Equal(ChangaManagerStatus.CompletionInvalidState, lateMark.Status);
        Assert.Equal(ChangaManagerStatus.CompletionNotPending, lateAction.Status);
        Assert.Equal(ChangaStatus.CompletionReview, changa.Status);
        Assert.Equal("expired", changa.Assignment!.CompletionReviewCause);
        Assert.Equal("completion_review", read.Completion!.Status);
        Assert.Equal("expired", read.Completion.ReviewCause);
        Assert.Equal("worker-id", changa.Assignment.WorkerId);
    }

    [Fact]
    public async Task Completion_checks_roles_assignment_and_unknown_ids()
    {
        var changa = AvailableChanga("author-id");
        var store = new FakeChangaStore(changa);
        var manager = new ChangaManager(store, new FakeAccountStore(true, true));

        await manager.TakeAsync("worker-id", changa.Id);
        changa.Status = ChangaStatus.Available;
        Assert.Equal(ChangaManagerStatus.CompletionInvalidState, (await manager.MarkDoneAsync("worker-id", changa.Id)).Status);
        Assert.Equal(ChangaManagerStatus.CompletionForbidden, (await manager.MarkDoneAsync("author-id", changa.Id)).Status);
        Assert.Equal(ChangaManagerStatus.CompletionForbidden, (await manager.ConfirmCompletionAsync("worker-id", changa.Id)).Status);
        Assert.Equal(ChangaManagerStatus.CompletionForbidden, (await manager.RejectCompletionAsync("worker-id", changa.Id)).Status);
        Assert.Equal(ChangaManagerStatus.CompletionForbidden, (await manager.GetCompletionAsync("outsider-id", changa.Id)).Status);
        Assert.Equal(ChangaManagerStatus.NotFound, (await manager.GetCompletionAsync("author-id", Guid.NewGuid())).Status);
        var unassigned = AvailableChanga("author-id");
        store.Changas.Add(unassigned);
        Assert.Equal(ChangaManagerStatus.NotAssigned, (await manager.GetCompletionAsync("author-id", unassigned.Id)).Status);
    }

    private sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset current = now;
        public override DateTimeOffset GetUtcNow() => current;
        public void Advance(TimeSpan elapsed) => current += elapsed;
    }

    private static Changa AvailableChanga(string authorId) => new()
    {
        Id = Guid.NewGuid(),
        Description = "Descripción válida para una changa.",
        Reward = 10000,
        Zone = "Nueva Córdoba",
        ExactAddress = "Dirección privada 123",
        MaterialsNeeded = false,
        AssignmentMode = AssignmentMode.FirstCome,
        Status = ChangaStatus.Available,
        AuthorId = authorId,
        CreatedAt = DateTimeOffset.UtcNow
    };

    public async Task Search_uses_three_kilometres_by_default_and_excludes_items_outside_radius()
    {
        var store = new FakeChangaStore
        {
            Changas =
            [
                SampleChanga(Guid.NewGuid(), -31.4167, -64.1833),
                SampleChanga(Guid.NewGuid(), -31.4620, -64.1500)
            ]
        };
        var manager = new ChangaManager(store, new FakeAccountStore(emailConfirmed: true, declaredAdult: true));

        var result = await manager.SearchAvailableAsync(
            "account-id",
            new ChangaSearchQuery(-31.4167, -64.1833, 3, false));

        var item = Assert.Single(result.Changas!);
        Assert.Equal(0, item.DistanceKm);
        Assert.DoesNotContain(result.Changas!, changa => changa.Id == store.Changas[1].Id);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task Search_accepts_only_supported_radii(int radiusKm)
    {
        var store = new FakeChangaStore
        {
            Changas = [SampleChanga(Guid.NewGuid(), -31.4167, -64.1833)]
        };
        var manager = new ChangaManager(store, new FakeAccountStore(emailConfirmed: true, declaredAdult: true));

        var result = await manager.SearchAvailableAsync(
            "account-id",
            new ChangaSearchQuery(-31.4167, -64.1833, radiusKm, false));

        Assert.Equal(ChangaManagerStatus.Success, result.Status);
    }

    [Fact]
    public async Task Search_rejects_an_unsupported_radius()
    {
        var manager = new ChangaManager(
            new FakeChangaStore(),
            new FakeAccountStore(emailConfirmed: true, declaredAdult: true));

        var result = await manager.SearchAvailableAsync(
            "account-id",
            new ChangaSearchQuery(-31.4167, -64.1833, 2, false));

        Assert.Equal(ChangaManagerStatus.Invalid, result.Status);
        Assert.Contains("radiusKm", result.Errors!.Keys);
    }

    [Fact]
    public async Task Search_saves_manual_location_only_when_requested()
    {
        var accountStore = new FakeAccountStore(emailConfirmed: true, declaredAdult: true);
        var manager = new ChangaManager(new FakeChangaStore(), accountStore);

        await manager.SearchAvailableAsync(
            "account-id",
            new ChangaSearchQuery(-31.42, -64.18, 3, true));

        Assert.Equal(-31.42, accountStore.User.SearchLatitude);
        Assert.Equal(-64.18, accountStore.User.SearchLongitude);
        Assert.True(accountStore.PreferenceSaved);
    }

    private static Changa SampleChanga(Guid id, double latitude, double longitude) => new()
    {
        Id = id,
        Description = "Descripción válida para muestra.",
        Reward = 1000,
        Zone = "Nueva Córdoba",
        ExactAddress = "Dirección privada 123",
        MaterialsNeeded = false,
        AssignmentMode = AssignmentMode.FirstCome,
        Status = ChangaStatus.Available,
        Latitude = latitude,
        Longitude = longitude,
        CreatedAt = DateTimeOffset.UtcNow
    };
    private static ChangaCreateCommand ValidCommand() => new(
        "Descripción válida para una changa.",
        10000,
        "Nueva Córdoba",
        "Dirección privada 123",
        true,
        "worker",
        true,
        CategoryId: "limpieza-domestica");

    private sealed class FakeChangaStore : IChangaStore
    {
        public List<Changa> Changas { get; set; } = [];
        public List<ChangaAssignment> Assignments { get; } = [];

        public FakeChangaStore(params Changa[] changas) => Changas.AddRange(changas);

        public Task AddAsync(Changa changa)
        {
            Changas.Add(changa);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Changa>> ListAvailableAsync() =>
            Task.FromResult<IReadOnlyList<Changa>>(Changas.Where(item => item.Status == ChangaStatus.Available).ToArray());

        public Task<IReadOnlyList<Changa>> ListMyUnassignedAsync(string userId) =>
            Task.FromResult<IReadOnlyList<Changa>>(Changas
                .Where(item => item.AuthorId == userId && item.Assignment is null)
                .ToArray());

        public Task<IReadOnlyList<Changa>> ListAvailableForSearchAsync() => ListAvailableAsync();

        public Task<Changa?> FindAvailableByIdAsync(Guid id) =>
            Task.FromResult(Changas.SingleOrDefault(item => item.Id == id && item.Status == ChangaStatus.Available));

        public Task EnsureSamplesAsync(IReadOnlyCollection<Changa> samples)
        {
            Changas.AddRange(samples.Where(sample => Changas.All(existing => existing.Id != sample.Id)));
            return Task.CompletedTask;
        }

        public Task<ChangaCompletionTransitionResult> TransitionCompletionAsync(
            Guid id, ChangaStatus expectedStatus, ChangaStatus newStatus,
            DateTimeOffset? requestedAt = null, DateTimeOffset? deadline = null,
            DateTimeOffset? respondedAt = null, string? reviewCause = null)
        {
            var changa = Changas.SingleOrDefault(item => item.Id == id);
            if (changa?.Assignment is null || changa.Status != expectedStatus ||
                (respondedAt is { } at && changa.Assignment.CompletionDeadline is { } limit && limit <= at))
            {
                return Task.FromResult(ChangaCompletionTransitionResult.StateChanged);
            }
            changa.Status = newStatus;
            changa.Assignment.CompletionRequestedAt = requestedAt ?? changa.Assignment.CompletionRequestedAt;
            changa.Assignment.CompletionDeadline = deadline ?? changa.Assignment.CompletionDeadline;
            changa.Assignment.CompletionRespondedAt = respondedAt ?? changa.Assignment.CompletionRespondedAt;
            changa.Assignment.CompletionReviewCause = reviewCause;
            return Task.FromResult(ChangaCompletionTransitionResult.Updated);
        }

        public Task SetPhoneConsentAsync(Guid id, bool isAuthor, PhoneConsentStatus status, string? phone)
        {
            var assignment = Changas.Single(item => item.Id == id).Assignment!;
            if (isAuthor)
            {
                assignment.RequesterPhoneConsent = status;
                assignment.RequesterPhone = phone;
            }
            else
            {
                assignment.WorkerPhoneConsent = status;
                assignment.WorkerPhone = phone;
            }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ChangaAssignment>> ListAssignmentsForAccountAsync(string userId) =>
            Task.FromResult<IReadOnlyList<ChangaAssignment>>(Assignments
                .Where(item => item.WorkerId == userId || item.Changa.AuthorId == userId)
                .ToArray());

        public Task<Changa?> FindByIdAsync(Guid id) =>
            Task.FromResult(Changas.SingleOrDefault(item => item.Id == id));

        public Task<ChangaTakePersistenceResult> TakeAsync(Guid id, string workerId)
        {
            var changa = Changas.SingleOrDefault(item => item.Id == id);
            if (changa is null)
            {
                return Task.FromResult(ChangaTakePersistenceResult.NotFound);
            }

            if (changa.Assignment is not null)
            {
                return Task.FromResult(changa.Assignment.WorkerId == workerId
                    ? ChangaTakePersistenceResult.AlreadyAssignedToWorker
                    : ChangaTakePersistenceResult.AlreadyAssignedToOther);
            }

            if (changa.Status != ChangaStatus.Available)
            {
                return Task.FromResult(ChangaTakePersistenceResult.NotAvailable);
            }

            var assignment = new ChangaAssignment
            {
                Id = Guid.NewGuid(),
                ChangaId = changa.Id,
                WorkerId = workerId,
                AssignedAt = DateTimeOffset.UtcNow,
                Changa = changa
            };
            changa.Assignment = assignment;
            changa.Status = ChangaStatus.Assigned;
            Assignments.Add(assignment);
            return Task.FromResult(ChangaTakePersistenceResult.Assigned);
        }

        public Task<ChangaCancelPersistenceResult> CancelAsync(Guid id)
        {
            var changa = Changas.SingleOrDefault(item => item.Id == id);
            if (changa is null) return Task.FromResult(ChangaCancelPersistenceResult.NotFound);
            if (changa.Assignment is not null) return Task.FromResult(ChangaCancelPersistenceResult.AlreadyAssigned);
            if (changa.Status != ChangaStatus.Available) return Task.FromResult(ChangaCancelPersistenceResult.NotAvailable);
            changa.Status = ChangaStatus.Cancelled;
            return Task.FromResult(ChangaCancelPersistenceResult.Cancelled);
        }
    }

    private sealed class FakeAccountStore(bool emailConfirmed, bool declaredAdult) : IAccountIdentityStore
    {
        public AppUser User { get; } = new()
        {
            Id = "account-id",
            EmailConfirmed = emailConfirmed,
            DeclaredAdult = declaredAdult
        };
        public bool PreferenceSaved { get; private set; }

        public Task<AccountCreationResult> CreateAdultAsync(string email, string password) =>
            Task.FromResult(new AccountCreationResult(false));

        public Task<AccountIdentityLoginStatus> SignInAsync(string email, string password) =>
            Task.FromResult(AccountIdentityLoginStatus.InvalidCredentials);

        public Task<bool> ConfirmEmailAsync(string userId, string token) => Task.FromResult(false);

        public Task SignOutAsync() => Task.CompletedTask;

        public Task<AppUser?> FindByIdAsync(string userId) => Task.FromResult<AppUser?>(User);

        public Task SaveSearchPreferenceAsync(AppUser user)
        {
            PreferenceSaved = true;
            return Task.CompletedTask;
        }
    }
}
