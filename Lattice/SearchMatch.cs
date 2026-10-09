namespace Lattice.Core;
/// <summary>One line that matched a search, with its source location.</summary>
public sealed record SearchMatch
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
}