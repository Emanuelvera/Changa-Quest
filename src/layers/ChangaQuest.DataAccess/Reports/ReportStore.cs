using ChangaQuest.DataAccess.Accounts;
using ChangaQuest.Entities.Reports;
using Microsoft.EntityFrameworkCore;

namespace ChangaQuest.DataAccess.Reports;

public sealed class ReportStore(ApplicationDbContext context) : IReportStore
{
    public async Task AddAsync(Report report)
    {
        context.Reports.Add(report);
        await context.SaveChangesAsync();
    }

    public Task<Report?> FindByIdAsync(Guid id) =>
        context.Reports.AsNoTracking().SingleOrDefaultAsync(report => report.Id == id);
}
