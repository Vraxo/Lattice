using System.Collections.Immutable;

namespace Lattice.CSharp;
/// <summary>
/// The result of parsing C# source. <see cref="IsValid"/> is false when any error-severity
/// diagnostic is present; warnings alone do not make source invalid.
/// </summary>
public sealed record SyntaxInspection
{
    public SyntaxInspection(bool isValid, ImmutableArray<SyntaxDiagnostic> diagnostics)
    {
        IsValid = isValid;
        Diagnostics = diagnostics;
    }

    public bool IsValid { get; }

    public ImmutableArray<SyntaxDiagnostic> Diagnostics { get; }

    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct diagnostic sets as unequal.
    public bool Equals(SyntaxInspection? other)
    {
        return other is not null
        && IsValid == other.IsValid
        && Diagnostics.SequenceEqual(other.Diagnostics);
    }

    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(IsValid);
        foreach (SyntaxDiagnostic diagnostic in Diagnostics)
        {
            hash.Add(diagnostic);
        }

        return hash.ToHashCode();
    }
}