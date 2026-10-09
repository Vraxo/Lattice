namespace Lattice.Core.Tests;

public sealed class ErrorTests
{
    [Fact]
    public void RejectsEmptyCode()
    {
        Assert.Throws<ArgumentException>(() => new Error(string.Empty, "message"));
    }

    [Fact]
    public void RejectsEmptyMessage()
    {
        Assert.Throws<ArgumentException>(() => new Error("code", string.Empty));
    }

    [Fact]
    public void HasValueEqualityByContent()
    {
        Error first = new("code", "message");
        Error second = new("code", "message");
        Assert.Equal(first, second);
    }

    [Fact]
    public void ToStringIncludesCodeAndMessage()
    {
        Error error = new("code", "message");
        Assert.Equal("code: message", error.ToString());
    }
}