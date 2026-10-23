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
