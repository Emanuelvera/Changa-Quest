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
        public List<Changa> Changas { get; } = [];
        public List<ChangaAssignment> Assignments { get; } = [];

        public FakeChangaStore(params Changa[] changas) => Changas.AddRange(changas);

        public Task AddAsync(Changa changa)
        {
            Changas.Add(changa);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Changa>> ListAvailableAsync() =>
            Task.FromResult<IReadOnlyList<Changa>>(Changas.Where(item => item.Status == ChangaStatus.Available).ToArray());

        public Task<Changa?> FindAvailableByIdAsync(Guid id) =>
            Task.FromResult(Changas.SingleOrDefault(item => item.Id == id && item.Status == ChangaStatus.Available));

        public Task EnsureSamplesAsync(IReadOnlyCollection<Changa> samples)
        {
            Changas.AddRange(samples.Where(sample => Changas.All(existing => existing.Id != sample.Id)));
            return Task.CompletedTask;
        }

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
                AssignedAt = DateTimeOffset.UtcNow
            };
            changa.Assignment = assignment;
            changa.Status = ChangaStatus.Assigned;
            Assignments.Add(assignment);
            return Task.FromResult(ChangaTakePersistenceResult.Assigned);
        }
    }

    private sealed class FakeAccountStore(bool emailConfirmed, bool declaredAdult) : IAccountIdentityStore
    {
        private readonly AppUser _user = new()
        {
            Id = "account-id",
            EmailConfirmed = emailConfirmed,
            DeclaredAdult = declaredAdult
        };

        public Task<AccountCreationResult> CreateAdultAsync(string email, string password) =>
            Task.FromResult(new AccountCreationResult(false));

        public Task<AccountIdentityLoginStatus> SignInAsync(string email, string password) =>
            Task.FromResult(AccountIdentityLoginStatus.InvalidCredentials);

        public Task<bool> ConfirmEmailAsync(string userId, string token) => Task.FromResult(false);

        public Task SignOutAsync() => Task.CompletedTask;

        public Task<AppUser?> FindByIdAsync(string userId) => Task.FromResult<AppUser?>(_user);
    }
}
