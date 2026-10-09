namespace Lattice.Core;
public sealed record Constraint
{
    public Constraint(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Constraint description must not be empty.", nameof(description));
        }
        Description = description;
    }
    public string Description { get; }
}