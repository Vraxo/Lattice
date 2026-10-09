namespace Lattice.Core;

public readonly record struct SessionId(Guid Value)
{
    public static SessionId New()
    {
        return new(Guid.NewGuid());
    }
}