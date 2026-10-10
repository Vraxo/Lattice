namespace Lattice.Core.Tests;

public sealed class ToolRegistryTests
{
    [Fact]
    public void ResolvesRegisteredTool()
    {
        ToolRegistry registry = new([new CalculatorTool()]);
        Assert.True(registry.TryResolve(CalculatorTool.Id, out ITool? tool));
        Assert.IsType<CalculatorTool>(tool);
    }

    [Fact]
    public void UnknownToolFailsToResolve()
    {
        ToolRegistry registry = new([new CalculatorTool()]);
        Assert.False(registry.TryResolve(new ToolId("missing"), out _));
    }

    [Fact]
    public void RejectsDuplicateToolIds()
    {
        ITool[] tools = [new CalculatorTool(), new CalculatorTool()];
        Assert.Throws<ArgumentException>(() => new ToolRegistry(tools));
    }

    [Fact]
    public void ToCatalogIncludesAllDescriptors()
    {
        ToolRegistry registry = new([new CalculatorTool(), new FailingTool()]);
        ToolCatalog catalog = registry.ToCatalog();
        Assert.Equal(2, catalog.Descriptors.Length);
    }
}