namespace ChangaQuest.AcceptanceTests.Contracts;

public class OpenApiContractTests
{
    [Fact]
    public void Versioned_openapi_contract_describes_account_registration_confirmation_and_login()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Contracts", "openapi-v1.yaml");
        var contract = File.ReadAllText(path);

        Assert.Contains("openapi: 3.", contract, StringComparison.Ordinal);
        Assert.Contains("/api/v1/accounts/register:", contract, StringComparison.Ordinal);
        Assert.Contains("/api/v1/accounts/confirm-email:", contract, StringComparison.Ordinal);
        Assert.Contains("/api/v1/accounts/login:", contract, StringComparison.Ordinal);
        Assert.Contains("declaredAdult:", contract, StringComparison.Ordinal);
        Assert.Contains("application/problem+json", contract, StringComparison.Ordinal);
        Assert.Contains("HttpOnly", contract, StringComparison.Ordinal);
    }
}
