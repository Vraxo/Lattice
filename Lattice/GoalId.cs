namespace Lattice.Core;

public readonly record struct GoalId(Guid Value)
{
    public static GoalId New()
    {
        return new(Guid.NewGuid());
    }
}