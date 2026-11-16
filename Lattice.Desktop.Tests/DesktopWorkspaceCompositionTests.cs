using System.Diagnostics;
using Lattice.Core;

namespace Lattice.Desktop.Tests;

public sealed class DesktopWorkspaceCompositionTests
{
    [Fact]
    public void ConnectedWorkspaceRegistersOnlyReadOnlyToolsAndPolicy()
    {
        using DesktopTempWorkspace workspace = new();
        WorkspaceConnectionResult result = WorkspaceConnector.Connect(workspace.Path);

        result.IsSuccess.Should().BeTrue();
        ConnectedWorkspace connected = result.Workspace!;

        connected.Policy.Should().Be(ToolPermissionPolicy.ReadOnlyOnly);
        connected.Policy.IsAllowed(ToolSideEffect.ReadOnly).Should().BeTrue();
        connected.Policy.IsAllowed(ToolSideEffect.LocalWrite).Should().BeFalse();
        connected.Policy.IsAllowed(ToolSideEffect.CommandExecution).Should().BeFalse();
        connected.Policy.IsAllowed(ToolSideEffect.ExternalSideEffect).Should().BeFalse();

        connected.Registry.Tools.Should().AllSatisfy(tool => tool.Descriptor.SideEffect.Should().Be(ToolSideEffect.ReadOnly));
        connected.Registry.TryResolve(WriteFileTool.Id, out _).Should().BeFalse();
        connected.Registry.TryResolve(MarkdownEditTool.Id, out _).Should().BeFalse();
        connected.Registry.TryResolve(GuardedCommandTool.Id, out _).Should().BeFalse();
    }

    [Fact]
    public void WorkspaceConnectorRejectsDirectoryReparsePointAsWorkspaceRoot()
    {
        using DesktopTempWorkspace target = new();
        using DesktopTempWorkspace container = new();
        string linkRoot = Path.Combine(container.Path, "linked-root");
        CreateDirectoryLinkOrJunction(linkRoot, target.Path);

        try
        {
            WorkspaceConnectionResult result = WorkspaceConnector.Connect(linkRoot);

            result.IsSuccess.Should().BeFalse();
            result.Workspace.Should().BeNull();
            result.ErrorMessage.Should().ContainEquivalentOf("reparse point");
        }
        finally
        {
            if (Directory.Exists(linkRoot))
            {
                Directory.Delete(linkRoot, recursive: false);
            }
        }
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
