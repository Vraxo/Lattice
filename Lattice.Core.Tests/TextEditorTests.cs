namespace Lattice.Core.Tests;

public sealed class TextEditorTests
{
    [Fact]
    public void ReplaceSwapsTheTargetedLines()
    {
        TextDocument document = TextDocument.Parse("a\nb\nc\n");
        Result<TextDocument> result = TextEditor.Replace(document, new LineRange(1, 2), "X\n");
        result.IsSuccess.Should().BeTrue();
        result.Value.ToText().Should().Be("a\nX\nc\n");
    }

    [Fact]
    public void ReplacePreservesLinesOutsideTheRange()
    {
        TextDocument document = TextDocument.Parse("keep1\nold1\nold2\nkeep2\n");
        Result<TextDocument> result = TextEditor.Replace(document, new LineRange(1, 3), "new\n");
        result.IsSuccess.Should().BeTrue();
        result.Value.ToText().Should().Be("keep1\nnew\nkeep2\n");
    }

    [Fact]
    public void ReplacementWithoutTerminatorMergesWithTheFollowingLine()
    {
        // Pinned deliberately: content is inserted verbatim, so an unterminated replacement joins
        // the next line. Supplying the terminator is the caller's responsibility.
        TextDocument document = TextDocument.Parse("a\nb\nc\n");
        Result<TextDocument> result = TextEditor.Replace(document, new LineRange(1, 2), "X");
        result.IsSuccess.Should().BeTrue();
        result.Value.ToText().Should().Be("a\nXc\n");
    }

    [Fact]
    public void ReplaceCanConsumeAndReplaceMultipleLines()
    {
        TextDocument document = TextDocument.Parse("a\nb\nc\nd\n");
        Result<TextDocument> result = TextEditor.Replace(document, new LineRange(1, 3), "X\nY\n");
        result.IsSuccess.Should().BeTrue();
        result.Value.ToText().Should().Be("a\nX\nY\nd\n");
    }

    [Fact]
    public void ReplaceWithEmptyRangeInserts()
    {
        TextDocument document = TextDocument.Parse("a\nb\n");
        Result<TextDocument> result = TextEditor.Replace(document, LineRange.At(1), "X\n");
        result.IsSuccess.Should().BeTrue();
        result.Value.ToText().Should().Be("a\nX\nb\n");
    }

    [Fact]
    public void ReplaceBeyondTheDocumentFails()
    {
        TextDocument document = TextDocument.Parse("a\nb\n");
        Result<TextDocument> result = TextEditor.Replace(document, new LineRange(0, 5), "X\n");
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(DocumentErrorCodes.RangeOutOfBounds);
    }

    [Fact]
    public void DeleteRemovesTheTargetedLines()
    {
        TextDocument document = TextDocument.Parse("a\nb\nc\n");
        Result<TextDocument> result = TextEditor.Delete(document, new LineRange(1, 2));
        result.IsSuccess.Should().BeTrue();
        result.Value.ToText().Should().Be("a\nc\n");
    }

    [Fact]
    public void DeleteAtEndRemovesTrailingLines()
    {
        TextDocument document = TextDocument.Parse("a\nb\nc\n");
        Result<TextDocument> result = TextEditor.Delete(document, new LineRange(2, 3));
        result.IsSuccess.Should().BeTrue();
        result.Value.ToText().Should().Be("a\nb\n");
    }

    [Fact]
    public void DeleteOfEverythingLeavesAnEmptyDocument()
    {
        TextDocument document = TextDocument.Parse("a\nb\n");
        Result<TextDocument> result = TextEditor.Delete(document, new LineRange(0, 2));
        result.IsSuccess.Should().BeTrue();
        result.Value.LineCount.Should().Be(0);
        result.Value.ToText().Should().Be(string.Empty);
    }

    [Fact]
    public void EmptyRangeDeletesNothing()
    {
        TextDocument document = TextDocument.Parse("a\nb\n");
        Result<TextDocument> result = TextEditor.Delete(document, LineRange.At(1));
        result.IsSuccess.Should().BeTrue();
        result.Value.ToText().Should().Be("a\nb\n");
    }

    [Fact]
    public void InsertAtStartPrependsLines()
    {
        TextDocument document = TextDocument.Parse("a\nb\n");
        Result<TextDocument> result = TextEditor.Insert(document, 0, "X\n");
        result.IsSuccess.Should().BeTrue();
        result.Value.ToText().Should().Be("X\na\nb\n");
    }

    [Fact]
    public void InsertInMiddleAddsLines()
    {
        TextDocument document = TextDocument.Parse("a\nb\n");
        Result<TextDocument> result = TextEditor.Insert(document, 1, "X\n");
        result.IsSuccess.Should().BeTrue();
        result.Value.ToText().Should().Be("a\nX\nb\n");
    }

    [Fact]
    public void InsertAtLineCountAppends()
    {
        TextDocument document = TextDocument.Parse("a\nb\n");
        Result<TextDocument> result = TextEditor.Insert(document, 2, "X\n");
        result.IsSuccess.Should().BeTrue();
        result.Value.ToText().Should().Be("a\nb\nX\n");
    }

    [Fact]
    public void InsertIntoEmptyDocumentProducesContent()
    {
        TextDocument document = TextDocument.Parse(string.Empty);
        Result<TextDocument> result = TextEditor.Insert(document, 0, "X\n");
        result.IsSuccess.Should().BeTrue();
        result.Value.ToText().Should().Be("X\n");
    }

    [Fact]
    public void InsertBeyondTheDocumentFails()
    {
        TextDocument document = TextDocument.Parse("a\n");
        Result<TextDocument> result = TextEditor.Insert(document, 5, "X\n");
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(DocumentErrorCodes.RangeOutOfBounds);
    }

    [Fact]
    public void InsertAtNegativeLineFails()
    {
        TextDocument document = TextDocument.Parse("a\n");
        Result<TextDocument> result = TextEditor.Insert(document, -1, "X\n");
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(DocumentErrorCodes.RangeOutOfBounds);
    }

    [Fact]
    public void CarriageReturnLineFeedTerminatorsArePreserved()
    {
        TextDocument document = TextDocument.Parse("a\r\nb\r\nc\r\n");
        Result<TextDocument> result = TextEditor.Replace(document, new LineRange(1, 2), "X\r\n");
        result.IsSuccess.Should().BeTrue();
        result.Value.ToText().Should().Be("a\r\nX\r\nc\r\n");
    }

    [Fact]
    public void UntouchedLinesKeepTheirOwnTerminatorsInAMixedDocument()
    {
        TextDocument document = TextDocument.Parse("a\r\nb\nc\r\n");
        Result<TextDocument> result = TextEditor.Replace(document, new LineRange(1, 2), "X\n");
        result.IsSuccess.Should().BeTrue();
        result.Value.ToText().Should().Be("a\r\nX\nc\r\n");
    }

    [Fact]
    public void EditingADocumentWithoutAFinalNewlineIsFine()
    {
        TextDocument document = TextDocument.Parse("a\nb");
        Result<TextDocument> result = TextEditor.Replace(document, new LineRange(1, 2), "X\n");
        result.IsSuccess.Should().BeTrue();
        result.Value.ToText().Should().Be("a\nX\n");
    }

    [Fact]
    public void OriginalDocumentIsNotMutated()
    {
        TextDocument document = TextDocument.Parse("a\nb\nc\n");
        TextEditor.Replace(document, new LineRange(1, 2), "X\n");
        document.ToText().Should().Be("a\nb\nc\n");
    }

    [Fact]
    public void ReplacingTheWholeDocumentEqualsTheReplacement()
    {
        TextDocument document = TextDocument.Parse("a\nb\nc\n");
        Result<TextDocument> result = TextEditor.Replace(document, new LineRange(0, 3), "X\nY\n");
        result.IsSuccess.Should().BeTrue();
        result.Value.ToText().Should().Be("X\nY\n");
    }

    [Fact]
    public void EditingIsDeterministic()
    {
        TextDocument document = TextDocument.Parse("a\nb\nc\n");
        Result<TextDocument> first = TextEditor.Replace(document, new LineRange(1, 2), "X\n");
        Result<TextDocument> second = TextEditor.Replace(document, new LineRange(1, 2), "X\n");
        second.Value.Should().Be(first.Value);
    }

    [Fact]
    public void NullDocumentIsRejected()
    {
        FluentActions.Invoking(() => TextEditor.Replace(null!, LineRange.Empty, "X")).Should().ThrowExactly<ArgumentNullException>();
        FluentActions.Invoking(() => TextEditor.Insert(null!, 0, "X")).Should().ThrowExactly<ArgumentNullException>();
        FluentActions.Invoking(() => TextEditor.Delete(null!, LineRange.Empty)).Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void NullReplacementIsRejected()
    {
        TextDocument document = TextDocument.Parse("a\n");
        FluentActions.Invoking(() => TextEditor.Replace(document, LineRange.Empty, null!)).Should().ThrowExactly<ArgumentNullException>();
        FluentActions.Invoking(() => TextEditor.Insert(document, 0, null!)).Should().ThrowExactly<ArgumentNullException>();
    }
}