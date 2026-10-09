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
        var first = new Error("code", "message");
        var second = new Error("code", "message");
        Assert.Equal(first, second);
    }
    [Fact]
    public void ToStringIncludesCodeAndMessage()
    {
        var error = new Error("code", "message");
        Assert.Equal("code: message", error.ToString());
    }
}