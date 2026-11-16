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
        invocation.Result.IsSuccess.Should().BeTrue();
        invocation.Result.Output!.AsString().Should().Contain("exited with code 0");
        invocation.Result.Output.AsString().Should().Contain("hello");
    }

    [Fact]
    public void CommandIsLookedUpByNameNotSuppliedAsAString()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(0, string.Empty, string.Empty, timedOut: false));
        GuardedCommandTool tool = Create(tool: runner);
        Run(tool, "greet", ToolPermissionPolicy.AllowAll);
        runner.LastExecutable.Should().Be("greeter");
        runner.LastArguments.Should().Equal(["--polite"]);
    }

    [Fact]
    public void NonZeroExitIsStillAResultNotAFailure()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(1, "out", "err", timedOut: false));
        GuardedCommandTool tool = Create(tool: runner);
        ToolInvocation invocation = Run(tool, "greet", ToolPermissionPolicy.AllowAll);

        // The command ran; its exit code is information, not a tool malfunction.
        invocation.Result.IsSuccess.Should().BeTrue();
        invocation.Result.Output!.AsString().Should().Contain("exited with code 1");
        invocation.Result.Output.AsString().Should().Contain("err");
    }

    [Fact]
    public void TimeoutIsReportedAsFailure()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(-1, string.Empty, string.Empty, timedOut: true));
        GuardedCommandTool tool = Create(tool: runner);
        ToolInvocation invocation = Run(tool, "greet", ToolPermissionPolicy.AllowAll);
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be(CommandErrorCodes.CommandTimedOut);
    }

    [Fact]
    public void UnknownCommandNameIsRejected()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(0, string.Empty, string.Empty, timedOut: false));
        GuardedCommandTool tool = Create(tool: runner);
        ToolInvocation invocation = Run(tool, "rm-rf", ToolPermissionPolicy.AllowAll);
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be(CommandErrorCodes.CommandNotAllowed);
        runner.LastExecutable.Should().BeNull();
    }

    [Fact]
    public void DeniedPolicyPreventsExecutionEntirely()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(0, string.Empty, string.Empty, timedOut: false));
        GuardedCommandTool tool = Create(tool: runner);
        ToolInvocation invocation = Run(tool, "greet", ToolPermissionPolicy.ReadOnlyOnly);
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be("tool.permission.denied");
        runner.LastExecutable.Should().BeNull();
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
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be("tool.argument.missing");
    }

    [Fact]
    public void RunnerFailureToStartIsReported()
    {
        GuardedCommandTool tool = Create(tool: new ThrowingProcessRunner());
        ToolInvocation invocation = Run(tool, "greet", ToolPermissionPolicy.AllowAll);
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be(CommandErrorCodes.CommandFailedToStart);
    }

    [Fact]
    public void ConfiguredTimeoutIsPassedToTheRunner()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(0, string.Empty, string.Empty, timedOut: false));
        GuardedCommandTool tool = Create(tool: runner, timeout: TimeSpan.FromSeconds(5));
        Run(tool, "greet", ToolPermissionPolicy.AllowAll);
        runner.LastTimeout.Should().Be(TimeSpan.FromSeconds(5));
        tool.Descriptor.Limits.Timeout.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CommandRunsInTheWorkspaceRoot()
    {
        using TempWorkspace workspace = new();
        FakeProcessRunner runner = new(new ProcessOutcome(0, string.Empty, string.Empty, timedOut: false));
        GuardedCommandTool tool = Create(workspace, runner);
        ToolExecutor.Execute(tool, Name("greet"), ToolPermissionPolicy.AllowAll);
        runner.LastWorkingDirectory.Should().Be(workspace.Path);
    }

    [Fact]
    public void LargeOutputIsTruncated()
    {
        string huge = new('x', GuardedCommandTool.MaxOutputChars + 500);
        FakeProcessRunner runner = new(new ProcessOutcome(0, huge, string.Empty, timedOut: false));
        GuardedCommandTool tool = Create(tool: runner);
        ToolInvocation invocation = Run(tool, "greet", ToolPermissionPolicy.AllowAll);
        invocation.Result.IsSuccess.Should().BeTrue();
        invocation.Result.Output!.AsString().Should().Contain("output truncated");
        (invocation.Result.Output.AsString().Length < huge.Length).Should().BeTrue();
    }

    [Fact]
    public void DiagnosticsAreExtractedAlongsideRawOutput()
    {
        const string stdout = "src\\A.cs(3,7): error CS0246: The type 'Foo' could not be found [C:\\App.csproj]\nBuild FAILED.";
        FakeProcessRunner runner = new(new ProcessOutcome(1, stdout, string.Empty, timedOut: false));
        GuardedCommandTool tool = Create(tool: runner);
        ToolInvocation invocation = Run(tool, "greet", ToolPermissionPolicy.AllowAll);
        invocation.Result.IsSuccess.Should().BeTrue();
        string output = invocation.Result.Output!.AsString();

        // Raw output is preserved...
        output.Should().Contain("Build FAILED.");

        // ...and a structured view is appended.
        output.Should().Contain("diagnostics:");
        output.Should().Contain("CS0246");
    }

    [Fact]
    public void OutputWithoutDiagnosticsHasNoDiagnosticsSection()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(0, "Build succeeded.", string.Empty, timedOut: false));
        GuardedCommandTool tool = Create(tool: runner);
        ToolInvocation invocation = Run(tool, "greet", ToolPermissionPolicy.AllowAll);
        invocation.Result.Output!.AsString().Should().NotContain("diagnostics:");
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
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be(ToolErrorCodes.Cancelled);
        runner.LastExecutable.Should().BeNull();
    }

    [Fact]
    public void CancelledOutcomeFromTheRunnerIsReportedAsCancellation()
    {
        FakeProcessRunner runner = new(
            new ProcessOutcome(-1, string.Empty, string.Empty, timedOut: false, cancelled: true));
        GuardedCommandTool tool = Create(tool: runner);
        ToolInvocation invocation = Run(tool, "greet", ToolPermissionPolicy.AllowAll);
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be(ToolErrorCodes.Cancelled);
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
