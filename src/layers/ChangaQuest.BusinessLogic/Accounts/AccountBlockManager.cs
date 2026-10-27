using ChangaQuest.DataAccess.Accounts;
using ChangaQuest.Entities.Accounts;

namespace ChangaQuest.BusinessLogic.Accounts;

public sealed class AccountBlockManager(
    IAccountBlockStore blockStore,
    IAccountIdentityStore accountStore,
    TimeProvider? timeProvider = null)
{
    public async Task<AccountBlockManagerResult> BlockAsync(string? blockerId, string? blockedId)
    {
        if (string.IsNullOrWhiteSpace(blockerId))
            return new(AccountBlockManagerStatus.Unauthenticated);
        if (string.Equals(blockerId, blockedId, StringComparison.Ordinal))
            return new(AccountBlockManagerStatus.CannotBlockSelf);
        if (string.IsNullOrWhiteSpace(blockedId) || await accountStore.FindByIdAsync(blockedId) is null)
            return new(AccountBlockManagerStatus.AccountNotFound);
        if (await blockStore.ExistsAsync(blockerId, blockedId))
            return new(AccountBlockManagerStatus.AlreadyBlocked);

        var created = await blockStore.AddAsync(new AccountBlock
        {
            Id = Guid.NewGuid(),
            BlockerId = blockerId,
            BlockedId = blockedId,
            CreatedAt = (timeProvider ?? TimeProvider.System).GetUtcNow()
        });
        return new(created
            ? AccountBlockManagerStatus.Created
            : AccountBlockManagerStatus.AlreadyBlocked);
    }
}

public enum AccountBlockManagerStatus
{
    Created,
    AlreadyBlocked,
    Unauthenticated,
    AccountNotFound,
    CannotBlockSelf
}

public sealed record AccountBlockManagerResult(AccountBlockManagerStatus Status);
