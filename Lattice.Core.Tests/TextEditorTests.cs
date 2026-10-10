namespace Lattice.Core.Tests;
public sealed class TextEditorTests
{
    [Fact]
    public void ReplaceSwapsTheTargetedLines()
    {
        TextDocument document = TextDocument.Parse("a\nb\nc\n");
        Result<TextDocument> result = TextEditor.Replace(document, new LineRange(1, 2), "X\n");
        Assert.True(result.IsSuccess);
        Assert.Equal("a\nX\nc\n", result.Value.ToText());
    }
    [Fact]
    public void ReplacePreservesLinesOutsideTheRange()
    {
        TextDocument document = TextDocument.Parse("keep1\nold1\nold2\nkeep2\n");
        Result<TextDocument> result = TextEditor.Replace(document, new LineRange(1, 3), "new\n");
        Assert.True(result.IsSuccess);
        Assert.Equal("keep1\nnew\nkeep2\n", result.Value.ToText());
    }
    [Fact]
    public void ReplacementWithoutTerminatorMergesWithTheFollowingLine()
    {
        // Pinned deliberately: content is inserted verbatim, so an unterminated replacement joins
        // the next line. Supplying the terminator is the caller's responsibility.
        TextDocument document = TextDocument.Parse("a\nb\nc\n");
        Result<TextDocument> result = TextEditor.Replace(document, new LineRange(1, 2), "X");
        Assert.True(result.IsSuccess);
        Assert.Equal("a\nXc\n", result.Value.ToText());
    }
    [Fact]
    public void ReplaceCanConsumeAndReplaceMultipleLines()
    {
        TextDocument document = TextDocument.Parse("a\nb\nc\nd\n");
        Result<TextDocument> result = TextEditor.Replace(document, new LineRange(1, 3), "X\nY\n");
        Assert.True(result.IsSuccess);
        Assert.Equal("a\nX\nY\nd\n", result.Value.ToText());
    }
    [Fact]
    public void ReplaceWithEmptyRangeInserts()
    {
        TextDocument document = TextDocument.Parse("a\nb\n");
        Result<TextDocument> result = TextEditor.Replace(document, LineRange.At(1), "X\n");
        Assert.True(result.IsSuccess);
        Assert.Equal("a\nX\nb\n", result.Value.ToText());
    }
    [Fact]
    public void ReplaceBeyondTheDocumentFails()
    {
        TextDocument document = TextDocument.Parse("a\nb\n");
        Result<TextDocument> result = TextEditor.Replace(document, new LineRange(0, 5), "X\n");
        Assert.False(result.IsSuccess);
        Assert.Equal(DocumentErrorCodes.RangeOutOfBounds, result.Error!.Code);
    }
    [Fact]
    public void DeleteRemovesTheTargetedLines()
    {
        TextDocument document = TextDocument.Parse("a\nb\nc\n");
        Result<TextDocument> result = TextEditor.Delete(document, new LineRange(1, 2));
        Assert.True(result.IsSuccess);
        Assert.Equal("a\nc\n", result.Value.ToText());
    }
    [Fact]
    public void DeleteAtEndRemovesTrailingLines()
    {
        TextDocument document = TextDocument.Parse("a\nb\nc\n");
        Result<TextDocument> result = TextEditor.Delete(document, new LineRange(2, 3));
        Assert.True(result.IsSuccess);
        Assert.Equal("a\nb\n", result.Value.ToText());
    }
    [Fact]
    public void DeleteOfEverythingLeavesAnEmptyDocument()
    {
        TextDocument document = TextDocument.Parse("a\nb\n");
        Result<TextDocument> result = TextEditor.Delete(document, new LineRange(0, 2));
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.LineCount);
        Assert.Equal(string.Empty, result.Value.ToText());
    }
    [Fact]
    public void EmptyRangeDeletesNothing()
    {
        TextDocument document = TextDocument.Parse("a\nb\n");
        Result<TextDocument> result = TextEditor.Delete(document, LineRange.At(1));
        Assert.True(result.IsSuccess);
        Assert.Equal("a\nb\n", result.Value.ToText());
    }
    [Fact]
    public void InsertAtStartPrependsLines()
    {
        TextDocument document = TextDocument.Parse("a\nb\n");
        Result<TextDocument> result = TextEditor.Insert(document, 0, "X\n");
        Assert.True(result.IsSuccess);
        Assert.Equal("X\na\nb\n", result.Value.ToText());
    }
    [Fact]
    public void InsertInMiddleAddsLines()
    {
        TextDocument document = TextDocument.Parse("a\nb\n");
        Result<TextDocument> result = TextEditor.Insert(document, 1, "X\n");
        Assert.True(result.IsSuccess);
        Assert.Equal("a\nX\nb\n", result.Value.ToText());
    }
    [Fact]
    public void InsertAtLineCountAppends()
    {
        TextDocument document = TextDocument.Parse("a\nb\n");
        Result<TextDocument> result = TextEditor.Insert(document, 2, "X\n");
        Assert.True(result.IsSuccess);
        Assert.Equal("a\nb\nX\n", result.Value.ToText());
    }
    [Fact]
    public void InsertIntoEmptyDocumentProducesContent()
    {
        TextDocument document = TextDocument.Parse(string.Empty);
        Result<TextDocument> result = TextEditor.Insert(document, 0, "X\n");
        Assert.True(result.IsSuccess);
        Assert.Equal("X\n", result.Value.ToText());
    }
    [Fact]
    public void InsertBeyondTheDocumentFails()
    {
        TextDocument document = TextDocument.Parse("a\n");
        Result<TextDocument> result = TextEditor.Insert(document, 5, "X\n");
        Assert.False(result.IsSuccess);
        Assert.Equal(DocumentErrorCodes.RangeOutOfBounds, result.Error!.Code);
    }
    [Fact]
    public void InsertAtNegativeLineFails()
    {
        TextDocument document = TextDocument.Parse("a\n");
        Result<TextDocument> result = TextEditor.Insert(document, -1, "X\n");
        Assert.False(result.IsSuccess);
        Assert.Equal(DocumentErrorCodes.RangeOutOfBounds, result.Error!.Code);
    }
    [Fact]
    public void CarriageReturnLineFeedTerminatorsArePreserved()
    {
        TextDocument document = TextDocument.Parse("a\r\nb\r\nc\r\n");
        Result<TextDocument> result = TextEditor.Replace(document, new LineRange(1, 2), "X\r\n");
        Assert.True(result.IsSuccess);
        Assert.Equal("a\r\nX\r\nc\r\n", result.Value.ToText());
    }
    [Fact]
    public void UntouchedLinesKeepTheirOwnTerminatorsInAMixedDocument()
    {
        TextDocument document = TextDocument.Parse("a\r\nb\nc\r\n");
        Result<TextDocument> result = TextEditor.Replace(document, new LineRange(1, 2), "X\n");
        Assert.True(result.IsSuccess);
        Assert.Equal("a\r\nX\nc\r\n", result.Value.ToText());
    }
    [Fact]
    public void EditingADocumentWithoutAFinalNewlineIsFine()
    {
        TextDocument document = TextDocument.Parse("a\nb");
        Result<TextDocument> result = TextEditor.Replace(document, new LineRange(1, 2), "X\n");
        Assert.True(result.IsSuccess);
        Assert.Equal("a\nX\n", result.Value.ToText());
    }
    [Fact]
    public void OriginalDocumentIsNotMutated()
    {
        TextDocument document = TextDocument.Parse("a\nb\nc\n");
        TextEditor.Replace(document, new LineRange(1, 2), "X\n");
        Assert.Equal("a\nb\nc\n", document.ToText());
    }
    [Fact]
    public void ReplacingTheWholeDocumentEqualsTheReplacement()
    {
        TextDocument document = TextDocument.Parse("a\nb\nc\n");
        Result<TextDocument> result = TextEditor.Replace(document, new LineRange(0, 3), "X\nY\n");
        Assert.True(result.IsSuccess);
        Assert.Equal("X\nY\n", result.Value.ToText());
    }
    [Fact]
    public void EditingIsDeterministic()
    {
        TextDocument document = TextDocument.Parse("a\nb\nc\n");
        Result<TextDocument> first = TextEditor.Replace(document, new LineRange(1, 2), "X\n");
        Result<TextDocument> second = TextEditor.Replace(document, new LineRange(1, 2), "X\n");
        Assert.Equal(first.Value, second.Value);
    }
    [Fact]
    public void NullDocumentIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => TextEditor.Replace(null!, LineRange.Empty, "X"));
        Assert.Throws<ArgumentNullException>(() => TextEditor.Insert(null!, 0, "X"));
        Assert.Throws<ArgumentNullException>(() => TextEditor.Delete(null!, LineRange.Empty));
    }
    [Fact]
    public void NullReplacementIsRejected()
    {
        TextDocument document = TextDocument.Parse("a\n");
        Assert.Throws<ArgumentNullException>(() => TextEditor.Replace(document, LineRange.Empty, null!));
        Assert.Throws<ArgumentNullException>(() => TextEditor.Insert(document, 0, null!));
    }
}