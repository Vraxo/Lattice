using System.Collections.Immutable;

namespace Lattice.Core;
/// <summary>
/// A line model of Markdown with just enough structure to target edits safely: ATX headings and
/// fenced code awareness. It is deliberately not a CommonMark parser.
/// </summary>
/// <remarks>
/// Supported subset: ATX headings (<c>#</c> through <c>######</c>) with at most three leading
/// spaces, an optional space after the hashes, and an optional trailing closing hash run; fenced
/// code blocks opened by three or more backticks or tildes, closed by a run of the same marker at
/// least as long. Four or more leading spaces denote indented code and are never read as a heading
/// or a fence.
/// <para>
/// Not interpreted: setext headings, tabs as indentation, inline formatting, list nesting, HTML
/// blocks, and backtick info strings containing backticks. Anything outside this subset is treated
/// as ordinary text rather than guessed at.
/// </para>
/// </remarks>
public sealed record MarkdownDocument
{
    private const int MaxIndent = 4;

    private MarkdownDocument(TextDocument text, ImmutableArray<MarkdownHeading> headings)
    {
        Text = text;
        Headings = headings;
    }

    public TextDocument Text { get; }

    public ImmutableArray<MarkdownHeading> Headings { get; }

    public int LineCount => Text.LineCount;

    public static MarkdownDocument Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        TextDocument text = TextDocument.Parse(source);
        return new MarkdownDocument(text, FindHeadings(text));
    }

    /// <summary>
    /// Resolves a heading to the section it governs. When <paramref name="level"/> is supplied only
    /// headings of that level match. Zero matches yield TargetNotFound; more than one yields
    /// TargetAmbiguous. A single match is never chosen by default.
    /// </summary>
    /// <returns></returns>
    public Result<MarkdownSection> FindSection(string headingText, int? level = null)
    {
        ArgumentNullException.ThrowIfNull(headingText);
        if (level is < 1 or > 6)
        {
            return Result<MarkdownSection>.Failure(new Error(
                DocumentErrorCodes.HeadingLevelInvalid,
                "Heading level must be between 1 and 6."));
        }

        ImmutableArray<MarkdownHeading> matches = MatchingHeadings(headingText, level);
        if (matches.IsEmpty)
        {
            return Result<MarkdownSection>.Failure(new Error(
                DocumentErrorCodes.TargetNotFound,
                $"No heading matching '{headingText}' was found."));
        }

        if (matches.Length > 1)
        {
            return Result<MarkdownSection>.Failure(new Error(
                DocumentErrorCodes.TargetAmbiguous,
                $"{matches.Length} headings match '{headingText}'; the target is ambiguous."));
        }

        return Result<MarkdownSection>.Success(new MarkdownSection(matches[0], ContentRangeOf(matches[0])));
    }

    private ImmutableArray<MarkdownHeading> MatchingHeadings(string headingText, int? level)
    {
        ImmutableArray<MarkdownHeading>.Builder builder = ImmutableArray.CreateBuilder<MarkdownHeading>();
        foreach (MarkdownHeading heading in Headings)
        {
            if (level is not null && heading.Level != level)
            {
                continue;
            }

            if (string.Equals(heading.Text, headingText, StringComparison.Ordinal))
            {
                builder.Add(heading);
            }
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// A section ends immediately before the next heading of the same or higher level, or at the
    /// end of the document. The heading line itself is not part of the content range.
    /// </summary>
    private LineRange ContentRangeOf(MarkdownHeading heading)
    {
        int end = Text.LineCount;
        foreach (MarkdownHeading candidate in Headings)
        {
            if (candidate.LineIndex > heading.LineIndex && candidate.Level <= heading.Level)
            {
                end = candidate.LineIndex;
                break;
            }
        }

        return new LineRange(heading.LineIndex + 1, end);
    }

    private static ImmutableArray<MarkdownHeading> FindHeadings(TextDocument text)
    {
        ImmutableArray<MarkdownHeading>.Builder builder = ImmutableArray.CreateBuilder<MarkdownHeading>();
        FenceState fence = FenceState.None;
        for (int i = 0; i < text.LineCount; i++)
        {
            string content = text.Lines[i].Content;
            if (fence.IsOpen)
            {
                if (fence.Closes(content))
                {
                    fence = FenceState.None;
                }

                continue;
            }

            if (FenceState.TryOpen(content, out FenceState opened))
            {
                fence = opened;
                continue;
            }

            if (TryReadHeading(content, i, out MarkdownHeading? heading))
            {
                builder.Add(heading);
            }
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Counts leading spaces, capped at <see cref="MaxIndent"/>. Reaching the cap means the line is
    /// indented code, which is never a heading or a fence.
    /// </summary>
    private static bool TrySkipIndent(string content, out int index)
    {
        index = 0;
        while (index < content.Length && index < MaxIndent && content[index] == ' ')
        {
            index++;
        }

        return index < MaxIndent;
    }

    private static bool TryReadHeading(string content, int lineIndex, out MarkdownHeading heading)
    {
        heading = null!;
        if (!TrySkipIndent(content, out int index))
        {
            return false;
        }

        int hashes = 0;
        while (index + hashes < content.Length && content[index + hashes] == '#')
        {
            hashes++;
        }

        if (hashes is < 1 or > 6)
        {
            return false;
        }

        int afterHashes = index + hashes;
        if (afterHashes < content.Length && content[afterHashes] != ' ')
        {
            return false;
        }

        string text = content[afterHashes..].Trim();
        heading = new MarkdownHeading(hashes, StripClosingHashes(text), lineIndex);
        return true;
    }

    /// <summary>
    /// Removes an optional trailing run of hashes from heading text. The run counts only when it is
    /// the whole remainder or is preceded by a space, so <c>C#</c> keeps its hash.
    /// </summary>
    private static string StripClosingHashes(string text)
    {
        int closingStart = text.Length;
        while (closingStart > 0 && text[closingStart - 1] == '#')
        {
            closingStart--;
        }

        if (closingStart < text.Length && (closingStart == 0 || text[closingStart - 1] == ' '))
        {
            return text[..closingStart].TrimEnd();
        }

        return text;
    }

    /// <summary>Tracks an open fenced code block so its contents are never read as headings.</summary>
    private readonly struct FenceState
    {
        private FenceState(char marker, int length)
        {
            Marker = marker;
            Length = length;
            IsOpen = true;
        }

        public static FenceState None => default;

        public char Marker { get; }

        public int Length { get; }

        public bool IsOpen { get; }

        public static bool TryOpen(string content, out FenceState fence)
        {
            fence = None;
            if (!TrySkipIndent(content, out int index) || index >= content.Length)
            {
                return false;
            }

            char marker = content[index];
            if (marker is not ('`' or '~'))
            {
                return false;
            }

            int length = 0;
            while (index + length < content.Length && content[index + length] == marker)
            {
                length++;
            }

            if (length < 3)
            {
                return false;
            }

            fence = new FenceState(marker, length);
            return true;
        }

        public bool Closes(string content)
        {
            if (!TrySkipIndent(content, out int index))
            {
                return false;
            }

            int length = 0;
            while (index + length < content.Length && content[index + length] == Marker)
            {
                length++;
            }

            if (length < Length)
            {
                return false;
            }

            // A closing fence carries nothing but whitespace after it.
            return content[(index + length)..].Trim().Length == 0;
        }
    }
}