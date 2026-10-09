namespace Lattice.Core.Tests;
public sealed class IntentPatternCatalogTests
{
    [Fact]
    public void ParsesThePatternsFile()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "config", "nlp", "patterns.json");
        Assert.True(File.Exists(path), $"Patterns file not found at {path}.");
        var result = IntentPatternCatalogParser.Parse(File.ReadAllText(path));
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.NotEmpty(result.Value.Patterns);
    }
    [Fact]
    public void RejectsMalformedJson()
    {
        Assert.Equal("nlp.invalid-patterns", Failure("{ not json").Code);
    }
    [Fact]
    public void RejectsUnknownField()
    {
        var error = Failure("{\"patternVersion\":\"1.0\",\"extra\":1,\"patterns\":[]}");
        Assert.Contains("document.extra: unknown field.", error.Message);
    }
    [Fact]
    public void RejectsUnsupportedMajorVersion()
    {
        var error = Failure("{\"patternVersion\":\"2.0\",\"patterns\":[]}");
        Assert.Contains("unsupported major version 2", error.Message);
    }
    [Fact]
    public void RejectsUnknownIntent()
    {
        var error = Failure("{\"patternVersion\":\"1.0\",\"patterns\":[{\"id\":\"a\",\"intent\":\"nope\",\"template\":\"x\"}]}");
        Assert.Contains("unknown intent 'nope'", error.Message);
    }
    [Fact]
    public void RejectsDuplicatePatternId()
    {
        var error = Failure(
            "{\"patternVersion\":\"1.0\",\"patterns\":[" +
            "{\"id\":\"a\",\"intent\":\"question\",\"template\":\"x\"}," +
            "{\"id\":\"a\",\"intent\":\"question\",\"template\":\"y\"}]}");
        Assert.Contains("duplicate pattern id 'a'", error.Message);
    }
    [Fact]
    public void RejectsInvalidTemplate()
    {
        var error = Failure("{\"patternVersion\":\"1.0\",\"patterns\":[{\"id\":\"a\",\"intent\":\"question\",\"template\":\"{x}\"}]}");
        Assert.Contains("patterns[0].template", error.Message);
    }
    [Fact]
    public void CatalogConstructorRejectsDuplicateIds()
    {
        var patterns = new[]
        {
            new IntentPattern("a", RequestIntentKind.Question, System.Collections.Immutable.ImmutableArray.Create("x"), null),
            new IntentPattern("a", RequestIntentKind.Question, System.Collections.Immutable.ImmutableArray.Create("y"), null),
        };
        Assert.Throws<ArgumentException>(() => new IntentPatternCatalog(patterns));
    }
    private static Error Failure(string json)
    {
        var result = IntentPatternCatalogParser.Parse(json);
        Assert.False(result.IsSuccess);
        return result.Error!;
    }
}