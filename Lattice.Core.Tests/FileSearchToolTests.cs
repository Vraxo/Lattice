namespace Lattice.Core.Tests;

public sealed class FileSearchToolTests
{
    [Fact]
    public void FindsMatchesWithLineNumbers()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("a.txt", "first\nneedle here\nlast");
        FileSearchTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "needle");
        result.IsSuccess.Should().BeTrue();
        result.Output!.AsString().Should().Be("a.txt:2: needle here");
    }

    [Fact]
    public void SearchIsCaseInsensitive()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("a.txt", "Needle");
        FileSearchTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "needle");
        result.IsSuccess.Should().BeTrue();
        result.Output!.AsString().Should().Be("a.txt:1: Needle");
    }

    [Fact]
    public void NoMatchesReturnsEmptyString()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("a.txt", "nothing here");
        FileSearchTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "needle");
        result.IsSuccess.Should().BeTrue();
        result.Output!.AsString().Should().Be(string.Empty);
    }

    [Fact]
    public void ExcludedDirectoriesAreSkipped()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("keep.txt", "needle");
        workspace.WriteFile("bin/skip.txt", "needle");
        FileSearchTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "needle");
        result.IsSuccess.Should().BeTrue();
        result.Output!.AsString().Should().Be("keep.txt:1: needle");
    }

    [Fact]
    public void CustomExclusionsAreHonored()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("keep.txt", "needle");
        workspace.WriteFile("skip/skip.txt", "needle");
        FileSearchTool tool = new(new WorkspaceRoot(workspace.Path), ["skip"]);
        ToolResult result = Execute(tool, "needle");
        result.IsSuccess.Should().BeTrue();
        result.Output!.AsString().Should().Be("keep.txt:1: needle");
    }

    [Fact]
    public void OversizedFilesAreSkippedNotFatal()
    {
        using TempWorkspace workspace = new();
        workspace.WriteBytes("big.txt", new byte[FileSearchTool.MaxScannedFileBytes + 1]);
        workspace.WriteFile("small.txt", "needle");
        FileSearchTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "needle");
        result.IsSuccess.Should().BeTrue();
        result.Output!.AsString().Should().Be("small.txt:1: needle");
    }

    [Fact]
    public void InvalidUtf8FilesAreSkippedNotFatal()
    {
        using TempWorkspace workspace = new();
        workspace.WriteBytes("bad.txt", [0xC3, 0x28]);
        workspace.WriteFile("good.txt", "needle");
        FileSearchTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "needle");
        result.IsSuccess.Should().BeTrue();
        result.Output!.AsString().Should().Be("good.txt:1: needle");
    }

    [Fact]
    public void PathsAreReportedRelativeWithForwardSlashes()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("sub/dir/a.txt", "needle");
        FileSearchTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "needle");
        result.IsSuccess.Should().BeTrue();
        result.Output!.AsString().Should().Be("sub/dir/a.txt:1: needle");
    }

    [Fact]
    public void ResultsAreTruncatedAtTheLimit()
    {
        using TempWorkspace workspace = new();
        string contents = string.Join('\n', Enumerable.Repeat("needle", FileSearchTool.MaxMatches + 5));
        workspace.WriteFile("a.txt", contents);
        FileSearchTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "needle");
        result.IsSuccess.Should().BeTrue();
        result.Output!.AsString().Should().Contain("results truncated");
    }

    [Fact]
    public void LongLinesAreTruncated()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("a.txt", new string('x', FileSearchTool.MaxLineLength + 50) + "needle");
        FileSearchTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "needle");
        result.IsSuccess.Should().BeTrue();
        result.Output!.AsString().Should().Contain("...");
    }

    [Fact]
    public void EmptyQueryFails()
    {
        using TempWorkspace workspace = new();
        FileSearchTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "   ");
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(FileToolErrorCodes.QueryEmpty);
    }

    [Fact]
    public void MissingQueryFails()
    {
        using TempWorkspace workspace = new();
        FileSearchTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = tool.Execute(ArgumentBag.Empty);
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(FileToolErrorCodes.QueryEmpty);
    }

    [Fact]
    public void MissingStartPathFails()
    {
        using TempWorkspace workspace = new();
        FileSearchTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = ExecuteWithPath(tool, "needle", "nope");
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(FileToolErrorCodes.PathNotFound);
    }

    [Fact]
    public void TraversalInStartPathIsRejected()
    {
        using TempWorkspace workspace = new();
        FileSearchTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = ExecuteWithPath(tool, "needle", "../");
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(FileToolErrorCodes.PathOutsideRoot);
    }

    [Fact]
    public void RestrictingToSubdirectoryLimitsResults()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("outer.txt", "needle");
        workspace.WriteFile("sub/inner.txt", "needle");
        FileSearchTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = ExecuteWithPath(tool, "needle", "sub");
        result.IsSuccess.Should().BeTrue();
        result.Output!.AsString().Should().Be("sub/inner.txt:1: needle");
    }

    private static ToolResult Execute(FileSearchTool tool, string query)
    {
        return tool.Execute(ArgumentBag.From([new ArgumentEntry("query", ArgumentValue.FromString(query))]));
    }

    private static ToolResult ExecuteWithPath(FileSearchTool tool, string query, string path)
    {
        return tool.Execute(ArgumentBag.From(
            [
            new ArgumentEntry("query", ArgumentValue.FromString(query)),
            new ArgumentEntry("path", ArgumentValue.FromString(path)),
            ]));
    }
}