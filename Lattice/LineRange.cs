namespace Lattice.Core;
/// <summary>
/// A zero-based, end-exclusive range of lines. <c>[0, 0)</c> is the empty range at the start of a
/// document; <c>[n, n)</c> is an insertion point after the first n lines. One-based user-facing
/// line numbers are converted at the boundary with <see cref="FromOneBasedLines"/>.
/// </summary>
public readonly record struct LineRange
{
    public LineRange(int start, int end)
    {
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(start), "Range start must not be negative.");
        }

        if (end < start)
        {
            throw new ArgumentOutOfRangeException(nameof(end), "Range end must not precede its start.");
        }

        Start = start;
        End = end;
    }

    /// <summary>Gets zero-based index of the first line in the range.</summary>
    public int Start { get; }

    /// <summary>Gets zero-based, exclusive index one past the last line in the range.</summary>
    public int End { get; }

    public int Length => End - Start;

    public bool IsEmpty => Start == End;

    public static LineRange Empty { get; } = new(0, 0);

    /// <summary>An empty range positioned before the given zero-based line, for insertion.</summary>
    /// <returns></returns>
    public static LineRange At(int line)
    {
        return new(line, line);
    }

    /// <summary>
    /// Converts an inclusive, one-based line span into a zero-based, end-exclusive range. For
    /// example lines 2..3 become <c>[1, 3)</c>.
    /// </summary>
    /// <returns></returns>
    public static LineRange FromOneBasedLines(int firstLine, int lastLine)
    {
        if (firstLine < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(firstLine), "Line numbers are one-based.");
        }

        if (lastLine < firstLine)
        {
            throw new ArgumentOutOfRangeException(nameof(lastLine), "Last line must not precede the first.");
        }

        return new LineRange(firstLine - 1, lastLine);
    }

    /// <summary>Converts a single one-based line number into a one-line range.</summary>
    /// <returns></returns>
    public static LineRange FromOneBasedLine(int line)
    {
        return FromOneBasedLines(line, line);
    }

    public override string ToString()
    {
        return $"[{Start}, {End})";
    }
}