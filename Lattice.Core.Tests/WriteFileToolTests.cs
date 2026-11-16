namespace Lattice.Core.Tests;

public sealed class WriteFileToolTests
{
    [Fact]
    public void DeniedWriteLeavesFileUnchanged()
    {
        using TempWorkspace workspace = new();
        string file = workspace.WriteFile("a.txt", "hello world");
        WriteFileTool tool = new(new WorkspaceRoot(workspace.Path));

        // ReadOnlyOnly does not grant LocalWrite, so the executor must not invoke the tool.
        ToolInvocation invocation = ToolExecutor.Execute(tool, Patch("a.txt", "world", "there"), ToolPermissionPolicy.ReadOnlyOnly);
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be("tool.permission.denied");
        File.ReadAllText(file).Should().Be("hello world");
    }

    [Fact]
    public void DeniedWriteCreatesNoBackup()
    {
        using TempWorkspace workspace = new();
        string file = workspace.WriteFile("a.txt", "hello world");
        WriteFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolExecutor.Execute(tool, Patch("a.txt", "world", "there"), ToolPermissionPolicy.ReadOnlyOnly);
        File.Exists(file + WriteFileTool.BackupSuffix).Should().BeFalse();
    }

    [Fact]
    public void ApprovedWriteChangesFileAndReturnsExpectedDiff()
    {
        using TempWorkspace workspace = new();
        string file = workspace.WriteFile("a.txt", "alpha\nbeta\ngamma\n");
        WriteFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolInvocation invocation = ToolExecutor.Execute(tool, Patch("a.txt", "beta", "BETA"), ToolPermissionPolicy.AllowAll);
        invocation.Result.IsSuccess.Should().BeTrue();
        File.ReadAllText(file).Should().Be("alpha\nBETA\ngamma\n");
        invocation.Result.Output!.AsString().Should().Be("@@ -1,3 +1,3 @@\n alpha\n-beta\n+BETA\n gamma\n");
    }

    [Fact]
    public void ApprovedWriteCreatesBackupWithOriginalContent()
    {
        using TempWorkspace workspace = new();
        string file = workspace.WriteFile("a.txt", "original");
        WriteFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolExecutor.Execute(tool, Patch("a.txt", "original", "changed"), ToolPermissionPolicy.AllowAll);
        File.Exists(file + WriteFileTool.BackupSuffix).Should().BeTrue();
        File.ReadAllText(file + WriteFileTool.BackupSuffix).Should().Be("original");
    }

    [Fact]
    public void StaleContextIsRejectedAndFileUnchanged()
    {
        using TempWorkspace workspace = new();
        string file = workspace.WriteFile("a.txt", "hello");
        WriteFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolInvocation invocation = ToolExecutor.Execute(tool, Patch("a.txt", "missing", "x"), ToolPermissionPolicy.AllowAll);
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be(FileToolErrorCodes.PatchContextNotFound);
        File.ReadAllText(file).Should().Be("hello");
    }

    [Fact]
    public void AmbiguousContextIsRejectedAndFileUnchanged()
    {
        using TempWorkspace workspace = new();
        string file = workspace.WriteFile("a.txt", "x y x");
        WriteFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolInvocation invocation = ToolExecutor.Execute(tool, Patch("a.txt", "x", "z"), ToolPermissionPolicy.AllowAll);
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be(FileToolErrorCodes.PatchContextAmbiguous);
        File.ReadAllText(file).Should().Be("x y x");
    }

    [Fact]
    public void TraversalIsRejected()
    {
        using TempWorkspace workspace = new();
        WriteFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolInvocation invocation = ToolExecutor.Execute(tool, Patch("../outside.txt", "a", "b"), ToolPermissionPolicy.AllowAll);
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be(FileToolErrorCodes.PathOutsideRoot);
    }

    [Fact]
    public void MissingFileFails()
    {
        using TempWorkspace workspace = new();
        WriteFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolInvocation invocation = ToolExecutor.Execute(tool, Patch("nope.txt", "a", "b"), ToolPermissionPolicy.AllowAll);
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be(FileToolErrorCodes.PathNotFound);
    }

    [Fact]
    public void EmptyFindIsRejected()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("a.txt", "hello");
        WriteFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolInvocation invocation = ToolExecutor.Execute(tool, Patch("a.txt", string.Empty, "x"), ToolPermissionPolicy.AllowAll);
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be(FileToolErrorCodes.WriteFailed);
    }

    [Fact]
    public void NonStringArgumentFails()
    {
        using TempWorkspace workspace = new();
        WriteFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ArgumentBag arguments = ArgumentBag.From(
        [
            new ArgumentEntry("path", ArgumentValue.FromInteger(1)),
            new ArgumentEntry("find", ArgumentValue.FromString("a")),
            new ArgumentEntry("replace", ArgumentValue.FromString("b")),
        ]);
        ToolInvocation invocation = ToolExecutor.Execute(tool, arguments, ToolPermissionPolicy.AllowAll);

        // The argument validator rejects the type mismatch before the tool is invoked, so the
        // error comes from the executor boundary, not from the tool's own fallback.
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be("tool.argument.type");
    }

    [Fact]
    public void DeletionIsAllowed()
    {
        using TempWorkspace workspace = new();
        string file = workspace.WriteFile("a.txt", "keep remove");
        WriteFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolInvocation invocation = ToolExecutor.Execute(tool, Patch("a.txt", " remove", string.Empty), ToolPermissionPolicy.AllowAll);
        invocation.Result.IsSuccess.Should().BeTrue();
        File.ReadAllText(file).Should().Be("keep");
    }

    private static ArgumentBag Patch(string path, string find, string replace)
    {
        return ArgumentBag.From(
        [
            new ArgumentEntry("path", ArgumentValue.FromString(path)),
            new ArgumentEntry("find", ArgumentValue.FromString(find)),
            new ArgumentEntry("replace", ArgumentValue.FromString(replace)),
        ]);
    }
}