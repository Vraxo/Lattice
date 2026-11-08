using System.Collections.Immutable;

namespace Lattice.Core;

/// <summary>
/// Result of a bounded, read-only basename lookup inside a <see cref="WorkspaceRoot"/>.
/// </summary>
public sealed record WorkspaceFileLookupResult(
    string RequestedFileName,
    ImmutableArray<string> RelativePaths,
    int TotalMatches,
    bool HasMoreMatches = false)
{
    public ImmutableArray<string> SafeRelativePaths =>
        RelativePaths.IsDefault ? [] : RelativePaths;

    public int OmittedFromReturnedCount =>
        Math.Max(0, TotalMatches - SafeRelativePaths.Length);
}
