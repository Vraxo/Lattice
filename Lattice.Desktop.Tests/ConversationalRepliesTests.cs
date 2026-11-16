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

        var exchange = component.FindAll("#turn-history > li.chat-exchange").Should().ContainSingle().Which;
        exchange.QuerySelector(".user-body")!.TextContent.Should().Be("Where is OrderService used in this project?");

        string summary = exchange.QuerySelector(".turn-summary")!.TextContent;
        summary.Should().Be("I found 4 matching occurrences of 'OrderService' across 3 files.");
        exchange.QuerySelector(".search-mode-note")!.TextContent.Should().Contain("Literal source-text matches");

        string[] locations = [.. exchange.QuerySelectorAll(".evidence-location").Select(node => node.TextContent)];
        locations.Should().Contain("README.md:1");
        locations.Should().Contain("src/Alpha.cs:1");
        locations.Should().Contain("src/Alpha.cs:3");
        locations.Should().Contain("src/Beta.cs:3");
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
        exchanges.Count.Should().Be(6);

        exchanges[0].QuerySelector(".turn-summary")!.TextContent.Should().Be("Here is 'src/Item.cs' (3 lines).");
        exchanges[0].QuerySelector(".file-path")!.TextContent.Should().Be("src/Item.cs");

        foreach (var exchange in exchanges)
        {
            string assistantText = exchange.QuerySelector("[data-message-role='assistant']")!.TextContent;
            assistantText.Should().NotContain("files.path.");
            assistantText.Should().NotContain("files.file.");
            assistantText.Should().NotContainEquivalentOf("inspect <");
            assistantText.Should().NotContainEquivalentOf("find symbol <");
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
        exchanges.Count.Should().Be(3);

        exchanges[0].QuerySelector(".user-body")!.TextContent.Should().Be("Can you rewrite README.md to add installation steps?");
        string unsupportedReply = exchanges[0].QuerySelector(".turn-summary")!.TextContent;
        unsupportedReply.Should().NotContain("Unsupported request");
        unsupportedReply.Should().NotContain("Supported forms");
        unsupportedReply.Should().Contain("Show me README.md");
        exchanges[0].QuerySelectorAll(".file-viewer, .evidence-box").Should().BeEmpty();

        exchanges[1].QuerySelector(".user-body")!.TextContent.Should().Be("show README.md");
        string ambiguousReply = exchanges[1].QuerySelector(".turn-summary")!.TextContent;
        ambiguousReply.Should().Contain("Did you want me to open the file 'README.md'?");
        ambiguousReply.Should().NotContain("show.inspect");
        ambiguousReply.Should().NotContain("show.explain");
        exchanges[1].QuerySelectorAll(".file-viewer, .evidence-box").Should().BeEmpty();

        string explanationReply = exchanges[2].QuerySelector(".turn-summary")!.TextContent;
        explanationReply.Should().NotContain("Inspected");
        explanationReply.Should().NotContain("I found");
        exchanges[2].QuerySelectorAll(".file-viewer, .evidence-box").Should().BeEmpty();
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
