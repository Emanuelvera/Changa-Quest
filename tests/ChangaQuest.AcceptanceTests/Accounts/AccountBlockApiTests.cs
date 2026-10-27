using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ChangaQuest.AcceptanceTests.Accounts;

namespace ChangaQuest.AcceptanceTests.Accounts;

public sealed class AccountBlockApiTests(AccountApiFactory factory) : IClassFixture<AccountApiFactory>
{
    [Fact]
    public async Task Blocking_requires_authentication_and_rejects_self_or_unknown_target()
    {
        using var anonymous = factory.CreateClient();
        using var unauthenticated = await anonymous.PostAsJsonAsync(
            $"/api/v1/accounts/{Guid.NewGuid()}/block", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);

        var (client, ownId) = await CreateVerifiedClientWithIdAsync();
        using (client)
        using (var self = await client.PostAsync($"/api/v1/accounts/{ownId}/block", null))
        using (var selfJson = await JsonDocument.ParseAsync(await self.Content.ReadAsStreamAsync()))
        {
            Assert.Equal(HttpStatusCode.Conflict, self.StatusCode);
            Assert.Equal("cannot_block_self", selfJson.RootElement.GetProperty("code").GetString());
        }

        using var unknownClient = await CreateVerifiedClientAsync();
        using var unknown = await unknownClient.PostAsync(
            $"/api/v1/accounts/{Guid.NewGuid()}/block", null);
        using var unknownJson = await JsonDocument.ParseAsync(await unknown.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal("account_not_found", unknownJson.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Blocking_is_confirmed_by_the_client_request_and_is_idempotent()
    {
        var (blocker, _) = await CreateVerifiedClientWithIdAsync();
        var (target, targetId) = await CreateVerifiedClientWithIdAsync();
        using (blocker)
        using (target)
        {

            using var first = await blocker.PostAsync($"/api/v1/accounts/{targetId}/block", null);
            using var second = await blocker.PostAsync($"/api/v1/accounts/{targetId}/block", null);
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            Assert.Contains("no cancela ni resuelve", await first.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }
    }

    private async Task<HttpClient> CreateVerifiedClientAsync() =>
        (await CreateVerifiedClientWithIdAsync()).Client;

    private async Task<(HttpClient Client, string UserId)> CreateVerifiedClientWithIdAsync()
    {
        var client = factory.CreateClient();
        var email = $"block-{Guid.NewGuid():N}@example.test";
        const string password = "contraseña-larga-de-prueba";
        using var registration = await client.PostAsJsonAsync(
            "/api/v1/accounts/register", new { email, password, declaredAdult = true });
        Assert.Equal(HttpStatusCode.Accepted, registration.StatusCode);
        var logEntry = factory.LogEntries.Last(entry =>
            entry.StartsWith("Enlace local de verificación:", StringComparison.Ordinal));
        var verificationLink = new Uri(logEntry[logEntry.IndexOf("http", StringComparison.Ordinal)..]);
        var userId = verificationLink.Query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(parts => parts.Length == 2 && parts[0] == "userId")
            .Select(parts => Uri.UnescapeDataString(parts[1]))
            .SingleOrDefault();
        Assert.False(string.IsNullOrWhiteSpace(userId));
        using var confirmation = await client.GetAsync(verificationLink.PathAndQuery);
        Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);
        using var login = await client.PostAsJsonAsync(
            "/api/v1/accounts/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return (client, userId!);
    }
}
