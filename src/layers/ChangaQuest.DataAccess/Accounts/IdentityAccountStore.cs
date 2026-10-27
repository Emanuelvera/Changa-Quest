using ChangaQuest.Entities.Accounts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ChangaQuest.DataAccess.Accounts;

public sealed class IdentityAccountStore(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager) : IAccountIdentityStore
{
    public async Task<AccountCreationResult> CreateAdultAsync(string email, string password)
    {
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            DeclaredAdult = true
        };

        IdentityResult result;
        try
        {
            result = await userManager.CreateAsync(user, password);
        }
        catch (DbUpdateException exception) when (
            exception.GetBaseException() is SqlException { Number: 2601 or 2627 })
        {
            return new AccountCreationResult(false, Errors: new Dictionary<string, string[]>
            {
                ["email"] = ["Ese correo ya tiene una cuenta."]
            });
        }
        if (!result.Succeeded)
        {
            var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
            foreach (var error in result.Errors)
            {
                var field = error.Code switch
                {
                    "DuplicateEmail" or "DuplicateUserName" => "email",
                    "InvalidEmail" or "InvalidUserName" or "UserNameTooLong" => "email",
                    _ when error.Code.StartsWith("Password", StringComparison.Ordinal) => "password",
                    _ => "account"
                };
                errors[field] = field switch
                {
                    "email" when error.Code.StartsWith("Duplicate", StringComparison.Ordinal) =>
                        ["Ese correo ya tiene una cuenta."],
                    "email" => ["Ingresá un correo electrónico válido."],
                    "password" => ["La contraseña no cumple los requisitos."],
                    _ => ["No se pudo crear la cuenta. Intentá de nuevo."]
                };
            }

            return new AccountCreationResult(false, Errors: errors);
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        return new AccountCreationResult(true, user.Id, token);
    }

    public async Task<AccountIdentityLoginStatus> SignInAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return AccountIdentityLoginStatus.InvalidCredentials;
        }

        if (!user.DeclaredAdult)
        {
            return AccountIdentityLoginStatus.NotEligible;
        }

        if (!user.EmailConfirmed)
        {
            return AccountIdentityLoginStatus.EmailNotVerified;
        }

        var passwordResult = await signInManager.CheckPasswordSignInAsync(
            user,
            password,
            lockoutOnFailure: false);
        if (!passwordResult.Succeeded)
        {
            return AccountIdentityLoginStatus.InvalidCredentials;
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return AccountIdentityLoginStatus.Succeeded;
    }

    public async Task<bool> ConfirmEmailAsync(string userId, string token)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || user.EmailConfirmed || !user.DeclaredAdult)
        {
            return false;
        }

        var result = await userManager.ConfirmEmailAsync(user, token);
        return result.Succeeded;
    }

    public Task SignOutAsync() => signInManager.SignOutAsync();

    public Task<AppUser?> FindByIdAsync(string userId) => userManager.FindByIdAsync(userId);

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is not null && await userManager.IsInRoleAsync(user, role);
    }

    public async Task SaveSearchPreferenceAsync(AppUser user)
    {
        await userManager.UpdateAsync(user);
    }
}
