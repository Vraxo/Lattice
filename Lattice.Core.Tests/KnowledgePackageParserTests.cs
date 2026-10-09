namespace Lattice.Core.Tests;
public sealed class KnowledgePackageParserTests
{
    private const string ValidPackage =
        "{\"schemaVersion\":\"1.0\",\"id\":\"test.pkg\",\"name\":\"Test Package\"," +
        "\"concepts\":[{\"id\":\"concept.a\",\"preferred\":\"a\",\"aliases\":[\"alias\"]}]," +
        "\"facts\":[{\"id\":\"fact.a\",\"subject\":\"concept.a\",\"predicate\":\"p\",\"value\":\"v\"}]," +
        "\"rules\":[{\"id\":\"rule.a\",\"description\":\"derives q\"," +
        "\"premises\":[{\"subject\":\"concept.a\",\"predicate\":\"p\",\"value\":\"v\"}]," +
        "\"conclusion\":{\"subject\":\"concept.a\",\"predicate\":\"q\",\"value\":\"w\"}}]}";
    [Fact]
    public void ParsesTheExamplePackage()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "knowledge", "example.physics", "package.json");
        Assert.True(File.Exists(path), $"Example package not found at {path}.");
        var result = KnowledgePackageParser.Parse(File.ReadAllText(path));
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal("example.physics", result.Value.Id);
        Assert.Equal(2, result.Value.Concepts.Length);
        Assert.Equal(4, result.Value.Facts.Length);
        Assert.Equal(1, result.Value.Rules.Length);
    }
    [Fact]
    public void ParsesValidPackage()
    {
        var result = KnowledgePackageParser.Parse(ValidPackage);
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(new SchemaVersion(1, 0), result.Value.SchemaVersion);
        Assert.Equal("test.pkg", result.Value.Id);
        Assert.Single(result.Value.Concepts);
        Assert.Single(result.Value.Facts);
        Assert.Single(result.Value.Rules);
        Assert.Equal("alias", Assert.Single(result.Value.Concepts[0].Aliases));
    }
    [Fact]
    public void RejectsEmptyInput()
    {
        Assert.Equal("knowledge.invalid", Failure("").Code);
    }
    [Fact]
    public void RejectsMalformedJson()
    {
        Assert.Equal("knowledge.invalid", Failure("{ not json").Code);
    }
    [Fact]
    public void RejectsNonObjectRoot()
    {
        var error = Failure("[]");
        Assert.Contains("root must be a JSON object", error.Message);
    }
    [Fact]
    public void RejectsUnknownField()
    {
        var error = Failure(WithTopLevel("\"extra\":1,"));
        Assert.Contains("document.extra: unknown field.", error.Message);
    }
    [Fact]
    public void RejectsUnsupportedMajorVersion()
    {
        var error = Failure(ValidPackage.Replace("\"schemaVersion\":\"1.0\"", "\"schemaVersion\":\"2.0\""));
        Assert.Contains("unsupported major version 2", error.Message);
    }
    [Fact]
    public void AcceptsNewerMinorVersion()
    {
        var result = KnowledgePackageParser.Parse(
            ValidPackage.Replace("\"schemaVersion\":\"1.0\"", "\"schemaVersion\":\"1.5\""));
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(new SchemaVersion(1, 5), result.Value.SchemaVersion);
    }
    [Fact]
    public void RejectsMissingRequiredField()
    {
        var error = Failure(ValidPackage.Replace("\"name\":\"Test Package\",", ""));
        Assert.Contains("document.name: missing required field.", error.Message);
    }
    [Fact]
    public void RejectsInvalidIdentifier()
    {
        var error = Failure(ValidPackage.Replace("\"id\":\"test.pkg\"", "\"id\":\"Test.Pkg\""));
        Assert.Contains("document.id: 'Test.Pkg' is not a valid identifier.", error.Message);
    }
    [Fact]
    public void RejectsDuplicateEntityIdAcrossKinds()
    {
        var error = Failure(ValidPackage.Replace("\"id\":\"fact.a\"", "\"id\":\"concept.a\""));
        Assert.Contains("duplicate entity id 'concept.a'", error.Message);
    }
    [Fact]
    public void RejectsUnknownSubjectReference()
    {
        var error = Failure(ValidPackage.Replace("\"subject\":\"concept.a\"", "\"subject\":\"concept.missing\""));
        Assert.Contains("unknown concept 'concept.missing'", error.Message);
    }
    [Fact]
    public void RejectsEmptyPremises()
    {
        var error = Failure(ValidPackage.Replace(
            "\"premises\":[{\"subject\":\"concept.a\",\"predicate\":\"p\",\"value\":\"v\"}]",
            "\"premises\":[]"));
        Assert.Contains("rules[0].premises", error.Message);
    }
    [Fact]
    public void ReportsMultipleProblems()
    {
        var error = Failure(WithTopLevel("\"extra\":1,").Replace("\"name\":\"Test Package\",", ""));
        Assert.Contains("document.extra: unknown field.", error.Message);
        Assert.Contains("document.name: missing required field.", error.Message);
    }
    private static Error Failure(string json)
    {
        var result = KnowledgePackageParser.Parse(json);
        Assert.False(result.IsSuccess);
        return result.Error!;
    }
    private static string WithTopLevel(string insert) =>
        ValidPackage.Replace("\"schemaVersion\":\"1.0\",", "\"schemaVersion\":\"1.0\"," + insert);
}