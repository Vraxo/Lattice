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

        component.FindAll("#request-form").Should().BeEmpty();
        component.Markup.Should().NotContain("Workspace Request");
        component.Markup.Should().NotContainEquivalentOf("inspect <");
        component.Markup.Should().NotContainEquivalentOf("find symbol <");

        var composer = component.Find("textarea#message-input");
        composer.NodeName.Should().Be("TEXTAREA");
        component.Find("#send-message").TextContent.Trim().Should().Be("Send");

        var emptyState = component.Find("#chat-empty-state");
        emptyState.TextContent.Should().Contain("Connect a workspace folder");
        emptyState.TextContent.Should().Contain("Where does WorkspaceRoot appear?");
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
        emptyState.TextContent.Should().Contain("Ask about your codebase");
        emptyState.TextContent.Should().Contain(Path.GetFullPath(workspace.Path));
        emptyState.TextContent.Should().Contain("Where is WorkspaceRoot used in this project?");
        emptyState.TextContent.Should().NotContainEquivalentOf("inspect <");
        emptyState.TextContent.Should().NotContainEquivalentOf("find symbol <");
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

        session.MessageInput.Should().Be(string.Empty);
        var exchange = component.FindAll("#turn-history > li.chat-exchange").Should().ContainSingle().Which;
        var userBubble = exchange.QuerySelector("[data-message-role='user']")!;
        var assistantBubble = exchange.QuerySelector("[data-message-role='assistant']")!;

        userBubble.ClassList.Should().Contain("user-message");
        assistantBubble.ClassList.Should().Contain("assistant-message");
        userBubble.QuerySelector(".user-body")!.TextContent.Should().Contain("inspect README.md");
        assistantBubble.QuerySelector(".file-path")!.TextContent.Should().Be("README.md");
        assistantBubble.QuerySelector(".file-contents")!.TextContent.Should().Contain("# Sample Repo");

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

        session.Entries.Should().BeEmpty();
        session.MessageInput.Should().Be("inspect");

        await component.Find("#message-input").InputAsync("inspect\nREADME.md");
        await component.Find("#send-message").ClickAsync();

        var exchange = component.FindAll("#turn-history > li.chat-exchange").Should().ContainSingle().Which;
        exchange.QuerySelector(".user-body")!.TextContent.Should().Be("inspect\nREADME.md");
        exchange.GetAttribute("data-turn-kind").Should().Be("FileInspected");
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
                session.MessageInput.Should().Be(string.Empty);
                component.Find("#chat-form fieldset").HasAttribute("disabled").Should().BeTrue();
                component.Find("#send-message").TextContent.Should().Contain("Sending");

                var pendingTurn = component.Find("#pending-turn");
                pendingTurn.QuerySelector(".user-body")!.TextContent.Should().Contain("inspect README.md");
                pendingTurn.QuerySelector(".pending-text")!.TextContent.Should().Contain("Checking the workspace");
                component.Markup.Should().NotContainEquivalentOf("Running command");
            });
        }
        finally
        {
            pending.TrySetResult(
                DesktopRequestProcessor.Default.Execute(session.ActiveConnection, "inspect README.md"));
        }

        await inflight;

        component.FindAll("#pending-turn").Should().BeEmpty();
        component.Find("#chat-form fieldset").HasAttribute("disabled").Should().BeFalse();
        component.FindAll("#turn-history > li.chat-exchange").Should().ContainSingle();
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

        session.MessageInput.Should().Be("   ");
        session.Entries.Should().BeEmpty();
        component.FindAll("#composer-error[role='alert']").Should().ContainSingle();

        await component.Find("#message-input").InputAsync("Where does WorkspaceRoot appear?");
        await component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        session.MessageInput.Should().Be("Where does WorkspaceRoot appear?");
        DesktopTurnEntry failure = session.Entries.Should().ContainSingle().Which;
        failure.Kind.Should().Be(DesktopTurnKind.UnexpectedError);
    }
}
