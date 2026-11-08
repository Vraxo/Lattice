using System.Collections.Immutable;
using System.Text;
using Lattice.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lattice.CSharp;

/// <summary>
/// Collects C# declarations from a workspace using the existing read-only directory and file
/// tools. It parses syntax trees only: it does not load project files, resolve NuGet references,
/// compile projects, or resolve symbol references.
/// </summary>
public sealed class CSharpWorkspaceStructureInspector
{
    public const int MaxDirectoriesVisited = 512;
    public const int MaxSourceFilesRead = 200;
    public const int MaxTotalSourceBytes = 8 * 1024 * 1024;
    public const int MaxDeclarations = 1_000;
    public const int MaxDirectoryDepth = 32;

    private static readonly HashSet<string> ExcludedDirectories = new(
        WorkspaceFileLocator.DefaultExcludedDirectories,
        StringComparer.OrdinalIgnoreCase);

    private readonly ListDirectoryTool _listDirectoryTool;
    private readonly ReadFileTool _readFileTool;

    public CSharpWorkspaceStructureInspector(WorkspaceRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        _listDirectoryTool = new ListDirectoryTool(root);
        _readFileTool = new ReadFileTool(root);
    }

    public Result<CSharpWorkspaceStructure> Inspect(CancellationToken cancellationToken = default)
    {
        Queue<string> directories = new();
        directories.Enqueue(".");
        ImmutableArray<CSharpDeclaration>.Builder declarations = ImmutableArray.CreateBuilder<CSharpDeclaration>();
        int directoriesVisited = 0;
        int sourceFilesRead = 0;
        int sourceFilesSkipped = 0;
        int totalSourceBytes = 0;
        int syntaxErrorFiles = 0;
        bool truncated = false;

        while (directories.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (directoriesVisited >= MaxDirectoriesVisited)
            {
                truncated = true;
                break;
            }

            string directory = directories.Dequeue();
            directoriesVisited++;

            ToolInvocation listing = ToolExecutor.Execute(
                _listDirectoryTool,
                BuildPathArguments(directory),
                ToolPermissionPolicy.ReadOnlyOnly,
                cancellationToken);
            if (!listing.Result.IsSuccess)
            {
                if (directory == ".")
                {
                    return Result<CSharpWorkspaceStructure>.Failure(listing.Result.Error!);
                }

                sourceFilesSkipped++;
                continue;
            }

            string listingText = listing.Result.Output!.AsString();
            foreach (string rawEntry in listingText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string entry = rawEntry.TrimEnd('\r');
                if (entry.StartsWith("...", StringComparison.Ordinal))
                {
                    truncated = true;
                    continue;
                }

                if (entry.EndsWith("/", StringComparison.Ordinal))
                {
                    string directoryName = entry[..^1];
                    if (ExcludedDirectories.Contains(directoryName))
                    {
                        continue;
                    }

                    string childPath = CombineRelativePath(directory, directoryName);
                    if (GetDepth(childPath) > MaxDirectoryDepth)
                    {
                        truncated = true;
                        continue;
                    }

                    directories.Enqueue(childPath);
                    continue;
                }

                if (!Path.GetExtension(entry).Equals(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (sourceFilesRead >= MaxSourceFilesRead)
                {
                    truncated = true;
                    break;
                }

                string relativePath = CombineRelativePath(directory, entry);
                ToolInvocation read = ToolExecutor.Execute(
                    _readFileTool,
                    BuildPathArguments(relativePath),
                    ToolPermissionPolicy.ReadOnlyOnly,
                    cancellationToken);
                if (!read.Result.IsSuccess)
                {
                    sourceFilesSkipped++;
                    continue;
                }

                string source = read.Result.Output!.AsString();
                int sourceBytes = Encoding.UTF8.GetByteCount(source);
                if (sourceBytes > MaxTotalSourceBytes - totalSourceBytes)
                {
                    truncated = true;
                    break;
                }

                totalSourceBytes += sourceBytes;
                sourceFilesRead++;

                SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: relativePath);
                if (tree.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
                {
                    syntaxErrorFiles++;
                }

                CollectDeclarations(tree, relativePath, declarations, ref truncated);
                if (truncated)
                {
                    break;
                }
            }

            if (truncated)
            {
                break;
            }
        }

        ImmutableArray<CSharpDeclaration> ordered = [.. declarations
            .OrderBy(declaration => declaration.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(declaration => declaration.Line)
            .ThenBy(declaration => declaration.Column)
            .ThenBy(declaration => declaration.Name, StringComparer.OrdinalIgnoreCase)];

        return Result<CSharpWorkspaceStructure>.Success(new CSharpWorkspaceStructure(
            ordered,
            sourceFilesRead,
            sourceFilesSkipped,
            syntaxErrorFiles,
            truncated));
    }

    private static void CollectDeclarations(
        SyntaxTree tree,
        string relativePath,
        ImmutableArray<CSharpDeclaration>.Builder declarations,
        ref bool truncated)
    {
        SyntaxNode root = tree.GetRoot();
        foreach (TypeDeclarationSyntax type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (!TryAddDeclaration(
                type,
                relativePath,
                type.Identifier.ValueText,
                GetTypeKind(type),
                BuildTypeSignature(type),
                declarations,
                ref truncated))
            {
                return;
            }

            foreach (MemberDeclarationSyntax member in type.Members)
            {
                if (!CollectMember(member, relativePath, declarations, ref truncated))
                {
                    return;
                }
            }
        }

        foreach (EnumDeclarationSyntax enumeration in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
        {
            if (!TryAddDeclaration(
                enumeration,
                relativePath,
                enumeration.Identifier.ValueText,
                "enum",
                $"{FormatModifiers(enumeration.Modifiers)}enum {enumeration.Identifier}",
                declarations,
                ref truncated))
            {
                return;
            }

            foreach (EnumMemberDeclarationSyntax member in enumeration.Members)
            {
                if (!TryAddDeclaration(
                    member,
                    relativePath,
                    member.Identifier.ValueText,
                    "enum member",
                    member.Identifier.Text,
                    declarations,
                    ref truncated))
                {
                    return;
                }
            }
        }

        foreach (DelegateDeclarationSyntax declaration in root.DescendantNodes().OfType<DelegateDeclarationSyntax>())
        {
            string signature = $"{FormatModifiers(declaration.Modifiers)}delegate {declaration.ReturnType} {declaration.Identifier}{declaration.TypeParameterList}{declaration.ParameterList}";
            if (!TryAddDeclaration(
                declaration,
                relativePath,
                declaration.Identifier.ValueText,
                "delegate",
                signature,
                declarations,
                ref truncated))
            {
                return;
            }
        }
    }

    private static bool CollectMember(
        MemberDeclarationSyntax member,
        string relativePath,
        ImmutableArray<CSharpDeclaration>.Builder declarations,
        ref bool truncated)
    {
        switch (member)
        {
            case MethodDeclarationSyntax method:
                return TryAddDeclaration(
                    method,
                    relativePath,
                    method.Identifier.ValueText,
                    "method",
                    $"{FormatModifiers(method.Modifiers)}{method.ReturnType} {method.Identifier}{method.TypeParameterList}{method.ParameterList}",
                    declarations,
                    ref truncated);

            case ConstructorDeclarationSyntax constructor:
                return TryAddDeclaration(
                    constructor,
                    relativePath,
                    constructor.Identifier.ValueText,
                    "constructor",
                    $"{FormatModifiers(constructor.Modifiers)}{constructor.Identifier}{constructor.ParameterList}",
                    declarations,
                    ref truncated);

            case PropertyDeclarationSyntax property:
                return TryAddDeclaration(
                    property,
                    relativePath,
                    property.Identifier.ValueText,
                    "property",
                    $"{FormatModifiers(property.Modifiers)}{property.Type} {property.Identifier}",
                    declarations,
                    ref truncated);

            case IndexerDeclarationSyntax indexer:
                return TryAddDeclaration(
                    indexer,
                    relativePath,
                    "this",
                    "indexer",
                    $"{FormatModifiers(indexer.Modifiers)}{indexer.Type} this{indexer.ParameterList}",
                    declarations,
                    ref truncated);

            case FieldDeclarationSyntax field:
                foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
                {
                    if (!TryAddDeclaration(
                        variable,
                        relativePath,
                        variable.Identifier.ValueText,
                        "field",
                        $"{FormatModifiers(field.Modifiers)}{field.Declaration.Type} {variable.Identifier}",
                        declarations,
                        ref truncated))
                    {
                        return false;
                    }
                }

                return true;

            case EventFieldDeclarationSyntax eventField:
                foreach (VariableDeclaratorSyntax variable in eventField.Declaration.Variables)
                {
                    if (!TryAddDeclaration(
                        variable,
                        relativePath,
                        variable.Identifier.ValueText,
                        "event",
                        $"{FormatModifiers(eventField.Modifiers)}event {eventField.Declaration.Type} {variable.Identifier}",
                        declarations,
                        ref truncated))
                    {
                        return false;
                    }
                }

                return true;

            case EventDeclarationSyntax eventDeclaration:
                return TryAddDeclaration(
                    eventDeclaration,
                    relativePath,
                    eventDeclaration.Identifier.ValueText,
                    "event",
                    $"{FormatModifiers(eventDeclaration.Modifiers)}event {eventDeclaration.Type} {eventDeclaration.Identifier}",
                    declarations,
                    ref truncated);

            case OperatorDeclarationSyntax operatorDeclaration:
                return TryAddDeclaration(
                    operatorDeclaration,
                    relativePath,
                    operatorDeclaration.OperatorToken.Text,
                    "operator",
                    $"{FormatModifiers(operatorDeclaration.Modifiers)}{operatorDeclaration.ReturnType} operator {operatorDeclaration.OperatorToken}{operatorDeclaration.ParameterList}",
                    declarations,
                    ref truncated);

            case DestructorDeclarationSyntax destructor:
                return TryAddDeclaration(
                    destructor,
                    relativePath,
                    destructor.Identifier.ValueText,
                    "destructor",
                    $"~{destructor.Identifier}{destructor.ParameterList}",
                    declarations,
                    ref truncated);

            default:
                return true;
        }
    }

    private static bool TryAddDeclaration(
        SyntaxNode node,
        string relativePath,
        string name,
        string kind,
        string signature,
        ImmutableArray<CSharpDeclaration>.Builder declarations,
        ref bool truncated)
    {
        if (declarations.Count >= MaxDeclarations)
        {
            truncated = true;
            return false;
        }

        Microsoft.CodeAnalysis.Text.LinePosition position = node.GetLocation().GetLineSpan().StartLinePosition;
        TypeDeclarationSyntax? containingType = node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        EnumDeclarationSyntax? containingEnum = node.Ancestors().OfType<EnumDeclarationSyntax>().FirstOrDefault();
        string? containingTypeName = containingType?.Identifier.ValueText ?? containingEnum?.Identifier.ValueText;

        string[] namespaceParts = node.AncestorsAndSelf()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Reverse()
            .Select(namespaceNode => namespaceNode.Name.ToString())
            .ToArray();

        declarations.Add(new CSharpDeclaration(
            relativePath,
            name,
            kind,
            position.Line + 1,
            position.Character + 1,
            signature.Trim(),
            string.Join('.', namespaceParts),
            containingTypeName));
        return true;
    }

    private static string GetTypeKind(TypeDeclarationSyntax type)
    {
        return type switch
        {
            RecordDeclarationSyntax => "record",
            ClassDeclarationSyntax => "class",
            StructDeclarationSyntax => "struct",
            InterfaceDeclarationSyntax => "interface",
            _ => "type",
        };
    }

    private static string BuildTypeSignature(TypeDeclarationSyntax type)
    {
        string keyword = type is RecordDeclarationSyntax record
            ? $"record {record.ClassOrStructKeyword.Text}".TrimEnd()
            : type.Keyword.Text;
        string baseList = type.BaseList?.ToString() ?? string.Empty;
        return $"{FormatModifiers(type.Modifiers)}{keyword} {type.Identifier}{type.TypeParameterList}{baseList}";
    }

    private static string FormatModifiers(SyntaxTokenList modifiers)
    {
        string value = modifiers.ToString();
        return string.IsNullOrEmpty(value) ? string.Empty : value + " ";
    }

    private static ArgumentBag BuildPathArguments(string relativePath)
    {
        return ArgumentBag.From(
        [
            new ArgumentEntry("path", ArgumentValue.FromString(relativePath)),
        ]);
    }

    private static string CombineRelativePath(string parent, string child)
    {
        return parent == "." ? child : $"{parent}/{child}";
    }

    private static int GetDepth(string relativePath)
    {
        return relativePath.Count(character => character == '/') + 1;
    }
}
