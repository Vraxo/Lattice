using System.Collections.Immutable;

namespace Lattice.CSharp;
/// <summary>
/// The result of analysing C# source with a compilation. <see cref="IsValid"/> is false when any
/// error-severity diagnostic is present. <see cref="Declarations"/> lists the symbols found,
/// optionally narrowed by a name filter.
/// </summary>
public sealed record SemanticInspection
{
    public SemanticInspection(
        bool isValid,
        ImmutableArray<SyntaxDiagnostic> diagnostics,
        ImmutableArray<SemanticSymbol> declarations)
    {
        IsValid = isValid;
        Diagnostics = diagnostics;
        Declarations = declarations;
    }

    public bool IsValid { get; }

    public ImmutableArray<SyntaxDiagnostic> Diagnostics { get; }

    public ImmutableArray<SemanticSymbol> Declarations { get; }

    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct sets as unequal.
    public bool Equals(SemanticInspection? other)
    {
        return other is not null
        && IsValid == other.IsValid
        && Diagnostics.SequenceEqual(other.Diagnostics)
        && Declarations.SequenceEqual(other.Declarations);
    }

    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(IsValid);
        foreach (SyntaxDiagnostic diagnostic in Diagnostics)
        {
            hash.Add(diagnostic);
        }

        foreach (SemanticSymbol declaration in Declarations)
        {
            hash.Add(declaration);
        }

        return hash.ToHashCode();
    }
}