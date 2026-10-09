namespace Lattice.Core;

public sealed record ArgumentEntry
{
    public ArgumentEntry(string name, ArgumentValue value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Argument name must not be empty.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(value);
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public ArgumentValue Value { get; }
}