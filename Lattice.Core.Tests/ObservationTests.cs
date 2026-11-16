namespace Lattice.Core.Tests;

public sealed class ObservationTests
{
    [Fact]
    public void RejectsEmptyContent()
    {
        FluentActions.Invoking(() => new Observation(string.Empty, "tool:x")).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void RejectsEmptySource()
    {
        FluentActions.Invoking(() => new Observation("content", string.Empty)).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void ObservationsWithSameContentAndSourceAreEqual()
    {
        Observation first = new("exit code 0", "tool:x");
        Observation second = new("exit code 0", "tool:x");
        second.Should().Be(first);
    }
}