namespace Lattice.Core.Tests;
public sealed class ToolInvocationTests
{
    [Fact]
    public void CarriesToolIdArgumentsAndResult()
    {
        var arguments = ArgumentBag.From(new[] { new ArgumentEntry("left", ArgumentValue.FromInteger(1)) });
        var result = ToolResult.Success(ArgumentValue.FromInteger(1));
        var invocation = new ToolInvocation(CalculatorTool.Id, arguments, result);
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
        var first = new ToolInvocation(CalculatorTool.Id, ArgumentBag.Empty, ToolResult.Success(ArgumentValue.FromInteger(1)));
        var second = new ToolInvocation(CalculatorTool.Id, ArgumentBag.Empty, ToolResult.Success(ArgumentValue.FromInteger(1)));
        Assert.Equal(first, second);
    }
}