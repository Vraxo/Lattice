using System.Collections.Immutable;
namespace Lattice.Core;
public sealed record ConceptLookup
{
    private ConceptLookup(
        KnowledgeMatchKind kind,
        string packageId,
        KnowledgeConcept? concept,
        ImmutableArray<KnowledgeConcept> candidates)
    {
        Kind = kind;
        PackageId = packageId;
        Concept = concept;
        Candidates = candidates;
    }
    public KnowledgeMatchKind Kind { get; }
    public string PackageId { get; }
    public KnowledgeConcept? Concept { get; }
    public ImmutableArray<KnowledgeConcept> Candidates { get; }
    public bool IsFound => Kind == KnowledgeMatchKind.Found;
    public static ConceptLookup Found(string packageId, KnowledgeConcept concept)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        ArgumentNullException.ThrowIfNull(concept);
        return new ConceptLookup(KnowledgeMatchKind.Found, packageId, concept, ImmutableArray<KnowledgeConcept>.Empty);
    }
    public static ConceptLookup NotFound(string packageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        return new ConceptLookup(KnowledgeMatchKind.NotFound, packageId, null, ImmutableArray<KnowledgeConcept>.Empty);
    }
    public static ConceptLookup Ambiguous(string packageId, IEnumerable<KnowledgeConcept> candidates)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        ArgumentNullException.ThrowIfNull(candidates);
        var array = candidates.ToImmutableArray();
        if (array.Length < 2)
        {
            throw new ArgumentException("An ambiguous lookup requires at least two candidates.", nameof(candidates));
        }
        return new ConceptLookup(KnowledgeMatchKind.Ambiguous, packageId, null, array);
    }
    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct candidate sets as unequal.
    public bool Equals(ConceptLookup? other)
    {
        if (other is null)
        {
            return false;
        }
        return Kind == other.Kind
            && PackageId == other.PackageId
            && Concept == other.Concept
            && Candidates.SequenceEqual(other.Candidates);
    }
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Kind);
        hash.Add(PackageId);
        hash.Add(Concept);
        foreach (var candidate in Candidates)
        {
            hash.Add(candidate);
        }
        return hash.ToHashCode();
    }
}