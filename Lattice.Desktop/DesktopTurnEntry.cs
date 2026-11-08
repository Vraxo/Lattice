using System.Collections.Immutable;
using Lattice.Core;
using Lattice.CSharp;

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
    string? ErrorCode = null,
    string? MatchedPatternId = null,
    string? CapturedValue = null,
    ImmutableArray<string> CandidatePaths = default,
    ImmutableArray<string> AllCandidatePaths = default,
    int? TotalCandidateCount = null,
    bool HasMoreCandidates = false,
    string? LeadSummary = null,
    ImmutableArray<CSharpDeclaration> Declarations = default,
    int? StructureSourceFileCount = null,
    int? StructureSkippedFileCount = null,
    int? StructureSyntaxErrorFiles = null,
    bool StructureIsTruncated = false)
{
    public ImmutableArray<EvidenceReference> SafeEvidence =>
        Evidence.IsDefault ? [] : Evidence;

    public ImmutableArray<CSharpDeclaration> SafeDeclarations =>
        Declarations.IsDefault ? [] : Declarations;

    public ImmutableArray<string> SafeCandidatePaths =>
        CandidatePaths.IsDefault ? [] : CandidatePaths;

    public ImmutableArray<string> SafeAllCandidatePaths =>
        AllCandidatePaths.IsDefault ? SafeCandidatePaths : AllCandidatePaths;

    public int OmittedCandidateCount =>
        TotalCandidateCount is int total
            ? Math.Max(0, total - SafeCandidatePaths.Length)
            : 0;

    public bool IsError =>
        Kind is DesktopTurnKind.MissingFile
            or DesktopTurnKind.InspectError
            or DesktopTurnKind.AccessRejected
            or DesktopTurnKind.InvalidWorkspace
            or DesktopTurnKind.AmbiguousInput
            or DesktopTurnKind.UnsupportedInput
            or DesktopTurnKind.UnexpectedError;
}
