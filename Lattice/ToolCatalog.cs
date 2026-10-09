using System.Collections.Immutable;
namespace Lattice.Core;
public sealed record ToolCatalog
{
    public ToolCatalog(IEnumerable<ToolDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        var array = descriptors.ToImmutableArray();
        var seen = new HashSet<ToolId>();
        foreach (var descriptor in array)
        {
            if (!seen.Add(descriptor.Id))
            {
                throw new ArgumentException($"Duplicate tool id: {descriptor.Id.Value}", nameof(descriptors));
            }
        }
        Descriptors = array;
    }
    public ImmutableArray<ToolDescriptor> Descriptors { get; }
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
        var hash = new HashCode();
        foreach (var descriptor in Descriptors)
        {
            hash.Add(descriptor);
        }
        return hash.ToHashCode();
    }
}