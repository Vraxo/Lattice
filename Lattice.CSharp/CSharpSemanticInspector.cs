using System.Collections.Immutable;
using Lattice.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Lattice.CSharp;

/// <summary>
/// Compiles C# source against a reference set and reports semantic diagnostics and declared
/// symbols. When no references are supplied, the running runtime's platform assemblies are used.
/// Project files, MSBuild, and NuGet resolution are deliberately out of scope.
/// </summary>
public static class CSharpSemanticInspector
{
    public const int MaxDiagnostics = 100;
    public const int MaxDeclarations = 200;
    private static readonly ImmutableArray<string> PlatformReferencePaths = LoadPlatformReferencePaths();

    public static Result<SemanticInspection> Inspect(string source, string? nameFilter = null)
    {
        if (source is null)
        {
            return Result<SemanticInspection>.Failure(
                new Error(CSharpErrorCodes.SourceRequired, "Source text is required."));
        }

        return Inspect([new CSharpSourceFile("source.cs", source)], null, nameFilter);
    }

    /// <summary>
    /// Compiles all supplied files as one compilation. <paramref name="referencePaths"/> names the
    /// metadata references to use; when null, the host runtime's platform assemblies are used.
    /// An empty or unusable reference set is reported as an inspector failure, never as
    /// diagnostics about the supplied source.
    /// </summary>
    /// <returns></returns>
    public static Result<SemanticInspection> Inspect(
        IEnumerable<CSharpSourceFile> files,
        IEnumerable<string>? referencePaths = null,
        string? nameFilter = null)
    {
        if (files is null)
        {
            return Result<SemanticInspection>.Failure(
                new Error(CSharpErrorCodes.SourceRequired, "Source files are required."));
        }

        ImmutableArray<CSharpSourceFile> fileList = [.. files];
        if (fileList.IsEmpty)
        {
            return Result<SemanticInspection>.Failure(
                new Error(CSharpErrorCodes.SourceRequired, "At least one source file is required."));
        }

        Result<ImmutableArray<MetadataReference>> references = ResolveReferences(referencePaths);
        if (!references.IsSuccess)
        {
            return Result<SemanticInspection>.Failure(references.Error!);
        }

        ImmutableArray<SyntaxTree> trees = [.. fileList.Select(file => CSharpSyntaxTree.ParseText(file.Text, path: file.Path))];
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "Lattice.Inspected",
            syntaxTrees: trees,
            references: references.Value,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        (ImmutableArray<SyntaxDiagnostic> diagnostics, bool hasError) = CollectDiagnostics(compilation);
        ImmutableArray<SemanticSymbol> declarations = CollectDeclarations(trees, compilation, nameFilter);
        return Result<SemanticInspection>.Success(
            new SemanticInspection(!hasError, diagnostics, declarations));
    }

    private static Result<ImmutableArray<MetadataReference>> ResolveReferences(
        IEnumerable<string>? referencePaths)
    {
        ImmutableArray<string> paths = referencePaths is null
            ? PlatformReferencePaths
            : [.. referencePaths];
        if (paths.IsEmpty)
        {
            return Result<ImmutableArray<MetadataReference>>.Failure(new Error(
                CSharpErrorCodes.ReferencesUnavailable,
                "No metadata references were supplied, and no platform references are available."));
        }

        ImmutableArray<MetadataReference>.Builder builder = ImmutableArray.CreateBuilder<MetadataReference>();
        foreach (string path in paths)
        {
            if (!File.Exists(path))
            {
                return Result<ImmutableArray<MetadataReference>>.Failure(new Error(
                    CSharpErrorCodes.ReferencesUnavailable,
                    $"Reference '{path}' does not exist."));
            }

            try
            {
                builder.Add(MetadataReference.CreateFromFile(path));
            }
            catch (Exception exception) when (exception is IOException or BadImageFormatException)
            {
                return Result<ImmutableArray<MetadataReference>>.Failure(new Error(
                    CSharpErrorCodes.ReferencesUnavailable,
                    $"Reference '{path}' could not be loaded: {exception.Message}"));
            }
        }

        return Result<ImmutableArray<MetadataReference>>.Success(builder.ToImmutable());
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
        ImmutableArray<SyntaxTree> trees,
        Compilation compilation,
        string? nameFilter)
    {
        ImmutableArray<SemanticSymbol>.Builder builder = ImmutableArray.CreateBuilder<SemanticSymbol>();
        foreach (SyntaxTree tree in trees)
        {
            if (builder.Count >= MaxDeclarations)
            {
                break;
            }

            SemanticModel model = compilation.GetSemanticModel(tree);
            string path = tree.FilePath;
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
                    path,
                    symbol.Name,
                    symbol.Kind.ToString(),
                    position.Line + 1,
                    position.Character + 1,
                    TypeOf(symbol)));
            }
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

    private static ImmutableArray<string> LoadPlatformReferencePaths()
    {
        string? trusted = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (string.IsNullOrEmpty(trusted))
        {
            return [];
        }

        return
        [
            .. trusted
                .Split(Path.PathSeparator)
                .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)),
        ];
    }
}