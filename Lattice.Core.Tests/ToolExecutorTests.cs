namespace Lattice.Core.Tests;

public sealed class ToolExecutorTests
{
    [Fact]
    public void ValidArgumentsProduceSuccessInvocation()
    {
        ArgumentBag arguments = ArgumentBag.From(
        [
            new ArgumentEntry("left", ArgumentValue.FromInteger(2)),
            new ArgumentEntry("right", ArgumentValue.FromInteger(3)),
        ]);
        ToolInvocation invocation = ToolExecutor.Execute(new CalculatorTool(), arguments, ToolPermissionPolicy.ReadOnlyOnly);
        invocation.Result.IsSuccess.Should().BeTrue();
        invocation.Result.Output!.AsInteger().Should().Be(5);
        invocation.ToolId.Should().Be(CalculatorTool.Id);
    }

    [Fact]
    public void InvalidArgumentsProduceFailureAndDoNotInvokeTool()
    {
        RecordingTool tool = new();
        ArgumentBag arguments = ArgumentBag.Empty;
        ToolInvocation invocation = ToolExecutor.Execute(tool, arguments, ToolPermissionPolicy.ReadOnlyOnly);
        invocation.Result.IsSuccess.Should().BeFalse();
        tool.WasExecuted.Should().BeFalse();
    }

    [Fact]
    public void RuntimeToolFailureIsRecorded()
    {
        ToolInvocation invocation = ToolExecutor.Execute(new FailingTool(), ArgumentBag.Empty, ToolPermissionPolicy.ReadOnlyOnly);
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be("failing.tool");
    }

    [Fact]
    public void DeniedToolDoesNotReachImplementation()
    {
        WriteRecordingTool tool = new();
        ToolInvocation invocation = ToolExecutor.Execute(tool, ArgumentBag.Empty, ToolPermissionPolicy.ReadOnlyOnly);
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be("tool.permission.denied");
        tool.WasExecuted.Should().BeFalse();
    }

    [Fact]
    public void AllowedNonReadOnlyToolReachesImplementation()
    {
        WriteRecordingTool tool = new();
        ToolInvocation invocation = ToolExecutor.Execute(tool, ArgumentBag.Empty, ToolPermissionPolicy.AllowAll);
        invocation.Result.IsSuccess.Should().BeTrue();
        tool.WasExecuted.Should().BeTrue();
    }
}