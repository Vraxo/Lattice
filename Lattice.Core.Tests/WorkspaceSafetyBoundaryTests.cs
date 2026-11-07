using System.Diagnostics;

namespace Lattice.Core.Tests;

public sealed class WorkspaceSafetyBoundaryTests
{
    [Fact]
    public void OrdinaryFileUnderWorkspaceRootRemainsReadableSearchableAndListable()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("src/Example.cs", "public sealed class Example { }");
        WorkspaceRoot root = new(workspace.Path);

        ToolResult read = new ReadFileTool(root).Execute(PathArguments("src/Example.cs"));
        Assert.True(read.IsSuccess);
        Assert.Equal("public sealed class Example { }", read.Output!.AsString());

        ToolResult search = new FileSearchTool(root).Execute(QueryArguments("Example"));
        Assert.True(search.IsSuccess);
        Assert.Equal("src/Example.cs:1: public sealed class Example { }", search.Output!.AsString());

        ToolResult list = new ListDirectoryTool(root).Execute(PathArguments("src"));
        Assert.True(list.IsSuccess);
        Assert.Equal("Example.cs", list.Output!.AsString());
    }

    [Theory]
    [InlineData("../outside.txt")]
    [InlineData("sub/../../outside.txt")]
    [InlineData("..\\outside.txt")]
    public void ParentTraversalIsRejectedAcrossWorkspaceTools(string escapingPath)
    {
        using TempWorkspace workspace = new();
        workspace.CreateDirectory("sub");
        WorkspaceRoot root = new(workspace.Path);

        ToolResult read = new ReadFileTool(root).Execute(PathArguments(escapingPath));
        Assert.False(read.IsSuccess);
        Assert.Equal(FileToolErrorCodes.PathOutsideRoot, read.Error!.Code);

        ToolResult list = new ListDirectoryTool(root).Execute(PathArguments(escapingPath));
        Assert.False(list.IsSuccess);
        Assert.Equal(FileToolErrorCodes.PathOutsideRoot, list.Error!.Code);

        ToolResult search = new FileSearchTool(root).Execute(QueryWithPathArguments("needle", escapingPath));
        Assert.False(search.IsSuccess);
        Assert.Equal(FileToolErrorCodes.PathOutsideRoot, search.Error!.Code);
    }

    [Fact]
    public void RootedAndAbsolutePathsAreRejectedAcrossWorkspaceTools()
    {
        using TempWorkspace workspace = new();
        string fullFilePath = workspace.WriteFile("inside.txt", "needle");
        WorkspaceRoot root = new(workspace.Path);

        ToolResult read = new ReadFileTool(root).Execute(PathArguments(fullFilePath));
        Assert.False(read.IsSuccess);
        Assert.Equal(FileToolErrorCodes.PathOutsideRoot, read.Error!.Code);

        ToolResult list = new ListDirectoryTool(root).Execute(PathArguments(workspace.Path));
        Assert.False(list.IsSuccess);
        Assert.Equal(FileToolErrorCodes.PathOutsideRoot, list.Error!.Code);

        ToolResult search = new FileSearchTool(root).Execute(QueryWithPathArguments("needle", workspace.Path));
        Assert.False(search.IsSuccess);
        Assert.Equal(FileToolErrorCodes.PathOutsideRoot, search.Error!.Code);
    }

    [SymlinkFact]
    public void SymbolicLinkFileIsRejectedOnDirectReadAndSkippedDuringSearchAndListing()
    {
        using TempWorkspace outside = new();
        string secretFile = outside.WriteFile("secret.txt", "needle-secret-outside");

        using TempWorkspace workspace = new();
        workspace.WriteFile("ordinary.txt", "needle-ordinary-inside");
        string linkedFile = Path.Combine(workspace.Path, "linked.txt");
        File.CreateSymbolicLink(linkedFile, secretFile);

        WorkspaceRoot root = new(workspace.Path);

        ToolResult directRead = new ReadFileTool(root).Execute(PathArguments("linked.txt"));
        Assert.False(directRead.IsSuccess);
        Assert.Equal(FileToolErrorCodes.ReparsePointNotAllowed, directRead.Error!.Code);

        ToolResult search = new FileSearchTool(root).Execute(QueryArguments("needle"));
        Assert.True(search.IsSuccess);
        Assert.Equal("ordinary.txt:1: needle-ordinary-inside", search.Output!.AsString());

        ToolResult list = new ListDirectoryTool(root).Execute(PathArguments("."));
        Assert.True(list.IsSuccess);
        Assert.Equal("ordinary.txt", list.Output!.AsString());
    }

    [SymlinkFact]
    public void SymbolicLinkDirectoryIsRejectedOnDirectAccessAndSkippedDuringRecursiveSearch()
    {
        using TempWorkspace outside = new();
        outside.WriteFile("secret.cs", "public class SharedToken { }");

        using TempWorkspace workspace = new();
        workspace.WriteFile("src/Real.cs", "public class SharedToken { }");
        string linkedDirectory = Path.Combine(workspace.Path, "linked-dir");
        Directory.CreateSymbolicLink(linkedDirectory, outside.Path);

        WorkspaceRoot root = new(workspace.Path);

        ToolResult readThroughLink = new ReadFileTool(root).Execute(PathArguments("linked-dir/secret.cs"));
        Assert.False(readThroughLink.IsSuccess);
        Assert.Equal(FileToolErrorCodes.ReparsePointNotAllowed, readThroughLink.Error!.Code);

        ToolResult readLinkDirItself = new ReadFileTool(root).Execute(PathArguments("linked-dir"));
        Assert.False(readLinkDirItself.IsSuccess);
        Assert.Equal(FileToolErrorCodes.ReparsePointNotAllowed, readLinkDirItself.Error!.Code);

        ToolResult listLinkDir = new ListDirectoryTool(root).Execute(PathArguments("linked-dir"));
        Assert.False(listLinkDir.IsSuccess);
        Assert.Equal(FileToolErrorCodes.ReparsePointNotAllowed, listLinkDir.Error!.Code);

        ToolResult searchLinkDir = new FileSearchTool(root).Execute(QueryWithPathArguments("SharedToken", "linked-dir"));
        Assert.False(searchLinkDir.IsSuccess);
        Assert.Equal(FileToolErrorCodes.ReparsePointNotAllowed, searchLinkDir.Error!.Code);

        ToolResult recursiveSearch = new FileSearchTool(root).Execute(QueryArguments("SharedToken"));
        Assert.True(recursiveSearch.IsSuccess);
        Assert.Equal("src/Real.cs:1: public class SharedToken { }", recursiveSearch.Output!.AsString());

        ToolResult rootListing = new ListDirectoryTool(root).Execute(PathArguments("."));
        Assert.True(rootListing.IsSuccess);
        Assert.Equal("src/", rootListing.Output!.AsString());
    }

    [Fact]
    public void DirectoryReparsePointIsRejectedAndNotTraversedWithoutRequiringElevation()
    {
        using TempWorkspace outside = new();
        outside.WriteFile("escaped.txt", "reparse-target-token");

        using TempWorkspace workspace = new();
        workspace.WriteFile("local.txt", "reparse-target-token");
        string linkPath = Path.Combine(workspace.Path, "mount-link");
        CreateDirectoryLinkOrJunction(linkPath, outside.Path);

        try
        {
            WorkspaceRoot root = new(workspace.Path);

            ToolResult directRead = new ReadFileTool(root).Execute(PathArguments("mount-link/escaped.txt"));
            Assert.False(directRead.IsSuccess);
            Assert.Equal(FileToolErrorCodes.ReparsePointNotAllowed, directRead.Error!.Code);

            ToolResult search = new FileSearchTool(root).Execute(QueryArguments("reparse-target-token"));
            Assert.True(search.IsSuccess);
            Assert.Equal("local.txt:1: reparse-target-token", search.Output!.AsString());

            ToolResult listing = new ListDirectoryTool(root).Execute(PathArguments("."));
            Assert.True(listing.IsSuccess);
            Assert.Equal("local.txt", listing.Output!.AsString());
        }
        finally
        {
            if (Directory.Exists(linkPath))
            {
                Directory.Delete(linkPath, recursive: false);
            }
        }
    }

    private static void CreateDirectoryLinkOrJunction(string linkPath, string targetPath)
    {
        if (OperatingSystem.IsWindows())
        {
            ProcessStartInfo startInfo = new("cmd.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            startInfo.ArgumentList.Add("/c");
            startInfo.ArgumentList.Add("mklink");
            startInfo.ArgumentList.Add("/J");
            startInfo.ArgumentList.Add(linkPath);
            startInfo.ArgumentList.Add(targetPath);

            using Process process = Process.Start(startInfo)!;
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"mklink /J failed: {process.StandardError.ReadToEnd()}");
            return;
        }

        Directory.CreateSymbolicLink(linkPath, targetPath);
    }

    private static ArgumentBag PathArguments(string path)
    {
        return ArgumentBag.From([new ArgumentEntry("path", ArgumentValue.FromString(path))]);
    }

    private static ArgumentBag QueryArguments(string query)
    {
        return ArgumentBag.From([new ArgumentEntry("query", ArgumentValue.FromString(query))]);
    }

    private static ArgumentBag QueryWithPathArguments(string query, string path)
    {
        return ArgumentBag.From(
        [
            new ArgumentEntry("query", ArgumentValue.FromString(query)),
            new ArgumentEntry("path", ArgumentValue.FromString(path)),
        ]);
    }
}
