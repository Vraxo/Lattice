namespace Lattice.Core;

public sealed record KnowledgePattern
{
    public KnowledgePattern(string subject, string predicate, string value)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Pattern subject must not be empty.", nameof(subject));
        }

        if (string.IsNullOrWhiteSpace(predicate))
        {
            throw new ArgumentException("Pattern predicate must not be empty.", nameof(predicate));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Pattern value must not be empty.", nameof(value));
        }

        Subject = subject;
        Predicate = predicate;
        Value = value;
    }

    public string Subject { get; }

    public string Predicate { get; }

    public string Value { get; }
}