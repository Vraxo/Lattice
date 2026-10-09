namespace Lattice.Core.Tests;
public sealed class ToolRegistryTests
{
    [Fact]
    public void ResolvesRegisteredTool()
    {
        var registry = new ToolRegistry(new ITool[] { new CalculatorTool() });
        Assert.True(registry.TryResolve(CalculatorTool.Id, out var tool));
        Assert.IsType<CalculatorTool>(tool);
    }
    [Fact]
    public void UnknownToolFailsToResolve()
    {
        var registry = new ToolRegistry(new ITool[] { new CalculatorTool() });
        Assert.False(registry.TryResolve(new ToolId("missing"), out _));
    }
    [Fact]
    public void RejectsDuplicateToolIds()
    {
        var tools = new ITool[] { new CalculatorTool(), new CalculatorTool() };
        Assert.Throws<ArgumentException>(() => new ToolRegistry(tools));
    }
    [Fact]
    public void ToCatalogIncludesAllDescriptors()
    {
        var registry = new ToolRegistry(new ITool[] { new CalculatorTool(), new FailingTool() });
        var catalog = registry.ToCatalog();
        Assert.Equal(2, catalog.Descriptors.Length);
    }
}