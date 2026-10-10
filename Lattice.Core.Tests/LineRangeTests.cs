namespace Lattice.Core.Tests;
public sealed class LineRangeTests
{
    [Fact]
    public void StoresStartAndEnd()
    {
        LineRange range = new(2, 5);
        Assert.Equal(2, range.Start);
        Assert.Equal(5, range.End);
        Assert.Equal(3, range.Length);
        Assert.False(range.IsEmpty);
    }
    [Fact]
    public void EmptyRangeHasZeroLength()
    {
        Assert.True(LineRange.At(4).IsEmpty);
        Assert.True(LineRange.Empty.IsEmpty);
    }
    [Fact]
    public void NegativeStartIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LineRange(-1, 0));
    }
    [Fact]
    public void EndBeforeStartIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LineRange(3, 2));
    }
    [Fact]
    public void FromOneBasedLinesConvertsToEndExclusiveRange()
    {
        LineRange range = LineRange.FromOneBasedLines(2, 3);
        Assert.Equal(1, range.Start);
        Assert.Equal(3, range.End);
    }
    [Fact]
    public void FromOneBasedLineCoversASingleLine()
    {
        LineRange range = LineRange.FromOneBasedLine(5);
        Assert.Equal(4, range.Start);
        Assert.Equal(5, range.End);
        Assert.Equal(1, range.Length);
    }
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    public void OneBasedConversionRejectsNonPositiveFirstLine(int first, int last)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LineRange.FromOneBasedLines(first, last));
    }
    [Fact]
    public void OneBasedConversionRejectsInvertedSpan()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LineRange.FromOneBasedLines(5, 3));
    }
    [Fact]
    public void RangesWithSameBoundsAreEqual()
    {
        Assert.Equal(new LineRange(1, 4), new LineRange(1, 4));
    }
}