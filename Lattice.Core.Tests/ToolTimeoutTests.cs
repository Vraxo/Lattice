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
            parsed.IsSuccess.Should().BeTrue("{0}", parsed.Error?.Message ?? string.Empty);

            AgentTurnResult result = loop.Run(Session.Empty(SessionId.New()), parsed.Value);

            result.Outcome.Should().Be(TurnOutcome.ToolTimedOut);
            result.Invocation!.Result.Error!.Code.Should().Be(ToolErrorCodes.TimedOut);
            tool.WasExecuted.Should().BeTrue();
            tool.CancellationObserved.Should().BeTrue();
            result.Response.Should().ContainEquivalentOf("timeout");
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

        started.Should().BeTrue("{0}", "Tool did not start." ?? string.Empty);
        invocation.Result.Error!.Code.Should().Be(ToolErrorCodes.Cancelled);
        tool.CancellationObserved.Should().BeTrue();
    }
}
