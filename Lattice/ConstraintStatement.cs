namespace Lattice.Core;

public sealed record ConstraintStatement : IControlledStatement
{
    public ConstraintStatement(string description, SourceSpan span)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Constraint description must not be empty.", nameof(description));
        }

        Description = description;
        Span = span;
    }

    public string Description { get; }

    public SourceSpan Span { get; }
}