namespace Lattice.Core.Tests;

public sealed class CalculatorToolTests
{
    private readonly CalculatorTool _tool = new();

    [Fact]
    public void DescriptorIdentifiesCalculator()
    {
        Assert.Equal(new ToolId("calculator"), _tool.Descriptor.Id);
        Assert.Equal(ToolSideEffect.ReadOnly, _tool.Descriptor.SideEffect);
    }

    [Fact]
    public void AddsTwoIntegers()
    {
        ArgumentBag arguments = ArgumentBag.From(
        [
            new ArgumentEntry("left", ArgumentValue.FromInteger(2)),
            new ArgumentEntry("right", ArgumentValue.FromInteger(3)),
        ]);
        ToolResult result = _tool.Execute(arguments);
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Output!.AsInteger());
    }

    [Fact]
    public void MissingOperandFails()
    {
        ArgumentBag arguments = ArgumentBag.From([new ArgumentEntry("left", ArgumentValue.FromInteger(2))]);
        ToolResult result = _tool.Execute(arguments);
        Assert.False(result.IsSuccess);
        Assert.Equal("calculator.right", result.Error!.Code);
    }

    [Fact]
    public void WrongOperandTypeFails()
    {
        ArgumentBag arguments = ArgumentBag.From(
        [
            new ArgumentEntry("left", ArgumentValue.FromString("two")),
            new ArgumentEntry("right", ArgumentValue.FromInteger(3)),
        ]);
        ToolResult result = _tool.Execute(arguments);
        Assert.False(result.IsSuccess);
        Assert.Equal("calculator.left", result.Error!.Code);
    }
}