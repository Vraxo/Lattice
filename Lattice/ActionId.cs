namespace Lattice.Core;

public readonly record struct ActionId(Guid Value)
{
    public static ActionId New()
    {
        return new(Guid.NewGuid());
    }
}