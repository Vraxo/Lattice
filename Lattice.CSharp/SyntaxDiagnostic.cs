namespace Lattice.CSharp;
public sealed record SyntaxDiagnostic(
    string Id,
    SyntaxDiagnosticSeverity Severity,
    int Line,
    int Column,
    string Message)
{
    public override string ToString() =>
        $"{Id} {Severity.ToString().ToLowerInvariant()} {Line}:{Column} {Message}";
}