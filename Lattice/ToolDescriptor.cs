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
        ToolExecutionLimits? limits = null,
        IEnumerable<string>? aliases = null,
        IEnumerable<string>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Tool description must not be empty.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(outputDescription))
        {
            throw new ArgumentException("Output description must not be empty.", nameof(outputDescription));
        }

        ImmutableArray<ToolParameter> parameterArray = parameters?.ToImmutableArray() ?? [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (ToolParameter? parameter in parameterArray)
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
        Aliases = Normalize(aliases, nameof(aliases));
        Tags = Normalize(tags, nameof(tags));
    }

    public ToolId Id { get; }

    public string Description { get; }

    public string OutputDescription { get; }

    public ToolSideEffect SideEffect { get; }

    public ImmutableArray<ToolParameter> Parameters { get; }

    public ToolExecutionLimits Limits { get; }

    /// <summary>Gets alternative names this capability answers to during discovery.</summary>
    public ImmutableArray<string> Aliases { get; }

    /// <summary>Gets free-form classification labels used by discovery.</summary>
    public ImmutableArray<string> Tags { get; }

    private static ImmutableArray<string> Normalize(IEnumerable<string>? values, string parameterName)
    {
        // ImmutableArray<T> is a struct, so a default value is not null and would throw when
        // enumerated. Treat default and empty alike.
        if (values is null)
        {
            return [];
        }

        if (values is ImmutableArray<string> arrayValue && arrayValue.IsDefault)
        {
            return [];
        }

        ImmutableArray<string> array = [.. values];
        foreach (string value in array)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Alias and tag values must not be empty.", parameterName);
            }
        }

        return array;
    }

    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct collections as unequal.
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
            && Parameters.SequenceEqual(other.Parameters)
            && Aliases.SequenceEqual(other.Aliases)
            && Tags.SequenceEqual(other.Tags);
    }

    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(Id);
        hash.Add(Description);
        hash.Add(OutputDescription);
        hash.Add(SideEffect);
        hash.Add(Limits);
        foreach (ToolParameter parameter in Parameters)
        {
            hash.Add(parameter);
        }

        foreach (string alias in Aliases)
        {
            hash.Add(alias);
        }

        foreach (string tag in Tags)
        {
            hash.Add(tag);
        }

        return hash.ToHashCode();
    }
}