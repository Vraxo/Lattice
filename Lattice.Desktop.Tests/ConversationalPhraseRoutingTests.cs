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

        DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
        entry.RequestText.Should().Be(message);
        entry.Kind.Should().Be(DesktopTurnKind.SearchResults);
        entry.SearchQuery.Should().Be("WorkspaceRoot");
        entry.CapturedValue.Should().Be("WorkspaceRoot");
        entry.MatchedPatternId.Should().BeNotNull();
        entry.SafeEvidence.Length.Should().Be(2);
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

        DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
        entry.RequestText.Should().Be(message);
        entry.Kind.Should().Be(DesktopTurnKind.FileInspected);
        entry.InspectedRelativePath.Should().Be("Lattice/WorkspaceRoot.cs");
        entry.CapturedValue.Should().Be("Lattice/WorkspaceRoot.cs");
        entry.MatchedPatternId.Should().BeNotNull();
        entry.FileContents.Should().Be(expectedSource);
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

        session.Entries.Count.Should().Be(2);
        session.Entries[0].Kind.Should().Be(DesktopTurnKind.FileInspected);
        session.Entries[0].InspectedRelativePath.Should().Be("docs/Architecture Notes.md");
        session.Entries[0].FileContents.Should().Be("# Notes\n");

        session.Entries[1].Kind.Should().Be(DesktopTurnKind.AccessRejected);
    }
}
