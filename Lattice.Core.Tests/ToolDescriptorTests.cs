namespace Lattice.Core.Tests;
public sealed class ToolDescriptorTests
{
    [Fact]
    public void RejectsEmptyDescription()
    {
        Assert.Throws<ArgumentException>(
            () => new ToolDescriptor(new ToolId("calculator"), string.Empty, "out", ToolSideEffect.ReadOnly));
    }
    [Fact]
    public void RejectsEmptyOutputDescription()
    {
        Assert.Throws<ArgumentException>(
            () => new ToolDescriptor(new ToolId("calculator"), "desc", string.Empty, ToolSideEffect.ReadOnly));
    }
    [Fact]
    public void RejectsDuplicateParameterNames()
    {
        var parameters = new[]
        {
            new ToolParameter("a", ToolParameterType.Integer),
            new ToolParameter("a", ToolParameterType.Integer),
        };
        Assert.Throws<ArgumentException>(() => CreateDescriptor(parameters));
    }
    [Fact]
    public void DescriptorsWithSameParametersAreEqual()
    {
        var first = CreateDescriptor(new[] { new ToolParameter("a", ToolParameterType.Integer) });
        var second = CreateDescriptor(new[] { new ToolParameter("a", ToolParameterType.Integer) });
        Assert.Equal(first, second);
    }
    [Fact]
    public void DescriptorsWithDifferentSideEffectAreNotEqual()
    {
        var readOnly = CreateDescriptor();
        var write = new ToolDescriptor(
            new ToolId("calculator"), "Adds two integers.", "An integer sum.", ToolSideEffect.LocalWrite);
        Assert.NotEqual(readOnly, write);
    }
    [Fact]
    public void DefaultLimitsAreApplied()
    {
        Assert.Equal(ToolExecutionLimits.Default, CreateDescriptor().Limits);
    }
    private static ToolDescriptor CreateDescriptor(IEnumerable<ToolParameter>? parameters = null) =>
        new(new ToolId("calculator"), "Adds two integers.", "An integer sum.", ToolSideEffect.ReadOnly, parameters);
}