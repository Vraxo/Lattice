namespace Lattice.Core;

public sealed record FactStatement : IControlledStatement
{
    public FactStatement(string subject, string predicate, string value, SourceSpan span)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Fact subject must not be empty.", nameof(subject));
        }

        if (string.IsNullOrWhiteSpace(predicate))
        {
            throw new ArgumentException("Fact predicate must not be empty.", nameof(predicate));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Fact value must not be empty.", nameof(value));
        }

        Subject = subject;
        Predicate = predicate;
        Value = value;
        Span = span;
    }

    public string Subject { get; }

    public string Predicate { get; }

    public string Value { get; }

    public SourceSpan Span { get; }
}