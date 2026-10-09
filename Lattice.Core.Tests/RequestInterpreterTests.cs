using System.Collections.Immutable;
namespace Lattice.Core.Tests;
public sealed class RequestInterpreterTests
{
    private static RequestInterpreter CreateInterpreter() => new(new IntentPatternCatalog(new[]
    {
        new IntentPattern("question.what-is", RequestIntentKind.Question, ImmutableArray.Create("what", "is"), "topic"),
        new IntentPattern("explain.plain", RequestIntentKind.Explanation, ImmutableArray.Create("explain"), "topic"),
        new IntentPattern("explain.why", RequestIntentKind.Explanation, ImmutableArray.Create("why"), "subject"),
        new IntentPattern("find.symbol", RequestIntentKind.FindSymbol, ImmutableArray.Create("find"), "symbol"),
        new IntentPattern("find.symbol-named", RequestIntentKind.FindSymbol, ImmutableArray.Create("find", "symbol"), "symbol"),
        new IntentPattern("show.inspect", RequestIntentKind.InspectPath, ImmutableArray.Create("show"), "target"),
        new IntentPattern("show.explain", RequestIntentKind.Explanation, ImmutableArray.Create("show"), "target"),
    }));
    [Fact]
    public void MatchesSinglePatternWithSlot()
    {
        var result = CreateInterpreter().Interpret("what is the capital of France");
        Assert.Equal(IntentMatchKind.Matched, result.Kind);
        Assert.Equal(RequestIntentKind.Question, result.Intent);
        Assert.Equal("topic", result.Match!.Slot!.Name);
        Assert.Equal("the capital of France", result.Match.Slot.Value);
    }
    [Fact]
    public void MatchingIsCaseInsensitive()
    {
        var result = CreateInterpreter().Interpret("WHAT IS gravity");
        Assert.Equal(IntentMatchKind.Matched, result.Kind);
        Assert.Equal(RequestIntentKind.Question, result.Intent);
    }
    [Fact]
    public void MoreSpecificPatternWins()
    {
        var result = CreateInterpreter().Interpret("find symbol Calculator");
        Assert.Equal(IntentMatchKind.Matched, result.Kind);
        Assert.Equal("find.symbol-named", result.Match!.PatternId);
        Assert.Equal("Calculator", result.Match.Slot!.Value);
    }
    [Fact]
    public void ParaphraseReachesSameIntent()
    {
        var interpreter = CreateInterpreter();
        var first = interpreter.Interpret("explain gravity");
        var second = interpreter.Interpret("why gravity");
        Assert.Equal(RequestIntentKind.Explanation, first.Intent);
        Assert.Equal(RequestIntentKind.Explanation, second.Intent);
        Assert.NotEqual(first.Match!.PatternId, second.Match!.PatternId);
    }
    [Fact]
    public void TieProducesAmbiguousWithCandidates()
    {
        var result = CreateInterpreter().Interpret("show README.md");
        Assert.Equal(IntentMatchKind.Ambiguous, result.Kind);
        Assert.Equal(RequestIntentKind.Unknown, result.Intent);
        Assert.Equal(2, result.Candidates.Length);
        Assert.Null(result.Match);
    }
    [Fact]
    public void UnknownInputIsNotGivenAConfidentIntent()
    {
        var result = CreateInterpreter().Interpret("hello there");
        Assert.Equal(IntentMatchKind.Unknown, result.Kind);
        Assert.Equal(RequestIntentKind.Unknown, result.Intent);
        Assert.Null(result.Match);
        Assert.NotNull(result.Diagnostic);
    }
    [Fact]
    public void SlotWithNoRemainingTokensDoesNotMatch()
    {
        var result = CreateInterpreter().Interpret("what is");
        Assert.Equal(IntentMatchKind.Unknown, result.Kind);
    }
    [Fact]
    public void NoSlotPatternRejectsTrailingTokens()
    {
        var interpreter = new RequestInterpreter(new IntentPatternCatalog(new[]
        {
            new IntentPattern("exact", RequestIntentKind.Explanation, ImmutableArray.Create("explain"), null),
        }));
        Assert.Equal(IntentMatchKind.Matched, interpreter.Interpret("explain").Kind);
        Assert.Equal(IntentMatchKind.Unknown, interpreter.Interpret("explain more").Kind);
    }
    [Fact]
    public void EmptyInputIsUnknown()
    {
        var result = CreateInterpreter().Interpret("   ");
        Assert.Equal(IntentMatchKind.Unknown, result.Kind);
    }
    [Fact]
    public void InterpretationsPreserveOriginalText()
    {
        const string input = "what is gravity";
        var result = CreateInterpreter().Interpret(input);
        Assert.Equal(input, result.Text);
        Assert.Equal(0, result.Span.Start);
        Assert.Equal(input.Length, result.Span.Length);
    }
}