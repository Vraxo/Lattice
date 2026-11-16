namespace Lattice.Core.Tests;

public sealed class ToolDescriptorTests
{
    [Fact]
    public void RejectsEmptyDescription()
    {
        FluentActions.Invoking(() => new ToolDescriptor(new ToolId("calculator"), string.Empty, "out", ToolSideEffect.ReadOnly)).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void RejectsEmptyOutputDescription()
    {
        FluentActions.Invoking(() => new ToolDescriptor(new ToolId("calculator"), "desc", string.Empty, ToolSideEffect.ReadOnly)).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void RejectsDuplicateParameterNames()
    {
        ToolParameter[] parameters =
        [
            new ToolParameter("a", ToolParameterType.Integer),
            new ToolParameter("a", ToolParameterType.Integer),
        ];
        FluentActions.Invoking(() => CreateDescriptor(parameters)).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void DescriptorsWithSameParametersAreEqual()
    {
        ToolDescriptor first = CreateDescriptor([new ToolParameter("a", ToolParameterType.Integer)]);
        ToolDescriptor second = CreateDescriptor([new ToolParameter("a", ToolParameterType.Integer)]);
        second.Should().Be(first);
    }

    [Fact]
    public void DescriptorsWithDifferentSideEffectAreNotEqual()
    {
        ToolDescriptor readOnly = CreateDescriptor();
        ToolDescriptor write = new(
            new ToolId("calculator"), "Adds two integers.", "An integer sum.", ToolSideEffect.LocalWrite);
        write.Should().NotBe(readOnly);
    }

    [Fact]
    public void DefaultLimitsAreApplied()
    {
        CreateDescriptor().Limits.Should().Be(ToolExecutionLimits.Default);
    }

    private static ToolDescriptor CreateDescriptor(IEnumerable<ToolParameter>? parameters = null)
    {
        return new(new ToolId("calculator"), "Adds two integers.", "An integer sum.", ToolSideEffect.ReadOnly, parameters);
    }
}