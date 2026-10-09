using System.Text.RegularExpressions;
namespace Lattice.Core;
/// <summary>One line that matched a search, with its source location.</summary>
public sealed partial record SearchMatch
{
    public SearchMatch(string relativePath, int lineNumber, string lineText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentOutOfRangeException.ThrowIfLessThan(lineNumber, 1);
        ArgumentNullException.ThrowIfNull(lineText);
        RelativePath = relativePath;
        LineNumber = lineNumber;
        LineText = lineText;
    }
    public string RelativePath { get; }
    /// <summary>One-based line number.</summary>
    public int LineNumber { get; }
    public string LineText { get; }
    public override string ToString() => $"{RelativePath}:{LineNumber}: {LineText}";
    /// <summary>
    /// Parses a line produced by <see cref="ToString"/>. The line number is the first
    /// <c>:digits: </c> sequence, so a path containing that exact pattern would misparse.
    /// </summary>
    public static bool TryParse(string? line, out SearchMatch match)
    {
        match = null!;
        if (string.IsNullOrEmpty(line))
        {
            return false;
        }
        var parsed = LinePattern().Match(line);
        if (!parsed.Success)
        {
            return false;
        }
        match = new SearchMatch(
            parsed.Groups["path"].Value,
            int.Parse(parsed.Groups["line"].Value, System.Globalization.CultureInfo.InvariantCulture),
            parsed.Groups["text"].Value);
        return true;
    }
    [GeneratedRegex(@"^(?<path>.+?):(?<line>\d+): (?<text>.*)$")]
    private static partial Regex LinePattern();
}