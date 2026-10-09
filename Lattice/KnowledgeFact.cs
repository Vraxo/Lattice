namespace Lattice.Core;

public sealed record KnowledgeFact
{
    public KnowledgeFact(string id, string subject, string predicate, string value, string? source = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Fact id must not be empty.", nameof(id));
        }

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

        Id = id;
        Subject = subject;
        Predicate = predicate;
        Value = value;
        Source = source;
    }

    public string Id { get; }

    public string Subject { get; }

    public string Predicate { get; }

    public string Value { get; }

    public string? Source { get; }
}