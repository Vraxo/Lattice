using System.Collections.Immutable;
namespace Lattice.Core;
/// <summary>Maps tool identifiers to executable implementations.</summary>
public sealed class ToolRegistry
{
    private readonly ImmutableArray<ITool> _tools;
    public ToolRegistry(IEnumerable<ITool> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);
        var array = tools.ToImmutableArray();
        var seen = new HashSet<ToolId>();
        foreach (var tool in array)
        {
            if (!seen.Add(tool.Descriptor.Id))
            {
                throw new ArgumentException($"Duplicate tool id: {tool.Descriptor.Id.Value}", nameof(tools));
            }
        }
        _tools = array;
    }
    public ImmutableArray<ITool> Tools => _tools;
    public bool TryResolve(ToolId id, out ITool tool)
    {
        foreach (var candidate in _tools)
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
    public ToolCatalog ToCatalog() => new(_tools.Select(tool => tool.Descriptor));
}