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
        var bag = ArgumentBag.From(new[]
        {
            new ArgumentEntry("b", ArgumentValue.FromInteger(2)),
            new ArgumentEntry("a", ArgumentValue.FromInteger(1)),
        });
        Assert.Equal(new[] { "a", "b" }, bag.Entries.Select(entry => entry.Name));
    }
    [Fact]
    public void BagsWithSameEntriesInDifferentOrderAreEqual()
    {
        var first = ArgumentBag.From(new[]
        {
            new ArgumentEntry("a", ArgumentValue.FromInteger(1)),
            new ArgumentEntry("b", ArgumentValue.FromInteger(2)),
        });
        var second = ArgumentBag.From(new[]
        {
            new ArgumentEntry("b", ArgumentValue.FromInteger(2)),
            new ArgumentEntry("a", ArgumentValue.FromInteger(1)),
        });
        Assert.Equal(first, second);
    }
    [Fact]
    public void FromRejectsDuplicateNames()
    {
        var entries = new[]
        {
            new ArgumentEntry("a", ArgumentValue.FromInteger(1)),
            new ArgumentEntry("a", ArgumentValue.FromInteger(2)),
        };
        Assert.Throws<ArgumentException>(() => ArgumentBag.From(entries));
    }
    [Fact]
    public void AddAppendsEntry()
    {
        var bag = ArgumentBag.Empty.Add(new ArgumentEntry("a", ArgumentValue.FromInteger(1)));
        Assert.Single(bag.Entries);
    }
    [Fact]
    public void TryGetValueFindsExistingEntry()
    {
        var bag = ArgumentBag.From(new[] { new ArgumentEntry("a", ArgumentValue.FromInteger(1)) });
        Assert.True(bag.TryGetValue("a", out var value));
        Assert.Equal(ArgumentValue.FromInteger(1), value);
    }
    [Fact]
    public void TryGetValueReturnsFalseForMissingEntry()
    {
        Assert.False(ArgumentBag.Empty.TryGetValue("a", out _));
    }
}