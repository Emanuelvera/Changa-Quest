using ChangaQuest.Entities.Changas;

namespace ChangaQuest.DataAccess.Changas;

public interface IChangaStore
{
    Task AddAsync(Changa changa);
    Task<IReadOnlyList<Changa>> ListAvailableAsync();
    Task<IReadOnlyList<Changa>> ListAvailableForSearchAsync();
    Task<Changa?> FindAvailableByIdAsync(Guid id);
    Task EnsureSamplesAsync(IReadOnlyCollection<Changa> samples);
}
