namespace Lattice.Core.Tests;

public sealed class UnifiedDiffTests
{
    [Fact]
    public void IdenticalTextsProduceEmptyDiff()
    {
        UnifiedDiff.Create("a\nb", "a\nb").Should().Be(string.Empty);
    }

    [Fact]
    public void SingleLineChangeProducesOneHunk()
    {
        string diff = UnifiedDiff.Create("a\nb\nc", "a\nX\nc");
        diff.Should().Contain("@@ -1,3 +1,3 @@");
        diff.Should().Contain("-b");
        diff.Should().Contain("+X");
        diff.Should().Contain(" a");
        diff.Should().Contain(" c");
    }

    [Fact]
    public void AddedLineAppearsAsAddition()
    {
        string diff = UnifiedDiff.Create("a\nc", "a\nb\nc");
        diff.Should().Contain("+b");
    }

    [Fact]
    public void RemovedLineAppearsAsDeletion()
    {
        string diff = UnifiedDiff.Create("a\nb\nc", "a\nc");
        diff.Should().Contain("-b");
    }

    [Fact]
    public void ContextIsLimitedAroundChange()
    {
        string original = string.Join('\n', Enumerable.Range(1, 20).Select(i => $"line{i}"));
        string patched = original.Replace("line10", "CHANGED", StringComparison.Ordinal);
        string diff = UnifiedDiff.Create(original, patched, contextLines: 1);
        diff.Should().Contain("line9");
        diff.Should().Contain("line11");
        diff.Should().NotContain("line5");
        diff.Should().NotContain("line15");
    }

    [Fact]
    public void CarriageReturnsAreTrimmedFromDisplay()
    {
        string diff = UnifiedDiff.Create("a\r\nb", "a\r\nX");
        diff.Should().NotContain("\r");
    }

    [Fact]
    public void NullInputsThrow()
    {
        FluentActions.Invoking(() => UnifiedDiff.Create(null!, "a")).Should().ThrowExactly<ArgumentNullException>();
        FluentActions.Invoking(() => UnifiedDiff.Create("a", null!)).Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void NegativeContextThrows()
    {
        FluentActions.Invoking(() => UnifiedDiff.Create("a", "b", -1)).Should().ThrowExactly<ArgumentOutOfRangeException>();
    }
}