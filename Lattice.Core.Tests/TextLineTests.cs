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
        line.Content.Should().Be("content");
        line.Terminator.Should().Be(terminator);
        line.Text.Should().Be("content" + terminator);
    }

    [Fact]
    public void RejectsUnknownTerminator()
    {
        FluentActions.Invoking(() => new TextLine("content", "\n\n")).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void RejectsNullContent()
    {
        FluentActions.Invoking(() => new TextLine(null!, "\n")).Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void EmptyLineWithNoTerminatorIsValid()
    {
        TextLine line = new(string.Empty, string.Empty);
        line.Text.Should().Be(string.Empty);
    }

    [Fact]
    public void LinesWithSameContentAndTerminatorAreEqual()
    {
        new TextLine("a", "\n").Should().Be(new TextLine("a", "\n"));
    }

    [Fact]
    public void LinesDifferingOnlyByTerminatorAreNotEqual()
    {
        new TextLine("a", "\r\n").Should().NotBe(new TextLine("a", "\n"));
    }
}