namespace Lattice.Core;
/// <summary>
/// One line of source text together with the exact terminator that followed it. The terminator is
/// empty for a final line with no trailing newline, so reassembling the lines reproduces the
/// original text byte for byte.
/// </summary>
public sealed record TextLine
{
    public TextLine(string content, string terminator)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(terminator);
        if (!IsKnownTerminator(terminator))
        {
            throw new ArgumentException(
                $"Terminator must be empty, \"\\n\", \"\\r\\n\", or \"\\r\"; got \"{Escape(terminator)}\".",
                nameof(terminator));
        }

        Content = content;
        Terminator = terminator;
    }

    /// <summary>Gets the line without its terminator.</summary>
    public string Content { get; }

    /// <summary>Gets the exact terminator, or the empty string when the line is unterminated.</summary>
    public string Terminator { get; }

    /// <summary>Gets the content and terminator as they appeared in the source.</summary>
    public string Text => Content + Terminator;

    private static bool IsKnownTerminator(string terminator)
    {
        return terminator is "" or "\n" or "\r\n" or "\r";
    }

    private static string Escape(string value)
    {
        return value.Replace("\r", "\\r", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal);
    }
}