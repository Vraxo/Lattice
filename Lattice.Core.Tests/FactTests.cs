namespace Lattice.Core.Tests;

public sealed class FactTests
{
    [Fact]
    public void FactsWithSameContentAndDifferentSourceAreNotEqual()
    {
        FactId id = FactId.New();
        Fact userFact = new(id, "Water boils at 100C.", "user:turn-1", FactStatus.UserAsserted);
        Fact packageFact = new(id, "Water boils at 100C.", "package:physics", FactStatus.Retrieved);
        packageFact.Should().NotBe(userFact);
    }

    [Fact]
    public void FactsWithSameContentAndDifferentStatusAreNotEqual()
    {
        FactId id = FactId.New();
        Fact asserted = new(id, "Water boils at 100C.", "user:turn-1", FactStatus.UserAsserted);
        Fact assumed = new(id, "Water boils at 100C.", "user:turn-1", FactStatus.Assumed);
        assumed.Should().NotBe(asserted);
    }

    [Fact]
    public void FactsWithAllComponentsEqualAreEqual()
    {
        FactId id = FactId.New();
        Fact first = new(id, "Water boils at 100C.", "user:turn-1", FactStatus.UserAsserted);
        Fact second = new(id, "Water boils at 100C.", "user:turn-1", FactStatus.UserAsserted);
        second.Should().Be(first);
    }

    [Fact]
    public void RejectsEmptyContent()
    {
        FluentActions.Invoking(() => new Fact(FactId.New(), string.Empty, "user:turn-1", FactStatus.UserAsserted)).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void RejectsEmptySource()
    {
        FluentActions.Invoking(() => new Fact(FactId.New(), "content", string.Empty, FactStatus.UserAsserted)).Should().ThrowExactly<ArgumentException>();
    }
}