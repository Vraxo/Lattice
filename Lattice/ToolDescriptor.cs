using System.Collections.Immutable;
namespace Lattice.Core;
public sealed record ToolDescriptor
{
    public ToolDescriptor(
        ToolId id,
        string description,
        string outputDescription,
        ToolSideEffect sideEffect,
        IEnumerable<ToolParameter>? parameters = null,
        ToolExecutionLimits? limits = null)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Tool description must not be empty.", nameof(description));
        }
        if (string.IsNullOrWhiteSpace(outputDescription))
        {
            throw new ArgumentException("Output description must not be empty.", nameof(outputDescription));
        }
        var parameterArray = parameters?.ToImmutableArray() ?? ImmutableArray<ToolParameter>.Empty;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var parameter in parameterArray)
        {
            if (!seen.Add(parameter.Name))
            {
                throw new ArgumentException($"Duplicate parameter name: {parameter.Name}", nameof(parameters));
            }
        }
        Id = id;
        Description = description;
        OutputDescription = outputDescription;
        SideEffect = sideEffect;
        Parameters = parameterArray;
        Limits = limits ?? ToolExecutionLimits.Default;
    }
    public ToolId Id { get; }
    public string Description { get; }
    public string OutputDescription { get; }
    public ToolSideEffect SideEffect { get; }
    public ImmutableArray<ToolParameter> Parameters { get; }
    public ToolExecutionLimits Limits { get; }
    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct parameter sets as unequal.
    public bool Equals(ToolDescriptor? other)
    {
        if (other is null)
        {
            return false;
        }
        return Id == other.Id
            && Description == other.Description
            && OutputDescription == other.OutputDescription
            && SideEffect == other.SideEffect
            && Limits == other.Limits
            && Parameters.SequenceEqual(other.Parameters);
    }
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id);
        hash.Add(Description);
        hash.Add(OutputDescription);
        hash.Add(SideEffect);
        hash.Add(Limits);
        foreach (var parameter in Parameters)
        {
            hash.Add(parameter);
        }
        return hash.ToHashCode();
    }
}