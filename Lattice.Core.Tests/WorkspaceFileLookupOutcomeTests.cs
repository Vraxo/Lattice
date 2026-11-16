namespace Lattice.Core.Tests;

public sealed class WorkspaceFileLookupOutcomeTests
{
    [Fact]
    public void UniqueMatchCarriesOnlyResolvedPath()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("src/Program.cs", "class Program { }");
        WorkspaceFileLocator locator = new(new WorkspaceRoot(workspace.Path));

        WorkspaceFileLookupOutcome outcome = locator.ResolveByFileName("program.CS");

        WorkspaceFileFound found = Assert.IsType<WorkspaceFileFound>(outcome.Value);
        Assert.Equal("src/Program.cs", found.RelativePath);
    }

    [Fact]
    public void MissingMatchCarriesRequestedName()
    {
        using TempWorkspace workspace = new();
        WorkspaceFileLocator locator = new(new WorkspaceRoot(workspace.Path));

        WorkspaceFileLookupOutcome outcome = locator.ResolveByFileName("Absent.cs");

        Assert.Equal("Absent.cs", Assert.IsType<WorkspaceFileMissing>(outcome.Value).RequestedFileName);
    }

    [Fact]
    public void AmbiguityPreservesAllCandidateMetadata()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("a/Program.cs", "a");
        workspace.WriteFile("b/Program.cs", "b");
        WorkspaceFileLocator locator = new(new WorkspaceRoot(workspace.Path), maxReturnedMatches: 1);

        WorkspaceFileLookupOutcome outcome = locator.ResolveByFileName("Program.cs");

        WorkspaceFileLookupResult matches = Assert.IsType<WorkspaceFileAmbiguous>(outcome.Value).Matches;
        Assert.Equal(2, matches.TotalMatches);
        Assert.Single(matches.SafeRelativePaths);
        Assert.Equal(1, matches.OmittedFromReturnedCount);
    }

    [Theory]
    [InlineData("Absent.cs", 0)]
    [InlineData("Program.cs", 1)]
    public void ScanLimitCannotBecomeMissingOrUnique(string requestedName, int expectedMatches)
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("a/Program.cs", "a");
        workspace.WriteFile("b/Program.cs", "b");
        WorkspaceFileLocator locator = new(new WorkspaceRoot(workspace.Path), maxScannedFiles: 1);

        WorkspaceFileLookupOutcome outcome = locator.ResolveByFileName(requestedName);

        WorkspaceFileLookupResult matches = Assert.IsType<WorkspaceFileLookupIncomplete>(outcome.Value).Matches;
        Assert.Equal(expectedMatches, matches.TotalMatches);
        Assert.True(matches.HasMoreMatches);
    }

    [Fact]
    public void InvalidPathPreservesSafetyError()
    {
        using TempWorkspace workspace = new();
        WorkspaceFileLocator locator = new(new WorkspaceRoot(workspace.Path));

        WorkspaceFileLookupOutcome outcome = locator.ResolveByFileName("../Program.cs");

        Assert.Equal(FileToolErrorCodes.QueryInvalid,
            Assert.IsType<WorkspaceFileLookupFailed>(outcome.Value).Error.Code);
    }

    [Fact]
    public void CancellationStillPropagates()
    {
        using TempWorkspace workspace = new();
        WorkspaceFileLocator locator = new(new WorkspaceRoot(workspace.Path));
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => locator.ResolveByFileName("Program.cs", cancellation.Token));
    }
}
