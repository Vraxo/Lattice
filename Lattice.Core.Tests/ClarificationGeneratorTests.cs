using System.Collections.Immutable;
namespace Lattice.Core.Tests;
public sealed class ClarificationGeneratorTests
{
    [Fact]
    public void MatchedInterpretationNeedsNoClarification()
    {
        var interpretation = RequestInterpretation.Matched(
            "what is gravity",
            new IntentMatch("question.what-is", RequestIntentKind.Question, new IntentSlot("topic", "gravity")));
        Assert.Null(ClarificationGenerator.TryCreate(interpretation));
    }
    [Fact]
    public void MissingValueAsksForTheSpecificSlot()
    {
        var interpretation = RequestInterpretation.MissingValue(
            "what is",
            new IntentMatch("question.what-is", RequestIntentKind.Question, null),
            "topic");
        var request = ClarificationGenerator.TryCreate(interpretation);
        Assert.NotNull(request);
        Assert.Equal(ClarificationKind.MissingValue, request.Kind);
        Assert.Equal("What topic?", request.Question);
    }
    [Fact]
    public void AmbiguousInterpretationNamesTheCompetingIntents()
    {
        var interpretation = RequestInterpretation.Ambiguous(
            "show README.md",
            new[]
            {
                new IntentMatch("show.inspect", RequestIntentKind.InspectPath, new IntentSlot("target", "README.md")),
                new IntentMatch("show.explain", RequestIntentKind.Explanation, new IntentSlot("target", "README.md")),
            });
        var request = ClarificationGenerator.TryCreate(interpretation);
        Assert.NotNull(request);
        Assert.Equal(ClarificationKind.AmbiguousIntent, request.Kind);
        Assert.Contains("an explanation", request.Question);
        Assert.Contains("a file inspection", request.Question);
    }
    [Fact]
    public void AmbiguousInterpretationWithOneIntentFallsBackToGenericQuestion()
    {
        var interpretation = RequestInterpretation.Ambiguous(
            "show README.md",
            new[]
            {
                new IntentMatch("a", RequestIntentKind.Explanation, new IntentSlot("target", "README.md")),
                new IntentMatch("b", RequestIntentKind.Explanation, new IntentSlot("target", "README.md")),
            });
        var request = ClarificationGenerator.TryCreate(interpretation);
        Assert.NotNull(request);
        Assert.Equal(ClarificationKind.AmbiguousIntent, request.Kind);
        Assert.Contains("more specific", request.Question);
    }
    [Fact]
    public void UnknownInterpretationAsksToRephrase()
    {
        var interpretation = RequestInterpretation.Unknown("hello there", "No known pattern matched.");
        var request = ClarificationGenerator.TryCreate(interpretation);
        Assert.NotNull(request);
        Assert.Equal(ClarificationKind.UnknownIntent, request.Kind);
        Assert.Contains("rephrase", request.Question);
    }
    [Fact]
    public void GenerationIsDeterministic()
    {
        var interpretation = RequestInterpretation.Ambiguous(
            "show README.md",
            new[]
            {
                new IntentMatch("show.inspect", RequestIntentKind.InspectPath, null),
                new IntentMatch("show.explain", RequestIntentKind.Explanation, null),
            });
        var first = ClarificationGenerator.TryCreate(interpretation);
        var second = ClarificationGenerator.TryCreate(interpretation);
        Assert.Equal(first, second);
    }
}