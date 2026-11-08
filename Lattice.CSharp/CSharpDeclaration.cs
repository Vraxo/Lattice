namespace Lattice.CSharp;

/// <summary>
/// A C# declaration discovered from source syntax. This is structural information only; it does
/// not claim that the containing project compiles or that references bind to this declaration.
/// </summary>
public sealed record CSharpDeclaration(
    string RelativePath,
    string Name,
    string Kind,
    int Line,
    int Column,
    string Signature,
    string Namespace,
    string? ContainingType);
