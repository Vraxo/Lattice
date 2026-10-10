namespace Lattice.Core;
/// <summary>
/// Applies validated line-range edits to an immutable <see cref="TextDocument"/>. Content outside
/// the edited range is preserved character for character, including its original terminators.
/// </summary>
/// <remarks>
/// The replacement text is inserted verbatim. A replacement that does not end with a line
/// terminator merges with the line that follows the range; supplying the terminator is the
/// caller's responsibility, because inferring one would be a guess. Deletion is replacement with
/// the empty string, and insertion is replacement of an empty range.
/// </remarks>
public static class TextEditor
{
    /// <summary>
    /// Replaces the lines in <paramref name="range"/> with <paramref name="replacement"/>.
    /// </summary>
    public static Result<TextDocument> Replace(TextDocument document, LineRange range, string replacement)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(replacement);
        if (range.End > document.LineCount)
        {
            return Result<TextDocument>.Failure(new Error(
                DocumentErrorCodes.RangeOutOfBounds,
                $"Range {range} exceeds the document's {document.LineCount} line(s)."));
        }
        string source = document.ToText();
        int start = CharacterOffset(document, range.Start);
        int end = CharacterOffset(document, range.End);
        string updated = string.Concat(source.AsSpan(0, start), replacement, source.AsSpan(end));
        return Result<TextDocument>.Success(TextDocument.Parse(updated));
    }
    /// <summary>
    /// Inserts <paramref name="text"/> immediately before the zero-based <paramref name="lineIndex"/>.
    /// An index equal to the line count appends at the end of the document.
    /// </summary>
    public static Result<TextDocument> Insert(TextDocument document, int lineIndex, string text)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(text);
        if (lineIndex < 0 || lineIndex > document.LineCount)
        {
            return Result<TextDocument>.Failure(new Error(
                DocumentErrorCodes.RangeOutOfBounds,
                $"Insertion line {lineIndex} is outside the document's {document.LineCount} line(s)."));
        }
        return Replace(document, LineRange.At(lineIndex), text);
    }
    /// <summary>Removes the lines in <paramref name="range"/>.</summary>
    public static Result<TextDocument> Delete(TextDocument document, LineRange range)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Replace(document, range, string.Empty);
    }
    /// <summary>Character offset at which the zero-based <paramref name="lineIndex"/> begins.</summary>
    private static int CharacterOffset(TextDocument document, int lineIndex)
    {
        int offset = 0;
        for (int i = 0; i < lineIndex; i++)
        {
            offset += document.Lines[i].Text.Length;
        }
        return offset;
    }
}