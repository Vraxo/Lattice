namespace Lattice.Core.Tests;

public sealed class TextDocumentTests
{
    [Fact]
    public void EmptyTextHasNoLines()
    {
        TextDocument document = TextDocument.Parse(string.Empty);
        document.LineCount.Should().Be(0);
        document.ToText().Should().Be(string.Empty);
    }

    [Fact]
    public void SingleLineWithoutTerminatorHasOneLine()
    {
        TextDocument document = TextDocument.Parse("hello");
        document.LineCount.Should().Be(1);
        document.Lines[0].Content.Should().Be("hello");
        document.Lines[0].Terminator.Should().Be(string.Empty);
    }

    [Fact]
    public void TrailingNewlineDoesNotProduceAnExtraEmptyLine()
    {
        TextDocument document = TextDocument.Parse("a\n");
        document.LineCount.Should().Be(1);
        document.Lines[0].Terminator.Should().Be("\n");
    }

    [Fact]
    public void BlankLineIsPreservedAsItsOwnLine()
    {
        TextDocument document = TextDocument.Parse("a\n\nb");
        document.LineCount.Should().Be(3);
        document.Lines[1].Content.Should().Be(string.Empty);
        document.Lines[1].Terminator.Should().Be("\n");
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("a\n")]
    [InlineData("a\nb")]
    [InlineData("a\nb\n")]
    [InlineData("a\n\nb\n")]
    [InlineData("a\r\nb\r\n")]
    [InlineData("a\r\nb")]
    [InlineData("a\nb\r\nc")]
    [InlineData("\n\n\n")]
    [InlineData("line without terminator at end")]
    public void RoundTripReproducesTheOriginalTextExactly(string original)
    {
        TextDocument document = TextDocument.Parse(original);
        document.ToText().Should().Be(original);
    }

    [Fact]
    public void CarriageReturnLineFeedIsOneTerminator()
    {
        TextDocument document = TextDocument.Parse("a\r\nb");
        document.LineCount.Should().Be(2);
        document.Lines[0].Terminator.Should().Be("\r\n");
        document.Lines[1].Content.Should().Be("b");
    }

    [Fact]
    public void LoneCarriageReturnIsATerminator()
    {
        TextDocument document = TextDocument.Parse("a\rb");
        document.LineCount.Should().Be(2);
        document.Lines[0].Terminator.Should().Be("\r");
        document.Lines[1].Content.Should().Be("b");
    }

    [Fact]
    public void MixedTerminatorsArePreservedIndividually()
    {
        TextDocument document = TextDocument.Parse("a\r\nb\nc");
        document.LineCount.Should().Be(3);
        document.Lines[0].Terminator.Should().Be("\r\n");
        document.Lines[1].Terminator.Should().Be("\n");
        document.Lines[2].Terminator.Should().Be(string.Empty);
    }

    [Fact]
    public void NullTextIsRejected()
    {
        FluentActions.Invoking(() => TextDocument.Parse(null!)).Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void GetTextReturnsRequestedLinesWithTerminators()
    {
        TextDocument document = TextDocument.Parse("a\nb\nc\n");
        Result<string> result = document.GetText(new LineRange(1, 3));
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("b\nc\n");
    }

    [Fact]
    public void GetTextOfEmptyRangeReturnsEmptyString()
    {
        TextDocument document = TextDocument.Parse("a\nb\n");
        Result<string> result = document.GetText(LineRange.At(1));
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(string.Empty);
    }

    [Fact]
    public void GetTextBeyondTheDocumentFails()
    {
        TextDocument document = TextDocument.Parse("a\nb\n");
        Result<string> result = document.GetText(new LineRange(0, 5));
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(DocumentErrorCodes.RangeOutOfBounds);
    }

    [Fact]
    public void GetTextOfWholeDocumentEqualsOriginal()
    {
        const string original = "a\r\nb\nc";
        TextDocument document = TextDocument.Parse(original);
        Result<string> result = document.GetText(new LineRange(0, document.LineCount));
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(original);
    }

    [Fact]
    public void DocumentsWithSameLinesAreEqual()
    {
        TextDocument.Parse("a\r\nb").Should().Be(TextDocument.Parse("a\r\nb"));
    }

    [Fact]
    public void DocumentsDifferingOnlyByTerminatorAreNotEqual()
    {
        TextDocument.Parse("a\r\n").Should().NotBe(TextDocument.Parse("a\n"));
    }
}