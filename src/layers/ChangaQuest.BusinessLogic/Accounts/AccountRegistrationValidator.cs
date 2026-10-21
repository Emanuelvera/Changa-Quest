using System.ComponentModel.DataAnnotations;
using ChangaQuest.Entities.Accounts;

namespace ChangaQuest.BusinessLogic.Accounts;

public static class AccountRegistrationValidator
{
    public const int MinimumPasswordLength = AccountPolicy.MinimumPasswordLength;

    public static AccountRegistrationValidationResult Validate(
        string? email,
        string? password,
        bool declaredAdult)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var normalizedEmail = email?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedEmail) ||
            !new EmailAddressAttribute().IsValid(normalizedEmail))
        {
            errors["email"] = ["Ingresá un correo electrónico válido."];
        }
        else if (normalizedEmail.Length > AccountPolicy.MaximumEmailLength)
        {
            errors["email"] = [$"El correo electrónico no puede superar {AccountPolicy.MaximumEmailLength} caracteres."];
        }

        if (password is null || password.Length < MinimumPasswordLength)
        {
            errors["password"] = [$"La contraseña debe tener al menos {MinimumPasswordLength} caracteres."];
        }

        if (!declaredAdult)
        {
            errors["declaredAdult"] = ["Confirmá que tenés 18 años o más para crear una cuenta."];
        }

        return new AccountRegistrationValidationResult(errors);
    }
}
