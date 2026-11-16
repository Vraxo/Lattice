namespace Lattice.Core.Tests;

public sealed class ArgumentBagTests
{
    [Fact]
    public void EmptyHasNoEntries()
    {
        ArgumentBag.Empty.Entries.Should().BeEmpty();
    }

    [Fact]
    public void FromOrdersEntriesByName()
    {
        ArgumentBag bag = ArgumentBag.From(
        [
            new ArgumentEntry("b", ArgumentValue.FromInteger(2)),
            new ArgumentEntry("a", ArgumentValue.FromInteger(1)),
        ]);
        bag.Entries.Select(entry => entry.Name).Should().Equal(["a", "b"]);
    }

    [Fact]
    public void BagsWithSameEntriesInDifferentOrderAreEqual()
    {
        ArgumentBag first = ArgumentBag.From(
        [
            new ArgumentEntry("a", ArgumentValue.FromInteger(1)),
            new ArgumentEntry("b", ArgumentValue.FromInteger(2)),
        ]);
        ArgumentBag second = ArgumentBag.From(
        [
            new ArgumentEntry("b", ArgumentValue.FromInteger(2)),
            new ArgumentEntry("a", ArgumentValue.FromInteger(1)),
        ]);
        second.Should().Be(first);
    }

    [Fact]
    public void FromRejectsDuplicateNames()
    {
        ArgumentEntry[] entries =
        [
            new ArgumentEntry("a", ArgumentValue.FromInteger(1)),
            new ArgumentEntry("a", ArgumentValue.FromInteger(2)),
        ];
        FluentActions.Invoking(() => ArgumentBag.From(entries)).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void AddAppendsEntry()
    {
        ArgumentBag bag = ArgumentBag.Empty.Add(new ArgumentEntry("a", ArgumentValue.FromInteger(1)));
        bag.Entries.Should().ContainSingle();
    }

    [Fact]
    public void TryGetValueFindsExistingEntry()
    {
        ArgumentBag bag = ArgumentBag.From([new ArgumentEntry("a", ArgumentValue.FromInteger(1))]);
        bag.TryGetValue("a", out ArgumentValue? value).Should().BeTrue();
        value.Should().Be(ArgumentValue.FromInteger(1));
    }

    [Fact]
    public void TryGetValueReturnsFalseForMissingEntry()
    {
        ArgumentBag.Empty.TryGetValue("a", out _).Should().BeFalse();
    }
}