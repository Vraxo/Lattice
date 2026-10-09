namespace Lattice.Core;
public sealed record Fact
{
    public Fact(FactId id, string content, string source, FactStatus status)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Fact content must not be empty.", nameof(content));
        }
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Fact source must not be empty.", nameof(source));
        }
        Id = id;
        Content = content;
        Source = source;
        Status = status;
    }
    public FactId Id { get; }
    public string Content { get; }
    public string Source { get; }
    public FactStatus Status { get; }
}