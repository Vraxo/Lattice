using System.Collections.Immutable;
namespace Lattice.Core.Tests;
public sealed class IntentPatternTests
{
    [Fact]
    public void ParsesTemplateWithTrailingSlot()
    {
        Assert.True(IntentPattern.TryParseTemplate("what is {topic}", out var literals, out var slot, out _));
        Assert.Equal(new[] { "what", "is" }, literals);
        Assert.Equal("topic", slot);
    }
    [Fact]
    public void ParsesTemplateWithNoSlot()
    {
        Assert.True(IntentPattern.TryParseTemplate("hello world", out var literals, out var slot, out _));
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
            ImmutableArray.Create("x"),
            null));
    }
    [Fact]
    public void PatternsWithSameContentAreEqual()
    {
        var first = new IntentPattern("p", RequestIntentKind.Question, ImmutableArray.Create("what", "is"), "topic");
        var second = new IntentPattern("p", RequestIntentKind.Question, ImmutableArray.Create("what", "is"), "topic");
        Assert.Equal(first, second);
    }
}