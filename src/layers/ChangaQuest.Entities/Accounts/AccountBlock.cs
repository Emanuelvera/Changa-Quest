namespace ChangaQuest.Entities.Accounts;

public sealed class AccountBlock
{
    public Guid Id { get; set; }
    public string BlockerId { get; set; } = string.Empty;
    public string BlockedId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
