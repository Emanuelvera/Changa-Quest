using System.Text.RegularExpressions;

namespace ChangaQuest.AcceptanceTests.Client;

public sealed class VisualStatusStyleTests
{
    [Fact]
    public void Informational_statuses_use_neutral_colors_instead_of_success_colors()
    {
        var cssPath = Path.Combine(AppContext.BaseDirectory, "ClientVisual", "app.css");
        var css = File.ReadAllText(cssPath);
        var statusRules = Regex.Matches(css, @"(?m)^\.status\s*\{(?<declarations>[^}]*)\}");
        var successRules = Regex.Matches(css, @"(?m)^\.success-message\s*\{(?<declarations>[^}]*)\}");

        Assert.NotEmpty(statusRules);
        Assert.NotEmpty(successRules);

        var statusDeclarations = statusRules[^1].Groups["declarations"].Value;
        var successDeclarations = successRules[^1].Groups["declarations"].Value;

        Assert.Contains("background: #1a3040", statusDeclarations, StringComparison.Ordinal);
        Assert.Contains("background: #1d382f", successDeclarations, StringComparison.Ordinal);
        Assert.DoesNotContain(".success-message,\n.status", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".success-message,\r\n.status", css, StringComparison.Ordinal);
    }
}
