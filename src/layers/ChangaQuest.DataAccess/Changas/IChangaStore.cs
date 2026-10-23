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
    Task EnsureSamplesAsync(IReadOnlyCollection<Changa> samples);
}

public enum ChangaTakePersistenceResult
{
    Assigned,
    AlreadyAssignedToWorker,
    AlreadyAssignedToOther,
    NotAvailable,
    NotFound
}
