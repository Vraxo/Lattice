using System.Text;
using Lattice.Core;

namespace Lattice.CSharp;

/// <summary>
/// Exposes <see cref="CSharpSyntaxInspector"/> as a read-only capability. The tool takes source
/// text rather than a path, so it does not depend on the filesystem; an agent that wants to
/// inspect a file reads it first and passes the contents.
/// </summary>
public sealed class CSharpSyntaxTool : ITool
{
    public const string SourceParameter = "source";

    public static ToolId Id { get; } = new("csharp.syntax");

    public ToolDescriptor Descriptor { get; } = new(
        Id,
        "Reports C# syntax diagnostics for the supplied source text.",
        "A validity flag followed by one line per diagnostic: id, severity, line:column, message.",
        ToolSideEffect.ReadOnly,
        [new ToolParameter(SourceParameter, ToolParameterType.String)]);

    public ToolResult Execute(ArgumentBag arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (!arguments.TryGetValue(SourceParameter, out ArgumentValue? value)
            || value.Type != ToolParameterType.String)
        {
            return ToolResult.Failure(
                new Error(CSharpErrorCodes.SourceRequired, "Argument 'source' must be a string."));
        }

        Result<SyntaxInspection> inspected = CSharpSyntaxInspector.Inspect(value.AsString());
        if (!inspected.IsSuccess)
        {
            return ToolResult.Failure(inspected.Error!);
        }

        return ToolResult.Success(ArgumentValue.FromString(Format(inspected.Value)));
    }

    private static string Format(SyntaxInspection inspection)
    {
        StringBuilder builder = new();
        builder.Append(inspection.IsValid ? "valid" : "invalid");
        foreach (SyntaxDiagnostic diagnostic in inspection.Diagnostics)
        {
            builder.Append('\n').Append(diagnostic);
        }

        return builder.ToString();
    }
}