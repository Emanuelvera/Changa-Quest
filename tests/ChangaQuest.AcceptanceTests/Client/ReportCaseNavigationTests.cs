using System.Net;
using System.Text;
using Bunit;
using ChangaQuest.Client.Layout;
using ChangaQuest.Client.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace ChangaQuest.AcceptanceTests.Client;

public sealed class ReportCaseNavigationTests : BunitContext
{
    [Fact]
    public void Coordinations_link_to_the_restricted_case_index()
    {
        Services.AddSingleton(new HttpClient(new EmptyListHandler())
        {
            BaseAddress = new Uri("http://localhost")
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/coordinaciones");

        var page = Render<Coordinaciones>();

        page.WaitForAssertion(() =>
            Assert.Contains("href=\"/reportes\"", page.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void Main_navigation_exposes_the_authorized_case_index()
    {
        var page = Render<MainLayout>();

        Assert.Contains("href=\"/reportes\"", page.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Case_index_shows_authorized_cases_without_private_reason_or_evidence()
    {
        Services.AddSingleton(new HttpClient(new CaseListHandler())
        {
            BaseAddress = new Uri("http://localhost")
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/reportes");

        var page = Render<ReportCaseIndex>();

        page.WaitForAssertion(() =>
        {
            Assert.Contains("Changa asignada", page.Markup, StringComparison.Ordinal);
            Assert.Contains("href=\"/reportes/78f84be8-2d21-4c69-aa73-75e1044101df\"", page.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Motivo privado", page.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Evidencia privada", page.Markup, StringComparison.Ordinal);
        });
    }

    private sealed class EmptyListHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });
    }

    private sealed class CaseListHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """[{"id":"78f84be8-2d21-4c69-aa73-75e1044101df","changaId":"b57abeb0-4b81-4abc-801e-c70f5e31fbd4","changaDescription":"Changa asignada","status":"pending","createdAt":"2026-10-25T10:00:00Z"}]""",
                    Encoding.UTF8,
                    "application/json")
            });
    }
}
