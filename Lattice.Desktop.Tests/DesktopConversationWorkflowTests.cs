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
        Assert.Contains("zero literal occurrences", cards[3].QuerySelector(".no-matches-note")!.TextContent, StringComparison.OrdinalIgnoreCase);
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
        Assert.Contains("Simulated storage failure", cards[1].TextContent, StringComparison.Ordinal);
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

        await component.Find("#request-input").InputAsync("inspect README.md");
        Task inflight = component.Find("#request-form").TriggerEventAsync("onsubmit", EventArgs.Empty);
        try
        {
            component.WaitForAssertion(() =>
            {
                Assert.True(component.Find("#request-form fieldset").HasAttribute("disabled"));
                Assert.True(component.Find("#workspace-form fieldset").HasAttribute("disabled"));
                Assert.Contains("Running", component.Find("#submit-request").TextContent, StringComparison.Ordinal);
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

    private static async Task SubmitRequestAsync(IRenderedComponent<App> component, string request)
    {
        await component.Find("#request-input").InputAsync(request);
        await component.Find("#request-form").TriggerEventAsync("onsubmit", EventArgs.Empty);
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
