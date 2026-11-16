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
        string path = Path.Combine(AppContext.BaseDirectory, "knowledge", "example.physics", "package.json");
        File.Exists(path).Should().BeTrue("{0}", $"Example package not found at {path}.");
        Result<KnowledgePackage> result = KnowledgePackageParser.Parse(File.ReadAllText(path));
        result.IsSuccess.Should().BeTrue("{0}", result.Error?.Message ?? string.Empty);
        result.Value.Id.Should().Be("example.physics");
        result.Value.Concepts.Length.Should().Be(2);
        result.Value.Facts.Length.Should().Be(4);
        result.Value.Rules.Should().ContainSingle();
    }

    [Fact]
    public void ParsesValidPackage()
    {
        Result<KnowledgePackage> result = KnowledgePackageParser.Parse(ValidPackage);
        result.IsSuccess.Should().BeTrue("{0}", result.Error?.Message ?? string.Empty);
        result.Value.SchemaVersion.Should().Be(new SchemaVersion(1, 0));
        result.Value.Id.Should().Be("test.pkg");
        result.Value.Concepts.Should().ContainSingle();
        result.Value.Facts.Should().ContainSingle();
        result.Value.Rules.Should().ContainSingle();
        result.Value.Concepts[0].Aliases.Should().ContainSingle().Which.Should().Be("alias");
    }

    [Fact]
    public void RejectsEmptyInput()
    {
        Failure(string.Empty).Code.Should().Be("knowledge.invalid");
    }

    [Fact]
    public void RejectsMalformedJson()
    {
        Failure("{ not json").Code.Should().Be("knowledge.invalid");
    }

    [Fact]
    public void RejectsNonObjectRoot()
    {
        Error error = Failure("[]");
        error.Message.Should().Contain("root must be a JSON object");
    }

    [Fact]
    public void RejectsUnknownField()
    {
        Error error = Failure(WithTopLevel("\"extra\":1,"));
        error.Message.Should().Contain("document.extra: unknown field.");
    }

    [Fact]
    public void RejectsUnsupportedMajorVersion()
    {
        Error error = Failure(ValidPackage.Replace("\"schemaVersion\":\"1.0\"", "\"schemaVersion\":\"2.0\""));
        error.Message.Should().Contain("unsupported major version 2");
    }

    [Fact]
    public void AcceptsNewerMinorVersion()
    {
        Result<KnowledgePackage> result = KnowledgePackageParser.Parse(
            ValidPackage.Replace("\"schemaVersion\":\"1.0\"", "\"schemaVersion\":\"1.5\""));
        result.IsSuccess.Should().BeTrue("{0}", result.Error?.Message ?? string.Empty);
        result.Value.SchemaVersion.Should().Be(new SchemaVersion(1, 5));
    }

    [Fact]
    public void RejectsMissingRequiredField()
    {
        Error error = Failure(ValidPackage.Replace("\"name\":\"Test Package\",", string.Empty));
        error.Message.Should().Contain("document.name: missing required field.");
    }

    [Fact]
    public void RejectsInvalidIdentifier()
    {
        Error error = Failure(ValidPackage.Replace("\"id\":\"test.pkg\"", "\"id\":\"Test.Pkg\""));
        error.Message.Should().Contain("document.id: 'Test.Pkg' is not a valid identifier.");
    }

    [Fact]
    public void RejectsDuplicateEntityIdAcrossKinds()
    {
        Error error = Failure(ValidPackage.Replace("\"id\":\"fact.a\"", "\"id\":\"concept.a\""));
        error.Message.Should().Contain("duplicate entity id 'concept.a'");
    }

    [Fact]
    public void RejectsUnknownSubjectReference()
    {
        Error error = Failure(ValidPackage.Replace("\"subject\":\"concept.a\"", "\"subject\":\"concept.missing\""));
        error.Message.Should().Contain("unknown concept 'concept.missing'");
    }

    [Fact]
    public void RejectsEmptyPremises()
    {
        Error error = Failure(ValidPackage.Replace(
            "\"premises\":[{\"subject\":\"concept.a\",\"predicate\":\"p\",\"value\":\"v\"}]",
            "\"premises\":[]"));
        error.Message.Should().Contain("rules[0].premises");
    }

    [Fact]
    public void ReportsMultipleProblems()
    {
        Error error = Failure(WithTopLevel("\"extra\":1,").Replace("\"name\":\"Test Package\",", string.Empty));
        error.Message.Should().Contain("document.extra: unknown field.");
        error.Message.Should().Contain("document.name: missing required field.");
    }

    private static Error Failure(string json)
    {
        Result<KnowledgePackage> result = KnowledgePackageParser.Parse(json);
        result.IsSuccess.Should().BeFalse();
        return result.Error!;
    }

    private static string WithTopLevel(string insert)
    {
        return ValidPackage.Replace("\"schemaVersion\":\"1.0\",", "\"schemaVersion\":\"1.0\"," + insert);
    }
}