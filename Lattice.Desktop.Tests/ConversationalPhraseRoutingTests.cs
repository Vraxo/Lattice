namespace Lattice.Desktop.Tests;

public sealed class ConversationalPhraseRoutingTests
{
    [Theory]
    [InlineData("Where does WorkspaceRoot appear?")]
    [InlineData("Where is WorkspaceRoot used in this project?")]
    [InlineData("Can you find where WorkspaceRoot is used throughout this project?")]
    [InlineData("Search this project for WorkspaceRoot.")]
    [InlineData("Find all occurrences of WorkspaceRoot.")]
    [InlineData("find symbol WorkspaceRoot")]
    public async Task NaturalSearchPhrasesRouteToSourceTextSearchAndExposeDiagnostics(string message)
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("Lattice/WorkspaceRoot.cs", "namespace Lattice.Core;\npublic sealed class WorkspaceRoot { }\n");
        workspace.WriteFile("README.md", "WorkspaceRoot confines file paths.\n");

        DesktopSession session = new()
        {
            WorkspacePathInput = workspace.Path,
        };
        await session.ConnectAsync();

        session.MessageInput = message;
        await session.SendMessageAsync();

        DesktopTurnEntry entry = Assert.Single(session.Entries);
        Assert.Equal(message, entry.RequestText);
        Assert.Equal(DesktopTurnKind.SearchResults, entry.Kind);
        Assert.Equal("WorkspaceRoot", entry.SearchQuery);
        Assert.Equal("WorkspaceRoot", entry.CapturedValue);
        Assert.NotNull(entry.MatchedPatternId);
        Assert.Equal(2, entry.SafeEvidence.Length);
    }

    [Theory]
    [InlineData("Show me Lattice/WorkspaceRoot.cs.")]
    [InlineData("Open the file Lattice/WorkspaceRoot.cs.")]
    [InlineData("Can you read Lattice/WorkspaceRoot.cs?")]
    [InlineData("What's in Lattice/WorkspaceRoot.cs?")]
    [InlineData("open Lattice/WorkspaceRoot.cs")]
    [InlineData("read Lattice/WorkspaceRoot.cs")]
    [InlineData("inspect Lattice/WorkspaceRoot.cs")]
    public async Task NaturalInspectPhrasesRouteThroughReadFileToolAndPreserveContents(string message)
    {
        using DesktopTempWorkspace workspace = new();
        const string expectedSource = "namespace Lattice.Core;\n\npublic sealed class WorkspaceRoot\n{\n}\n";
        workspace.WriteFile("Lattice/WorkspaceRoot.cs", expectedSource);

        DesktopSession session = new()
        {
            WorkspacePathInput = workspace.Path,
        };
        await session.ConnectAsync();

        session.MessageInput = message;
        await session.SendMessageAsync();

        DesktopTurnEntry entry = Assert.Single(session.Entries);
        Assert.Equal(message, entry.RequestText);
        Assert.Equal(DesktopTurnKind.FileInspected, entry.Kind);
        Assert.Equal("Lattice/WorkspaceRoot.cs", entry.InspectedRelativePath);
        Assert.Equal("Lattice/WorkspaceRoot.cs", entry.CapturedValue);
        Assert.NotNull(entry.MatchedPatternId);
        Assert.Equal(expectedSource, entry.FileContents);
    }

    [Fact]
    public async Task NaturalInspectPhraseSupportsRelativePathWithSpacesAndRejectsRootEscape()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("docs/Architecture Notes.md", "# Notes\n");

        DesktopSession session = new()
        {
            WorkspacePathInput = workspace.Path,
        };
        await session.ConnectAsync();

        session.MessageInput = "Open the file docs/Architecture Notes.md.";
        await session.SendMessageAsync();

        session.MessageInput = "Show me ../outside.txt.";
        await session.SendMessageAsync();

        Assert.Equal(2, session.Entries.Count);
        Assert.Equal(DesktopTurnKind.FileInspected, session.Entries[0].Kind);
        Assert.Equal("docs/Architecture Notes.md", session.Entries[0].InspectedRelativePath);
        Assert.Equal("# Notes\n", session.Entries[0].FileContents);

        Assert.Equal(DesktopTurnKind.AccessRejected, session.Entries[1].Kind);
    }
}
