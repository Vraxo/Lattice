using System.Collections.Immutable;
using Lattice.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Lattice.CSharp;

/// <summary>
/// Compiles C# source against the running runtime's platform assemblies and reports semantic
/// diagnostics and declared symbols. References come from the host runtime rather than a package,
/// so no additional dependency is introduced.
/// </summary>
public static class CSharpSemanticInspector
{
    public const int MaxDiagnostics = 100;
    public const int MaxDeclarations = 200;
    private static readonly ImmutableArray<MetadataReference> DefaultReferences = LoadDefaultReferences();

    public static Result<SemanticInspection> Inspect(string source, string? nameFilter = null)
    {
        if (source is null)
        {
            return Result<SemanticInspection>.Failure(
                new Error(CSharpErrorCodes.SourceRequired, "Source text is required."));
        }

        SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "Lattice.Inspected",
            syntaxTrees: [tree],
            references: DefaultReferences,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        SemanticModel model = compilation.GetSemanticModel(tree);
        (ImmutableArray<SyntaxDiagnostic> diagnostics, bool hasError) = CollectDiagnostics(compilation);
        ImmutableArray<SemanticSymbol> declarations = CollectDeclarations(tree, model, nameFilter);
        return Result<SemanticInspection>.Success(
            new SemanticInspection(!hasError, diagnostics, declarations));
    }

    private static (ImmutableArray<SyntaxDiagnostic> Diagnostics, bool HasError) CollectDiagnostics(
        Compilation compilation)
    {
        ImmutableArray<SyntaxDiagnostic>.Builder builder = ImmutableArray.CreateBuilder<SyntaxDiagnostic>();
        bool hasError = false;

        // Every diagnostic is examined for errors; only the collected list is capped.
        foreach (Diagnostic diagnostic in compilation.GetDiagnostics())
        {
            SyntaxDiagnosticSeverity? severity = diagnostic.Severity switch
            {
                DiagnosticSeverity.Error => SyntaxDiagnosticSeverity.Error,
                DiagnosticSeverity.Warning => SyntaxDiagnosticSeverity.Warning,
                _ => null,
            };
            if (severity is null)
            {
                continue;
            }

            if (severity == SyntaxDiagnosticSeverity.Error)
            {
                hasError = true;
            }

            if (builder.Count >= MaxDiagnostics)
            {
                continue;
            }

            LinePosition position = diagnostic.Location.GetLineSpan().StartLinePosition;
            builder.Add(new SyntaxDiagnostic(
                diagnostic.Id,
                severity.Value,
                position.Line + 1,
                position.Character + 1,
                diagnostic.GetMessage()));
        }

        return (builder.ToImmutable(), hasError);
    }

    private static ImmutableArray<SemanticSymbol> CollectDeclarations(
        SyntaxTree tree,
        SemanticModel model,
        string? nameFilter)
    {
        ImmutableArray<SemanticSymbol>.Builder builder = ImmutableArray.CreateBuilder<SemanticSymbol>();
        foreach (SyntaxNode node in tree.GetRoot().DescendantNodes())
        {
            if (builder.Count >= MaxDeclarations)
            {
                break;
            }

            ISymbol? symbol = model.GetDeclaredSymbol(node);
            if (symbol is null)
            {
                continue;
            }

            if (nameFilter is not null
                && !string.Equals(symbol.Name, nameFilter, StringComparison.Ordinal))
            {
                continue;
            }

            Location? location = symbol.Locations.FirstOrDefault();
            if (location is null)
            {
                continue;
            }

            LinePosition position = location.GetLineSpan().StartLinePosition;
            builder.Add(new SemanticSymbol(
                symbol.Name,
                symbol.Kind.ToString(),
                position.Line + 1,
                position.Character + 1,
                TypeOf(symbol)));
        }

        return builder.ToImmutable();
    }

    private static string? TypeOf(ISymbol symbol)
    {
        return symbol switch
        {
            ILocalSymbol local => local.Type.ToDisplayString(),
            IFieldSymbol field => field.Type.ToDisplayString(),
            IPropertySymbol property => property.Type.ToDisplayString(),
            IParameterSymbol parameter => parameter.Type.ToDisplayString(),
            IMethodSymbol method => method.ReturnType.ToDisplayString(),
            _ => null,
        };
    }

    private static ImmutableArray<MetadataReference> LoadDefaultReferences()
    {
        string? trusted = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (string.IsNullOrEmpty(trusted))
        {
            return [];
        }

        ImmutableArray<MetadataReference>.Builder builder = ImmutableArray.CreateBuilder<MetadataReference>();
        foreach (string path in trusted.Split(Path.PathSeparator))
        {
            if (!path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                builder.Add(MetadataReference.CreateFromFile(path));
            }
            catch (Exception exception) when (exception is IOException or BadImageFormatException)
            {
                // Not every platform assembly can be loaded as metadata; skip it.
            }
        }

        return builder.ToImmutable();
    }
}