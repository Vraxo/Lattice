using System.Diagnostics;
using Bunit;
using Lattice.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Lattice.Desktop.Tests;

public sealed class DesktopFindSymbolTests
{
    [Fact]
    public async Task FindSymbolReturnsMultipleGroundedOccurrencesAcrossFiles()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("README.md", "# Demo\nUses WidgetEngine for tasks.\n");
        workspace.WriteFile("src/WidgetEngine.cs", "namespace Demo;\n\npublic sealed class WidgetEngine\n{\n}\n");
        DesktopSession session = await ConnectSessionAsync(workspace.Path);

        session.RequestInput = "find symbol WidgetEngine";
        await session.SubmitRequestAsync();

        DesktopTurnEntry entry = Assert.Single(session.Entries);
        Assert.Equal(DesktopTurnKind.SearchResults, entry.Kind);
        Assert.Equal("Found 'WidgetEngine' in 2 files, on 2 lines.", entry.Summary);
        Assert.Equal(2, entry.SafeEvidence.Length);
        Assert.Contains(
            entry.SafeEvidence,
            item => item.RelativePath == "README.md"
                && item.StartLine == 2
                && item.Snippet == "Uses WidgetEngine for tasks.");
        Assert.Contains(
            entry.SafeEvidence,
            item => item.RelativePath == "src/WidgetEngine.cs"
                && item.StartLine == 3
                && item.Snippet == "public sealed class WidgetEngine");
        Assert.All(
            entry.SafeEvidence,
            item => Assert.True(session.ActiveWorkspace!.TryResolve(item.RelativePath, out _)));
    }

    [Fact]
    public async Task FindSymbolSingleMatchIsCaseInsensitiveAndGrammatical()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("src/UniqueToken.cs", "public static class UniqueToken { }");
        DesktopSession session = await ConnectSessionAsync(workspace.Path);

        session.RequestInput = "find symbol uniquetoken";
        await session.SubmitRequestAsync();

        DesktopTurnEntry entry = Assert.Single(session.Entries);
        Assert.Equal(DesktopTurnKind.SearchResults, entry.Kind);
        Assert.Equal("Found 'uniquetoken' in 1 file, on 1 line.", entry.Summary);
        EvidenceReference single = Assert.Single(entry.SafeEvidence);
        Assert.Equal("src/UniqueToken.cs", single.RelativePath);
        Assert.Equal(1, single.StartLine);
    }

    [Fact]
    public async Task FindSymbolNoMatchesReportsClearNoMatchesState()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("src/OnlyThis.cs", "public sealed class OnlyThis { }");
        DesktopSession session = await ConnectSessionAsync(workspace.Path);

        session.RequestInput = "find symbol AbsentSymbol";
        await session.SubmitRequestAsync();

        DesktopTurnEntry entry = Assert.Single(session.Entries);
        Assert.Equal(DesktopTurnKind.NoMatches, entry.Kind);
        Assert.Empty(entry.SafeEvidence);
        Assert.Contains("No occurrences of 'AbsentSymbol'", entry.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FindSymbolSkipsExcludedDirectoriesOversizedFilesAndLinkedEntries()
    {
        using DesktopTempWorkspace outside = new();
        outside.WriteFile("Leaked.cs", "public class TargetToken { }");

        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("src/Kept.cs", "// line 1\npublic sealed class TargetToken { }\n");
        workspace.WriteFile("bin/Ignored.cs", "public sealed class TargetToken { }");
        workspace.WriteFile("artifacts/Ignored.cs", "public sealed class TargetToken { }");

        byte[] oversized = new byte[FileSearchTool.MaxScannedFileBytes + 64];
        Array.Fill(oversized, (byte)'a');
        "TargetToken\n"u8.CopyTo(oversized);
        workspace.WriteBytes("src/Oversized.cs", oversized);

        string linkDir = Path.Combine(workspace.Path, "external-link");
        CreateDirectoryLinkOrJunction(linkDir, outside.Path);

        try
        {
            DesktopSession session = await ConnectSessionAsync(workspace.Path);
            session.RequestInput = "find symbol TargetToken";
            await session.SubmitRequestAsync();

            DesktopTurnEntry entry = Assert.Single(session.Entries);
            Assert.Equal(DesktopTurnKind.SearchResults, entry.Kind);
            EvidenceReference match = Assert.Single(entry.SafeEvidence);
            Assert.Equal("src/Kept.cs", match.RelativePath);
            Assert.Equal(2, match.StartLine);
        }
        finally
        {
            if (Directory.Exists(linkDir))
            {
                Directory.Delete(linkDir, recursive: false);
            }
        }
    }

    [Theory]
    [InlineData("hello lattice")]
    [InlineData("what is WorkspaceRoot")]
    [InlineData("explain Calculator")]
    [InlineData("find symbol")]
    [InlineData("find Calculator")]
    [InlineData("refactor README.md")]
    [InlineData("find symbol Two Words")]
    public async Task UnsupportedRequestsAreNotSilentlyConverted(string request)
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("README.md", "Calculator");
        DesktopSession session = await ConnectSessionAsync(workspace.Path);

        session.RequestInput = request;
        await session.SubmitRequestAsync();

        DesktopTurnEntry entry = Assert.Single(session.Entries);
        Assert.Equal(DesktopTurnKind.UnsupportedInput, entry.Kind);
        Assert.Empty(entry.SafeEvidence);
        Assert.Null(entry.FileContents);
    }

    [Fact]
    public async Task AmbiguousRequestProducesDistinctAmbiguousInputState()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("README.md", "# Demo");
        DesktopSession session = await ConnectSessionAsync(workspace.Path);

        session.RequestInput = "show README.md";
        await session.SubmitRequestAsync();

        DesktopTurnEntry entry = Assert.Single(session.Entries);
        Assert.Equal(DesktopTurnKind.AmbiguousInput, entry.Kind);
        Assert.Contains("Ambiguous request", entry.Summary, StringComparison.Ordinal);
        Assert.Empty(entry.SafeEvidence);
        Assert.Null(entry.FileContents);
    }

    [Fact]
    public async Task RenderedUiShowsEvidencePathsLineNumbersAndSourceTextSearchLabel()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("src/Alpha.cs", "namespace Demo;\npublic sealed class SharedSymbol { }\n");
        workspace.WriteFile("docs/Guide.md", "Reference to SharedSymbol.\n");

        await using BunitContext context = new();
        context.Services.AddSingleton<DesktopSession>();
        IRenderedComponent<App> component = context.Render<App>();

        Assert.Contains("source-text", component.Find("#composer-hint").TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not semantic", component.Find("#composer-hint").TextContent, StringComparison.OrdinalIgnoreCase);

        await component.Find("#workspace-path").InputAsync(workspace.Path);
        await component.Find("#workspace-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        await component.Find("#message-input").InputAsync("find symbol SharedSymbol");
        await component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        Assert.Contains(
            "source-text",
            component.Find(".search-mode-note").TextContent,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "not semantic",
            component.Find(".search-mode-note").TextContent,
            StringComparison.OrdinalIgnoreCase);

        var locations = component.FindAll(".evidence-location").Select(node => node.TextContent).ToArray();
        Assert.Contains("src/Alpha.cs:2", locations);
        Assert.Contains("docs/Guide.md:1", locations);
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
