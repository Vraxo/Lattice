namespace Lattice.Core.Tests;

public sealed class ToolIdTests
{
    [Fact]
    public void RejectsEmptyValue()
    {
        Assert.Throws<ArgumentException>(() => new ToolId(string.Empty));
    }

    [Fact]
    public void IdsWithSameValueAreEqual()
    {
        Assert.Equal(new ToolId("calculator"), new ToolId("calculator"));
    }

    [Fact]
    public void IdsWithDifferentValueAreNotEqual()
    {
        Assert.NotEqual(new ToolId("calculator"), new ToolId("clock"));
    }
}