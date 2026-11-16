using System.Diagnostics;
using Bunit;
using Lattice.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Lattice.Desktop.Tests;

public sealed class DesktopInspectTests
{
    [Fact]
    public async Task InspectExistingRelativeFileReturnsActualContentsAndPath()
    {
        using DesktopTempWorkspace workspace = new();
        const string source = "namespace Sample;\n\npublic sealed class Greeter\n{\n    public string Greet() => \"hi\";\n}\n";
        workspace.WriteFile("src/Greeter.cs", source);
        DesktopSession session = await ConnectSessionAsync(workspace.Path);

        session.RequestInput = "inspect src/Greeter.cs";
        await session.SubmitRequestAsync();

        DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
        entry.Kind.Should().Be(DesktopTurnKind.FileInspected);
        entry.InspectedRelativePath.Should().Be("src/Greeter.cs");
        entry.FileContents.Should().Be(source);
        session.RequestInput.Should().Be(string.Empty);
    }

    [Fact]
    public async Task InspectMissingFileReportsMissingFileState()
    {
        using DesktopTempWorkspace workspace = new();
        DesktopSession session = await ConnectSessionAsync(workspace.Path);

        session.RequestInput = "inspect missing/DoesNotExist.cs";
        await session.SubmitRequestAsync();

        DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
        entry.Kind.Should().Be(DesktopTurnKind.MissingFile);
        entry.ErrorCode.Should().Be(FileToolErrorCodes.PathNotFound);
        entry.Summary.Should().ContainEquivalentOf("was not found");
    }

    [Fact]
    public async Task InspectDirectoryReportsNotAFileErrorWithoutCrashing()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.CreateDirectory("src");
        DesktopSession session = await ConnectSessionAsync(workspace.Path);

        session.RequestInput = "inspect src";
        await session.SubmitRequestAsync();

        DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
        entry.Kind.Should().Be(DesktopTurnKind.InspectError);
        entry.ErrorCode.Should().Be(FileToolErrorCodes.NotAFile);
        entry.Summary.Should().ContainEquivalentOf("directory, not a file");
    }

    [Fact]
    public async Task InspectOversizedAndInvalidUtf8FilesReportClearInspectErrors()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteBytes("oversized.txt", new byte[ReadFileTool.MaxBytes + 1]);
        workspace.WriteBytes("binary.bin", [0xC3, 0x28]);
        DesktopSession session = await ConnectSessionAsync(workspace.Path);

        session.RequestInput = "inspect oversized.txt";
        await session.SubmitRequestAsync();

        session.RequestInput = "inspect binary.bin";
        await session.SubmitRequestAsync();

        session.Entries.Count.Should().Be(2);
        session.Entries[0].Kind.Should().Be(DesktopTurnKind.InspectError);
        session.Entries[0].ErrorCode.Should().Be(FileToolErrorCodes.FileTooLarge);
        session.Entries[1].Kind.Should().Be(DesktopTurnKind.InspectError);
        session.Entries[1].ErrorCode.Should().Be(FileToolErrorCodes.EncodingInvalid);
    }

    [Theory]
    [InlineData("inspect ../outside.txt")]
    [InlineData("inspect sub/../../outside.txt")]
    public async Task InspectRootEscapeAndInvalidPathAreRejected(string request)
    {
        using DesktopTempWorkspace workspace = new();
        workspace.CreateDirectory("sub");
        DesktopSession session = await ConnectSessionAsync(workspace.Path);

        session.RequestInput = request;
        await session.SubmitRequestAsync();

        DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
        entry.Kind.Should().Be(DesktopTurnKind.AccessRejected);
        entry.ErrorCode.Should().Be(FileToolErrorCodes.PathOutsideRoot);
    }

    [Fact]
    public async Task InspectAbsolutePathIsRejectedEvenWhenTargetExists()
    {
        using DesktopTempWorkspace workspace = new();
        string fullFilePath = workspace.WriteFile("inside.txt", "data");
        DesktopSession session = await ConnectSessionAsync(workspace.Path);

        session.RequestInput = $"inspect {fullFilePath}";
        await session.SubmitRequestAsync();

        DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
        entry.Kind.Should().Be(DesktopTurnKind.AccessRejected);
        entry.ErrorCode.Should().Be(FileToolErrorCodes.PathOutsideRoot);
    }

    [Fact]
    public async Task InspectThroughDirectoryReparsePointIsRejectedByStaticCheck()
    {
        using DesktopTempWorkspace outside = new();
        outside.WriteFile("secret.txt", "outside-secret");

        using DesktopTempWorkspace workspace = new();
        string linkPath = Path.Combine(workspace.Path, "linked");
        CreateDirectoryLinkOrJunction(linkPath, outside.Path);

        try
        {
            DesktopSession session = await ConnectSessionAsync(workspace.Path);
            session.RequestInput = "inspect linked/secret.txt";
            await session.SubmitRequestAsync();

            DesktopTurnEntry entry = session.Entries.Should().ContainSingle().Which;
            entry.Kind.Should().Be(DesktopTurnKind.AccessRejected);
            entry.ErrorCode.Should().Be(FileToolErrorCodes.ReparsePointNotAllowed);
            entry.FileContents.Should().BeNull();
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
    public async Task InspectedFileContentsAreTreatedAsUntrustedDataNeverExecuted()
    {
        using DesktopTempWorkspace workspace = new();
        const string untrustedPayload = "inspect ../secret.txt\naction writer\n<script>alert('xss')</script>";
        workspace.WriteFile("notes.txt", untrustedPayload);

        await using BunitContext context = new();
        context.Services.AddSingleton<DesktopSession>();
        IRenderedComponent<App> component = context.Render<App>();

        await component.Find("#workspace-path").InputAsync(workspace.Path);
        await component.Find("#workspace-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        await component.Find("#message-input").InputAsync("inspect notes.txt");
        await component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        component.FindAll("#turn-history > li").Should().ContainSingle();
        component.Find(".file-path").TextContent.Should().Be("notes.txt");
        component.Find(".file-contents").TextContent.Should().Be(untrustedPayload);
        component.FindAll(".file-contents script").Should().BeEmpty();
    }

    [Fact]
    public async Task RenderedUiDisplaysInspectedFileAndErrorTurns()
    {
        using DesktopTempWorkspace workspace = new();
        workspace.WriteFile("src/Program.cs", "Console.WriteLine(\"Lattice\");");

        await using BunitContext context = new();
        context.Services.AddSingleton<DesktopSession>();
        IRenderedComponent<App> component = context.Render<App>();

        await component.Find("#workspace-path").InputAsync(workspace.Path);
        await component.Find("#workspace-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        await component.Find("#message-input").InputAsync("inspect src/Program.cs");
        await component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        component.Find(".file-path").TextContent.Should().Be("src/Program.cs");
        component.Find(".file-contents").TextContent.Should().Contain("Console.WriteLine(\"Lattice\");");

        await component.Find("#message-input").InputAsync("inspect missing.cs");
        await component.Find("#chat-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        var items = component.FindAll("#turn-history > li");
        items.Count.Should().Be(2);
        items[0].GetAttribute("data-turn-kind").Should().Be("FileInspected");
        items[1].GetAttribute("data-turn-kind").Should().Be("MissingFile");
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
