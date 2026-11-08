using System.Diagnostics;

namespace Lattice.Core.Tests;

public sealed class WorkspaceFileLocatorTests
{
    [Theory]
    [InlineData("Program.cs", true)]
    [InlineData("Directory.Build.props", true)]
    [InlineData("Architecture Notes.md", true)]
    [InlineData(".editorconfig", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(".", false)]
    [InlineData("..", false)]
    [InlineData("src/Program.cs", false)]
    [InlineData("src\\Program.cs", false)]
    [InlineData("../Program.cs", false)]
    [InlineData("/etc/passwd", false)]
    [InlineData("C:\\Temp\\Program.cs", false)]
    public void IsBareFileNameRejectsPathsSeparatorsAndNavigationSegments(string candidate, bool expected)
    {
        Assert.Equal(expected, WorkspaceFileLocator.IsBareFileName(candidate));
        Assert.False(WorkspaceFileLocator.IsBareFileName("bad\0name.cs"));
    }

    [Fact]
    public void FindByFileNameMatchesExactCaseInsensitiveBasenameAndRejectsPartialNames()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("Lattice.Cli/Program.cs", "Console.WriteLine(1);");
        workspace.WriteFile("Lattice.Desktop/Program.cs", "Console.WriteLine(2);");
        workspace.WriteFile("Lattice/MyProgram.cs", "class MyProgram { }");
        workspace.WriteFile("Lattice/Program.cs.bak", "backup");

        WorkspaceFileLocator locator = new(new WorkspaceRoot(workspace.Path));

        Result<WorkspaceFileLookupResult> lookup = locator.FindByFileName("program.CS");
        Assert.True(lookup.IsSuccess);
        Assert.Equal(2, lookup.Value.TotalMatches);
        Assert.False(lookup.Value.HasMoreMatches);
        Assert.Equal<IEnumerable<string>>(
            ["Lattice.Cli/Program.cs", "Lattice.Desktop/Program.cs"],
            lookup.Value.SafeRelativePaths);

        Result<string> noRootMatch = locator.TryFindSingleRootFile("Program.cs");
        Assert.True(noRootMatch.IsSuccess);
        Assert.Equal(string.Empty, noRootMatch.Value);

        workspace.WriteFile("README.md", "# Root");
        Result<string> singleRootMatch = locator.TryFindSingleRootFile("readme.md");
        Assert.True(singleRootMatch.IsSuccess);
        Assert.Equal("README.md", singleRootMatch.Value);
    }

    [Fact]
    public void FindByFileNameSkipsExcludedDirectoriesAndDoesNotReadFileContents()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("src/App/Program.cs", "class Program { }");
        workspace.WriteFile(".git/Program.cs", "ignored");
        workspace.WriteFile(".vs/Program.cs", "ignored");
        workspace.WriteFile("bin/Release/Program.cs", "ignored");
        workspace.WriteFile("obj/Debug/Program.cs", "ignored");
        workspace.WriteFile("artifacts/out/Program.cs", "ignored");

        // Oversized and invalid-UTF8 candidate files must still be enumerable without reading bytes.
        workspace.WriteBytes("tools/BinaryTool/Data.dat", [0xFF, 0xFE, 0xC3, 0x28]);
        workspace.WriteBytes("tools/HugeTool/Data.dat", new byte[ReadFileTool.MaxBytes + 64]);

        WorkspaceFileLocator locator = new(new WorkspaceRoot(workspace.Path));

        Result<WorkspaceFileLookupResult> programLookup = locator.FindByFileName("Program.cs");
        Assert.True(programLookup.IsSuccess);
        Assert.Equal<IEnumerable<string>>(["src/App/Program.cs"], programLookup.Value.SafeRelativePaths);

        Result<WorkspaceFileLookupResult> binaryLookup = locator.FindByFileName("Data.dat");
        Assert.True(binaryLookup.IsSuccess);
        Assert.Equal(2, binaryLookup.Value.TotalMatches);
        Assert.Equal<IEnumerable<string>>(
            ["tools/BinaryTool/Data.dat", "tools/HugeTool/Data.dat"],
            binaryLookup.Value.SafeRelativePaths);
    }

    [Fact]
    public void FindByFileNameEnforcesReturnedAndCountedResultBounds()
    {
        using TempWorkspace workspace = new();
        for (int i = 1; i <= 7; i++)
        {
            workspace.WriteFile($"Proj{i:D2}/Program.cs", $"// {i}");
        }

        WorkspaceFileLocator bounded = new(
            new WorkspaceRoot(workspace.Path),
            maxReturnedMatches: 3,
            maxCountedMatches: 5);

        Result<WorkspaceFileLookupResult> result = bounded.FindByFileName("Program.cs");
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.TotalMatches);
        Assert.True(result.Value.HasMoreMatches);
        Assert.Equal(2, result.Value.OmittedFromReturnedCount);
        Assert.Equal<IEnumerable<string>>(
            ["Proj01/Program.cs", "Proj02/Program.cs", "Proj03/Program.cs"],
            result.Value.SafeRelativePaths);
    }

    [SymlinkFact]
    public void FindByFileNameSkipsSymbolicLinkFilesAndLinkedDirectories()
    {
        using TempWorkspace outside = new();
        string outsideFile = outside.WriteFile("Secret.cs", "outside");
        outside.WriteFile("NestedSecret.cs", "outside-nested");

        using TempWorkspace workspace = new();
        workspace.WriteFile("src/Secret.cs", "inside");
        File.CreateSymbolicLink(Path.Combine(workspace.Path, "Secret.cs"), outsideFile);
        Directory.CreateSymbolicLink(Path.Combine(workspace.Path, "linked-dir"), outside.Path);

        WorkspaceFileLocator locator = new(new WorkspaceRoot(workspace.Path));

        Result<WorkspaceFileLookupResult> secretLookup = locator.FindByFileName("Secret.cs");
        Assert.True(secretLookup.IsSuccess);
        Assert.Equal<IEnumerable<string>>(["src/Secret.cs"], secretLookup.Value.SafeRelativePaths);

        Result<WorkspaceFileLookupResult> nestedLookup = locator.FindByFileName("NestedSecret.cs");
        Assert.True(nestedLookup.IsSuccess);
        Assert.Equal(0, nestedLookup.Value.TotalMatches);
        Assert.Empty(nestedLookup.Value.SafeRelativePaths);
    }

    [Fact]
    public void FindByFileNameDoesNotTraverseDirectoryJunctionOrReparsePoint()
    {
        using TempWorkspace outside = new();
        outside.WriteFile("Escaped.cs", "escaped");

        using TempWorkspace workspace = new();
        workspace.WriteFile("src/Local.cs", "local");
        string linkPath = Path.Combine(workspace.Path, "junction-dir");
        CreateDirectoryLinkOrJunction(linkPath, outside.Path);

        try
        {
            WorkspaceFileLocator locator = new(new WorkspaceRoot(workspace.Path));
            Result<WorkspaceFileLookupResult> lookup = locator.FindByFileName("Escaped.cs");

            Assert.True(lookup.IsSuccess);
            Assert.Equal(0, lookup.Value.TotalMatches);
            Assert.Empty(lookup.Value.SafeRelativePaths);
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
        Assert.True(process.ExitCode == 0, $"mklink /J failed: {process.StandardError.ReadToEnd()}");
    }
}
