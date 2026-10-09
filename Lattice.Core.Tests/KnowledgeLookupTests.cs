namespace Lattice.Core.Tests;

public sealed class KnowledgeLookupTests
{
    private static KnowledgeLookup CreateLookup()
    {
        return new(CreatePackage());
    }

    [Fact]
    public void FindByIdReturnsFoundConcept()
    {
        KnowledgeLookup lookup = CreateLookup();
        ConceptLookup result = lookup.FindById("concept.water");
        Assert.True(result.IsFound);
        Assert.Equal("concept.water", result.Concept!.Id);
        Assert.Equal("example.physics", result.PackageId);
    }

    [Fact]
    public void FindByIdReturnsNotFoundForUnknownId()
    {
        KnowledgeLookup lookup = CreateLookup();
        ConceptLookup result = lookup.FindById("concept.missing");
        Assert.Equal(KnowledgeMatchKind.NotFound, result.Kind);
        Assert.Equal("example.physics", result.PackageId);
    }

    [Fact]
    public void FindByAliasIsCaseInsensitive()
    {
        KnowledgeLookup lookup = CreateLookup();
        ConceptLookup result = lookup.FindByAlias("h2o");
        Assert.True(result.IsFound);
        Assert.Equal("concept.water", result.Concept!.Id);
    }

    [Fact]
    public void FindByAliasReturnsNotFoundWhenAbsent()
    {
        KnowledgeLookup lookup = CreateLookup();
        ConceptLookup result = lookup.FindByAlias("unobtainium");
        Assert.Equal(KnowledgeMatchKind.NotFound, result.Kind);
    }

    [Fact]
    public void FindByAliasReportsAmbiguity()
    {
        KnowledgeLookup lookup = new(new KnowledgePackage(
            new SchemaVersion(1, 0),
            "example.physics",
            "Basic Physics Example",
            concepts:
            [
                new KnowledgeConcept("concept.water", "water", ["shared"]),
                new KnowledgeConcept("concept.ice", "ice", ["shared"]),
            ]));
        ConceptLookup result = lookup.FindByAlias("shared");
        Assert.Equal(KnowledgeMatchKind.Ambiguous, result.Kind);
        Assert.Equal(2, result.Candidates.Length);
        Assert.Equal("example.physics", result.PackageId);
    }

    [Fact]
    public void FindByIdRejectsEmptyId()
    {
        KnowledgeLookup lookup = CreateLookup();
        Assert.Throws<ArgumentException>(() => lookup.FindById(string.Empty));
    }

    private static KnowledgePackage CreatePackage()
    {
        return new(
        new SchemaVersion(1, 0),
        "example.physics",
        "Basic Physics Example",
        concepts:
        [
            new KnowledgeConcept("concept.water", "water", ["H2O"]),
            new KnowledgeConcept("concept.steam", "steam", ["water vapor"]),
        ]);
    }
}