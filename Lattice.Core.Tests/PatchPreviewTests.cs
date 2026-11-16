namespace Lattice.Core.Tests;

public sealed class PatchPreviewTests
{
    [Fact]
    public void AppliedCarriesPatchedTextAndIndex()
    {
        PatchPreview preview = PatchPreview.Applied("abc", new TextPatch("b", "x"), "axc", 1);
        preview.IsApplied.Should().BeTrue();
        preview.PatchedText.Should().Be("axc");
        preview.MatchIndex.Should().Be(1);
    }

    [Fact]
    public void ContextNotFoundHasNoPatchedText()
    {
        PatchPreview preview = PatchPreview.ContextNotFound("abc", new TextPatch("z", "x"));
        preview.IsApplied.Should().BeFalse();
        preview.Status.Should().Be(PatchStatus.ContextNotFound);
        preview.PatchedText.Should().BeNull();
        preview.MatchIndex.Should().Be(-1);
    }

    [Fact]
    public void ContextAmbiguousHasNoPatchedText()
    {
        PatchPreview preview = PatchPreview.ContextAmbiguous("abc", new TextPatch("a", "x"));
        preview.IsApplied.Should().BeFalse();
        preview.Status.Should().Be(PatchStatus.ContextAmbiguous);
        preview.PatchedText.Should().BeNull();
        preview.MatchIndex.Should().Be(-1);
    }

    [Fact]
    public void RejectsNullOriginal()
    {
        FluentActions.Invoking(() => PatchPreview.Applied(null!, new TextPatch("a", "b"), "c", 0)).Should().ThrowExactly<ArgumentNullException>();
    }
}