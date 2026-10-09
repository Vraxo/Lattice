namespace Lattice.Core.Tests;

public sealed class IdentifierTests
{
    [Fact]
    public void NewProducesNonEmptyDistinctValues()
    {
        SessionId first = SessionId.New();
        SessionId second = SessionId.New();
        Assert.NotEqual(Guid.Empty, first.Value);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void IdentifiersWithSameValueAreEqual()
    {
        Guid value = Guid.NewGuid();
        Assert.Equal(new FactId(value), new FactId(value));
        Assert.Equal(new GoalId(value), new GoalId(value));
        Assert.Equal(new ActionId(value), new ActionId(value));
    }

    [Fact]
    public void SameBackingValueInDifferentIdentifierTypesAreNotEqual()
    {
        Guid value = Guid.NewGuid();
        Assert.NotEqual<object>(new SessionId(value), new FactId(value));
    }
}