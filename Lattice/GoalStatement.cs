namespace Lattice.Core;

public sealed record GoalStatement : IControlledStatement
{
    public GoalStatement(string description, string completion, SourceSpan span)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Goal description must not be empty.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(completion))
        {
            throw new ArgumentException("Goal completion must not be empty.", nameof(completion));
        }

        Description = description;
        Completion = completion;
        Span = span;
    }

    public string Description { get; }

    public string Completion { get; }

    public SourceSpan Span { get; }
}