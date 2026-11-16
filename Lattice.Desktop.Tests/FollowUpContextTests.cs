using Bunit;
using Lattice.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Lattice.Desktop.Tests;

public sealed class FollowUpContextTests
{
    [Theory]
    [InlineData("Show me that file.")]
    [InlineData("Open it.")]
    [InlineData("What's in that file?")]
    [InlineData("Show that file.")]
    public async Task FollowUpResolvesWhenPrecedingSearchMatchesExactlyOneFile(string followUp)
    {
        using DesktopTempWorkspace workspace = new();
        const string source = "namespace Demo;\n\npublic sealed class SoleTarget\n{\n    public void Run() { }\n}\n";
        workspace.WriteFile("src/SoleTarget.cs", source);
        workspace.WriteFile("src/Unrelated.cs", "namespace Demo;\npublic sealed class Unrelated { }\n");

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "Where does SoleTarget appear?";
        await session.SendMessageAsync();

        session.MessageInput = followUp;
        await session.SendMessageAsync();

        session.Entries.Count.Should().Be(2);
        session.Entries[0].Kind.Should().Be(DesktopTurnKind.SearchResults);
        session.Entries[1].Kind.Should().Be(DesktopTurnKind.FileInspected);
        session.Entries[1].InspectedRelativePath.Should().Be("src/SoleTarget.cs");
        session.Entries[1].FileContents.Should().Be(source);
    }

    [Fact]
    public async Task FollowUpAsksWhichFileWhenPrecedingSearchMatchesMultipleFiles()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("src/First.cs", "public sealed class SharedType { }");
        workspace.WriteFile("src/Second.cs", "internal sealed class UsesSharedType { SharedType? Item; }");

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "Where is SharedType used in this project?";
        await session.SendMessageAsync();

        session.MessageInput = "Show me that file.";
        await session.SendMessageAsync();

        session.Entries.Count.Should().Be(2);
        DesktopTurnEntry clarification = session.Entries[1];
        clarification.Kind.Should().Be(DesktopTurnKind.AmbiguousInput);
        clarification.FileContents.Should().BeNull();
        clarification.InspectedRelativePath.Should().BeNull();
        clarification.Summary.Should().Contain("src/First.cs");
        clarification.Summary.Should().Contain("src/Second.cs");
        clarification.Summary.Should().Contain("Which file");
    }

    [Fact]
    public async Task FollowUpWithoutPrecedingReferenceOrAfterNoMatchesAsksForSpecificFile()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("README.md", "# Demo\nContains PresentToken.\n");

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "Show me that file.";
        await session.SendMessageAsync();

        session.Entries[0].Kind.Should().Be(DesktopTurnKind.AmbiguousInput);
        session.Entries[0].FileContents.Should().BeNull();
        session.Entries[0].Summary.Should().Contain("Which file");

        session.MessageInput = "Where does PresentToken appear?";
        await session.SendMessageAsync();
        session.RecentFileContext.Kind.Should().Be(RecentFileContextKind.SingleFile);

        session.MessageInput = "Where does AbsentToken appear?";
        await session.SendMessageAsync();
        session.RecentFileContext.Kind.Should().Be(RecentFileContextKind.None);

        session.MessageInput = "Open it.";
        await session.SendMessageAsync();
        session.Entries[3].Kind.Should().Be(DesktopTurnKind.AmbiguousInput);
        session.Entries[3].FileContents.Should().BeNull();
    }

    [Fact]
    public async Task SwitchingWorkspaceClearsRecentFileReferenceAndPreventsCrossWorkspaceRead()
    {
        using DesktopTempWorkspace firstWorkspace = new();
        using DesktopTempWorkspace secondWorkspace = new();
        firstWorkspace.WriteFile("src/Config.cs", "public static class Config { public const int Port = 8080; }");
        secondWorkspace.WriteFile("src/Config.cs", "SECOND_WORKSPACE_SENSITIVE_CONTENT");

        DesktopSession session = await ConnectSessionAsync(firstWorkspace.Path);
        session.MessageInput = "Where does Port appear?";
        await session.SendMessageAsync();
        session.RecentFileContext.SingleRelativePath.Should().Be("src/Config.cs");

        session.WorkspacePathInput = secondWorkspace.Path;
        await session.ConnectAsync();
        session.RecentFileContext.Kind.Should().Be(RecentFileContextKind.None);

        session.MessageInput = "Show me that file.";
        await session.SendMessageAsync();

        DesktopTurnEntry turnAfterSwitch = session.Entries[^1];
        turnAfterSwitch.Kind.Should().Be(DesktopTurnKind.AmbiguousInput);
        turnAfterSwitch.FileContents.Should().BeNull();
        turnAfterSwitch.Summary.Should().NotContain("SECOND_WORKSPACE_SENSITIVE_CONTENT");
    }

    [Fact]
    public async Task FollowUpHandlesDeletedTargetThroughSafeReadBoundary()
    {
        using DesktopTempWorkspace workspace = new();
        string fullFilePath = workspace.WriteFile("src/Temporary.cs", "public sealed class TemporarySymbol { }");

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "Where does TemporarySymbol appear?";
        await session.SendMessageAsync();

        File.Delete(fullFilePath);

        session.MessageInput = "Open it.";
        await session.SendMessageAsync();

        DesktopTurnEntry missingTurn = session.Entries[1];
        missingTurn.Kind.Should().Be(DesktopTurnKind.MissingFile);
        missingTurn.ErrorCode.Should().Be(FileToolErrorCodes.PathNotFound);
        missingTurn.FileContents.Should().BeNull();
        session.RecentFileContext.Kind.Should().Be(RecentFileContextKind.None);
    }

    [Fact]
    public async Task RenderedChatShowsAmbiguousFollowUpClarificationWithoutUnintendedReadThenOpensNamedFile()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("src/One.cs", "public sealed class CommonHelper { }");
        workspace.WriteFile("src/Two.cs", "internal sealed class Caller { CommonHelper? Helper; }");

        await using BunitContext context = new();
        DesktopSession session = new();
        context.Services.AddSingleton(session);
        IRenderedComponent<App> component = context.Render<App>();

        await component.Find("#workspace-path").InputAsync(workspace.Path);
        await component.Find("#workspace-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        await component.Find("#message-input").InputAsync("Find all occurrences of CommonHelper.");
        await component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        await component.Find("#message-input").InputAsync("Show me that file.");
        await component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        var exchanges = component.FindAll("#turn-history > li.chat-exchange");
        exchanges.Count.Should().Be(2);
        exchanges[1].GetAttribute("data-turn-kind").Should().Be("AmbiguousInput");
        exchanges[1].QuerySelectorAll(".file-viewer").Should().BeEmpty();
        string assistantReply = exchanges[1].QuerySelector("[data-message-role='assistant']")!.TextContent;
        assistantReply.Should().Contain("src/One.cs");
        assistantReply.Should().Contain("src/Two.cs");
        assistantReply.Should().Contain("Which file would you like me to open?");

        await component.Find("#message-input").InputAsync("Show me src/Two.cs.");
        await component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        exchanges = component.FindAll("#turn-history > li.chat-exchange");
        exchanges.Count.Should().Be(3);
        exchanges[2].GetAttribute("data-turn-kind").Should().Be("FileInspected");
        exchanges[2].QuerySelector(".file-path")!.TextContent.Should().Be("src/Two.cs");
        exchanges[2].QuerySelector(".file-contents")!.TextContent.Should().Contain("internal sealed class Caller");
    }

    private static async Task<DesktopSession> ConnectSessionAsync(string workspacePath)
    {
        DesktopSession session = new()
        {
            WorkspacePathInput = workspacePath,
        };
        await session.ConnectAsync();
        session.IsConnected.Should().BeTrue();
        return session;
    }
}
