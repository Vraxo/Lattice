using System.Collections.Immutable;
namespace Lattice.Core;
public sealed record KnowledgeRule
{
    public KnowledgeRule(
        string id,
        string description,
        IEnumerable<KnowledgePattern> premises,
        KnowledgePattern conclusion)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Rule id must not be empty.", nameof(id));
        }
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Rule description must not be empty.", nameof(description));
        }
        ArgumentNullException.ThrowIfNull(premises);
        ArgumentNullException.ThrowIfNull(conclusion);
        var premiseArray = premises.ToImmutableArray();
        if (premiseArray.IsEmpty)
        {
            throw new ArgumentException("A rule must have at least one premise.", nameof(premises));
        }
        Id = id;
        Description = description;
        Premises = premiseArray;
        Conclusion = conclusion;
    }
    public string Id { get; }
    public string Description { get; }
    public ImmutableArray<KnowledgePattern> Premises { get; }
    public KnowledgePattern Conclusion { get; }
    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct premise sets as unequal.
    public bool Equals(KnowledgeRule? other)
    {
        if (other is null)
        {
            return false;
        }
        return Id == other.Id
            && Description == other.Description
            && Conclusion == other.Conclusion
            && Premises.SequenceEqual(other.Premises);
    }
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id);
        hash.Add(Description);
        hash.Add(Conclusion);
        foreach (var premise in Premises)
        {
            hash.Add(premise);
        }
        return hash.ToHashCode();
    }
}