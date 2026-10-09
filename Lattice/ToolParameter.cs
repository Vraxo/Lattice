namespace Lattice.Core;

public sealed record ToolParameter
{
    public ToolParameter(string name, ToolParameterType type, bool required = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Parameter name must not be empty.", nameof(name));
        }

        Name = name;
        Type = type;
        Required = required;
    }

    public string Name { get; }

    public ToolParameterType Type { get; }

    public bool Required { get; }
}