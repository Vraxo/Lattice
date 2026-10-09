using System.Collections.Immutable;
namespace Lattice.Core;
public sealed record ArgumentBag
{
    public static ArgumentBag Empty { get; } = new(ImmutableArray<ArgumentEntry>.Empty);
    private ArgumentBag(ImmutableArray<ArgumentEntry> entries)
    {
        Entries = entries;
    }
    public ImmutableArray<ArgumentEntry> Entries { get; }
    public static ArgumentBag From(IEnumerable<ArgumentEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var sorted = entries.OrderBy(entry => entry.Name, StringComparer.Ordinal).ToImmutableArray();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in sorted)
        {
            if (!seen.Add(entry.Name))
            {
                throw new ArgumentException($"Duplicate argument name: {entry.Name}", nameof(entries));
            }
        }
        return new(sorted);
    }
    public ArgumentBag Add(ArgumentEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return From(Entries.Add(entry));
    }
    public bool TryGetValue(string name, out ArgumentValue value)
    {
        foreach (var entry in Entries)
        {
            if (string.Equals(entry.Name, name, StringComparison.Ordinal))
            {
                value = entry.Value;
                return true;
            }
        }
        value = null!;
        return false;
    }
    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct argument sets as unequal.
    public bool Equals(ArgumentBag? other) =>
        other is not null && Entries.SequenceEqual(other.Entries);
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var entry in Entries)
        {
            hash.Add(entry);
        }
        return hash.ToHashCode();
    }
}