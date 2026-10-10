using System.Collections.Immutable;
using System.Text;
namespace Lattice.Core;
/// <summary>
/// Immutable, terminator-preserving view of text as a sequence of lines. Reassembling the lines
/// reproduces the original source exactly, including individual line terminators and the presence
/// or absence of a final newline. This is a line model, not a Markdown or rich-text parser.
/// </summary>
public sealed record TextDocument
{
    private TextDocument(ImmutableArray<TextLine> lines)
    {
        Lines = lines;
    }
    public ImmutableArray<TextLine> Lines { get; }
    public int LineCount => Lines.Length;
    /// <summary>Splits text into lines, preserving every terminator exactly.</summary>
    public static TextDocument Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0)
        {
            return new TextDocument([]);
        }
        ImmutableArray<TextLine>.Builder builder = ImmutableArray.CreateBuilder<TextLine>();
        int index = 0;
        while (index < text.Length)
        {
            int terminatorIndex = IndexOfTerminator(text, index, out int terminatorLength);
            if (terminatorIndex < 0)
            {
                builder.Add(new TextLine(text[index..], string.Empty));
                break;
            }
            builder.Add(new TextLine(
                text[index..terminatorIndex],
                text.Substring(terminatorIndex, terminatorLength)));
            index = terminatorIndex + terminatorLength;
        }
        return new TextDocument(builder.ToImmutable());
    }
    /// <summary>Reassembles the document into its original text.</summary>
    public string ToText()
    {
        StringBuilder builder = new();
        foreach (TextLine line in Lines)
        {
            builder.Append(line.Text);
        }
        return builder.ToString();
    }
    /// <summary>
    /// Returns the exact source text covered by <paramref name="range"/>, terminators included.
    /// The range must lie within the document; an empty range yields an empty string.
    /// </summary>
    public Result<string> GetText(LineRange range)
    {
        if (range.End > Lines.Length)
        {
            return Result<string>.Failure(new Error(
                DocumentErrorCodes.RangeOutOfBounds,
                $"Range {range} exceeds the document's {Lines.Length} line(s)."));
        }
        if (range.IsEmpty)
        {
            return Result<string>.Success(string.Empty);
        }
        StringBuilder builder = new();
        for (int i = range.Start; i < range.End; i++)
        {
            builder.Append(Lines[i].Text);
        }
        return Result<string>.Success(builder.ToString());
    }
    /// <summary>
    /// Finds the next terminator at or after <paramref name="start"/>. A carriage return followed
    /// by a line feed is one terminator; a lone carriage return is also a terminator.
    /// </summary>
    private static int IndexOfTerminator(string text, int start, out int length)
    {
        for (int i = start; i < text.Length; i++)
        {
            char current = text[i];
            if (current == '\n')
            {
                length = 1;
                return i;
            }
            if (current == '\r')
            {
                bool followedByLineFeed = i + 1 < text.Length && text[i + 1] == '\n';
                length = followedByLineFeed ? 2 : 1;
                return i;
            }
        }
        length = 0;
        return -1;
    }
    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct line sets as unequal.
    public bool Equals(TextDocument? other) =>
        other is not null && Lines.SequenceEqual(other.Lines);
    public override int GetHashCode()
    {
        HashCode hash = default;
        foreach (TextLine line in Lines)
        {
            hash.Add(line);
        }
        return hash.ToHashCode();
    }
}