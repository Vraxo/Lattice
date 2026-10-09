namespace Lattice.Core.Tests;

public sealed class FactTests
{
    [Fact]
    public void FactsWithSameContentAndDifferentSourceAreNotEqual()
    {
        FactId id = FactId.New();
        Fact userFact = new(id, "Water boils at 100C.", "user:turn-1", FactStatus.UserAsserted);
        Fact packageFact = new(id, "Water boils at 100C.", "package:physics", FactStatus.Retrieved);
        Assert.NotEqual(userFact, packageFact);
    }

    [Fact]
    public void FactsWithSameContentAndDifferentStatusAreNotEqual()
    {
        FactId id = FactId.New();
        Fact asserted = new(id, "Water boils at 100C.", "user:turn-1", FactStatus.UserAsserted);
        Fact assumed = new(id, "Water boils at 100C.", "user:turn-1", FactStatus.Assumed);
        Assert.NotEqual(asserted, assumed);
    }

    [Fact]
    public void FactsWithAllComponentsEqualAreEqual()
    {
        FactId id = FactId.New();
        Fact first = new(id, "Water boils at 100C.", "user:turn-1", FactStatus.UserAsserted);
        Fact second = new(id, "Water boils at 100C.", "user:turn-1", FactStatus.UserAsserted);
        Assert.Equal(first, second);
    }

    [Fact]
    public void RejectsEmptyContent()
    {
        Assert.Throws<ArgumentException>(
            () => new Fact(FactId.New(), string.Empty, "user:turn-1", FactStatus.UserAsserted));
    }

    [Fact]
    public void RejectsEmptySource()
    {
        Assert.Throws<ArgumentException>(
            () => new Fact(FactId.New(), "content", string.Empty, FactStatus.UserAsserted));
    }
}