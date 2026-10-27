using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ChangaQuest.AcceptanceTests.Accounts;

namespace ChangaQuest.AcceptanceTests.Reports;

public sealed class ReportApiTests(AccountApiFactory factory) : IClassFixture<AccountApiFactory>
{
    [Fact]
    public async Task Report_endpoints_require_a_session()
    {
        using var client = factory.CreateClient();

        using var submit = await client.PostAsJsonAsync("/api/v1/reports", new { changaId = Guid.NewGuid(), reason = "x" });
        using var read = await client.GetAsync($"/api/v1/reports/{Guid.NewGuid()}");
        using var cases = await client.GetAsync("/api/v1/reports");

        Assert.Equal(HttpStatusCode.Unauthorized, submit.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, read.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, cases.StatusCode);
        Assert.Equal("application/problem+json", read.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Case_index_shows_only_participant_cases_or_the_administrative_queue()
    {
        using var author = await CreateVerifiedClientAsync();
        using var worker = await CreateVerifiedClientAsync();
        using var unrelated = await CreateVerifiedClientAsync();
        var (admin, adminEmail, adminPassword) = await CreateVerifiedClientWithCredentialsAsync();
        using var _ = admin;
        var changaId = await CreateChangaAsync(author);
        await SetChangaAvailableAsync(changaId);
        using var taken = await worker.PostAsync($"/api/v1/changas/{changaId}/take", null);
        Assert.Equal(HttpStatusCode.OK, taken.StatusCode);
        using var submitted = await author.PostAsJsonAsync(
            "/api/v1/reports",
            new { changaId, reason = "Motivo privado", evidence = "Evidencia privada" });
        using var submittedJson = await JsonDocument.ParseAsync(await submitted.Content.ReadAsStreamAsync());
        var reportId = submittedJson.RootElement.GetProperty("id").GetGuid();

        await GrantAdministratorAsync(adminEmail);
        using var login = await admin.PostAsJsonAsync(
            "/api/v1/accounts/login", new { email = adminEmail, password = adminPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using var authorCases = await author.GetAsync("/api/v1/reports");
        using var workerCases = await worker.GetAsync("/api/v1/reports");
        using var unrelatedCases = await unrelated.GetAsync("/api/v1/reports");
        using var adminCases = await admin.GetAsync("/api/v1/reports");
        using var authorJson = await JsonDocument.ParseAsync(await authorCases.Content.ReadAsStreamAsync());
        using var workerJson = await JsonDocument.ParseAsync(await workerCases.Content.ReadAsStreamAsync());
        using var unrelatedJson = await JsonDocument.ParseAsync(await unrelatedCases.Content.ReadAsStreamAsync());
        using var adminJson = await JsonDocument.ParseAsync(await adminCases.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, authorCases.StatusCode);
        Assert.Equal(HttpStatusCode.OK, workerCases.StatusCode);
        Assert.Equal(HttpStatusCode.OK, unrelatedCases.StatusCode);
        Assert.Equal(HttpStatusCode.OK, adminCases.StatusCode);
        Assert.Contains(authorJson.RootElement.EnumerateArray(), item => item.GetProperty("id").GetGuid() == reportId);
        Assert.Contains(workerJson.RootElement.EnumerateArray(), item => item.GetProperty("id").GetGuid() == reportId);
        Assert.Empty(unrelatedJson.RootElement.EnumerateArray());
        Assert.Contains(adminJson.RootElement.EnumerateArray(), item => item.GetProperty("id").GetGuid() == reportId);
        var listContent = await adminCases.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Motivo privado", listContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Evidencia privada", listContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verified_account_reports_a_changa_without_getting_reason_or_evidence_back()
    {
        using var author = await CreateVerifiedClientAsync();
        using var reporter = await CreateVerifiedClientAsync();
        var changaId = await CreateChangaAsync(author);

        using var response = await reporter.PostAsJsonAsync(
            "/api/v1/reports",
            new { changaId, reason = "Contenido inapropiado", evidence = "https://example.test/captura" });
        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("pending", json.RootElement.GetProperty("status").GetString());
        Assert.NotEqual(Guid.Empty, json.RootElement.GetProperty("id").GetGuid());
        Assert.DoesNotContain("Contenido inapropiado", content, StringComparison.Ordinal);
        Assert.DoesNotContain("captura", content, StringComparison.Ordinal);
        Assert.Equal(1, await CountReportsAsync(changaId));
    }

    [Fact]
    public async Task Report_without_evidence_is_accepted()
    {
        using var author = await CreateVerifiedClientAsync();
        var changaId = await CreateChangaAsync(author);

        using var response = await author.PostAsJsonAsync("/api/v1/reports", new { changaId, reason = "Solo motivo" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Empty_reason_is_a_validation_error_and_nothing_is_stored()
    {
        using var author = await CreateVerifiedClientAsync();
        var changaId = await CreateChangaAsync(author);

        using var response = await author.PostAsJsonAsync("/api/v1/reports", new { changaId, reason = "   " });
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty("reason", out _));
        Assert.Equal(0, await CountReportsAsync(changaId));
    }

    [Fact]
    public async Task Missing_body_or_unknown_changa_is_rejected()
    {
        using var client = await CreateVerifiedClientAsync();

        using var empty = await client.PostAsync("/api/v1/reports", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        using var unknown = await client.PostAsJsonAsync("/api/v1/reports", new { changaId = Guid.NewGuid(), reason = "Motivo" });
        using var unknownJson = await JsonDocument.ParseAsync(await unknown.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal("changa_not_found", unknownJson.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Only_an_administrator_reads_a_report_and_its_evidence()
    {
        using var author = await CreateVerifiedClientAsync();
        using var reporter = await CreateVerifiedClientAsync();
        using var other = await CreateVerifiedClientAsync();
        var (admin, adminEmail, adminPassword) = await CreateVerifiedClientWithCredentialsAsync();
        using var _ = admin;
        var changaId = await CreateChangaAsync(author);
        using var submitted = await reporter.PostAsJsonAsync(
            "/api/v1/reports",
            new { changaId, reason = "Motivo secreto", evidence = "Evidencia secreta" });
        using var submittedJson = await JsonDocument.ParseAsync(await submitted.Content.ReadAsStreamAsync());
        var reportId = submittedJson.RootElement.GetProperty("id").GetGuid();

        foreach (var client in new[] { reporter, other, author, admin })
        {
            using var denied = await client.GetAsync($"/api/v1/reports/{reportId}");
            var body = await denied.Content.ReadAsStringAsync();
            Assert.True(denied.StatusCode == HttpStatusCode.Forbidden, body);
            Assert.Contains("report_forbidden", body, StringComparison.Ordinal);
            Assert.DoesNotContain("secret", body, StringComparison.Ordinal);
        }

        await GrantAdministratorAsync(adminEmail);
        using var login = await admin.PostAsJsonAsync("/api/v1/accounts/login", new { email = adminEmail, password = adminPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var allowed = await admin.GetAsync($"/api/v1/reports/{reportId}");
        using var allowedJson = await JsonDocument.ParseAsync(await allowed.Content.ReadAsStreamAsync());
        using var notFound = await admin.GetAsync($"/api/v1/reports/{Guid.NewGuid()}");
        using var notFoundJson = await JsonDocument.ParseAsync(await notFound.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Equal("Motivo secreto", allowedJson.RootElement.GetProperty("reason").GetString());
        Assert.Equal("Evidencia secreta", allowedJson.RootElement.GetProperty("evidence").GetString());
        Assert.Equal(changaId, allowedJson.RootElement.GetProperty("changaId").GetGuid());
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
        Assert.Equal("report_not_found", notFoundJson.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Parties_can_respond_receive_a_decision_and_appeal_without_exposing_private_evidence()
    {
        using var author = await CreateVerifiedClientAsync();
        using var worker = await CreateVerifiedClientAsync();
        using var reporter = await CreateVerifiedClientAsync();
        using var unrelated = await CreateVerifiedClientAsync();
        var (admin, adminEmail, adminPassword) = await CreateVerifiedClientWithCredentialsAsync();
        using var _ = admin;
        var changaId = await CreateChangaAsync(author);
        await SetChangaAvailableAsync(changaId);
        using var taken = await worker.PostAsync($"/api/v1/changas/{changaId}/take", null);
        Assert.Equal(HttpStatusCode.OK, taken.StatusCode);

        using var submitted = await reporter.PostAsJsonAsync(
            "/api/v1/reports",
            new { changaId, reason = "Motivo privado", evidence = "Evidencia privada" });
        using var submittedJson = await JsonDocument.ParseAsync(await submitted.Content.ReadAsStreamAsync());
        var reportId = submittedJson.RootElement.GetProperty("id").GetGuid();

        await GrantAdministratorAsync(adminEmail);
        using var login = await admin.PostAsJsonAsync("/api/v1/accounts/login", new { email = adminEmail, password = adminPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using var authorCase = await author.GetAsync($"/api/v1/reports/{reportId}/case");
        using var workerCase = await worker.GetAsync($"/api/v1/reports/{reportId}/case");
        using var unrelatedCase = await unrelated.GetAsync($"/api/v1/reports/{reportId}/case");
        Assert.Equal(HttpStatusCode.OK, authorCase.StatusCode);
        Assert.Equal(HttpStatusCode.OK, workerCase.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, unrelatedCase.StatusCode);
        var caseBody = await authorCase.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Motivo privado", caseBody, StringComparison.Ordinal);
        Assert.DoesNotContain("Evidencia privada", caseBody, StringComparison.Ordinal);
        using var unauthorizedDecision = await author.PostAsJsonAsync(
            $"/api/v1/reports/{reportId}/decision",
            new { conclusion = "No autorizada", reason = "Motivo", evidenceConsidered = false });
        using var unrelatedResponse = await unrelated.PostAsJsonAsync(
            $"/api/v1/reports/{reportId}/responses", new { text = "Respuesta de una persona ajena" });
        using var prematureAppeal = await author.PostAsJsonAsync(
            $"/api/v1/reports/{reportId}/appeals", new { text = "Apelación prematura" });
        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedDecision.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, unrelatedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, prematureAppeal.StatusCode);

        using var authorResponse = await author.PostAsJsonAsync(
            $"/api/v1/reports/{reportId}/responses", new { text = "Respuesta de quien solicita" });
        using var workerResponse = await worker.PostAsJsonAsync(
            $"/api/v1/reports/{reportId}/responses", new { text = "Respuesta de quien realiza" });
        using var adminReport = await admin.GetAsync($"/api/v1/reports/{reportId}");
        using var adminReportJson = await JsonDocument.ParseAsync(await adminReport.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.Created, authorResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, workerResponse.StatusCode);
        Assert.Equal(2, adminReportJson.RootElement.GetProperty("responses").GetArrayLength());

        var initialChangaStatus = await GetChangaStatusAsync(changaId);
        using var decision = await admin.PostAsJsonAsync(
            $"/api/v1/reports/{reportId}/decision",
            new { conclusion = "Se revisó el reporte", reason = "Motivo de la decisión", evidenceConsidered = true });
        using var decisionJson = await JsonDocument.ParseAsync(await decision.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.OK, decision.StatusCode);
        Assert.Equal("Se revisó el reporte", decisionJson.RootElement.GetProperty("conclusion").GetString());
        Assert.Equal(initialChangaStatus, await GetChangaStatusAsync(changaId));
        using var duplicateDecision = await admin.PostAsJsonAsync(
            $"/api/v1/reports/{reportId}/decision",
            new { conclusion = "Segunda conclusión", reason = "Motivo", evidenceConsidered = false });
        Assert.Equal(HttpStatusCode.Conflict, duplicateDecision.StatusCode);

        using var caseAfterDecision = await author.GetAsync($"/api/v1/reports/{reportId}/case");
        using var caseJson = await JsonDocument.ParseAsync(await caseAfterDecision.Content.ReadAsStreamAsync());
        Assert.True(caseJson.RootElement.GetProperty("canAppeal").GetBoolean());
        using var lateResponse = await author.PostAsJsonAsync(
            $"/api/v1/reports/{reportId}/responses", new { text = "Respuesta tardía" });
        Assert.Equal(HttpStatusCode.Conflict, lateResponse.StatusCode);

        using var appeal = await author.PostAsJsonAsync(
            $"/api/v1/reports/{reportId}/appeals", new { text = "Apelo la decisión" });
        using var appealJson = await JsonDocument.ParseAsync(await appeal.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.OK, appeal.StatusCode);
        Assert.Null(appeal.Headers.Location);
        var appealId = appealJson.RootElement.GetProperty("id").GetGuid();
        using var unrelatedAppeal = await unrelated.PostAsJsonAsync(
            $"/api/v1/reports/{reportId}/appeals", new { text = "Apelación ajena" });
        Assert.Equal(HttpStatusCode.Forbidden, unrelatedAppeal.StatusCode);
        using var unauthorizedResolution = await unrelated.PostAsJsonAsync(
            $"/api/v1/reports/{reportId}/appeals/{appealId}/resolution",
            new { resolution = "No autorizada" });
        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedResolution.StatusCode);

        using var resolution = await admin.PostAsJsonAsync(
            $"/api/v1/reports/{reportId}/appeals/{appealId}/resolution",
            new { resolution = "Se mantiene la decisión" });
        using var resolutionJson = await JsonDocument.ParseAsync(await resolution.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.OK, resolution.StatusCode);
        Assert.Equal("resolved", resolutionJson.RootElement.GetProperty("status").GetString());
        Assert.Equal("Se mantiene la decisión", resolutionJson.RootElement.GetProperty("resolution").GetString());
    }

    [Fact]
    public async Task Ineligible_session_cannot_submit()
    {
        var (client, email, _) = await CreateVerifiedClientWithCredentialsAsync();
        using var _c = client;
        await ExecuteAsync("UPDATE AspNetUsers SET DeclaredAdult = 0 WHERE Email = @Email", ("@Email", email));

        using var response = await client.PostAsJsonAsync("/api/v1/reports", new { changaId = Guid.NewGuid(), reason = "Motivo" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<Guid> CreateChangaAsync(HttpClient author)
    {
        using var created = await author.PostAsJsonAsync("/api/v1/changas", new
        {
            categoryId = "limpieza-domestica",
            description = "Una changa descriptiva de prueba.",
            reward = 15000,
            zone = "Nueva Córdoba",
            exactAddress = "Calle privada 123, Córdoba",
            materialsNeeded = true,
            materialsProvider = "worker"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var json = await JsonDocument.ParseAsync(await created.Content.ReadAsStreamAsync());
        return json.RootElement.GetProperty("id").GetGuid();
    }

    private async Task SetChangaAvailableAsync(Guid changaId)
    {
        await ExecuteAsync(
            "UPDATE Changas SET Status = N'Available' WHERE Id = @Id",
            ("@Id", changaId));
    }

    private async Task<long> CountReportsAsync(Guid changaId)
    {
        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Reports WHERE ChangaId = @Id";
        command.Parameters.AddWithValue("@Id", changaId);
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private async Task<string> GetChangaStatusAsync(Guid changaId)
    {
        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Status FROM Changas WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", changaId);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private async Task GrantAdministratorAsync(string email)
    {
        await ExecuteAsync(
            "IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = N'ADMINISTRATOR') " +
            "INSERT INTO AspNetRoles (Id, Name, NormalizedName) VALUES (NEWID(), N'Administrator', N'ADMINISTRATOR'); " +
            "INSERT INTO AspNetUserRoles (UserId, RoleId) " +
            "SELECT u.Id, r.Id FROM AspNetUsers u, AspNetRoles r WHERE u.Email = @Email AND r.NormalizedName = N'ADMINISTRATOR'",
            ("@Email", email));
    }

    private async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        Assert.True(await command.ExecuteNonQueryAsync() >= 1);
    }

    private string ConnectionString =>
        $"Server=(localdb)\\MSSQLLocalDB;Database={factory.DatabaseName};Trusted_Connection=True;TrustServerCertificate=True";

    private async Task<HttpClient> CreateVerifiedClientAsync() => (await CreateVerifiedClientWithCredentialsAsync()).Client;

    private async Task<(HttpClient Client, string Email, string Password)> CreateVerifiedClientWithCredentialsAsync()
    {
        var client = factory.CreateClient();
        var email = $"report-{Guid.NewGuid():N}@example.test";
        const string password = "contraseña-larga-de-prueba";
        using var registration = await client.PostAsJsonAsync(
            "/api/v1/accounts/register",
            new { email, password, declaredAdult = true });
        Assert.Equal(HttpStatusCode.Accepted, registration.StatusCode);
        var logEntry = factory.LogEntries.Last(entry =>
            entry.StartsWith("Enlace local de verificación:", StringComparison.Ordinal));
        var verificationLink = new Uri(logEntry[logEntry.IndexOf("http", StringComparison.Ordinal)..]);
        using var confirmation = await client.GetAsync(verificationLink.PathAndQuery);
        Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);
        using var login = await client.PostAsJsonAsync("/api/v1/accounts/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return (client, email, password);
    }
}
