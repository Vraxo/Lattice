namespace Lattice.Core.Tests;

public sealed class CalculatorToolTests
{
    private readonly CalculatorTool _tool = new();

    [Fact]
    public void DescriptorIdentifiesCalculator()
    {
        _tool.Descriptor.Id.Should().Be(new ToolId("calculator"));
        _tool.Descriptor.SideEffect.Should().Be(ToolSideEffect.ReadOnly);
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
        result.IsSuccess.Should().BeTrue();
        result.Output!.AsInteger().Should().Be(5);
    }

    [Fact]
    public void MissingOperandFails()
    {
        ArgumentBag arguments = ArgumentBag.From([new ArgumentEntry("left", ArgumentValue.FromInteger(2))]);
        ToolResult result = _tool.Execute(arguments);
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("calculator.right");
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
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("calculator.left");
    }
}