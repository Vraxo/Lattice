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

        session.IsConnected.Should().BeTrue();
        session.ConnectedWorkspacePath.Should().Be(Path.GetFullPath(workspace.Path));
        session.WorkspaceError.Should().BeNull();
        session.StatusMessage.Should().ContainEquivalentOf("read-only");
    }

    [Fact]
    public async Task EmptyWorkspacePathReportsErrorWithoutConnecting()
    {
        DesktopSession session = new()
        {
            WorkspacePathInput = "   ",
        };

        await session.ConnectAsync();

        session.IsConnected.Should().BeFalse();
        session.ConnectedWorkspacePath.Should().BeNull();
        session.WorkspaceError.Should().BeNotNull();
        session.WorkspaceError.Should().ContainEquivalentOf("folder path");
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

        session.IsConnected.Should().BeFalse();
        session.WorkspaceError.Should().BeNotNull();
        session.WorkspaceError.Should().ContainEquivalentOf("does not exist");
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

        session.IsConnected.Should().BeTrue();
        session.ConnectedWorkspacePath.Should().Be(Path.GetFullPath(workspace.Path));
        session.WorkspaceError.Should().BeNotNull();
        session.WorkspaceError.Should().ContainEquivalentOf("file, not a directory");
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
        session.IsBusy.Should().BeTrue();
        await session.ConnectAsync();

        pending.SetResult(WorkspaceConnector.Connect(workspace.Path));
        await first;

        calls.Should().Be(1);
        session.IsBusy.Should().BeFalse();
        session.IsConnected.Should().BeTrue();
    }
}
