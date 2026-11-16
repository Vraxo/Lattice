using Lattice.Core;

namespace Lattice.Desktop.Tests;

public sealed class IncompleteFileLookupTests
{
    [Theory]
    [InlineData("Absent.cs", 0)]
    [InlineData("Program.cs", 1)]
    public void IncompleteLookupNeverOpensAFileOrClaimsAbsence(string requestedName, int expectedMatches)
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("a/Program.cs", "// first");
        workspace.WriteFile("b/Program.cs", "// second");
        WorkspaceRoot root = new(workspace.Path);
        ConnectedWorkspace connected = new(root, new WorkspaceFileLocator(root, maxScannedFiles: 1));
        DesktopRequestProcessor processor = new();

        DesktopTurnEntry entry = processor.Execute(connected, $"Show me {requestedName}");

        Assert.Equal(DesktopTurnKind.AmbiguousInput, entry.Kind);
        Assert.Null(entry.FileContents);
        Assert.True(entry.HasMoreCandidates);
        Assert.Equal(expectedMatches, entry.TotalCandidateCount);
        Assert.Contains("may be incomplete", entry.Summary, StringComparison.Ordinal);
        Assert.DoesNotContain("couldn't find", entry.Summary, StringComparison.Ordinal);
        Assert.DoesNotContain("more than", entry.Summary, StringComparison.Ordinal);
    }
}
