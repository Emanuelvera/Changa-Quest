using ChangaQuest.Entities.Accounts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ChangaQuest.DataAccess.Accounts;

public sealed class AccountBlockStore(ApplicationDbContext context) : IAccountBlockStore
{
    public Task<bool> ExistsAsync(string blockerId, string blockedId) =>
        context.AccountBlocks.AnyAsync(block =>
            block.BlockerId == blockerId && block.BlockedId == blockedId);

    public async Task<bool> AddAsync(AccountBlock block)
    {
        context.AccountBlocks.Add(block);
        try
        {
            await context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return false;
        }
    }
}
