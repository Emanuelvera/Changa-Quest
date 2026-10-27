using ChangaQuest.Entities.Accounts;

namespace ChangaQuest.DataAccess.Accounts;

public interface IAccountBlockStore
{
    Task<bool> ExistsAsync(string blockerId, string blockedId);
    Task<bool> AddAsync(AccountBlock block);
}
