using ChangaQuest.DataAccess.Accounts;
using ChangaQuest.Entities.Accounts;

namespace ChangaQuest.BusinessLogic.Accounts;

public sealed class AccountManager(IAccountIdentityStore accountStore)
{
    public async Task<AccountRegistrationResult> RegisterAsync(
        string? email,
        string? password,
        bool declaredAdult)
    {
        var validation = AccountRegistrationValidator.Validate(email, password, declaredAdult);
        if (!validation.IsValid)
        {
            return new AccountRegistrationResult(false, Errors: validation.Errors);
        }

        var creation = await accountStore.CreateAdultAsync(email!.Trim(), password!);
        return creation.Succeeded
            ? new AccountRegistrationResult(true, creation.UserId, creation.ConfirmationToken)
            : new AccountRegistrationResult(false, Errors: creation.Errors);
    }

    public async Task<AccountLoginStatus> SignInAsync(string email, string password)
    {
        var status = await accountStore.SignInAsync(email.Trim(), password);
        return status switch
        {
            AccountIdentityLoginStatus.Succeeded => AccountLoginStatus.Succeeded,
            AccountIdentityLoginStatus.EmailNotVerified => AccountLoginStatus.EmailNotVerified,
            AccountIdentityLoginStatus.NotEligible => AccountLoginStatus.NotEligible,
            _ => AccountLoginStatus.InvalidCredentials
        };
    }

    public Task<bool> ConfirmEmailAsync(string userId, string token) =>
        accountStore.ConfirmEmailAsync(userId, token);

    public Task SignOutAsync() => accountStore.SignOutAsync();

    public Task<AppUser?> FindByIdAsync(string userId) =>
        accountStore.FindByIdAsync(userId);
}

public enum AccountLoginStatus
{
    Succeeded,
    InvalidCredentials,
    EmailNotVerified,
    NotEligible
}

public sealed record AccountRegistrationResult(
    bool Succeeded,
    string? UserId = null,
    string? ConfirmationToken = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);
