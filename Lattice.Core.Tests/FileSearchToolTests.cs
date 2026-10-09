namespace Lattice.Core.Tests;
public sealed class FileSearchToolTests
{
    [Fact]
    public void FindsMatchesWithLineNumbers()
    {
        using var workspace = new TempWorkspace();
        workspace.WriteFile("a.txt", "first\nneedle here\nlast");
        var tool = new FileSearchTool(new WorkspaceRoot(workspace.Path));
        var result = Execute(tool, "needle");
        Assert.True(result.IsSuccess);
        Assert.Equal("a.txt:2: needle here", result.Output!.AsString());
    }
    [Fact]
    public void SearchIsCaseInsensitive()
    {
        using var workspace = new TempWorkspace();
        workspace.WriteFile("a.txt", "Needle");
        var tool = new FileSearchTool(new WorkspaceRoot(workspace.Path));
        var result = Execute(tool, "needle");
        Assert.True(result.IsSuccess);
        Assert.Equal("a.txt:1: Needle", result.Output!.AsString());
    }
    [Fact]
    public void NoMatchesReturnsEmptyString()
    {
        using var workspace = new TempWorkspace();
        workspace.WriteFile("a.txt", "nothing here");
        var tool = new FileSearchTool(new WorkspaceRoot(workspace.Path));
        var result = Execute(tool, "needle");
        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Output!.AsString());
    }
    [Fact]
    public void ExcludedDirectoriesAreSkipped()
    {
        using var workspace = new TempWorkspace();
        workspace.WriteFile("keep.txt", "needle");
        workspace.WriteFile("bin/skip.txt", "needle");
        var tool = new FileSearchTool(new WorkspaceRoot(workspace.Path));
        var result = Execute(tool, "needle");
        Assert.True(result.IsSuccess);
        Assert.Equal("keep.txt:1: needle", result.Output!.AsString());
    }
    [Fact]
    public void CustomExclusionsAreHonored()
    {
        using var workspace = new TempWorkspace();
        workspace.WriteFile("keep.txt", "needle");
        workspace.WriteFile("skip/skip.txt", "needle");
        var tool = new FileSearchTool(new WorkspaceRoot(workspace.Path), new[] { "skip" });
        var result = Execute(tool, "needle");
        Assert.True(result.IsSuccess);
        Assert.Equal("keep.txt:1: needle", result.Output!.AsString());
    }
    [Fact]
    public void OversizedFilesAreSkippedNotFatal()
    {
        using var workspace = new TempWorkspace();
        workspace.WriteBytes("big.txt", new byte[FileSearchTool.MaxScannedFileBytes + 1]);
        workspace.WriteFile("small.txt", "needle");
        var tool = new FileSearchTool(new WorkspaceRoot(workspace.Path));
        var result = Execute(tool, "needle");
        Assert.True(result.IsSuccess);
        Assert.Equal("small.txt:1: needle", result.Output!.AsString());
    }
    [Fact]
    public void InvalidUtf8FilesAreSkippedNotFatal()
    {
        using var workspace = new TempWorkspace();
        workspace.WriteBytes("bad.txt", new byte[] { 0xC3, 0x28 });
        workspace.WriteFile("good.txt", "needle");
        var tool = new FileSearchTool(new WorkspaceRoot(workspace.Path));
        var result = Execute(tool, "needle");
        Assert.True(result.IsSuccess);
        Assert.Equal("good.txt:1: needle", result.Output!.AsString());
    }
    [Fact]
    public void PathsAreReportedRelativeWithForwardSlashes()
    {
        using var workspace = new TempWorkspace();
        workspace.WriteFile("sub/dir/a.txt", "needle");
        var tool = new FileSearchTool(new WorkspaceRoot(workspace.Path));
        var result = Execute(tool, "needle");
        Assert.True(result.IsSuccess);
        Assert.Equal("sub/dir/a.txt:1: needle", result.Output!.AsString());
    }
    [Fact]
    public void ResultsAreTruncatedAtTheLimit()
    {
        using var workspace = new TempWorkspace();
        var contents = string.Join('\n', Enumerable.Repeat("needle", FileSearchTool.MaxMatches + 5));
        workspace.WriteFile("a.txt", contents);
        var tool = new FileSearchTool(new WorkspaceRoot(workspace.Path));
        var result = Execute(tool, "needle");
        Assert.True(result.IsSuccess);
        Assert.Contains("results truncated", result.Output!.AsString());
    }
    [Fact]
    public void LongLinesAreTruncated()
    {
        using var workspace = new TempWorkspace();
        workspace.WriteFile("a.txt", new string('x', FileSearchTool.MaxLineLength + 50) + "needle");
        var tool = new FileSearchTool(new WorkspaceRoot(workspace.Path));
        var result = Execute(tool, "needle");
        Assert.True(result.IsSuccess);
        Assert.Contains("...", result.Output!.AsString());
    }
    [Fact]
    public void EmptyQueryFails()
    {
        using var workspace = new TempWorkspace();
        var tool = new FileSearchTool(new WorkspaceRoot(workspace.Path));
        var result = Execute(tool, "   ");
        Assert.False(result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.QueryEmpty, result.Error!.Code);
    }
    [Fact]
    public void MissingQueryFails()
    {
        using var workspace = new TempWorkspace();
        var tool = new FileSearchTool(new WorkspaceRoot(workspace.Path));
        var result = tool.Execute(ArgumentBag.Empty);
        Assert.False(result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.QueryEmpty, result.Error!.Code);
    }
    [Fact]
    public void MissingStartPathFails()
    {
        using var workspace = new TempWorkspace();
        var tool = new FileSearchTool(new WorkspaceRoot(workspace.Path));
        var result = ExecuteWithPath(tool, "needle", "nope");
        Assert.False(result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.PathNotFound, result.Error!.Code);
    }
    [Fact]
    public void TraversalInStartPathIsRejected()
    {
        using var workspace = new TempWorkspace();
        var tool = new FileSearchTool(new WorkspaceRoot(workspace.Path));
        var result = ExecuteWithPath(tool, "needle", "../");
        Assert.False(result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.PathOutsideRoot, result.Error!.Code);
    }
    [Fact]
    public void RestrictingToSubdirectoryLimitsResults()
    {
        using var workspace = new TempWorkspace();
        workspace.WriteFile("outer.txt", "needle");
        workspace.WriteFile("sub/inner.txt", "needle");
        var tool = new FileSearchTool(new WorkspaceRoot(workspace.Path));
        var result = ExecuteWithPath(tool, "needle", "sub");
        Assert.True(result.IsSuccess);
        Assert.Equal("sub/inner.txt:1: needle", result.Output!.AsString());
    }
    private static ToolResult Execute(FileSearchTool tool, string query) =>
        tool.Execute(ArgumentBag.From(new[] { new ArgumentEntry("query", ArgumentValue.FromString(query)) }));
    private static ToolResult ExecuteWithPath(FileSearchTool tool, string query, string path) =>
        tool.Execute(ArgumentBag.From(new[]
        {
            new ArgumentEntry("query", ArgumentValue.FromString(query)),
            new ArgumentEntry("path", ArgumentValue.FromString(path)),
        }));
}