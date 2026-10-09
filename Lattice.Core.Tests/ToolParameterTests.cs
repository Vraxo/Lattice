namespace Lattice.Core.Tests;

public sealed class ToolParameterTests
{
    [Fact]
    public void RejectsEmptyName()
    {
        Assert.Throws<ArgumentException>(() => new ToolParameter(string.Empty, ToolParameterType.String));
    }

    [Fact]
    public void ParametersWithSameComponentsAreEqual()
    {
        ToolParameter first = new("a", ToolParameterType.Integer);
        ToolParameter second = new("a", ToolParameterType.Integer);
        Assert.Equal(first, second);
    }
}