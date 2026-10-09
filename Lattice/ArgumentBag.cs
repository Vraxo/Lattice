using System.Collections.Immutable;

namespace Lattice.Core;

public sealed record ArgumentBag
{
    public static ArgumentBag Empty { get; } = new([]);

    private ArgumentBag(ImmutableArray<ArgumentEntry> entries)
    {
        Entries = entries;
    }

    public ImmutableArray<ArgumentEntry> Entries { get; }

    public static ArgumentBag From(IEnumerable<ArgumentEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ImmutableArray<ArgumentEntry> sorted = [.. entries.OrderBy(entry => entry.Name, StringComparer.Ordinal)];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (ArgumentEntry entry in sorted)
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
        foreach (ArgumentEntry entry in Entries)
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
    public bool Equals(ArgumentBag? other)
    {
        return other is not null && Entries.SequenceEqual(other.Entries);
    }

    public override int GetHashCode()
    {
        HashCode hash = default;
        foreach (ArgumentEntry entry in Entries)
        {
            hash.Add(entry);
        }

        return hash.ToHashCode();
    }
}