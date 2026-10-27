using ChangaQuest.Entities.Changas;

namespace ChangaQuest.Entities.Reports;

public static class ReportPolicy
{
    public const int MaximumReasonLength = 500;
    public const int MaximumEvidenceLength = 1000;
    public const int MaximumResponseLength = 2000;
    public const int MaximumConclusionLength = 1000;
    public const int MaximumDecisionReasonLength = 2000;
    public const int MaximumAppealLength = 2000;
    public const string AdministratorRole = "Administrator";
}

public enum ReportStatus
{
    Pending,
    Decided
}

public sealed class Report
{
    public Guid Id { get; set; }
    public string ReporterId { get; set; } = string.Empty;
    public Guid ChangaId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Evidence { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; }
    public string? Conclusion { get; set; }
    public string? DecisionReason { get; set; }
    public bool? EvidenceConsidered { get; set; }
    public string? DecisionMakerId { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public Changa Changa { get; set; } = null!;
    public ICollection<ReportResponse> Responses { get; set; } = [];
    public ICollection<ReportAppeal> Appeals { get; set; } = [];
}

public sealed class ReportResponse
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public Report Report { get; set; } = null!;
}

public sealed class ReportAppeal
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public string AppellantId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string? Resolution { get; set; }
    public string? ResolverId { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public Report Report { get; set; } = null!;
}
