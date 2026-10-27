namespace ChangaQuest.Entities.Changas;

public sealed class Changa
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public int Reward { get; set; }
    public string Zone { get; set; } = string.Empty;
    public string ExactAddress { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool MaterialsNeeded { get; set; }
    public MaterialsProvider? MaterialsProvider { get; set; }
    public AssignmentMode AssignmentMode { get; set; }
    public ChangaStatus Status { get; set; }
    public string? ReviewReason { get; set; }
    public string? AuthorId { get; set; }
    public bool IsSample { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ChangaAssignment? Assignment { get; set; }
}

public enum ChangaStatus
{
    Available,
    Assigned,
    Cancelled,
    PendingReview,
    Rejected,
    PendingResponse,
    Confirmed,
    CompletionReview,
    Completed
}

public sealed class ChangaCategory
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
}

public static class ChangaCategoryCatalog
{
    public static IReadOnlyList<ChangaCategory> Approved { get; } =
    [
        new() { Id = "limpieza-domestica", Name = "Limpieza doméstica", IsApproved = true },
        new() { Id = "cocina-preparacion-basica", Name = "Cocina o preparación básica", IsApproved = true },
        new() { Id = "compras-mandados", Name = "Compras y mandados", IsApproved = true },
        new() { Id = "jardineria-basica", Name = "Jardinería básica", IsApproved = true },
        new() { Id = "organizacion-hogar", Name = "Organización del hogar", IsApproved = true },
        new() { Id = "cuidado-mascotas", Name = "Cuidado de mascotas", IsApproved = true }
    ];
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
    public const int CompletionResponseMinutes = 20;
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
    public DateTimeOffset? CompletionRequestedAt { get; set; }
    public DateTimeOffset? CompletionDeadline { get; set; }
    public DateTimeOffset? CompletionRespondedAt { get; set; }
    public string? CompletionReviewCause { get; set; }
    public DateTimeOffset? RequesterPaymentDeclaredAt { get; set; }
    public DateTimeOffset? WorkerPaymentDeclaredAt { get; set; }
    public Changa Changa { get; set; } = null!;
}

public sealed class ChangaRating
{
    public Guid Id { get; set; }
    public Guid ChangaId { get; set; }
    public string RaterId { get; set; } = string.Empty;
    public string RateeId { get; set; } = string.Empty;
    public int Score { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Changa Changa { get; set; } = null!;
}
