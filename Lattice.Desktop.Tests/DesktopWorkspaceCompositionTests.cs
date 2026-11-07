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

        Assert.True(result.IsSuccess);
        ConnectedWorkspace connected = result.Workspace!;

        Assert.Equal(ToolPermissionPolicy.ReadOnlyOnly, connected.Policy);
        Assert.True(connected.Policy.IsAllowed(ToolSideEffect.ReadOnly));
        Assert.False(connected.Policy.IsAllowed(ToolSideEffect.LocalWrite));
        Assert.False(connected.Policy.IsAllowed(ToolSideEffect.CommandExecution));
        Assert.False(connected.Policy.IsAllowed(ToolSideEffect.ExternalSideEffect));

        Assert.All(
            connected.Registry.Tools,
            tool => Assert.Equal(ToolSideEffect.ReadOnly, tool.Descriptor.SideEffect));
        Assert.False(connected.Registry.TryResolve(WriteFileTool.Id, out _));
        Assert.False(connected.Registry.TryResolve(MarkdownEditTool.Id, out _));
        Assert.False(connected.Registry.TryResolve(GuardedCommandTool.Id, out _));
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

            Assert.False(result.IsSuccess);
            Assert.Null(result.Workspace);
            Assert.Contains("reparse point", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
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
        if (OperatingSystem.IsWindows())
        {
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
            return;
        }

        Directory.CreateSymbolicLink(linkPath, targetPath);
    }
}
