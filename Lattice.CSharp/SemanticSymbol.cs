namespace Lattice.CSharp;
/// <summary>A declaration found in one of the supplied source files.</summary>
public sealed record SemanticSymbol(
    string Path,
    string Name,
    string Kind,
    int Line,
    int Column,
    string? TypeName)
{
    public override string ToString()
    {
        return TypeName is null
            ? $"{Path}:{Line}:{Column} {Kind} {Name}"
            : $"{Path}:{Line}:{Column} {Kind} {Name} : {TypeName}";
    }
}