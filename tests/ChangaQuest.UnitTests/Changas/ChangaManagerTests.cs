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

