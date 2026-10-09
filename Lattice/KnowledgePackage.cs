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
        Concepts = concepts?.ToImmutableArray() ?? [];
        Facts = facts?.ToImmutableArray() ?? [];
        Rules = rules?.ToImmutableArray() ?? [];
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
        HashCode hash = default;
        hash.Add(SchemaVersion);
        hash.Add(Id);
        hash.Add(Name);
        hash.Add(Description);
        foreach (KnowledgeConcept concept in Concepts)
        {
            hash.Add(concept);
        }

        foreach (KnowledgeFact fact in Facts)
        {
            hash.Add(fact);
        }

        foreach (KnowledgeRule rule in Rules)
        {
            hash.Add(rule);
        }

        return hash.ToHashCode();
    }
}