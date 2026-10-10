using Lattice.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace Lattice.CSharp;
/// <summary>
/// Applies one narrow, syntax-aware transformation: adding the <c>sealed</c> modifier to a named
/// class declaration. The tree is edited through Roslyn and re-emitted, so untouched regions keep
/// their original text and trivia.
/// </summary>
public static class CSharpSyntaxEditor
{
    public static CSharpEdit AddSealedModifier(string source, string className)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(className);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
        SyntaxNode root = tree.GetRoot();
        ClassDeclarationSyntax[] matches =
        [
            .. root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(candidate => string.Equals(candidate.Identifier.ValueText, className, StringComparison.Ordinal)),
        ];
        if (matches.Length == 0)
        {
            return CSharpEdit.Rejected(
                CSharpEditStatus.TypeNotFound,
                source,
                $"No class named '{className}' was found.");
        }
        if (matches.Length > 1)
        {
            return CSharpEdit.Rejected(
                CSharpEditStatus.AmbiguousType,
                source,
                $"More than one class named '{className}' was found.");
        }
        ClassDeclarationSyntax declaration = matches[0];
        if (declaration.Modifiers.Any(SyntaxKind.SealedKeyword))
        {
            return CSharpEdit.Rejected(
                CSharpEditStatus.AlreadyApplied,
                source,
                $"Class '{className}' is already sealed.");
        }
        SyntaxToken sealedToken = SyntaxFactory.Token(SyntaxKind.SealedKeyword)
            .WithTrailingTrivia(SyntaxFactory.Space);
        ClassDeclarationSyntax updated = declaration.AddModifiers(sealedToken);
        SyntaxNode newRoot = root.ReplaceNode(declaration, updated);
        string patchedText = newRoot.ToFullString();
        // Syntax-only validation would almost never fail: Roslyn normalizes syntax trees, so
        // AST edits do not produce malformed syntax. Semantic validation is what catches
        // "parses but would not compile" (for example abstract + sealed).
        Result<SemanticInspection> validation = CSharpSemanticInspector.Inspect(patchedText);
        if (!validation.IsSuccess || !validation.Value.IsValid)
        {
            return CSharpEdit.Rejected(
                CSharpEditStatus.OutputInvalid,
                source,
                "The edited source would not compile.");
        }
        return CSharpEdit.Applied(source, patchedText, $"Sealed class '{className}'.");
    }
}