namespace Lattice.Core.Tests;

public sealed class ToolTimeoutTests
{
    [Fact]
    public void RepeatedTimeoutsCancelCooperativeToolsAndReturnTypedOutcomes()
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            using CancellationAwareTool tool = new(TimeSpan.FromMilliseconds(30));
            ToolRegistry registry = new([tool]);
            AgentLoop loop = new(registry, ToolPermissionPolicy.ReadOnlyOnly);
            Result<IControlledStatement> parsed = ControlledLanguageParser.Parse("action cancellation-aware");
            Assert.True(parsed.IsSuccess, parsed.Error?.Message);

            AgentTurnResult result = loop.Run(Session.Empty(SessionId.New()), parsed.Value);

            Assert.Equal(TurnOutcome.ToolTimedOut, result.Outcome);
            Assert.Equal(ToolErrorCodes.TimedOut, result.Invocation!.Result.Error!.Code);
            Assert.True(tool.WasExecuted);
            Assert.True(tool.CancellationObserved);
            Assert.Contains("timeout", result.Response, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task CallerCancellationDuringExecutionIsNotReportedAsTimeout()
    {
        using CancellationAwareTool tool = new(TimeSpan.FromSeconds(5));
        using CancellationTokenSource source = new();
        ArgumentBag arguments = ArgumentBag.Empty;
        Task<ToolInvocation> execution = Task.Run(() => ToolExecutor.Execute(
            tool,
            arguments,
            ToolPermissionPolicy.ReadOnlyOnly,
            source.Token));

        bool started = tool.Started.WaitOne(TimeSpan.FromSeconds(2));
        source.Cancel();
        ToolInvocation invocation = await execution.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.True(started, "Tool did not start.");
        Assert.Equal(ToolErrorCodes.Cancelled, invocation.Result.Error!.Code);
        Assert.True(tool.CancellationObserved);
    }
}
