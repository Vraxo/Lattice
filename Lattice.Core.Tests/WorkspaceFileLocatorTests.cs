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
        WorkspaceFileLocator.IsBareFileName(candidate).Should().Be(expected);
        WorkspaceFileLocator.IsBareFileName("bad\0name.cs").Should().BeFalse();
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
        lookup.IsSuccess.Should().BeTrue();
        lookup.Value.TotalMatches.Should().Be(2);
        lookup.Value.HasMoreMatches.Should().BeFalse();
        lookup.Value.SafeRelativePaths.Should().Equal(["Lattice.Cli/Program.cs", "Lattice.Desktop/Program.cs"]);

        Result<string> noRootMatch = locator.TryFindSingleRootFile("Program.cs");
        noRootMatch.IsSuccess.Should().BeTrue();
        noRootMatch.Value.Should().Be(string.Empty);

        workspace.WriteFile("README.md", "# Root");
        Result<string> singleRootMatch = locator.TryFindSingleRootFile("readme.md");
        singleRootMatch.IsSuccess.Should().BeTrue();
        singleRootMatch.Value.Should().Be("README.md");
    }

    [Fact]
    public void FindByFileNameFallsBackToFileStemOnlyWhenNoExactNameExists()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("Lattice.Cli/Program.cs", "// cli");
        workspace.WriteFile("Lattice.Desktop/Program.cs", "// desktop");

        WorkspaceFileLocator locator = new(new WorkspaceRoot(workspace.Path));

        Result<WorkspaceFileLookupResult> lookup = locator.FindByFileName("program");
        lookup.IsSuccess.Should().BeTrue();
        lookup.Value.TotalMatches.Should().Be(2);
        lookup.Value.SafeRelativePaths.Should().Equal(["Lattice.Cli/Program.cs", "Lattice.Desktop/Program.cs"]);

        workspace.WriteFile("tools/program", "exact extensionless filename");
        Result<WorkspaceFileLookupResult> exact = locator.FindByFileName("program");
        exact.IsSuccess.Should().BeTrue();
        exact.Value.TotalMatches.Should().Be(1);
        exact.Value.SafeRelativePaths.Should().Equal(["tools/program"]);
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
        programLookup.IsSuccess.Should().BeTrue();
        programLookup.Value.SafeRelativePaths.Should().Equal(["src/App/Program.cs"]);

        Result<WorkspaceFileLookupResult> binaryLookup = locator.FindByFileName("Data.dat");
        binaryLookup.IsSuccess.Should().BeTrue();
        binaryLookup.Value.TotalMatches.Should().Be(2);
        binaryLookup.Value.SafeRelativePaths.Should().Equal(["tools/BinaryTool/Data.dat", "tools/HugeTool/Data.dat"]);
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
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalMatches.Should().Be(5);
        result.Value.HasMoreMatches.Should().BeTrue();
        result.Value.OmittedFromReturnedCount.Should().Be(2);
        result.Value.SafeRelativePaths.Should().Equal(["Proj01/Program.cs", "Proj02/Program.cs", "Proj03/Program.cs"]);
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
        secretLookup.IsSuccess.Should().BeTrue();
        secretLookup.Value.SafeRelativePaths.Should().Equal(["src/Secret.cs"]);

        Result<WorkspaceFileLookupResult> nestedLookup = locator.FindByFileName("NestedSecret.cs");
        nestedLookup.IsSuccess.Should().BeTrue();
        nestedLookup.Value.TotalMatches.Should().Be(0);
        nestedLookup.Value.SafeRelativePaths.Should().BeEmpty();
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

            lookup.IsSuccess.Should().BeTrue();
            lookup.Value.TotalMatches.Should().Be(0);
            lookup.Value.SafeRelativePaths.Should().BeEmpty();
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
}
