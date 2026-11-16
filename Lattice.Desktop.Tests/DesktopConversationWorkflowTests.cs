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
        cards.Count.Should().Be(7);

        string[] kinds = [.. cards.Select(card => card.GetAttribute("data-turn-kind")!)];
        kinds.Should().Equal([
                "InvalidWorkspace",
                "FileInspected",
                "SearchResults",
                "NoMatches",
                "MissingFile",
                "AmbiguousInput",
                "UnsupportedInput",
            ]);

        cards[1].QuerySelector(".file-path")!.TextContent.Should().Contain("src/TokenAlpha.cs");
        cards[1].QuerySelector(".file-contents")!.TextContent.Should().Contain("public sealed class TokenAlpha");

        var evidenceLocations = cards[2]
            .QuerySelectorAll(".evidence-location")
            .Select(node => node.TextContent)
            .ToArray();
        evidenceLocations.Should().Contain("README.md:2");
        evidenceLocations.Should().Contain("src/TokenAlpha.cs:2");
        cards[3].QuerySelector("[data-message-role='assistant']")!.TextContent.Should().Contain("MissingSymbol");
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
        cards.Count.Should().Be(3);
        cards[0].GetAttribute("data-turn-kind").Should().Be("FileInspected");
        cards[1].GetAttribute("data-turn-kind").Should().Be("UnexpectedError");
        cards[1].TextContent.Should().ContainEquivalentOf("unexpected error");
        cards[1].TextContent.Should().NotContain("InvalidOperationException");
        cards[2].GetAttribute("data-turn-kind").Should().Be("SearchResults");
        session.IsBusy.Should().BeFalse();
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
                component.Find("#chat-form fieldset").HasAttribute("disabled").Should().BeTrue();
                component.Find("#workspace-form fieldset").HasAttribute("disabled").Should().BeTrue();
                component.FindAll("#pending-turn").Should().ContainSingle();
                component.Find("#pending-turn").TextContent.Should().Contain("Checking the workspace");
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

        executedRequests.Should().Be(1);
        session.ConnectedWorkspacePath.Should().Be(Path.GetFullPath(firstWorkspace.Path));
        DesktopTurnEntry onlyEntry = session.Entries.Should().ContainSingle().Which;
        onlyEntry.RequestText.Should().Be("inspect README.md");
        onlyEntry.Kind.Should().Be(DesktopTurnKind.FileInspected);
        session.IsBusy.Should().BeFalse();
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
        component.Find("#active-workspace").GetAttribute("data-connected").Should().Be("true");

        await SubmitRequestAsync(component, "inspect Lattice/WorkspaceRoot.cs");
        await SubmitRequestAsync(component, "find symbol WorkspaceQuestionAnswerer");

        var cards = component.FindAll("#turn-history > li");
        cards.Count.Should().Be(2);
        cards[0].GetAttribute("data-turn-kind").Should().Be("FileInspected");
        cards[0].QuerySelector(".file-path")!.TextContent.Should().Be("Lattice/WorkspaceRoot.cs");
        cards[0].QuerySelector(".file-contents")!.TextContent.Should().Contain("public sealed class WorkspaceRoot");

        cards[1].GetAttribute("data-turn-kind").Should().Be("SearchResults");
        var locations = cards[1].QuerySelectorAll(".evidence-location").Select(node => node.TextContent).ToArray();
        locations.Where(location => location.StartsWith("Lattice/WorkspaceQuestionAnswerer.cs:", StringComparison.Ordinal)).Should().NotBeEmpty();
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
        cards.Count.Should().Be(12);

        for (int i = 0; i < searchPhrases.Length; i++)
        {
            cards[i].QuerySelector(".user-body")!.TextContent.Should().Be(searchPhrases[i]);
            cards[i].GetAttribute("data-turn-kind").Should().Be("SearchResults");
            cards[i].QuerySelector(".turn-summary")!.TextContent.Should().Contain("matching occurrences of 'WorkspaceRoot'");
            cards[i].QuerySelector(".evidence-list[tabindex='0']").Should().BeNotNull();
        }

        cards[5].GetAttribute("data-turn-kind").Should().Be("AmbiguousInput");
        cards[5].QuerySelectorAll(".file-viewer").Should().BeEmpty();
        cards[5].QuerySelector(".turn-summary")!.TextContent.Should().Contain("Which file would you like me to open?");

        for (int i = 0; i < inspectPhrases.Length; i++)
        {
            var card = cards[6 + i];
            card.QuerySelector(".user-body")!.TextContent.Should().Be(inspectPhrases[i]);
            card.GetAttribute("data-turn-kind").Should().Be("FileInspected");
            card.QuerySelector(".file-path")!.TextContent.Should().Be("Lattice/WorkspaceRoot.cs");
            card.QuerySelector(".file-contents[tabindex='0']").Should().BeNotNull();
            card.QuerySelector(".file-contents")!.TextContent.Should().Contain("public sealed class WorkspaceRoot");
        }

        cards[10].GetAttribute("data-turn-kind").Should().Be("SearchResults");
        cards[10].QuerySelector(".turn-summary")!.TextContent.Should().Contain("in 1 file");

        cards[11].GetAttribute("data-turn-kind").Should().Be("FileInspected");
        cards[11].QuerySelector(".file-path")!.TextContent.Should().Be("Lattice.Desktop/Program.cs");
        cards[11].QuerySelector(".file-contents")!.TextContent.Should().Contain(singleFileIdentifier);

        foreach (var assistantNode in component.FindAll("[data-message-role='assistant'] .turn-summary"))
        {
            string reply = assistantNode.TextContent;
            reply.Should().NotContain("find.where-");
            reply.Should().NotContain("inspect.show-me");
            reply.Should().NotContain("files.read");
            reply.Should().NotContain("files.search");
            reply.Should().NotContainEquivalentOf("inspect <");
            reply.Should().NotContainEquivalentOf("find symbol <");
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
