namespace Lattice.Core.Tests;

public sealed class TextPatcherTests
{
    [Fact]
    public void AppliesUniqueMatch()
    {
        PatchPreview preview = TextPatcher.Apply("hello world", new TextPatch("world", "there"));
        preview.IsApplied.Should().BeTrue();
        preview.PatchedText.Should().Be("hello there");
        preview.MatchIndex.Should().Be(6);
    }

    [Fact]
    public void UnchangedRegionsStayUnchanged()
    {
        const string original = "alpha\nbeta\ngamma\n";
        PatchPreview preview = TextPatcher.Apply(original, new TextPatch("beta", "BETA"));
        preview.IsApplied.Should().BeTrue();
        preview.PatchedText.Should().Be("alpha\nBETA\ngamma\n");

        // Everything outside the replaced span must be byte-identical.
        preview.PatchedText.Should().StartWith("alpha\n");
        preview.PatchedText.Should().EndWith("\ngamma\n");
    }

    [Fact]
    public void StaleContextIsRejected()
    {
        PatchPreview preview = TextPatcher.Apply("hello world", new TextPatch("goodbye", "hi"));
        preview.IsApplied.Should().BeFalse();
        preview.Status.Should().Be(PatchStatus.ContextNotFound);
        preview.PatchedText.Should().BeNull();
    }

    [Fact]
    public void AmbiguousContextIsRejected()
    {
        PatchPreview preview = TextPatcher.Apply("a b a", new TextPatch("a", "x"));
        preview.IsApplied.Should().BeFalse();
        preview.Status.Should().Be(PatchStatus.ContextAmbiguous);
        preview.PatchedText.Should().BeNull();
    }

    [Fact]
    public void ContextPresentTwiceIsRejectedEvenIfAdjacent()
    {
        PatchPreview preview = TextPatcher.Apply("aaaa", new TextPatch("aa", "b"));
        preview.IsApplied.Should().BeFalse();
        preview.Status.Should().Be(PatchStatus.ContextAmbiguous);
    }

    [Fact]
    public void DeletingTextRemovesIt()
    {
        PatchPreview preview = TextPatcher.Apply("keep delete keep2", new TextPatch(" delete", string.Empty));
        preview.IsApplied.Should().BeTrue();
        preview.PatchedText.Should().Be("keep keep2");
    }

    [Fact]
    public void MatchingIsCaseSensitive()
    {
        PatchPreview preview = TextPatcher.Apply("Hello", new TextPatch("hello", "hi"));
        preview.IsApplied.Should().BeFalse();
        preview.Status.Should().Be(PatchStatus.ContextNotFound);
    }

    [Fact]
    public void MatchAtStartIsApplied()
    {
        PatchPreview preview = TextPatcher.Apply("start end", new TextPatch("start", "begin"));
        preview.IsApplied.Should().BeTrue();
        preview.MatchIndex.Should().Be(0);
        preview.PatchedText.Should().Be("begin end");
    }

    [Fact]
    public void EmptyOriginalCannotContainContext()
    {
        PatchPreview preview = TextPatcher.Apply(string.Empty, new TextPatch("a", "b"));
        preview.IsApplied.Should().BeFalse();
        preview.Status.Should().Be(PatchStatus.ContextNotFound);
    }

    [Fact]
    public void ReplacementContainingContextDoesNotRecurse()
    {
        // Replacing "a" with "aa" must not then match the inserted text.
        PatchPreview preview = TextPatcher.Apply("a", new TextPatch("a", "aa"));
        preview.IsApplied.Should().BeTrue();
        preview.PatchedText.Should().Be("aa");
    }

    [Fact]
    public void ApplyingPatchIsDeterministic()
    {
        TextPatch patch = new("b", "x");
        PatchPreview first = TextPatcher.Apply("abc", patch);
        PatchPreview second = TextPatcher.Apply("abc", patch);
        second.PatchedText.Should().Be(first.PatchedText);
        second.MatchIndex.Should().Be(first.MatchIndex);
    }

    [Fact]
    public void NullInputsThrow()
    {
        FluentActions.Invoking(() => TextPatcher.Apply(null!, new TextPatch("a", "b"))).Should().ThrowExactly<ArgumentNullException>();
        FluentActions.Invoking(() => TextPatcher.Apply("abc", null!)).Should().ThrowExactly<ArgumentNullException>();
    }
}