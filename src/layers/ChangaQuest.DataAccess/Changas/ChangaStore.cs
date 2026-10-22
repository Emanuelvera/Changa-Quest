using System.Data;
using ChangaQuest.Entities.Changas;
using ChangaQuest.DataAccess.Accounts;
using Microsoft.EntityFrameworkCore;

namespace ChangaQuest.DataAccess.Changas;

public sealed class ChangaStore(ApplicationDbContext context) : IChangaStore
{
    public async Task AddAsync(Changa changa)
    {
        context.Changas.Add(changa);
        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Changa>> ListAvailableAsync() =>
        await context.Changas
            .AsNoTracking()
            .Where(changa => changa.Status == ChangaStatus.Available)
            .OrderByDescending(changa => changa.CreatedAt)
            .ToArrayAsync();

    public Task<Changa?> FindAvailableByIdAsync(Guid id) =>
        context.Changas
            .AsNoTracking()
            .SingleOrDefaultAsync(changa => changa.Id == id && changa.Status == ChangaStatus.Available);

    public Task<Changa?> FindByIdAsync(Guid id) =>
        context.Changas
            .Include(changa => changa.Assignment)
            .SingleOrDefaultAsync(changa => changa.Id == id);

    public async Task<ChangaTakePersistenceResult> TakeAsync(Guid id, string workerId)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var changa = await context.Changas
            .FromSqlInterpolated(
                $"SELECT * FROM [Changas] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {id}")
            .SingleOrDefaultAsync();
        if (changa is null)
        {
            return ChangaTakePersistenceResult.NotFound;
        }

        var assignment = await context.Set<ChangaAssignment>()
            .SingleOrDefaultAsync(item => item.ChangaId == id);
        if (assignment is not null)
        {
            return assignment.WorkerId == workerId
                ? ChangaTakePersistenceResult.AlreadyAssignedToWorker
                : ChangaTakePersistenceResult.AlreadyAssignedToOther;
        }

        if (changa.Status != ChangaStatus.Available)
        {
            return ChangaTakePersistenceResult.NotAvailable;
        }

        var assignmentId = Guid.NewGuid();
        var assignedAt = DateTimeOffset.UtcNow;
        var updated = await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [Changas] SET [Status] = N'Assigned' WHERE [Id] = {id} AND [Status] = N'Available'");
        if (updated != 1)
        {
            return ChangaTakePersistenceResult.NotAvailable;
        }

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO [ChangaAssignment] ([Id], [ChangaId], [WorkerId], [AssignedAt]) VALUES ({assignmentId}, {id}, {workerId}, {assignedAt})");
        context.ChangeTracker.Clear();
        await transaction.CommitAsync();
        return ChangaTakePersistenceResult.Assigned;
    }

    public async Task EnsureSamplesAsync(IReadOnlyCollection<Changa> samples)
    {
        var ids = samples.Select(sample => sample.Id).ToArray();
        var existingIds = await context.Changas
            .Where(changa => ids.Contains(changa.Id))
            .Select(changa => changa.Id)
            .ToHashSetAsync();
        var missing = samples.Where(sample => !existingIds.Contains(sample.Id)).ToArray();
        if (missing.Length == 0)
        {
            return;
        }

        context.Changas.AddRange(missing);
        await context.SaveChangesAsync();
    }
}
