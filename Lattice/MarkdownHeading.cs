namespace Lattice.Core;
/// <summary>
/// One ATX heading recognized in a document. <see cref="Text"/> is the heading's content with the
/// leading <c>#</c> run and any trailing closing run removed, so <c>## Section ##</c> has the text
/// <c>Section</c>.
/// </summary>
public sealed record MarkdownHeading
{
    public MarkdownHeading(int level, string text, int lineIndex)
    {
        if (level is < 1 or > 6)
        {
            throw new ArgumentOutOfRangeException(nameof(level), "Heading level must be between 1 and 6.");
        }

        ArgumentNullException.ThrowIfNull(text);
        if (lineIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lineIndex), "Line index must not be negative.");
        }

        Level = level;
        Text = text;
        LineIndex = lineIndex;
    }

    /// <summary>Gets heading level, 1 for <c>#</c> through 6 for <c>######</c>.</summary>
    public int Level { get; }

    /// <summary>Gets heading text, which may be empty.</summary>
    public string Text { get; }

    /// <summary>Gets zero-based index of the heading line.</summary>
    public int LineIndex { get; }

    public override string ToString()
    {
        return $"{new string('#', Level)} {Text}";
    }
}