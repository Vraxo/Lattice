namespace Lattice.Core.Tests;

public sealed class SystemProcessRunnerTests
{
    [Fact]
    public void RunsRealProcessAndCapturesOutput()
    {
        using TempWorkspace workspace = new();
        SystemProcessRunner runner = new();

        // `dotnet --version` is guaranteed present: the tests themselves run under it.
        ProcessOutcome outcome = runner.Run(
            "dotnet",
            ["--version"],
            workspace.Path,
            TimeSpan.FromSeconds(60));
        Assert.False(outcome.TimedOut);
        Assert.Equal(0, outcome.ExitCode);
        Assert.False(string.IsNullOrWhiteSpace(outcome.StandardOutput));
    }

    [Fact]
    public void NonZeroExitCodeIsReported()
    {
        using TempWorkspace workspace = new();
        SystemProcessRunner runner = new();
        ProcessOutcome outcome = runner.Run(
            "dotnet",
            ["--definitely-not-a-real-option"],
            workspace.Path,
            TimeSpan.FromSeconds(60));
        Assert.False(outcome.TimedOut);
        Assert.NotEqual(0, outcome.ExitCode);
    }

    [Fact]
    public void PreCancelledTokenTerminatesWithoutRunning()
    {
        using TempWorkspace workspace = new();
        SystemProcessRunner runner = new();
        using CancellationTokenSource source = new();
        source.Cancel();
        ProcessOutcome outcome = runner.Run(
            "dotnet",
            ["--info"],
            workspace.Path,
            TimeSpan.FromSeconds(60),
            source.Token);
        Assert.True(outcome.Cancelled);
        Assert.False(outcome.TimedOut);
    }

    [Fact]
    public void CancellationDuringExecutionIsReportedWithoutThrowing()
    {
        using TempWorkspace workspace = new();
        SystemProcessRunner runner = new();
        using CancellationTokenSource source = new();
        // Cancel shortly after the process starts, so the kill happens mid-flight. This is the
        // path that faults the output reads; a pre-cancelled token never reaches it.
        source.CancelAfter(TimeSpan.FromMilliseconds(150));
        ProcessOutcome outcome = runner.Run(
            "dotnet",
            ["--info"],
            workspace.Path,
            TimeSpan.FromSeconds(60),
            source.Token);
        Assert.True(outcome.Cancelled);
        Assert.False(outcome.TimedOut);
    }
    [Fact]
    public void TimeoutTerminatesTheProcess()
    {
        using TempWorkspace workspace = new();
        SystemProcessRunner runner = new();

        // A dotnet process doing nothing useful will not finish inside 200ms.
        ProcessOutcome outcome = runner.Run(
            "dotnet",
            ["--info"],
            workspace.Path,
            TimeSpan.FromMilliseconds(1));
        Assert.True(outcome.TimedOut);
        Assert.Equal(-1, outcome.ExitCode);
    }
}