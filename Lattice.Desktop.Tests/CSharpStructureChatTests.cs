using Bunit;
using Lattice.CSharp;
using Microsoft.Extensions.DependencyInjection;

namespace Lattice.Desktop.Tests;

public sealed class CSharpStructureChatTests
{
    [Fact]
    public async Task ListClassesUsesRoslynDeclarationsAndIgnoresNonCSharpAndGeneratedFiles()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile(
            "src/Alpha.cs",
            "namespace Demo;\npublic sealed class Alpha\n{\n    public void Run() { }\n}\n");
        workspace.WriteFile("src/Beta.cs", "namespace Demo;\npublic record Beta(int Id);\n");
        workspace.WriteFile("src/IThing.cs", "namespace Demo;\npublic interface IThing { }\n");
        workspace.WriteFile("docs/Guide.md", "public class FakeClass { }\n");
        workspace.WriteFile("bin/Generated.cs", "public class Generated { }\n");

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "What classes are in this project?";
        await session.SendMessageAsync();

        DesktopTurnEntry entry = Assert.Single(session.Entries);
        Assert.Equal(DesktopTurnKind.CSharpStructureResults, entry.Kind);
        Assert.Equal(2, entry.SafeDeclarations.Length);
        Assert.Contains(entry.SafeDeclarations, declaration =>
            declaration.Name == "Alpha"
            && declaration.Kind == "class"
            && declaration.RelativePath == "src/Alpha.cs"
            && declaration.Signature.Contains("public sealed class Alpha", StringComparison.Ordinal));
        Assert.Contains(entry.SafeDeclarations, declaration =>
            declaration.Name == "Beta"
            && declaration.Kind == "record"
            && declaration.RelativePath == "src/Beta.cs");
        Assert.DoesNotContain(entry.SafeDeclarations, declaration => declaration.Name is "IThing" or "FakeClass" or "Generated");
        Assert.Contains("2 C# classes and records", entry.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FindDefinitionReturnsExactDeclarationLocationAndAllowsOpeningThatFile()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile(
            "src/WorkspaceRoot.cs",
            "namespace Demo;\n\npublic sealed class WorkspaceRoot\n{\n    public string FullPath { get; }\n}\n");
        workspace.WriteFile("docs/README.md", "WorkspaceRoot is mentioned here, but is not declared.");

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "Where is WorkspaceRoot defined?";
        await session.SendMessageAsync();

        DesktopTurnEntry definition = Assert.Single(session.Entries);
        CSharpDeclaration declaration = Assert.Single(definition.SafeDeclarations);
        Assert.Equal(DesktopTurnKind.CSharpStructureResults, definition.Kind);
        Assert.Equal("WorkspaceRoot", declaration.Name);
        Assert.Equal("class", declaration.Kind);
        Assert.Equal("src/WorkspaceRoot.cs", declaration.RelativePath);
        Assert.Equal(3, declaration.Line);

        session.MessageInput = "Show me that file.";
        await session.SendMessageAsync();

        Assert.Equal(2, session.Entries.Count);
        DesktopTurnEntry opened = session.Entries[1];
        Assert.Equal(DesktopTurnKind.FileInspected, opened.Kind);
        Assert.Equal("src/WorkspaceRoot.cs", opened.InspectedRelativePath);
        Assert.Contains("public sealed class WorkspaceRoot", opened.FileContents, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListMembersReturnsMethodsPropertiesFieldsAndConstructorForRequestedType()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile(
            "src/WorkspaceRoot.cs",
            "namespace Demo;\npublic class WorkspaceRoot\n{\n    private readonly string _path;\n    public WorkspaceRoot(string path) { _path = path; }\n    public string FullPath => _path;\n    public void Reset() { }\n}\n");
        workspace.WriteFile(
            "src/Other.cs",
            "namespace Demo;\npublic class Other\n{\n    public void DoNotInclude() { }\n}\n");

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "Show me the members of WorkspaceRoot.";
        await session.SendMessageAsync();

        DesktopTurnEntry entry = Assert.Single(session.Entries);
        Assert.Equal(DesktopTurnKind.CSharpStructureResults, entry.Kind);
        Assert.Equal(4, entry.SafeDeclarations.Length);
        Assert.Contains(entry.SafeDeclarations, declaration => declaration.Name == "_path" && declaration.Kind == "field");
        Assert.Contains(entry.SafeDeclarations, declaration => declaration.Name == "WorkspaceRoot" && declaration.Kind == "constructor");
        Assert.Contains(entry.SafeDeclarations, declaration => declaration.Name == "FullPath" && declaration.Kind == "property");
        Assert.Contains(entry.SafeDeclarations, declaration => declaration.Name == "Reset" && declaration.Kind == "method");
        Assert.DoesNotContain(entry.SafeDeclarations, declaration => declaration.Name == "DoNotInclude");
    }

    [Fact]
    public async Task StructureScanSkipsExcludedFoldersAndUnreadableSourceFiles()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("src/Visible.cs", "public class Visible { }");
        workspace.WriteBytes("src/Binary.cs", [0xFF, 0xC3, 0x28]);
        workspace.WriteFile("bin/Generated.cs", "public class Generated { }");
        workspace.WriteFile("obj/Generated.cs", "public class GeneratedToo { }");

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "What classes are in this project?";
        await session.SendMessageAsync();

        DesktopTurnEntry entry = Assert.Single(session.Entries);
        CSharpDeclaration declaration = Assert.Single(entry.SafeDeclarations);
        Assert.Equal("Visible", declaration.Name);
        Assert.Equal(1, entry.StructureSourceFileCount);
        Assert.Equal(1, entry.StructureSkippedFileCount);
        Assert.DoesNotContain("Generated", entry.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingDefinitionReturnsAnEmptyStructureResultWithoutGuessingFromTextMatches()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("docs/README.md", "WorkspaceRoot is mentioned in this document.");
        workspace.WriteFile("src/Other.cs", "public sealed class Other { }");

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "Where is WorkspaceRoot declared?";
        await session.SendMessageAsync();

        DesktopTurnEntry entry = Assert.Single(session.Entries);
        Assert.Equal(DesktopTurnKind.CSharpStructureResults, entry.Kind);
        Assert.Empty(entry.SafeDeclarations);
        Assert.Contains("couldn't find a C# declaration named 'WorkspaceRoot'", entry.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RenderedChatDisplaysCSharpDeclarationEvidence()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("src/Widget.cs", "namespace Demo;\npublic sealed class Widget { public void Run() { } }\n");

        await using BunitContext context = new();
        context.Services.AddSingleton<DesktopSession>();
        IRenderedComponent<App> component = context.Render<App>();

        await component.Find("#workspace-path").InputAsync(workspace.Path);
        await component.Find("#workspace-form").TriggerEventAsync("onsubmit", EventArgs.Empty);
        await component.Find("#message-input").InputAsync("What classes are in this project?");
        await component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        var exchange = Assert.Single(component.FindAll("#turn-history > li.chat-exchange"));
        Assert.Equal("CSharpStructureResults", exchange.GetAttribute("data-turn-kind"));
        Assert.Contains("Roslyn syntax declarations", exchange.QuerySelector(".structure-header")!.TextContent, StringComparison.Ordinal);
        Assert.Contains("src/Widget.cs:2", exchange.QuerySelector(".structure-location")!.TextContent, StringComparison.Ordinal);
        Assert.Contains("public sealed class Widget", exchange.QuerySelector(".structure-signature")!.TextContent, StringComparison.Ordinal);
        Assert.Contains("does not yet compile the project", exchange.QuerySelector(".structure-footnote")!.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StructureRequestsRemainDeterministicAndUnrelatedExplanationsStayUnsupported()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("src/Widget.cs", "public sealed class Widget { }");

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "What does Widget do?";
        await session.SendMessageAsync();

        DesktopTurnEntry entry = Assert.Single(session.Entries);
        Assert.Equal(DesktopTurnKind.UnsupportedInput, entry.Kind);
        Assert.Empty(entry.SafeDeclarations);
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
