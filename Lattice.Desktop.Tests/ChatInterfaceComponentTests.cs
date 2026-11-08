using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Lattice.Desktop.Tests;

public sealed class ChatInterfaceComponentTests
{
    [Fact]
    public async Task InitialChatLayoutRendersMultilineComposerEmptyStateAndNoCommandSyntaxInstructions()
    {
        await using BunitContext context = new();
        context.Services.AddSingleton<DesktopSession>();

        IRenderedComponent<App> component = context.Render<App>();

        Assert.Empty(component.FindAll("#request-form"));
        Assert.DoesNotContain("Workspace Request", component.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("inspect <", component.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("find symbol <", component.Markup, StringComparison.OrdinalIgnoreCase);

        var composer = component.Find("textarea#message-input");
        Assert.Equal("TEXTAREA", composer.NodeName);
        Assert.Equal("Send", component.Find("#send-message").TextContent.Trim());

        var emptyState = component.Find("#chat-empty-state");
        Assert.Contains("Connect a workspace folder", emptyState.TextContent, StringComparison.Ordinal);
        Assert.Contains("Where does WorkspaceRoot appear?", emptyState.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConnectedEmptyStateInvitesConversationalQuestionsWithoutCommandSyntax()
    {
        using DesktopTempWorkspace workspace = new();
        await using BunitContext context = new();
        context.Services.AddSingleton<DesktopSession>();

        IRenderedComponent<App> component = context.Render<App>();
        await component.Find("#workspace-path").InputAsync(workspace.Path);
        await component.Find("#workspace-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        var emptyState = component.Find("#chat-empty-state");
        Assert.Contains("Ask about your codebase", emptyState.TextContent, StringComparison.Ordinal);
        Assert.Contains(Path.GetFullPath(workspace.Path), emptyState.TextContent, StringComparison.Ordinal);
        Assert.Contains("Where is WorkspaceRoot used in this project?", emptyState.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("inspect <", emptyState.TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("find symbol <", emptyState.TextContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EnterSubmitsMessageAppendsDistinctUserAndAssistantTurnsAndRestoresFocus()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("README.md", "# Sample Repo\n");

        await using BunitContext context = new();
        DesktopSession session = new();
        context.Services.AddSingleton(session);

        IRenderedComponent<App> component = context.Render<App>();
        await component.Find("#workspace-path").InputAsync(workspace.Path);
        await component.Find("#workspace-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        await component.Find("#message-input").InputAsync("inspect README.md");
        await component.Find("#message-input").KeyDownAsync(new KeyboardEventArgs
        {
            Key = "Enter",
            ShiftKey = false,
        });

        Assert.Equal(string.Empty, session.MessageInput);
        var exchange = Assert.Single(component.FindAll("#turn-history > li.chat-exchange"));
        var userBubble = exchange.QuerySelector("[data-message-role='user']")!;
        var assistantBubble = exchange.QuerySelector("[data-message-role='assistant']")!;

        Assert.Contains("user-message", userBubble.ClassList);
        Assert.Contains("assistant-message", assistantBubble.ClassList);
        Assert.Contains("inspect README.md", userBubble.QuerySelector(".user-body")!.TextContent, StringComparison.Ordinal);
        Assert.Equal("README.md", assistantBubble.QuerySelector(".file-path")!.TextContent);
        Assert.Contains("# Sample Repo", assistantBubble.QuerySelector(".file-contents")!.TextContent, StringComparison.Ordinal);

        context.JSInterop.VerifyFocusAsyncInvoke();
    }

    [Fact]
    public async Task ShiftEnterDoesNotSubmitAndPreservesMultilineTextInComposer()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("README.md", "# Multiline test\n");

        await using BunitContext context = new();
        DesktopSession session = new();
        context.Services.AddSingleton(session);

        IRenderedComponent<App> component = context.Render<App>();
        await component.Find("#workspace-path").InputAsync(workspace.Path);
        await component.Find("#workspace-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        await component.Find("#message-input").InputAsync("inspect");
        await component.Find("#message-input").KeyDownAsync(new KeyboardEventArgs
        {
            Key = "Enter",
            ShiftKey = true,
        });

        Assert.Empty(session.Entries);
        Assert.Equal("inspect", session.MessageInput);

        await component.Find("#message-input").InputAsync("inspect\nREADME.md");
        await component.Find("#send-message").ClickAsync();

        var exchange = Assert.Single(component.FindAll("#turn-history > li.chat-exchange"));
        Assert.Equal("inspect\nREADME.md", exchange.QuerySelector(".user-body")!.TextContent);
        Assert.Equal("FileInspected", exchange.GetAttribute("data-turn-kind"));
    }

    [Fact]
    public async Task InFlightMessageClearsComposerDisablesSendAndRendersInConversationPendingTurn()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("README.md", "# Async test\n");
        TaskCompletionSource<DesktopTurnEntry> pending =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        await using BunitContext context = new();
        DesktopSession session = new(executeRequest: (_, _, _) => pending.Task);
        context.Services.AddSingleton(session);

        IRenderedComponent<App> component = context.Render<App>();
        await component.Find("#workspace-path").InputAsync(workspace.Path);
        await component.Find("#workspace-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        await component.Find("#message-input").InputAsync("inspect README.md");
        Task inflight = component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);
        try
        {
            component.WaitForAssertion(() =>
            {
                Assert.Equal(string.Empty, session.MessageInput);
                Assert.True(component.Find("#chat-form fieldset").HasAttribute("disabled"));
                Assert.Contains("Sending", component.Find("#send-message").TextContent, StringComparison.Ordinal);

                var pendingTurn = component.Find("#pending-turn");
                Assert.Contains("inspect README.md", pendingTurn.QuerySelector(".user-body")!.TextContent, StringComparison.Ordinal);
                Assert.Contains("Checking the workspace", pendingTurn.QuerySelector(".pending-text")!.TextContent, StringComparison.Ordinal);
                Assert.DoesNotContain("Running command", component.Markup, StringComparison.OrdinalIgnoreCase);
            });
        }
        finally
        {
            pending.TrySetResult(
                DesktopRequestProcessor.Default.Execute(session.ActiveConnection, "inspect README.md"));
        }

        await inflight;

        Assert.Empty(component.FindAll("#pending-turn"));
        Assert.False(component.Find("#chat-form fieldset").HasAttribute("disabled"));
        Assert.Single(component.FindAll("#turn-history > li.chat-exchange"));
    }

    [Fact]
    public async Task ValidationAndUnexpectedFailureDoNotLoseComposerText()
    {
        using DesktopTempWorkspace workspace = new();
        DesktopSession session = new(
            executeRequest: (_, _, _) => throw new InvalidOperationException("Simulated crash."));

        await using BunitContext context = new();
        context.Services.AddSingleton(session);
        IRenderedComponent<App> component = context.Render<App>();

        await component.Find("#workspace-path").InputAsync(workspace.Path);
        await component.Find("#workspace-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        await component.Find("#message-input").InputAsync("   ");
        await component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        Assert.Equal("   ", session.MessageInput);
        Assert.Empty(session.Entries);
        Assert.Single(component.FindAll("#composer-error[role='alert']"));

        await component.Find("#message-input").InputAsync("Where does WorkspaceRoot appear?");
        await component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        Assert.Equal("Where does WorkspaceRoot appear?", session.MessageInput);
        DesktopTurnEntry failure = Assert.Single(session.Entries);
        Assert.Equal(DesktopTurnKind.UnexpectedError, failure.Kind);
    }
}
