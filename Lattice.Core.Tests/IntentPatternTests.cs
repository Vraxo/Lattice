using System.Collections.Immutable;

namespace Lattice.Core.Tests;

public sealed class IntentPatternTests
{
    [Fact]
    public void ParsesTemplateWithTrailingSlot()
    {
        Assert.True(IntentPattern.TryParseTemplate("what is {topic}", out ImmutableArray<string> literals, out string? slot, out _));
        Assert.Equal(new[] { "what", "is" }, literals);
        Assert.Equal("topic", slot);
    }

    [Fact]
    public void ParsesTemplateWithNoSlot()
    {
        Assert.True(IntentPattern.TryParseTemplate("hello world", out ImmutableArray<string> literals, out string? slot, out _));
        Assert.Equal(new[] { "hello", "world" }, literals);
        Assert.Null(slot);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{topic}")]
    [InlineData("{a} is {b}")]
    [InlineData("is {a} true")]
    [InlineData("what is {}")]
    [InlineData("what is {9bad}")]
    public void RejectsInvalidTemplates(string template)
    {
        Assert.False(IntentPattern.TryParseTemplate(template, out _, out _, out _));
    }

    [Fact]
    public void RejectsUnknownIntentInConstructor()
    {
        Assert.Throws<ArgumentException>(() => new IntentPattern(
            "p",
            RequestIntentKind.Unknown,
            ["x"],
            null));
    }

    [Fact]
    public void PatternsWithSameContentAreEqual()
    {
        IntentPattern first = new("p", RequestIntentKind.Question, ["what", "is"], "topic");
        IntentPattern second = new("p", RequestIntentKind.Question, ["what", "is"], "topic");
        Assert.Equal(first, second);
    }

    [Fact]
    public void ParsesTemplateWithLeadingAndTrailingLiteralsAroundSingleSlot()
    {
        Assert.True(IntentPattern.TryParseTemplate(
            "can you find where {symbol} is used throughout this project",
            out ImmutableArray<string> prefixLiterals,
            out string? slotName,
            out ImmutableArray<string> suffixLiterals,
            out _));

        Assert.Equal<IEnumerable<string>>(["can", "you", "find", "where"], prefixLiterals);
        Assert.Equal("symbol", slotName);
        Assert.Equal<IEnumerable<string>>(["is", "used", "throughout", "this", "project"], suffixLiterals);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{symbol}")]
    [InlineData("{symbol} appear")]
    [InlineData("{a} is {b}")]
    [InlineData("where does {a} or {b} appear")]
    [InlineData("where does {} appear")]
    [InlineData("where does {9bad} appear")]
    public void RejectsMalformedSurroundedSlotTemplates(string template)
    {
        Assert.False(IntentPattern.TryParseTemplate(template, out _, out _, out _, out _));
    }

    [Fact]
    public void RejectsSuffixLiteralsWithoutSlot()
    {
        Assert.Throws<ArgumentException>(() => new IntentPattern(
            "invalid",
            RequestIntentKind.FindSymbol,
            ["where"],
            null,
            ["appear"]));
    }

    [Fact]
    public void PatternsDifferWhenSuffixLiteralsDiffer()
    {
        IntentPattern first = new("p", RequestIntentKind.FindSymbol, ["where", "is"], "symbol", ["used"]);
        IntentPattern second = new("p", RequestIntentKind.FindSymbol, ["where", "is"], "symbol", ["defined"]);
        IntentPattern same = new("p", RequestIntentKind.FindSymbol, ["where", "is"], "symbol", ["used"]);

        Assert.NotEqual(first, second);
        Assert.Equal(first, same);
        Assert.Equal(first.GetHashCode(), same.GetHashCode());
        Assert.Equal(3, first.LiteralCount);
    }
}