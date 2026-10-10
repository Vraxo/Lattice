namespace Lattice.Core.Tests;

public sealed class TextLineTests
{
    [Theory]
    [InlineData("")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\r")]
    public void AcceptsKnownTerminators(string terminator)
    {
        TextLine line = new("content", terminator);
        Assert.Equal("content", line.Content);
        Assert.Equal(terminator, line.Terminator);
        Assert.Equal("content" + terminator, line.Text);
    }

    [Fact]
    public void RejectsUnknownTerminator()
    {
        Assert.Throws<ArgumentException>(() => new TextLine("content", "\n\n"));
    }

    [Fact]
    public void RejectsNullContent()
    {
        Assert.Throws<ArgumentNullException>(() => new TextLine(null!, "\n"));
    }

    [Fact]
    public void EmptyLineWithNoTerminatorIsValid()
    {
        TextLine line = new(string.Empty, string.Empty);
        Assert.Equal(string.Empty, line.Text);
    }

    [Fact]
    public void LinesWithSameContentAndTerminatorAreEqual()
    {
        Assert.Equal(new TextLine("a", "\n"), new TextLine("a", "\n"));
    }

    [Fact]
    public void LinesDifferingOnlyByTerminatorAreNotEqual()
    {
        Assert.NotEqual(new TextLine("a", "\n"), new TextLine("a", "\r\n"));
    }
}