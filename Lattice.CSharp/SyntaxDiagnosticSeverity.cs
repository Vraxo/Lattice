namespace Lattice.CSharp;
/// <summary>
/// Diagnostic severities surfaced by the syntax inspector. Deliberately a local enum rather
/// than Roslyn's, so this assembly's public surface does not leak compiler types.
/// </summary>
public enum SyntaxDiagnosticSeverity
{
    Warning,
    Error,
}