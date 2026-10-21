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
        Assert.Contains("minLength: 12", contract, StringComparison.Ordinal);
        Assert.Contains("application/problem+json", contract, StringComparison.Ordinal);
        Assert.Contains("HttpOnly", contract, StringComparison.Ordinal);

        var registration = ExtractOperation(
            contract,
            "  /api/v1/accounts/register:",
            "  /api/v1/accounts/confirm-email:");
        Assert.Contains("\"202\":", registration, StringComparison.Ordinal);
        Assert.Contains("application/json:", registration, StringComparison.Ordinal);
        Assert.Contains("#/components/schemas/AccountRegistrationAccepted", registration, StringComparison.Ordinal);

        var confirmation = ExtractOperation(
            contract,
            "  /api/v1/accounts/confirm-email:",
            "  /api/v1/accounts/login:");
        Assert.Contains("\"200\":", confirmation, StringComparison.Ordinal);
        Assert.Contains("text/html:", confirmation, StringComparison.Ordinal);
        Assert.Contains("type: string", confirmation, StringComparison.Ordinal);
        Assert.Contains("\"400\":", confirmation, StringComparison.Ordinal);

        var login = ExtractOperation(
            contract,
            "  /api/v1/accounts/login:",
            "  /api/v1/accounts/logout:");
        Assert.Contains("\"200\":", login, StringComparison.Ordinal);
        Assert.Contains("application/json:", login, StringComparison.Ordinal);
        Assert.Contains("#/components/schemas/LoginResponse", login, StringComparison.Ordinal);
    }

    private static string ExtractOperation(string document, string start, string end)
    {
        var startIndex = document.IndexOf(start, StringComparison.Ordinal);
        var endIndex = document.IndexOf(end, startIndex + start.Length, StringComparison.Ordinal);

        Assert.True(startIndex >= 0, $"Missing OpenAPI operation: {start}");
        Assert.True(endIndex > startIndex, $"Missing following operation: {end}");
        return document[startIndex..endIndex];
    }
}
