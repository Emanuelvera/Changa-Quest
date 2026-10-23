using ChangaQuest.BusinessLogic.Changas;
using ChangaQuest.DataAccess.Accounts;
using ChangaQuest.DataAccess.Changas;
using ChangaQuest.Entities.Accounts;
using ChangaQuest.Entities.Changas;

namespace ChangaQuest.UnitTests.Changas;

public sealed class ChangaManagerTests
{
    [Fact]
    public async Task Eligible_account_creates_first_come_available_changa_and_public_projection()
    {
        var store = new FakeChangaStore();
        var manager = new ChangaManager(store, new FakeAccountStore(emailConfirmed: true, declaredAdult: true));

        var result = await manager.CreateAsync("account-id", ValidCommand());

        Assert.Equal(ChangaManagerStatus.Success, result.Status);
        var saved = Assert.Single(store.Changas);
        Assert.Equal(ChangaStatus.Available, saved.Status);
        Assert.Equal(AssignmentMode.FirstCome, saved.AssignmentMode);
        Assert.Equal("account-id", saved.AuthorId);
        Assert.Equal("Dirección privada 123", saved.ExactAddress);
        Assert.Equal("first_come", result.Changa?.AssignmentMode);
        Assert.DoesNotContain(typeof(ChangaPublicView).GetProperties(), property => property.Name == "ExactAddress");
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
        true);

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

        public Task<IReadOnlyList<Changa>> ListAvailableForSearchAsync() => ListAvailableAsync();

        public Task<Changa?> FindAvailableByIdAsync(Guid id) =>
            Task.FromResult(Changas.SingleOrDefault(item => item.Id == id && item.Status == ChangaStatus.Available));

        public Task EnsureSamplesAsync(IReadOnlyCollection<Changa> samples)
        {
            Changas.AddRange(samples.Where(sample => Changas.All(existing => existing.Id != sample.Id)));
            return Task.CompletedTask;
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
