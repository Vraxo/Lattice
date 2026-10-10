namespace Lattice.Core.Tests;

public sealed class TextDocumentTests
{
    [Fact]
    public void EmptyTextHasNoLines()
    {
        TextDocument document = TextDocument.Parse(string.Empty);
        Assert.Equal(0, document.LineCount);
        Assert.Equal(string.Empty, document.ToText());
    }

    [Fact]
    public void SingleLineWithoutTerminatorHasOneLine()
    {
        TextDocument document = TextDocument.Parse("hello");
        Assert.Equal(1, document.LineCount);
        Assert.Equal("hello", document.Lines[0].Content);
        Assert.Equal(string.Empty, document.Lines[0].Terminator);
    }

    [Fact]
    public void TrailingNewlineDoesNotProduceAnExtraEmptyLine()
    {
        TextDocument document = TextDocument.Parse("a\n");
        Assert.Equal(1, document.LineCount);
        Assert.Equal("\n", document.Lines[0].Terminator);
    }

    [Fact]
    public void BlankLineIsPreservedAsItsOwnLine()
    {
        TextDocument document = TextDocument.Parse("a\n\nb");
        Assert.Equal(3, document.LineCount);
        Assert.Equal(string.Empty, document.Lines[1].Content);
        Assert.Equal("\n", document.Lines[1].Terminator);
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
        Assert.Equal(original, document.ToText());
    }

    [Fact]
    public void CarriageReturnLineFeedIsOneTerminator()
    {
        TextDocument document = TextDocument.Parse("a\r\nb");
        Assert.Equal(2, document.LineCount);
        Assert.Equal("\r\n", document.Lines[0].Terminator);
        Assert.Equal("b", document.Lines[1].Content);
    }

    [Fact]
    public void LoneCarriageReturnIsATerminator()
    {
        TextDocument document = TextDocument.Parse("a\rb");
        Assert.Equal(2, document.LineCount);
        Assert.Equal("\r", document.Lines[0].Terminator);
        Assert.Equal("b", document.Lines[1].Content);
    }

    [Fact]
    public void MixedTerminatorsArePreservedIndividually()
    {
        TextDocument document = TextDocument.Parse("a\r\nb\nc");
        Assert.Equal(3, document.LineCount);
        Assert.Equal("\r\n", document.Lines[0].Terminator);
        Assert.Equal("\n", document.Lines[1].Terminator);
        Assert.Equal(string.Empty, document.Lines[2].Terminator);
    }

    [Fact]
    public void NullTextIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => TextDocument.Parse(null!));
    }

    [Fact]
    public void GetTextReturnsRequestedLinesWithTerminators()
    {
        TextDocument document = TextDocument.Parse("a\nb\nc\n");
        Result<string> result = document.GetText(new LineRange(1, 3));
        Assert.True(result.IsSuccess);
        Assert.Equal("b\nc\n", result.Value);
    }

    [Fact]
    public void GetTextOfEmptyRangeReturnsEmptyString()
    {
        TextDocument document = TextDocument.Parse("a\nb\n");
        Result<string> result = document.GetText(LineRange.At(1));
        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Value);
    }

    [Fact]
    public void GetTextBeyondTheDocumentFails()
    {
        TextDocument document = TextDocument.Parse("a\nb\n");
        Result<string> result = document.GetText(new LineRange(0, 5));
        Assert.False(result.IsSuccess);
        Assert.Equal(DocumentErrorCodes.RangeOutOfBounds, result.Error!.Code);
    }

    [Fact]
    public void GetTextOfWholeDocumentEqualsOriginal()
    {
        const string original = "a\r\nb\nc";
        TextDocument document = TextDocument.Parse(original);
        Result<string> result = document.GetText(new LineRange(0, document.LineCount));
        Assert.True(result.IsSuccess);
        Assert.Equal(original, result.Value);
    }

    [Fact]
    public void DocumentsWithSameLinesAreEqual()
    {
        Assert.Equal(TextDocument.Parse("a\r\nb"), TextDocument.Parse("a\r\nb"));
    }

    [Fact]
    public void DocumentsDifferingOnlyByTerminatorAreNotEqual()
    {
        Assert.NotEqual(TextDocument.Parse("a\n"), TextDocument.Parse("a\r\n"));
    }
}