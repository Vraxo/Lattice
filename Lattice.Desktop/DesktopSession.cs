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
    private string _messageInput = string.Empty;
    private int _busyGate;

    public string WorkspacePathInput { get; set; } = string.Empty;

    public string MessageInput
    {
        get => _messageInput;
        set => _messageInput = value ?? string.Empty;
    }

    public string RequestInput
    {
        get => _messageInput;
        set => _messageInput = value ?? string.Empty;
    }

    public ConnectedWorkspace? ActiveConnection { get; private set; }

    public WorkspaceRoot? ActiveWorkspace => ActiveConnection?.Root;

    public string? ConnectedWorkspacePath => ActiveWorkspace?.FullPath;

    public bool IsConnected => ActiveConnection is not null;

    public bool IsConnecting { get; private set; }

    public bool IsSubmitting { get; private set; }

    public bool IsBusy => IsConnecting || IsSubmitting || Volatile.Read(ref _busyGate) != 0;

    public string? PendingRequestText { get; private set; }

    public string? WorkspaceError { get; private set; }

    public string? RequestError { get; private set; }

    public string StatusMessage { get; private set; } =
        "Connect a local workspace folder to start asking about your codebase.";

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

    public Task SendMessageAsync(CancellationToken cancellationToken = default)
    {
        return SubmitRequestAsync(cancellationToken);
    }

    public async Task SubmitRequestAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _busyGate, 1, 0) != 0)
        {
            return;
        }

        RequestError = null;
        string rawInput = _messageInput;
        string trimmed = rawInput.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            Volatile.Write(ref _busyGate, 0);
            RequestError = "Write a question or file request before sending.";
            StatusMessage = "Message was empty.";
            return;
        }

        string displayMessage = rawInput.Trim('\r', '\n');
        if (string.IsNullOrWhiteSpace(displayMessage))
        {
            displayMessage = trimmed;
        }

        _messageInput = string.Empty;
        PendingRequestText = displayMessage;
        IsSubmitting = true;
        StatusMessage = "Checking the connected workspace…";
        try
        {
            DesktopTurnEntry entry = await _executeRequest(ActiveConnection, trimmed, cancellationToken);
            if (entry.RequestText != displayMessage)
            {
                entry = entry with { RequestText = displayMessage };
            }

            _entries.Add(entry);
            StatusMessage = entry.Summary;
        }
        catch (Exception exception)
        {
            if (string.IsNullOrEmpty(_messageInput))
            {
                _messageInput = rawInput;
            }

            DesktopTurnEntry failure = new(
                displayMessage,
                DesktopTurnKind.UnexpectedError,
                $"Unexpected error while handling your message: {exception.Message}",
                ConnectedWorkspacePath);
            _entries.Add(failure);
            StatusMessage = failure.Summary;
        }
        finally
        {
            PendingRequestText = null;
            IsSubmitting = false;
            Volatile.Write(ref _busyGate, 0);
        }
    }
}
