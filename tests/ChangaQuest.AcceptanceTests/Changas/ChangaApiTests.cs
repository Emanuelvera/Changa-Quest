using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ChangaQuest.AcceptanceTests.Accounts;

namespace ChangaQuest.AcceptanceTests.Changas;

public sealed class ChangaApiTests(AccountApiFactory factory) : IClassFixture<AccountApiFactory>
{
    [Fact]
    public async Task Publication_and_board_require_a_verified_adult_session()
    {
        using var client = factory.CreateClient();
        using var publish = await client.PostAsJsonAsync("/api/v1/changas", ValidRequest());
        using var board = await client.GetAsync("/api/v1/changas");

        Assert.Equal(HttpStatusCode.Unauthorized, publish.StatusCode);
        Assert.Equal("application/problem+json", publish.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.Unauthorized, board.StatusCode);
        Assert.Equal("application/problem+json", board.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Session_whose_account_is_no_longer_eligible_gets_forbidden()
    {
        using var client = factory.CreateClient();
        var email = $"eligibility-{Guid.NewGuid():N}@example.test";
        const string password = "long-test-password";
        using var registration = await client.PostAsJsonAsync(
            "/api/v1/accounts/register",
            new { email, password, declaredAdult = true });
        Assert.Equal(HttpStatusCode.Accepted, registration.StatusCode);
        var logEntry = factory.LogEntries.Last(entry =>
            entry.StartsWith("Enlace local de", StringComparison.Ordinal));
        var verificationLink = new Uri(logEntry[logEntry.IndexOf("http", StringComparison.Ordinal)..]);
        using var confirmation = await client.GetAsync(verificationLink.PathAndQuery);
        using var login = await client.PostAsJsonAsync("/api/v1/accounts/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using (var connection = new Microsoft.Data.SqlClient.SqlConnection(
            $"Server=(localdb)\\MSSQLLocalDB;Database={factory.DatabaseName};Trusted_Connection=True;TrustServerCertificate=True"))
        {
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE AspNetUsers SET DeclaredAdult = 0 WHERE Email = @Email";
            command.Parameters.AddWithValue("@Email", email);
            Assert.Equal(1, await command.ExecuteNonQueryAsync());
        }

        using var board = await client.GetAsync("/api/v1/changas");
        Assert.Equal(HttpStatusCode.Forbidden, board.StatusCode);
        Assert.Equal("application/problem+json", board.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Verified_adult_can_publish_and_read_public_changa_without_exact_address()
    {
        using var client = await CreateVerifiedClientAsync();
        var exactAddress = $"Calle privada {Guid.NewGuid():N}, 123";
        var request = ValidRequest(exactAddress);

        using var created = await client.PostAsJsonAsync("/api/v1/changas", request);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var createdContent = await created.Content.ReadAsStringAsync();
        Assert.Equal("application/json", created.Content.Headers.ContentType?.MediaType);
        using var createdJson = JsonDocument.Parse(createdContent);
        var item = createdJson.RootElement;
        Assert.Equal("available", item.GetProperty("status").GetString());
        Assert.Equal("first_come", item.GetProperty("assignmentMode").GetString());
        Assert.Equal(request.description, item.GetProperty("description").GetString());
        Assert.Equal(request.reward, item.GetProperty("reward").GetInt32());
        Assert.Equal(request.zone, item.GetProperty("zone").GetString());
        Assert.True(item.GetProperty("materialsNeeded").GetBoolean());
        Assert.Equal("worker", item.GetProperty("materialsProvider").GetString());
        Assert.DoesNotContain(exactAddress, createdContent, StringComparison.Ordinal);
        Assert.False(item.TryGetProperty("exactAddress", out _));

        var id = item.GetProperty("id").GetGuid();
        using var listed = await client.GetAsync("/api/v1/changas");
        var listContent = await listed.Content.ReadAsStringAsync();
        using var listJson = JsonDocument.Parse(listContent);
        Assert.Contains(listJson.RootElement.EnumerateArray(), changa => changa.GetProperty("id").GetGuid() == id);
        Assert.DoesNotContain(exactAddress, listContent, StringComparison.Ordinal);

        using var detail = await client.GetAsync($"/api/v1/changas/{id}");
        var detailContent = await detail.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.DoesNotContain(exactAddress, detailContent, StringComparison.Ordinal);
        using var detailJson = JsonDocument.Parse(detailContent);
        Assert.False(detailJson.RootElement.TryGetProperty("exactAddress", out _));
    }

    [Fact]
    public async Task Invalid_publication_reports_field_errors_and_does_not_create_a_changa()
    {
        using var client = await CreateVerifiedClientAsync();
        using var before = await client.GetAsync("/api/v1/changas");
        using var beforeJson = await JsonDocument.ParseAsync(await before.Content.ReadAsStreamAsync());
        var existingCount = beforeJson.RootElement.GetArrayLength();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/changas",
            new { reward = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var errors = body.RootElement.GetProperty("errors");
        Assert.All(new[] { "description", "reward", "zone", "exactAddress", "materialsNeeded" },
            expected => Assert.True(errors.TryGetProperty(expected, out _), $"Missing validation error for {expected}."));

        using var list = await client.GetAsync("/api/v1/changas");
        using var listJson = await JsonDocument.ParseAsync(await list.Content.ReadAsStreamAsync());
        Assert.Equal(existingCount, listJson.RootElement.GetArrayLength());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10000001)]
    public async Task Reward_must_be_within_the_supported_ars_range(int reward)
    {
        using var client = await CreateVerifiedClientAsync();
        using var response = await client.PostAsJsonAsync("/api/v1/changas", ValidRequest() with { reward = reward });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("reward", out _));
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(true, "other")]
    [InlineData(false, "worker")]
    public async Task Materials_provider_must_match_the_explicit_materials_answer(
        bool materialsNeeded,
        string? materialsProvider)
    {
        using var client = await CreateVerifiedClientAsync();
        using var response = await client.PostAsJsonAsync(
            "/api/v1/changas",
            ValidRequest() with { materialsNeeded = materialsNeeded, materialsProvider = materialsProvider });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("materialsProvider", out _));
    }

    [Fact]
    public async Task Development_samples_are_labeled_and_idempotent()
    {
        using var client = await CreateVerifiedClientAsync();
        using var first = await client.GetAsync("/api/v1/changas");
        using var firstJson = await JsonDocument.ParseAsync(await first.Content.ReadAsStreamAsync());
        var samples = firstJson.RootElement.EnumerateArray()
            .Where(changa => changa.GetProperty("isSample").GetBoolean())
            .ToDictionary(changa => changa.GetProperty("id").GetGuid(), changa => changa.GetProperty("description").GetString());

        Assert.NotEmpty(samples);
        Assert.All(samples.Values, description => Assert.Contains("Ejemplo ficticio", description, StringComparison.Ordinal));
        Assert.All(firstJson.RootElement.EnumerateArray(), changa => Assert.False(changa.TryGetProperty("exactAddress", out _)));

        using var second = await client.GetAsync("/api/v1/changas");
        using var secondJson = await JsonDocument.ParseAsync(await second.Content.ReadAsStreamAsync());
        var secondSampleIds = secondJson.RootElement.EnumerateArray()
            .Where(changa => changa.GetProperty("isSample").GetBoolean())
            .Select(changa => changa.GetProperty("id").GetGuid())
            .ToHashSet();
        Assert.Equal(samples.Keys.ToHashSet(), secondSampleIds);
    }

    [Fact]
    public async Task Production_does_not_seed_sample_changas()
    {
        using var productionFactory = AccountApiFactory.ForEnvironment("Production");
        using var client = productionFactory.CreateClient();
        using var connection = new Microsoft.Data.SqlClient.SqlConnection(
            $"Server=(localdb)\\MSSQLLocalDB;Database={productionFactory.DatabaseName};Trusted_Connection=True;TrustServerCertificate=True");
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Changas WHERE IsSample = 1";

        Assert.Equal(0, Convert.ToInt32(await command.ExecuteScalarAsync()));
    }

    [Fact]
    public async Task Unknown_changa_returns_not_found()
    {
        using var client = await CreateVerifiedClientAsync();
        using var response = await client.GetAsync($"/api/v1/changas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Verified_worker_takes_once_and_retry_is_idempotent()
    {
        using var author = await CreateVerifiedClientAsync();
        using var worker = await CreateVerifiedClientAsync();
        using var created = await author.PostAsJsonAsync("/api/v1/changas", ValidRequest());
        using var createdJson = await JsonDocument.ParseAsync(await created.Content.ReadAsStreamAsync());
        var id = createdJson.RootElement.GetProperty("id").GetGuid();

        using var first = await worker.PostAsync($"/api/v1/changas/{id}/take", null);
        using var retry = await worker.PostAsync($"/api/v1/changas/{id}/take", null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        using var firstJson = await JsonDocument.ParseAsync(await first.Content.ReadAsStreamAsync());
        using var retryJson = await JsonDocument.ParseAsync(await retry.Content.ReadAsStreamAsync());
        Assert.Equal(
            firstJson.RootElement.GetProperty("assignment").GetProperty("id").GetGuid(),
            retryJson.RootElement.GetProperty("assignment").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Author_cannot_take_own_changa_and_other_worker_gets_stable_conflict()
    {
        using var author = await CreateVerifiedClientAsync();
        using var otherWorker = await CreateVerifiedClientAsync();
        using var thirdWorker = await CreateVerifiedClientAsync();
        using var created = await author.PostAsJsonAsync("/api/v1/changas", ValidRequest());
        using var createdJson = await JsonDocument.ParseAsync(await created.Content.ReadAsStreamAsync());
        var id = createdJson.RootElement.GetProperty("id").GetGuid();

        using var self = await author.PostAsync($"/api/v1/changas/{id}/take", null);
        Assert.Equal(HttpStatusCode.Conflict, self.StatusCode);
        using var selfBody = await JsonDocument.ParseAsync(await self.Content.ReadAsStreamAsync());
        Assert.Equal("changa_author_cannot_take", selfBody.RootElement.GetProperty("code").GetString());

        using var taken = await otherWorker.PostAsync($"/api/v1/changas/{id}/take", null);
        Assert.Equal(HttpStatusCode.OK, taken.StatusCode);
        using var conflict = await thirdWorker.PostAsync($"/api/v1/changas/{id}/take", null);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        using var conflictBody = await JsonDocument.ParseAsync(await conflict.Content.ReadAsStreamAsync());
        Assert.Equal("changa_already_assigned", conflictBody.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Board_filters_by_radius_and_returns_approximate_distance_without_exact_address()
    {
        using var client = await CreateVerifiedClientAsync();
        var nearbyAddress = $"Calle cercana {Guid.NewGuid():N}";
        var farAddress = $"Calle lejana {Guid.NewGuid():N}";
        using var nearby = await client.PostAsJsonAsync(
            "/api/v1/changas",
            ValidRequest(nearbyAddress) with { latitude = -31.4167, longitude = -64.1833 });
        using var far = await client.PostAsJsonAsync(
            "/api/v1/changas",
            ValidRequest(farAddress) with { latitude = -31.5000, longitude = -64.3000 });
        Assert.Equal(HttpStatusCode.Created, nearby.StatusCode);
        Assert.Equal(HttpStatusCode.Created, far.StatusCode);

        using var response = await client.GetAsync(
            "/api/v1/changas?latitude=-31.4167&longitude=-64.1833&radiusKm=1");
        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(json.RootElement.EnumerateArray(), item =>
            item.GetProperty("description").GetString() == "Una changa descriptiva de prueba.");
        Assert.DoesNotContain(farAddress, content, StringComparison.Ordinal);
        Assert.DoesNotContain(nearbyAddress, content, StringComparison.Ordinal);
        Assert.All(json.RootElement.EnumerateArray(), item =>
        {
            Assert.True(item.GetProperty("distanceKm").GetDouble() <= 1);
            Assert.False(item.TryGetProperty("exactAddress", out _));
        });
    }

    [Fact]
    public async Task Board_rejects_unsupported_radius()
    {
        using var client = await CreateVerifiedClientAsync();

        using var response = await client.GetAsync(
            "/api/v1/changas?latitude=-31.4167&longitude=-64.1833&radiusKm=2");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("radiusKm", out _));
    }

    private async Task<HttpClient> CreateVerifiedClientAsync(AccountApiFactory? apiFactory = null)
    {
        var client = (apiFactory ?? factory).CreateClient();
        var email = $"changa-{Guid.NewGuid():N}@example.test";
        const string password = "contraseña-larga-de-prueba";
        using var registration = await client.PostAsJsonAsync(
            "/api/v1/accounts/register",
            new { email, password, declaredAdult = true });
        Assert.Equal(HttpStatusCode.Accepted, registration.StatusCode);

        var logEntry = (apiFactory ?? factory).LogEntries.Last(entry =>
            entry.StartsWith("Enlace local de verificación:", StringComparison.Ordinal));
        var verificationLink = new Uri(logEntry[logEntry.IndexOf("http", StringComparison.Ordinal)..]);
        using var confirmation = await client.GetAsync(verificationLink.PathAndQuery);
        Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);
        using var login = await client.PostAsJsonAsync("/api/v1/accounts/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return client;
    }

    private static ChangaRequest ValidRequest(string exactAddress = "Calle privada 123, Córdoba") => new(
        "Una changa descriptiva de prueba.",
        15000,
        "Nueva Córdoba",
        exactAddress,
        true,
        "worker",
        null,
        null);

    private sealed record ChangaRequest(
        string description,
        int reward,
        string zone,
        string exactAddress,
        bool materialsNeeded,
        string? materialsProvider,
        double? latitude,
        double? longitude);
}
