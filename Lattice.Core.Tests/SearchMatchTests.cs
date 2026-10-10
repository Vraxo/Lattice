namespace Lattice.Core.Tests;

public sealed class SearchMatchTests
{
    [Fact]
    public void RoundTripsThroughToString()
    {
        SearchMatch match = new("a/b.txt", 3, "some text");
        Assert.True(SearchMatch.TryParse(match.ToString(), out SearchMatch? parsed));
        Assert.Equal(match, parsed);
    }

    [Fact]
    public void ParsesPathWithDirectories()
    {
        Assert.True(SearchMatch.TryParse("src/deep/file.cs:12: content", out SearchMatch? match));
        Assert.Equal("src/deep/file.cs", match.RelativePath);
        Assert.Equal(12, match.LineNumber);
        Assert.Equal("content", match.LineText);
    }

    [Fact]
    public void ParsesEmptyLineText()
    {
        Assert.True(SearchMatch.TryParse("a.txt:1: ", out SearchMatch? match));
        Assert.Equal(string.Empty, match.LineText);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no colon here")]
    [InlineData("a.txt:notanumber: text")]
    [InlineData("a.txt:1:no space after colon")]
    public void RejectsMalformedLines(string? line)
    {
        Assert.False(SearchMatch.TryParse(line, out _));
    }

    [Fact]
    public void RejectsNonPositiveLineNumber()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SearchMatch("a.txt", 0, "x"));
    }
}