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