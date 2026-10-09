namespace Lattice.Core.Tests;
public sealed class ObservationTests
{
    [Fact]
    public void RejectsEmptyContent()
    {
        Assert.Throws<ArgumentException>(() => new Observation(string.Empty, "tool:x"));
    }
    [Fact]
    public void RejectsEmptySource()
    {
        Assert.Throws<ArgumentException>(() => new Observation("content", string.Empty));
    }
    [Fact]
    public void ObservationsWithSameContentAndSourceAreEqual()
    {
        var first = new Observation("exit code 0", "tool:x");
        var second = new Observation("exit code 0", "tool:x");
        Assert.Equal(first, second);
    }
}