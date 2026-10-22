using System.Net;
using System.Text;
using Bunit;
using ChangaQuest.Client;
using ChangaQuest.Client.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace ChangaQuest.AcceptanceTests.Client;

public sealed class ChangaPageTests : Bunit.TestContext
{
    [Fact]
    public void Main_navigation_keeps_board_and_publish_destinations_available()
    {
        var layout = Render<MainLayout>(parameters => parameters.Add(
            component => component.Body,
            builder => builder.AddContent(0, "Contenido de prueba")));

        Assert.Contains("href=\"/tareas\"", layout.Markup, StringComparison.Ordinal);
        Assert.Contains("Ver tareas", layout.Markup, StringComparison.Ordinal);
        Assert.Contains("href=\"/publicar\"", layout.Markup, StringComparison.Ordinal);
        Assert.Contains("Publicar changa", layout.Markup, StringComparison.Ordinal);
        Assert.Contains("Contenido de prueba", layout.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Publish_form_keeps_input_after_api_error_and_associates_field_errors()
    {
        string? submittedBody = null;
        var handler = new RecordingHandler(async request =>
        {
            if (request.RequestUri?.AbsolutePath == "/api/v1/accounts/me")
            {
                return Json("""{"email":"persona@example.test"}""");
            }

            if (request.RequestUri?.AbsolutePath == "/api/v1/changas" && request.Method == HttpMethod.Post)
            {
                submittedBody = await request.Content!.ReadAsStringAsync();
                return Problem(HttpStatusCode.BadRequest,
                    "Revisá los datos.",
                    """{"description":["La descripción es obligatoria."]}""");
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/publicar");

        var app = Render<App>();
        app.WaitForAssertion(() => Assert.Contains("Publicar changa", app.Markup));
        app.Find("#description").Change("Ayuda para armar una biblioteca.");
        app.Find("#reward").Change("15000");
        app.Find("#zone").Change("Nueva Córdoba");
        app.Find("#exact-address").Change("Calle privada 123, Córdoba");
        app.Find("#materials-needed").Change("true");
        app.Find("#materials-provider").Change("worker");
        app.Find("form").Submit();

        app.WaitForAssertion(() =>
        {
            Assert.Contains("La descripción es obligatoria.", app.Markup);
            Assert.Contains("label for=\"description\"", app.Markup);
            Assert.Contains("label for=\"reward\"", app.Markup);
            Assert.Contains("label for=\"zone\"", app.Markup);
            Assert.Contains("label for=\"exact-address\"", app.Markup);
            Assert.Contains("label for=\"materials-needed\"", app.Markup);
            Assert.Contains("aria-describedby=\"description-validation description-error\"", app.Markup);
            Assert.Contains("Ayuda para armar una biblioteca.", app.Markup);
            Assert.Contains("\"description\":\"Ayuda para armar una biblioteca.\"", submittedBody);
        });
    }

    [Fact]
    public void Network_error_does_not_fake_success_or_clear_publication_fields()
    {
        var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath == "/api/v1/accounts/me")
            {
                return Task.FromResult(Json("""{"email":"persona@example.test"}"""));
            }

            if (request.RequestUri?.AbsolutePath == "/api/v1/changas" && request.Method == HttpMethod.Post)
            {
                throw new HttpRequestException("Servidor no disponible.");
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/publicar");

        var app = Render<App>();
        app.WaitForAssertion(() => Assert.Contains("Publicar changa", app.Markup));
        app.Find("#description").Change("Ayuda para armar una biblioteca.");
        app.Find("#reward").Change("15000");
        app.Find("#zone").Change("Nueva Cordoba");
        app.Find("#exact-address").Change("Calle privada 123, Cordoba");
        app.Find("#materials-needed").Change("false");
        app.Find("form").Submit();

        app.WaitForAssertion(() =>
        {
            Assert.Contains("No se pudo conectar con el servidor", app.Markup);
            Assert.Contains("Ayuda para armar una biblioteca.", app.Markup);
            Assert.DoesNotContain("success-message", app.Markup);
        });
    }

    [Fact]
    public async Task No_materials_answer_is_explicit_and_omits_materials_provider()
    {
        string? submittedBody = null;
        var handler = new RecordingHandler(async request =>
        {
            if (request.RequestUri?.AbsolutePath == "/api/v1/accounts/me")
            {
                return Json("""{"email":"persona@example.test"}""");
            }

            if (request.RequestUri?.AbsolutePath == "/api/v1/changas" && request.Method == HttpMethod.Post)
            {
                submittedBody = await request.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/publicar");

        var app = Render<App>();
        app.WaitForAssertion(() => Assert.Contains("Publicar changa", app.Markup));
        app.Find("#description").Change("Ayuda para armar una biblioteca.");
        app.Find("#reward").Change("15000");
        app.Find("#zone").Change("Nueva Cordoba");
        app.Find("#exact-address").Change("Calle privada 123, Cordoba");
        app.Find("#materials-needed").Change("false");
        app.Find("form").Submit();

        app.WaitForAssertion(() =>
        {
            Assert.Contains("La changa qued", app.Markup);
            Assert.Contains("\"materialsNeeded\":false", submittedBody);
            Assert.DoesNotContain("materialsProvider", submittedBody, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Board_shows_sample_label_without_exposing_private_address()
    {
        const string exactAddress = "Direccion privada de la fixture";
        var handler = new RecordingHandler(request => Task.FromResult(
            request.RequestUri?.AbsolutePath switch
            {
                "/api/v1/accounts/me" => Json("""{"email":"persona@example.test"}"""),
                "/api/v1/changas" => Json("""[{"id":"a1000000-0000-4000-8000-000000000001","description":"Ejemplo ficticio: ordenar libros y armar una biblioteca pequeña.","reward":12000,"zone":"Nueva Córdoba","materialsNeeded":false,"materialsProvider":null,"status":"available","assignmentMode":"first_come","isSample":true,"createdAt":"2026-01-01T12:00:00Z"}]"""),
                _ => throw new InvalidOperationException($"Unexpected request: {request.RequestUri}")
            }));
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/tareas");

        var app = Render<App>();

        app.WaitForAssertion(() =>
        {
            Assert.Contains("Ejemplo ficticio", app.Markup);
            Assert.Contains("Nueva Córdoba", app.Markup);
            Assert.Contains("No hacen falta materiales", app.Markup);
            Assert.Contains("Por orden de llegada", app.Markup);
            Assert.DoesNotContain(exactAddress, app.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Board_has_loading_empty_and_retryable_error_states()
    {
        var requests = 0;
        var listResponse = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath == "/api/v1/accounts/me")
            {
                return Task.FromResult(Json("""{"email":"persona@example.test"}"""));
            }

            if (request.RequestUri?.AbsolutePath == "/api/v1/changas" && Interlocked.Increment(ref requests) == 1)
            {
                return listResponse.Task;
            }

            return Task.FromResult(Json("[]"));
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/tareas");

        var app = Render<App>();
        app.WaitForAssertion(() => Assert.Contains("Cargando changas", app.Markup));
        listResponse.SetResult(Problem(HttpStatusCode.ServiceUnavailable, "Servicio fuera de línea."));
        app.WaitForAssertion(() => Assert.Contains("No se pudieron cargar las changas", app.Markup));

        app.Find("button").Click();
        app.WaitForAssertion(() => Assert.Contains("Todavía no hay changas disponibles", app.Markup));
    }
    private static HttpResponseMessage Json(string content) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(content, Encoding.UTF8, "application/json")
    };

    private static HttpResponseMessage Problem(HttpStatusCode status, string detail, string? errors = null) =>
        new(status)
        {
            Content = new StringContent(
                errors is null
                    ? $$"""{"status":{{(int)status}},"title":"Error","detail":"{{detail}}"}"""
                    : $$"""{"status":{{(int)status}},"title":"Error","detail":"{{detail}}","errors":{{errors}}}""",
                Encoding.UTF8,
                "application/problem+json")
        };

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => respond(request);
    }
}



