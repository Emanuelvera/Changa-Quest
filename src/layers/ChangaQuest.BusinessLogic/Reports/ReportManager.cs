using ChangaQuest.DataAccess.Accounts;
using ChangaQuest.DataAccess.Changas;
using ChangaQuest.DataAccess.Reports;
using ChangaQuest.Entities.Reports;

namespace ChangaQuest.BusinessLogic.Reports;

public sealed class ReportManager(
    IReportStore reportStore,
    IChangaStore changaStore,
    IAccountIdentityStore accountStore,
    TimeProvider? timeProvider = null)
{
    public async Task<ReportManagerResult> SubmitAsync(string? userId, ReportSubmitCommand command)
    {
        var access = await CheckAccessAsync(userId);
        if (access is not null)
        {
            return access;
        }

        var reason = command.Reason?.Trim();
        var evidence = string.IsNullOrWhiteSpace(command.Evidence) ? null : command.Evidence.Trim();
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrEmpty(reason))
        {
            errors["reason"] = ["Indicá el motivo del reporte."];
        }
        else if (reason.Length > ReportPolicy.MaximumReasonLength)
        {
            errors["reason"] = [$"El motivo no puede superar los {ReportPolicy.MaximumReasonLength} caracteres."];
        }

        if (evidence is not null && evidence.Length > ReportPolicy.MaximumEvidenceLength)
        {
            errors["evidence"] = [$"La evidencia no puede superar los {ReportPolicy.MaximumEvidenceLength} caracteres."];
        }

        if (errors.Count > 0)
        {
            return new ReportManagerResult(ReportManagerStatus.Invalid, Errors: errors);
        }

        if (await changaStore.FindByIdAsync(command.ChangaId) is null)
        {
            return new ReportManagerResult(ReportManagerStatus.ChangaNotFound);
        }

        var report = new Report
        {
            Id = Guid.NewGuid(),
            ReporterId = userId!,
            ChangaId = command.ChangaId,
            Reason = reason!,
            Evidence = evidence,
            Status = ReportStatus.Pending,
            CreatedAt = (timeProvider ?? TimeProvider.System).GetUtcNow()
        };
        await reportStore.AddAsync(report);
        return new ReportManagerResult(
            ReportManagerStatus.Success,
            Submitted: new ReportSubmittedView(report.Id, "pending", report.CreatedAt));
    }

    public async Task<ReportManagerResult> GetAsync(string? userId, Guid id)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new ReportManagerResult(ReportManagerStatus.Unauthenticated);
        }

        if (!await accountStore.IsInRoleAsync(userId, ReportPolicy.AdministratorRole))
        {
            return new ReportManagerResult(ReportManagerStatus.AdminForbidden);
        }

        var report = await reportStore.FindByIdAsync(id);
        return report is null
            ? new ReportManagerResult(ReportManagerStatus.ReportNotFound)
            : new ReportManagerResult(
                ReportManagerStatus.Success,
                Report: new ReportAdminView(
                    report.Id,
                    report.ChangaId,
                    report.ReporterId,
                    report.Reason,
                    report.Evidence,
                    "pending",
                    report.CreatedAt));
    }

    private async Task<ReportManagerResult?> CheckAccessAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new ReportManagerResult(ReportManagerStatus.Unauthenticated);
        }

        var user = await accountStore.FindByIdAsync(userId);
        return user is null || !user.EmailConfirmed || !user.DeclaredAdult
            ? new ReportManagerResult(ReportManagerStatus.Forbidden)
            : null;
    }
}

public sealed record ReportSubmitCommand(Guid ChangaId, string? Reason, string? Evidence);

public enum ReportManagerStatus
{
    Success,
    Invalid,
    Unauthenticated,
    Forbidden,
    ChangaNotFound,
    ReportNotFound,
    AdminForbidden
}

public sealed record ReportSubmittedView(Guid Id, string Status, DateTimeOffset CreatedAt);

public sealed record ReportAdminView(
    Guid Id,
    Guid ChangaId,
    string ReporterId,
    string Reason,
    string? Evidence,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record ReportManagerResult(
    ReportManagerStatus Status,
    ReportSubmittedView? Submitted = null,
    ReportAdminView? Report = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);
