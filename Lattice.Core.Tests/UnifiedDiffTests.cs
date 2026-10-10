namespace Lattice.Core.Tests;

public sealed class UnifiedDiffTests
{
    [Fact]
    public void IdenticalTextsProduceEmptyDiff()
    {
        Assert.Equal(string.Empty, UnifiedDiff.Create("a\nb", "a\nb"));
    }

    [Fact]
    public void SingleLineChangeProducesOneHunk()
    {
        string diff = UnifiedDiff.Create("a\nb\nc", "a\nX\nc");
        Assert.Contains("@@ -1,3 +1,3 @@", diff);
        Assert.Contains("-b", diff);
        Assert.Contains("+X", diff);
        Assert.Contains(" a", diff);
        Assert.Contains(" c", diff);
    }

    [Fact]
    public void AddedLineAppearsAsAddition()
    {
        string diff = UnifiedDiff.Create("a\nc", "a\nb\nc");
        Assert.Contains("+b", diff);
    }

    [Fact]
    public void RemovedLineAppearsAsDeletion()
    {
        string diff = UnifiedDiff.Create("a\nb\nc", "a\nc");
        Assert.Contains("-b", diff);
    }

    [Fact]
    public void ContextIsLimitedAroundChange()
    {
        string original = string.Join('\n', Enumerable.Range(1, 20).Select(i => $"line{i}"));
        string patched = original.Replace("line10", "CHANGED", StringComparison.Ordinal);
        string diff = UnifiedDiff.Create(original, patched, contextLines: 1);
        Assert.Contains("line9", diff);
        Assert.Contains("line11", diff);
        Assert.DoesNotContain("line5", diff);
        Assert.DoesNotContain("line15", diff);
    }

    [Fact]
    public void CarriageReturnsAreTrimmedFromDisplay()
    {
        string diff = UnifiedDiff.Create("a\r\nb", "a\r\nX");
        Assert.DoesNotContain("\r", diff);
    }

    [Fact]
    public void NullInputsThrow()
    {
        Assert.Throws<ArgumentNullException>(() => UnifiedDiff.Create(null!, "a"));
        Assert.Throws<ArgumentNullException>(() => UnifiedDiff.Create("a", null!));
    }

    [Fact]
    public void NegativeContextThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UnifiedDiff.Create("a", "b", -1));
    }
}