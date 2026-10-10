using System.Collections.Immutable;

namespace Lattice.Core;

/// <summary>Maps tool identifiers to executable implementations.</summary>
public sealed class ToolRegistry
{
    public ToolRegistry(IEnumerable<ITool> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);
        ImmutableArray<ITool> array = [.. tools];
        HashSet<ToolId> seen = [];
        foreach (ITool tool in array)
        {
            if (!seen.Add(tool.Descriptor.Id))
            {
                throw new ArgumentException($"Duplicate tool id: {tool.Descriptor.Id.Value}", nameof(tools));
            }
        }

        Tools = array;
    }

    public ImmutableArray<ITool> Tools { get; }

    public bool TryResolve(ToolId id, out ITool tool)
    {
        foreach (ITool candidate in Tools)
        {
            if (candidate.Descriptor.Id == id)
            {
                tool = candidate;
                return true;
            }
        }

        tool = null!;
        return false;
    }

    public ToolCatalog ToCatalog()
    {
        return new(Tools.Select(tool => tool.Descriptor));
    }
}