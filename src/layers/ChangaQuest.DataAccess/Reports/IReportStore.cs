using ChangaQuest.Entities.Reports;

namespace ChangaQuest.DataAccess.Reports;

public interface IReportStore
{
    Task AddAsync(Report report);
    Task<Report?> FindByIdAsync(Guid id);
}
