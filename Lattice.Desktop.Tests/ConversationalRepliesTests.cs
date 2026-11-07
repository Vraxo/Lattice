using Bunit;
using Lattice.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Lattice.Desktop.Tests;

public sealed class ConversationalRepliesTests
{
    [Fact]
    public async Task SearchReplyUsesNaturalCountsKeepsEvidenceAndExplainsLiteralMatching()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("src/Alpha.cs", "public sealed class OrderService\n{\n    public OrderService() { }\n}\n");
        workspace.WriteFile("src/Beta.cs", "internal sealed class Consumer\n{\n    private readonly OrderService _svc;\n}\n");
        workspace.WriteFile("README.md", "OrderService handles orders.\n");

        await using BunitContext context = new();
        context.Services.AddSingleton<DesktopSession>();
        IRenderedComponent<App> component = context.Render<App>();

        await ConnectAsync(component, workspace.Path);
        await SendAsync(component, "Where is OrderService used in this project?");

        var exchange = Assert.Single(component.FindAll("#turn-history > li.chat-exchange"));
        Assert.Equal("Where is OrderService used in this project?", exchange.QuerySelector(".user-body")!.TextContent);

        string summary = exchange.QuerySelector(".turn-summary")!.TextContent;
        Assert.Equal("I found 4 matching occurrences of 'OrderService' across 3 files.", summary);
        Assert.Contains("Literal source-text matches", exchange.QuerySelector(".search-mode-note")!.TextContent, StringComparison.Ordinal);

        string[] locations = [.. exchange.QuerySelectorAll(".evidence-location").Select(node => node.TextContent)];
        Assert.Contains("README.md:1", locations);
        Assert.Contains("src/Alpha.cs:1", locations);
        Assert.Contains("src/Alpha.cs:3", locations);
        Assert.Contains("src/Beta.cs:3", locations);
    }

    [Fact]
    public async Task FileInspectAndErrorRepliesUsePlainLanguageWithoutRawErrorCodesOrCommandSyntax()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("src/Item.cs", "namespace Demo;\npublic record Item(string Name);\n");
        workspace.CreateDirectory("src/subfolder");
        workspace.WriteBytes("src/binary.dat", [0xFF, 0xFE, 0xC3, 0x28]);
        workspace.WriteBytes("src/huge.txt", new byte[ReadFileTool.MaxBytes + 10]);

        await using BunitContext context = new();
        context.Services.AddSingleton<DesktopSession>();
        IRenderedComponent<App> component = context.Render<App>();

        await ConnectAsync(component, workspace.Path);
        await SendAsync(component, "Show me src/Item.cs.");
        await SendAsync(component, "What's in src/Missing.cs?");
        await SendAsync(component, "Open the file src/subfolder.");
        await SendAsync(component, "Can you read src/binary.dat?");
        await SendAsync(component, "Show me src/huge.txt.");
        await SendAsync(component, "Show me ../escape.txt.");

        var exchanges = component.FindAll("#turn-history > li.chat-exchange");
        Assert.Equal(6, exchanges.Count);

        Assert.Equal("Here is 'src/Item.cs' (3 lines).", exchanges[0].QuerySelector(".turn-summary")!.TextContent);
        Assert.Equal("src/Item.cs", exchanges[0].QuerySelector(".file-path")!.TextContent);

        foreach (var exchange in exchanges)
        {
            string assistantText = exchange.QuerySelector("[data-message-role='assistant']")!.TextContent;
            Assert.DoesNotContain("files.path.", assistantText, StringComparison.Ordinal);
            Assert.DoesNotContain("files.file.", assistantText, StringComparison.Ordinal);
            Assert.DoesNotContain("inspect <", assistantText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("find symbol <", assistantText, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task UnknownAndAmbiguousInputsGiveHelpfulNaturalRepliesWithoutClaimingToolExecution()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("README.md", "# Project\n");

        await using BunitContext context = new();
        context.Services.AddSingleton<DesktopSession>();
        IRenderedComponent<App> component = context.Render<App>();

        await ConnectAsync(component, workspace.Path);
        await SendAsync(component, "Can you rewrite README.md to add installation steps?");
        await SendAsync(component, "show README.md");
        await SendAsync(component, "Why does WorkspaceRoot exist?");

        var exchanges = component.FindAll("#turn-history > li.chat-exchange");
        Assert.Equal(3, exchanges.Count);

        Assert.Equal("Can you rewrite README.md to add installation steps?", exchanges[0].QuerySelector(".user-body")!.TextContent);
        string unsupportedReply = exchanges[0].QuerySelector(".turn-summary")!.TextContent;
        Assert.DoesNotContain("Unsupported request", unsupportedReply, StringComparison.Ordinal);
        Assert.DoesNotContain("Supported forms", unsupportedReply, StringComparison.Ordinal);
        Assert.Contains("Show me README.md", unsupportedReply, StringComparison.Ordinal);
        Assert.Empty(exchanges[0].QuerySelectorAll(".file-viewer, .evidence-box"));

        Assert.Equal("show README.md", exchanges[1].QuerySelector(".user-body")!.TextContent);
        string ambiguousReply = exchanges[1].QuerySelector(".turn-summary")!.TextContent;
        Assert.Contains("Did you want me to open the file 'README.md'?", ambiguousReply, StringComparison.Ordinal);
        Assert.DoesNotContain("show.inspect", ambiguousReply, StringComparison.Ordinal);
        Assert.DoesNotContain("show.explain", ambiguousReply, StringComparison.Ordinal);
        Assert.Empty(exchanges[1].QuerySelectorAll(".file-viewer, .evidence-box"));

        string explanationReply = exchanges[2].QuerySelector(".turn-summary")!.TextContent;
        Assert.DoesNotContain("Inspected", explanationReply, StringComparison.Ordinal);
        Assert.DoesNotContain("I found", explanationReply, StringComparison.Ordinal);
        Assert.Empty(exchanges[2].QuerySelectorAll(".file-viewer, .evidence-box"));
    }

    private static async Task ConnectAsync(IRenderedComponent<App> component, string path)
    {
        await component.Find("#workspace-path").InputAsync(path);
        await component.Find("#workspace-form").TriggerEventAsync("onsubmit", EventArgs.Empty);
    }

    private static async Task SendAsync(IRenderedComponent<App> component, string message)
    {
        await component.Find("#message-input").InputAsync(message);
        await component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);
    }
}
