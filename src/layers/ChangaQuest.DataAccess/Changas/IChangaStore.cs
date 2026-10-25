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
