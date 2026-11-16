namespace Lattice.Core.Tests;

public sealed class TextPatchTests
{
    [Fact]
    public void RejectsEmptyFind()
    {
        FluentActions.Invoking(() => new TextPatch(string.Empty, "x")).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void RejectsNullReplace()
    {
        FluentActions.Invoking(() => new TextPatch("a", null!)).Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void AllowsEmptyReplaceForDeletion()
    {
        TextPatch patch = new("remove me", string.Empty);
        patch.Replace.Should().Be(string.Empty);
    }

    [Fact]
    public void PatchesWithSameContentAreEqual()
    {
        new TextPatch("a", "b").Should().Be(new TextPatch("a", "b"));
    }
}