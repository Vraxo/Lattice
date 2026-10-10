namespace Lattice.CSharp;
/// <summary>A declaration found in the source, with its kind, position, and type where known.</summary>
public sealed record SemanticSymbol(
    string Name,
    string Kind,
    int Line,
    int Column,
    string? TypeName)
{
    public override string ToString()
    {
        return TypeName is null
            ? $"{Kind} {Name} {Line}:{Column}"
            : $"{Kind} {Name} {Line}:{Column} : {TypeName}";
    }
}