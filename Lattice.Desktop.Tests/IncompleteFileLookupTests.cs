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

        entry.Kind.Should().Be(DesktopTurnKind.AmbiguousInput);
        entry.FileContents.Should().BeNull();
        entry.HasMoreCandidates.Should().BeTrue();
        entry.TotalCandidateCount.Should().Be(expectedMatches);
        entry.Summary.Should().Contain("may be incomplete");
        entry.Summary.Should().NotContain("couldn't find");
        entry.Summary.Should().NotContain("more than");
    }
}
