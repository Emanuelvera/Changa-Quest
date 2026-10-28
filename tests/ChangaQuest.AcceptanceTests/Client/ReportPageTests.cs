using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using ChangaQuest.Client;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace ChangaQuest.AcceptanceTests.Client;

public sealed class ReportPageTests : Bunit.TestContext
{
    private static readonly Guid ChangaId = Guid.Parse("a3761640-17ea-4f4a-a2c2-7f03d8a40b94");

    [Fact]
    public void Report_submission_uses_the_api_and_does_not_disclose_report_details()
    {
        string? submittedBody = null;
        var handler = new RecordingHandler(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/v1/reports", request.RequestUri?.AbsolutePath);
            submittedBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(
                    """{"id":"e4e6160a-5e28-4f1b-a760-62c7414f6e1d","status":"pending","createdAt":"2026-10-27T00:00:00Z"}""",
                    Encoding.UTF8,
                    "application/json")
            };
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"http://localhost/tareas/{ChangaId}/reporte");

        var app = Render<App>();
        app.WaitForAssertion(() => Assert.Contains("Enviar reporte", app.Markup));
        app.Find("#report-reason").Change("motivo privado");
        app.Find("#report-evidence").Change("evidencia privada");
        app.Find("form").Submit();

        app.WaitForAssertion(() =>
        {
            Assert.Contains("\"changaId\":\"a3761640-17ea-4f4a-a2c2-7f03d8a40b94\"", submittedBody);
            Assert.Contains("\"reason\":\"motivo privado\"", submittedBody);
            Assert.Contains("\"evidence\":\"evidencia privada\"", submittedBody);
            Assert.Contains("El reporte quedó enviado para revisión privada.", app.Markup);
            Assert.DoesNotContain("motivo privado", app.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("evidencia privada", app.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Report_API_error_is_explicit_and_preserves_the_form()
    {
        var handler = new RecordingHandler(async request =>
        {
            _ = await request.Content!.ReadAsStringAsync();
            return Problem(
                HttpStatusCode.BadRequest,
                """{"reason":["Indicá un motivo válido para el reporte."]}""");
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"http://localhost/tareas/{ChangaId}/reporte");

        var app = Render<App>();
        app.WaitForAssertion(() => Assert.Contains("Enviar reporte", app.Markup));
        app.Find("#report-reason").Change("detalle del motivo");
        app.Find("#report-evidence").Change("referencia de evidencia");
        app.Find("form").Submit();

        app.WaitForAssertion(() =>
        {
            Assert.Contains("Indicá un motivo válido para el reporte.", app.Markup);
            Assert.Contains("detalle del motivo", app.Markup);
            Assert.Contains("referencia de evidencia", app.Markup);
            Assert.Contains("id=\"report-reason-error\"", app.Markup);
            Assert.Contains("aria-describedby=\"report-reason-hint report-reason-validation report-reason-error\"", app.Markup);
            Assert.Equal("true", app.Find("#report-reason").GetAttribute("aria-invalid"));
            Assert.DoesNotContain("reporte quedó enviado", app.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Report_form_omits_blank_optional_evidence()
    {
        string? submittedBody = null;
        var handler = new RecordingHandler(async request =>
        {
            submittedBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(
                    """{"id":"e4e6160a-5e28-4f1b-a760-62c7414f6e1d","status":"pending","createdAt":"2026-10-27T00:00:00Z"}""",
                    Encoding.UTF8,
                    "application/json")
            };
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"http://localhost/tareas/{ChangaId}/reporte");

        var app = Render<App>();
        app.WaitForAssertion(() => Assert.Contains("Enviar reporte", app.Markup));
        app.Find("#report-reason").Change("Un motivo sin adjuntos");
        app.Find("form").Submit();

        app.WaitForAssertion(() =>
        {
            using var body = JsonDocument.Parse(submittedBody!);
            Assert.Equal(ChangaId, body.RootElement.GetProperty("changaId").GetGuid());
            Assert.Equal("Un motivo sin adjuntos", body.RootElement.GetProperty("reason").GetString());
            Assert.False(body.RootElement.TryGetProperty("evidence", out _));
        });
    }

    [Fact]
    public void Empty_report_reason_is_blocked_and_its_error_is_associated()
    {
        var handler = new RecordingHandler(_ =>
            throw new InvalidOperationException("The API must not be called for an empty reason."));
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"http://localhost/tareas/{ChangaId}/reporte");

        var app = Render<App>();
        app.WaitForAssertion(() => Assert.Contains("Enviar reporte", app.Markup));
        app.Find("form").Submit();

        app.WaitForAssertion(() =>
        {
            Assert.Contains("Indicá el motivo del reporte.", app.Markup);
            Assert.Contains("aria-describedby=\"report-reason-hint report-reason-validation\"", app.Markup);
            Assert.Equal("true", app.Find("#report-reason").GetAttribute("aria-invalid"));
            Assert.Contains("id=\"report-reason-validation\"", app.Markup);
        });
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "Iniciá sesión para enviar un reporte.")]
    [InlineData(HttpStatusCode.Forbidden, "Tu cuenta no está habilitada para enviar reportes.")]
    public void Report_authentication_errors_are_shown_without_false_success(
        HttpStatusCode statusCode,
        string detail)
    {
        var handler = new RecordingHandler(_ => Task.FromResult(ApiProblem(statusCode, detail)));
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"http://localhost/tareas/{ChangaId}/reporte");

        var app = Render<App>();
        app.WaitForAssertion(() => Assert.Contains("Enviar reporte", app.Markup));
        app.Find("#report-reason").Change("Motivo de prueba");
        app.Find("form").Submit();

        app.WaitForAssertion(() =>
        {
            Assert.Contains(detail, app.Markup);
            Assert.DoesNotContain("El reporte quedó enviado", app.Markup, StringComparison.Ordinal);
            Assert.Contains("Motivo de prueba", app.Markup);
        });
    }

    private static HttpResponseMessage Problem(HttpStatusCode statusCode, string errors) =>
        new(statusCode)
        {
            Content = new StringContent(
                $$"""{"type":"about:blank","status":{{(int)statusCode}},"title":"Error","detail":"Revisá los datos del reporte.","errors":{{errors}}}""",
                Encoding.UTF8,
                "application/problem+json")
        };

    private static HttpResponseMessage ApiProblem(HttpStatusCode statusCode, string detail) =>
        new(statusCode)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { detail }),
                Encoding.UTF8,
                "application/problem+json")
        };

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            respond(request);
    }
}
