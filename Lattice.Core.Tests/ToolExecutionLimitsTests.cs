namespace Lattice.Core.Tests;

public sealed class ToolExecutionLimitsTests
{
    [Fact]
    public void RejectsNonPositiveTimeout()
    {
        FluentActions.Invoking(() => new ToolExecutionLimits(TimeSpan.Zero)).Should().ThrowExactly<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RejectsTimeoutBeyondCancellationTimerRange()
    {
        FluentActions.Invoking(() => new ToolExecutionLimits(TimeSpan.FromDays(60))).Should().ThrowExactly<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DefaultHasPositiveTimeout()
    {
        (ToolExecutionLimits.Default.Timeout > TimeSpan.Zero).Should().BeTrue();
    }
}
