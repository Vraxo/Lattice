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

        DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
        entry.Kind.Should().Be(DesktopTurnKind.SearchResults);
        entry.Summary.Should().Be("I found 2 matching occurrences of 'WidgetEngine' across 2 files.");
        entry.SafeEvidence.Length.Should().Be(2);
        entry.SafeEvidence.Where(item => item.RelativePath == "README.md"
                && item.StartLine == 2
                && item.Snippet == "Uses WidgetEngine for tasks.").Should().NotBeEmpty();
        entry.SafeEvidence.Where(item => item.RelativePath == "src/WidgetEngine.cs"
                && item.StartLine == 3
                && item.Snippet == "public sealed class WidgetEngine").Should().NotBeEmpty();
        entry.SafeEvidence.Should().AllSatisfy(item => session.ActiveWorkspace!.TryResolve(item.RelativePath, out _).Should().BeTrue());
    }

    [Fact]
    public async Task FindSymbolSingleMatchIsCaseInsensitiveAndGrammatical()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("src/UniqueToken.cs", "public static class UniqueToken { }");
        DesktopSession session = await ConnectSessionAsync(workspace.Path);

        session.RequestInput = "find symbol uniquetoken";
        await session.SubmitRequestAsync();

        DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
        entry.Kind.Should().Be(DesktopTurnKind.SearchResults);
        entry.Summary.Should().Be("I found 1 matching occurrence of 'uniquetoken' in 1 file.");
        EvidenceReference single = entry.SafeEvidence.Should().ContainSingle().Which;
        single.RelativePath.Should().Be("src/UniqueToken.cs");
        single.StartLine.Should().Be(1);
    }

    [Fact]
    public async Task FindSymbolNoMatchesReportsClearNoMatchesState()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("src/OnlyThis.cs", "public sealed class OnlyThis { }");
        DesktopSession session = await ConnectSessionAsync(workspace.Path);

        session.RequestInput = "find symbol AbsentSymbol";
        await session.SubmitRequestAsync();

        DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
        entry.Kind.Should().Be(DesktopTurnKind.NoMatches);
        entry.SafeEvidence.Should().BeEmpty();
        entry.Summary.Should().Contain("no literal source-text occurrences of 'AbsentSymbol'");
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

            DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
            entry.Kind.Should().Be(DesktopTurnKind.SearchResults);
            EvidenceReference match = entry.SafeEvidence.Should().ContainSingle().Which;
            match.RelativePath.Should().Be("src/Kept.cs");
            match.StartLine.Should().Be(2);
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

        DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
        entry.Kind.Should().Be(DesktopTurnKind.UnsupportedInput);
        entry.SafeEvidence.Should().BeEmpty();
        entry.FileContents.Should().BeNull();
    }

    [Fact]
    public async Task AmbiguousRequestProducesDistinctAmbiguousInputState()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("README.md", "# Demo");
        DesktopSession session = await ConnectSessionAsync(workspace.Path);

        session.RequestInput = "show README.md";
        await session.SubmitRequestAsync();

        DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
        entry.Kind.Should().Be(DesktopTurnKind.AmbiguousInput);
        entry.Summary.Should().Contain("Did you want me to open the file 'README.md'?");
        entry.Summary.Should().NotContain("show.inspect");
        entry.SafeEvidence.Should().BeEmpty();
        entry.FileContents.Should().BeNull();
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

        component.Find("#composer-hint").TextContent.Should().ContainEquivalentOf("source-text");
        component.Find("#composer-hint").TextContent.Should().ContainEquivalentOf("not semantic");

        await component.Find("#workspace-path").InputAsync(workspace.Path);
        await component.Find("#workspace-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        await component.Find("#message-input").InputAsync("find symbol SharedSymbol");
        await component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        component.Find(".search-mode-note").TextContent.Should().ContainEquivalentOf("source-text");
        component.Find(".search-mode-note").TextContent.Should().ContainEquivalentOf("not semantic");

        var locations = component.FindAll(".evidence-location").Select(node => node.TextContent).ToArray();
        locations.Should().Contain("src/Alpha.cs:2");
        locations.Should().Contain("docs/Guide.md:1");
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
        (process.ExitCode == 0).Should().BeTrue("{0}", $"mklink /J failed: {process.StandardError.ReadToEnd()}" ?? string.Empty);
    }
}
