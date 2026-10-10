namespace Lattice.Core.Tests;

public sealed class MarkdownDocumentSectionTests
{
    private const string Nested =
        "# A\n" +
        "a-content\n" +
        "## B\n" +
        "b-content\n" +
        "### C\n" +
        "c-content\n" +
        "## D\n" +
        "d-content\n";

    [Fact]
    public void TopLevelSectionRunsToEndOfDocument()
    {
        MarkdownDocument document = MarkdownDocument.Parse(Nested);
        Result<MarkdownSection> section = document.FindSection("A");
        Assert.True(section.IsSuccess);
        Assert.Equal(1, section.Value.ContentRange.Start);
        Assert.Equal(8, section.Value.ContentRange.End);
    }

    [Fact]
    public void SectionEndsBeforeNextHeadingOfSameLevel()
    {
        MarkdownDocument document = MarkdownDocument.Parse(Nested);
        Result<MarkdownSection> section = document.FindSection("B");
        Assert.True(section.IsSuccess);
        Assert.Equal(3, section.Value.ContentRange.Start);
        Assert.Equal(6, section.Value.ContentRange.End);
    }

    [Fact]
    public void DeeperHeadingDoesNotEndAParentSection()
    {
        MarkdownDocument document = MarkdownDocument.Parse(Nested);
        Result<MarkdownSection> section = document.FindSection("B");

        // C is deeper than B, so it belongs to B's content range rather than ending it.
        Assert.True(section.IsSuccess);
        Assert.Equal(6, section.Value.ContentRange.End);
    }

    [Fact]
    public void HigherLevelHeadingEndsADeeperSection()
    {
        MarkdownDocument document = MarkdownDocument.Parse(Nested);
        Result<MarkdownSection> section = document.FindSection("C");
        Assert.True(section.IsSuccess);
        Assert.Equal(5, section.Value.ContentRange.Start);
        Assert.Equal(6, section.Value.ContentRange.End);
    }

    [Fact]
    public void ContentRangeExcludesTheHeadingLine()
    {
        MarkdownDocument document = MarkdownDocument.Parse("# A\nbody\n");
        Result<MarkdownSection> section = document.FindSection("A");
        Assert.True(section.IsSuccess);
        Assert.Equal(0, section.Value.Heading.LineIndex);
        Assert.Equal(1, section.Value.ContentRange.Start);
    }

    [Fact]
    public void FullRangeIncludesTheHeadingLine()
    {
        MarkdownDocument document = MarkdownDocument.Parse("# A\nbody\n");
        Result<MarkdownSection> section = document.FindSection("A");
        Assert.True(section.IsSuccess);
        Assert.Equal(0, section.Value.FullRange.Start);
        Assert.Equal(2, section.Value.FullRange.End);
    }

    [Fact]
    public void EmptySectionHasAnEmptyContentRange()
    {
        MarkdownDocument document = MarkdownDocument.Parse("# A\n# B\n");
        Result<MarkdownSection> section = document.FindSection("A");
        Assert.True(section.IsSuccess);
        Assert.True(section.Value.ContentRange.IsEmpty);
    }

    [Fact]
    public void MissingHeadingIsReportedAsNotFound()
    {
        MarkdownDocument document = MarkdownDocument.Parse("# A\n");
        Result<MarkdownSection> section = document.FindSection("Missing");
        Assert.False(section.IsSuccess);
        Assert.Equal(DocumentErrorCodes.TargetNotFound, section.Error!.Code);
    }

    [Fact]
    public void DuplicateHeadingsAreReportedAsAmbiguous()
    {
        MarkdownDocument document = MarkdownDocument.Parse("# Same\n# Same\n");
        Result<MarkdownSection> section = document.FindSection("Same");
        Assert.False(section.IsSuccess);
        Assert.Equal(DocumentErrorCodes.TargetAmbiguous, section.Error!.Code);
    }

    [Fact]
    public void LevelDisambiguatesHeadingsWithTheSameText()
    {
        MarkdownDocument document = MarkdownDocument.Parse("# Same\n## Same\n");
        Result<MarkdownSection> section = document.FindSection("Same", level: 2);
        Assert.True(section.IsSuccess);
        Assert.Equal(2, section.Value.Heading.Level);
        Assert.Equal(1, section.Value.Heading.LineIndex);
    }

    [Fact]
    public void SameTextAtDifferentLevelsIsAmbiguousWithoutALevel()
    {
        MarkdownDocument document = MarkdownDocument.Parse("# Same\n## Same\n");
        Result<MarkdownSection> section = document.FindSection("Same");
        Assert.False(section.IsSuccess);
        Assert.Equal(DocumentErrorCodes.TargetAmbiguous, section.Error!.Code);
    }

    [Fact]
    public void InvalidLevelIsRejected()
    {
        MarkdownDocument document = MarkdownDocument.Parse("# A\n");
        Result<MarkdownSection> section = document.FindSection("A", level: 7);
        Assert.False(section.IsSuccess);
        Assert.Equal(DocumentErrorCodes.HeadingLevelInvalid, section.Error!.Code);
    }

    [Fact]
    public void HeadingMatchIsExactAndCaseSensitive()
    {
        MarkdownDocument document = MarkdownDocument.Parse("# Section\n");
        Assert.Equal(DocumentErrorCodes.TargetNotFound, document.FindSection("section").Error!.Code);
        Assert.Equal(DocumentErrorCodes.TargetNotFound, document.FindSection("Section ").Error!.Code);
        Assert.True(document.FindSection("Section").IsSuccess);
    }

    [Fact]
    public void HeadingInsideFenceCannotBeSelected()
    {
        MarkdownDocument document = MarkdownDocument.Parse("```\n# Hidden\n```\n");
        Result<MarkdownSection> section = document.FindSection("Hidden");
        Assert.False(section.IsSuccess);
        Assert.Equal(DocumentErrorCodes.TargetNotFound, section.Error!.Code);
    }

    [Fact]
    public void SectionContentCanBeRetrievedAsExactText()
    {
        MarkdownDocument document = MarkdownDocument.Parse(Nested);
        Result<MarkdownSection> section = document.FindSection("B");
        Assert.True(section.IsSuccess);
        Result<string> content = document.Text.GetText(section.Value.ContentRange);
        Assert.True(content.IsSuccess);
        Assert.Equal("b-content\n### C\nc-content\n", content.Value);
    }
}