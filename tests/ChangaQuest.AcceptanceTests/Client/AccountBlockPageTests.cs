using System.Net;
using System.Text;
using Bunit;
using ChangaQuest.Client.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace ChangaQuest.AcceptanceTests.Client;

public sealed class AccountBlockPageTests : Bunit.TestContext
{
    [Fact]
    public void Profile_requires_explicit_confirmation_and_cancel_does_not_send_request()
    {
        var posts = 0;
        var handler = new RecordingHandler(request =>
        {
            if (request.Method == HttpMethod.Get)
                return Task.FromResult(Json("""{"averageRating":null,"ratingCount":0}"""));
            posts++;
            return Task.FromResult(Json("""{"status":"account_blocked","detail":"Este bloqueo no cancela ni resuelve una changa ya asignada."}"""));
        });
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var page = Render<Perfil>(parameters =>
            parameters.Add(component => component.AccountId, "target-account"));
        page.WaitForAssertion(() => Assert.Contains("Bloquear cuenta", page.Markup));
        page.Find("button.secondary").Click();

        Assert.Contains("¿Bloquear esta cuenta?", page.Markup);
        page.FindAll("button").First(button => button.TextContent == "Cancelar").Click();
        Assert.Equal(0, posts);

        page.Find("button.secondary").Click();
        page.Find("button").Click();
        page.WaitForAssertion(() =>
        {
            Assert.Equal(1, posts);
            Assert.Contains("no cancela ni resuelve", page.Markup);
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
