using ChangaQuest.BusinessLogic.Accounts;
using ChangaQuest.DataAccess.Accounts;
using ChangaQuest.Entities.Accounts;

namespace ChangaQuest.UnitTests.Accounts;

public sealed class AccountBlockManagerTests
{
    [Fact]
    public async Task Confirmed_block_is_persisted_and_repeat_is_idempotent()
    {
        var fixture = new Fixture();

        var first = await fixture.Manager.BlockAsync("blocker", "target");
        var second = await fixture.Manager.BlockAsync("blocker", "target");

        Assert.Equal(AccountBlockManagerStatus.Created, first.Status);
        Assert.Equal(AccountBlockManagerStatus.AlreadyBlocked, second.Status);
        Assert.Single(fixture.Store.Items);
        Assert.Equal("blocker", fixture.Store.Items[0].BlockerId);
        Assert.Equal("target", fixture.Store.Items[0].BlockedId);
    }

    [Fact]
    public async Task Anonymous_self_and_unknown_target_are_rejected_without_persistence()
    {
        var fixture = new Fixture();

        Assert.Equal(AccountBlockManagerStatus.Unauthenticated, (await fixture.Manager.BlockAsync(null, "target")).Status);
        Assert.Equal(AccountBlockManagerStatus.CannotBlockSelf, (await fixture.Manager.BlockAsync("blocker", "blocker")).Status);
        Assert.Equal(AccountBlockManagerStatus.AccountNotFound, (await fixture.Manager.BlockAsync("blocker", "missing")).Status);
        Assert.Empty(fixture.Store.Items);
    }

    [Fact]
    public async Task Blocking_accounts_with_an_assigned_changa_does_not_mutate_that_assignment()
    {
        var fixture = new Fixture();
        var assignedStatus = "assigned";

        var result = await fixture.Manager.BlockAsync("blocker", "target");

        Assert.Equal(AccountBlockManagerStatus.Created, result.Status);
        Assert.Equal("assigned", assignedStatus);
        Assert.Single(fixture.Store.Items);
    }

    private sealed class Fixture
    {
        public Fixture()
        {
            Store = new FakeBlockStore();
            Manager = new AccountBlockManager(Store, new FakeIdentityStore());
        }

        public FakeBlockStore Store { get; }
        public AccountBlockManager Manager { get; }
    }

    private sealed class FakeBlockStore : IAccountBlockStore
    {
        public List<AccountBlock> Items { get; } = [];

        public Task<bool> AddAsync(AccountBlock block)
        {
            Items.Add(block);
            return Task.FromResult(true);
        }

        public Task<bool> ExistsAsync(string blockerId, string blockedId) =>
            Task.FromResult(Items.Any(item => item.BlockerId == blockerId && item.BlockedId == blockedId));
    }

    private sealed class FakeIdentityStore : IAccountIdentityStore
    {
        public Task<AppUser?> FindByIdAsync(string userId) =>
            Task.FromResult<AppUser?>(userId == "blocker" || userId == "target"
                ? new AppUser { Id = userId, EmailConfirmed = true, DeclaredAdult = true }
                : null);

        public Task<AccountCreationResult> CreateAdultAsync(string email, string password) => throw new NotSupportedException();
        public Task<AccountIdentityLoginStatus> SignInAsync(string email, string password) => throw new NotSupportedException();
        public Task<bool> ConfirmEmailAsync(string userId, string token) => throw new NotSupportedException();
        public Task SignOutAsync() => throw new NotSupportedException();
        public Task SaveSearchPreferenceAsync(AppUser user) => throw new NotSupportedException();
    }
}
