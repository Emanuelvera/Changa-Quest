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
        Assert.Contains("href=\"/perfil\"", layout.Markup, StringComparison.Ordinal);
        Assert.Contains("Perfil", layout.Markup, StringComparison.Ordinal);
        Assert.Contains("Saltar al contenido", layout.Markup, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Navegación móvil\"", layout.Markup, StringComparison.Ordinal);
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

            if (request.RequestUri?.AbsolutePath == "/api/v1/changas/categories")
            {
                return Json("""[{"id":"limpieza-domestica","name":"Limpieza doméstica"}]""");
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
        app.Find("#category").Change("limpieza-domestica");
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

            if (request.RequestUri?.AbsolutePath == "/api/v1/changas/categories")
            {
                return Task.FromResult(Json("""[{"id":"limpieza-domestica","name":"Limpieza doméstica"}]"""));
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
        app.Find("#category").Change("limpieza-domestica");
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

            if (request.RequestUri?.AbsolutePath == "/api/v1/changas/categories")
            {
                return Json("""[{"id":"limpieza-domestica","name":"Limpieza doméstica"}]""");
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
        app.Find("#category").Change("limpieza-domestica");
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

    [Fact]
    public void Detail_requires_explicit_take_and_reports_conflict_without_success()
    {
        var id = Guid.Parse("a1000000-0000-4000-8000-000000000001");
        var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath == $"/api/v1/changas/{id}" && request.Method == HttpMethod.Get)
            {
                return Task.FromResult(Json(
                    """{"id":"a1000000-0000-4000-8000-000000000001","description":"Ayudar a ordenar una biblioteca pequeña.","reward":12000,"zone":"Nueva Córdoba","materialsNeeded":false,"materialsProvider":null,"status":"available","assignmentMode":"first_come","isSample":false,"createdAt":"2026-01-01T12:00:00Z"}"""));
            }

            if (request.RequestUri?.AbsolutePath == $"/api/v1/changas/{id}/take" && request.Method == HttpMethod.Post)
            {
                return Task.FromResult(Problem(
                    HttpStatusCode.Conflict,
                    "La changa ya fue asignada",
                    """{"code":"changa_already_assigned"}"""));
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        Services.GetRequiredService<NavigationManager>().NavigateTo($"http://localhost/tareas/{id}");

        var app = Render<App>();

        app.WaitForAssertion(() =>
        {
            Assert.Contains("Tomar esta changa", app.Markup);
            Assert.Contains("confirmación es una acción separada", app.Markup);
            Assert.Contains($"/tareas/{id}/reporte", app.Markup);
        });
        app.Find("button").Click();
        app.WaitForAssertion(() =>
        {
            Assert.Contains("ya no está disponible", app.Markup);
            Assert.DoesNotContain("La changa quedó asignada a vos", app.Markup);
        });
    }
    [Fact]
    public void Detail_shows_coordination_link_after_successful_take()
    {
        var id = Guid.Parse("a1000000-0000-4000-8000-000000000001");
        var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath == $"/api/v1/changas/{id}")
            {
                return Task.FromResult(Json("""{"id":"a1000000-0000-4000-8000-000000000001","description":"Ayudar a ordenar una biblioteca.","reward":12000,"zone":"Centro","materialsNeeded":false,"materialsProvider":null,"status":"available","assignmentMode":"first_come","isSample":false,"createdAt":"2026-01-01T12:00:00Z"}"""));
            }
            if (request.RequestUri?.AbsolutePath == $"/api/v1/changas/{id}/take")
            {
                return Task.FromResult(Json("""{"changa":{"id":"a1000000-0000-4000-8000-000000000001"},"assignment":{"id":"b1000000-0000-4000-8000-000000000001","assignedAt":"2026-01-01T12:00:00Z"}}"""));
            }
            throw new InvalidOperationException($"Unexpected request: {request.RequestUri}");
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var app = Render<ChangaQuest.Client.Pages.DetalleChanga>(parameters => parameters.Add(component => component.Id, id));
        app.WaitForAssertion(() => Assert.Contains("Tomar esta changa", app.Markup));
        app.Find("button").Click();
        app.WaitForAssertion(() => Assert.Contains($"/tareas/{id}/coordinacion", app.Markup));
    }

    [Fact]
    public void Coordination_page_shows_worker_address_but_no_phone_before_mutual_consent_and_supports_decline()
    {
        var id = Guid.Parse("a1000000-0000-4000-8000-000000000001");
        string? submitted = null;
        var handler = new RecordingHandler(async request =>
        {
            if (request.RequestUri?.AbsolutePath == $"/api/v1/changas/{id}/coordination" && request.Method == HttpMethod.Get)
            {
                return Json("""{"changaId":"a1000000-0000-4000-8000-000000000001","description":"Organizar una changa ficticia.","role":"worker","yourConsent":"pending","otherPartyConsent":"not_shared","hasMutualConsent":false,"otherPhone":null,"exactAddress":"Dirección ficticia 123"}""");
            }

            if (request.RequestUri?.AbsolutePath == $"/api/v1/changas/{id}/coordination/phone-consent" && request.Method == HttpMethod.Put)
            {
                submitted = await request.Content!.ReadAsStringAsync();
                return Json("""{"changaId":"a1000000-0000-4000-8000-000000000001","description":"Organizar una changa ficticia.","role":"worker","yourConsent":"declined","otherPartyConsent":"not_shared","hasMutualConsent":false,"otherPhone":null,"exactAddress":"Dirección ficticia 123"}""");
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var page = Render<ChangaQuest.Client.Pages.Coordinacion>(parameters => parameters.Add(component => component.Id, id));
        page.WaitForAssertion(() =>
        {
            Assert.Contains("Dirección ficticia 123", page.Markup, StringComparison.Ordinal);
            Assert.Contains("permanecen ocultos", page.Markup);
            Assert.Contains("teléfono ficticio", page.Markup);
            Assert.Contains($"/tareas/{id}/finalizacion", page.Markup);
            Assert.Contains("label for=\"phone\"", page.Markup);
        });

        page.Find("button.button.secondary").Click();
        page.WaitForAssertion(() =>
        {
            Assert.Contains("La asignación sigue activa", page.Markup);
            using var body = System.Text.Json.JsonDocument.Parse(submitted!);
            Assert.False(body.RootElement.GetProperty("sharePhone").GetBoolean());
            Assert.False(body.RootElement.TryGetProperty("phone", out _));
        });
    }

    [Fact]
    public void Coordinations_list_handles_unauthorized_access_with_a_clear_message()
    {
        var handler = new RecordingHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var page = Render<ChangaQuest.Client.Pages.Coordinaciones>();

        page.WaitForAssertion(() => Assert.Contains("Inici\u00e1 sesi\u00f3n e intent\u00e1 de nuevo", page.Markup));
    }

    [Fact]
    public void Coordinations_list_links_to_each_private_assignment()
    {
        var id = Guid.Parse("a1000000-0000-4000-8000-000000000001");
        var handler = new RecordingHandler(_ => Task.FromResult(Json("""[{"changaId":"a1000000-0000-4000-8000-000000000001","description":"Pintar una pared interior.","role":"worker","yourConsent":"pending","otherPartyConsent":"not_shared","hasMutualConsent":false,"otherPhone":null,"exactAddress":"Dirección ficticia 123"}]""")));
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var page = Render<ChangaQuest.Client.Pages.Coordinaciones>();

        page.WaitForAssertion(() =>
        {
            Assert.Contains("Pintar una pared interior", page.Markup);
            Assert.DoesNotContain("Dirección ficticia 123", page.Markup);
            Assert.Contains($"/tareas/{id}/coordinacion", page.Markup);
        });
    }

    [Fact]
    public void Coordinations_list_keeps_mutual_cancellation_confirmation_after_assignment_is_released()
    {
        var getCount = 0;
        var handler = new RecordingHandler(request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                getCount++;
                return Task.FromResult(getCount == 1
                    ? Json("""[{"changaId":"a1000000-0000-4000-8000-000000000001","description":"Organizar una changa ficticia.","role":"worker","yourConsent":"pending","otherPartyConsent":"not_shared","hasMutualConsent":false,"otherPhone":null,"exactAddress":null,"cancellationRequest":{"id":"a1000000-0000-4000-8000-000000000002","changaId":"a1000000-0000-4000-8000-000000000001","status":"pending","requestedByRole":"requester","createdAt":"2026-01-01T12:00:00Z","respondedAt":null}}]""")
                    : Json("[]"));
            }

            if (request.Method == HttpMethod.Post)
            {
                return Task.FromResult(Json("""{"code":"cancellation_request_updated"}"""));
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        JSInterop.Setup<bool>("confirm", _ => true).SetResult(true);

        var page = Render<ChangaQuest.Client.Pages.Coordinaciones>();

        page.WaitForAssertion(() => Assert.Contains("Aceptar cancelación", page.Markup));
        page.Find("button").Click();

        page.WaitForAssertion(() =>
        {
            Assert.Contains("La cancelación fue confirmada y la asignación fue liberada.", page.Markup);
            Assert.DoesNotContain("Organizar una changa ficticia.", page.Markup);
        });
    }

    [Fact]
    public void Completion_page_marks_work_done_and_shows_a_clear_response_deadline()
    {
        var id = Guid.Parse("a1000000-0000-4000-8000-000000000001");
        var marked = false;
        var handler = new RecordingHandler(async request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return Json("""{"changaId":"a1000000-0000-4000-8000-000000000001","description":"Organizar una changa ficticia.","role":"worker","status":"assigned","markedDoneAt":null,"responseDeadline":null,"reviewCause":null}""");
            }
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.EndsWith("/completion/mark-done", request.RequestUri!.AbsolutePath, StringComparison.Ordinal);
            marked = true;
            return Json("""{"changaId":"a1000000-0000-4000-8000-000000000001","description":"Organizar una changa ficticia.","role":"worker","status":"pending_response","markedDoneAt":"2026-10-25T15:00:00Z","responseDeadline":"2026-10-25T15:20:00Z","reviewCause":null}""");
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var page = Render<ChangaQuest.Client.Pages.Finalizacion>(parameters => parameters.Add(component => component.Id, id));
        page.WaitForAssertion(() => Assert.Contains("Marcar como terminada", page.Markup));
        page.Find("button").Click();

        page.WaitForAssertion(() =>
        {
            Assert.True(marked);
            Assert.Contains("pendiente de respuesta", page.Markup);
            Assert.Contains("La persona solicitante puede responder hasta", page.Markup);
            Assert.Contains("2026-10-25T15:20:00", page.Markup);
            Assert.Contains("no procesa ni verifica pagos", page.Markup);
            Assert.DoesNotContain("Direcci\u00f3n", page.Markup);
        });
    }

    [Fact]
    public void Completion_page_confirms_or_rejects_without_claiming_closure_or_administrative_resolution()
    {
        var id = Guid.Parse("a1000000-0000-4000-8000-000000000001");
        var handler = new RecordingHandler(async request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return Json("""{"changaId":"a1000000-0000-4000-8000-000000000001","description":"Organizar una changa ficticia.","role":"requester","status":"pending_response","markedDoneAt":"2026-10-25T15:00:00Z","responseDeadline":"2026-10-25T15:20:00Z","reviewCause":null}""");
            }
            Assert.EndsWith("/completion/reject", request.RequestUri!.AbsolutePath, StringComparison.Ordinal);
            return Json("""{"changaId":"a1000000-0000-4000-8000-000000000001","description":"Organizar una changa ficticia.","role":"requester","status":"completion_review","markedDoneAt":"2026-10-25T15:00:00Z","responseDeadline":"2026-10-25T15:20:00Z","reviewCause":"rejected"}""");
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var page = Render<ChangaQuest.Client.Pages.Finalizacion>(parameters => parameters.Add(component => component.Id, id));
        page.WaitForAssertion(() => Assert.Contains("Rechazar y dejar pendiente de", page.Markup));
        page.Find("button.button.secondary").Click();

        page.WaitForAssertion(() =>
        {
            Assert.Contains("La changa", page.Markup);
            Assert.Contains("no se reasigna", page.Markup);
            Assert.DoesNotContain("administrativa", page.Markup);
        });
    }

    [Fact]
    public void Completion_page_confirm_button_posts_confirm_and_shows_confirmed_state()
    {
        var id = Guid.Parse("a1000000-0000-4000-8000-000000000001");
        var handler = new RecordingHandler(async request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return Json("""{"changaId":"a1000000-0000-4000-8000-000000000001","description":"Organizar una changa ficticia.","role":"requester","status":"pending_response","markedDoneAt":"2026-10-25T15:00:00Z","responseDeadline":"2026-10-25T15:20:00Z","reviewCause":null}""");
            }
            Assert.EndsWith("/completion/confirm", request.RequestUri!.AbsolutePath, StringComparison.Ordinal);
            return Json("""{"changaId":"a1000000-0000-4000-8000-000000000001","description":"Organizar una changa ficticia.","role":"requester","status":"confirmed","markedDoneAt":"2026-10-25T15:00:00Z","responseDeadline":"2026-10-25T15:20:00Z","reviewCause":null}""");
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var page = Render<ChangaQuest.Client.Pages.Finalizacion>(parameters => parameters.Add(component => component.Id, id));
        page.WaitForAssertion(() => Assert.Contains("Confirmar", page.Markup));
        page.Find("button.button:not(.secondary)").Click();

        page.WaitForAssertion(() => Assert.Contains("confirmada", page.Markup));
    }

    [Fact]
    public void Rating_page_hides_submission_until_normal_close()
    {
        Services.AddSingleton(new HttpClient(new RecordingHandler(_ =>
            Task.FromResult(Json("""{"changaId":"a1000000-0000-4000-8000-000000000001","canRate":false,"hasRated":false}"""))))
        {
            BaseAddress = new Uri("http://localhost")
        });

        var page = Render<ChangaQuest.Client.Pages.Calificacion>(parameters =>
            parameters.Add(component => component.Id, CoordinationId));

        page.WaitForAssertion(() => Assert.Contains("solo después del cierre normal", page.Markup));
        Assert.DoesNotContain("<form", page.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Rating_page_reports_when_a_rating_was_already_submitted()
    {
        Services.AddSingleton(new HttpClient(new RecordingHandler(_ =>
            Task.FromResult(Json("""{"changaId":"a1000000-0000-4000-8000-000000000001","canRate":false,"hasRated":true}"""))))
        {
            BaseAddress = new Uri("http://localhost")
        });

        var page = Render<ChangaQuest.Client.Pages.Calificacion>(parameters =>
            parameters.Add(component => component.Id, CoordinationId));

        page.WaitForAssertion(() => Assert.Contains("Ya enviaste tu calificación", page.Markup));
        Assert.DoesNotContain("<form", page.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Rating_page_shows_load_errors_and_can_retry()
    {
        var requests = 0;
        Services.AddSingleton(new HttpClient(new RecordingHandler(_ =>
        {
            requests++;
            return Task.FromResult(requests == 1
                ? Problem(HttpStatusCode.InternalServerError, "No se pudo cargar.")
                : Json("""{"changaId":"a1000000-0000-4000-8000-000000000001","canRate":true,"hasRated":false}"""));
        }))
        {
            BaseAddress = new Uri("http://localhost")
        });

        var page = Render<ChangaQuest.Client.Pages.Calificacion>(parameters =>
            parameters.Add(component => component.Id, CoordinationId));

        page.WaitForAssertion(() => Assert.Contains("No se pudo cargar.", page.Markup));
        page.Find("button.secondary").Click();
        page.WaitForAssertion(() => Assert.Contains("Elegí una nota", page.Markup));
    }

    [Fact]
    public void Rating_page_shows_submission_errors_without_discarding_the_selected_score()
    {
        Services.AddSingleton(new HttpClient(new RecordingHandler(request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return Task.FromResult(Json(
                    """{"changaId":"a1000000-0000-4000-8000-000000000001","canRate":true,"hasRated":false}"""));
            }

            return Task.FromResult(Problem(HttpStatusCode.ServiceUnavailable, "No se pudo guardar."));
        }))
        {
            BaseAddress = new Uri("http://localhost")
        });

        var page = Render<ChangaQuest.Client.Pages.Calificacion>(parameters =>
            parameters.Add(component => component.Id, CoordinationId));
        page.WaitForElement("#score-4");
        page.Find("#score-4").Change("4");
        page.Find("form").Submit();

        page.WaitForAssertion(() => Assert.Contains("No se pudo guardar.", page.Markup));
        page.Find("button.secondary").Click();
        page.WaitForElement("#score-4");
        Assert.Contains("checked", page.Find("#score-4").OuterHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Rating_page_posts_an_integer_score_and_reports_success()
    {
        string? submittedBody = null;
        Services.AddSingleton(new HttpClient(new RecordingHandler(async request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return Json("""{"changaId":"a1000000-0000-4000-8000-000000000001","canRate":true,"hasRated":false}""");
            }

            submittedBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(
                    """{"changaId":"a1000000-0000-4000-8000-000000000001","score":5}""",
                    Encoding.UTF8,
                    "application/json")
            };
        })) { BaseAddress = new Uri("http://localhost") });

        var page = Render<ChangaQuest.Client.Pages.Calificacion>(parameters =>
            parameters.Add(component => component.Id, CoordinationId));
        page.WaitForAssertion(() => Assert.Contains("Elegí una nota", page.Markup));
        page.Find("#score-5").Change("5");
        page.Find("form").Submit();

        page.WaitForAssertion(() =>
        {
            using var body = System.Text.Json.JsonDocument.Parse(submittedBody!);
            Assert.Equal(5, body.RootElement.GetProperty("score").GetInt32());
            Assert.Contains("Calificación guardada", page.Markup);
        });
    }

    [Fact]
    public void Rating_page_reloads_and_discards_stale_eligibility_when_route_id_changes()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var firstResponse = new TaskCompletionSource<HttpResponseMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Services.AddSingleton(new HttpClient(new RecordingHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith(firstId.ToString(), StringComparison.Ordinal))
            {
                return firstResponse.Task;
            }

            return Task.FromResult(Json(
                $$"""{"changaId":"{{secondId}}","canRate":true,"hasRated":false}"""));
        }))
        {
            BaseAddress = new Uri("http://localhost")
        });

        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo($"http://localhost/tareas/{firstId}/calificacion");
        var app = Render<App>();
        navigation.NavigateTo($"http://localhost/tareas/{secondId}/calificacion");

        app.WaitForAssertion(() => Assert.Contains("Elegí una nota", app.Markup));
        firstResponse.SetResult(Json(
            $$"""{"changaId":"{{firstId}}","canRate":false,"hasRated":false}"""));
        app.WaitForAssertion(() => Assert.Contains("Elegí una nota", app.Markup));
        Assert.DoesNotContain("solo después del cierre normal", app.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Profile_page_shows_empty_state_without_fabricating_average()
    {
        Services.AddSingleton(new HttpClient(new RecordingHandler(_ =>
            Task.FromResult(Json("""{"averageRating":null,"ratingCount":0}"""))))
        {
            BaseAddress = new Uri("http://localhost")
        });

        var page = Render<ChangaQuest.Client.Pages.Perfil>();

        page.WaitForAssertion(() => Assert.Contains("Todavía no hay calificaciones", page.Markup));
        Assert.DoesNotContain("Promedio:", page.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Profile_page_shows_only_average_and_rating_count()
    {
        Services.AddSingleton(new HttpClient(new RecordingHandler(_ =>
            Task.FromResult(Json("""{"averageRating":4.5,"ratingCount":2}"""))))
        {
            BaseAddress = new Uri("http://localhost")
        });

        var page = Render<ChangaQuest.Client.Pages.Perfil>();

        page.WaitForAssertion(() =>
        {
            Assert.Contains("Promedio:", page.Markup);
            Assert.Contains("4,5", page.Markup);
            Assert.Contains("Cantidad:", page.Markup);
            Assert.Contains(">2<", page.Markup);
            Assert.DoesNotContain("evaluador", page.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Profile_page_does_not_replace_current_profile_with_a_stale_response()
    {
        const string firstAccountId = "account-a";
        const string secondAccountId = "account-b";
        var firstResponse = new TaskCompletionSource<HttpResponseMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Services.AddSingleton(new HttpClient(new RecordingHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith(firstAccountId, StringComparison.Ordinal))
            {
                return firstResponse.Task;
            }

            return Task.FromResult(Json("""{"averageRating":2,"ratingCount":1}"""));
        }))
        {
            BaseAddress = new Uri("http://localhost")
        });

        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo($"http://localhost/perfil/{firstAccountId}");
        var app = Render<App>();
        navigation.NavigateTo($"http://localhost/perfil/{secondAccountId}");

        app.WaitForAssertion(() => Assert.Contains("2,0", app.Markup));
        firstResponse.SetResult(Json("""{"averageRating":4,"ratingCount":1}"""));
        app.WaitForAssertion(() => Assert.Contains("2,0", app.Markup));
        Assert.DoesNotContain("4,0", app.Markup, StringComparison.Ordinal);
    }
    private const string PendingCoordination = """{"changaId":"a1000000-0000-4000-8000-000000000001","description":"Organizar una changa ficticia.","role":"worker","yourConsent":"pending","otherPartyConsent":"accepted","hasMutualConsent":false,"otherPhone":null,"exactAddress":"Dirección ficticia 123"}""";
    private const string MutualCoordination = """{"changaId":"a1000000-0000-4000-8000-000000000001","description":"Organizar una changa ficticia.","role":"worker","yourConsent":"accepted","otherPartyConsent":"accepted","hasMutualConsent":true,"otherPhone":"+54 351 555-0199","exactAddress":"Dirección ficticia 123"}""";
    private const string TimeoutMessage = "La solicitud tardó demasiado. Intentá de nuevo.";

    private static readonly Guid CoordinationId = Guid.Parse("a1000000-0000-4000-8000-000000000001");

    private IRenderedComponent<ChangaQuest.Client.Pages.Coordinacion> RenderCoordination(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> respond)
    {
        Services.AddSingleton(new HttpClient(new RecordingHandler(respond)) { BaseAddress = new Uri("http://localhost") });
        return Render<ChangaQuest.Client.Pages.Coordinacion>(parameters => parameters.Add(component => component.Id, CoordinationId));
    }

    [Fact]
    public void Coordination_page_shows_timeout_message_when_loading_is_cancelled()
    {
        var page = RenderCoordination(_ => throw new TaskCanceledException());

        page.WaitForAssertion(() =>
        {
            Assert.Contains(TimeoutMessage, page.Markup);
            Assert.Contains("Reintentar", page.Markup);
        });
    }

    [Fact]
    public void Coordinations_list_shows_timeout_message_when_loading_is_cancelled()
    {
        Services.AddSingleton(new HttpClient(new RecordingHandler(_ => throw new TaskCanceledException())) { BaseAddress = new Uri("http://localhost") });

        var page = Render<ChangaQuest.Client.Pages.Coordinaciones>();

        page.WaitForAssertion(() => Assert.Contains("La solicitud de coordinaciones tardó demasiado", page.Markup));
    }

    [Fact]
    public void My_publications_require_explicit_confirmation_and_report_cancelled_status()
    {
        var id = "a1000000-0000-4000-8000-000000000001";
        var cancelRequests = 0;
        var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath == "/api/v1/coordinations")
            {
                return Task.FromResult(Json("[]"));
            }

            if (request.RequestUri?.AbsolutePath == "/api/v1/changas/mine")
            {
                return Task.FromResult(Json(
                    $$"""[{"id":"{{id}}","description":"Organizar una changa ficticia.","categoryId":"organizacion-hogar","reward":1000,"zone":"Centro","materialsNeeded":false,"materialsProvider":null,"status":"available","reviewReason":null,"assignmentMode":"first_come","isSample":false,"createdAt":"2026-10-25T15:00:00Z","distanceKm":0}]"""));
            }

            if (request.RequestUri?.AbsolutePath == $"/api/v1/changas/{id}/cancel" && request.Method == HttpMethod.Post)
            {
                cancelRequests++;
                return Task.FromResult(Json(
                    $$"""{"id":"{{id}}","description":"Organizar una changa ficticia.","categoryId":"organizacion-hogar","reward":1000,"zone":"Centro","materialsNeeded":false,"materialsProvider":null,"status":"cancelled","reviewReason":null,"assignmentMode":"first_come","isSample":false,"createdAt":"2026-10-25T15:00:00Z","distanceKm":0}"""));
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var page = Render<ChangaQuest.Client.Pages.Coordinaciones>();
        page.WaitForAssertion(() => Assert.Contains("Publicadas por mí", page.Markup));
        Assert.Contains("Zona: Centro", page.Markup);
        Assert.Contains("Recompensa:", page.Markup);
        Assert.Contains("aria-label=\"Cancelar publicación: Organizar una changa ficticia.\"", page.Markup);
        Assert.DoesNotContain("Dirección privada", page.Markup);
        page.FindAll("button").First(button => button.TextContent == "Cancelar publicación").Click();
        page.WaitForAssertion(() => Assert.Contains("role=\"group\"", page.Markup));
        Assert.Equal(0, cancelRequests);

        page.Find("button[aria-label='Volver sin cancelar']").Click();
        Assert.Equal(0, cancelRequests);
        Assert.DoesNotContain("role=\"group\"", page.Markup);

        page.FindAll("button").Single(button => button.TextContent == "Cancelar publicación").Click();
        page.Find("button[aria-label='Confirmar cancelación']").Click();
        page.WaitForAssertion(() => Assert.Contains("Cancelada", page.Markup));
        Assert.Equal(1, cancelRequests);
        Assert.Empty(page.FindAll("button").Where(button => button.TextContent == "Cancelar publicación"));
        Assert.Contains(page.FindAll("p[role='status']"), element => element.TextContent.Contains("Cancelada", StringComparison.Ordinal));
    }

    [Fact]
    public void Cancellation_http_failure_does_not_announce_success_and_refreshes_actual_state()
    {
        var id = "a1000000-0000-4000-8000-000000000001";
        var mineRequests = 0;
        var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath == "/api/v1/coordinations")
            {
                return Task.FromResult(Json("[]"));
            }

            if (request.RequestUri?.AbsolutePath == "/api/v1/changas/mine")
            {
                mineRequests++;
                return Task.FromResult(Json(
                    $$"""[{"id":"{{id}}","description":"Organizar una changa ficticia.","categoryId":"organizacion-hogar","reward":1000,"zone":"Centro","materialsNeeded":false,"materialsProvider":null,"status":"available","reviewReason":null,"assignmentMode":"first_come","isSample":false,"createdAt":"2026-10-25T15:00:00Z","distanceKm":0}]"""));
            }

            if (request.RequestUri?.AbsolutePath == $"/api/v1/changas/{id}/cancel")
            {
                return Task.FromResult(Problem(HttpStatusCode.Conflict, "La changa ya no está disponible."));
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var page = Render<ChangaQuest.Client.Pages.Coordinaciones>();
        page.WaitForAssertion(() => Assert.Contains("Publicadas por mí", page.Markup));
        page.FindAll("button").Single(button => button.TextContent == "Cancelar publicación").Click();
        page.Find("button[aria-label='Confirmar cancelación']").Click();

        page.WaitForAssertion(() => Assert.Contains("No se pudo cancelar la publicación", page.Markup));
        Assert.DoesNotContain("Cancelada", page.Markup);
        Assert.True(mineRequests >= 2);
        page.FindAll("button")
            .Single(button => button.TextContent.Contains("Actualizar publicaciones", StringComparison.Ordinal))
            .Click();
        page.WaitForAssertion(() => Assert.DoesNotContain("No se pudo cancelar la publicación", page.Markup));
    }

    [Fact]
    public void Coordination_load_failure_does_not_hide_own_publications()
    {
        var id = "a1000000-0000-4000-8000-000000000001";
        var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath == "/api/v1/coordinations")
            {
                return Task.FromResult(Problem(HttpStatusCode.ServiceUnavailable, "No disponible."));
            }

            if (request.RequestUri?.AbsolutePath == "/api/v1/changas/mine")
            {
                return Task.FromResult(Json(
                    $$"""[{"id":"{{id}}","description":"Organizar una changa ficticia.","categoryId":"organizacion-hogar","reward":1000,"zone":"Centro","materialsNeeded":false,"materialsProvider":null,"status":"available","reviewReason":null,"assignmentMode":"first_come","isSample":false,"createdAt":"2026-10-25T15:00:00Z","distanceKm":0}]"""));
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var page = Render<ChangaQuest.Client.Pages.Coordinaciones>();

        page.WaitForAssertion(() =>
        {
            Assert.Contains("Publicadas por mí", page.Markup);
            Assert.Contains("Organizar una changa ficticia.", page.Markup);
        });
    }

    [Fact]
    public void Publication_load_failure_does_not_hide_coordinations()
    {
        var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath == "/api/v1/coordinations")
            {
                return Task.FromResult(Json(
                    """[{"changaId":"a1000000-0000-4000-8000-000000000001","description":"Coordinar trabajo pendiente.","role":"worker","yourConsent":"pending","otherPartyConsent":"pending","hasMutualConsent":false,"otherPhone":null}]"""));
            }

            if (request.RequestUri?.AbsolutePath == "/api/v1/changas/mine")
            {
                return Task.FromResult(Problem(HttpStatusCode.ServiceUnavailable, "No disponible."));
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var page = Render<ChangaQuest.Client.Pages.Coordinaciones>();

        page.WaitForAssertion(() =>
        {
            Assert.Contains("Coordinar trabajo pendiente.", page.Markup);
            Assert.Contains("No se pudieron cargar tus publicaciones.", page.Markup);
        });
    }

    [Fact]
    public async Task Other_publication_cannot_replace_confirmation_while_cancellation_is_pending()
    {
        const string firstId = "a1000000-0000-4000-8000-000000000001";
        const string secondId = "a1000000-0000-4000-8000-000000000002";
        var cancelResponse = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath == "/api/v1/coordinations")
            {
                return Task.FromResult(Json("[]"));
            }

            if (request.RequestUri?.AbsolutePath == "/api/v1/changas/mine")
            {
                return Task.FromResult(Json(
                    $$"""[{"id":"{{firstId}}","description":"Primera publicación","categoryId":"organizacion-hogar","reward":1000,"zone":"Centro","materialsNeeded":false,"materialsProvider":null,"status":"available","reviewReason":null,"assignmentMode":"first_come","isSample":false,"createdAt":"2026-10-25T15:00:00Z","distanceKm":0},{"id":"{{secondId}}","description":"Segunda publicación","categoryId":"organizacion-hogar","reward":1000,"zone":"Centro","materialsNeeded":false,"materialsProvider":null,"status":"available","reviewReason":null,"assignmentMode":"first_come","isSample":false,"createdAt":"2026-10-25T15:00:00Z","distanceKm":0}]"""));
            }

            if (request.RequestUri?.AbsolutePath == $"/api/v1/changas/{firstId}/cancel")
            {
                return cancelResponse.Task;
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var page = Render<ChangaQuest.Client.Pages.Coordinaciones>();
        page.WaitForAssertion(() => Assert.Contains("Primera publicación", page.Markup));
        page.FindAll("button").First(button => button.TextContent == "Cancelar publicación").Click();
        var confirmTask = page.Find("button[aria-label='Confirmar cancelación']").ClickAsync();

        page.WaitForAssertion(() =>
        {
            Assert.Single(page.FindAll("[role='group']"));
            Assert.All(
                page.FindAll("button").Where(button => button.TextContent == "Cancelar publicación"),
                button => Assert.True(button.HasAttribute("disabled")));
        });

        cancelResponse.SetResult(Json(
            $$"""{"id":"{{firstId}}","description":"Primera publicación","categoryId":"organizacion-hogar","reward":1000,"zone":"Centro","materialsNeeded":false,"materialsProvider":null,"status":"cancelled","reviewReason":null,"assignmentMode":"first_come","isSample":false,"createdAt":"2026-10-25T15:00:00Z","distanceKm":0}"""));
        await confirmTask;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cancellation_network_or_timeout_failure_is_reported_without_false_success(bool timeout)
    {
        var id = "a1000000-0000-4000-8000-000000000001";
        var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath == "/api/v1/coordinations")
            {
                return Task.FromResult(Json("[]"));
            }

            if (request.RequestUri?.AbsolutePath == "/api/v1/changas/mine")
            {
                return Task.FromResult(Json(
                    $$"""[{"id":"{{id}}","description":"Organizar una changa ficticia.","categoryId":"organizacion-hogar","reward":1000,"zone":"Centro","materialsNeeded":false,"materialsProvider":null,"status":"available","reviewReason":null,"assignmentMode":"first_come","isSample":false,"createdAt":"2026-10-25T15:00:00Z","distanceKm":0}]"""));
            }

            if (request.RequestUri?.AbsolutePath == $"/api/v1/changas/{id}/cancel")
            {
                return Task.FromException<HttpResponseMessage>(
                    timeout ? new OperationCanceledException() : new HttpRequestException());
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var page = Render<ChangaQuest.Client.Pages.Coordinaciones>();
        page.WaitForAssertion(() => Assert.Contains("Publicadas por mí", page.Markup));
        page.FindAll("button").Single(button => button.TextContent == "Cancelar publicación").Click();
        page.Find("button[aria-label='Confirmar cancelación']").Click();

        var message = timeout ? "La solicitud tardó demasiado" : "No se pudo conectar con el servidor";
        page.WaitForAssertion(() => Assert.Contains(message, page.Markup));
        Assert.DoesNotContain("Cancelada", page.Markup);
    }


    [Fact]
    public void Cancellation_reports_when_refresh_also_fails()
    {
        var id = "a1000000-0000-4000-8000-000000000001";
        var mineRequests = 0;
        var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath == "/api/v1/coordinations")
            {
                return Task.FromResult(Json("[]"));
            }

            if (request.RequestUri?.AbsolutePath == "/api/v1/changas/mine")
            {
                mineRequests++;
                if (mineRequests > 1)
                {
                    return Task.FromResult(Problem(HttpStatusCode.ServiceUnavailable, "No disponible."));
                }

                return Task.FromResult(Json(
                    $$"""[{"id":"{{id}}","description":"Organizar una changa ficticia.","categoryId":"organizacion-hogar","reward":1000,"zone":"Centro","materialsNeeded":false,"materialsProvider":null,"status":"available","reviewReason":null,"assignmentMode":"first_come","isSample":false,"createdAt":"2026-10-25T15:00:00Z","distanceKm":0}]"""));
            }

            if (request.RequestUri?.AbsolutePath == $"/api/v1/changas/{id}/cancel")
            {
                return Task.FromResult(Problem(HttpStatusCode.Conflict, "La changa ya no está disponible."));
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var page = Render<ChangaQuest.Client.Pages.Coordinaciones>();
        page.WaitForAssertion(() => Assert.Contains("Publicadas por mí", page.Markup));
        page.FindAll("button").Single(button => button.TextContent == "Cancelar publicación").Click();
        page.Find("button[aria-label='Confirmar cancelación']").Click();

        page.WaitForAssertion(() => Assert.Contains("No se pudo actualizar el estado", page.Markup));
        Assert.Contains("Actualizar publicaciones", page.Markup);
    }

    [Fact]
    public void Coordination_page_shows_timeout_message_without_false_success_when_saving_is_cancelled()
    {
        var page = RenderCoordination(request => request.Method == HttpMethod.Get
            ? Task.FromResult(Json(PendingCoordination))
            : throw new TaskCanceledException());
        page.WaitForAssertion(() => Assert.Contains("label for=\"phone\"", page.Markup));

        page.Find("input#phone").Change("12345678");
        page.Find("button.button:not(.secondary)").Click();

        page.WaitForAssertion(() =>
        {
            Assert.Contains(TimeoutMessage, page.Markup);
            Assert.DoesNotContain("Guardamos tu", page.Markup);
        });
    }

    [Fact]
    public void Coordination_page_treats_null_save_response_as_error_and_keeps_the_form()
    {
        var page = RenderCoordination(request => Task.FromResult(request.Method == HttpMethod.Get
            ? Json(PendingCoordination)
            : Json("null")));
        page.WaitForAssertion(() => Assert.Contains("label for=\"phone\"", page.Markup));

        page.Find("input#phone").Change("12345678");
        page.Find("button.button:not(.secondary)").Click();

        page.WaitForAssertion(() =>
        {
            Assert.Contains("role=\"alert\"", page.Markup);
            Assert.DoesNotContain("Guardamos tu", page.Markup);
            Assert.Contains("label for=\"phone\"", page.Markup);
            Assert.Contains("Dirección ficticia 123", page.Markup);
        });
    }

    [Fact]
    public void Coordination_page_uses_a_fallback_message_when_phone_errors_array_is_empty()
    {
        var page = RenderCoordination(request => Task.FromResult(request.Method == HttpMethod.Get
            ? Json(PendingCoordination)
            : Problem(HttpStatusCode.BadRequest, "Datos inválidos.", """{"phone":[]}""")));
        page.WaitForAssertion(() => Assert.Contains("label for=\"phone\"", page.Markup));

        page.Find("input#phone").Change("123");
        page.Find("button.button:not(.secondary)").Click();

        page.WaitForAssertion(() =>
        {
            Assert.Contains("id=\"phone-error\"", page.Markup);
            Assert.Contains("Ingresá un teléfono ficticio válido", page.Markup);
        });
    }

    [Fact]
    public void Coordination_page_sends_share_phone_true_with_the_phone_when_accepting()
    {
        string? submitted = null;
        var page = RenderCoordination(async request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return Json(PendingCoordination);
            }

            submitted = await request.Content!.ReadAsStringAsync();
            return Json(MutualCoordination);
        });
        page.WaitForAssertion(() => Assert.Contains("label for=\"phone\"", page.Markup));

        page.Find("input#phone").Change("+54 351 555-0100");
        page.Find("button.button:not(.secondary)").Click();

        page.WaitForAssertion(() =>
        {
            using var body = System.Text.Json.JsonDocument.Parse(submitted!);
            Assert.True(body.RootElement.GetProperty("sharePhone").GetBoolean());
            Assert.Equal("+54 351 555-0100", body.RootElement.GetProperty("phone").GetString());
        });
    }

    [Fact]
    public void Coordination_page_shows_the_other_phone_with_mutual_consent()
    {
        var page = RenderCoordination(_ => Task.FromResult(Json(MutualCoordination)));

        page.WaitForAssertion(() => Assert.Contains("+54 351 555-0199", page.Markup));
    }

    [Fact]
    public void Coordinations_list_shows_the_other_phone_with_mutual_consent()
    {
        Services.AddSingleton(new HttpClient(new RecordingHandler(_ => Task.FromResult(Json($"[{MutualCoordination}]")))) { BaseAddress = new Uri("http://localhost") });

        var page = Render<ChangaQuest.Client.Pages.Coordinaciones>();

        page.WaitForAssertion(() => Assert.Contains("+54 351 555-0199", page.Markup));
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
