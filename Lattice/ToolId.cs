namespace Lattice.Core;
public readonly record struct ToolId
{
    public ToolId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Tool id must not be empty.", nameof(value));
        }
        Value = value;
    }
    public string Value { get; }
}