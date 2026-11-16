namespace Lattice.Core.Tests;

public sealed class IntentPatternCatalogTests
{
    [Fact]
    public void ParsesThePatternsFile()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "config", "nlp", "patterns.json");
        File.Exists(path).Should().BeTrue("{0}", $"Patterns file not found at {path}." ?? string.Empty);
        Result<IntentPatternCatalog> result = IntentPatternCatalogParser.Parse(File.ReadAllText(path));
        result.IsSuccess.Should().BeTrue("{0}", result.Error?.Message ?? string.Empty);
        result.Value.Patterns.Should().NotBeEmpty();
    }

    [Fact]
    public void RejectsMalformedJson()
    {
        Failure("{ not json").Code.Should().Be("nlp.invalid-patterns");
    }

    [Fact]
    public void RejectsUnknownField()
    {
        Error error = Failure("{\"patternVersion\":\"1.0\",\"extra\":1,\"patterns\":[]}");
        error.Message.Should().Contain("document.extra: unknown field.");
    }

    [Fact]
    public void RejectsUnsupportedMajorVersion()
    {
        Error error = Failure("{\"patternVersion\":\"2.0\",\"patterns\":[]}");
        error.Message.Should().Contain("unsupported major version 2");
    }

    [Fact]
    public void RejectsUnknownIntent()
    {
        Error error = Failure("{\"patternVersion\":\"1.0\",\"patterns\":[{\"id\":\"a\",\"intent\":\"nope\",\"template\":\"x\"}]}");
        error.Message.Should().Contain("unknown intent 'nope'");
    }

    [Fact]
    public void RejectsDuplicatePatternId()
    {
        Error error = Failure(
            "{\"patternVersion\":\"1.0\",\"patterns\":[" +
            "{\"id\":\"a\",\"intent\":\"question\",\"template\":\"x\"}," +
            "{\"id\":\"a\",\"intent\":\"question\",\"template\":\"y\"}]}");
        error.Message.Should().Contain("duplicate pattern id 'a'");
    }

    [Fact]
    public void RejectsInvalidTemplate()
    {
        Error error = Failure("{\"patternVersion\":\"1.0\",\"patterns\":[{\"id\":\"a\",\"intent\":\"question\",\"template\":\"{x}\"}]}");
        error.Message.Should().Contain("patterns[0].template");
    }

    [Fact]
    public void CatalogConstructorRejectsDuplicateIds()
    {
        IntentPattern[] patterns =
        [
            new IntentPattern("a", RequestIntentKind.Question, ["x"], null),
            new IntentPattern("a", RequestIntentKind.Question, ["y"], null),
        ];
        FluentActions.Invoking(() => new IntentPatternCatalog(patterns)).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void AcceptsVersion10And11CatalogsIncludingSurroundedSlotTemplates()
    {
        const string v10Json =
            "{\"patternVersion\":\"1.0\",\"patterns\":[{\"id\":\"inspect.path\",\"intent\":\"inspect-path\",\"template\":\"inspect {path}\"}]}";
        const string v11Json =
            "{\"patternVersion\":\"1.1\",\"patterns\":[{\"id\":\"find.appear\",\"intent\":\"find-symbol\",\"template\":\"where does {symbol} appear\"}]}";

        Result<IntentPatternCatalog> v10 = IntentPatternCatalogParser.Parse(v10Json);
        Result<IntentPatternCatalog> v11 = IntentPatternCatalogParser.Parse(v11Json);

        v10.IsSuccess.Should().BeTrue("{0}", v10.Error?.Message ?? string.Empty);
        v11.IsSuccess.Should().BeTrue("{0}", v11.Error?.Message ?? string.Empty);
        IntentPattern pattern = v11.Value.Patterns.Should().ContainSingle().Which;
        pattern.Literals.Should().Equal(["where", "does"]);
        pattern.SlotName.Should().Be("symbol");
        pattern.SuffixLiterals.Should().Equal(["appear"]);
    }

    [Theory]
    [InlineData("{symbol} appear")]
    [InlineData("where does {a} and {b} appear")]
    [InlineData("where does {} appear")]
    public void RejectsMalformedSurroundedSlotTemplatesInCatalog(string template)
    {
        Error error = Failure(
            $"{{\"patternVersion\":\"1.1\",\"patterns\":[{{\"id\":\"bad\",\"intent\":\"find-symbol\",\"template\":\"{template}\"}}]}}");
        error.Code.Should().Be("nlp.invalid-patterns");
        error.Message.Should().Contain("patterns[0].template");
    }

    private static Error Failure(string json)
    {
        Result<IntentPatternCatalog> result = IntentPatternCatalogParser.Parse(json);
        result.IsSuccess.Should().BeFalse();
        return result.Error!;
    }
}