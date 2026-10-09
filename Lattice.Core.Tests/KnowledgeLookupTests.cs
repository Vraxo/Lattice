namespace Lattice.Core.Tests;
public sealed class KnowledgeLookupTests
{
    private static KnowledgeLookup CreateLookup() => new(CreatePackage());
    [Fact]
    public void FindByIdReturnsFoundConcept()
    {
        var lookup = CreateLookup();
        var result = lookup.FindById("concept.water");
        Assert.True(result.IsFound);
        Assert.Equal("concept.water", result.Concept!.Id);
        Assert.Equal("example.physics", result.PackageId);
    }
    [Fact]
    public void FindByIdReturnsNotFoundForUnknownId()
    {
        var lookup = CreateLookup();
        var result = lookup.FindById("concept.missing");
        Assert.Equal(KnowledgeMatchKind.NotFound, result.Kind);
        Assert.Equal("example.physics", result.PackageId);
    }
    [Fact]
    public void FindByAliasIsCaseInsensitive()
    {
        var lookup = CreateLookup();
        var result = lookup.FindByAlias("h2o");
        Assert.True(result.IsFound);
        Assert.Equal("concept.water", result.Concept!.Id);
    }
    [Fact]
    public void FindByAliasReturnsNotFoundWhenAbsent()
    {
        var lookup = CreateLookup();
        var result = lookup.FindByAlias("unobtainium");
        Assert.Equal(KnowledgeMatchKind.NotFound, result.Kind);
    }
    [Fact]
    public void FindByAliasReportsAmbiguity()
    {
        var lookup = new KnowledgeLookup(new KnowledgePackage(
            new SchemaVersion(1, 0),
            "example.physics",
            "Basic Physics Example",
            concepts: new[]
            {
                new KnowledgeConcept("concept.water", "water", new[] { "shared" }),
                new KnowledgeConcept("concept.ice", "ice", new[] { "shared" }),
            }));
        var result = lookup.FindByAlias("shared");
        Assert.Equal(KnowledgeMatchKind.Ambiguous, result.Kind);
        Assert.Equal(2, result.Candidates.Length);
        Assert.Equal("example.physics", result.PackageId);
    }
    [Fact]
    public void FindByIdRejectsEmptyId()
    {
        var lookup = CreateLookup();
        Assert.Throws<ArgumentException>(() => lookup.FindById(string.Empty));
    }
    private static KnowledgePackage CreatePackage() => new(
        new SchemaVersion(1, 0),
        "example.physics",
        "Basic Physics Example",
        concepts: new[]
        {
            new KnowledgeConcept("concept.water", "water", new[] { "H2O" }),
            new KnowledgeConcept("concept.steam", "steam", new[] { "water vapor" }),
        });
}