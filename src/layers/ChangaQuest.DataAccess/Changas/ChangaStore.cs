using System.Data;
using ChangaQuest.Entities.Changas;
using ChangaQuest.DataAccess.Accounts;
using Microsoft.Data.SqlClient;
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

    public Task<IReadOnlyList<Changa>> ListAvailableForSearchAsync() =>
        ListAvailableAsync();

    public Task<Changa?> FindAvailableByIdAsync(Guid id) =>
        context.Changas
            .AsNoTracking()
            .SingleOrDefaultAsync(changa => changa.Id == id && changa.Status == ChangaStatus.Available);

    public Task<Changa?> FindByIdAsync(Guid id) =>
        context.Changas
            .Include(changa => changa.Assignment)
            .Include(changa => changa.CancellationRequests)
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

    public async Task SetPhoneConsentAsync(Guid id, bool isAuthor, PhoneConsentStatus status, string? phone)
    {
        var changa = await context.Changas
            .Include(item => item.Assignment)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (changa?.Assignment is null)
        {
            return;
        }

        if (isAuthor)
        {
            changa.Assignment.RequesterPhoneConsent = status;
            changa.Assignment.RequesterPhone = phone;
        }
        else
        {
            changa.Assignment.WorkerPhoneConsent = status;
            changa.Assignment.WorkerPhone = phone;
        }

        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<ChangaAssignment>> ListAssignmentsForAccountAsync(string userId) =>
        await context.Set<ChangaAssignment>()
            .AsNoTracking()
            .Include(assignment => assignment.Changa)
            .ThenInclude(changa => changa.CancellationRequests)
            .Where(assignment => assignment.WorkerId == userId || assignment.Changa.AuthorId == userId)
            .OrderByDescending(assignment => assignment.AssignedAt)
            .ToArrayAsync();

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

    public async Task<ChangaCancellationPersistenceResult> CreateCancellationRequestAsync(
        Guid changaId, string requestedById, string targetId)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var changa = await context.Changas
            .Include(item => item.Assignment)
            .SingleOrDefaultAsync(item => item.Id == changaId);
        if (changa is null)
        {
            return new(ChangaCancellationPersistenceStatus.NotFound);
        }

        if (changa.Status != ChangaStatus.Assigned || changa.Assignment is null)
        {
            return new(ChangaCancellationPersistenceStatus.Conflict);
        }

        if (changa.AuthorId != requestedById && changa.Assignment.WorkerId != requestedById)
        {
            return new(ChangaCancellationPersistenceStatus.Forbidden);
        }

        var expectedTargetId = changa.AuthorId == requestedById
            ? changa.Assignment.WorkerId
            : changa.AuthorId;
        if (string.IsNullOrWhiteSpace(expectedTargetId) || expectedTargetId != targetId)
        {
            return new(ChangaCancellationPersistenceStatus.Forbidden);
        }

        var pending = await context.Set<ChangaCancellationRequest>()
            .AnyAsync(item => item.ChangaId == changaId &&
                              item.Status == ChangaCancellationRequestStatus.Pending);
        if (pending)
        {
            return new(ChangaCancellationPersistenceStatus.Conflict);
        }

        var request = new ChangaCancellationRequest
        {
            Id = Guid.NewGuid(),
            ChangaId = changaId,
            RequestedById = requestedById,
            TargetId = targetId,
            Status = ChangaCancellationRequestStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            Changa = changa
        };
        context.Set<ChangaCancellationRequest>().Add(request);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return new(ChangaCancellationPersistenceStatus.Conflict);
        }

        await transaction.CommitAsync();
        return new(ChangaCancellationPersistenceStatus.Created, request);
    }

    public async Task<ChangaCancellationPersistenceResult> RespondCancellationRequestAsync(
        Guid changaId, Guid requestId, string responderId, bool accept)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var changa = await context.Changas
            .Include(item => item.Assignment)
            .SingleOrDefaultAsync(item => item.Id == changaId);
        var request = await context.Set<ChangaCancellationRequest>()
            .SingleOrDefaultAsync(item => item.Id == requestId && item.ChangaId == changaId);
        if (changa is null || request is null)
        {
            return new(ChangaCancellationPersistenceStatus.NotFound);
        }

        if (request.TargetId != responderId)
        {
            return new(ChangaCancellationPersistenceStatus.Forbidden);
        }

        if (request.Status != ChangaCancellationRequestStatus.Pending ||
            changa.Status != ChangaStatus.Assigned ||
            changa.Assignment is null ||
            (responderId != changa.AuthorId && responderId != changa.Assignment.WorkerId))
        {
            return new(ChangaCancellationPersistenceStatus.Conflict);
        }

        request.Status = accept
            ? ChangaCancellationRequestStatus.Accepted
            : ChangaCancellationRequestStatus.Rejected;
        request.RespondedAt = DateTimeOffset.UtcNow;
        if (accept)
        {
            changa.Status = ChangaStatus.Cancelled;
            context.Set<ChangaAssignment>().Remove(changa.Assignment);
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return new(
            accept ? ChangaCancellationPersistenceStatus.Accepted : ChangaCancellationPersistenceStatus.Rejected,
            request);
    }
}
