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

    public async Task<IReadOnlyList<ChangaCategory>> ListApprovedCategoriesAsync() =>
        await context.ChangaCategories
            .AsNoTracking()
            .Where(category => category.IsApproved)
            .OrderBy(category => category.Name)
            .ToArrayAsync();

    public Task<ChangaCategory?> FindCategoryAsync(string id) =>
        context.ChangaCategories
            .AsNoTracking()
            .SingleOrDefaultAsync(category => category.Id == id && category.IsApproved);

    public Task SaveAsync(Changa changa) => context.SaveChangesAsync();

    public async Task<IReadOnlyList<Changa>> ListAvailableAsync() =>
        await context.Changas
            .AsNoTracking()
            .Where(changa => changa.Status == ChangaStatus.Available &&
                context.ChangaCategories.Any(category =>
                    category.Id == changa.CategoryId && category.IsApproved))
            .OrderByDescending(changa => changa.CreatedAt)
            .ToArrayAsync();

    public Task<IReadOnlyList<Changa>> ListAvailableForSearchAsync() =>
        ListAvailableAsync();

    public async Task<IReadOnlyList<Changa>> ListMyUnassignedAsync(string authorId) =>
        await context.Changas
            .AsNoTracking()
            .Where(changa => changa.AuthorId == authorId &&
                !context.Set<ChangaAssignment>().Any(assignment => assignment.ChangaId == changa.Id))
            .OrderByDescending(changa => changa.CreatedAt)
            .ToArrayAsync();

    public Task<Changa?> FindAvailableByIdAsync(Guid id) =>
        context.Changas
            .AsNoTracking()
            .SingleOrDefaultAsync(changa =>
                changa.Id == id &&
                changa.Status == ChangaStatus.Available &&
                context.ChangaCategories.Any(category =>
                    category.Id == changa.CategoryId && category.IsApproved));

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

    public async Task<ChangaCancelPersistenceResult> CancelAsync(Guid id)
    {
        context.ChangeTracker.Clear();
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var changa = await context.Changas
            .FromSqlInterpolated(
                $"SELECT * FROM [Changas] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {id}")
            .SingleOrDefaultAsync();
        if (changa is null)
        {
            return ChangaCancelPersistenceResult.NotFound;
        }

        var assignmentExists = await context.Set<ChangaAssignment>()
            .AnyAsync(item => item.ChangaId == id);
        if (assignmentExists)
        {
            return ChangaCancelPersistenceResult.AlreadyAssigned;
        }

        if (changa.Status != ChangaStatus.Available)
        {
            return ChangaCancelPersistenceResult.NotAvailable;
        }

        var updated = await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [Changas] SET [Status] = N'Cancelled' WHERE [Id] = {id} AND [Status] = N'Available' AND NOT EXISTS (SELECT 1 FROM [ChangaAssignment] WHERE [ChangaId] = {id})");
        if (updated != 1)
        {
            return ChangaCancelPersistenceResult.NotAvailable;
        }

        await transaction.CommitAsync();
        context.ChangeTracker.Clear();
        return ChangaCancelPersistenceResult.Cancelled;
    }

    public async Task<ChangaCompletionTransitionResult> TransitionCompletionAsync(
        Guid id,
        ChangaStatus expectedStatus,
        ChangaStatus newStatus,
        DateTimeOffset? requestedAt = null,
        DateTimeOffset? deadline = null,
        DateTimeOffset? respondedAt = null,
        string? reviewCause = null)
    {
        context.ChangeTracker.Clear();
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var updated = respondedAt is { } responseTime
            ? await context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [Changas] SET [Status] = {newStatus.ToString()} WHERE [Id] = {id} AND [Status] = {expectedStatus.ToString()} AND EXISTS (SELECT 1 FROM [ChangaAssignment] WHERE [ChangaId] = {id} AND [CompletionDeadline] > {responseTime})")
            : await context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [Changas] SET [Status] = {newStatus.ToString()} WHERE [Id] = {id} AND [Status] = {expectedStatus.ToString()}");
        if (updated != 1)
        {
            return ChangaCompletionTransitionResult.StateChanged;
        }

        var assignmentUpdated = await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [ChangaAssignment] SET [CompletionRequestedAt] = COALESCE({requestedAt}, [CompletionRequestedAt]), [CompletionDeadline] = COALESCE({deadline}, [CompletionDeadline]), [CompletionRespondedAt] = COALESCE({respondedAt}, [CompletionRespondedAt]), [CompletionReviewCause] = {reviewCause} WHERE [ChangaId] = {id}");
        if (assignmentUpdated != 1)
        {
            await transaction.RollbackAsync();
            context.ChangeTracker.Clear();
            return ChangaCompletionTransitionResult.StateChanged;
        }

        await transaction.CommitAsync();
        context.ChangeTracker.Clear();
        return ChangaCompletionTransitionResult.Updated;
    }

    public async Task<ChangaPaymentDeclarationPersistenceResult> DeclarePaymentAsync(
        Guid id, bool requester, DateTimeOffset declaredAt)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var changa = await context.Changas
            .FromSqlInterpolated(
                $"SELECT * FROM [Changas] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {id}")
            .SingleOrDefaultAsync();
        var assignment = await context.Set<ChangaAssignment>()
            .SingleOrDefaultAsync(item => item.ChangaId == id);
        if (changa is null || assignment is null)
        {
            return ChangaPaymentDeclarationPersistenceResult.StateChanged;
        }

        if (changa.Status != ChangaStatus.Confirmed)
        {
            return ChangaPaymentDeclarationPersistenceResult.StateChanged;
        }

        if (requester)
        {
            if (assignment.RequesterPaymentDeclaredAt is not null)
            {
                return ChangaPaymentDeclarationPersistenceResult.AlreadyDeclared;
            }
            assignment.RequesterPaymentDeclaredAt = declaredAt;
        }
        else
        {
            if (assignment.WorkerPaymentDeclaredAt is not null)
            {
                return ChangaPaymentDeclarationPersistenceResult.AlreadyDeclared;
            }
            assignment.WorkerPaymentDeclaredAt = declaredAt;
        }

        if (assignment.RequesterPaymentDeclaredAt is not null &&
            assignment.WorkerPaymentDeclaredAt is not null)
        {
            changa.Status = ChangaStatus.Completed;
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        context.ChangeTracker.Clear();
        return ChangaPaymentDeclarationPersistenceResult.Updated;
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
            .Where(assignment => assignment.WorkerId == userId || assignment.Changa.AuthorId == userId)
            .OrderByDescending(assignment => assignment.AssignedAt)
            .ToArrayAsync();

    public Task<bool> HasRatingAsync(Guid changaId, string raterId) =>
        context.ChangaRatings
            .AsNoTracking()
            .AnyAsync(rating => rating.ChangaId == changaId && rating.RaterId == raterId);

    public async Task<ChangaRatingPersistenceResult> AddRatingAsync(ChangaRating rating)
    {
        context.ChangaRatings.Add(rating);
        try
        {
            await context.SaveChangesAsync();
            return ChangaRatingPersistenceResult.Added;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 } sqlException &&
            sqlException.Message.Contains("IX_ChangaRatings_ChangaId_RaterId", StringComparison.Ordinal))
        {
            context.Entry(rating).State = EntityState.Detached;
            return ChangaRatingPersistenceResult.AlreadyExists;
        }
    }

    public async Task<ChangaRatingAggregate> GetRatingAggregateAsync(string rateeId)
    {
        var aggregate = await context.ChangaRatings
            .AsNoTracking()
            .Where(rating => rating.RateeId == rateeId)
            .GroupBy(_ => 1)
            .Select(group => new ChangaRatingAggregate(
                group.Average(rating => (double)rating.Score),
                group.Count()))
            .SingleOrDefaultAsync();

        return aggregate ?? new ChangaRatingAggregate(null, 0);
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
