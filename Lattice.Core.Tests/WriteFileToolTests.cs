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
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal("tool.permission.denied", invocation.Result.Error!.Code);
        Assert.Equal("hello world", File.ReadAllText(file));
    }

    [Fact]
    public void DeniedWriteCreatesNoBackup()
    {
        using TempWorkspace workspace = new();
        string file = workspace.WriteFile("a.txt", "hello world");
        WriteFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolExecutor.Execute(tool, Patch("a.txt", "world", "there"), ToolPermissionPolicy.ReadOnlyOnly);
        Assert.False(File.Exists(file + WriteFileTool.BackupSuffix));
    }

    [Fact]
    public void ApprovedWriteChangesFileAndReturnsExpectedDiff()
    {
        using TempWorkspace workspace = new();
        string file = workspace.WriteFile("a.txt", "alpha\nbeta\ngamma\n");
        WriteFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolInvocation invocation = ToolExecutor.Execute(tool, Patch("a.txt", "beta", "BETA"), ToolPermissionPolicy.AllowAll);
        Assert.True(invocation.Result.IsSuccess);
        Assert.Equal("alpha\nBETA\ngamma\n", File.ReadAllText(file));
        Assert.Equal("@@ -1,3 +1,3 @@\n alpha\n-beta\n+BETA\n gamma\n", invocation.Result.Output!.AsString());
    }

    [Fact]
    public void ApprovedWriteCreatesBackupWithOriginalContent()
    {
        using TempWorkspace workspace = new();
        string file = workspace.WriteFile("a.txt", "original");
        WriteFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolExecutor.Execute(tool, Patch("a.txt", "original", "changed"), ToolPermissionPolicy.AllowAll);
        Assert.True(File.Exists(file + WriteFileTool.BackupSuffix));
        Assert.Equal("original", File.ReadAllText(file + WriteFileTool.BackupSuffix));
    }

    [Fact]
    public void StaleContextIsRejectedAndFileUnchanged()
    {
        using TempWorkspace workspace = new();
        string file = workspace.WriteFile("a.txt", "hello");
        WriteFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolInvocation invocation = ToolExecutor.Execute(tool, Patch("a.txt", "missing", "x"), ToolPermissionPolicy.AllowAll);
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.PatchContextNotFound, invocation.Result.Error!.Code);
        Assert.Equal("hello", File.ReadAllText(file));
    }

    [Fact]
    public void AmbiguousContextIsRejectedAndFileUnchanged()
    {
        using TempWorkspace workspace = new();
        string file = workspace.WriteFile("a.txt", "x y x");
        WriteFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolInvocation invocation = ToolExecutor.Execute(tool, Patch("a.txt", "x", "z"), ToolPermissionPolicy.AllowAll);
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.PatchContextAmbiguous, invocation.Result.Error!.Code);
        Assert.Equal("x y x", File.ReadAllText(file));
    }

    [Fact]
    public void TraversalIsRejected()
    {
        using TempWorkspace workspace = new();
        WriteFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolInvocation invocation = ToolExecutor.Execute(tool, Patch("../outside.txt", "a", "b"), ToolPermissionPolicy.AllowAll);
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.PathOutsideRoot, invocation.Result.Error!.Code);
    }

    [Fact]
    public void MissingFileFails()
    {
        using TempWorkspace workspace = new();
        WriteFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolInvocation invocation = ToolExecutor.Execute(tool, Patch("nope.txt", "a", "b"), ToolPermissionPolicy.AllowAll);
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.PathNotFound, invocation.Result.Error!.Code);
    }

    [Fact]
    public void EmptyFindIsRejected()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("a.txt", "hello");
        WriteFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolInvocation invocation = ToolExecutor.Execute(tool, Patch("a.txt", string.Empty, "x"), ToolPermissionPolicy.AllowAll);
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.WriteFailed, invocation.Result.Error!.Code);
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
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal("tool.argument.type", invocation.Result.Error!.Code);
    }

    [Fact]
    public void DeletionIsAllowed()
    {
        using TempWorkspace workspace = new();
        string file = workspace.WriteFile("a.txt", "keep remove");
        WriteFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolInvocation invocation = ToolExecutor.Execute(tool, Patch("a.txt", " remove", string.Empty), ToolPermissionPolicy.AllowAll);
        Assert.True(invocation.Result.IsSuccess);
        Assert.Equal("keep", File.ReadAllText(file));
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