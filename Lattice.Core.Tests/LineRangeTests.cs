namespace Lattice.Core.Tests;

public sealed class LineRangeTests
{
    [Fact]
    public void StoresStartAndEnd()
    {
        LineRange range = new(2, 5);
        range.Start.Should().Be(2);
        range.End.Should().Be(5);
        range.Length.Should().Be(3);
        range.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void EmptyRangeHasZeroLength()
    {
        LineRange.At(4).IsEmpty.Should().BeTrue();
        LineRange.Empty.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void NegativeStartIsRejected()
    {
        FluentActions.Invoking(() => new LineRange(-1, 0)).Should().ThrowExactly<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void EndBeforeStartIsRejected()
    {
        FluentActions.Invoking(() => new LineRange(3, 2)).Should().ThrowExactly<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FromOneBasedLinesConvertsToEndExclusiveRange()
    {
        LineRange range = LineRange.FromOneBasedLines(2, 3);
        range.Start.Should().Be(1);
        range.End.Should().Be(3);
    }

    [Fact]
    public void FromOneBasedLineCoversASingleLine()
    {
        LineRange range = LineRange.FromOneBasedLine(5);
        range.Start.Should().Be(4);
        range.End.Should().Be(5);
        range.Length.Should().Be(1);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    public void OneBasedConversionRejectsNonPositiveFirstLine(int first, int last)
    {
        FluentActions.Invoking(() => LineRange.FromOneBasedLines(first, last)).Should().ThrowExactly<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void OneBasedConversionRejectsInvertedSpan()
    {
        FluentActions.Invoking(() => LineRange.FromOneBasedLines(5, 3)).Should().ThrowExactly<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RangesWithSameBoundsAreEqual()
    {
        new LineRange(1, 4).Should().Be(new LineRange(1, 4));
    }
}