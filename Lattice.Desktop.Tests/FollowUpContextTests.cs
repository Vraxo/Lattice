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

        Assert.Equal(2, session.Entries.Count);
        Assert.Equal(DesktopTurnKind.SearchResults, session.Entries[0].Kind);
        Assert.Equal(DesktopTurnKind.FileInspected, session.Entries[1].Kind);
        Assert.Equal("src/SoleTarget.cs", session.Entries[1].InspectedRelativePath);
        Assert.Equal(source, session.Entries[1].FileContents);
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

        Assert.Equal(2, session.Entries.Count);
        DesktopTurnEntry clarification = session.Entries[1];
        Assert.Equal(DesktopTurnKind.AmbiguousInput, clarification.Kind);
        Assert.Null(clarification.FileContents);
        Assert.Null(clarification.InspectedRelativePath);
        Assert.Contains("src/First.cs", clarification.Summary, StringComparison.Ordinal);
        Assert.Contains("src/Second.cs", clarification.Summary, StringComparison.Ordinal);
        Assert.Contains("Which file", clarification.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FollowUpWithoutPrecedingReferenceOrAfterNoMatchesAsksForSpecificFile()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("README.md", "# Demo\nContains PresentToken.\n");

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "Show me that file.";
        await session.SendMessageAsync();

        Assert.Equal(DesktopTurnKind.AmbiguousInput, session.Entries[0].Kind);
        Assert.Null(session.Entries[0].FileContents);
        Assert.Contains("Which file", session.Entries[0].Summary, StringComparison.Ordinal);

        session.MessageInput = "Where does PresentToken appear?";
        await session.SendMessageAsync();
        Assert.Equal(RecentFileContextKind.SingleFile, session.RecentFileContext.Kind);

        session.MessageInput = "Where does AbsentToken appear?";
        await session.SendMessageAsync();
        Assert.Equal(RecentFileContextKind.None, session.RecentFileContext.Kind);

        session.MessageInput = "Open it.";
        await session.SendMessageAsync();
        Assert.Equal(DesktopTurnKind.AmbiguousInput, session.Entries[3].Kind);
        Assert.Null(session.Entries[3].FileContents);
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
        Assert.Equal("src/Config.cs", session.RecentFileContext.SingleRelativePath);

        session.WorkspacePathInput = secondWorkspace.Path;
        await session.ConnectAsync();
        Assert.Equal(RecentFileContextKind.None, session.RecentFileContext.Kind);

        session.MessageInput = "Show me that file.";
        await session.SendMessageAsync();

        DesktopTurnEntry turnAfterSwitch = session.Entries[^1];
        Assert.Equal(DesktopTurnKind.AmbiguousInput, turnAfterSwitch.Kind);
        Assert.Null(turnAfterSwitch.FileContents);
        Assert.DoesNotContain("SECOND_WORKSPACE_SENSITIVE_CONTENT", turnAfterSwitch.Summary, StringComparison.Ordinal);
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
        Assert.Equal(DesktopTurnKind.MissingFile, missingTurn.Kind);
        Assert.Equal(FileToolErrorCodes.PathNotFound, missingTurn.ErrorCode);
        Assert.Null(missingTurn.FileContents);
        Assert.Equal(RecentFileContextKind.None, session.RecentFileContext.Kind);
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
        Assert.Equal(2, exchanges.Count);
        Assert.Equal("AmbiguousInput", exchanges[1].GetAttribute("data-turn-kind"));
        Assert.Empty(exchanges[1].QuerySelectorAll(".file-viewer"));
        string assistantReply = exchanges[1].QuerySelector("[data-message-role='assistant']")!.TextContent;
        Assert.Contains("src/One.cs", assistantReply, StringComparison.Ordinal);
        Assert.Contains("src/Two.cs", assistantReply, StringComparison.Ordinal);
        Assert.Contains("Which file would you like me to open?", assistantReply, StringComparison.Ordinal);

        await component.Find("#message-input").InputAsync("Show me src/Two.cs.");
        await component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        exchanges = component.FindAll("#turn-history > li.chat-exchange");
        Assert.Equal(3, exchanges.Count);
        Assert.Equal("FileInspected", exchanges[2].GetAttribute("data-turn-kind"));
        Assert.Equal("src/Two.cs", exchanges[2].QuerySelector(".file-path")!.TextContent);
        Assert.Contains("internal sealed class Caller", exchanges[2].QuerySelector(".file-contents")!.TextContent, StringComparison.Ordinal);
    }

    private static async Task<DesktopSession> ConnectSessionAsync(string workspacePath)
    {
        DesktopSession session = new()
        {
            WorkspacePathInput = workspacePath,
        };
        await session.ConnectAsync();
        Assert.True(session.IsConnected);
        return session;
    }
}
