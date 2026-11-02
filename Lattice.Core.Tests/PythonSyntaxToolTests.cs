using Lattice.Core;
using Lattice.Python;

namespace Lattice.Core.Tests;

public sealed class PythonSyntaxToolTests
{
    [Fact]
    public void ValidSourceIsDiscoveredAndInvokedThroughGenericBoundary()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(
            0,
            "{\"valid\":true,\"version\":\"3.13.2\"}",
            string.Empty,
            timedOut: false));
        PythonSyntaxTool tool = CreateTool(runner);
        ToolRegistry registry = new([tool]);

        CapabilityCandidate candidate = Assert.Single(CapabilityDiscovery.Discover(
            registry.ToCatalog(),
            new CapabilityRequest("python-ast", Source("print('source is data')"))));
        Assert.Equal(PythonSyntaxTool.Id, candidate.Proposal.ToolId);
        Assert.True(registry.TryResolve(candidate.Proposal.ToolId, out ITool? registered));

        ToolInvocation invocation = ToolExecutor.Execute(
            registered!,
            candidate.Proposal.Arguments,
            ToolPermissionPolicy.AllowAll);

        Assert.True(invocation.Result.IsSuccess);
        Assert.Equal("valid (Python 3.13.2)", invocation.Result.Output!.AsString());
        Assert.Equal("python3", runner.LastExecutable);
        Assert.Equal("-I", runner.LastArguments[0]);
        Assert.Equal("-S", runner.LastArguments[1]);
        Assert.Equal("-c", runner.LastArguments[2]);
        Assert.Equal("print('source is data')", System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(runner.LastArguments[4])));
        Assert.Equal(ToolSideEffect.CommandExecution, tool.Descriptor.SideEffect);
        Assert.Contains("python", tool.Descriptor.Tags);
        Assert.Contains("syntax", tool.Descriptor.Tags);
    }

    [Fact]
    public void SyntaxErrorIsReportedAsAValidToolResultWithLocation()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(
            0,
            "{\"valid\":false,\"version\":\"3.12.1\",\"line\":3,\"column\":5,\"message\":\"expected ':'\"}",
            string.Empty,
            timedOut: false));
        ToolInvocation invocation = ToolExecutor.Execute(
            CreateTool(runner),
            Source("if True:\n  pass\nelse\n  pass"),
            ToolPermissionPolicy.AllowAll);

        Assert.True(invocation.Result.IsSuccess);
        Assert.Equal(
            $"invalid (Python 3.12.1){Environment.NewLine}SyntaxError at 3:5: expected ':'",
            invocation.Result.Output!.AsString());
    }

    [Fact]
    public void ReadOnlyPolicyDeniesPythonProcessBeforeInvocation()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(
            0,
            "{\"valid\":true,\"version\":\"3.12.1\"}",
            string.Empty,
            timedOut: false));
        ToolInvocation invocation = ToolExecutor.Execute(
            CreateTool(runner),
            Source("pass"),
            ToolPermissionPolicy.ReadOnlyOnly);

        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal("tool.permission.denied", invocation.Result.Error!.Code);
        Assert.Null(runner.LastExecutable);
    }

    [Fact]
    public void OmittingAdapterFromRegistryDisablesDiscoveryAndInvocation()
    {
        ToolRegistry registry = new([new CalculatorTool()]);

        Assert.False(registry.TryResolve(PythonSyntaxTool.Id, out _));
        Assert.Empty(CapabilityDiscovery.Discover(
            registry.ToCatalog(),
            new CapabilityRequest("python.syntax")));
    }

    [Fact]
    public void OversizedSourceIsRejectedBeforeProcessStart()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(
            0,
            "{\"valid\":true,\"version\":\"3.12.1\"}",
            string.Empty,
            timedOut: false));
        string source = new('x', PythonSyntaxTool.MaxSourceUtf8Bytes + 1);

        ToolResult result = CreateTool(runner).Execute(Source(source));

        Assert.False(result.IsSuccess);
        Assert.Equal(PythonErrorCodes.SourceTooLarge, result.Error!.Code);
        Assert.Null(runner.LastExecutable);
    }

    [Fact]
    public void MissingSourceIsRejectedByGenericArgumentValidation()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(
            0,
            "{\"valid\":true,\"version\":\"3.12.1\"}",
            string.Empty,
            timedOut: false));
        ToolInvocation invocation = ToolExecutor.Execute(
            CreateTool(runner),
            ArgumentBag.Empty,
            ToolPermissionPolicy.AllowAll);

        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal("tool.argument.missing", invocation.Result.Error!.Code);
        Assert.Null(runner.LastExecutable);
    }

    [Fact]
    public void TimeoutIsReportedAsFailure()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(
            -1,
            string.Empty,
            string.Empty,
            timedOut: true));
        ToolInvocation invocation = ToolExecutor.Execute(
            CreateTool(runner),
            Source("pass"),
            ToolPermissionPolicy.AllowAll);

        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal(PythonErrorCodes.ParserTimedOut, invocation.Result.Error!.Code);
    }

    [Fact]
    public void InvalidUnicodeIsRejectedBeforeProcessStart()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(
            0,
            "{\"valid\":true,\"version\":\"3.12.1\"}",
            string.Empty,
            timedOut: false));

        string source = new((char)0xD800, 1);
        ToolResult result = CreateTool(runner).Execute(Source(source));

        Assert.False(result.IsSuccess);
        Assert.Equal(PythonErrorCodes.SourceEncodingInvalid, result.Error!.Code);
        Assert.Null(runner.LastExecutable);
    }

    [Fact]
    public void MalformedParserOutputIsReportedAsFailure()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(0, "not json", string.Empty, timedOut: false));

        ToolInvocation invocation = ToolExecutor.Execute(
            CreateTool(runner),
            Source("pass"),
            ToolPermissionPolicy.AllowAll);

        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal(PythonErrorCodes.InvalidParserResponse, invocation.Result.Error!.Code);
    }

    private static PythonSyntaxTool CreateTool(FakeProcessRunner runner)
    {
        return new("python3", Environment.CurrentDirectory, runner);
    }

    private static ArgumentBag Source(string source)
    {
        return ArgumentBag.From([new ArgumentEntry(PythonSyntaxTool.SourceParameter, ArgumentValue.FromString(source))]);
    }
}
