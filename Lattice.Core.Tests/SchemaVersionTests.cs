namespace Lattice.Core.Tests;

public sealed class SchemaVersionTests
{
    [Fact]
    public void ParsesMajorAndMinor()
    {
        SchemaVersion.TryParse("1.0", out SchemaVersion version).Should().BeTrue();
        version.Major.Should().Be(1);
        version.Minor.Should().Be(0);
    }

    [Fact]
    public void ParsesMultiDigitMinor()
    {
        SchemaVersion.TryParse("1.12", out SchemaVersion version).Should().BeTrue();
        version.Minor.Should().Be(12);
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
        SchemaVersion.TryParse(text, out _).Should().BeFalse();
    }

    [Fact]
    public void ToStringRoundTrips()
    {
        new SchemaVersion(1, 0).ToString().Should().Be("1.0");
    }
}