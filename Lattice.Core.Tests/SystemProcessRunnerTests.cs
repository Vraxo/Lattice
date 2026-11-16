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
        outcome.TimedOut.Should().BeFalse();
        outcome.ExitCode.Should().Be(0);
        string.IsNullOrWhiteSpace(outcome.StandardOutput).Should().BeFalse();
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
        outcome.TimedOut.Should().BeFalse();
        outcome.ExitCode.Should().NotBe(0);
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
        outcome.Cancelled.Should().BeTrue();
        outcome.TimedOut.Should().BeFalse();
    }

    [Fact]
    public void CancellationIsReportedWithoutThrowing()
    {
        using TempWorkspace workspace = new();
        SystemProcessRunner runner = new();
        using CancellationTokenSource source = new();

        // The delay must be far shorter than the process runtime, not longer. Cancellation has to
        // land before the process exits; starting any process takes more than a millisecond, so
        // this reliably exercises the kill-during-read path without racing the command's speed.
        source.CancelAfter(TimeSpan.FromMilliseconds(1));
        ProcessOutcome outcome = runner.Run(
            "dotnet",
            ["--info"],
            workspace.Path,
            TimeSpan.FromSeconds(60),
            source.Token);
        outcome.Cancelled.Should().BeTrue();
        outcome.TimedOut.Should().BeFalse();
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
        outcome.TimedOut.Should().BeTrue();
        outcome.ExitCode.Should().Be(-1);
    }
}