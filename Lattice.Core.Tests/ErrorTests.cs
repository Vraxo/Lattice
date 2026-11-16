namespace Lattice.Core.Tests;

public sealed class ErrorTests
{
    [Fact]
    public void RejectsEmptyCode()
    {
        FluentActions.Invoking(() => new Error(string.Empty, "message")).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void RejectsEmptyMessage()
    {
        FluentActions.Invoking(() => new Error("code", string.Empty)).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void HasValueEqualityByContent()
    {
        Error first = new("code", "message");
        Error second = new("code", "message");
        second.Should().Be(first);
    }

    [Fact]
    public void ToStringIncludesCodeAndMessage()
    {
        Error error = new("code", "message");
        error.ToString().Should().Be("code: message");
    }
}