namespace Lattice.Core.Tests;
public sealed class ListDirectoryToolTests
{
    [Fact]
    public void ListsEntriesSortedWithDirectoryMarker()
    {
        using var workspace = new TempWorkspace();
        workspace.WriteFile("b.txt", "b");
        workspace.WriteFile("a.txt", "a");
        workspace.CreateDirectory("sub");
        var tool = new ListDirectoryTool(new WorkspaceRoot(workspace.Path));
        var result = Execute(tool, ".");
        Assert.True(result.IsSuccess);
        Assert.Equal("a.txt\nb.txt\nsub/", result.Output!.AsString());
    }
    [Fact]
    public void ListsSubdirectory()
    {
        using var workspace = new TempWorkspace();
        workspace.WriteFile("sub/inner.txt", "x");
        var tool = new ListDirectoryTool(new WorkspaceRoot(workspace.Path));
        var result = Execute(tool, "sub");
        Assert.True(result.IsSuccess);
        Assert.Equal("inner.txt", result.Output!.AsString());
    }
    [Fact]
    public void MissingDirectoryFails()
    {
        using var workspace = new TempWorkspace();
        var tool = new ListDirectoryTool(new WorkspaceRoot(workspace.Path));
        var result = Execute(tool, "nope");
        Assert.False(result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.PathNotFound, result.Error!.Code);
    }
    [Fact]
    public void FileInsteadOfDirectoryFails()
    {
        using var workspace = new TempWorkspace();
        workspace.WriteFile("a.txt", "a");
        var tool = new ListDirectoryTool(new WorkspaceRoot(workspace.Path));
        var result = Execute(tool, "a.txt");
        Assert.False(result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.NotADirectory, result.Error!.Code);
    }
    [Fact]
    public void TraversalIsRejected()
    {
        using var workspace = new TempWorkspace();
        var tool = new ListDirectoryTool(new WorkspaceRoot(workspace.Path));
        var result = Execute(tool, "../");
        Assert.False(result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.PathOutsideRoot, result.Error!.Code);
    }
    [Fact]
    public void EmptyDirectorySucceedsWithEmptyListing()
    {
        using var workspace = new TempWorkspace();
        var tool = new ListDirectoryTool(new WorkspaceRoot(workspace.Path));
        var result = Execute(tool, ".");
        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Output!.AsString());
    }
    private static ToolResult Execute(ListDirectoryTool tool, string path)
    {
        var arguments = ArgumentBag.From(new[] { new ArgumentEntry("path", ArgumentValue.FromString(path)) });
        return tool.Execute(arguments);
    }
}