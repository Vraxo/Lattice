namespace Lattice.Core.Tests;
public sealed class FactTests
{
    [Fact]
    public void FactsWithSameContentAndDifferentSourceAreNotEqual()
    {
        var id = FactId.New();
        var userFact = new Fact(id, "Water boils at 100C.", "user:turn-1", FactStatus.UserAsserted);
        var packageFact = new Fact(id, "Water boils at 100C.", "package:physics", FactStatus.Retrieved);
        Assert.NotEqual(userFact, packageFact);
    }
    [Fact]
    public void FactsWithSameContentAndDifferentStatusAreNotEqual()
    {
        var id = FactId.New();
        var asserted = new Fact(id, "Water boils at 100C.", "user:turn-1", FactStatus.UserAsserted);
        var assumed = new Fact(id, "Water boils at 100C.", "user:turn-1", FactStatus.Assumed);
        Assert.NotEqual(asserted, assumed);
    }
    [Fact]
    public void FactsWithAllComponentsEqualAreEqual()
    {
        var id = FactId.New();
        var first = new Fact(id, "Water boils at 100C.", "user:turn-1", FactStatus.UserAsserted);
        var second = new Fact(id, "Water boils at 100C.", "user:turn-1", FactStatus.UserAsserted);
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