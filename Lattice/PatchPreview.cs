namespace Lattice.Core;
/// <summary>
/// The result of applying a patch to in-memory text. Nothing is written to disk; this is a
/// preview. <see cref="PatchedText"/> is non-null only when the status is
/// <see cref="PatchStatus.Applied"/>.
/// </summary>
public sealed record PatchPreview
{
    private PatchPreview(
        PatchStatus status,
        string original,
        TextPatch patch,
        string? patchedText,
        int matchIndex)
    {
        Status = status;
        Original = original;
        Patch = patch;
        PatchedText = patchedText;
        MatchIndex = matchIndex;
    }
    public PatchStatus Status { get; }
    public string Original { get; }
    public TextPatch Patch { get; }
    public string? PatchedText { get; }
    /// <summary>Zero-based character index of the replaced region, or -1 when not applied.</summary>
    public int MatchIndex { get; }
    public bool IsApplied => Status == PatchStatus.Applied;
    public static PatchPreview Applied(string original, TextPatch patch, string patchedText, int matchIndex)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(patch);
        ArgumentNullException.ThrowIfNull(patchedText);
        return new PatchPreview(PatchStatus.Applied, original, patch, patchedText, matchIndex);
    }
    public static PatchPreview ContextNotFound(string original, TextPatch patch)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(patch);
        return new PatchPreview(PatchStatus.ContextNotFound, original, patch, null, -1);
    }
    public static PatchPreview ContextAmbiguous(string original, TextPatch patch)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(patch);
        return new PatchPreview(PatchStatus.ContextAmbiguous, original, patch, null, -1);
    }
}