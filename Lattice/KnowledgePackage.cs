using System.Collections.Immutable;
namespace Lattice.Core;
public sealed record KnowledgePackage
{
    public KnowledgePackage(
        SchemaVersion schemaVersion,
        string id,
        string name,
        string? description = null,
        IEnumerable<KnowledgeConcept>? concepts = null,
        IEnumerable<KnowledgeFact>? facts = null,
        IEnumerable<KnowledgeRule>? rules = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Package id must not be empty.", nameof(id));
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Package name must not be empty.", nameof(name));
        }
        SchemaVersion = schemaVersion;
        Id = id;
        Name = name;
        Description = description;
        Concepts = concepts?.ToImmutableArray() ?? ImmutableArray<KnowledgeConcept>.Empty;
        Facts = facts?.ToImmutableArray() ?? ImmutableArray<KnowledgeFact>.Empty;
        Rules = rules?.ToImmutableArray() ?? ImmutableArray<KnowledgeRule>.Empty;
    }
    public SchemaVersion SchemaVersion { get; }
    public string Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public ImmutableArray<KnowledgeConcept> Concepts { get; }
    public ImmutableArray<KnowledgeFact> Facts { get; }
    public ImmutableArray<KnowledgeRule> Rules { get; }
    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct content as unequal.
    public bool Equals(KnowledgePackage? other)
    {
        if (other is null)
        {
            return false;
        }
        return SchemaVersion == other.SchemaVersion
            && Id == other.Id
            && Name == other.Name
            && Description == other.Description
            && Concepts.SequenceEqual(other.Concepts)
            && Facts.SequenceEqual(other.Facts)
            && Rules.SequenceEqual(other.Rules);
    }
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(SchemaVersion);
        hash.Add(Id);
        hash.Add(Name);
        hash.Add(Description);
        foreach (var concept in Concepts)
        {
            hash.Add(concept);
        }
        foreach (var fact in Facts)
        {
            hash.Add(fact);
        }
        foreach (var rule in Rules)
        {
            hash.Add(rule);
        }
        return hash.ToHashCode();
    }
}