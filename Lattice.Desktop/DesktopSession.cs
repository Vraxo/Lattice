using Lattice.Core;

namespace Lattice.Desktop;

internal sealed class DesktopSession(
    Func<string?, CancellationToken, Task<WorkspaceConnectionResult>>? connectWorkspace = null)
{
    private readonly Func<string?, CancellationToken, Task<WorkspaceConnectionResult>> _connectWorkspace =
        connectWorkspace ?? ((path, _) => Task.FromResult(WorkspaceConnector.Connect(path)));

    public string WorkspacePathInput { get; set; } = string.Empty;

    public WorkspaceRoot? ActiveWorkspace { get; private set; }

    public string? ConnectedWorkspacePath => ActiveWorkspace?.FullPath;

    public bool IsConnected => ActiveWorkspace is not null;

    public bool IsConnecting { get; private set; }

    public bool IsBusy => IsConnecting;

    public string? WorkspaceError { get; private set; }

    public string StatusMessage { get; private set; } =
        "Connect a local workspace folder to begin read-only inspection.";

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        WorkspaceError = null;
        IsConnecting = true;
        StatusMessage = "Validating workspace folder…";
        try
        {
            WorkspaceConnectionResult result = await _connectWorkspace(WorkspacePathInput, cancellationToken);
            if (!result.IsSuccess || result.Workspace is null)
            {
                WorkspaceError = result.ErrorMessage ?? "Could not connect to the requested workspace folder.";
                StatusMessage = ActiveWorkspace is null
                    ? "No workspace connected."
                    : $"Workspace '{ActiveWorkspace.FullPath}' remains active.";
                return;
            }

            ActiveWorkspace = result.Workspace;
            WorkspacePathInput = result.Workspace.FullPath;
            StatusMessage = $"Connected to '{result.Workspace.FullPath}' (read-only).";
        }
        catch (Exception exception)
        {
            WorkspaceError = $"Unexpected error while connecting workspace: {exception.Message}";
            StatusMessage = ActiveWorkspace is null
                ? "No workspace connected."
                : $"Workspace '{ActiveWorkspace.FullPath}' remains active.";
        }
        finally
        {
            IsConnecting = false;
        }
    }
}
