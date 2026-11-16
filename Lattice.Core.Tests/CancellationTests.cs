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
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be(ToolErrorCodes.Cancelled);
        tool.WasExecuted.Should().BeFalse();
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
        invocation.Result.IsSuccess.Should().BeTrue();
        tool.WasExecuted.Should().BeTrue();
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
        invocation.Result.Error!.Code.Should().Be("tool.permission.denied");
        tool.WasExecuted.Should().BeFalse();
    }

    [Fact]
    public void CancellationDoesNotApplyControlledStateChanges()
    {
        AgentLoop loop = new(
            new ToolRegistry([new RecordingTool()]),
            ToolPermissionPolicy.ReadOnlyOnly);
        Result<IControlledStatement> parsed = ControlledLanguageParser.Parse("fact water | boils_at | 100C");
        parsed.IsSuccess.Should().BeTrue();
        Session original = Session.Empty(SessionId.New());
        using CancellationTokenSource source = new();
        source.Cancel();

        AgentTurnResult result = loop.Run(original, parsed.Value, source.Token);

        result.Outcome.Should().Be(TurnOutcome.Cancelled);
        result.Session.Should().BeSameAs(original);
        result.Session.Facts.Should().BeEmpty();
    }

    [Fact]
    public void AgentLoopReportsCancelledOutcome()
    {
        AgentLoop loop = new(
            new ToolRegistry([new RecordingTool()]),
            ToolPermissionPolicy.ReadOnlyOnly);
        Result<IControlledStatement> parsed = ControlledLanguageParser.Parse("action recording | value=1");
        parsed.IsSuccess.Should().BeTrue();
        using CancellationTokenSource source = new();
        source.Cancel();
        AgentTurnResult result = loop.Run(Session.Empty(SessionId.New()), parsed.Value, source.Token);
        result.Outcome.Should().Be(TurnOutcome.Cancelled);
    }
}
