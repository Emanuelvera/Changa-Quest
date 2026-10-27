using ChangaQuest.Entities.Changas;

namespace ChangaQuest.DataAccess.Changas;

public interface IChangaStore
{
    Task AddAsync(Changa changa);
    Task<IReadOnlyList<Changa>> ListAvailableAsync();
    Task<IReadOnlyList<Changa>> ListAvailableForSearchAsync();
    Task<IReadOnlyList<Changa>> ListMyUnassignedAsync(string authorId);
    Task<Changa?> FindAvailableByIdAsync(Guid id);
    Task<Changa?> FindByIdAsync(Guid id);
    Task<ChangaTakePersistenceResult> TakeAsync(Guid id, string workerId);
    Task<ChangaCancelPersistenceResult> CancelAsync(Guid id);
    Task<ChangaCompletionTransitionResult> TransitionCompletionAsync(
        Guid id, ChangaStatus expectedStatus, ChangaStatus newStatus,
        DateTimeOffset? requestedAt = null, DateTimeOffset? deadline = null,
        DateTimeOffset? respondedAt = null, string? reviewCause = null);
    Task<ChangaPaymentDeclarationPersistenceResult> DeclarePaymentAsync(
        Guid id, bool requester, DateTimeOffset declaredAt);
    Task SetPhoneConsentAsync(Guid id, bool isAuthor, PhoneConsentStatus status, string? phone);
    Task<IReadOnlyList<ChangaAssignment>> ListAssignmentsForAccountAsync(string userId);
    Task<bool> HasRatingAsync(Guid changaId, string raterId);
    Task<ChangaRatingPersistenceResult> AddRatingAsync(ChangaRating rating);
    Task<ChangaRatingAggregate> GetRatingAggregateAsync(string rateeId);
    Task EnsureSamplesAsync(IReadOnlyCollection<Changa> samples);
    Task<ChangaCancellationPersistenceResult> CreateCancellationRequestAsync(
        Guid changaId, string requestedById, string targetId);
    Task<ChangaCancellationPersistenceResult> RespondCancellationRequestAsync(
        Guid changaId, Guid requestId, string responderId, bool accept);
    Task<IReadOnlyList<ChangaCategory>> ListApprovedCategoriesAsync() =>
        Task.FromResult<IReadOnlyList<ChangaCategory>>(ChangaCategoryCatalog.Approved);
    Task<ChangaCategory?> FindCategoryAsync(string id) =>
        Task.FromResult(ChangaCategoryCatalog.Approved.SingleOrDefault(category => category.Id == id));
    Task SaveAsync(Changa changa) => Task.CompletedTask;
}

public enum ChangaTakePersistenceResult
{
    Assigned,
    AlreadyAssignedToWorker,
    AlreadyAssignedToOther,
    NotAvailable,
    NotFound
}

public sealed record ChangaCancellationPersistenceResult(
    ChangaCancellationPersistenceStatus Status,
    ChangaCancellationRequest? Request = null);

public enum ChangaCancellationPersistenceStatus
{
    Created,
    Accepted,
    Rejected,
    Forbidden,
    Conflict,
    NotFound
}

public enum ChangaCancelPersistenceResult
{
    Cancelled,
    AlreadyAssigned,
    NotAvailable,
    NotFound
}

public enum ChangaCompletionTransitionResult
{
    Updated,
    StateChanged
}

public enum ChangaRatingPersistenceResult
{
    Added,
    AlreadyExists
}

public sealed record ChangaRatingAggregate(double? AverageRating, int RatingCount);

public enum ChangaPaymentDeclarationPersistenceResult
{
    Updated,
    AlreadyDeclared,
    StateChanged
}
