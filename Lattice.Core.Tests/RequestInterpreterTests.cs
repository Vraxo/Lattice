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

    [Theory]
    [InlineData("Where does WorkspaceRoot appear?", "find.where-does-appear", "WorkspaceRoot")]
    [InlineData("where does My_Namespace.WorkspaceRoot appear", "find.where-does-appear", "My_Namespace.WorkspaceRoot")]
    [InlineData("Where is WorkspaceRoot used in this project?", "find.where-is-used-in-project", "WorkspaceRoot")]
    [InlineData("Can you find where WorkspaceRoot is used throughout this project?", "find.can-you-find-where-used-throughout-project", "WorkspaceRoot")]
    [InlineData("Search this project for WorkspaceRoot.", "find.search-project-for", "WorkspaceRoot")]
    [InlineData("Find all occurrences of WorkspaceRoot.", "find.all-occurrences", "WorkspaceRoot")]
    [InlineData("FIND ALL OCCURRENCES OF _internal_helper!", "find.all-occurrences", "_internal_helper")]
    [InlineData("find symbol WorkspaceRoot", "find.symbol-named", "WorkspaceRoot")]
    public void ConfiguredCatalogMatchesNaturalSearchPhrasesAndPreservesOriginalText(
        string input,
        string expectedPatternId,
        string expectedSymbol)
    {
        RequestInterpretation result = CreateProductionInterpreter().Interpret(input);

        Assert.Equal(IntentMatchKind.Matched, result.Kind);
        Assert.Equal(RequestIntentKind.FindSymbol, result.Intent);
        Assert.Equal(input, result.Text);
        Assert.Equal(expectedPatternId, result.Match!.PatternId);
        Assert.Equal("symbol", result.Match.Slot!.Name);
        Assert.Equal(expectedSymbol, result.Match.Slot.Value);
    }

    [Theory]
    [InlineData("Show me Lattice/WorkspaceRoot.cs.", "inspect.show-me", "Lattice/WorkspaceRoot.cs")]
    [InlineData("Open the file Lattice/WorkspaceRoot.cs.", "inspect.open-the-file", "Lattice/WorkspaceRoot.cs")]
    [InlineData("Can you read Lattice/WorkspaceRoot.cs?", "inspect.can-you-read", "Lattice/WorkspaceRoot.cs")]
    [InlineData("What's in Lattice/WorkspaceRoot.cs?", "inspect.whats-in", "Lattice/WorkspaceRoot.cs")]
    [InlineData("What\u2019s in .editorconfig?", "inspect.whats-in", ".editorconfig")]
    [InlineData("Open the file docs/my notes/Read Me-v2.test.md.", "inspect.open-the-file", "docs/my notes/Read Me-v2.test.md")]
    [InlineData("read README.md", "inspect.read", "README.md")]
    [InlineData("inspect Lattice/WorkspaceRoot.cs", "inspect.path", "Lattice/WorkspaceRoot.cs")]
    [InlineData("inspect ..", "inspect.path", "..")]
    public void ConfiguredCatalogMatchesNaturalFileInspectionPhrasesAndPreservesPaths(
        string input,
        string expectedPatternId,
        string expectedPath)
    {
        RequestInterpretation result = CreateProductionInterpreter().Interpret(input);

        Assert.Equal(IntentMatchKind.Matched, result.Kind);
        Assert.Equal(RequestIntentKind.InspectPath, result.Intent);
        Assert.Equal(input, result.Text);
        Assert.Equal(expectedPatternId, result.Match!.PatternId);
        Assert.Equal("path", result.Match.Slot!.Name);
        Assert.Equal(expectedPath, result.Match.Slot.Value);
    }

    [Theory]
    [InlineData("where does appear?", RequestIntentKind.FindSymbol, "symbol")]
    [InlineData("find all occurrences of", RequestIntentKind.FindSymbol, "symbol")]
    [InlineData("find symbol", RequestIntentKind.FindSymbol, "symbol")]
    [InlineData("open the file.", RequestIntentKind.InspectPath, "path")]
    [InlineData("show me?", RequestIntentKind.InspectPath, "path")]
    public void ConfiguredCatalogReportsMissingValueForIncompletePhrases(
        string input,
        RequestIntentKind expectedIntent,
        string expectedSlot)
    {
        RequestInterpretation result = CreateProductionInterpreter().Interpret(input);

        Assert.Equal(IntentMatchKind.MissingValue, result.Kind);
        Assert.Equal(expectedIntent, result.Intent);
        Assert.Equal(expectedSlot, result.MissingSlotName);
        Assert.Equal(input, result.Text);
    }

    [Fact]
    public void CompetingSurroundedPatternsWithEqualSpecificityRemainAmbiguous()
    {
        RequestInterpreter interpreter = new(new IntentPatternCatalog(
        [
            new IntentPattern("locate.symbol", RequestIntentKind.FindSymbol, ["locate"], "symbol", ["here"]),
            new IntentPattern("locate.path", RequestIntentKind.InspectPath, ["locate"], "path", ["here"]),
        ]));

        RequestInterpretation result = interpreter.Interpret("Locate WorkspaceRoot here.");

        Assert.Equal(IntentMatchKind.Ambiguous, result.Kind);
        Assert.Equal(2, result.Candidates.Length);
        Assert.Null(result.Match);
    }

    [Theory]
    [InlineData("Please refactor WorkspaceRoot.cs to use records.")]
    [InlineData("Run dotnet test for me.")]
    [InlineData("???")]
    public void UnsupportedNaturalSentencesRemainUnknown(string input)
    {
        RequestInterpretation result = CreateProductionInterpreter().Interpret(input);

        Assert.Equal(IntentMatchKind.Unknown, result.Kind);
        Assert.Null(result.Match);
        Assert.Equal(input, result.Text);
    }

    private static RequestInterpreter CreateProductionInterpreter()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "config", "nlp", "patterns.json");
        Result<IntentPatternCatalog> parsed = IntentPatternCatalogParser.Parse(File.ReadAllText(path));
        Assert.True(parsed.IsSuccess, parsed.Error?.Message);
        return new RequestInterpreter(parsed.Value);
    }
}