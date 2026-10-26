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
        using var mine = await client.GetAsync("/api/v1/changas/mine");
        using var cancel = await client.PostAsync($"/api/v1/changas/{Guid.NewGuid()}/cancel", null);

        Assert.Equal(HttpStatusCode.Unauthorized, publish.StatusCode);
        Assert.Equal("application/problem+json", publish.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.Unauthorized, board.StatusCode);
        Assert.Equal("application/problem+json", board.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.Unauthorized, mine.StatusCode);
        Assert.Equal("application/problem+json", mine.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.Unauthorized, cancel.StatusCode);
        Assert.Equal("application/problem+json", cancel.Content.Headers.ContentType?.MediaType);
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
        using var categories = await client.GetAsync("/api/v1/changas/categories");
        using var mine = await client.GetAsync("/api/v1/changas/mine");
        using var cancel = await client.PostAsync($"/api/v1/changas/{Guid.NewGuid()}/cancel", null);
        Assert.Equal(HttpStatusCode.Forbidden, board.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, categories.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, mine.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, cancel.StatusCode);
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
        Assert.Equal("pending_review", item.GetProperty("status").GetString());
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
        Assert.DoesNotContain(listJson.RootElement.EnumerateArray(), changa => changa.GetProperty("id").GetGuid() == id);
        Assert.DoesNotContain(exactAddress, listContent, StringComparison.Ordinal);

        using var detail = await client.GetAsync($"/api/v1/changas/{id}");
        var detailContent = await detail.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
        Assert.DoesNotContain(exactAddress, detailContent, StringComparison.Ordinal);
        using var detailJson = JsonDocument.Parse(detailContent);
        Assert.False(detailJson.RootElement.TryGetProperty("exactAddress", out _));
    }

    [Fact]
    public async Task Categories_endpoint_lists_only_the_approved_catalog()
    {
        using var client = await CreateVerifiedClientAsync();

        using var response = await client.GetAsync("/api/v1/changas/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var ids = body.RootElement.EnumerateArray()
            .Select(category => category.GetProperty("id").GetString())
            .ToArray();
        Assert.Equal(
            new[]
            {
                "cocina-preparacion-basica",
                "compras-mandados",
                "cuidado-mascotas",
                "jardineria-basica",
                "limpieza-domestica",
                "organizacion-hogar"
            },
            ids);
    }

    [Fact]
    public async Task Unapproved_category_is_rejected_and_not_listed()
    {
        using var client = await CreateVerifiedClientAsync();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/changas",
            ValidRequest() with { categoryId = "trabajo-electrico" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("category_not_approved", body.RootElement.GetProperty("code").GetString());
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
        Assert.All(new[] { "description", "reward", "zone", "exactAddress", "materialsNeeded", "categoryId" },
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
        await ApproveAsync(id);

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
    public async Task Author_lists_and_cancels_only_own_available_publication_without_private_data()
    {
        using var author = await CreateVerifiedClientAsync();
        using var other = await CreateVerifiedClientAsync();
        var address = $"Calle privada {Guid.NewGuid():N}";
        using var created = await author.PostAsJsonAsync("/api/v1/changas", ValidRequest(address));
        using var createdJson = await JsonDocument.ParseAsync(await created.Content.ReadAsStreamAsync());
        var id = createdJson.RootElement.GetProperty("id").GetGuid();
        await ApproveAsync(id);

        using var mine = await author.GetAsync("/api/v1/changas/mine");
        var mineContent = await mine.Content.ReadAsStringAsync();
        using var mineJson = JsonDocument.Parse(mineContent);
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        Assert.Contains(mineJson.RootElement.EnumerateArray(), item => item.GetProperty("id").GetGuid() == id);
        Assert.DoesNotContain(address, mineContent, StringComparison.Ordinal);
        Assert.DoesNotContain("exactAddress", mineContent, StringComparison.OrdinalIgnoreCase);
        using var otherMine = await other.GetAsync("/api/v1/changas/mine");
        using var otherMineJson = await JsonDocument.ParseAsync(await otherMine.Content.ReadAsStreamAsync());
        Assert.DoesNotContain(otherMineJson.RootElement.EnumerateArray(), item => item.GetProperty("id").GetGuid() == id);

        using var forbidden = await other.PostAsync($"/api/v1/changas/{id}/cancel", null);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        using var forbiddenJson = await JsonDocument.ParseAsync(await forbidden.Content.ReadAsStreamAsync());
        Assert.Equal("changa_cancel_forbidden", forbiddenJson.RootElement.GetProperty("code").GetString());

        using var cancelled = await author.PostAsync($"/api/v1/changas/{id}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        var cancelledContent = await cancelled.Content.ReadAsStringAsync();
        using var cancelledJson = JsonDocument.Parse(cancelledContent);
        Assert.Equal("cancelled", cancelledJson.RootElement.GetProperty("status").GetString());
        Assert.DoesNotContain(address, cancelledContent, StringComparison.Ordinal);

        using var board = await author.GetAsync("/api/v1/changas");
        using var boardJson = await JsonDocument.ParseAsync(await board.Content.ReadAsStreamAsync());
        Assert.DoesNotContain(boardJson.RootElement.EnumerateArray(), item => item.GetProperty("id").GetGuid() == id);
        using var mineAfter = await author.GetAsync("/api/v1/changas/mine");
        using var mineAfterJson = await JsonDocument.ParseAsync(await mineAfter.Content.ReadAsStreamAsync());
        Assert.Contains(mineAfterJson.RootElement.EnumerateArray(), item =>
            item.GetProperty("id").GetGuid() == id && item.GetProperty("status").GetString() == "cancelled");
    }

    [Fact]
    public async Task Author_cannot_cancel_an_assigned_changa_and_unknown_id_is_not_found()
    {
        using var author = await CreateVerifiedClientAsync();
        using var worker = await CreateVerifiedClientAsync();
        using var created = await author.PostAsJsonAsync("/api/v1/changas", ValidRequest());
        using var createdJson = await JsonDocument.ParseAsync(await created.Content.ReadAsStreamAsync());
        var id = createdJson.RootElement.GetProperty("id").GetGuid();
        await ApproveAsync(id);
        using var taken = await worker.PostAsync($"/api/v1/changas/{id}/take", null);
        Assert.Equal(HttpStatusCode.OK, taken.StatusCode);

        using var conflict = await author.PostAsync($"/api/v1/changas/{id}/cancel", null);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        using var conflictJson = await JsonDocument.ParseAsync(await conflict.Content.ReadAsStreamAsync());
        Assert.Equal("changa_already_assigned", conflictJson.RootElement.GetProperty("code").GetString());

        using var missing = await author.PostAsync($"/api/v1/changas/{Guid.NewGuid()}/cancel", null);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        using var missingJson = await JsonDocument.ParseAsync(await missing.Content.ReadAsStreamAsync());
        Assert.Equal("changa_not_found", missingJson.RootElement.GetProperty("code").GetString());

        using var pending = await author.PostAsJsonAsync("/api/v1/changas", ValidRequest());
        using var pendingJson = await JsonDocument.ParseAsync(await pending.Content.ReadAsStreamAsync());
        var pendingId = pendingJson.RootElement.GetProperty("id").GetGuid();
        using var notAvailable = await author.PostAsync($"/api/v1/changas/{pendingId}/cancel", null);
        Assert.Equal(HttpStatusCode.Conflict, notAvailable.StatusCode);
        using var notAvailableJson = await JsonDocument.ParseAsync(await notAvailable.Content.ReadAsStreamAsync());
        Assert.Equal("changa_not_available", notAvailableJson.RootElement.GetProperty("code").GetString());
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
        await ApproveAsync(id);

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
        using var nearbyJson = await JsonDocument.ParseAsync(await nearby.Content.ReadAsStreamAsync());
        using var farJson = await JsonDocument.ParseAsync(await far.Content.ReadAsStreamAsync());
        await ApproveAsync(nearbyJson.RootElement.GetProperty("id").GetGuid());
        await ApproveAsync(farJson.RootElement.GetProperty("id").GetGuid());

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

    [Fact]
    public async Task Coordination_endpoints_without_session_return_401_and_no_private_data()
    {
        using var author = await CreateVerifiedClientAsync();
        using var worker = await CreateVerifiedClientAsync();
        var address = $"Dirección ficticia {Guid.NewGuid():N}";
        using var created = await author.PostAsJsonAsync("/api/v1/changas", ValidRequest(address));
        using var createdJson = await JsonDocument.ParseAsync(await created.Content.ReadAsStreamAsync());
        var id = createdJson.RootElement.GetProperty("id").GetGuid();
        await ApproveAsync(id);
        using var taken = await worker.PostAsync($"/api/v1/changas/{id}/take", null);
        Assert.Equal(HttpStatusCode.OK, taken.StatusCode);
        using var authorShared = await author.PutAsJsonAsync(
            $"/api/v1/changas/{id}/coordination/phone-consent", new { sharePhone = true, phone = "+54 351 555-0111" });
        using var workerShared = await worker.PutAsJsonAsync(
            $"/api/v1/changas/{id}/coordination/phone-consent", new { sharePhone = true, phone = "351 555-0122" });
        Assert.Equal(HttpStatusCode.OK, authorShared.StatusCode);
        Assert.Equal(HttpStatusCode.OK, workerShared.StatusCode);

        using var anonymous = factory.CreateClient();
        using var read = await anonymous.GetAsync($"/api/v1/changas/{id}/coordination");
        using var write = await anonymous.PutAsJsonAsync(
            $"/api/v1/changas/{id}/coordination/phone-consent", new { sharePhone = true, phone = "+54 351 555-0133" });
        using var list = await anonymous.GetAsync("/api/v1/coordinations");

        foreach (var response in new[] { read, write, list })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain(address, body, StringComparison.Ordinal);
            Assert.DoesNotContain("555-01", body, StringComparison.Ordinal);
            Assert.DoesNotContain("otherPhone", body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Coordination_matrix_enforces_mutual_phone_consent_and_assigned_roles()
    {
        using var author = await CreateVerifiedClientAsync();
        using var worker = await CreateVerifiedClientAsync();
        using var outsider = await CreateVerifiedClientAsync();
        var requesterPhone = "+54 (351) 234-5678";
        var workerPhone = "351-123-4567";
        var address = $"Dirección ficticia {Guid.NewGuid():N}";
        using var created = await author.PostAsJsonAsync("/api/v1/changas", ValidRequest(address));
        using var createdJson = await JsonDocument.ParseAsync(await created.Content.ReadAsStreamAsync());
        var id = createdJson.RootElement.GetProperty("id").GetGuid();
        await ApproveAsync(id);
        using var taken = await worker.PostAsync($"/api/v1/changas/{id}/take", null);
        Assert.Equal(HttpStatusCode.OK, taken.StatusCode);

        using var workerPending = await worker.GetAsync($"/api/v1/changas/{id}/coordination");
        using var workerPendingJson = await JsonDocument.ParseAsync(await workerPending.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.OK, workerPending.StatusCode);
        Assert.Equal(address, workerPendingJson.RootElement.GetProperty("exactAddress").GetString());
        Assert.Null(workerPendingJson.RootElement.GetProperty("otherPhone").GetString());
        Assert.False(workerPendingJson.RootElement.GetProperty("hasMutualConsent").GetBoolean());

        using var authorPending = await author.GetAsync($"/api/v1/changas/{id}/coordination");
        using var authorPendingJson = await JsonDocument.ParseAsync(await authorPending.Content.ReadAsStreamAsync());
        Assert.Null(authorPendingJson.RootElement.GetProperty("exactAddress").GetString());
        Assert.Null(authorPendingJson.RootElement.GetProperty("otherPhone").GetString());

        using var requesterList = await author.GetAsync("/api/v1/coordinations");
        var requesterListContent = await requesterList.Content.ReadAsStringAsync();
        using var requesterListJson = JsonDocument.Parse(requesterListContent);
        var requesterListItem = Assert.Single(requesterListJson.RootElement.EnumerateArray());
        Assert.Null(requesterListItem.GetProperty("exactAddress").GetString());
        Assert.DoesNotContain(address, requesterListContent, StringComparison.Ordinal);
        using var workerList = await worker.GetAsync("/api/v1/coordinations");
        var workerListContent = await workerList.Content.ReadAsStringAsync();
        using var workerListJson = JsonDocument.Parse(workerListContent);
        var workerListItem = Assert.Single(workerListJson.RootElement.EnumerateArray());
        Assert.Null(workerListItem.GetProperty("exactAddress").GetString());
        Assert.DoesNotContain(address, workerListContent, StringComparison.Ordinal);

        using var forbiddenRead = await outsider.GetAsync($"/api/v1/changas/{id}/coordination");
        using var forbiddenBody = await JsonDocument.ParseAsync(await forbiddenRead.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenRead.StatusCode);
        Assert.Equal("coordination_forbidden", forbiddenBody.RootElement.GetProperty("code").GetString());
        using var forbiddenWrite = await outsider.PutAsJsonAsync(
            $"/api/v1/changas/{id}/coordination/phone-consent",
            new { sharePhone = true, phone = "3511234567" });
        var forbiddenWriteContent = await forbiddenWrite.Content.ReadAsStringAsync();
        using var forbiddenWriteBody = JsonDocument.Parse(forbiddenWriteContent);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenWrite.StatusCode);
        Assert.Equal("coordination_forbidden", forbiddenWriteBody.RootElement.GetProperty("code").GetString());
        Assert.DoesNotContain(requesterPhone, forbiddenWriteContent, StringComparison.Ordinal);
        using var unchangedAfterOutsider = await author.GetAsync($"/api/v1/changas/{id}/coordination");
        using var unchangedAfterOutsiderJson = await JsonDocument.ParseAsync(await unchangedAfterOutsider.Content.ReadAsStreamAsync());
        Assert.False(unchangedAfterOutsiderJson.RootElement.GetProperty("hasMutualConsent").GetBoolean());

        using var invalid = await author.PutAsJsonAsync(
            $"/api/v1/changas/{id}/coordination/phone-consent",
            new { sharePhone = true, phone = "123-45" });
        using var invalidBody = await JsonDocument.ParseAsync(await invalid.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.True(invalidBody.RootElement.GetProperty("errors").TryGetProperty("phone", out _));
        using var missingPhone = await author.PutAsJsonAsync(
            $"/api/v1/changas/{id}/coordination/phone-consent",
            new { sharePhone = true });
        using var missingPhoneBody = await JsonDocument.ParseAsync(await missingPhone.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.BadRequest, missingPhone.StatusCode);
        Assert.Equal(
            "Ingres\u00e1 un tel\u00e9fono ficticio v\u00e1lido con entre 8 y 15 d\u00edgitos.",
            missingPhoneBody.RootElement.GetProperty("errors").GetProperty("phone")[0].GetString());
        using var unchangedAfterInvalid = await author.GetAsync($"/api/v1/changas/{id}/coordination");
        using var unchangedAfterInvalidJson = await JsonDocument.ParseAsync(await unchangedAfterInvalid.Content.ReadAsStreamAsync());
        Assert.Equal("pending", unchangedAfterInvalidJson.RootElement.GetProperty("yourConsent").GetString());

        using var requesterConsent = await author.PutAsJsonAsync(
            $"/api/v1/changas/{id}/coordination/phone-consent",
            new { sharePhone = true, phone = requesterPhone });
        using var pendingAfterOne = await author.GetAsync($"/api/v1/changas/{id}/coordination");
        using var pendingAfterOneJson = await JsonDocument.ParseAsync(await pendingAfterOne.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.OK, requesterConsent.StatusCode);
        Assert.Null(pendingAfterOneJson.RootElement.GetProperty("otherPhone").GetString());
        Assert.Equal("not_shared", pendingAfterOneJson.RootElement.GetProperty("otherPartyConsent").GetString());

        using var workerConsent = await worker.PutAsJsonAsync(
            $"/api/v1/changas/{id}/coordination/phone-consent",
            new { sharePhone = true, phone = workerPhone });
        using var requesterMutual = await author.GetAsync($"/api/v1/changas/{id}/coordination");
        using var requesterMutualJson = await JsonDocument.ParseAsync(await requesterMutual.Content.ReadAsStreamAsync());
        using var workerMutual = await worker.GetAsync($"/api/v1/changas/{id}/coordination");
        using var workerMutualJson = await JsonDocument.ParseAsync(await workerMutual.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.OK, workerConsent.StatusCode);
        Assert.True(requesterMutualJson.RootElement.GetProperty("hasMutualConsent").GetBoolean());
        Assert.Equal(workerPhone, requesterMutualJson.RootElement.GetProperty("otherPhone").GetString());
        Assert.Equal(requesterPhone, workerMutualJson.RootElement.GetProperty("otherPhone").GetString());
        Assert.Equal(address, workerMutualJson.RootElement.GetProperty("exactAddress").GetString());
        Assert.Null(requesterMutualJson.RootElement.GetProperty("exactAddress").GetString());

        using var revoke = await worker.PutAsJsonAsync(
            $"/api/v1/changas/{id}/coordination/phone-consent",
            new { sharePhone = false });
        using var hiddenAfterRevoke = await author.GetAsync($"/api/v1/changas/{id}/coordination");
        using var hiddenAfterRevokeJson = await JsonDocument.ParseAsync(await hiddenAfterRevoke.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);
        Assert.False(hiddenAfterRevokeJson.RootElement.GetProperty("hasMutualConsent").GetBoolean());
        Assert.Null(hiddenAfterRevokeJson.RootElement.GetProperty("otherPhone").GetString());

        using var authorDecline = await author.PutAsJsonAsync(
            $"/api/v1/changas/{id}/coordination/phone-consent",
            new { sharePhone = false });
        Assert.Equal(HttpStatusCode.OK, authorDecline.StatusCode);
        using (var connection = new Microsoft.Data.SqlClient.SqlConnection(
            $"Server=(localdb)\\MSSQLLocalDB;Database={factory.DatabaseName};Trusted_Connection=True;TrustServerCertificate=True"))
        {
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Status FROM Changas WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id);
            Assert.Equal("Assigned", await command.ExecuteScalarAsync());
        }

        using var assignments = await worker.GetAsync("/api/v1/coordinations");
        using var assignmentsJson = await JsonDocument.ParseAsync(await assignments.Content.ReadAsStreamAsync());
        Assert.Single(assignmentsJson.RootElement.EnumerateArray());

        using var notAssigned = await author.PostAsJsonAsync("/api/v1/changas", ValidRequest());
        using var notAssignedJson = await JsonDocument.ParseAsync(await notAssigned.Content.ReadAsStreamAsync());
        var availableId = notAssignedJson.RootElement.GetProperty("id").GetGuid();
        await ApproveAsync(availableId);
        using var noAssignment = await author.GetAsync($"/api/v1/changas/{availableId}/coordination");
        using var noAssignmentBody = await JsonDocument.ParseAsync(await noAssignment.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.Conflict, noAssignment.StatusCode);
        Assert.Equal("changa_not_assigned", noAssignmentBody.RootElement.GetProperty("code").GetString());

        using var missing = await author.GetAsync($"/api/v1/changas/{Guid.NewGuid()}/coordination");
        using var missingBody = await JsonDocument.ParseAsync(await missing.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("changa_not_found", missingBody.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Completion_flow_enforces_roles_and_confirms_without_closing_or_releasing_assignment()
    {
        using var author = await CreateVerifiedClientAsync();
        using var worker = await CreateVerifiedClientAsync();
        using var outsider = await CreateVerifiedClientAsync();
        using var created = await author.PostAsJsonAsync("/api/v1/changas", ValidRequest());
        using var createdJson = await JsonDocument.ParseAsync(await created.Content.ReadAsStreamAsync());
        var id = createdJson.RootElement.GetProperty("id").GetGuid();
        await ApproveAsync(id);
        using var unassigned = await author.GetAsync($"/api/v1/changas/{id}/completion");
        using var unassignedJson = await JsonDocument.ParseAsync(await unassigned.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.Conflict, unassigned.StatusCode);
        Assert.Equal("changa_not_assigned", unassignedJson.RootElement.GetProperty("code").GetString());
        using var unknown = await author.GetAsync($"/api/v1/changas/{Guid.NewGuid()}/completion");
        using var unknownJson = await JsonDocument.ParseAsync(await unknown.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal("changa_not_found", unknownJson.RootElement.GetProperty("code").GetString());
        using var taken = await worker.PostAsync($"/api/v1/changas/{id}/take", null);
        Assert.Equal(HttpStatusCode.OK, taken.StatusCode);

        using var workerRead = await worker.GetAsync($"/api/v1/changas/{id}/completion");
        using var workerReadJson = await JsonDocument.ParseAsync(await workerRead.Content.ReadAsStreamAsync());
        Assert.Equal("assigned", workerReadJson.RootElement.GetProperty("status").GetString());
        using var wrongMark = await author.PostAsync($"/api/v1/changas/{id}/completion/mark-done", null);
        Assert.Equal(HttpStatusCode.Forbidden, wrongMark.StatusCode);
        using var wrongMarkJson = await JsonDocument.ParseAsync(await wrongMark.Content.ReadAsStreamAsync());
        Assert.Equal("completion_forbidden", wrongMarkJson.RootElement.GetProperty("code").GetString());
        using var outsiderRead = await outsider.GetAsync($"/api/v1/changas/{id}/completion");
        using var outsiderJson = await JsonDocument.ParseAsync(await outsiderRead.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.Forbidden, outsiderRead.StatusCode);
        Assert.Equal("completion_forbidden", outsiderJson.RootElement.GetProperty("code").GetString());
        using var outsiderAction = await outsider.PostAsync($"/api/v1/changas/{id}/completion/reject", null);
        using var outsiderActionJson = await JsonDocument.ParseAsync(await outsiderAction.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.Forbidden, outsiderAction.StatusCode);
        Assert.Equal("completion_forbidden", outsiderActionJson.RootElement.GetProperty("code").GetString());

        using var marked = await worker.PostAsync($"/api/v1/changas/{id}/completion/mark-done", null);
        Assert.Equal(HttpStatusCode.OK, marked.StatusCode);
        using var markedJson = await JsonDocument.ParseAsync(await marked.Content.ReadAsStreamAsync());
        Assert.Equal("pending_response", markedJson.RootElement.GetProperty("status").GetString());
        using var repeatMark = await worker.PostAsync($"/api/v1/changas/{id}/completion/mark-done", null);
        using var repeatMarkJson = await JsonDocument.ParseAsync(await repeatMark.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.Conflict, repeatMark.StatusCode);
        Assert.Equal("completion_invalid_state", repeatMarkJson.RootElement.GetProperty("code").GetString());
        var markedAt = markedJson.RootElement.GetProperty("markedDoneAt").GetDateTimeOffset();
        var deadline = markedJson.RootElement.GetProperty("responseDeadline").GetDateTimeOffset();
        Assert.Equal(TimeSpan.FromMinutes(20), deadline - markedAt);
        Assert.False(markedJson.RootElement.TryGetProperty("exactAddress", out _));

        using var wrongConfirm = await worker.PostAsync($"/api/v1/changas/{id}/completion/confirm", null);
        Assert.Equal(HttpStatusCode.Forbidden, wrongConfirm.StatusCode);
        using var wrongReject = await worker.PostAsync($"/api/v1/changas/{id}/completion/reject", null);
        Assert.Equal(HttpStatusCode.Forbidden, wrongReject.StatusCode);
        using var confirmed = await author.PostAsync($"/api/v1/changas/{id}/completion/confirm", null);
        Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
        using var confirmedJson = await JsonDocument.ParseAsync(await confirmed.Content.ReadAsStreamAsync());
        Assert.Equal("confirmed", confirmedJson.RootElement.GetProperty("status").GetString());
        using var connection = new Microsoft.Data.SqlClient.SqlConnection(
            $"Server=(localdb)\\MSSQLLocalDB;Database={factory.DatabaseName};Trusted_Connection=True;TrustServerCertificate=True");
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Status FROM Changas WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);
        Assert.Equal("Confirmed", await command.ExecuteScalarAsync());
        using var coordination = await worker.GetAsync("/api/v1/coordinations");
        Assert.Equal(HttpStatusCode.OK, coordination.StatusCode);
        Assert.Contains(id.ToString(), await coordination.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Completion_rejection_and_expiration_leave_assignment_in_review_and_reject_late_answers()
    {
        using var author = await CreateVerifiedClientAsync();
        using var worker = await CreateVerifiedClientAsync();
        using var created = await author.PostAsJsonAsync("/api/v1/changas", ValidRequest());
        using var createdJson = await JsonDocument.ParseAsync(await created.Content.ReadAsStreamAsync());
        var id = createdJson.RootElement.GetProperty("id").GetGuid();
        await ApproveAsync(id);
        using var taken = await worker.PostAsync($"/api/v1/changas/{id}/take", null);
        using var marked = await worker.PostAsync($"/api/v1/changas/{id}/completion/mark-done", null);
        Assert.Equal(HttpStatusCode.OK, marked.StatusCode);
        using var rejected = await author.PostAsync($"/api/v1/changas/{id}/completion/reject", null);
        using var rejectedJson = await JsonDocument.ParseAsync(await rejected.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);
        Assert.Equal("completion_review", rejectedJson.RootElement.GetProperty("status").GetString());
        Assert.Equal("rejected", rejectedJson.RootElement.GetProperty("reviewCause").GetString());
        using var lateConfirm = await author.PostAsync($"/api/v1/changas/{id}/completion/confirm", null);
        using var lateJson = await JsonDocument.ParseAsync(await lateConfirm.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.Conflict, lateConfirm.StatusCode);
        Assert.Equal("completion_not_pending", lateJson.RootElement.GetProperty("code").GetString());

        using var newCreated = await author.PostAsJsonAsync("/api/v1/changas", ValidRequest());
        using var newJson = await JsonDocument.ParseAsync(await newCreated.Content.ReadAsStreamAsync());
        var expiredId = newJson.RootElement.GetProperty("id").GetGuid();
        await ApproveAsync(expiredId);
        using var newTaken = await worker.PostAsync($"/api/v1/changas/{expiredId}/take", null);
        using var newMarked = await worker.PostAsync($"/api/v1/changas/{expiredId}/completion/mark-done", null);
        Assert.Equal(HttpStatusCode.OK, newMarked.StatusCode);
        using (var connection = new Microsoft.Data.SqlClient.SqlConnection(
            $"Server=(localdb)\\MSSQLLocalDB;Database={factory.DatabaseName};Trusted_Connection=True;TrustServerCertificate=True"))
        {
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE ChangaAssignment SET CompletionDeadline = @Deadline WHERE ChangaId = @Id";
            command.Parameters.AddWithValue("@Deadline", DateTimeOffset.UtcNow.AddMinutes(-1));
            command.Parameters.AddWithValue("@Id", expiredId);
            Assert.Equal(1, await command.ExecuteNonQueryAsync());
        }
        using var expiredRead = await author.GetAsync($"/api/v1/changas/{expiredId}/completion");
        using var expiredJson = await JsonDocument.ParseAsync(await expiredRead.Content.ReadAsStreamAsync());
        Assert.Equal("completion_review", expiredJson.RootElement.GetProperty("status").GetString());
        Assert.Equal("expired", expiredJson.RootElement.GetProperty("reviewCause").GetString());
        using (var connection = new Microsoft.Data.SqlClient.SqlConnection(
            $"Server=(localdb)\\MSSQLLocalDB;Database={factory.DatabaseName};Trusted_Connection=True;TrustServerCertificate=True"))
        {
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT c.Status, a.CompletionReviewCause FROM Changas c JOIN ChangaAssignment a ON a.ChangaId = c.Id WHERE c.Id = @Id";
            command.Parameters.AddWithValue("@Id", expiredId);
            using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal("CompletionReview", reader.GetString(0));
            Assert.Equal("expired", reader.GetString(1));
        }
        using var lateReject = await author.PostAsync($"/api/v1/changas/{expiredId}/completion/reject", null);
        using var lateRejectJson = await JsonDocument.ParseAsync(await lateReject.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.Conflict, lateReject.StatusCode);
        Assert.Equal("completion_not_pending", lateRejectJson.RootElement.GetProperty("code").GetString());
    }

    private async Task ApproveAsync(Guid changaId)
    {
        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(
            $"Server=(localdb)\\MSSQLLocalDB;Database={factory.DatabaseName};Trusted_Connection=True;TrustServerCertificate=True");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Changas SET Status = N'Available' WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", changaId);
        Assert.Equal(1, await command.ExecuteNonQueryAsync());
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
        "limpieza-domestica",
        "Una changa descriptiva de prueba.",
        15000,
        "Nueva Córdoba",
        exactAddress,
        true,
        "worker",
        null,
        null);

    private sealed record ChangaRequest(
        string categoryId,
        string description,
        int reward,
        string zone,
        string exactAddress,
        bool materialsNeeded,
        string? materialsProvider,
        double? latitude,
        double? longitude);
}
