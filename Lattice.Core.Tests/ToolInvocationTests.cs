namespace Lattice.Core.Tests;

public sealed class ToolInvocationTests
{
    [Fact]
    public void CarriesToolIdArgumentsAndResult()
    {
        ArgumentBag arguments = ArgumentBag.From([new ArgumentEntry("left", ArgumentValue.FromInteger(1))]);
        ToolResult result = ToolResult.Success(ArgumentValue.FromInteger(1));
        ToolInvocation invocation = new(CalculatorTool.Id, arguments, result);
        invocation.ToolId.Should().Be(CalculatorTool.Id);
        invocation.Arguments.Should().Be(arguments);
        invocation.Result.Should().Be(result);
    }

    [Fact]
    public void RejectsNullArguments()
    {
        FluentActions.Invoking(() => new ToolInvocation(CalculatorTool.Id, null!, ToolResult.Success(ArgumentValue.FromInteger(1)))).Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void InvocationsWithSameComponentsAreEqual()
    {
        ToolInvocation first = new(CalculatorTool.Id, ArgumentBag.Empty, ToolResult.Success(ArgumentValue.FromInteger(1)));
        ToolInvocation second = new(CalculatorTool.Id, ArgumentBag.Empty, ToolResult.Success(ArgumentValue.FromInteger(1)));
        second.Should().Be(first);
    }
}