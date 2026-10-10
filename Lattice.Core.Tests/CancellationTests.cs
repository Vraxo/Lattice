namespace Lattice.Core.Tests;

public sealed class CancellationTests
{
    [Fact]
    public void CancelledTokenPreventsToolExecution()
    {
        RecordingTool tool = new();
        using CancellationTokenSource source = new();
        source.Cancel();
        ToolInvocation invocation = ToolExecutor.Execute(
            tool,
            ArgumentBag.From([new ArgumentEntry("value", ArgumentValue.FromInteger(1))]),
            ToolPermissionPolicy.ReadOnlyOnly,
            source.Token);
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal(ToolErrorCodes.Cancelled, invocation.Result.Error!.Code);
        Assert.False(tool.WasExecuted);
    }

    [Fact]
    public void UncancelledTokenExecutesNormally()
    {
        RecordingTool tool = new();
        ToolInvocation invocation = ToolExecutor.Execute(
            tool,
            ArgumentBag.From([new ArgumentEntry("value", ArgumentValue.FromInteger(1))]),
            ToolPermissionPolicy.ReadOnlyOnly,
            CancellationToken.None);
        Assert.True(invocation.Result.IsSuccess);
        Assert.True(tool.WasExecuted);
    }

    [Fact]
    public void CancellationIsCheckedAfterPermissionAndValidation()
    {
        // A denied tool must report denial, not cancellation, even when the token is cancelled:
        // the executor's checks run in a fixed order.
        WriteRecordingTool tool = new();
        using CancellationTokenSource source = new();
        source.Cancel();
        ToolInvocation invocation = ToolExecutor.Execute(
            tool,
            ArgumentBag.Empty,
            ToolPermissionPolicy.ReadOnlyOnly,
            source.Token);
        Assert.Equal("tool.permission.denied", invocation.Result.Error!.Code);
        Assert.False(tool.WasExecuted);
    }

    [Fact]
    public void AgentLoopReportsCancelledOutcome()
    {
        AgentLoop loop = new(
            new ToolRegistry([new RecordingTool()]),
            ToolPermissionPolicy.ReadOnlyOnly);
        Result<IControlledStatement> parsed = ControlledLanguageParser.Parse("action recording | value=1");
        Assert.True(parsed.IsSuccess);
        using CancellationTokenSource source = new();
        source.Cancel();
        AgentTurnResult result = loop.Run(Session.Empty(SessionId.New()), parsed.Value, source.Token);
        Assert.Equal(TurnOutcome.Cancelled, result.Outcome);
    }
}