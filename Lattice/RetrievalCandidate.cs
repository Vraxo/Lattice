using System.Collections.Immutable;

namespace Lattice.Core;
/// <summary>
/// A ranked retrieval result. Carries the concept, the total score, the factor breakdown that
/// produced it, the text that matched, and the source it came from. Scores are ranking values, not
/// probabilities; only their relative order is meaningful.
/// </summary>
public sealed record RetrievalCandidate
{
    public RetrievalCandidate(
        KnowledgeConcept concept,
        string packageId,
        IEnumerable<RetrievalFactor> factors,
        string matchedField,
        string matchedText)
    {
        ArgumentNullException.ThrowIfNull(concept);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        ArgumentNullException.ThrowIfNull(factors);
        ArgumentException.ThrowIfNullOrWhiteSpace(matchedField);
        ArgumentNullException.ThrowIfNull(matchedText);
        Concept = concept;
        PackageId = packageId;
        Factors = [.. factors];
        MatchedField = matchedField;
        MatchedText = matchedText;
    }

    public KnowledgeConcept Concept { get; }

    public string PackageId { get; }

    public ImmutableArray<RetrievalFactor> Factors { get; }

    /// <summary>Gets which concept field produced the best textual match (for example "definition").</summary>
    public string MatchedField { get; }

    /// <summary>Gets the text of <see cref="MatchedField"/>, retained to explain the match.</summary>
    public string MatchedText { get; }

    public double Score => Factors.Sum(factor => factor.Contribution);

    /// <summary>Gets stable provenance: the package and concept the candidate came from.</summary>
    public string Source => $"{PackageId}:{Concept.Id}";

    // ImmutableArray<T> compares by reference of its backing array, so the record's synthesized
    // equality would treat equal-but-distinct factor lists as unequal.
    public bool Equals(RetrievalCandidate? other)
    {
        if (other is null)
        {
            return false;
        }

        return Concept == other.Concept
            && PackageId == other.PackageId
            && MatchedField == other.MatchedField
            && MatchedText == other.MatchedText
            && Factors.SequenceEqual(other.Factors);
    }

    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(Concept);
        hash.Add(PackageId);
        hash.Add(MatchedField);
        hash.Add(MatchedText);
        foreach (RetrievalFactor factor in Factors)
        {
            hash.Add(factor);
        }

        return hash.ToHashCode();
    }
}