namespace Lattice.Desktop.Tests;

public sealed class DesktopSessionConnectionTests
{
    [Fact]
    public async Task ConnectsExistingDirectoryAndSetsActiveWorkspace()
    {
        using DesktopTempWorkspace workspace = new();
        DesktopSession session = new()
        {
            WorkspacePathInput = workspace.Path,
        };

        await session.ConnectAsync();

        Assert.True(session.IsConnected);
        Assert.Equal(Path.GetFullPath(workspace.Path), session.ConnectedWorkspacePath);
        Assert.Null(session.WorkspaceError);
        Assert.Contains("read-only", session.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EmptyWorkspacePathReportsErrorWithoutConnecting()
    {
        DesktopSession session = new()
        {
            WorkspacePathInput = "   ",
        };

        await session.ConnectAsync();

        Assert.False(session.IsConnected);
        Assert.Null(session.ConnectedWorkspacePath);
        Assert.NotNull(session.WorkspaceError);
        Assert.Contains("folder path", session.WorkspaceError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MissingDirectoryReportsErrorWithoutConnecting()
    {
        string missing = Path.Combine(Path.GetTempPath(), "lattice-missing-" + Guid.NewGuid().ToString("N"));
        DesktopSession session = new()
        {
            WorkspacePathInput = missing,
        };

        await session.ConnectAsync();

        Assert.False(session.IsConnected);
        Assert.NotNull(session.WorkspaceError);
        Assert.Contains("does not exist", session.WorkspaceError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FilePathInsteadOfDirectoryReportsErrorAndRetainsActiveWorkspace()
    {
        using DesktopTempWorkspace workspace = new();
        string filePath = workspace.WriteFile("README.md", "# Demo");
        DesktopSession session = new()
        {
            WorkspacePathInput = workspace.Path,
        };
        await session.ConnectAsync();

        session.WorkspacePathInput = filePath;
        await session.ConnectAsync();

        Assert.True(session.IsConnected);
        Assert.Equal(Path.GetFullPath(workspace.Path), session.ConnectedWorkspacePath);
        Assert.NotNull(session.WorkspaceError);
        Assert.Contains("file, not a directory", session.WorkspaceError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OverlappingConnectAttemptIsIgnoredWhileBusy()
    {
        using DesktopTempWorkspace workspace = new();
        TaskCompletionSource<WorkspaceConnectionResult> pending =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        DesktopSession session = new((_, _) =>
        {
            calls++;
            return pending.Task;
        })
        {
            WorkspacePathInput = workspace.Path,
        };

        Task first = session.ConnectAsync();
        Assert.True(session.IsBusy);
        await session.ConnectAsync();

        pending.SetResult(WorkspaceConnector.Connect(workspace.Path));
        await first;

        Assert.Equal(1, calls);
        Assert.False(session.IsBusy);
        Assert.True(session.IsConnected);
    }
}
