using Lattice.CSharp;

namespace Lattice.Core.Tests;

public sealed class CSharpSyntaxToolTests
{
    [Fact]
    public void ValidSourceReportsValidAndExecutesThroughExecutor()
    {
        ToolInvocation invocation = ToolExecutor.Execute(
            new CSharpSyntaxTool(),
            Source("class C { }"),
            ToolPermissionPolicy.ReadOnlyOnly);
        invocation.Result.IsSuccess.Should().BeTrue();
        invocation.Result.Output!.AsString().Should().Be("valid");
    }

    [Fact]
    public void InvalidSourceReportsInvalidWithDiagnostics()
    {
        ToolInvocation invocation = ToolExecutor.Execute(
            new CSharpSyntaxTool(),
            Source("class C {"),
            ToolPermissionPolicy.ReadOnlyOnly);
        invocation.Result.IsSuccess.Should().BeTrue();
        string output = invocation.Result.Output!.AsString();
        output.Should().StartWith("invalid");
        output.Should().Contain("error");
    }

    [Fact]
    public void MissingSourceArgumentFailsValidation()
    {
        ToolInvocation invocation = ToolExecutor.Execute(
            new CSharpSyntaxTool(),
            ArgumentBag.Empty,
            ToolPermissionPolicy.ReadOnlyOnly);
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be("tool.argument.missing");
    }

    [Fact]
    public void NonStringSourceFailsValidation()
    {
        ArgumentBag arguments = ArgumentBag.From(
        [
            new ArgumentEntry("source", ArgumentValue.FromInteger(1)),
        ]);
        ToolInvocation invocation = ToolExecutor.Execute(
            new CSharpSyntaxTool(),
            arguments,
            ToolPermissionPolicy.ReadOnlyOnly);
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be("tool.argument.type");
    }

    [Fact]
    public void ToolIsReadOnlySoNoWritePermissionIsNeeded()
    {
        new CSharpSyntaxTool().Descriptor.SideEffect.Should().Be(ToolSideEffect.ReadOnly);
    }

    private static ArgumentBag Source(string source)
    {
        return ArgumentBag.From([new ArgumentEntry("source", ArgumentValue.FromString(source))]);
    }
}