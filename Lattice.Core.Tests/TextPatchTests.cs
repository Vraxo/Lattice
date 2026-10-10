namespace Lattice.Core.Tests;

public sealed class TextPatchTests
{
    [Fact]
    public void RejectsEmptyFind()
    {
        Assert.Throws<ArgumentException>(() => new TextPatch(string.Empty, "x"));
    }

    [Fact]
    public void RejectsNullReplace()
    {
        Assert.Throws<ArgumentNullException>(() => new TextPatch("a", null!));
    }

    [Fact]
    public void AllowsEmptyReplaceForDeletion()
    {
        TextPatch patch = new("remove me", string.Empty);
        Assert.Equal(string.Empty, patch.Replace);
    }

    [Fact]
    public void PatchesWithSameContentAreEqual()
    {
        Assert.Equal(new TextPatch("a", "b"), new TextPatch("a", "b"));
    }
}