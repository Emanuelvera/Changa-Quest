using ChangaQuest.Entities.Accounts;

namespace ChangaQuest.DataAccess.Accounts;

public interface IAccountIdentityStore
{
    Task<AccountCreationResult> CreateAdultAsync(string email, string password);
    Task<AccountIdentityLoginStatus> SignInAsync(string email, string password);
    Task<bool> ConfirmEmailAsync(string userId, string token);
    Task SignOutAsync();
    Task<AppUser?> FindByIdAsync(string userId);
    Task SaveSearchPreferenceAsync(AppUser user);
    Task<bool> IsInRoleAsync(string userId, string role) => Task.FromResult(false);
}

public sealed record AccountCreationResult(
    bool Succeeded,
    string? UserId = null,
    string? ConfirmationToken = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);

public enum AccountIdentityLoginStatus
{
    Succeeded,
    InvalidCredentials,
    EmailNotVerified,
    NotEligible
}
