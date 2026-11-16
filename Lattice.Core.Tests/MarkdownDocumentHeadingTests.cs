namespace Lattice.Core.Tests;

public sealed class MarkdownDocumentHeadingTests
{
    [Fact]
    public void RecognizesHeadingsOfEachLevel()
    {
        const string source = "# One\n## Two\n### Three\n#### Four\n##### Five\n###### Six\n";
        MarkdownDocument document = MarkdownDocument.Parse(source);
        document.Headings.Length.Should().Be(6);
        document.Headings[0].Level.Should().Be(1);
        document.Headings[5].Level.Should().Be(6);
    }

    [Fact]
    public void StripsClosingHashes()
    {
        MarkdownDocument document = MarkdownDocument.Parse("## Section ##\n");
        document.Headings[0].Text.Should().Be("Section");
    }

    [Fact]
    public void KeepsHashThatIsPartOfTheText()
    {
        MarkdownDocument document = MarkdownDocument.Parse("# C#\n");
        document.Headings[0].Text.Should().Be("C#");
    }

    [Fact]
    public void HeadingWithNoTextIsRecognized()
    {
        MarkdownDocument document = MarkdownDocument.Parse("#\n");
        document.Headings.Should().ContainSingle();
        document.Headings[0].Text.Should().Be(string.Empty);
    }

    [Fact]
    public void UpToThreeLeadingSpacesAreAllowed()
    {
        MarkdownDocument document = MarkdownDocument.Parse("   # Indented\n");
        document.Headings.Should().ContainSingle();
        document.Headings[0].Text.Should().Be("Indented");
    }

    [Fact]
    public void FourLeadingSpacesIsIndentedCodeNotAHeading()
    {
        MarkdownDocument document = MarkdownDocument.Parse("    # Not a heading\n");
        document.Headings.Should().BeEmpty();
    }

    [Fact]
    public void SevenHashesIsNotAHeading()
    {
        MarkdownDocument document = MarkdownDocument.Parse("####### Too many\n");
        document.Headings.Should().BeEmpty();
    }

    [Fact]
    public void HashWithoutFollowingSpaceIsNotAHeading()
    {
        MarkdownDocument document = MarkdownDocument.Parse("#NotAHeading\n");
        document.Headings.Should().BeEmpty();
    }

    [Fact]
    public void HeadingsAreRecordedWithTheirLineIndexes()
    {
        MarkdownDocument document = MarkdownDocument.Parse("intro\n# First\nbody\n## Second\n");
        document.Headings.Length.Should().Be(2);
        document.Headings[0].LineIndex.Should().Be(1);
        document.Headings[1].LineIndex.Should().Be(3);
    }

    [Fact]
    public void HeadingInsideBacktickFenceIsIgnored()
    {
        const string source = "# Real\n```\n# Fake\n```\n# AlsoReal\n";
        MarkdownDocument document = MarkdownDocument.Parse(source);
        document.Headings.Length.Should().Be(2);
        document.Headings[0].Text.Should().Be("Real");
        document.Headings[1].Text.Should().Be("AlsoReal");
    }

    [Fact]
    public void HeadingInsideTildeFenceIsIgnored()
    {
        const string source = "~~~\n# Fake\n~~~\n# Real\n";
        MarkdownDocument document = MarkdownDocument.Parse(source);
        document.Headings.Should().ContainSingle();
        document.Headings[0].Text.Should().Be("Real");
    }

    [Fact]
    public void UnclosedFenceSuppressesAllFollowingHeadings()
    {
        const string source = "# Real\n```\n# Fake\n## AlsoFake\n";
        MarkdownDocument document = MarkdownDocument.Parse(source);
        document.Headings.Should().ContainSingle();
    }

    [Fact]
    public void FenceMustBeClosedByAtLeastAsManyMarkers()
    {
        const string source = "````\n# Fake\n```\n# StillFake\n````\n# Real\n";
        MarkdownDocument document = MarkdownDocument.Parse(source);
        document.Headings.Should().ContainSingle();
        document.Headings[0].Text.Should().Be("Real");
    }

    [Fact]
    public void FewerThanThreeBackticksIsNotAFence()
    {
        MarkdownDocument document = MarkdownDocument.Parse("``\n# Real\n");
        document.Headings.Should().ContainSingle();
    }

    [Fact]
    public void SetextUnderlineIsNotTreatedAsAHeading()
    {
        MarkdownDocument document = MarkdownDocument.Parse("Title\n=====\n");
        document.Headings.Should().BeEmpty();
    }

    [Fact]
    public void IndentedFenceDoesNotOpen()
    {
        MarkdownDocument document = MarkdownDocument.Parse("    ```\n# Real\n");
        document.Headings.Should().ContainSingle();
    }

    [Fact]
    public void TextOutsideTheSubsetIsTreatedAsOrdinaryText()
    {
        MarkdownDocument document = MarkdownDocument.Parse("Just prose.\n\nMore prose.\n");
        document.Headings.Should().BeEmpty();
    }

    [Fact]
    public void EmptyDocumentHasNoHeadings()
    {
        MarkdownDocument.Parse(string.Empty).Headings.Should().BeEmpty();
    }

    [Fact]
    public void NullSourceIsRejected()
    {
        FluentActions.Invoking(() => MarkdownDocument.Parse(null!)).Should().ThrowExactly<ArgumentNullException>();
    }
}