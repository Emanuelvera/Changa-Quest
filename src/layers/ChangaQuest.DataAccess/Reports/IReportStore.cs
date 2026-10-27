using ChangaQuest.Entities.Reports;

namespace ChangaQuest.DataAccess.Reports;

public interface IReportStore
{
    Task AddAsync(Report report);
    Task<Report?> FindByIdAsync(Guid id);
    Task<IReadOnlyList<Report>> ListAsync(string? participantId);
    Task<ReportMutationStatus> AddResponseAsync(ReportResponse response);
    Task<ReportMutationStatus> RecordDecisionAsync(
        Guid reportId, string reviewerId, string conclusion, string reason,
        bool evidenceConsidered, DateTimeOffset decidedAt);
    Task<ReportMutationStatus> AddAppealAsync(ReportAppeal appeal);
    Task<ReportMutationStatus> ResolveAppealAsync(
        Guid reportId, Guid appealId, string resolverId, string resolution, DateTimeOffset resolvedAt);
}

public enum ReportMutationStatus
{
    Updated,
    NotFound,
    Conflict
}
