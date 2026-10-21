using ChangaQuest.BusinessLogic.Accounts;

namespace ChangaQuest.UnitTests.Accounts;

public class AccountRegistrationValidatorTests
{
    [Fact]
    public void Validate_accepts_a_valid_adult_registration()
    {
        var result = AccountRegistrationValidator.Validate(
            "persona@example.com",
            "una contraseña extensa",
            declaredAdult: true);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("no-es-email", "una contraseña extensa", true, "email")]
    [InlineData("persona@example.com", "corta", true, "password")]
    [InlineData("persona@example.com", "una contraseña extensa", false, "declaredAdult")]
    public void Validate_rejects_ineligible_or_invalid_registration(
        string email,
        string password,
        bool declaredAdult,
        string expectedField)
    {
        var result = AccountRegistrationValidator.Validate(email, password, declaredAdult);

        Assert.False(result.IsValid);
        Assert.Contains(expectedField, result.Errors.Keys);
    }

    [Fact]
    public void Validate_requires_twelve_password_characters()
    {
        var result = AccountRegistrationValidator.Validate(
            "persona@example.com",
            "12345678901",
            declaredAdult: true);

        Assert.False(result.IsValid);
        Assert.Contains("password", result.Errors.Keys);
    }

    [Fact]
    public void Validate_does_not_require_password_composition()
    {
        var result = AccountRegistrationValidator.Validate(
            "persona@example.com",
            "abcdefghijkl",
            declaredAdult: true);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_rejects_email_longer_than_the_identity_column()
    {
        var email = $"{new string('a', 245)}@example.test";

        var result = AccountRegistrationValidator.Validate(
            email,
            "abcdefghijkl",
            declaredAdult: true);

        Assert.False(result.IsValid);
        Assert.Contains("email", result.Errors.Keys);
    }
}
