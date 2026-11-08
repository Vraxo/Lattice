using System.Globalization;
using System.Text.RegularExpressions;

namespace Lattice.Desktop.Tests;

public sealed partial class CharcoalThemeStylesheetTests
{
    [Fact]
    public void StylesheetDefinesNeutralCharcoalCustomPropertiesAndRemovesBlueBlackPalette()
    {
        string cssPath = LocateAppCssPath();
        string css = File.ReadAllText(cssPath);

        // Required neutral charcoal token family
        Assert.Contains("--bg-app: #232323;", css, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--bg-panel: #2b2b2b;", css, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--bg-inset: #1c1c1c;", css, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--bg-user-turn: #353535;", css, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--text-primary: #ebebeb;", css, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--text-muted: #a3a3a3;", css, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--focus-ring: #bdbdbd;", css, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("@media (prefers-reduced-motion: reduce)", css, StringComparison.Ordinal);

        // Former blue-black/saturated-blue shades must not reappear.
        string[] legacyBlueShades =
        [
            "#0d1117",
            "#121821",
            "#141c25",
            "#1c3a63",
            "#2e5b96",
            "#2f81f7",
            "#4793ff",
            "#58a6ff",
            "#6ea8fe",
            "#79c0ff",
        ];
        foreach (string shade in legacyBlueShades)
        {
            Assert.DoesNotContain(shade, css, StringComparison.OrdinalIgnoreCase);
        }

        // Verify neutral surface & border hex tokens have zero blue channel dominance (B <= R + 2 and B <= G + 2).
        string[] neutralTokens =
        [
            "--bg-app",
            "--bg-panel",
            "--bg-panel-header",
            "--bg-user-turn",
            "--bg-inset",
            "--bg-control",
            "--bg-control-hover",
            "--bg-primary-button",
            "--bg-primary-button-hover",
            "--border-subtle",
            "--border-default",
            "--border-strong",
            "--focus-ring",
            "--text-strong",
            "--text-primary",
            "--text-secondary",
            "--text-muted",
            "--text-path",
        ];

        foreach (string token in neutralTokens)
        {
            (int r, int g, int b) = ExtractRgbToken(css, token);
            Assert.True(
                Math.Abs(r - g) <= 2 && Math.Abs(g - b) <= 2 && Math.Abs(r - b) <= 2,
                $"Expected '{token}' to be neutral gray, but got RGB({r}, {g}, {b}).");
        }

        // Verify WCAG AA (>= 4.5:1) contrast for primary and muted text against main and panel surfaces.
        Assert.True(ContrastRatio(ExtractRgbToken(css, "--text-primary"), ExtractRgbToken(css, "--bg-app")) >= 10.0);
        Assert.True(ContrastRatio(ExtractRgbToken(css, "--text-primary"), ExtractRgbToken(css, "--bg-panel")) >= 9.0);
        Assert.True(ContrastRatio(ExtractRgbToken(css, "--text-muted"), ExtractRgbToken(css, "--bg-app")) >= 4.5);
        Assert.True(ContrastRatio(ExtractRgbToken(css, "--text-muted"), ExtractRgbToken(css, "--bg-panel")) >= 4.5);
        Assert.True(ContrastRatio(ExtractRgbToken(css, "--text-path"), ExtractRgbToken(css, "--bg-inset")) >= 9.0);
    }

    private static (int R, int G, int B) ExtractRgbToken(string css, string tokenName)
    {
        Match match = HexTokenRegex().Match(css);
        while (match.Success)
        {
            if (string.Equals(match.Groups[1].Value, tokenName, StringComparison.Ordinal))
            {
                string hex = match.Groups[2].Value;
                int r = int.Parse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                return (r, g, b);
            }

            match = match.NextMatch();
        }

        throw new InvalidOperationException($"Token '{tokenName}' was not found in app.css.");
    }

    private static double ContrastRatio((int R, int G, int B) foreground, (int R, int G, int B) background)
    {
        double l1 = RelativeLuminance(foreground);
        double l2 = RelativeLuminance(background);
        double lighter = Math.Max(l1, l2);
        double darker = Math.Min(l1, l2);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance((int R, int G, int B) color)
    {
        static double Channel(int value)
        {
            double normalized = value / 255.0;
            return normalized <= 0.03928
                ? normalized / 12.92
                : Math.Pow((normalized + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Channel(color.R))
            + (0.7152 * Channel(color.G))
            + (0.0722 * Channel(color.B));
    }

    private static string LocateAppCssPath()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, "Lattice.Desktop", "wwwroot", "app.css");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Lattice.Desktop/wwwroot/app.css.");
    }

    [GeneratedRegex(@"(--[a-z0-9-]+)\s*:\s*#([0-9a-fA-F]{6})\s*;")]
    private static partial Regex HexTokenRegex();
}
