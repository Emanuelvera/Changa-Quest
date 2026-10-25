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
        Assert.Contains("/api/v1/changas:", contract, StringComparison.Ordinal);
        Assert.Contains("/api/v1/changas/{id}:", contract, StringComparison.Ordinal);
        Assert.Contains("/api/v1/changas/{id}/take:", contract, StringComparison.Ordinal);
        Assert.Contains("changa_already_assigned", contract, StringComparison.Ordinal);
        Assert.Contains("ChangaTakeResponse", contract, StringComparison.Ordinal);
        Assert.Contains("exactAddress:", contract, StringComparison.Ordinal);
        Assert.Contains("materialsNeeded:", contract, StringComparison.Ordinal);
        Assert.Contains("first_come", contract, StringComparison.Ordinal);
        Assert.Contains("assigned", contract, StringComparison.Ordinal);
        Assert.Contains("Ejemplo ficticio", contract, StringComparison.Ordinal);
        Assert.Contains("/api/v1/changas/{id}/coordination:", contract, StringComparison.Ordinal);
        Assert.Contains("/api/v1/changas/{id}/coordination/phone-consent:", contract, StringComparison.Ordinal);
        Assert.Contains("/api/v1/coordinations:", contract, StringComparison.Ordinal);
        Assert.Contains("PhoneConsentRequest", contract, StringComparison.Ordinal);
        Assert.Contains("coordination_forbidden", contract, StringComparison.Ordinal);
        Assert.Contains("changa_not_assigned", contract, StringComparison.Ordinal);

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

    [Fact]
    public void Coordination_contract_describes_the_phone_consent_variants_and_private_responses()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Contracts", "openapi-v1.yaml");
        var contract = File.ReadAllText(path);
        var getCoordination = ExtractOperation(
            contract,
            "  /api/v1/changas/{id}/coordination:",
            "  /api/v1/changas/{id}/coordination/phone-consent:");
        var setConsent = ExtractOperation(
            contract,
            "  /api/v1/changas/{id}/coordination/phone-consent:",
            "  /api/v1/coordinations:");
        var consentRequest = ExtractOperation(
            contract,
            "    PhoneConsentRequest:",
            "    ChangaCoordination:");
        var coordinationSchema = ExtractOperation(
            contract,
            "    ChangaCoordination:",
            "    ChangaTakeResponse:");

        var coordinationList = ExtractOperation(
            contract,
            "  /api/v1/coordinations:",
            "  /api/v1/changas/{id}/take:");
        Assert.Contains("operationId: getChangaCoordination", getCoordination, StringComparison.Ordinal);
        Assert.Contains("#/components/schemas/ChangaCoordination", getCoordination, StringComparison.Ordinal);
        Assert.Contains("coordination_forbidden", getCoordination, StringComparison.Ordinal);
        Assert.Contains("changa_not_assigned", getCoordination, StringComparison.Ordinal);
        Assert.Contains("operationId: setChangaPhoneConsent", setConsent, StringComparison.Ordinal);
        Assert.Contains("#/components/schemas/PhoneConsentRequest", setConsent, StringComparison.Ordinal);
        Assert.Contains("\"400\":", setConsent, StringComparison.Ordinal);
        Assert.Contains("required: [sharePhone, phone]", consentRequest, StringComparison.Ordinal);
        Assert.Contains("enum: [true]", consentRequest, StringComparison.Ordinal);
        Assert.Contains("enum: [false]", consentRequest, StringComparison.Ordinal);
        Assert.Contains("otherPartyConsent:", coordinationSchema, StringComparison.Ordinal);
        Assert.Contains("enum: [accepted, not_shared]", coordinationSchema, StringComparison.Ordinal);
        Assert.Contains("otherPhone:", coordinationSchema, StringComparison.Ordinal);
        Assert.Contains("exactAddress:", coordinationSchema, StringComparison.Ordinal);
        Assert.Contains("la direcci\u00f3n exacta siempre es null", coordinationList, StringComparison.Ordinal);
    }

    [Fact]
    public void Completion_contract_describes_private_participant_endpoints_and_statuses()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Contracts", "openapi-v1.yaml");
        var contract = File.ReadAllText(path);

        Assert.Contains("/api/v1/changas/{id}/completion:", contract, StringComparison.Ordinal);
        Assert.Contains("/api/v1/changas/{id}/completion/mark-done:", contract, StringComparison.Ordinal);
        Assert.Contains("/api/v1/changas/{id}/completion/confirm:", contract, StringComparison.Ordinal);
        Assert.Contains("/api/v1/changas/{id}/completion/reject:", contract, StringComparison.Ordinal);
        Assert.Contains("ChangaCompletion:", contract, StringComparison.Ordinal);
        Assert.Contains("pending_response", contract, StringComparison.Ordinal);
        Assert.Contains("completion_review", contract, StringComparison.Ordinal);
        Assert.Contains("completion_forbidden", contract, StringComparison.Ordinal);
        Assert.Contains("completion_not_pending", contract, StringComparison.Ordinal);
        Assert.Contains("completion_invalid_state", contract, StringComparison.Ordinal);
        Assert.Contains("enum: [available, assigned, cancelled, pending_review, rejected, pending_response, confirmed, completion_review, completed]", contract, StringComparison.Ordinal);
        Assert.Contains("/api/v1/changas/{id}/payment-declaration:", contract, StringComparison.Ordinal);
        Assert.Contains("ChangaPaymentDeclaration:", contract, StringComparison.Ordinal);
        Assert.Contains("payment_declaration_forbidden", contract, StringComparison.Ordinal);
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
