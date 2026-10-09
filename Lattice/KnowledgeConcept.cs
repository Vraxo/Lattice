using System.Collections.Immutable;
namespace Lattice.Core;
public sealed record KnowledgeConcept
{
    public KnowledgeConcept(
        string id,
        string preferred,
        IEnumerable<string>? aliases = null,
        string? definition = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Concept id must not be empty.", nameof(id));
        }
        if (string.IsNullOrWhiteSpace(preferred))
        {
            throw new ArgumentException("Concept preferred name must not be empty.", nameof(preferred));
        }
        Id = id;
        Preferred = preferred;
        Aliases = aliases?.ToImmutableArray() ?? ImmutableArray<string>.Empty;
        Definition = definition;
    }
    public string Id { get; }
    public string Preferred { get; }
    public ImmutableArray<string> Aliases { get; }
    public string? Definition { get; }
    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct alias sets as unequal.
    public bool Equals(KnowledgeConcept? other)
    {
        if (other is null)
        {
            return false;
        }
        return Id == other.Id
            && Preferred == other.Preferred
            && Definition == other.Definition
            && Aliases.SequenceEqual(other.Aliases);
    }
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id);
        hash.Add(Preferred);
        hash.Add(Definition);
        foreach (var alias in Aliases)
        {
            hash.Add(alias);
        }
        return hash.ToHashCode();
    }
}