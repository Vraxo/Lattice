namespace Lattice.Core.Tests;

public sealed class IdentifierTests
{
    [Fact]
    public void NewProducesNonEmptyDistinctValues()
    {
        SessionId first = SessionId.New();
        SessionId second = SessionId.New();
        first.Value.Should().NotBe(Guid.Empty);
        second.Should().NotBe(first);
    }

    [Fact]
    public void IdentifiersWithSameValueAreEqual()
    {
        Guid value = Guid.NewGuid();
        new FactId(value).Should().Be(new FactId(value));
        new GoalId(value).Should().Be(new GoalId(value));
        new ActionId(value).Should().Be(new ActionId(value));
    }

    [Fact]
    public void SameBackingValueInDifferentIdentifierTypesAreNotEqual()
    {
        Guid value = Guid.NewGuid();
        ((object)new FactId(value)).Should().NotBe(new SessionId(value));
    }
}