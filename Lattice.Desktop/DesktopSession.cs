using Lattice.Core;

namespace Lattice.Desktop;

internal sealed class DesktopSession(
    Func<string?, CancellationToken, Task<WorkspaceConnectionResult>>? connectWorkspace = null,
    Func<ConnectedWorkspace?, string, CancellationToken, Task<DesktopTurnEntry>>? executeRequest = null)
{
    private readonly Func<string?, CancellationToken, Task<WorkspaceConnectionResult>> _connectWorkspace =
        connectWorkspace ?? ((path, _) => Task.FromResult(WorkspaceConnector.Connect(path)));

    private readonly Func<ConnectedWorkspace?, string, CancellationToken, Task<DesktopTurnEntry>> _executeRequest =
        executeRequest ?? ((workspace, request, _) => Task.FromResult(DesktopRequestProcessor.Default.Execute(workspace, request)));

    private readonly List<DesktopTurnEntry> _entries = [];
    private int _busyGate;

    public string WorkspacePathInput { get; set; } = string.Empty;

    public string RequestInput { get; set; } = string.Empty;

    public ConnectedWorkspace? ActiveConnection { get; private set; }

    public WorkspaceRoot? ActiveWorkspace => ActiveConnection?.Root;

    public string? ConnectedWorkspacePath => ActiveWorkspace?.FullPath;

    public bool IsConnected => ActiveConnection is not null;

    public bool IsConnecting { get; private set; }

    public bool IsSubmitting { get; private set; }

    public bool IsBusy => IsConnecting || IsSubmitting || Volatile.Read(ref _busyGate) != 0;

    public string? WorkspaceError { get; private set; }

    public string? RequestError { get; private set; }

    public string StatusMessage { get; private set; } =
        "Connect a local workspace folder to begin read-only inspection.";

    public IReadOnlyList<DesktopTurnEntry> Entries => _entries;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _busyGate, 1, 0) != 0)
        {
            return;
        }

        WorkspaceError = null;
        RequestError = null;
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

            ActiveConnection = result.Workspace;
            WorkspacePathInput = result.Workspace.Root.FullPath;
            StatusMessage = $"Connected to '{result.Workspace.Root.FullPath}' (read-only).";
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
            Volatile.Write(ref _busyGate, 0);
        }
    }

    public async Task SubmitRequestAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _busyGate, 1, 0) != 0)
        {
            return;
        }

        RequestError = null;
        string trimmed = RequestInput.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            Volatile.Write(ref _busyGate, 0);
            RequestError = "Enter a request: 'inspect <relative-path>' or 'find symbol <name>'.";
            StatusMessage = "Request was empty.";
            return;
        }

        IsSubmitting = true;
        StatusMessage = "Running read-only workspace request…";
        try
        {
            DesktopTurnEntry entry = await _executeRequest(ActiveConnection, trimmed, cancellationToken);
            _entries.Add(entry);
            RequestInput = string.Empty;
            StatusMessage = entry.Summary;
        }
        catch (Exception exception)
        {
            DesktopTurnEntry failure = new(
                trimmed,
                DesktopTurnKind.UnexpectedError,
                $"Unexpected error while handling request: {exception.Message}",
                ConnectedWorkspacePath);
            _entries.Add(failure);
            StatusMessage = failure.Summary;
        }
        finally
        {
            IsSubmitting = false;
            Volatile.Write(ref _busyGate, 0);
        }
    }
}
