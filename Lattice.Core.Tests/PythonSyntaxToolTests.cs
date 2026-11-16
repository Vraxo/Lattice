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

        CapabilityCandidate candidate = CapabilityDiscovery.Discover(
            registry.ToCatalog(),
            new CapabilityRequest("python-ast", Source("print('source is data')"))).Should().ContainSingle().Which;
        candidate.Proposal.ToolId.Should().Be(PythonSyntaxTool.Id);
        registry.TryResolve(candidate.Proposal.ToolId, out ITool? registered).Should().BeTrue();

        ToolInvocation invocation = ToolExecutor.Execute(
            registered!,
            candidate.Proposal.Arguments,
            ToolPermissionPolicy.AllowAll);

        invocation.Result.IsSuccess.Should().BeTrue();
        invocation.Result.Output!.AsString().Should().Be("valid (Python 3.13.2)");
        runner.LastExecutable.Should().Be("python3");
        runner.LastArguments[0].Should().Be("-I");
        runner.LastArguments[1].Should().Be("-S");
        runner.LastArguments[2].Should().Be("-c");
        System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(runner.LastArguments[4])).Should().Be("print('source is data')");
        tool.Descriptor.SideEffect.Should().Be(ToolSideEffect.CommandExecution);
        tool.Descriptor.Tags.Should().Contain("python");
        tool.Descriptor.Tags.Should().Contain("syntax");
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

        invocation.Result.IsSuccess.Should().BeTrue();
        invocation.Result.Output!.AsString().Should().Be($"invalid (Python 3.12.1){Environment.NewLine}SyntaxError at 3:5: expected ':'");
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

        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be("tool.permission.denied");
        runner.LastExecutable.Should().BeNull();
    }

    [Fact]
    public void OmittingAdapterFromRegistryDisablesDiscoveryAndInvocation()
    {
        ToolRegistry registry = new([new CalculatorTool()]);

        registry.TryResolve(PythonSyntaxTool.Id, out _).Should().BeFalse();
        CapabilityDiscovery.Discover(
            registry.ToCatalog(),
            new CapabilityRequest("python.syntax")).Should().BeEmpty();
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

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(PythonErrorCodes.SourceTooLarge);
        runner.LastExecutable.Should().BeNull();
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

        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be("tool.argument.missing");
        runner.LastExecutable.Should().BeNull();
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

        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be(PythonErrorCodes.ParserTimedOut);
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

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(PythonErrorCodes.SourceEncodingInvalid);
        runner.LastExecutable.Should().BeNull();
    }

    [Fact]
    public void MalformedParserOutputIsReportedAsFailure()
    {
        FakeProcessRunner runner = new(new ProcessOutcome(0, "not json", string.Empty, timedOut: false));

        ToolInvocation invocation = ToolExecutor.Execute(
            CreateTool(runner),
            Source("pass"),
            ToolPermissionPolicy.AllowAll);

        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be(PythonErrorCodes.InvalidParserResponse);
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
