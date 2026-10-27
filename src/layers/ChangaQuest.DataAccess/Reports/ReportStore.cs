using System.Data;
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
        context.Reports.AsNoTracking()
            .AsSplitQuery()
            .Include(report => report.Responses)
            .Include(report => report.Appeals)
            .Include(report => report.Changa)
                .ThenInclude(changa => changa.Assignment)
            .SingleOrDefaultAsync(report => report.Id == id);

    public async Task<IReadOnlyList<Report>> ListAsync(string? participantId)
    {
        IQueryable<Report> query = context.Reports.AsNoTracking()
            .Include(report => report.Changa)
                .ThenInclude(changa => changa.Assignment);
        if (participantId is not null)
        {
            query = query.Where(report =>
                report.Changa.AuthorId == participantId ||
                report.Changa.Assignment != null && report.Changa.Assignment.WorkerId == participantId);
        }

        return await query
            .OrderByDescending(report => report.CreatedAt)
            .ThenBy(report => report.Id)
            .ToListAsync();
    }

    public async Task<ReportMutationStatus> AddResponseAsync(ReportResponse response)
    {
        context.ChangeTracker.Clear();
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var report = await FindForUpdateAsync(response.ReportId);
        if (report is null)
        {
            return ReportMutationStatus.NotFound;
        }

        if (report.Status != ReportStatus.Pending)
        {
            return ReportMutationStatus.Conflict;
        }

        context.ReportResponses.Add(response);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return ReportMutationStatus.Updated;
    }

    public async Task<ReportMutationStatus> RecordDecisionAsync(
        Guid reportId, string reviewerId, string conclusion, string reason,
        bool evidenceConsidered, DateTimeOffset decidedAt)
    {
        context.ChangeTracker.Clear();
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var report = await FindForUpdateAsync(reportId);
        if (report is null)
        {
            return ReportMutationStatus.NotFound;
        }

        if (report.Status != ReportStatus.Pending)
        {
            return ReportMutationStatus.Conflict;
        }

        report.Status = ReportStatus.Decided;
        report.Conclusion = conclusion;
        report.DecisionReason = reason;
        report.EvidenceConsidered = evidenceConsidered;
        report.DecisionMakerId = reviewerId;
        report.DecidedAt = decidedAt;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return ReportMutationStatus.Updated;
    }

    public async Task<ReportMutationStatus> AddAppealAsync(ReportAppeal appeal)
    {
        context.ChangeTracker.Clear();
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var report = await FindForUpdateAsync(appeal.ReportId);
        if (report is null)
        {
            return ReportMutationStatus.NotFound;
        }

        if (report.Status != ReportStatus.Decided)
        {
            return ReportMutationStatus.Conflict;
        }

        context.ReportAppeals.Add(appeal);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return ReportMutationStatus.Updated;
    }

    public async Task<ReportMutationStatus> ResolveAppealAsync(
        Guid reportId, Guid appealId, string resolverId, string resolution, DateTimeOffset resolvedAt)
    {
        context.ChangeTracker.Clear();
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var report = await FindForUpdateAsync(reportId);
        if (report is null)
        {
            return ReportMutationStatus.NotFound;
        }

        var appeal = await context.ReportAppeals
            .SingleOrDefaultAsync(item => item.Id == appealId && item.ReportId == reportId);
        if (appeal is null)
        {
            return ReportMutationStatus.NotFound;
        }

        if (appeal.ResolvedAt is not null)
        {
            return ReportMutationStatus.Conflict;
        }

        appeal.Resolution = resolution;
        appeal.ResolverId = resolverId;
        appeal.ResolvedAt = resolvedAt;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return ReportMutationStatus.Updated;
    }

    private Task<Report?> FindForUpdateAsync(Guid id) =>
        context.Reports
            .FromSqlInterpolated($"SELECT * FROM [Reports] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {id}")
            .SingleOrDefaultAsync();
}
