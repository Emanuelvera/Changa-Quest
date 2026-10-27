namespace ChangaQuest.Entities.Reports;

public static class ReportPolicy
{
    public const int MaximumReasonLength = 500;
    public const int MaximumEvidenceLength = 1000;
    public const string AdministratorRole = "Administrator";
}

public enum ReportStatus
{
    Pending
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
}
