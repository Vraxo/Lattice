namespace Lattice.Core.Tests;

public sealed class SearchMatchTests
{
    [Fact]
    public void RoundTripsThroughToString()
    {
        SearchMatch match = new("a/b.txt", 3, "some text");
        SearchMatch.TryParse(match.ToString(), out SearchMatch? parsed).Should().BeTrue();
        parsed.Should().Be(match);
    }

    [Fact]
    public void ParsesPathWithDirectories()
    {
        SearchMatch.TryParse("src/deep/file.cs:12: content", out SearchMatch? match).Should().BeTrue();
        match.RelativePath.Should().Be("src/deep/file.cs");
        match.LineNumber.Should().Be(12);
        match.LineText.Should().Be("content");
    }

    [Fact]
    public void ParsesEmptyLineText()
    {
        SearchMatch.TryParse("a.txt:1: ", out SearchMatch? match).Should().BeTrue();
        match.LineText.Should().Be(string.Empty);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no colon here")]
    [InlineData("a.txt:notanumber: text")]
    [InlineData("a.txt:1:no space after colon")]
    public void RejectsMalformedLines(string? line)
    {
        SearchMatch.TryParse(line, out _).Should().BeFalse();
    }

    [Fact]
    public void RejectsNonPositiveLineNumber()
    {
        FluentActions.Invoking(() => new SearchMatch("a.txt", 0, "x")).Should().ThrowExactly<ArgumentOutOfRangeException>();
    }
}