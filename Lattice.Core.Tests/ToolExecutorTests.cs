namespace Lattice.Core.Tests;
public sealed class ToolExecutorTests
{
    [Fact]
    public void ValidArgumentsProduceSuccessInvocation()
    {
        var arguments = ArgumentBag.From(new[]
        {
            new ArgumentEntry("left", ArgumentValue.FromInteger(2)),
            new ArgumentEntry("right", ArgumentValue.FromInteger(3)),
        });
        var invocation = ToolExecutor.Execute(new CalculatorTool(), arguments, ToolPermissionPolicy.ReadOnlyOnly);
        Assert.True(invocation.Result.IsSuccess);
        Assert.Equal(5, invocation.Result.Output!.AsInteger());
        Assert.Equal(CalculatorTool.Id, invocation.ToolId);
    }
    [Fact]
    public void InvalidArgumentsProduceFailureAndDoNotInvokeTool()
    {
        var tool = new RecordingTool();
        var arguments = ArgumentBag.Empty;
        var invocation = ToolExecutor.Execute(tool, arguments, ToolPermissionPolicy.ReadOnlyOnly);
        Assert.False(invocation.Result.IsSuccess);
        Assert.False(tool.WasExecuted);
    }
    [Fact]
    public void RuntimeToolFailureIsRecorded()
    {
        var invocation = ToolExecutor.Execute(new FailingTool(), ArgumentBag.Empty, ToolPermissionPolicy.ReadOnlyOnly);
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal("failing.tool", invocation.Result.Error!.Code);
    }
    [Fact]
    public void DeniedToolDoesNotReachImplementation()
    {
        var tool = new WriteRecordingTool();
        var invocation = ToolExecutor.Execute(tool, ArgumentBag.Empty, ToolPermissionPolicy.ReadOnlyOnly);
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal("tool.permission.denied", invocation.Result.Error!.Code);
        Assert.False(tool.WasExecuted);
    }
    [Fact]
    public void AllowedNonReadOnlyToolReachesImplementation()
    {
        var tool = new WriteRecordingTool();
        var invocation = ToolExecutor.Execute(tool, ArgumentBag.Empty, ToolPermissionPolicy.AllowAll);
        Assert.True(invocation.Result.IsSuccess);
        Assert.True(tool.WasExecuted);
    }
}