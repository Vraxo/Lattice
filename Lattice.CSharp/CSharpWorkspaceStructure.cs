using System.Collections.Immutable;

namespace Lattice.CSharp;

/// <summary>
/// Bounded syntax declarations collected from a connected workspace.
/// </summary>
public sealed record CSharpWorkspaceStructure(
    ImmutableArray<CSharpDeclaration> Declarations,
    int SourceFilesRead,
    int SourceFilesSkipped,
    int SyntaxErrorFiles,
    bool IsTruncated)
{
    public ImmutableArray<CSharpDeclaration> SafeDeclarations =>
        Declarations.IsDefault ? [] : Declarations;
}
