using System.Text.RegularExpressions;

namespace ChangaQuest.AcceptanceTests.Client;

public sealed class VisualStyleContractTests
{
    [Fact]
    public void Mobile_navigation_remains_fixed_to_the_bottom_below_the_mobile_breakpoint()
    {
        var css = ReadStylesheet();
        var mobileRules = ExtractBlock(css, "@media (max-width: 40rem)");
        var navigationRules = ExtractBlock(mobileRules, ".primary-nav");

        Assert.Equal("fixed", ReadDeclaration(navigationRules, "position"));
        Assert.Equal("0", ReadDeclaration(navigationRules, "bottom"));
    }

    [Fact]
    public void Semantic_status_text_meets_normal_text_contrast_on_its_surface()
    {
        var css = ReadStylesheet();
        var rootTokens = ExtractBlock(css, ":root");

        AssertStatusContrast(css, rootTokens, ".success-message", "--success", "--success-surface");
        AssertStatusContrast(css, rootTokens, ".status-label", "--success", "--success-surface");
        AssertStatusContrast(css, rootTokens, ".sample-label", "--warning", "--warning-surface");
        AssertStatusContrast(css, rootTokens, "#blazor-error-ui", "--ink-primary", "--danger-surface");
    }

    private static void AssertStatusContrast(
        string css,
        string rootTokens,
        string selector,
        string foregroundToken,
        string backgroundToken)
    {
        var rule = ExtractBlock(css, selector);
        Assert.Equal($"var({foregroundToken})", ReadDeclaration(rule, "color"));
        Assert.Equal($"var({backgroundToken})", ReadDeclaration(rule, "background"));

        var foreground = ReadToken(rootTokens, foregroundToken);
        var background = ReadToken(rootTokens, backgroundToken);

        Assert.True(
            ContrastRatio(foreground, background) >= 4.5,
            $"{selector} contrast was {ContrastRatio(foreground, background):F2}:1; expected at least 4.5:1.");
    }

    private static string ReadStylesheet() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", "app.css"));

    private static string ExtractBlock(string css, string selector)
    {
        var selectorIndex = css.LastIndexOf($"{selector} {{", StringComparison.Ordinal);
        Assert.True(selectorIndex >= 0, $"CSS rule or selector '{selector}' was not found.");

        var openingBrace = css.IndexOf('{', selectorIndex);
        Assert.True(openingBrace >= 0, $"CSS block for '{selector}' was not found.");

        var depth = 0;
        for (var index = openingBrace; index < css.Length; index++)
        {
            if (css[index] == '{')
            {
                depth++;
            }
            else if (css[index] == '}' && --depth == 0)
            {
                return css[(openingBrace + 1)..index];
            }
        }

        throw new InvalidOperationException($"CSS block for '{selector}' was not closed.");
    }

    private static string ReadDeclaration(string declarations, string property)
    {
        var match = Regex.Match(
            declarations,
            $@"(?:^|;)\s*{Regex.Escape(property)}\s*:\s*([^;]+)",
            RegexOptions.IgnoreCase);
        Assert.True(match.Success, $"CSS declaration '{property}' was not found.");
        return match.Groups[1].Value.Trim();
    }

    private static string ReadToken(string rootTokens, string token) =>
        ReadDeclaration(rootTokens, token);

    private static double ContrastRatio(string foreground, string background)
    {
        var foregroundLuminance = RelativeLuminance(foreground);
        var backgroundLuminance = RelativeLuminance(background);
        var lighter = Math.Max(foregroundLuminance, backgroundLuminance);
        var darker = Math.Min(foregroundLuminance, backgroundLuminance);

        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(string color)
    {
        Assert.Matches("^#[0-9a-fA-F]{6}$", color);

        var red = Linearize(Convert.ToInt32(color[1..3], 16) / 255.0);
        var green = Linearize(Convert.ToInt32(color[3..5], 16) / 255.0);
        var blue = Linearize(Convert.ToInt32(color[5..7], 16) / 255.0);

        return 0.2126 * red + 0.7152 * green + 0.0722 * blue;
    }

    private static double Linearize(double channel) =>
        channel <= 0.04045
            ? channel / 12.92
            : Math.Pow((channel + 0.055) / 1.055, 2.4);
}
