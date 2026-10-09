using System.Collections.Immutable;

namespace Lattice.Core;

public sealed record Derivation
{
    public Derivation(string ruleId, IEnumerable<string> premiseFactIds, KnowledgeFact conclusion)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            throw new ArgumentException("Rule id must not be empty.", nameof(ruleId));
        }

        ArgumentNullException.ThrowIfNull(premiseFactIds);
        ArgumentNullException.ThrowIfNull(conclusion);
        RuleId = ruleId;
        PremiseFactIds = [.. premiseFactIds];
        Conclusion = conclusion;
    }

    public string RuleId { get; }

    public ImmutableArray<string> PremiseFactIds { get; }

    public KnowledgeFact Conclusion { get; }

    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct premise sets as unequal.
    public bool Equals(Derivation? other)
    {
        if (other is null)
        {
            return false;
        }

        return RuleId == other.RuleId
            && Conclusion == other.Conclusion
            && PremiseFactIds.SequenceEqual(other.PremiseFactIds);
    }

    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(RuleId);
        hash.Add(Conclusion);
        foreach (string premiseId in PremiseFactIds)
        {
            hash.Add(premiseId);
        }

        return hash.ToHashCode();
    }
}