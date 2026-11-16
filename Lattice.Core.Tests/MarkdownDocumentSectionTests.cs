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
        section.IsSuccess.Should().BeTrue();
        section.Value.ContentRange.Start.Should().Be(1);
        section.Value.ContentRange.End.Should().Be(8);
    }

    [Fact]
    public void SectionEndsBeforeNextHeadingOfSameLevel()
    {
        MarkdownDocument document = MarkdownDocument.Parse(Nested);
        Result<MarkdownSection> section = document.FindSection("B");
        section.IsSuccess.Should().BeTrue();
        section.Value.ContentRange.Start.Should().Be(3);
        section.Value.ContentRange.End.Should().Be(6);
    }

    [Fact]
    public void DeeperHeadingDoesNotEndAParentSection()
    {
        MarkdownDocument document = MarkdownDocument.Parse(Nested);
        Result<MarkdownSection> section = document.FindSection("B");

        // C is deeper than B, so it belongs to B's content range rather than ending it.
        section.IsSuccess.Should().BeTrue();
        section.Value.ContentRange.End.Should().Be(6);
    }

    [Fact]
    public void HigherLevelHeadingEndsADeeperSection()
    {
        MarkdownDocument document = MarkdownDocument.Parse(Nested);
        Result<MarkdownSection> section = document.FindSection("C");
        section.IsSuccess.Should().BeTrue();
        section.Value.ContentRange.Start.Should().Be(5);
        section.Value.ContentRange.End.Should().Be(6);
    }

    [Fact]
    public void ContentRangeExcludesTheHeadingLine()
    {
        MarkdownDocument document = MarkdownDocument.Parse("# A\nbody\n");
        Result<MarkdownSection> section = document.FindSection("A");
        section.IsSuccess.Should().BeTrue();
        section.Value.Heading.LineIndex.Should().Be(0);
        section.Value.ContentRange.Start.Should().Be(1);
    }

    [Fact]
    public void FullRangeIncludesTheHeadingLine()
    {
        MarkdownDocument document = MarkdownDocument.Parse("# A\nbody\n");
        Result<MarkdownSection> section = document.FindSection("A");
        section.IsSuccess.Should().BeTrue();
        section.Value.FullRange.Start.Should().Be(0);
        section.Value.FullRange.End.Should().Be(2);
    }

    [Fact]
    public void EmptySectionHasAnEmptyContentRange()
    {
        MarkdownDocument document = MarkdownDocument.Parse("# A\n# B\n");
        Result<MarkdownSection> section = document.FindSection("A");
        section.IsSuccess.Should().BeTrue();
        section.Value.ContentRange.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void MissingHeadingIsReportedAsNotFound()
    {
        MarkdownDocument document = MarkdownDocument.Parse("# A\n");
        Result<MarkdownSection> section = document.FindSection("Missing");
        section.IsSuccess.Should().BeFalse();
        section.Error!.Code.Should().Be(DocumentErrorCodes.TargetNotFound);
    }

    [Fact]
    public void DuplicateHeadingsAreReportedAsAmbiguous()
    {
        MarkdownDocument document = MarkdownDocument.Parse("# Same\n# Same\n");
        Result<MarkdownSection> section = document.FindSection("Same");
        section.IsSuccess.Should().BeFalse();
        section.Error!.Code.Should().Be(DocumentErrorCodes.TargetAmbiguous);
    }

    [Fact]
    public void LevelDisambiguatesHeadingsWithTheSameText()
    {
        MarkdownDocument document = MarkdownDocument.Parse("# Same\n## Same\n");
        Result<MarkdownSection> section = document.FindSection("Same", level: 2);
        section.IsSuccess.Should().BeTrue();
        section.Value.Heading.Level.Should().Be(2);
        section.Value.Heading.LineIndex.Should().Be(1);
    }

    [Fact]
    public void SameTextAtDifferentLevelsIsAmbiguousWithoutALevel()
    {
        MarkdownDocument document = MarkdownDocument.Parse("# Same\n## Same\n");
        Result<MarkdownSection> section = document.FindSection("Same");
        section.IsSuccess.Should().BeFalse();
        section.Error!.Code.Should().Be(DocumentErrorCodes.TargetAmbiguous);
    }

    [Fact]
    public void InvalidLevelIsRejected()
    {
        MarkdownDocument document = MarkdownDocument.Parse("# A\n");
        Result<MarkdownSection> section = document.FindSection("A", level: 7);
        section.IsSuccess.Should().BeFalse();
        section.Error!.Code.Should().Be(DocumentErrorCodes.HeadingLevelInvalid);
    }

    [Fact]
    public void HeadingMatchIsExactAndCaseSensitive()
    {
        MarkdownDocument document = MarkdownDocument.Parse("# Section\n");
        document.FindSection("section").Error!.Code.Should().Be(DocumentErrorCodes.TargetNotFound);
        document.FindSection("Section ").Error!.Code.Should().Be(DocumentErrorCodes.TargetNotFound);
        document.FindSection("Section").IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void HeadingInsideFenceCannotBeSelected()
    {
        MarkdownDocument document = MarkdownDocument.Parse("```\n# Hidden\n```\n");
        Result<MarkdownSection> section = document.FindSection("Hidden");
        section.IsSuccess.Should().BeFalse();
        section.Error!.Code.Should().Be(DocumentErrorCodes.TargetNotFound);
    }

    [Fact]
    public void SectionContentCanBeRetrievedAsExactText()
    {
        MarkdownDocument document = MarkdownDocument.Parse(Nested);
        Result<MarkdownSection> section = document.FindSection("B");
        section.IsSuccess.Should().BeTrue();
        Result<string> content = document.Text.GetText(section.Value.ContentRange);
        content.IsSuccess.Should().BeTrue();
        content.Value.Should().Be("b-content\n### C\nc-content\n");
    }
}