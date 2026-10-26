using ChangaQuest.Entities.Changas;

namespace ChangaQuest.DataAccess.Changas;

public interface IChangaStore
{
    Task AddAsync(Changa changa);
    Task<IReadOnlyList<Changa>> ListAvailableAsync();
    Task<IReadOnlyList<Changa>> ListAvailableForSearchAsync();
    Task<Changa?> FindAvailableByIdAsync(Guid id);
    Task<Changa?> FindByIdAsync(Guid id);
    Task<ChangaTakePersistenceResult> TakeAsync(Guid id, string workerId);
    Task SetPhoneConsentAsync(Guid id, bool isAuthor, PhoneConsentStatus status, string? phone);
    Task<IReadOnlyList<ChangaAssignment>> ListAssignmentsForAccountAsync(string userId);
    Task EnsureSamplesAsync(IReadOnlyCollection<Changa> samples);
    Task<ChangaCancellationPersistenceResult> CreateCancellationRequestAsync(
        Guid changaId, string requestedById, string targetId);
    Task<ChangaCancellationPersistenceResult> RespondCancellationRequestAsync(
        Guid changaId, Guid requestId, string responderId, bool accept);
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
