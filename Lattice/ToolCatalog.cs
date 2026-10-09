using System.Collections.Immutable;

namespace Lattice.Core;

public sealed record ToolCatalog
{
    public ToolCatalog(IEnumerable<ToolDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        ImmutableArray<ToolDescriptor> array = [.. descriptors];
        HashSet<ToolId> seen = [];
        foreach (ToolDescriptor descriptor in array)
        {
            if (!seen.Add(descriptor.Id))
            {
                throw new ArgumentException($"Duplicate tool id: {descriptor.Id.Value}", nameof(descriptors));
            }
        }

        Descriptors = array;
    }

    public ImmutableArray<ToolDescriptor> Descriptors { get; }

    public bool TryResolve(ToolId id, out ToolDescriptor descriptor)
    {
        foreach (ToolDescriptor candidate in Descriptors)
        {
            if (candidate.Id == id)
            {
                descriptor = candidate;
                return true;
            }
        }

        descriptor = null!;
        return false;
    }

    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct descriptor sets as unequal.
    public bool Equals(ToolCatalog? other)
    {
        if (other is null)
        {
            return false;
        }

        return Descriptors.SequenceEqual(other.Descriptors);
    }

    public override int GetHashCode()
    {
        HashCode hash = default;
        foreach (ToolDescriptor descriptor in Descriptors)
        {
            hash.Add(descriptor);
        }

        return hash.ToHashCode();
    }
}