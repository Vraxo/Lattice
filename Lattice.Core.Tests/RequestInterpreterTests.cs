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
        result.Kind.Should().Be(IntentMatchKind.Matched);
        result.Intent.Should().Be(RequestIntentKind.Question);
        result.Match!.Slot!.Name.Should().Be("topic");
        result.Match.Slot.Value.Should().Be("the capital of France");
    }

    [Fact]
    public void MatchingIsCaseInsensitive()
    {
        RequestInterpretation result = CreateInterpreter().Interpret("WHAT IS gravity");
        result.Kind.Should().Be(IntentMatchKind.Matched);
        result.Intent.Should().Be(RequestIntentKind.Question);
    }

    [Fact]
    public void MoreSpecificPatternWins()
    {
        RequestInterpretation result = CreateInterpreter().Interpret("find symbol Calculator");
        result.Kind.Should().Be(IntentMatchKind.Matched);
        result.Match!.PatternId.Should().Be("find.symbol-named");
        result.Match.Slot!.Value.Should().Be("Calculator");
    }

    [Fact]
    public void ParaphraseReachesSameIntent()
    {
        RequestInterpreter interpreter = CreateInterpreter();
        RequestInterpretation first = interpreter.Interpret("explain gravity");
        RequestInterpretation second = interpreter.Interpret("why gravity");
        first.Intent.Should().Be(RequestIntentKind.Explanation);
        second.Intent.Should().Be(RequestIntentKind.Explanation);
        second.Match!.PatternId.Should().NotBe(first.Match!.PatternId);
    }

    [Fact]
    public void TieProducesAmbiguousWithCandidates()
    {
        RequestInterpretation result = CreateInterpreter().Interpret("show README.md");
        result.Kind.Should().Be(IntentMatchKind.Ambiguous);
        result.Intent.Should().Be(RequestIntentKind.Unknown);
        result.Candidates.Length.Should().Be(2);
        result.Match.Should().BeNull();
    }

    [Fact]
    public void UnknownInputIsNotGivenAConfidentIntent()
    {
        RequestInterpretation result = CreateInterpreter().Interpret("hello there");
        result.Kind.Should().Be(IntentMatchKind.Unknown);
        result.Intent.Should().Be(RequestIntentKind.Unknown);
        result.Match.Should().BeNull();
        result.Diagnostic.Should().BeNotNull();
    }

    [Fact]
    public void SlotWithNoRemainingTokensReportsMissingValue()
    {
        RequestInterpretation result = CreateInterpreter().Interpret("what is");
        result.Kind.Should().Be(IntentMatchKind.MissingValue);
        result.Intent.Should().Be(RequestIntentKind.Question);
        result.MissingSlotName.Should().Be("topic");
        result.Match!.Slot.Should().BeNull();
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
        result.Kind.Should().Be(IntentMatchKind.Matched);
        result.Match!.PatternId.Should().Be("explain.exact");
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
        result.Kind.Should().Be(IntentMatchKind.Ambiguous);
        result.Candidates.Length.Should().Be(2);
    }

    [Fact]
    public void NoSlotPatternRejectsTrailingTokens()
    {
        RequestInterpreter interpreter = new(new IntentPatternCatalog(
        [
            new IntentPattern("exact", RequestIntentKind.Explanation, ["explain"], null),
        ]));
        interpreter.Interpret("explain").Kind.Should().Be(IntentMatchKind.Matched);
        interpreter.Interpret("explain more").Kind.Should().Be(IntentMatchKind.Unknown);
    }

    [Fact]
    public void EmptyInputIsUnknown()
    {
        RequestInterpretation result = CreateInterpreter().Interpret("   ");
        result.Kind.Should().Be(IntentMatchKind.Unknown);
    }

    [Fact]
    public void InterpretationsPreserveOriginalText()
    {
        const string input = "what is gravity";
        RequestInterpretation result = CreateInterpreter().Interpret(input);
        result.Text.Should().Be(input);
        result.Span.Start.Should().Be(0);
        result.Span.Length.Should().Be(input.Length);
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

        result.Kind.Should().Be(IntentMatchKind.Matched);
        result.Intent.Should().Be(RequestIntentKind.FindSymbol);
        result.Text.Should().Be(input);
        result.Match!.PatternId.Should().Be(expectedPatternId);
        result.Match.Slot!.Name.Should().Be("symbol");
        result.Match.Slot.Value.Should().Be(expectedSymbol);
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

        result.Kind.Should().Be(IntentMatchKind.Matched);
        result.Intent.Should().Be(RequestIntentKind.InspectPath);
        result.Text.Should().Be(input);
        result.Match!.PatternId.Should().Be(expectedPatternId);
        result.Match.Slot!.Name.Should().Be("path");
        result.Match.Slot.Value.Should().Be(expectedPath);
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

        result.Kind.Should().Be(IntentMatchKind.MissingValue);
        result.Intent.Should().Be(expectedIntent);
        result.MissingSlotName.Should().Be(expectedSlot);
        result.Text.Should().Be(input);
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

        result.Kind.Should().Be(IntentMatchKind.Ambiguous);
        result.Candidates.Length.Should().Be(2);
        result.Match.Should().BeNull();
    }

    [Theory]
    [InlineData("Please refactor WorkspaceRoot.cs to use records.")]
    [InlineData("Run dotnet test for me.")]
    [InlineData("???")]
    public void UnsupportedNaturalSentencesRemainUnknown(string input)
    {
        RequestInterpretation result = CreateProductionInterpreter().Interpret(input);

        result.Kind.Should().Be(IntentMatchKind.Unknown);
        result.Match.Should().BeNull();
        result.Text.Should().Be(input);
    }

    private static RequestInterpreter CreateProductionInterpreter()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "config", "nlp", "patterns.json");
        Result<IntentPatternCatalog> parsed = IntentPatternCatalogParser.Parse(File.ReadAllText(path));
        parsed.IsSuccess.Should().BeTrue("{0}", parsed.Error?.Message ?? string.Empty);
        return new RequestInterpreter(parsed.Value);
    }
}