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
        Assert.True(invocation.Result.IsSuccess);
        Assert.Equal("valid", invocation.Result.Output!.AsString());
    }

    [Fact]
    public void InvalidSourceReportsInvalidWithDiagnostics()
    {
        ToolInvocation invocation = ToolExecutor.Execute(
            new CSharpSyntaxTool(),
            Source("class C {"),
            ToolPermissionPolicy.ReadOnlyOnly);
        Assert.True(invocation.Result.IsSuccess);
        string output = invocation.Result.Output!.AsString();
        Assert.StartsWith("invalid", output, StringComparison.Ordinal);
        Assert.Contains("error", output);
    }

    [Fact]
    public void MissingSourceArgumentFailsValidation()
    {
        ToolInvocation invocation = ToolExecutor.Execute(
            new CSharpSyntaxTool(),
            ArgumentBag.Empty,
            ToolPermissionPolicy.ReadOnlyOnly);
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal("tool.argument.missing", invocation.Result.Error!.Code);
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
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal("tool.argument.type", invocation.Result.Error!.Code);
    }

    [Fact]
    public void ToolIsReadOnlySoNoWritePermissionIsNeeded()
    {
        Assert.Equal(ToolSideEffect.ReadOnly, new CSharpSyntaxTool().Descriptor.SideEffect);
    }

    private static ArgumentBag Source(string source)
    {
        return ArgumentBag.From([new ArgumentEntry("source", ArgumentValue.FromString(source))]);
    }
}