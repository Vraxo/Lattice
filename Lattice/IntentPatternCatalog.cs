using System.Collections.Immutable;

namespace Lattice.Core;

public sealed record IntentPatternCatalog
{
    public IntentPatternCatalog(IEnumerable<IntentPattern> patterns)
    {
        ArgumentNullException.ThrowIfNull(patterns);
        ImmutableArray<IntentPattern> array = [.. patterns];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (IntentPattern pattern in array)
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
    public bool Equals(IntentPatternCatalog? other)
    {
        return other is not null && Patterns.SequenceEqual(other.Patterns);
    }

    public override int GetHashCode()
    {
        HashCode hash = default;
        foreach (IntentPattern pattern in Patterns)
        {
            hash.Add(pattern);
        }

        return hash.ToHashCode();
    }
}