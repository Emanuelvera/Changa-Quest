namespace ChangaQuest.Entities.Changas;

public sealed class Changa
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Reward { get; set; }
    public string Zone { get; set; } = string.Empty;
    public string ExactAddress { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool MaterialsNeeded { get; set; }
    public MaterialsProvider? MaterialsProvider { get; set; }
    public AssignmentMode AssignmentMode { get; set; }
    public ChangaStatus Status { get; set; }
    public string? AuthorId { get; set; }
    public bool IsSample { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ChangaAssignment? Assignment { get; set; }
    public ICollection<ChangaCancellationRequest> CancellationRequests { get; set; } = [];
}

public sealed class ChangaCancellationRequest
{
    public Guid Id { get; set; }
    public Guid ChangaId { get; set; }
    public string RequestedById { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public ChangaCancellationRequestStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
    public Changa Changa { get; set; } = null!;
}

public enum ChangaCancellationRequestStatus
{
    Pending,
    Accepted,
    Rejected
}

public enum ChangaStatus
{
    Available,
    Assigned,
    Cancelled
}

public enum AssignmentMode
{
    FirstCome
}

public enum MaterialsProvider
{
    Requester,
    Worker
}

public static class ChangaPolicy
{
    public const int MinimumDescriptionLength = 10;
    public const int MaximumDescriptionLength = 1000;
    public const int MinimumReward = 1;
    public const int MaximumReward = 10_000_000;
    public const int MinimumZoneLength = 3;
    public const int MaximumZoneLength = 80;
    public const int MinimumExactAddressLength = 5;
    public const int MaximumExactAddressLength = 200;
    public const int MinimumPhoneDigits = 8;
    public const int MaximumPhoneDigits = 15;
    public const int MaximumPhoneLength = 32;
    public const double DefaultSearchLatitude = -31.4167;
    public const double DefaultSearchLongitude = -64.1833;
}

public enum PhoneConsentStatus
{
    Pending,
    Accepted,
    Declined
}

public sealed class ChangaAssignment
{
    public Guid Id { get; set; }
    public Guid ChangaId { get; set; }
    public string WorkerId { get; set; } = string.Empty;
    public DateTimeOffset AssignedAt { get; set; }
    public PhoneConsentStatus RequesterPhoneConsent { get; set; }
    public string? RequesterPhone { get; set; }
    public PhoneConsentStatus WorkerPhoneConsent { get; set; }
    public string? WorkerPhone { get; set; }
    public Changa Changa { get; set; } = null!;
}
