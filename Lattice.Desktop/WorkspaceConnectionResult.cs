using Lattice.Core;

namespace Lattice.Desktop;

internal sealed record WorkspaceConnectionResult(WorkspaceRoot? Workspace, string? ErrorMessage)
{
    public bool IsSuccess => Workspace is not null && ErrorMessage is null;

    public static WorkspaceConnectionResult Succeeded(WorkspaceRoot workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        return new WorkspaceConnectionResult(workspace, null);
    }

    public static WorkspaceConnectionResult Failed(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return new WorkspaceConnectionResult(null, errorMessage);
    }
}
