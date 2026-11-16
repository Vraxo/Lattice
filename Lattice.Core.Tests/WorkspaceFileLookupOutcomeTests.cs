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

        WorkspaceFileFound found = outcome.Value.Should().BeOfType<WorkspaceFileFound>().Which;
        found.RelativePath.Should().Be("src/Program.cs");
    }

    [Fact]
    public void MissingMatchCarriesRequestedName()
    {
        using TempWorkspace workspace = new();
        WorkspaceFileLocator locator = new(new WorkspaceRoot(workspace.Path));

        WorkspaceFileLookupOutcome outcome = locator.ResolveByFileName("Absent.cs");

        outcome.Value.Should().BeOfType<WorkspaceFileMissing>().Which.RequestedFileName.Should().Be("Absent.cs");
    }

    [Fact]
    public void AmbiguityPreservesAllCandidateMetadata()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("a/Program.cs", "a");
        workspace.WriteFile("b/Program.cs", "b");
        WorkspaceFileLocator locator = new(new WorkspaceRoot(workspace.Path), maxReturnedMatches: 1);

        WorkspaceFileLookupOutcome outcome = locator.ResolveByFileName("Program.cs");

        WorkspaceFileLookupResult matches = outcome.Value.Should().BeOfType<WorkspaceFileAmbiguous>().Which.Matches;
        matches.TotalMatches.Should().Be(2);
        matches.SafeRelativePaths.Should().ContainSingle();
        matches.OmittedFromReturnedCount.Should().Be(1);
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

        WorkspaceFileLookupResult matches = outcome.Value.Should().BeOfType<WorkspaceFileLookupIncomplete>().Which.Matches;
        matches.TotalMatches.Should().Be(expectedMatches);
        matches.HasMoreMatches.Should().BeTrue();
    }

    [Fact]
    public void InvalidPathPreservesSafetyError()
    {
        using TempWorkspace workspace = new();
        WorkspaceFileLocator locator = new(new WorkspaceRoot(workspace.Path));

        WorkspaceFileLookupOutcome outcome = locator.ResolveByFileName("../Program.cs");

        outcome.Value.Should().BeOfType<WorkspaceFileLookupFailed>().Which.Error.Code.Should().Be(FileToolErrorCodes.QueryInvalid);
    }

    [Fact]
    public void CancellationStillPropagates()
    {
        using TempWorkspace workspace = new();
        WorkspaceFileLocator locator = new(new WorkspaceRoot(workspace.Path));
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        FluentActions.Invoking(() => locator.ResolveByFileName("Program.cs", cancellation.Token)).Should().ThrowExactly<OperationCanceledException>();
    }
}
