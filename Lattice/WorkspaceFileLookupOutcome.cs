namespace Lattice.Core;

/// <summary>
/// Explicit outcomes of a bounded filename lookup. Incomplete scans remain distinct from
/// proven missing or unique matches so callers cannot silently treat a partial scan as final.
/// </summary>
public union WorkspaceFileLookupOutcome(
    WorkspaceFileFound,
    WorkspaceFileMissing,
    WorkspaceFileAmbiguous,
    WorkspaceFileLookupIncomplete,
    WorkspaceFileLookupFailed);

public sealed record WorkspaceFileFound(string RelativePath);

public sealed record WorkspaceFileMissing(string RequestedFileName);

public sealed record WorkspaceFileAmbiguous(WorkspaceFileLookupResult Matches);

public sealed record WorkspaceFileLookupIncomplete(WorkspaceFileLookupResult Matches);

public sealed record WorkspaceFileLookupFailed(Error Error);
