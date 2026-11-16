namespace Lattice.Core.Tests;

public sealed class MarkdownHeadingTests
{
    [Fact]
    public void StoresLevelTextAndLine()
    {
        MarkdownHeading heading = new(2, "Section", 5);
        heading.Level.Should().Be(2);
        heading.Text.Should().Be("Section");
        heading.LineIndex.Should().Be(5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void RejectsLevelOutsideOneToSix(int level)
    {
        FluentActions.Invoking(() => new MarkdownHeading(level, "x", 0)).Should().ThrowExactly<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RejectsNegativeLineIndex()
    {
        FluentActions.Invoking(() => new MarkdownHeading(1, "x", -1)).Should().ThrowExactly<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void EmptyHeadingTextIsAllowed()
    {
        MarkdownHeading heading = new(1, string.Empty, 0);
        heading.Text.Should().Be(string.Empty);
    }

    [Fact]
    public void ToStringRendersHashesAndText()
    {
        new MarkdownHeading(2, "Section", 0).ToString().Should().Be("## Section");
    }
}