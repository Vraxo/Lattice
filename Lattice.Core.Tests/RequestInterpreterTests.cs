namespace Lattice.Core.Tests;

public sealed class RequestInterpreterTests
{
    private static RequestInterpreter CreateInterpreter()
    {
        return new(new IntentPatternCatalog(
    [
        new IntentPattern("question.what-is", RequestIntentKind.Question, ["what", "is"], "topic"),
        new IntentPattern("explain.plain", RequestIntentKind.Explanation, ["explain"], "topic"),
        new IntentPattern("explain.why", RequestIntentKind.Explanation, ["why"], "subject"),
        new IntentPattern("find.symbol", RequestIntentKind.FindSymbol, ["find"], "symbol"),
        new IntentPattern("find.symbol-named", RequestIntentKind.FindSymbol, ["find", "symbol"], "symbol"),
        new IntentPattern("show.inspect", RequestIntentKind.InspectPath, ["show"], "target"),
        new IntentPattern("show.explain", RequestIntentKind.Explanation, ["show"], "target"),
    ]));
    }

    [Fact]
    public void MatchesSinglePatternWithSlot()
    {
        RequestInterpretation result = CreateInterpreter().Interpret("what is the capital of France");
        Assert.Equal(IntentMatchKind.Matched, result.Kind);
        Assert.Equal(RequestIntentKind.Question, result.Intent);
        Assert.Equal("topic", result.Match!.Slot!.Name);
        Assert.Equal("the capital of France", result.Match.Slot.Value);
    }

    [Fact]
    public void MatchingIsCaseInsensitive()
    {
        RequestInterpretation result = CreateInterpreter().Interpret("WHAT IS gravity");
        Assert.Equal(IntentMatchKind.Matched, result.Kind);
        Assert.Equal(RequestIntentKind.Question, result.Intent);
    }

    [Fact]
    public void MoreSpecificPatternWins()
    {
        RequestInterpretation result = CreateInterpreter().Interpret("find symbol Calculator");
        Assert.Equal(IntentMatchKind.Matched, result.Kind);
        Assert.Equal("find.symbol-named", result.Match!.PatternId);
        Assert.Equal("Calculator", result.Match.Slot!.Value);
    }

    [Fact]
    public void ParaphraseReachesSameIntent()
    {
        RequestInterpreter interpreter = CreateInterpreter();
        RequestInterpretation first = interpreter.Interpret("explain gravity");
        RequestInterpretation second = interpreter.Interpret("why gravity");
        Assert.Equal(RequestIntentKind.Explanation, first.Intent);
        Assert.Equal(RequestIntentKind.Explanation, second.Intent);
        Assert.NotEqual(first.Match!.PatternId, second.Match!.PatternId);
    }

    [Fact]
    public void TieProducesAmbiguousWithCandidates()
    {
        RequestInterpretation result = CreateInterpreter().Interpret("show README.md");
        Assert.Equal(IntentMatchKind.Ambiguous, result.Kind);
        Assert.Equal(RequestIntentKind.Unknown, result.Intent);
        Assert.Equal(2, result.Candidates.Length);
        Assert.Null(result.Match);
    }

    [Fact]
    public void UnknownInputIsNotGivenAConfidentIntent()
    {
        RequestInterpretation result = CreateInterpreter().Interpret("hello there");
        Assert.Equal(IntentMatchKind.Unknown, result.Kind);
        Assert.Equal(RequestIntentKind.Unknown, result.Intent);
        Assert.Null(result.Match);
        Assert.NotNull(result.Diagnostic);
    }

    [Fact]
    public void SlotWithNoRemainingTokensReportsMissingValue()
    {
        RequestInterpretation result = CreateInterpreter().Interpret("what is");
        Assert.Equal(IntentMatchKind.MissingValue, result.Kind);
        Assert.Equal(RequestIntentKind.Question, result.Intent);
        Assert.Equal("topic", result.MissingSlotName);
        Assert.Null(result.Match!.Slot);
    }

    [Fact]
    public void MissingValueIsNotReportedWhenAnotherPatternMatches()
    {
        // "explain" alone has no slot value, but it is a plain no-slot pattern and must match.
        RequestInterpreter interpreter = new(new IntentPatternCatalog(
        [
            new IntentPattern("explain.plain", RequestIntentKind.Explanation, ["explain"], "topic"),
            new IntentPattern("explain.exact", RequestIntentKind.Explanation, ["explain"], null),
        ]));
        RequestInterpretation result = interpreter.Interpret("explain");
        Assert.Equal(IntentMatchKind.Matched, result.Kind);
        Assert.Equal("explain.exact", result.Match!.PatternId);
    }

    [Fact]
    public void MultipleMissingValuePatternsAreAmbiguous()
    {
        RequestInterpreter interpreter = new(new IntentPatternCatalog(
        [
            new IntentPattern("a", RequestIntentKind.Question, ["what"], "topic"),
            new IntentPattern("b", RequestIntentKind.Explanation, ["what"], "subject"),
        ]));
        RequestInterpretation result = interpreter.Interpret("what");
        Assert.Equal(IntentMatchKind.Ambiguous, result.Kind);
        Assert.Equal(2, result.Candidates.Length);
    }

    [Fact]
    public void NoSlotPatternRejectsTrailingTokens()
    {
        RequestInterpreter interpreter = new(new IntentPatternCatalog(
        [
            new IntentPattern("exact", RequestIntentKind.Explanation, ["explain"], null),
        ]));
        Assert.Equal(IntentMatchKind.Matched, interpreter.Interpret("explain").Kind);
        Assert.Equal(IntentMatchKind.Unknown, interpreter.Interpret("explain more").Kind);
    }

    [Fact]
    public void EmptyInputIsUnknown()
    {
        RequestInterpretation result = CreateInterpreter().Interpret("   ");
        Assert.Equal(IntentMatchKind.Unknown, result.Kind);
    }

    [Fact]
    public void InterpretationsPreserveOriginalText()
    {
        const string input = "what is gravity";
        RequestInterpretation result = CreateInterpreter().Interpret(input);
        Assert.Equal(input, result.Text);
        Assert.Equal(0, result.Span.Start);
        Assert.Equal(input.Length, result.Span.Length);
    }
}