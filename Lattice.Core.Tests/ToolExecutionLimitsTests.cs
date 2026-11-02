namespace Lattice.Core.Tests;

public sealed class ToolExecutionLimitsTests
{
    [Fact]
    public void RejectsNonPositiveTimeout()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ToolExecutionLimits(TimeSpan.Zero));
    }

    [Fact]
    public void RejectsTimeoutBeyondCancellationTimerRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ToolExecutionLimits(TimeSpan.FromDays(60)));
    }

    [Fact]
    public void DefaultHasPositiveTimeout()
    {
        Assert.True(ToolExecutionLimits.Default.Timeout > TimeSpan.Zero);
    }
}
