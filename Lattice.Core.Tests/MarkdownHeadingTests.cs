namespace Lattice.Core.Tests;

public sealed class MarkdownHeadingTests
{
    [Fact]
    public void StoresLevelTextAndLine()
    {
        MarkdownHeading heading = new(2, "Section", 5);
        Assert.Equal(2, heading.Level);
        Assert.Equal("Section", heading.Text);
        Assert.Equal(5, heading.LineIndex);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void RejectsLevelOutsideOneToSix(int level)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MarkdownHeading(level, "x", 0));
    }

    [Fact]
    public void RejectsNegativeLineIndex()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MarkdownHeading(1, "x", -1));
    }

    [Fact]
    public void EmptyHeadingTextIsAllowed()
    {
        MarkdownHeading heading = new(1, string.Empty, 0);
        Assert.Equal(string.Empty, heading.Text);
    }

    [Fact]
    public void ToStringRendersHashesAndText()
    {
        Assert.Equal("## Section", new MarkdownHeading(2, "Section", 0).ToString());
    }
}