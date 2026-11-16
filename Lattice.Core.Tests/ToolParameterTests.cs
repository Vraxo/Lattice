namespace Lattice.Core.Tests;

public sealed class ToolParameterTests
{
    [Fact]
    public void RejectsEmptyName()
    {
        FluentActions.Invoking(() => new ToolParameter(string.Empty, ToolParameterType.String)).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void ParametersWithSameComponentsAreEqual()
    {
        ToolParameter first = new("a", ToolParameterType.Integer);
        ToolParameter second = new("a", ToolParameterType.Integer);
        second.Should().Be(first);
    }
}