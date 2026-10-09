namespace Lattice.Core.Tests;

public sealed class KnowledgePackageTests
{
    [Fact]
    public void PackagesWithSameContentAreEqual()
    {
        KnowledgePackage first = Create();
        KnowledgePackage second = Create();
        Assert.Equal(first, second);
    }

    [Fact]
    public void PackagesWithDifferentFactsAreNotEqual()
    {
        KnowledgePackage first = Create();
        KnowledgePackage second = new(
            new SchemaVersion(1, 0),
            "test.pkg",
            "Test Package",
            facts: [new KnowledgeFact("fact.b", "concept.a", "p", "other")]);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ConceptsWithSameAliasesAreEqual()
    {
        KnowledgeConcept first = new("concept.a", "a", ["alias"]);
        KnowledgeConcept second = new("concept.a", "a", ["alias"]);
        Assert.Equal(first, second);
    }

    private static KnowledgePackage Create()
    {
        return new(
            new SchemaVersion(1, 0),
            "test.pkg",
            "Test Package",
            concepts: [new KnowledgeConcept("concept.a", "a")],
            facts: [new KnowledgeFact("fact.a", "concept.a", "p", "v")]);
    }
}