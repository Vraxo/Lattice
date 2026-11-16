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

        DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
        entry.Kind.Should().Be(DesktopTurnKind.CSharpStructureResults);
        entry.SafeDeclarations.Length.Should().Be(2);
        entry.SafeDeclarations.Where(declaration =>
            declaration.Name == "Alpha"
            && declaration.Kind == "class"
            && declaration.RelativePath == "src/Alpha.cs"
            && declaration.Signature.Contains("public sealed class Alpha", StringComparison.Ordinal)).Should().NotBeEmpty();
        entry.SafeDeclarations.Where(declaration =>
            declaration.Name == "Beta"
            && declaration.Kind == "record"
            && declaration.RelativePath == "src/Beta.cs").Should().NotBeEmpty();
        entry.SafeDeclarations.Where(declaration => declaration.Name is "IThing" or "FakeClass" or "Generated").Should().BeEmpty();
        entry.Summary.Should().Contain("2 C# classes and records");
    }

    [Fact]
    public async Task ListTypesIncludesStructsInterfacesEnumsDelegatesAndRecordStructs()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile(
            "src/Types.cs",
            "namespace Demo;\npublic struct Vector { }\npublic record struct Position(int X, int Y);\npublic interface IThing { }\npublic enum Mode { One, Two }\npublic delegate void Callback();\n");

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "What types are in this project?";
        await session.SendMessageAsync();

        DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
        entry.Kind.Should().Be(DesktopTurnKind.CSharpStructureResults);
        entry.SafeDeclarations.Where(declaration => declaration.Name == "Vector" && declaration.Kind == "struct").Should().NotBeEmpty();
        entry.SafeDeclarations.Where(declaration => declaration.Name == "Position"
            && declaration.Kind == "record struct"
            && declaration.Signature.Contains("(int X, int Y)", StringComparison.Ordinal)).Should().NotBeEmpty();
        entry.SafeDeclarations.Where(declaration => declaration.Name == "IThing" && declaration.Kind == "interface").Should().NotBeEmpty();
        entry.SafeDeclarations.Where(declaration => declaration.Name == "Mode" && declaration.Kind == "enum").Should().NotBeEmpty();
        entry.SafeDeclarations.Where(declaration => declaration.Name == "Callback" && declaration.Kind == "delegate").Should().NotBeEmpty();
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

        DesktopTurnEntry definition = session.Entries.Should().ContainSingle().Which;
        CSharpDeclaration declaration = definition.SafeDeclarations.Should().ContainSingle().Which;
        definition.Kind.Should().Be(DesktopTurnKind.CSharpStructureResults);
        declaration.Name.Should().Be("WorkspaceRoot");
        declaration.Kind.Should().Be("class");
        declaration.RelativePath.Should().Be("src/WorkspaceRoot.cs");
        declaration.Line.Should().Be(3);

        session.MessageInput = "Show me that file.";
        await session.SendMessageAsync();

        session.Entries.Count.Should().Be(2);
        DesktopTurnEntry opened = session.Entries[1];
        opened.Kind.Should().Be(DesktopTurnKind.FileInspected);
        opened.InspectedRelativePath.Should().Be("src/WorkspaceRoot.cs");
        opened.FileContents.Should().Contain("public sealed class WorkspaceRoot");
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

        DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
        entry.Kind.Should().Be(DesktopTurnKind.CSharpStructureResults);
        entry.SafeDeclarations.Length.Should().Be(4);
        entry.SafeDeclarations.Where(declaration => declaration.Name == "_path" && declaration.Kind == "field").Should().NotBeEmpty();
        entry.SafeDeclarations.Where(declaration => declaration.Name == "WorkspaceRoot" && declaration.Kind == "constructor").Should().NotBeEmpty();
        entry.SafeDeclarations.Where(declaration => declaration.Name == "FullPath" && declaration.Kind == "property").Should().NotBeEmpty();
        entry.SafeDeclarations.Where(declaration => declaration.Name == "Reset" && declaration.Kind == "method").Should().NotBeEmpty();
        entry.SafeDeclarations.Where(declaration => declaration.Name == "DoNotInclude").Should().BeEmpty();
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

        DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
        CSharpDeclaration declaration = entry.SafeDeclarations.Should().ContainSingle().Which;
        declaration.Name.Should().Be("Visible");
        entry.StructureSourceFileCount.Should().Be(1);
        entry.StructureSkippedFileCount.Should().Be(1);
        entry.Summary.Should().NotContain("Generated");
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

        DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
        entry.Kind.Should().Be(DesktopTurnKind.CSharpStructureResults);
        entry.SafeDeclarations.Should().BeEmpty();
        entry.Summary.Should().Contain("couldn't find a C# declaration named 'WorkspaceRoot'");
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

        var exchange = component.FindAll("#turn-history > li.chat-exchange").Should().ContainSingle().Which;
        exchange.GetAttribute("data-turn-kind").Should().Be("CSharpStructureResults");
        exchange.QuerySelector(".structure-header")!.TextContent.Should().Contain("Roslyn syntax declarations");
        exchange.QuerySelector(".structure-location")!.TextContent.Should().Contain("src/Widget.cs:2");
        exchange.QuerySelector(".structure-signature")!.TextContent.Should().Contain("public sealed class Widget");
        exchange.QuerySelector(".structure-footnote")!.TextContent.Should().Contain("does not yet compile the project");
    }

    [Fact]
    public async Task StructureRequestsRemainDeterministicAndUnrelatedExplanationsStayUnsupported()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("src/Widget.cs", "public sealed class Widget { }");

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "What does Widget do?";
        await session.SendMessageAsync();

        DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
        entry.Kind.Should().Be(DesktopTurnKind.UnsupportedInput);
        entry.SafeDeclarations.Should().BeEmpty();
    }

    [Fact]
    public async Task StructureScanStopsAfterMaximumSourceFilesAndMarksResultTruncated()
    {
        using DesktopTempWorkspace workspace = new();
        for (int index = 1; index <= CSharpWorkspaceStructureInspector.MaxSourceFilesRead + 1; index++)
        {
            workspace.WriteFile($"src/Type{index:D3}.cs", $"public class Type{index:D3} {{ }}");
        }

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "What classes are in this project?";
        await session.SendMessageAsync();

        DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
        entry.StructureSourceFileCount.Should().Be(CSharpWorkspaceStructureInspector.MaxSourceFilesRead);
        entry.StructureIsTruncated.Should().BeTrue();
        entry.SafeDeclarations.Length.Should().Be(CSharpWorkspaceStructureInspector.MaxSourceFilesRead);
    }

    [Fact]
    public async Task StructureScanStopsAtMaximumDeclarationCountAndMarksResultTruncated()
    {
        using DesktopTempWorkspace workspace = new();
        string source = string.Join(
            Environment.NewLine,
            Enumerable.Range(1, CSharpWorkspaceStructureInspector.MaxDeclarations + 1)
                .Select(index => $"public class GeneratedType{index:D4} {{ }}"));
        workspace.WriteFile("src/ManyTypes.cs", source);

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "What classes are in this project?";
        await session.SendMessageAsync();

        DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
        entry.SafeDeclarations.Length.Should().Be(CSharpWorkspaceStructureInspector.MaxDeclarations);
        entry.StructureIsTruncated.Should().BeTrue();
        entry.Summary.Should().Contain("safety limit");
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
