using System.Diagnostics;
using Bunit;
using Lattice.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Lattice.Desktop.Tests;

public sealed class WorkspaceFileDiscoveryTests
{
    [Fact]
    public async Task RootLevelBasenameOpensRootFileEvenWhenNestedDuplicateExists()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("README.md", "# Root README\n");
        workspace.WriteFile("docs/README.md", "# Docs README\n");

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "Show me readme.md.";
        await session.SendMessageAsync();

        DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
        entry.Kind.Should().Be(DesktopTurnKind.FileInspected);
        entry.FileContents.Should().Be("# Root README\n");
        string.Equals("README.md", entry.InspectedRelativePath, StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        entry.InspectedRelativePath!.Should().NotContain("/");
    }

    [Theory]
    [InlineData("Show me WorkspaceRoot.cs.", "Lattice/WorkspaceRoot.cs")]
    [InlineData("open the file workspaceroot.cs", "Lattice/WorkspaceRoot.cs")]
    [InlineData("What's in Directory.Build.props?", "eng/Directory.Build.props")]
    [InlineData("Can you read Architecture Notes.md?", "docs/notes/Architecture Notes.md")]
    public async Task UniqueNestedBasenameOpensDirectlyWithFullRelativePath(
        string message,
        string expectedRelativePath)
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("Lattice/WorkspaceRoot.cs", "public sealed class WorkspaceRoot { }\n");
        workspace.WriteFile("eng/Directory.Build.props", "<Project />\n");
        workspace.WriteFile("docs/notes/Architecture Notes.md", "# Architecture\n");

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = message;
        await session.SendMessageAsync();

        DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
        entry.Kind.Should().Be(DesktopTurnKind.FileInspected);
        entry.InspectedRelativePath.Should().Be(expectedRelativePath);
        entry.Summary.Should().Contain($"Here is '{expectedRelativePath}'");
        entry.FileContents.Should().NotBeNull();
    }

    [Fact]
    public async Task ExtensionlessStemFindsProgramCsAndNumericReplySelectsDisplayedCandidate()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("CherryCode.Terminal/Program.cs", "// terminal entry\n");
        workspace.WriteFile("CherryCode/Program.cs", "// app entry\n");

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "Show me program";
        await session.SendMessageAsync();

        DesktopTurnEntry ambiguous = session.Entries.Should().ContainSingle().Which;
        ambiguous.Kind.Should().Be(DesktopTurnKind.AmbiguousInput);
        ambiguous.TotalCandidateCount.Should().Be(2);
        ambiguous.Summary.Should().Contain("CherryCode.Terminal/Program.cs");
        ambiguous.Summary.Should().Contain("CherryCode/Program.cs");
        ambiguous.FileContents.Should().BeNull();

        session.MessageInput = "2";
        await session.SendMessageAsync();

        session.Entries.Count.Should().Be(2);
        DesktopTurnEntry opened = session.Entries[1];
        opened.Kind.Should().Be(DesktopTurnKind.FileInspected);
        opened.InspectedRelativePath.Should().Be("CherryCode/Program.cs");
        opened.FileContents.Should().Be("// app entry\n");
    }

    [Fact]
    public async Task ExtensionlessStemOpensUniqueProgramCs()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("src/Program.cs", "class Program { }\n");

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "Show me program";
        await session.SendMessageAsync();

        DesktopTurnEntry opened = session.Entries.Should().ContainSingle().Which;
        opened.Kind.Should().Be(DesktopTurnKind.FileInspected);
        opened.InspectedRelativePath.Should().Be("src/Program.cs");
        opened.FileContents.Should().Be("class Program { }\n");
    }

    [Fact]
    public async Task DuplicateNestedBasenamesListRelativePathsWithoutReadingUntilUserSelectsCandidate()
    {
        using DesktopTempWorkspace workspace = new();
        // Make the first alphabetical candidate invalid UTF-8 to prove no file is read while ambiguous.
        workspace.WriteBytes("Lattice.Cli/Program.cs", [0xFF, 0xFE, 0xC3, 0x28]);
        const string desktopProgram = "using Photino.Blazor;\nConsole.WriteLine(\"Desktop\");\n";
        workspace.WriteFile("Lattice.Desktop/Program.cs", desktopProgram);

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "Show me Program.cs.";
        await session.SendMessageAsync();

        DesktopTurnEntry ambiguous = session.Entries.Should().ContainSingle().Which;
        ambiguous.Kind.Should().Be(DesktopTurnKind.AmbiguousInput);
        ambiguous.FileContents.Should().BeNull();
        ambiguous.InspectedRelativePath.Should().BeNull();
        ambiguous.TotalCandidateCount.Should().Be(2);
        ambiguous.SafeCandidatePaths.Should().Equal(["Lattice.Cli/Program.cs", "Lattice.Desktop/Program.cs"]);
        ambiguous.Summary.Should().Contain("Lattice.Cli/Program.cs");
        ambiguous.Summary.Should().Contain("Lattice.Desktop/Program.cs");
        ambiguous.Summary.Should().Contain("Which file would you like me to open?");

        // User replies directly with a displayed relative path (without needing "Show me ").
        session.MessageInput = "Lattice.Desktop/Program.cs";
        await session.SendMessageAsync();

        session.Entries.Count.Should().Be(2);
        DesktopTurnEntry opened = session.Entries[1];
        opened.Kind.Should().Be(DesktopTurnKind.FileInspected);
        opened.InspectedRelativePath.Should().Be("Lattice.Desktop/Program.cs");
        opened.FileContents.Should().Be(desktopProgram);
    }

    [Fact]
    public async Task ExplicitRelativePathTakesPrecedenceAndIsNeverReplacedBySameBasenameElsewhere()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("Lattice.Desktop/Program.cs", "// desktop");

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "Show me Lattice.Cli/Program.cs.";
        await session.SendMessageAsync();

        DesktopTurnEntry missingExplicit = session.Entries.Should().ContainSingle().Which;
        missingExplicit.Kind.Should().Be(DesktopTurnKind.MissingFile);
        missingExplicit.InspectedRelativePath.Should().Be("Lattice.Cli/Program.cs");
        missingExplicit.FileContents.Should().BeNull();
        missingExplicit.Summary.Should().Contain("'Lattice.Cli/Program.cs' was not found");
    }

    [Fact]
    public async Task MissingBasenameExplainsFileWasNotFoundAnywhereInWorkspace()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("src/Existing.cs", "class Existing { }");

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "Show me MissingWidget.cs.";
        await session.SendMessageAsync();

        DesktopTurnEntry missing = session.Entries.Should().ContainSingle().Which;
        missing.Kind.Should().Be(DesktopTurnKind.MissingFile);
        missing.ErrorCode.Should().Be(FileToolErrorCodes.PathNotFound);
        missing.Summary.Should().Contain("couldn't find a file named 'MissingWidget.cs' anywhere in the connected workspace");
    }

    [Fact]
    public async Task LargeDuplicateSetTruncatesDisplayedListReportsOmittedCountAndStillOpensOmittedCandidate()
    {
        using DesktopTempWorkspace workspace = new();
        for (int i = 1; i <= 14; i++)
        {
            workspace.WriteFile($"Services/Service{i:D2}/Program.cs", $"// service {i}\n");
        }

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "Show me Program.cs";
        await session.SendMessageAsync();

        DesktopTurnEntry turn = session.Entries.Should().ContainSingle().Which;
        turn.Kind.Should().Be(DesktopTurnKind.AmbiguousInput);
        turn.TotalCandidateCount.Should().Be(14);
        turn.SafeCandidatePaths.Length.Should().Be(DesktopRequestProcessor.DefaultMaxDisplayedCandidatePaths);
        turn.OmittedCandidateCount.Should().Be(4);
        turn.Summary.Should().Contain("14 files named 'Program.cs'");
        turn.Summary.Should().Contain("4 more omitted");

        // Selecting candidate #13 (which was omitted from the first 10 displayed) still opens it cleanly.
        session.MessageInput = "Services/Service13/Program.cs";
        await session.SendMessageAsync();

        DesktopTurnEntry opened = session.Entries[1];
        opened.Kind.Should().Be(DesktopTurnKind.FileInspected);
        opened.InspectedRelativePath.Should().Be("Services/Service13/Program.cs");
        opened.FileContents.Should().Be("// service 13\n");
    }

    [Fact]
    public async Task BasenameDiscoveryDoesNotExposeFilesBehindLinkedDirectoryOrExcludedFolders()
    {
        using DesktopTempWorkspace outside = new();
        outside.WriteFile("SecretConfig.cs", "OUTSIDE_SECRET");

        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("bin/Debug/SecretConfig.cs", "BUILD_OUTPUT");
        workspace.WriteFile(".git/SecretConfig.cs", "GIT_INTERNAL");
        string linkPath = Path.Combine(workspace.Path, "linked-outside");
        CreateDirectoryLinkOrJunction(linkPath, outside.Path);

        try
        {
            DesktopSession session = await ConnectSessionAsync(workspace.Path);
            session.MessageInput = "Show me SecretConfig.cs";
            await session.SendMessageAsync();

            DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
            entry.Kind.Should().Be(DesktopTurnKind.MissingFile);
            entry.FileContents.Should().BeNull();
            entry.Summary.Should().NotContain("OUTSIDE_SECRET");
        }
        finally
        {
            if (Directory.Exists(linkPath))
            {
                Directory.Delete(linkPath, recursive: false);
            }
        }
    }

    [Fact]
    public async Task RenderedChatDisplaysCandidatePathsThenOpensSelectedRelativePath()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("Lattice.Cli/Program.cs", "// cli entry\n");
        workspace.WriteFile("Lattice.Desktop/Program.cs", "// desktop entry\n");

        await using BunitContext context = new();
        context.Services.AddSingleton<DesktopSession>();
        IRenderedComponent<App> component = context.Render<App>();

        await component.Find("#workspace-path").InputAsync(workspace.Path);
        await component.Find("#workspace-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        await component.Find("#message-input").InputAsync("Show me Program.cs");
        await component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        var exchanges = component.FindAll("#turn-history > li.chat-exchange");
        exchanges.Should().ContainSingle();
        exchanges[0].GetAttribute("data-turn-kind").Should().Be("AmbiguousInput");
        exchanges[0].QuerySelectorAll(".file-viewer").Should().BeEmpty();

        string[] renderedCandidates = [.. exchanges[0]
            .QuerySelectorAll(".candidate-path")
            .Select(node => node.TextContent)];
        renderedCandidates.Should().Equal(["Lattice.Cli/Program.cs", "Lattice.Desktop/Program.cs"]);

        await component.Find("#message-input").InputAsync("Lattice.Desktop/Program.cs");
        await component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        exchanges = component.FindAll("#turn-history > li.chat-exchange");
        exchanges.Count.Should().Be(2);
        exchanges[1].GetAttribute("data-turn-kind").Should().Be("FileInspected");
        exchanges[1].QuerySelector(".file-path")!.TextContent.Should().Be("Lattice.Desktop/Program.cs");
        exchanges[1].QuerySelector(".file-contents")!.TextContent.Should().Contain("// desktop entry");
    }

    [Fact]
    public async Task RealRepositorySupportsShowMeProgramCsDuplicateClarificationUniqueNestedBasenameAndRootPrecedence()
    {
        string repoRoot = LocateLatticeRepositoryRoot();
        await using BunitContext context = new();
        context.Services.AddSingleton<DesktopSession>();
        IRenderedComponent<App> component = context.Render<App>();

        await component.Find("#workspace-path").InputAsync(repoRoot);
        await component.Find("#workspace-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        // 1. "Show me Program.cs." discovers Lattice.Cli/Program.cs and Lattice.Desktop/Program.cs without guessing.
        await component.Find("#message-input").InputAsync("Show me Program.cs.");
        await component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        var exchanges = component.FindAll("#turn-history > li.chat-exchange");
        exchanges.Should().ContainSingle();
        exchanges[0].GetAttribute("data-turn-kind").Should().Be("AmbiguousInput");
        exchanges[0].QuerySelectorAll(".file-viewer").Should().BeEmpty();
        string[] programCandidates = [.. exchanges[0]
            .QuerySelectorAll(".candidate-path")
            .Select(node => node.TextContent)];
        programCandidates.Should().Equal(["Lattice.Cli/Program.cs", "Lattice.Desktop/Program.cs", "tests/evaluation/m9_3/baseline/Program.cs"]);

        // 2. Replying with a displayed relative path opens that exact file.
        await component.Find("#message-input").InputAsync("Lattice.Desktop/Program.cs");
        await component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        // 3. Unique nested basename opens directly.
        await component.Find("#message-input").InputAsync("Show me WorkspaceRoot.cs.");
        await component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        // 4. Root README.md takes precedence over docs/README.md.
        await component.Find("#message-input").InputAsync("What's in README.md?");
        await component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        exchanges = component.FindAll("#turn-history > li.chat-exchange");
        exchanges.Count.Should().Be(4);
        exchanges[1].GetAttribute("data-turn-kind").Should().Be("FileInspected");
        exchanges[1].QuerySelector(".file-path")!.TextContent.Should().Be("Lattice.Desktop/Program.cs");
        exchanges[2].GetAttribute("data-turn-kind").Should().Be("FileInspected");
        exchanges[2].QuerySelector(".file-path")!.TextContent.Should().Be("Lattice/WorkspaceRoot.cs");
        exchanges[3].GetAttribute("data-turn-kind").Should().Be("FileInspected");
        exchanges[3].QuerySelector(".file-path")!.TextContent.Should().Be("README.md");
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

        throw new DirectoryNotFoundException("Could not locate Lattice.slnx above the test base directory.");
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

    private static void CreateDirectoryLinkOrJunction(string linkPath, string targetPath)
    {
        try
        {
            Directory.CreateSymbolicLink(linkPath, targetPath);
            return;
        }
        catch (Exception exception) when (
            OperatingSystem.IsWindows()
            && exception is IOException or UnauthorizedAccessException)
        {
            // Fall back to an unprivileged NTFS junction when SeCreateSymbolicLinkPrivilege is absent.
        }

        ProcessStartInfo startInfo = new("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add("mklink");
        startInfo.ArgumentList.Add("/J");
        startInfo.ArgumentList.Add(linkPath);
        startInfo.ArgumentList.Add(targetPath);

        using Process process = Process.Start(startInfo)!;
        process.WaitForExit();
        (process.ExitCode == 0).Should().BeTrue("{0}", $"mklink /J failed: {process.StandardError.ReadToEnd()}");
    }
}
