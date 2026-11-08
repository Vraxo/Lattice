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

        DesktopTurnEntry entry = Assert.Single(session.Entries);
        Assert.Equal(DesktopTurnKind.FileInspected, entry.Kind);
        Assert.Equal("# Root README\n", entry.FileContents);
        Assert.True(string.Equals("README.md", entry.InspectedRelativePath, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("/", entry.InspectedRelativePath!, StringComparison.Ordinal);
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

        DesktopTurnEntry entry = Assert.Single(session.Entries);
        Assert.Equal(DesktopTurnKind.FileInspected, entry.Kind);
        Assert.Equal(expectedRelativePath, entry.InspectedRelativePath);
        Assert.Contains($"Here is '{expectedRelativePath}'", entry.Summary, StringComparison.Ordinal);
        Assert.NotNull(entry.FileContents);
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

        DesktopTurnEntry ambiguous = Assert.Single(session.Entries);
        Assert.Equal(DesktopTurnKind.AmbiguousInput, ambiguous.Kind);
        Assert.Null(ambiguous.FileContents);
        Assert.Null(ambiguous.InspectedRelativePath);
        Assert.Equal(2, ambiguous.TotalCandidateCount);
        Assert.Equal<IEnumerable<string>>(
            ["Lattice.Cli/Program.cs", "Lattice.Desktop/Program.cs"],
            ambiguous.SafeCandidatePaths);
        Assert.Contains("Lattice.Cli/Program.cs", ambiguous.Summary, StringComparison.Ordinal);
        Assert.Contains("Lattice.Desktop/Program.cs", ambiguous.Summary, StringComparison.Ordinal);
        Assert.Contains("Which file would you like me to open?", ambiguous.Summary, StringComparison.Ordinal);

        // User replies directly with a displayed relative path (without needing "Show me ").
        session.MessageInput = "Lattice.Desktop/Program.cs";
        await session.SendMessageAsync();

        Assert.Equal(2, session.Entries.Count);
        DesktopTurnEntry opened = session.Entries[1];
        Assert.Equal(DesktopTurnKind.FileInspected, opened.Kind);
        Assert.Equal("Lattice.Desktop/Program.cs", opened.InspectedRelativePath);
        Assert.Equal(desktopProgram, opened.FileContents);
    }

    [Fact]
    public async Task ExplicitRelativePathTakesPrecedenceAndIsNeverReplacedBySameBasenameElsewhere()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("Lattice.Desktop/Program.cs", "// desktop");

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "Show me Lattice.Cli/Program.cs.";
        await session.SendMessageAsync();

        DesktopTurnEntry missingExplicit = Assert.Single(session.Entries);
        Assert.Equal(DesktopTurnKind.MissingFile, missingExplicit.Kind);
        Assert.Equal("Lattice.Cli/Program.cs", missingExplicit.InspectedRelativePath);
        Assert.Null(missingExplicit.FileContents);
        Assert.Contains("'Lattice.Cli/Program.cs' was not found", missingExplicit.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingBasenameExplainsFileWasNotFoundAnywhereInWorkspace()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("src/Existing.cs", "class Existing { }");

        DesktopSession session = await ConnectSessionAsync(workspace.Path);
        session.MessageInput = "Show me MissingWidget.cs.";
        await session.SendMessageAsync();

        DesktopTurnEntry missing = Assert.Single(session.Entries);
        Assert.Equal(DesktopTurnKind.MissingFile, missing.Kind);
        Assert.Equal(FileToolErrorCodes.PathNotFound, missing.ErrorCode);
        Assert.Contains("couldn't find a file named 'MissingWidget.cs' anywhere in the connected workspace", missing.Summary, StringComparison.Ordinal);
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

        DesktopTurnEntry turn = Assert.Single(session.Entries);
        Assert.Equal(DesktopTurnKind.AmbiguousInput, turn.Kind);
        Assert.Equal(14, turn.TotalCandidateCount);
        Assert.Equal(DesktopRequestProcessor.DefaultMaxDisplayedCandidatePaths, turn.SafeCandidatePaths.Length);
        Assert.Equal(4, turn.OmittedCandidateCount);
        Assert.Contains("14 files named 'Program.cs'", turn.Summary, StringComparison.Ordinal);
        Assert.Contains("4 more omitted", turn.Summary, StringComparison.Ordinal);

        // Selecting candidate #13 (which was omitted from the first 10 displayed) still opens it cleanly.
        session.MessageInput = "Services/Service13/Program.cs";
        await session.SendMessageAsync();

        DesktopTurnEntry opened = session.Entries[1];
        Assert.Equal(DesktopTurnKind.FileInspected, opened.Kind);
        Assert.Equal("Services/Service13/Program.cs", opened.InspectedRelativePath);
        Assert.Equal("// service 13\n", opened.FileContents);
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

            DesktopTurnEntry entry = Assert.Single(session.Entries);
            Assert.Equal(DesktopTurnKind.MissingFile, entry.Kind);
            Assert.Null(entry.FileContents);
            Assert.DoesNotContain("OUTSIDE_SECRET", entry.Summary, StringComparison.Ordinal);
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
        Assert.Single(exchanges);
        Assert.Equal("AmbiguousInput", exchanges[0].GetAttribute("data-turn-kind"));
        Assert.Empty(exchanges[0].QuerySelectorAll(".file-viewer"));

        string[] renderedCandidates = [.. exchanges[0]
            .QuerySelectorAll(".candidate-path")
            .Select(node => node.TextContent)];
        Assert.Equal<IEnumerable<string>>(
            ["Lattice.Cli/Program.cs", "Lattice.Desktop/Program.cs"],
            renderedCandidates);

        await component.Find("#message-input").InputAsync("Lattice.Desktop/Program.cs");
        await component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        exchanges = component.FindAll("#turn-history > li.chat-exchange");
        Assert.Equal(2, exchanges.Count);
        Assert.Equal("FileInspected", exchanges[1].GetAttribute("data-turn-kind"));
        Assert.Equal("Lattice.Desktop/Program.cs", exchanges[1].QuerySelector(".file-path")!.TextContent);
        Assert.Contains("// desktop entry", exchanges[1].QuerySelector(".file-contents")!.TextContent, StringComparison.Ordinal);
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
        Assert.True(process.ExitCode == 0, $"mklink /J failed: {process.StandardError.ReadToEnd()}");
    }
}
