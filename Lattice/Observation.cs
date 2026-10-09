namespace Lattice.Core;
/// <summary>Raw content reported by a tool, treated as untrusted data until accepted as a fact.</summary>
public sealed record Observation
{
    public Observation(string content, string source)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Observation content must not be empty.", nameof(content));
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Observation source must not be empty.", nameof(source));
        }

        Content = content;
        Source = source;
    }

    public string Content { get; }

    public string Source { get; }
}