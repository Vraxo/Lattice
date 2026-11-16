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
        result.IsSuccess.Should().BeTrue();
        result.Output!.AsString().Should().Be("a.txt\nb.txt\nsub/");
    }

    [Fact]
    public void ListsSubdirectory()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("sub/inner.txt", "x");
        ListDirectoryTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "sub");
        result.IsSuccess.Should().BeTrue();
        result.Output!.AsString().Should().Be("inner.txt");
    }

    [Fact]
    public void MissingDirectoryFails()
    {
        using TempWorkspace workspace = new();
        ListDirectoryTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "nope");
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(FileToolErrorCodes.PathNotFound);
    }

    [Fact]
    public void FileInsteadOfDirectoryFails()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("a.txt", "a");
        ListDirectoryTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "a.txt");
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(FileToolErrorCodes.NotADirectory);
    }

    [Fact]
    public void TraversalIsRejected()
    {
        using TempWorkspace workspace = new();
        ListDirectoryTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "../");
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(FileToolErrorCodes.PathOutsideRoot);
    }

    [Fact]
    public void EmptyDirectorySucceedsWithEmptyListing()
    {
        using TempWorkspace workspace = new();
        ListDirectoryTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, ".");
        result.IsSuccess.Should().BeTrue();
        result.Output!.AsString().Should().Be(string.Empty);
    }

    private static ToolResult Execute(ListDirectoryTool tool, string path)
    {
        ArgumentBag arguments = ArgumentBag.From([new ArgumentEntry("path", ArgumentValue.FromString(path))]);
        return tool.Execute(arguments);
    }
}