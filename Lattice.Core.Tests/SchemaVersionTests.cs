namespace Lattice.Core.Tests;

public sealed class SchemaVersionTests
{
    [Fact]
    public void ParsesMajorAndMinor()
    {
        Assert.True(SchemaVersion.TryParse("1.0", out SchemaVersion version));
        Assert.Equal(1, version.Major);
        Assert.Equal(0, version.Minor);
    }

    [Fact]
    public void ParsesMultiDigitMinor()
    {
        Assert.True(SchemaVersion.TryParse("1.12", out SchemaVersion version));
        Assert.Equal(12, version.Minor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("1.2.3")]
    [InlineData("a.b")]
    [InlineData("1.")]
    [InlineData(".1")]
    [InlineData("-1.0")]
    public void RejectsMalformedVersions(string text)
    {
        Assert.False(SchemaVersion.TryParse(text, out _));
    }

    [Fact]
    public void ToStringRoundTrips()
    {
        Assert.Equal("1.0", new SchemaVersion(1, 0).ToString());
    }
}