using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ChangaQuest.AcceptanceTests.Accounts;

public class AccountRegistrationFlowTests(AccountApiFactory factory) : IClassFixture<AccountApiFactory>
{
    [Fact]
    public async Task Registration_requires_adult_declaration_valid_email_and_twelve_characters()
    {
        using var client = factory.CreateClient();

        using var invalidEmail = await client.PostAsJsonAsync(
            "/api/v1/accounts/register",
            new { email = "not-an-email", password = "corta", declaredAdult = true });
        Assert.Equal(HttpStatusCode.BadRequest, invalidEmail.StatusCode);
        using var invalidBody = await JsonDocument.ParseAsync(await invalidEmail.Content.ReadAsStreamAsync());
        Assert.Contains("email", invalidBody.RootElement.GetProperty("errors").EnumerateObject().Select(item => item.Name));
        Assert.Contains("password", invalidBody.RootElement.GetProperty("errors").EnumerateObject().Select(item => item.Name));

        using var noAdultDeclaration = await client.PostAsJsonAsync(
            "/api/v1/accounts/register",
            new { email = $"minor-{Guid.NewGuid():N}@example.test", password = "contraseña-de-prueba", declaredAdult = false });
        Assert.Equal(HttpStatusCode.BadRequest, noAdultDeclaration.StatusCode);
        using var adultBody = await JsonDocument.ParseAsync(await noAdultDeclaration.Content.ReadAsStreamAsync());
        Assert.Contains(
            "declaredAdult",
            adultBody.RootElement.GetProperty("errors").EnumerateObject().Select(item => item.Name));
    }

    [Fact]
    public async Task Registration_rejects_email_longer_than_the_identity_column()
    {
        using var client = factory.CreateClient();
        var email = $"{new string('a', 245)}@example.test";

        using var response = await client.PostAsJsonAsync(
            "/api/v1/accounts/register",
            new { email, password = "contraseña-larga", declaredAdult = true });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Contains("email", body.RootElement.GetProperty("errors").EnumerateObject().Select(item => item.Name));
    }

    [Fact]
    public async Task Production_registration_does_not_log_the_local_verification_link()
    {
        using var productionFactory = AccountApiFactory.ForEnvironment("Production");
        using var client = productionFactory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/accounts/register",
            new
            {
                email = $"production-{Guid.NewGuid():N}@example.test",
                password = "contraseña-larga",
                declaredAdult = true
            });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.DoesNotContain(
            productionFactory.LogEntries,
            entry => entry.Contains("Enlace local de verificación:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Login_rejects_malformed_email_as_a_validation_problem()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/accounts/login",
            new { email = "not-an-email", password = "contraseña-larga" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Contains("email", body.RootElement.GetProperty("errors").EnumerateObject().Select(item => item.Name));
    }

    [Fact]
    public async Task Duplicate_registration_is_a_field_error_not_a_success()
    {
        using var client = factory.CreateClient();
        var email = $"duplicate-{Guid.NewGuid():N}@example.test";
        var payload = new { email, password = "contraseña-larga-de-prueba", declaredAdult = true };

        using var first = await client.PostAsJsonAsync("/api/v1/accounts/register", payload);
        using var duplicate = await client.PostAsJsonAsync("/api/v1/accounts/register", payload);

        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        using var errorBody = await JsonDocument.ParseAsync(await duplicate.Content.ReadAsStreamAsync());
        Assert.Contains("email", errorBody.RootElement.GetProperty("errors").EnumerateObject().Select(item => item.Name));
        Assert.False(first.Headers.Contains("Set-Cookie"));
        Assert.False(duplicate.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Unverified_account_cannot_sign_in_and_receives_verification_guidance()
    {
        using var client = factory.CreateClient();
        var email = $"pending-{Guid.NewGuid():N}@example.test";
        const string password = "contraseña-larga-de-prueba";
        using var registration = await client.PostAsJsonAsync(
            "/api/v1/accounts/register",
            new { email, password, declaredAdult = true });

        using var login = await client.PostAsJsonAsync(
            "/api/v1/accounts/login",
            new { email, password });

        Assert.Equal(HttpStatusCode.Accepted, registration.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
        Assert.False(login.Headers.Contains("Set-Cookie"));
        using var body = await JsonDocument.ParseAsync(await login.Content.ReadAsStreamAsync());
        Assert.Equal("email_not_verified", body.RootElement.GetProperty("code").GetString());
        Assert.Contains("confirmá", body.RootElement.GetProperty("detail").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Verification_link_is_logged_locally_single_use_and_enables_http_only_session()
    {
        using var client = factory.CreateClient();
        var email = $"verified-{Guid.NewGuid():N}@example.test";
        const string password = "abcdefghijkl";
        using var registration = await client.PostAsJsonAsync(
            "/api/v1/accounts/register",
            new { email, password, declaredAdult = true });

        Assert.Equal(HttpStatusCode.Accepted, registration.StatusCode);
        Assert.False(registration.Headers.Contains("Set-Cookie"));
        var logEntry = factory.LogEntries.Last(entry =>
            entry.StartsWith("Enlace local de verificación:", StringComparison.Ordinal));
        var verificationLink = new Uri(logEntry[logEntry.IndexOf("http", StringComparison.Ordinal)..]);

        using var confirmation = await client.GetAsync(verificationLink.PathAndQuery);
        using var replay = await client.GetAsync(verificationLink.PathAndQuery);
        Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
        Assert.Contains("Correo verificado", await confirmation.Content.ReadAsStringAsync());

        using var login = await client.PostAsJsonAsync(
            "/api/v1/accounts/login",
            new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains(
            login.Headers.GetValues("Set-Cookie"),
            cookie => cookie.Contains("HttpOnly", StringComparison.OrdinalIgnoreCase));

        using var account = await client.GetAsync("/api/v1/accounts/me");
        Assert.Equal(HttpStatusCode.OK, account.StatusCode);
        var profile = await account.Content.ReadFromJsonAsync<AccountResponse>();
        Assert.Equal(email, profile?.Email);
    }

    [Fact]
    public async Task Incorrect_verification_token_changes_nothing()
    {
        using var client = factory.CreateClient();
        var email = $"invalid-token-{Guid.NewGuid():N}@example.test";
        const string password = "contraseña-larga-de-prueba";
        using var registration = await client.PostAsJsonAsync(
            "/api/v1/accounts/register",
            new { email, password, declaredAdult = true });
        var logEntry = factory.LogEntries.Last(entry =>
            entry.StartsWith("Enlace local de verificación:", StringComparison.Ordinal));
        var link = new Uri(logEntry[logEntry.IndexOf("http", StringComparison.Ordinal)..]);
        var invalidLink = new UriBuilder(link) { Query = $"userId={Uri.EscapeDataString(GetQueryValue(link, "userId"))}&token=wrong-token" }.Uri;

        using var confirmation = await client.GetAsync(invalidLink.PathAndQuery);
        using var login = await client.PostAsJsonAsync(
            "/api/v1/accounts/login",
            new { email, password });

        Assert.Equal(HttpStatusCode.BadRequest, confirmation.StatusCode);
        Assert.Equal("application/problem+json", confirmation.Content.Headers.ContentType?.MediaType);
        using var problem = await JsonDocument.ParseAsync(await confirmation.Content.ReadAsStreamAsync());
        Assert.Equal("email_confirmation_invalid", problem.RootElement.GetProperty("code").GetString());
        Assert.Contains(
            "no es válido",
            problem.RootElement.GetProperty("detail").GetString(),
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
        Assert.False(login.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Missing_verification_parameters_return_problem_details()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/accounts/confirm-email");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Protected_account_routes_return_problem_details_without_a_session()
    {
        using var client = factory.CreateClient();

        using var account = await client.GetAsync("/api/v1/accounts/me");
        using var signOut = await client.PostAsync("/api/v1/accounts/logout", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, account.StatusCode);
        Assert.Equal("application/problem+json", account.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.Unauthorized, signOut.StatusCode);
        Assert.Equal("application/problem+json", signOut.Content.Headers.ContentType?.MediaType);
    }

    private static string GetQueryValue(Uri uri, string key)
    {
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return query[key] ?? throw new InvalidOperationException($"Missing query key {key}.");
    }

    private sealed record AccountResponse(string Email);
}
