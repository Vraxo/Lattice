namespace Lattice.Core.Tests;

public sealed class ArgumentBagTests
{
    [Fact]
    public void EmptyHasNoEntries()
    {
        Assert.Empty(ArgumentBag.Empty.Entries);
    }

    [Fact]
    public void FromOrdersEntriesByName()
    {
        ArgumentBag bag = ArgumentBag.From(
        [
            new ArgumentEntry("b", ArgumentValue.FromInteger(2)),
            new ArgumentEntry("a", ArgumentValue.FromInteger(1)),
        ]);
        Assert.Equal(["a", "b"], bag.Entries.Select(entry => entry.Name));
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
        Assert.Equal(first, second);
    }

    [Fact]
    public void FromRejectsDuplicateNames()
    {
        ArgumentEntry[] entries =
        [
            new ArgumentEntry("a", ArgumentValue.FromInteger(1)),
            new ArgumentEntry("a", ArgumentValue.FromInteger(2)),
        ];
        Assert.Throws<ArgumentException>(() => ArgumentBag.From(entries));
    }

    [Fact]
    public void AddAppendsEntry()
    {
        ArgumentBag bag = ArgumentBag.Empty.Add(new ArgumentEntry("a", ArgumentValue.FromInteger(1)));
        Assert.Single(bag.Entries);
    }

    [Fact]
    public void TryGetValueFindsExistingEntry()
    {
        ArgumentBag bag = ArgumentBag.From([new ArgumentEntry("a", ArgumentValue.FromInteger(1))]);
        Assert.True(bag.TryGetValue("a", out ArgumentValue? value));
        Assert.Equal(ArgumentValue.FromInteger(1), value);
    }

    [Fact]
    public void TryGetValueReturnsFalseForMissingEntry()
    {
        Assert.False(ArgumentBag.Empty.TryGetValue("a", out _));
    }
}