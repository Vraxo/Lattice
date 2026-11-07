using System.Collections.Immutable;
using Lattice.Core;

namespace Lattice.Desktop;

internal sealed record DesktopTurnEntry(
    string RequestText,
    DesktopTurnKind Kind,
    string Summary,
    string? WorkspacePath,
    string? InspectedRelativePath = null,
    string? FileContents = null,
    int? LineCount = null,
    string? SearchQuery = null,
    ImmutableArray<EvidenceReference> Evidence = default,
    string? ErrorCode = null)
{
    public ImmutableArray<EvidenceReference> SafeEvidence =>
        Evidence.IsDefault ? [] : Evidence;

    public bool IsError =>
        Kind is DesktopTurnKind.MissingFile
            or DesktopTurnKind.InspectError
            or DesktopTurnKind.AccessRejected
            or DesktopTurnKind.InvalidWorkspace
            or DesktopTurnKind.AmbiguousInput
            or DesktopTurnKind.UnsupportedInput
            or DesktopTurnKind.UnexpectedError;
}
