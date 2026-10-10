namespace Lattice.Core.Tests;

public sealed class ListDirectoryToolTests
{
    [Fact]
    public void ListsEntriesSortedWithDirectoryMarker()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("b.txt", "b");
        workspace.WriteFile("a.txt", "a");
        workspace.CreateDirectory("sub");
        ListDirectoryTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, ".");
        Assert.True(result.IsSuccess);
        Assert.Equal("a.txt\nb.txt\nsub/", result.Output!.AsString());
    }

    [Fact]
    public void ListsSubdirectory()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("sub/inner.txt", "x");
        ListDirectoryTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "sub");
        Assert.True(result.IsSuccess);
        Assert.Equal("inner.txt", result.Output!.AsString());
    }

    [Fact]
    public void MissingDirectoryFails()
    {
        using TempWorkspace workspace = new();
        ListDirectoryTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "nope");
        Assert.False(result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.PathNotFound, result.Error!.Code);
    }

    [Fact]
    public void FileInsteadOfDirectoryFails()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("a.txt", "a");
        ListDirectoryTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "a.txt");
        Assert.False(result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.NotADirectory, result.Error!.Code);
    }

    [Fact]
    public void TraversalIsRejected()
    {
        using TempWorkspace workspace = new();
        ListDirectoryTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "../");
        Assert.False(result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.PathOutsideRoot, result.Error!.Code);
    }

    [Fact]
    public void EmptyDirectorySucceedsWithEmptyListing()
    {
        using TempWorkspace workspace = new();
        ListDirectoryTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, ".");
        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Output!.AsString());
    }

    private static ToolResult Execute(ListDirectoryTool tool, string path)
    {
        ArgumentBag arguments = ArgumentBag.From([new ArgumentEntry("path", ArgumentValue.FromString(path))]);
        return tool.Execute(arguments);
    }
}