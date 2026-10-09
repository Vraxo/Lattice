using System.Collections.Immutable;
namespace Lattice.Core;
public sealed record IntentPatternCatalog
{
    public IntentPatternCatalog(IEnumerable<IntentPattern> patterns)
    {
        ArgumentNullException.ThrowIfNull(patterns);
        var array = patterns.ToImmutableArray();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pattern in array)
        {
            if (!seen.Add(pattern.Id))
            {
                throw new ArgumentException($"Duplicate pattern id: {pattern.Id}", nameof(patterns));
            }
        }
        Patterns = array;
    }
    public ImmutableArray<IntentPattern> Patterns { get; }
    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct pattern sets as unequal.
    public bool Equals(IntentPatternCatalog? other) =>
        other is not null && Patterns.SequenceEqual(other.Patterns);
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var pattern in Patterns)
        {
            hash.Add(pattern);
        }
        return hash.ToHashCode();
    }
}