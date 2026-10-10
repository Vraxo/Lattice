namespace Lattice.Core.Tests;

public sealed class GuardedCommandToolTests : IDisposable
{
    private readonly TempWorkspace _defaultWorkspace = new();

    public void Dispose()
    {
        _defaultWorkspace.Dispose();
    }

    [Fact]
    public void AllowedCommandRunsAndReturnsOutput()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(0, "hello", string.Empty, timedOut: false));
        GuardedCommandTool tool = Create(tool: runner);
        ToolInvocation invocation = Run(tool, "greet", ToolPermissionPolicy.AllowAll);
        Assert.True(invocation.Result.IsSuccess);
        Assert.Contains("exited with code 0", invocation.Result.Output!.AsString(), StringComparison.Ordinal);
        Assert.Contains("hello", invocation.Result.Output.AsString(), StringComparison.Ordinal);
    }

    [Fact]
    public void CommandIsLookedUpByNameNotSuppliedAsAString()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(0, string.Empty, string.Empty, timedOut: false));
        GuardedCommandTool tool = Create(tool: runner);
        Run(tool, "greet", ToolPermissionPolicy.AllowAll);
        Assert.Equal("greeter", runner.LastExecutable);
        Assert.Equal(["--polite"], runner.LastArguments);
    }

    [Fact]
    public void NonZeroExitIsStillAResultNotAFailure()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(1, "out", "err", timedOut: false));
        GuardedCommandTool tool = Create(tool: runner);
        ToolInvocation invocation = Run(tool, "greet", ToolPermissionPolicy.AllowAll);

        // The command ran; its exit code is information, not a tool malfunction.
        Assert.True(invocation.Result.IsSuccess);
        Assert.Contains("exited with code 1", invocation.Result.Output!.AsString(), StringComparison.Ordinal);
        Assert.Contains("err", invocation.Result.Output.AsString(), StringComparison.Ordinal);
    }

    [Fact]
    public void TimeoutIsReportedAsFailure()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(-1, string.Empty, string.Empty, timedOut: true));
        GuardedCommandTool tool = Create(tool: runner);
        ToolInvocation invocation = Run(tool, "greet", ToolPermissionPolicy.AllowAll);
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal(CommandErrorCodes.CommandTimedOut, invocation.Result.Error!.Code);
    }

    [Fact]
    public void UnknownCommandNameIsRejected()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(0, string.Empty, string.Empty, timedOut: false));
        GuardedCommandTool tool = Create(tool: runner);
        ToolInvocation invocation = Run(tool, "rm-rf", ToolPermissionPolicy.AllowAll);
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal(CommandErrorCodes.CommandNotAllowed, invocation.Result.Error!.Code);
        Assert.Null(runner.LastExecutable);
    }

    [Fact]
    public void DeniedPolicyPreventsExecutionEntirely()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(0, string.Empty, string.Empty, timedOut: false));
        GuardedCommandTool tool = Create(tool: runner);
        ToolInvocation invocation = Run(tool, "greet", ToolPermissionPolicy.ReadOnlyOnly);
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal("tool.permission.denied", invocation.Result.Error!.Code);
        Assert.Null(runner.LastExecutable);
    }

    [Fact]
    public void MissingNameArgumentIsRejected()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(0, string.Empty, string.Empty, timedOut: false));
        GuardedCommandTool tool = Create(tool: runner);
        ToolInvocation invocation = ToolExecutor.Execute(
            tool,
            ArgumentBag.Empty,
            ToolPermissionPolicy.AllowAll);
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal("tool.argument.missing", invocation.Result.Error!.Code);
    }

    [Fact]
    public void RunnerFailureToStartIsReported()
    {
        GuardedCommandTool tool = Create(tool: new ThrowingProcessRunner());
        ToolInvocation invocation = Run(tool, "greet", ToolPermissionPolicy.AllowAll);
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal(CommandErrorCodes.CommandFailedToStart, invocation.Result.Error!.Code);
    }

    [Fact]
    public void ConfiguredTimeoutIsPassedToTheRunner()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(0, string.Empty, string.Empty, timedOut: false));
        GuardedCommandTool tool = Create(tool: runner, timeout: TimeSpan.FromSeconds(5));
        Run(tool, "greet", ToolPermissionPolicy.AllowAll);
        Assert.Equal(TimeSpan.FromSeconds(5), runner.LastTimeout);
    }

    [Fact]
    public void CommandRunsInTheWorkspaceRoot()
    {
        using TempWorkspace workspace = new();
        FakeProcessRunner runner = new(new ProcessOutcome(0, string.Empty, string.Empty, timedOut: false));
        GuardedCommandTool tool = Create(workspace, runner);
        ToolExecutor.Execute(tool, Name("greet"), ToolPermissionPolicy.AllowAll);
        Assert.Equal(workspace.Path, runner.LastWorkingDirectory);
    }

    [Fact]
    public void LargeOutputIsTruncated()
    {
        string huge = new('x', GuardedCommandTool.MaxOutputChars + 500);
        FakeProcessRunner runner = new(new ProcessOutcome(0, huge, string.Empty, timedOut: false));
        GuardedCommandTool tool = Create(tool: runner);
        ToolInvocation invocation = Run(tool, "greet", ToolPermissionPolicy.AllowAll);
        Assert.True(invocation.Result.IsSuccess);
        Assert.Contains("output truncated", invocation.Result.Output!.AsString(), StringComparison.Ordinal);
        Assert.True(invocation.Result.Output.AsString().Length < huge.Length);
    }

    [Fact]
    public void DiagnosticsAreExtractedAlongsideRawOutput()
    {
        const string stdout = "src\\A.cs(3,7): error CS0246: The type 'Foo' could not be found [C:\\App.csproj]\nBuild FAILED.";
        FakeProcessRunner runner = new(new ProcessOutcome(1, stdout, string.Empty, timedOut: false));
        GuardedCommandTool tool = Create(tool: runner);
        ToolInvocation invocation = Run(tool, "greet", ToolPermissionPolicy.AllowAll);
        Assert.True(invocation.Result.IsSuccess);
        string output = invocation.Result.Output!.AsString();
        // Raw output is preserved...
        Assert.Contains("Build FAILED.", output, StringComparison.Ordinal);
        // ...and a structured view is appended.
        Assert.Contains("diagnostics:", output, StringComparison.Ordinal);
        Assert.Contains("CS0246", output, StringComparison.Ordinal);
    }
    [Fact]
    public void OutputWithoutDiagnosticsHasNoDiagnosticsSection()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(0, "Build succeeded.", string.Empty, timedOut: false));
        GuardedCommandTool tool = Create(tool: runner);
        ToolInvocation invocation = Run(tool, "greet", ToolPermissionPolicy.AllowAll);
        Assert.DoesNotContain("diagnostics:", invocation.Result.Output!.AsString(), StringComparison.Ordinal);
    }
    [Fact]
    public void PreCancelledTokenPreventsTheRunnerFromBeingCalled()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(0, string.Empty, string.Empty, timedOut: false));
        GuardedCommandTool tool = Create(tool: runner);
        using CancellationTokenSource source = new();
        source.Cancel();
        ToolInvocation invocation = ToolExecutor.Execute(
            tool,
            Name("greet"),
            ToolPermissionPolicy.AllowAll,
            source.Token);
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal(ToolErrorCodes.Cancelled, invocation.Result.Error!.Code);
        Assert.Null(runner.LastExecutable);
    }

    [Fact]
    public void CancelledOutcomeFromTheRunnerIsReportedAsCancellation()
    {
        FakeProcessRunner runner = new(
            new ProcessOutcome(-1, string.Empty, string.Empty, timedOut: false, cancelled: true));
        GuardedCommandTool tool = Create(tool: runner);
        ToolInvocation invocation = Run(tool, "greet", ToolPermissionPolicy.AllowAll);
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal(ToolErrorCodes.Cancelled, invocation.Result.Error!.Code);
    }

    private GuardedCommandTool Create(
        TempWorkspace? workspace = null,
        IProcessRunner? tool = null,
        TimeSpan? timeout = null)
    {
        TempWorkspace ws = workspace ?? LeasedWorkspace();
        return new GuardedCommandTool(
            new WorkspaceRoot(ws.Path),
            new Dictionary<string, AllowedCommand>(StringComparer.Ordinal)
            {
                ["greet"] = new("greeter", ["--polite"]),
            },
            tool ?? new FakeProcessRunner(new ProcessOutcome(0, string.Empty, string.Empty, false)),
            timeout);
    }

    private TempWorkspace LeasedWorkspace()
    {
        return _defaultWorkspace;
    }

    private static ToolInvocation Run(GuardedCommandTool tool, string name, ToolPermissionPolicy policy)
    {
        return ToolExecutor.Execute(tool, Name(name), policy);
    }

    private static ArgumentBag Name(string name)
    {
        return ArgumentBag.From([new ArgumentEntry("name", ArgumentValue.FromString(name))]);
    }
}