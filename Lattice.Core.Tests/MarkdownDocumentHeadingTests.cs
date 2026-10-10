namespace Lattice.Core.Tests;

public sealed class MarkdownDocumentHeadingTests
{
    [Fact]
    public void RecognizesHeadingsOfEachLevel()
    {
        const string source = "# One\n## Two\n### Three\n#### Four\n##### Five\n###### Six\n";
        MarkdownDocument document = MarkdownDocument.Parse(source);
        Assert.Equal(6, document.Headings.Length);
        Assert.Equal(1, document.Headings[0].Level);
        Assert.Equal(6, document.Headings[5].Level);
    }

    [Fact]
    public void StripsClosingHashes()
    {
        MarkdownDocument document = MarkdownDocument.Parse("## Section ##\n");
        Assert.Equal("Section", document.Headings[0].Text);
    }

    [Fact]
    public void KeepsHashThatIsPartOfTheText()
    {
        MarkdownDocument document = MarkdownDocument.Parse("# C#\n");
        Assert.Equal("C#", document.Headings[0].Text);
    }

    [Fact]
    public void HeadingWithNoTextIsRecognized()
    {
        MarkdownDocument document = MarkdownDocument.Parse("#\n");
        Assert.Single(document.Headings);
        Assert.Equal(string.Empty, document.Headings[0].Text);
    }

    [Fact]
    public void UpToThreeLeadingSpacesAreAllowed()
    {
        MarkdownDocument document = MarkdownDocument.Parse("   # Indented\n");
        Assert.Single(document.Headings);
        Assert.Equal("Indented", document.Headings[0].Text);
    }

    [Fact]
    public void FourLeadingSpacesIsIndentedCodeNotAHeading()
    {
        MarkdownDocument document = MarkdownDocument.Parse("    # Not a heading\n");
        Assert.Empty(document.Headings);
    }

    [Fact]
    public void SevenHashesIsNotAHeading()
    {
        MarkdownDocument document = MarkdownDocument.Parse("####### Too many\n");
        Assert.Empty(document.Headings);
    }

    [Fact]
    public void HashWithoutFollowingSpaceIsNotAHeading()
    {
        MarkdownDocument document = MarkdownDocument.Parse("#NotAHeading\n");
        Assert.Empty(document.Headings);
    }

    [Fact]
    public void HeadingsAreRecordedWithTheirLineIndexes()
    {
        MarkdownDocument document = MarkdownDocument.Parse("intro\n# First\nbody\n## Second\n");
        Assert.Equal(2, document.Headings.Length);
        Assert.Equal(1, document.Headings[0].LineIndex);
        Assert.Equal(3, document.Headings[1].LineIndex);
    }

    [Fact]
    public void HeadingInsideBacktickFenceIsIgnored()
    {
        const string source = "# Real\n```\n# Fake\n```\n# AlsoReal\n";
        MarkdownDocument document = MarkdownDocument.Parse(source);
        Assert.Equal(2, document.Headings.Length);
        Assert.Equal("Real", document.Headings[0].Text);
        Assert.Equal("AlsoReal", document.Headings[1].Text);
    }

    [Fact]
    public void HeadingInsideTildeFenceIsIgnored()
    {
        const string source = "~~~\n# Fake\n~~~\n# Real\n";
        MarkdownDocument document = MarkdownDocument.Parse(source);
        Assert.Single(document.Headings);
        Assert.Equal("Real", document.Headings[0].Text);
    }

    [Fact]
    public void UnclosedFenceSuppressesAllFollowingHeadings()
    {
        const string source = "# Real\n```\n# Fake\n## AlsoFake\n";
        MarkdownDocument document = MarkdownDocument.Parse(source);
        Assert.Single(document.Headings);
    }

    [Fact]
    public void FenceMustBeClosedByAtLeastAsManyMarkers()
    {
        const string source = "````\n# Fake\n```\n# StillFake\n````\n# Real\n";
        MarkdownDocument document = MarkdownDocument.Parse(source);
        Assert.Single(document.Headings);
        Assert.Equal("Real", document.Headings[0].Text);
    }

    [Fact]
    public void FewerThanThreeBackticksIsNotAFence()
    {
        MarkdownDocument document = MarkdownDocument.Parse("``\n# Real\n");
        Assert.Single(document.Headings);
    }

    [Fact]
    public void SetextUnderlineIsNotTreatedAsAHeading()
    {
        MarkdownDocument document = MarkdownDocument.Parse("Title\n=====\n");
        Assert.Empty(document.Headings);
    }

    [Fact]
    public void IndentedFenceDoesNotOpen()
    {
        MarkdownDocument document = MarkdownDocument.Parse("    ```\n# Real\n");
        Assert.Single(document.Headings);
    }

    [Fact]
    public void TextOutsideTheSubsetIsTreatedAsOrdinaryText()
    {
        MarkdownDocument document = MarkdownDocument.Parse("Just prose.\n\nMore prose.\n");
        Assert.Empty(document.Headings);
    }

    [Fact]
    public void EmptyDocumentHasNoHeadings()
    {
        Assert.Empty(MarkdownDocument.Parse(string.Empty).Headings);
    }

    [Fact]
    public void NullSourceIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => MarkdownDocument.Parse(null!));
    }
}