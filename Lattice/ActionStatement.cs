using System.Collections.Immutable;

namespace Lattice.Core;

public sealed record ActionStatement : IControlledStatement
{
    public ActionStatement(string toolId, IEnumerable<ActionArgument> arguments, SourceSpan span)
    {
        if (string.IsNullOrWhiteSpace(toolId))
        {
            throw new ArgumentException("Tool id must not be empty.", nameof(toolId));
        }

        ArgumentNullException.ThrowIfNull(arguments);
        ToolId = toolId;
        Arguments = [.. arguments];
        Span = span;
    }

    public string ToolId { get; }

    public ImmutableArray<ActionArgument> Arguments { get; }

    public SourceSpan Span { get; }

    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct argument sets as unequal.
    public bool Equals(ActionStatement? other)
    {
        if (other is null)
        {
            return false;
        }

        return ToolId == other.ToolId
            && Span == other.Span
            && Arguments.SequenceEqual(other.Arguments);
    }

    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(ToolId);
        hash.Add(Span);
        foreach (ActionArgument argument in Arguments)
        {
            hash.Add(argument);
        }

        return hash.ToHashCode();
    }
}