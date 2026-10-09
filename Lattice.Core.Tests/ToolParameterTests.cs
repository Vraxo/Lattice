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
        var first = new ToolParameter("a", ToolParameterType.Integer);
        var second = new ToolParameter("a", ToolParameterType.Integer);
        Assert.Equal(first, second);
    }
}