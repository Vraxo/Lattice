namespace Lattice.Core.Tests;

public sealed class TextPatcherTests
{
    [Fact]
    public void AppliesUniqueMatch()
    {
        PatchPreview preview = TextPatcher.Apply("hello world", new TextPatch("world", "there"));
        Assert.True(preview.IsApplied);
        Assert.Equal("hello there", preview.PatchedText);
        Assert.Equal(6, preview.MatchIndex);
    }

    [Fact]
    public void UnchangedRegionsStayUnchanged()
    {
        const string original = "alpha\nbeta\ngamma\n";
        PatchPreview preview = TextPatcher.Apply(original, new TextPatch("beta", "BETA"));
        Assert.True(preview.IsApplied);
        Assert.Equal("alpha\nBETA\ngamma\n", preview.PatchedText);

        // Everything outside the replaced span must be byte-identical.
        Assert.StartsWith("alpha\n", preview.PatchedText, StringComparison.Ordinal);
        Assert.EndsWith("\ngamma\n", preview.PatchedText, StringComparison.Ordinal);
    }

    [Fact]
    public void StaleContextIsRejected()
    {
        PatchPreview preview = TextPatcher.Apply("hello world", new TextPatch("goodbye", "hi"));
        Assert.False(preview.IsApplied);
        Assert.Equal(PatchStatus.ContextNotFound, preview.Status);
        Assert.Null(preview.PatchedText);
    }

    [Fact]
    public void AmbiguousContextIsRejected()
    {
        PatchPreview preview = TextPatcher.Apply("a b a", new TextPatch("a", "x"));
        Assert.False(preview.IsApplied);
        Assert.Equal(PatchStatus.ContextAmbiguous, preview.Status);
        Assert.Null(preview.PatchedText);
    }

    [Fact]
    public void ContextPresentTwiceIsRejectedEvenIfAdjacent()
    {
        PatchPreview preview = TextPatcher.Apply("aaaa", new TextPatch("aa", "b"));
        Assert.False(preview.IsApplied);
        Assert.Equal(PatchStatus.ContextAmbiguous, preview.Status);
    }

    [Fact]
    public void DeletingTextRemovesIt()
    {
        PatchPreview preview = TextPatcher.Apply("keep delete keep2", new TextPatch(" delete", string.Empty));
        Assert.True(preview.IsApplied);
        Assert.Equal("keep keep2", preview.PatchedText);
    }

    [Fact]
    public void MatchingIsCaseSensitive()
    {
        PatchPreview preview = TextPatcher.Apply("Hello", new TextPatch("hello", "hi"));
        Assert.False(preview.IsApplied);
        Assert.Equal(PatchStatus.ContextNotFound, preview.Status);
    }

    [Fact]
    public void MatchAtStartIsApplied()
    {
        PatchPreview preview = TextPatcher.Apply("start end", new TextPatch("start", "begin"));
        Assert.True(preview.IsApplied);
        Assert.Equal(0, preview.MatchIndex);
        Assert.Equal("begin end", preview.PatchedText);
    }

    [Fact]
    public void EmptyOriginalCannotContainContext()
    {
        PatchPreview preview = TextPatcher.Apply(string.Empty, new TextPatch("a", "b"));
        Assert.False(preview.IsApplied);
        Assert.Equal(PatchStatus.ContextNotFound, preview.Status);
    }

    [Fact]
    public void ReplacementContainingContextDoesNotRecurse()
    {
        // Replacing "a" with "aa" must not then match the inserted text.
        PatchPreview preview = TextPatcher.Apply("a", new TextPatch("a", "aa"));
        Assert.True(preview.IsApplied);
        Assert.Equal("aa", preview.PatchedText);
    }

    [Fact]
    public void ApplyingPatchIsDeterministic()
    {
        TextPatch patch = new("b", "x");
        PatchPreview first = TextPatcher.Apply("abc", patch);
        PatchPreview second = TextPatcher.Apply("abc", patch);
        Assert.Equal(first.PatchedText, second.PatchedText);
        Assert.Equal(first.MatchIndex, second.MatchIndex);
    }

    [Fact]
    public void NullInputsThrow()
    {
        Assert.Throws<ArgumentNullException>(() => TextPatcher.Apply(null!, new TextPatch("a", "b")));
        Assert.Throws<ArgumentNullException>(() => TextPatcher.Apply("abc", null!));
    }
}