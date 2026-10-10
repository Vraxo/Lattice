using System.Text;

namespace Lattice.Core;

/// <summary>
/// Produces a minimal unified diff between two texts. A single patch produces a single
/// contiguous change, so one hunk is always sufficient.
/// </summary>
public static class UnifiedDiff
{
    public const int DefaultContextLines = 3;

    public static string Create(string original, string patched, int contextLines = DefaultContextLines)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(patched);
        ArgumentOutOfRangeException.ThrowIfNegative(contextLines);
        if (string.Equals(original, patched, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        string[] before = SplitLines(original);
        string[] after = SplitLines(patched);
        int prefix = 0;
        while (prefix < before.Length
            && prefix < after.Length
            && string.Equals(before[prefix], after[prefix], StringComparison.Ordinal))
        {
            prefix++;
        }

        int beforeEnd = before.Length - 1;
        int afterEnd = after.Length - 1;
        while (beforeEnd >= prefix
            && afterEnd >= prefix
            && string.Equals(before[beforeEnd], after[afterEnd], StringComparison.Ordinal))
        {
            beforeEnd--;
            afterEnd--;
        }

        int from = Math.Max(0, prefix - contextLines);
        int beforeTo = Math.Min(before.Length - 1, beforeEnd + contextLines);
        int afterTo = Math.Min(after.Length - 1, afterEnd + contextLines);
        StringBuilder builder = new();
        builder.Append("@@ -")
            .Append(from + 1).Append(',').Append(beforeTo - from + 1)
            .Append(" +")
            .Append(from + 1).Append(',').Append(afterTo - from + 1)
            .Append(" @@\n");
        for (int i = from; i < prefix; i++)
        {
            builder.Append(' ').Append(Trim(before[i])).Append('\n');
        }

        for (int i = prefix; i <= beforeEnd; i++)
        {
            builder.Append('-').Append(Trim(before[i])).Append('\n');
        }

        for (int i = prefix; i <= afterEnd; i++)
        {
            builder.Append('+').Append(Trim(after[i])).Append('\n');
        }

        for (int i = beforeEnd + 1; i <= beforeTo; i++)
        {
            builder.Append(' ').Append(Trim(before[i])).Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>
    /// Splits text into lines. A trailing newline terminates the last line rather than
    /// introducing an extra empty one, so a file ending in '\n' does not gain a phantom
    /// context line in the diff.
    /// </summary>
    private static string[] SplitLines(string text)
    {
        string[] lines = text.Split('\n');
        if (lines.Length > 1 && lines[^1].Length == 0)
        {
            return lines[..^1];
        }

        return lines;
    }

    private static string Trim(string line)
    {
        return line.TrimEnd('\r');
    }
}