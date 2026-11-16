namespace Lattice.Core.Tests;

public sealed class ClarificationGeneratorTests
{
    [Fact]
    public void MatchedInterpretationNeedsNoClarification()
    {
        RequestInterpretation interpretation = RequestInterpretation.Matched(
            "what is gravity",
            new IntentMatch("question.what-is", RequestIntentKind.Question, new IntentSlot("topic", "gravity")));
        ClarificationGenerator.TryCreate(interpretation).Should().BeNull();
    }

    [Fact]
    public void MissingValueAsksForTheSpecificSlot()
    {
        RequestInterpretation interpretation = RequestInterpretation.MissingValue(
            "what is",
            new IntentMatch("question.what-is", RequestIntentKind.Question, null),
            "topic");
        ClarificationRequest? request = ClarificationGenerator.TryCreate(interpretation);
        request.Should().NotBeNull();
        request.Kind.Should().Be(ClarificationKind.MissingValue);
        request.Question.Should().Be("What topic?");
    }

    [Fact]
    public void AmbiguousInterpretationNamesTheCompetingIntents()
    {
        RequestInterpretation interpretation = RequestInterpretation.Ambiguous(
            "show README.md",
            [
                new IntentMatch("show.inspect", RequestIntentKind.InspectPath, new IntentSlot("target", "README.md")),
                new IntentMatch("show.explain", RequestIntentKind.Explanation, new IntentSlot("target", "README.md")),
            ]);
        ClarificationRequest? request = ClarificationGenerator.TryCreate(interpretation);
        request.Should().NotBeNull();
        request.Kind.Should().Be(ClarificationKind.AmbiguousIntent);
        request.Question.Should().Contain("an explanation");
        request.Question.Should().Contain("a file inspection");
    }

    [Fact]
    public void AmbiguousInterpretationWithOneIntentFallsBackToGenericQuestion()
    {
        RequestInterpretation interpretation = RequestInterpretation.Ambiguous(
            "show README.md",
            [
                new IntentMatch("a", RequestIntentKind.Explanation, new IntentSlot("target", "README.md")),
                new IntentMatch("b", RequestIntentKind.Explanation, new IntentSlot("target", "README.md")),
            ]);
        ClarificationRequest? request = ClarificationGenerator.TryCreate(interpretation);
        request.Should().NotBeNull();
        request.Kind.Should().Be(ClarificationKind.AmbiguousIntent);
        request.Question.Should().Contain("more specific");
    }

    [Fact]
    public void UnknownInterpretationAsksToRephrase()
    {
        RequestInterpretation interpretation = RequestInterpretation.Unknown("hello there", "No known pattern matched.");
        ClarificationRequest? request = ClarificationGenerator.TryCreate(interpretation);
        request.Should().NotBeNull();
        request.Kind.Should().Be(ClarificationKind.UnknownIntent);
        request.Question.Should().Contain("rephrase");
    }

    [Fact]
    public void GenerationIsDeterministic()
    {
        RequestInterpretation interpretation = RequestInterpretation.Ambiguous(
            "show README.md",
            [
                new IntentMatch("show.inspect", RequestIntentKind.InspectPath, null),
                new IntentMatch("show.explain", RequestIntentKind.Explanation, null),
            ]);
        ClarificationRequest? first = ClarificationGenerator.TryCreate(interpretation);
        ClarificationRequest? second = ClarificationGenerator.TryCreate(interpretation);
        second.Should().Be(first);
    }
}