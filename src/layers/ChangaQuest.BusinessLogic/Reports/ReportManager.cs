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
            CreatedAt = CurrentTime()
        };
        await reportStore.AddAsync(report);
        return new ReportManagerResult(
            ReportManagerStatus.Success,
            Submitted: new ReportSubmittedView(report.Id, "pending", report.CreatedAt));
    }

    public async Task<ReportManagerResult> GetAsync(string? userId, Guid id)
    {
        var access = await CheckAdministratorAsync(userId);
        if (access is not null)
        {
            return new ReportManagerResult(access.Value);
        }

        var report = await reportStore.FindByIdAsync(id);
        return report is null
            ? new ReportManagerResult(ReportManagerStatus.ReportNotFound)
            : new ReportManagerResult(ReportManagerStatus.Success, Report: ToAdminView(report));
    }

    public async Task<ReportManagerResult> GetCaseAsync(string? userId, Guid id)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new ReportManagerResult(ReportManagerStatus.Unauthenticated);
        }

        var report = await reportStore.FindByIdAsync(id);
        if (report is null)
        {
            return new ReportManagerResult(ReportManagerStatus.ReportNotFound);
        }

        if (!IsParty(report, userId))
        {
            return new ReportManagerResult(ReportManagerStatus.PartyForbidden);
        }

        return new ReportManagerResult(
            ReportManagerStatus.Success,
            Case: new ReportPartyCaseView(
                report.Id,
                report.ChangaId,
                ToStatus(report.Status),
                report.Status == ReportStatus.Pending,
                report.Status == ReportStatus.Decided,
                report.Responses.Where(response => response.AuthorId == userId).Select(ToView).ToArray(),
                ToDecisionView(report),
                report.Appeals.Where(appeal => appeal.AppellantId == userId).Select(ToView).ToArray()));
    }

    public async Task<ReportManagerResult> SubmitResponseAsync(
        string? userId, Guid id, ReportTextCommand command)
    {
        var (report, error) = await FindPartyReportAsync(userId, id);
        if (error is not null)
        {
            return new ReportManagerResult(error.Value);
        }

        if (!TryValidateText("text", command.Text, ReportPolicy.MaximumResponseLength, out var text, out var errors))
        {
            return new ReportManagerResult(ReportManagerStatus.Invalid, Errors: errors);
        }

        if (report!.Status != ReportStatus.Pending)
        {
            return new ReportManagerResult(ReportManagerStatus.CaseClosed);
        }

        var response = new ReportResponse
        {
            Id = Guid.NewGuid(),
            ReportId = report.Id,
            AuthorId = userId!,
            Text = text,
            CreatedAt = CurrentTime()
        };
        var result = await reportStore.AddResponseAsync(response);
        return result switch
        {
            ReportMutationStatus.Updated => new ReportManagerResult(ReportManagerStatus.Success, Response: ToView(response)),
            ReportMutationStatus.NotFound => new ReportManagerResult(ReportManagerStatus.ReportNotFound),
            _ => new ReportManagerResult(ReportManagerStatus.CaseClosed)
        };
    }

    public async Task<ReportManagerResult> DecideAsync(
        string? userId, Guid id, ReportDecisionCommand command)
    {
        var access = await CheckAdministratorAsync(userId);
        if (access is not null)
        {
            return new ReportManagerResult(access.Value);
        }

        var validConclusion = TryValidateText(
            "conclusion", command.Conclusion, ReportPolicy.MaximumConclusionLength,
            out var conclusion, out var errors);
        var validReason = TryValidateText(
            "reason", command.Reason, ReportPolicy.MaximumDecisionReasonLength,
            out var reason, out var reasonErrors);
        if (!validConclusion || !validReason || command.EvidenceConsidered is null)
        {
            foreach (var (key, value) in reasonErrors)
            {
                errors[key] = value;
            }

            if (command.EvidenceConsidered is null)
            {
                errors["evidenceConsidered"] = ["Indicá si se consideró la evidencia textual."];
            }

            return new ReportManagerResult(ReportManagerStatus.Invalid, Errors: errors);
        }

        if (await reportStore.FindByIdAsync(id) is null)
        {
            return new ReportManagerResult(ReportManagerStatus.ReportNotFound);
        }

        var now = CurrentTime();
        var result = await reportStore.RecordDecisionAsync(
            id, userId!, conclusion, reason, command.EvidenceConsidered.Value, now);
        return result switch
        {
            ReportMutationStatus.Updated => new ReportManagerResult(
                ReportManagerStatus.Success,
                Decision: new ReportDecisionView(userId!, conclusion, reason, command.EvidenceConsidered.Value, now)),
            ReportMutationStatus.NotFound => new ReportManagerResult(ReportManagerStatus.ReportNotFound),
            _ => new ReportManagerResult(ReportManagerStatus.DecisionExists)
        };
    }

    public async Task<ReportManagerResult> AppealAsync(
        string? userId, Guid id, ReportTextCommand command)
    {
        var (report, error) = await FindPartyReportAsync(userId, id);
        if (error is not null)
        {
            return new ReportManagerResult(error.Value);
        }

        if (!TryValidateText("text", command.Text, ReportPolicy.MaximumAppealLength, out var text, out var errors))
        {
            return new ReportManagerResult(ReportManagerStatus.Invalid, Errors: errors);
        }

        if (report!.Status != ReportStatus.Decided)
        {
            return new ReportManagerResult(ReportManagerStatus.DecisionMissing);
        }

        var appeal = new ReportAppeal
        {
            Id = Guid.NewGuid(),
            ReportId = report.Id,
            AppellantId = userId!,
            Text = text,
            CreatedAt = CurrentTime()
        };
        var result = await reportStore.AddAppealAsync(appeal);
        return result switch
        {
            ReportMutationStatus.Updated => new ReportManagerResult(ReportManagerStatus.Success, Appeal: ToView(appeal)),
            ReportMutationStatus.NotFound => new ReportManagerResult(ReportManagerStatus.ReportNotFound),
            _ => new ReportManagerResult(ReportManagerStatus.DecisionMissing)
        };
    }

    public async Task<ReportManagerResult> ResolveAppealAsync(
        string? userId, Guid id, Guid appealId, ReportAppealResolutionCommand command)
    {
        var access = await CheckAdministratorAsync(userId);
        if (access is not null)
        {
            return new ReportManagerResult(access.Value);
        }

        if (!TryValidateText(
                "resolution", command.Resolution, ReportPolicy.MaximumAppealLength,
                out var resolution, out var errors))
        {
            return new ReportManagerResult(ReportManagerStatus.Invalid, Errors: errors);
        }

        var now = CurrentTime();
        var result = await reportStore.ResolveAppealAsync(id, appealId, userId!, resolution, now);
        if (result != ReportMutationStatus.Updated)
        {
            return new ReportManagerResult(result == ReportMutationStatus.NotFound
                ? ReportManagerStatus.AppealNotFound
                : ReportManagerStatus.AppealResolved);
        }

        var savedReport = await reportStore.FindByIdAsync(id);
        var savedAppeal = savedReport?.Appeals.SingleOrDefault(appeal => appeal.Id == appealId);
        if (savedAppeal is null)
        {
            return new ReportManagerResult(ReportManagerStatus.AppealNotFound);
        }

        return new ReportManagerResult(
            ReportManagerStatus.Success,
            Appeal: ToView(savedAppeal));
    }

    private async Task<(Report? Report, ReportManagerStatus? Error)> FindPartyReportAsync(string? userId, Guid id)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return (null, ReportManagerStatus.Unauthenticated);
        }

        var report = await reportStore.FindByIdAsync(id);
        if (report is null)
        {
            return (null, ReportManagerStatus.ReportNotFound);
        }

        return IsParty(report, userId)
            ? (report, null)
            : (null, ReportManagerStatus.PartyForbidden);
    }

    private async Task<ReportManagerStatus?> CheckAdministratorAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return ReportManagerStatus.Unauthenticated;
        }

        return await accountStore.IsInRoleAsync(userId, ReportPolicy.AdministratorRole)
            ? null
            : ReportManagerStatus.AdminForbidden;
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

    private static bool IsParty(Report report, string userId) =>
        report.Changa.AuthorId == userId || report.Changa.Assignment?.WorkerId == userId;

    private static string ToStatus(ReportStatus status) => status == ReportStatus.Decided ? "decided" : "pending";

    private static ReportAdminView ToAdminView(Report report) => new(
        report.Id,
        report.ChangaId,
        report.ReporterId,
        report.Reason,
        report.Evidence,
        ToStatus(report.Status),
        report.CreatedAt,
        report.Responses.Select(ToView).ToArray(),
        ToDecisionView(report),
        report.Appeals.Select(ToView).ToArray());

    private static ReportDecisionView? ToDecisionView(Report report) =>
        report.DecidedAt is null || report.DecisionMakerId is null ||
        report.Conclusion is null || report.DecisionReason is null || report.EvidenceConsidered is null
            ? null
            : new ReportDecisionView(
                report.DecisionMakerId,
                report.Conclusion,
                report.DecisionReason,
                report.EvidenceConsidered.Value,
                report.DecidedAt.Value);

    private static ReportResponseView ToView(ReportResponse response) =>
        new(response.Id, response.AuthorId, response.Text, response.CreatedAt);

    private static ReportAppealView ToView(ReportAppeal appeal) =>
        new(
            appeal.Id,
            appeal.AppellantId,
            appeal.Text,
            appeal.CreatedAt,
            appeal.ResolvedAt is null ? "pending" : "resolved",
            appeal.Resolution,
            appeal.ResolverId,
            appeal.ResolvedAt);

    private static bool TryValidateText(
        string field, string? value, int maximumLength, out string text, out Dictionary<string, string[]> errors)
    {
        text = value?.Trim() ?? string.Empty;
        errors = new Dictionary<string, string[]>();
        if (text.Length == 0)
        {
            errors[field] = ["Este campo es obligatorio."];
        }
        else if (text.Length > maximumLength)
        {
            errors[field] = [$"Este campo no puede superar los {maximumLength} caracteres."];
        }

        return errors.Count == 0;
    }

    private DateTimeOffset CurrentTime() => (timeProvider ?? TimeProvider.System).GetUtcNow();
}

public sealed record ReportSubmitCommand(Guid ChangaId, string? Reason, string? Evidence);
public sealed record ReportTextCommand(string? Text);
public sealed record ReportDecisionCommand(string? Conclusion, string? Reason, bool? EvidenceConsidered);
public sealed record ReportAppealResolutionCommand(string? Resolution);

public enum ReportManagerStatus
{
    Success,
    Invalid,
    Unauthenticated,
    Forbidden,
    ChangaNotFound,
    ReportNotFound,
    AdminForbidden,
    PartyForbidden,
    CaseClosed,
    DecisionExists,
    DecisionMissing,
    AppealNotFound,
    AppealResolved
}

public sealed record ReportSubmittedView(Guid Id, string Status, DateTimeOffset CreatedAt);
public sealed record ReportResponseView(Guid Id, string AuthorId, string Text, DateTimeOffset CreatedAt);
public sealed record ReportDecisionView(
    string ReviewerId, string Conclusion, string Reason, bool EvidenceConsidered, DateTimeOffset DecidedAt);
public sealed record ReportAppealView(
    Guid Id, string AppellantId, string Text, DateTimeOffset CreatedAt, string Status,
    string? Resolution, string? ResolverId, DateTimeOffset? ResolvedAt);
public sealed record ReportAdminView(
    Guid Id, Guid ChangaId, string ReporterId, string Reason, string? Evidence, string Status,
    DateTimeOffset CreatedAt, IReadOnlyList<ReportResponseView> Responses,
    ReportDecisionView? Decision, IReadOnlyList<ReportAppealView> Appeals);
public sealed record ReportPartyCaseView(
    Guid Id, Guid ChangaId, string Status, bool CanRespond, bool CanAppeal,
    IReadOnlyList<ReportResponseView> MyResponses, ReportDecisionView? Decision,
    IReadOnlyList<ReportAppealView> MyAppeals);
public sealed record ReportManagerResult(
    ReportManagerStatus Status,
    ReportSubmittedView? Submitted = null,
    ReportAdminView? Report = null,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    ReportPartyCaseView? Case = null,
    ReportResponseView? Response = null,
    ReportDecisionView? Decision = null,
    ReportAppealView? Appeal = null);
