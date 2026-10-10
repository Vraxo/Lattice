namespace Lattice.Core.Tests;

public sealed class PatchPreviewTests
{
    [Fact]
    public void AppliedCarriesPatchedTextAndIndex()
    {
        PatchPreview preview = PatchPreview.Applied("abc", new TextPatch("b", "x"), "axc", 1);
        Assert.True(preview.IsApplied);
        Assert.Equal("axc", preview.PatchedText);
        Assert.Equal(1, preview.MatchIndex);
    }

    [Fact]
    public void ContextNotFoundHasNoPatchedText()
    {
        PatchPreview preview = PatchPreview.ContextNotFound("abc", new TextPatch("z", "x"));
        Assert.False(preview.IsApplied);
        Assert.Equal(PatchStatus.ContextNotFound, preview.Status);
        Assert.Null(preview.PatchedText);
        Assert.Equal(-1, preview.MatchIndex);
    }

    [Fact]
    public void ContextAmbiguousHasNoPatchedText()
    {
        PatchPreview preview = PatchPreview.ContextAmbiguous("abc", new TextPatch("a", "x"));
        Assert.False(preview.IsApplied);
        Assert.Equal(PatchStatus.ContextAmbiguous, preview.Status);
        Assert.Null(preview.PatchedText);
        Assert.Equal(-1, preview.MatchIndex);
    }

    [Fact]
    public void RejectsNullOriginal()
    {
        Assert.Throws<ArgumentNullException>(
            () => PatchPreview.Applied(null!, new TextPatch("a", "b"), "c", 0));
    }
}