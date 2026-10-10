namespace Lattice.Core;
/// <summary>
/// One structured diagnostic extracted from build or test output. The raw output is preserved
/// separately; this is the machine-readable view of it.
/// </summary>
public sealed record VerificationDiagnostic(
    VerificationSeverity Severity,
    string File,
    int Line,
    int Column,
    string Code,
    string Message)
{
    public override string ToString()
    {
        return $"{File}({Line},{Column}): {(Severity == VerificationSeverity.Error ? "error" : "warning")} {Code}: {Message}";
    }
}