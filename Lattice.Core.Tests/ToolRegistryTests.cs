namespace Lattice.Core.Tests;

public sealed class ToolRegistryTests
{
    [Fact]
    public void ResolvesRegisteredTool()
    {
        ToolRegistry registry = new([new CalculatorTool()]);
        registry.TryResolve(CalculatorTool.Id, out ITool? tool).Should().BeTrue();
        tool.Should().BeOfType<CalculatorTool>();
    }

    [Fact]
    public void UnknownToolFailsToResolve()
    {
        ToolRegistry registry = new([new CalculatorTool()]);
        registry.TryResolve(new ToolId("missing"), out _).Should().BeFalse();
    }

    [Fact]
    public void RejectsDuplicateToolIds()
    {
        ITool[] tools = [new CalculatorTool(), new CalculatorTool()];
        FluentActions.Invoking(() => new ToolRegistry(tools)).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void ToCatalogIncludesAllDescriptors()
    {
        ToolRegistry registry = new([new CalculatorTool(), new FailingTool()]);
        ToolCatalog catalog = registry.ToCatalog();
        catalog.Descriptors.Length.Should().Be(2);
    }
}