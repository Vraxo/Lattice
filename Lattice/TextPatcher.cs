namespace Lattice.Core;

public static class TextPatcher
{
    /// <summary>
    /// Applies <paramref name="patch"/> to <paramref name="original"/> and returns a preview.
    /// The patch applies only when its context occurs exactly once; zero occurrences are
    /// treated as stale and multiple occurrences as ambiguous, and neither is applied.
    /// </summary>
    /// <returns></returns>
    public static PatchPreview Apply(string original, TextPatch patch)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(patch);
        int first = original.IndexOf(patch.Find, StringComparison.Ordinal);
        if (first < 0)
        {
            return PatchPreview.ContextNotFound(original, patch);
        }

        int second = original.IndexOf(patch.Find, first + 1, StringComparison.Ordinal);
        if (second >= 0)
        {
            return PatchPreview.ContextAmbiguous(original, patch);
        }

        string patched = string.Concat(
            original.AsSpan(0, first),
            patch.Replace,
            original.AsSpan(first + patch.Find.Length));
        return PatchPreview.Applied(original, patch, patched, first);
    }
}