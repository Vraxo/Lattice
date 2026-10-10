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
}