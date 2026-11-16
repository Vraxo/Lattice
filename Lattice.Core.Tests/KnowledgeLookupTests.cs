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
        result.IsFound.Should().BeTrue();
        result.Concept!.Id.Should().Be("concept.water");
        result.PackageId.Should().Be("example.physics");
    }

    [Fact]
    public void FindByIdReturnsNotFoundForUnknownId()
    {
        KnowledgeLookup lookup = CreateLookup();
        ConceptLookup result = lookup.FindById("concept.missing");
        result.Kind.Should().Be(KnowledgeMatchKind.NotFound);
        result.PackageId.Should().Be("example.physics");
    }

    [Fact]
    public void FindByAliasIsCaseInsensitive()
    {
        KnowledgeLookup lookup = CreateLookup();
        ConceptLookup result = lookup.FindByAlias("h2o");
        result.IsFound.Should().BeTrue();
        result.Concept!.Id.Should().Be("concept.water");
    }

    [Fact]
    public void FindByAliasReturnsNotFoundWhenAbsent()
    {
        KnowledgeLookup lookup = CreateLookup();
        ConceptLookup result = lookup.FindByAlias("unobtainium");
        result.Kind.Should().Be(KnowledgeMatchKind.NotFound);
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
        result.Kind.Should().Be(KnowledgeMatchKind.Ambiguous);
        result.Candidates.Length.Should().Be(2);
        result.PackageId.Should().Be("example.physics");
    }

    [Fact]
    public void FindByIdRejectsEmptyId()
    {
        KnowledgeLookup lookup = CreateLookup();
        FluentActions.Invoking(() => lookup.FindById(string.Empty)).Should().ThrowExactly<ArgumentException>();
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