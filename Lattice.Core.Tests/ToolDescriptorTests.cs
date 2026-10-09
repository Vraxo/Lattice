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
        ToolParameter[] parameters =
        [
            new ToolParameter("a", ToolParameterType.Integer),
            new ToolParameter("a", ToolParameterType.Integer),
        ];
        Assert.Throws<ArgumentException>(() => CreateDescriptor(parameters));
    }

    [Fact]
    public void DescriptorsWithSameParametersAreEqual()
    {
        ToolDescriptor first = CreateDescriptor([new ToolParameter("a", ToolParameterType.Integer)]);
        ToolDescriptor second = CreateDescriptor([new ToolParameter("a", ToolParameterType.Integer)]);
        Assert.Equal(first, second);
    }

    [Fact]
    public void DescriptorsWithDifferentSideEffectAreNotEqual()
    {
        ToolDescriptor readOnly = CreateDescriptor();
        ToolDescriptor write = new(
            new ToolId("calculator"), "Adds two integers.", "An integer sum.", ToolSideEffect.LocalWrite);
        Assert.NotEqual(readOnly, write);
    }

    [Fact]
    public void DefaultLimitsAreApplied()
    {
        Assert.Equal(ToolExecutionLimits.Default, CreateDescriptor().Limits);
    }

    private static ToolDescriptor CreateDescriptor(IEnumerable<ToolParameter>? parameters = null)
    {
        return new(new ToolId("calculator"), "Adds two integers.", "An integer sum.", ToolSideEffect.ReadOnly, parameters);
    }
}