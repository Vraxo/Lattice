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
        read.IsSuccess.Should().BeTrue();
        read.Output!.AsString().Should().Be("public sealed class Example { }");

        ToolResult search = new FileSearchTool(root).Execute(QueryArguments("Example"));
        search.IsSuccess.Should().BeTrue();
        search.Output!.AsString().Should().Be("src/Example.cs:1: public sealed class Example { }");

        ToolResult list = new ListDirectoryTool(root).Execute(PathArguments("src"));
        list.IsSuccess.Should().BeTrue();
        list.Output!.AsString().Should().Be("Example.cs");
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
        read.IsSuccess.Should().BeFalse();
        read.Error!.Code.Should().Be(FileToolErrorCodes.PathOutsideRoot);

        ToolResult list = new ListDirectoryTool(root).Execute(PathArguments(escapingPath));
        list.IsSuccess.Should().BeFalse();
        list.Error!.Code.Should().Be(FileToolErrorCodes.PathOutsideRoot);

        ToolResult search = new FileSearchTool(root).Execute(QueryWithPathArguments("needle", escapingPath));
        search.IsSuccess.Should().BeFalse();
        search.Error!.Code.Should().Be(FileToolErrorCodes.PathOutsideRoot);
    }

    [Fact]
    public void RootedAndAbsolutePathsAreRejectedAcrossWorkspaceTools()
    {
        using TempWorkspace workspace = new();
        string fullFilePath = workspace.WriteFile("inside.txt", "needle");
        WorkspaceRoot root = new(workspace.Path);

        ToolResult read = new ReadFileTool(root).Execute(PathArguments(fullFilePath));
        read.IsSuccess.Should().BeFalse();
        read.Error!.Code.Should().Be(FileToolErrorCodes.PathOutsideRoot);

        ToolResult list = new ListDirectoryTool(root).Execute(PathArguments(workspace.Path));
        list.IsSuccess.Should().BeFalse();
        list.Error!.Code.Should().Be(FileToolErrorCodes.PathOutsideRoot);

        ToolResult search = new FileSearchTool(root).Execute(QueryWithPathArguments("needle", workspace.Path));
        search.IsSuccess.Should().BeFalse();
        search.Error!.Code.Should().Be(FileToolErrorCodes.PathOutsideRoot);
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
        directRead.IsSuccess.Should().BeFalse();
        directRead.Error!.Code.Should().Be(FileToolErrorCodes.ReparsePointNotAllowed);

        ToolResult search = new FileSearchTool(root).Execute(QueryArguments("needle"));
        search.IsSuccess.Should().BeTrue();
        search.Output!.AsString().Should().Be("ordinary.txt:1: needle-ordinary-inside");

        ToolResult list = new ListDirectoryTool(root).Execute(PathArguments("."));
        list.IsSuccess.Should().BeTrue();
        list.Output!.AsString().Should().Be("ordinary.txt");
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
        readThroughLink.IsSuccess.Should().BeFalse();
        readThroughLink.Error!.Code.Should().Be(FileToolErrorCodes.ReparsePointNotAllowed);

        ToolResult readLinkDirItself = new ReadFileTool(root).Execute(PathArguments("linked-dir"));
        readLinkDirItself.IsSuccess.Should().BeFalse();
        readLinkDirItself.Error!.Code.Should().Be(FileToolErrorCodes.ReparsePointNotAllowed);

        ToolResult listLinkDir = new ListDirectoryTool(root).Execute(PathArguments("linked-dir"));
        listLinkDir.IsSuccess.Should().BeFalse();
        listLinkDir.Error!.Code.Should().Be(FileToolErrorCodes.ReparsePointNotAllowed);

        ToolResult searchLinkDir = new FileSearchTool(root).Execute(QueryWithPathArguments("SharedToken", "linked-dir"));
        searchLinkDir.IsSuccess.Should().BeFalse();
        searchLinkDir.Error!.Code.Should().Be(FileToolErrorCodes.ReparsePointNotAllowed);

        ToolResult recursiveSearch = new FileSearchTool(root).Execute(QueryArguments("SharedToken"));
        recursiveSearch.IsSuccess.Should().BeTrue();
        recursiveSearch.Output!.AsString().Should().Be("src/Real.cs:1: public class SharedToken { }");

        ToolResult rootListing = new ListDirectoryTool(root).Execute(PathArguments("."));
        rootListing.IsSuccess.Should().BeTrue();
        rootListing.Output!.AsString().Should().Be("src/");
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
            directRead.IsSuccess.Should().BeFalse();
            directRead.Error!.Code.Should().Be(FileToolErrorCodes.ReparsePointNotAllowed);

            ToolResult search = new FileSearchTool(root).Execute(QueryArguments("reparse-target-token"));
            search.IsSuccess.Should().BeTrue();
            search.Output!.AsString().Should().Be("local.txt:1: reparse-target-token");

            ToolResult listing = new ListDirectoryTool(root).Execute(PathArguments("."));
            listing.IsSuccess.Should().BeTrue();
            listing.Output!.AsString().Should().Be("local.txt");
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
        try
        {
            Directory.CreateSymbolicLink(linkPath, targetPath);
            return;
        }
        catch (Exception exception) when (
            OperatingSystem.IsWindows()
            && exception is IOException or UnauthorizedAccessException)
        {
            // Fall back to an unprivileged NTFS junction when SeCreateSymbolicLinkPrivilege is absent.
        }

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
        (process.ExitCode == 0).Should().BeTrue("{0}", $"mklink /J failed: {process.StandardError.ReadToEnd()}" ?? string.Empty);
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
