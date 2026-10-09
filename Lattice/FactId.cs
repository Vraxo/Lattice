namespace Lattice.Core;

public readonly record struct FactId(Guid Value)
{
    public static FactId New()
    {
        return new(Guid.NewGuid());
    }
}