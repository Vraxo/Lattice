namespace Lattice.Core;

public sealed record ActionArgument
{
    public ActionArgument(string name, ToolParameterType type, string value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Argument name must not be empty.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Argument value must not be empty.", nameof(value));
        }

        Name = name;
        Type = type;
        Value = value;
    }

    public string Name { get; }

    public ToolParameterType Type { get; }

    public string Value { get; }
}