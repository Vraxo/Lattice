namespace Lattice.Core.Tests;

public sealed class ToolIdTests
{
    [Fact]
    public void RejectsEmptyValue()
    {
        FluentActions.Invoking(() => new ToolId(string.Empty)).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void IdsWithSameValueAreEqual()
    {
        new ToolId("calculator").Should().Be(new ToolId("calculator"));
    }

    [Fact]
    public void IdsWithDifferentValueAreNotEqual()
    {
        new ToolId("clock").Should().NotBe(new ToolId("calculator"));
    }
}