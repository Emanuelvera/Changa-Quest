using System.Net;
using System.Text;
using Bunit;
using ChangaQuest.Client.Pages;
using Microsoft.AspNetCore.Components;

namespace ChangaQuest.AcceptanceTests.Client;

public class HomePageTests : Bunit.TestContext
{
    [Fact]
    public async Task Registration_form_posts_all_fields_and_displays_success()
    {
        string? registrationBody = null;
        var handler = new RecordingHandler(async request =>
        {
            if (request.RequestUri?.AbsolutePath == "/api/v1/accounts/me")
            {
                return Problem(HttpStatusCode.Unauthorized, "Iniciá sesión.");
            }

            if (request.RequestUri?.AbsolutePath == "/api/v1/accounts/register")
            {
                registrationBody = await request.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.Accepted)
                {
                    Content = new StringContent(
                        """{"message":"Cuenta creada."}""",
                        Encoding.UTF8,
                        "application/json")
                };
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var page = RenderComponent<Home>();
        page.Find(".tabs button:nth-child(2)").Click();
        page.Find("#register-email").Change("persona@example.test");
        page.Find("#register-password").Change("abcdefghijkl");
        page.Find("#declared-adult").Change(true);

        page.Find("form").Submit();

        page.WaitForAssertion(() =>
        {
            Assert.Contains("Cuenta creada.", page.Markup);
            Assert.Contains("label for=\"register-email\"", page.Markup);
            Assert.Contains("aria-describedby=\"register-email-validation register-email-error\"", page.Markup);
            Assert.Contains("\"email\":\"persona@example.test\"", registrationBody);
            Assert.Contains("\"password\":\"abcdefghijkl\"", registrationBody);
            Assert.Contains("\"declaredAdult\":true", registrationBody);
        });
    }

    [Fact]
    public async Task Login_errors_are_announced_and_associated_with_labeled_fields()
    {
        var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath == "/api/v1/accounts/me")
            {
                return Task.FromResult(Problem(HttpStatusCode.Unauthorized, "Iniciá sesión."));
            }

            if (request.RequestUri?.AbsolutePath == "/api/v1/accounts/login")
            {
                return Task.FromResult(Problem(HttpStatusCode.Unauthorized, "Revisá el correo y la contraseña."));
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var page = RenderComponent<Home>();
        page.Find("#login-email").Change("persona@example.test");
        page.Find("#login-password").Change("incorrecta");

        page.Find("form").Submit();

        page.WaitForAssertion(() =>
        {
            Assert.Contains("label for=\"login-email\"", page.Markup);
            Assert.Contains("aria-describedby=\"login-email-validation\"", page.Markup);
            Assert.Contains("role=\"alert\"", page.Markup);
            Assert.Contains("Revisá el correo y la contraseña.", page.Markup);
        });
    }

    private static HttpResponseMessage Problem(HttpStatusCode statusCode, string detail) =>
        new(statusCode)
        {
            Content = new StringContent(
                $$"""{"type":"about:blank","status":{{(int)statusCode}},"title":"Error","detail":"{{detail}}"}""",
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
