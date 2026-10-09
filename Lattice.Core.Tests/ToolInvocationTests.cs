namespace Lattice.Core.Tests;

public sealed class ToolInvocationTests
{
    [Fact]
    public void CarriesToolIdArgumentsAndResult()
    {
        ArgumentBag arguments = ArgumentBag.From([new ArgumentEntry("left", ArgumentValue.FromInteger(1))]);
        ToolResult result = ToolResult.Success(ArgumentValue.FromInteger(1));
        ToolInvocation invocation = new(CalculatorTool.Id, arguments, result);
        Assert.Equal(CalculatorTool.Id, invocation.ToolId);
        Assert.Equal(arguments, invocation.Arguments);
        Assert.Equal(result, invocation.Result);
    }

    [Fact]
    public void RejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(
            () => new ToolInvocation(CalculatorTool.Id, null!, ToolResult.Success(ArgumentValue.FromInteger(1))));
    }

    [Fact]
    public void InvocationsWithSameComponentsAreEqual()
    {
        ToolInvocation first = new(CalculatorTool.Id, ArgumentBag.Empty, ToolResult.Success(ArgumentValue.FromInteger(1)));
        ToolInvocation second = new(CalculatorTool.Id, ArgumentBag.Empty, ToolResult.Success(ArgumentValue.FromInteger(1)));
        Assert.Equal(first, second);
    }
}