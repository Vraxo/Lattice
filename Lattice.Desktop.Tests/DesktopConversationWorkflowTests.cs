using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Lattice.Desktop.Tests;

public sealed class DesktopConversationWorkflowTests
{
    [Fact]
    public async Task MultiTurnConversationRetainsAllPriorRequestsEvidenceAndDistinctStates()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("README.md", "# Demo\nRefers to TokenAlpha.\n");
        workspace.WriteFile("src/TokenAlpha.cs", "namespace Demo;\npublic sealed class TokenAlpha { }\n");

        await using BunitContext context = new();
        context.Services.AddSingleton<DesktopSession>();
        IRenderedComponent<App> component = context.Render<App>();

        // 1. Submit before connecting -> InvalidWorkspace
        await SubmitRequestAsync(component, "inspect README.md");

        // 2. Connect valid workspace
        await component.Find("#workspace-path").InputAsync(workspace.Path);
        await component.Find("#workspace-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        // 3. Inspect existing file -> FileInspected
        await SubmitRequestAsync(component, "inspect src/TokenAlpha.cs");

        // 4. Search matching symbol -> SearchResults
        await SubmitRequestAsync(component, "find symbol TokenAlpha");

        // 5. Search absent symbol -> NoMatches
        await SubmitRequestAsync(component, "find symbol MissingSymbol");

        // 6. Inspect missing file -> MissingFile
        await SubmitRequestAsync(component, "inspect src/Missing.cs");

        // 7. Ambiguous request -> AmbiguousInput
        await SubmitRequestAsync(component, "show README.md");

        // 8. Unsupported request -> UnsupportedInput
        await SubmitRequestAsync(component, "edit src/TokenAlpha.cs");

        var cards = component.FindAll("#turn-history > li");
        Assert.Equal(7, cards.Count);

        string[] kinds = [.. cards.Select(card => card.GetAttribute("data-turn-kind")!)];
        Assert.Equal(
            [
                "InvalidWorkspace",
                "FileInspected",
                "SearchResults",
                "NoMatches",
                "MissingFile",
                "AmbiguousInput",
                "UnsupportedInput",
            ],
            kinds);

        Assert.Contains("src/TokenAlpha.cs", cards[1].QuerySelector(".file-path")!.TextContent, StringComparison.Ordinal);
        Assert.Contains("public sealed class TokenAlpha", cards[1].QuerySelector(".file-contents")!.TextContent, StringComparison.Ordinal);

        var evidenceLocations = cards[2]
            .QuerySelectorAll(".evidence-location")
            .Select(node => node.TextContent)
            .ToArray();
        Assert.Contains("README.md:2", evidenceLocations);
        Assert.Contains("src/TokenAlpha.cs:2", evidenceLocations);
        Assert.Contains("MissingSymbol", cards[3].QuerySelector("[data-message-role='assistant']")!.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnexpectedExceptionProducesDistinctUnexpectedErrorTurnAndPreservesSession()
    {
        using DesktopTempWorkspace workspace = new();
        int call = 0;
        DesktopSession session = new(
            executeRequest: (connected, request, _) =>
            {
                call++;
                if (call == 2)
                {
                    throw new InvalidOperationException("Simulated storage failure.");
                }

                return Task.FromResult(DesktopRequestProcessor.Default.Execute(connected, request));
            });
        workspace.WriteFile("README.md", "# Hello");

        await using BunitContext context = new();
        context.Services.AddSingleton(session);
        IRenderedComponent<App> component = context.Render<App>();

        await component.Find("#workspace-path").InputAsync(workspace.Path);
        await component.Find("#workspace-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        await SubmitRequestAsync(component, "inspect README.md");
        await SubmitRequestAsync(component, "inspect README.md");
        await SubmitRequestAsync(component, "find symbol Hello");

        var cards = component.FindAll("#turn-history > li");
        Assert.Equal(3, cards.Count);
        Assert.Equal("FileInspected", cards[0].GetAttribute("data-turn-kind"));
        Assert.Equal("UnexpectedError", cards[1].GetAttribute("data-turn-kind"));
        Assert.Contains("unexpected error", cards[1].TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InvalidOperationException", cards[1].TextContent, StringComparison.Ordinal);
        Assert.Equal("SearchResults", cards[2].GetAttribute("data-turn-kind"));
        Assert.False(session.IsBusy);
    }

    [Fact]
    public async Task OverlappingRequestAndConnectSubmissionsDoNotCorruptSessionState()
    {
        using DesktopTempWorkspace firstWorkspace = new();
        using DesktopTempWorkspace secondWorkspace = new();
        firstWorkspace.WriteFile("README.md", "# First");

        TaskCompletionSource<DesktopTurnEntry> pendingTurn =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        int executedRequests = 0;
        DesktopSession session = new(
            executeRequest: (_, _, _) =>
            {
                executedRequests++;
                return pendingTurn.Task;
            });

        await using BunitContext context = new();
        context.Services.AddSingleton(session);
        IRenderedComponent<App> component = context.Render<App>();

        await component.Find("#workspace-path").InputAsync(firstWorkspace.Path);
        await component.Find("#workspace-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        await component.Find("#message-input").InputAsync("inspect README.md");
        Task inflight = component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);
        try
        {
            component.WaitForAssertion(() =>
            {
                Assert.True(component.Find("#chat-form fieldset").HasAttribute("disabled"));
                Assert.True(component.Find("#workspace-form fieldset").HasAttribute("disabled"));
                Assert.Single(component.FindAll("#pending-turn"));
                Assert.Contains("Checking the workspace", component.Find("#pending-turn").TextContent, StringComparison.Ordinal);
            });

            // Attempt overlapping request and workspace switch while busy.
            session.RequestInput = "find symbol Injected";
            await session.SubmitRequestAsync();
            session.WorkspacePathInput = secondWorkspace.Path;
            await session.ConnectAsync();
        }
        finally
        {
            pendingTurn.TrySetResult(
                DesktopRequestProcessor.Default.Execute(session.ActiveConnection, "inspect README.md"));
        }

        await inflight;

        Assert.Equal(1, executedRequests);
        Assert.Equal(Path.GetFullPath(firstWorkspace.Path), session.ConnectedWorkspacePath);
        DesktopTurnEntry onlyEntry = Assert.Single(session.Entries);
        Assert.Equal("inspect README.md", onlyEntry.RequestText);
        Assert.Equal(DesktopTurnKind.FileInspected, onlyEntry.Kind);
        Assert.False(session.IsBusy);
    }

    [Fact]
    public async Task ConnectsToActualLatticeRepositoryCheckoutAndDemonstratesInspectAndFind()
    {
        string repoRoot = LocateLatticeRepositoryRoot();
        await using BunitContext context = new();
        context.Services.AddSingleton<DesktopSession>();
        IRenderedComponent<App> component = context.Render<App>();

        await component.Find("#workspace-path").InputAsync(repoRoot);
        await component.Find("#workspace-form").TriggerEventAsync("onsubmit", EventArgs.Empty);
        Assert.Equal("true", component.Find("#active-workspace").GetAttribute("data-connected"));

        await SubmitRequestAsync(component, "inspect Lattice/WorkspaceRoot.cs");
        await SubmitRequestAsync(component, "find symbol WorkspaceQuestionAnswerer");

        var cards = component.FindAll("#turn-history > li");
        Assert.Equal(2, cards.Count);
        Assert.Equal("FileInspected", cards[0].GetAttribute("data-turn-kind"));
        Assert.Equal("Lattice/WorkspaceRoot.cs", cards[0].QuerySelector(".file-path")!.TextContent);
        Assert.Contains("public sealed class WorkspaceRoot", cards[0].QuerySelector(".file-contents")!.TextContent, StringComparison.Ordinal);

        Assert.Equal("SearchResults", cards[1].GetAttribute("data-turn-kind"));
        var locations = cards[1].QuerySelectorAll(".evidence-location").Select(node => node.TextContent).ToArray();
        Assert.Contains(
            locations,
            location => location.StartsWith("Lattice/WorkspaceQuestionAnswerer.cs:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RenderedChatWalksThroughAllDefinitionOfDonePhrasesAndFollowUpsAgainstRealRepository()
    {
        string repoRoot = LocateLatticeRepositoryRoot();
        await using BunitContext context = new();
        context.Services.AddSingleton<DesktopSession>();
        IRenderedComponent<App> component = context.Render<App>();

        await component.Find("#workspace-path").InputAsync(repoRoot);
        await component.Find("#workspace-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        string[] searchPhrases =
        [
            "Where does WorkspaceRoot appear?",
            "Where is WorkspaceRoot used in this project?",
            "Can you find where WorkspaceRoot is used throughout this project?",
            "Search this project for WorkspaceRoot.",
            "Find all occurrences of WorkspaceRoot.",
        ];
        foreach (string phrase in searchPhrases)
        {
            await SubmitRequestAsync(component, phrase);
        }

        // Follow-up after multi-file search must ask which file rather than guessing.
        await SubmitRequestAsync(component, "Show me that file.");

        string[] inspectPhrases =
        [
            "Show me Lattice/WorkspaceRoot.cs.",
            "Open the file Lattice/WorkspaceRoot.cs.",
            "Can you read Lattice/WorkspaceRoot.cs?",
            "What's in Lattice/WorkspaceRoot.cs?",
        ];
        foreach (string phrase in inspectPhrases)
        {
            await SubmitRequestAsync(component, phrase);
        }

        // Single-file search followed by "Show me that file." must open Lattice.Desktop/Program.cs.
        string singleFileIdentifier = "ConfigureMain" + "Window";
        await SubmitRequestAsync(component, $"Where does {singleFileIdentifier} appear?");
        await SubmitRequestAsync(component, "Show me that file.");

        var cards = component.FindAll("#turn-history > li.chat-exchange");
        Assert.Equal(12, cards.Count);

        for (int i = 0; i < searchPhrases.Length; i++)
        {
            Assert.Equal(searchPhrases[i], cards[i].QuerySelector(".user-body")!.TextContent);
            Assert.Equal("SearchResults", cards[i].GetAttribute("data-turn-kind"));
            Assert.Contains("matching occurrences of 'WorkspaceRoot'", cards[i].QuerySelector(".turn-summary")!.TextContent, StringComparison.Ordinal);
            Assert.NotNull(cards[i].QuerySelector(".evidence-list[tabindex='0']"));
        }

        Assert.Equal("AmbiguousInput", cards[5].GetAttribute("data-turn-kind"));
        Assert.Empty(cards[5].QuerySelectorAll(".file-viewer"));
        Assert.Contains("Which file would you like me to open?", cards[5].QuerySelector(".turn-summary")!.TextContent, StringComparison.Ordinal);

        for (int i = 0; i < inspectPhrases.Length; i++)
        {
            var card = cards[6 + i];
            Assert.Equal(inspectPhrases[i], card.QuerySelector(".user-body")!.TextContent);
            Assert.Equal("FileInspected", card.GetAttribute("data-turn-kind"));
            Assert.Equal("Lattice/WorkspaceRoot.cs", card.QuerySelector(".file-path")!.TextContent);
            Assert.NotNull(card.QuerySelector(".file-contents[tabindex='0']"));
            Assert.Contains("public sealed class WorkspaceRoot", card.QuerySelector(".file-contents")!.TextContent, StringComparison.Ordinal);
        }

        Assert.Equal("SearchResults", cards[10].GetAttribute("data-turn-kind"));
        Assert.Contains("in 1 file", cards[10].QuerySelector(".turn-summary")!.TextContent, StringComparison.Ordinal);

        Assert.Equal("FileInspected", cards[11].GetAttribute("data-turn-kind"));
        Assert.Equal("Lattice.Desktop/Program.cs", cards[11].QuerySelector(".file-path")!.TextContent);
        Assert.Contains(singleFileIdentifier, cards[11].QuerySelector(".file-contents")!.TextContent, StringComparison.Ordinal);

        foreach (var assistantNode in component.FindAll("[data-message-role='assistant'] .turn-summary"))
        {
            string reply = assistantNode.TextContent;
            Assert.DoesNotContain("find.where-", reply, StringComparison.Ordinal);
            Assert.DoesNotContain("inspect.show-me", reply, StringComparison.Ordinal);
            Assert.DoesNotContain("files.read", reply, StringComparison.Ordinal);
            Assert.DoesNotContain("files.search", reply, StringComparison.Ordinal);
            Assert.DoesNotContain("inspect <", reply, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("find symbol <", reply, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task SubmitRequestAsync(IRenderedComponent<App> component, string request)
    {
        await component.Find("#message-input").InputAsync(request);
        await component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);
    }

    private static string LocateLatticeRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Lattice.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Lattice.slnx above AppContext.BaseDirectory.");
    }
}
