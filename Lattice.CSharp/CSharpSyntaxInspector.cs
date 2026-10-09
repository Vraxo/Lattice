using System.Collections.Immutable;
using Lattice.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
namespace Lattice.CSharp;
/// <summary>
/// Parses C# source with Roslyn and reports syntax diagnostics. This inspects syntax only;
/// symbol and semantic analysis require a compilation and belong to a later step.
/// </summary>
public static class CSharpSyntaxInspector
{
    public const int MaxDiagnostics = 100;
    public static Result<SyntaxInspection> Inspect(string source)
    {
        if (source is null)
        {
            return Result<SyntaxInspection>.Failure(
                new Error(CSharpErrorCodes.SourceRequired, "Source text is required."));
        }
        var tree = CSharpSyntaxTree.ParseText(source);
        var diagnostics = ImmutableArray.CreateBuilder<SyntaxDiagnostic>();
        var hasError = false;
        // Every diagnostic is examined for errors; only the collected list is capped. Capping
        // the scan itself could report "valid" for source whose errors sit past the cap.
        foreach (var diagnostic in tree.GetDiagnostics())
        {
            var severity = diagnostic.Severity switch
            {
                DiagnosticSeverity.Error => SyntaxDiagnosticSeverity.Error,
                DiagnosticSeverity.Warning => SyntaxDiagnosticSeverity.Warning,
                _ => (SyntaxDiagnosticSeverity?)null,
            };
            if (severity is null)
            {
                continue;
            }
            if (severity == SyntaxDiagnosticSeverity.Error)
            {
                hasError = true;
            }
            if (diagnostics.Count >= MaxDiagnostics)
            {
                continue;
            }
            var position = diagnostic.Location.GetLineSpan().StartLinePosition;
            diagnostics.Add(new SyntaxDiagnostic(
                diagnostic.Id,
                severity.Value,
                position.Line + 1,
                position.Character + 1,
                diagnostic.GetMessage()));
        }
        return Result<SyntaxInspection>.Success(
            new SyntaxInspection(!hasError, diagnostics.ToImmutable()));
    }
}