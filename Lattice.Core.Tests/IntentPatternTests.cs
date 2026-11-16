using System.Collections.Immutable;

namespace Lattice.Core.Tests;

public sealed class IntentPatternTests
{
    [Fact]
    public void ParsesTemplateWithTrailingSlot()
    {
        IntentPattern.TryParseTemplate("what is {topic}", out ImmutableArray<string> literals, out string? slot, out _).Should().BeTrue();
        literals.Should().Equal(new[] { "what", "is" });
        slot.Should().Be("topic");
    }

    [Fact]
    public void ParsesTemplateWithNoSlot()
    {
        IntentPattern.TryParseTemplate("hello world", out ImmutableArray<string> literals, out string? slot, out _).Should().BeTrue();
        literals.Should().Equal(new[] { "hello", "world" });
        slot.Should().BeNull();
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
        IntentPattern.TryParseTemplate(template, out _, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void RejectsUnknownIntentInConstructor()
    {
        FluentActions.Invoking(() => new IntentPattern(
            "p",
            RequestIntentKind.Unknown,
            ["x"],
            null)).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void PatternsWithSameContentAreEqual()
    {
        IntentPattern first = new("p", RequestIntentKind.Question, ["what", "is"], "topic");
        IntentPattern second = new("p", RequestIntentKind.Question, ["what", "is"], "topic");
        second.Should().Be(first);
    }

    [Fact]
    public void ParsesTemplateWithLeadingAndTrailingLiteralsAroundSingleSlot()
    {
        IntentPattern.TryParseTemplate(
            "can you find where {symbol} is used throughout this project",
            out ImmutableArray<string> prefixLiterals,
            out string? slotName,
            out ImmutableArray<string> suffixLiterals,
            out _).Should().BeTrue();

        prefixLiterals.Should().Equal(["can", "you", "find", "where"]);
        slotName.Should().Be("symbol");
        suffixLiterals.Should().Equal(["is", "used", "throughout", "this", "project"]);
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
        IntentPattern.TryParseTemplate(template, out _, out _, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void RejectsSuffixLiteralsWithoutSlot()
    {
        FluentActions.Invoking(() => new IntentPattern(
            "invalid",
            RequestIntentKind.FindSymbol,
            ["where"],
            null,
            ["appear"])).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void PatternsDifferWhenSuffixLiteralsDiffer()
    {
        IntentPattern first = new("p", RequestIntentKind.FindSymbol, ["where", "is"], "symbol", ["used"]);
        IntentPattern second = new("p", RequestIntentKind.FindSymbol, ["where", "is"], "symbol", ["defined"]);
        IntentPattern same = new("p", RequestIntentKind.FindSymbol, ["where", "is"], "symbol", ["used"]);

        second.Should().NotBe(first);
        same.Should().Be(first);
        same.GetHashCode().Should().Be(first.GetHashCode());
        first.LiteralCount.Should().Be(3);
    }
}