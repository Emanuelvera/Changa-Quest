using System.Net;
using System.Text;
using Bunit;
using ChangaQuest.Client.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace ChangaQuest.AcceptanceTests.Client;

public sealed class ReportReviewPageTests : Bunit.TestContext
{
    [Fact]
    public void Party_case_shows_only_participant_actions_without_private_evidence_or_deadlines()
    {
        var caseRequested = false;
        var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath.EndsWith("/case", StringComparison.Ordinal) == true)
            {
                caseRequested = true;
                return Task.FromResult(Json(
                    """{"id":"78f84be8-2d21-4c69-aa73-75e1044101df","changaId":"b57abeb0-4b81-4abc-801e-c70f5e31fbd4","status":"pending","canRespond":true,"canAppeal":false,"myResponses":[],"decision":null,"myAppeals":[]}"""));
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        Services.GetRequiredService<NavigationManager>().NavigateTo(
            "http://localhost/reportes/78f84be8-2d21-4c69-aa73-75e1044101df");

        var page = Render<ReportReview>();

        page.WaitForAssertion(() =>
        {
            Assert.True(caseRequested);
            Assert.Contains("La revisión está en curso.", page.Markup, StringComparison.Ordinal);
            Assert.Contains("case-response", page.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Evidencia privada", page.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("vencimiento", page.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Administrative_case_shows_party_roles_and_event_timestamps()
    {
        var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath.EndsWith("/case", StringComparison.Ordinal) == true)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden));
            }

            return Task.FromResult(Json(
                """{"id":"78f84be8-2d21-4c69-aa73-75e1044101df","changaId":"b57abeb0-4b81-4abc-801e-c70f5e31fbd4","reporterId":"reporter","reason":"Motivo","evidence":"Evidencia","status":"pending","createdAt":"2026-10-25T10:00:00Z","responses":[{"id":"0f8715e2-999b-44cf-a367-24b1334f27f6","authorId":"author","authorRole":"requester","text":"Respuesta","createdAt":"2026-10-25T10:01:00Z"}],"decision":null,"appeals":[{"id":"ef36a524-0e91-4bd3-93bf-6c094845aedc","appellantId":"worker","appellantRole":"worker","text":"Apelación","createdAt":"2026-10-25T10:02:00Z","status":"pending","resolution":null,"resolverId":null,"resolvedAt":null}]}"""));
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        Services.GetRequiredService<NavigationManager>().NavigateTo(
            "http://localhost/reportes/78f84be8-2d21-4c69-aa73-75e1044101df");

        var page = Render<ReportReview>();

        page.WaitForAssertion(() =>
        {
            Assert.Contains("Persona solicitante", page.Markup, StringComparison.Ordinal);
            Assert.Contains("Persona realizadora", page.Markup, StringComparison.Ordinal);
            Assert.Contains("2026", page.Markup, StringComparison.Ordinal);
        });
    }

    private static HttpResponseMessage Json(string content) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(content, Encoding.UTF8, "application/json")
    };

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) => respond(request);
    }
}
