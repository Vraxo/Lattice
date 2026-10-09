namespace Lattice.Core.Tests;
public sealed class KnowledgePackageTests
{
    [Fact]
    public void PackagesWithSameContentAreEqual()
    {
        var first = Create();
        var second = Create();
        Assert.Equal(first, second);
    }
    [Fact]
    public void PackagesWithDifferentFactsAreNotEqual()
    {
        var first = Create();
        var second = new KnowledgePackage(
            new SchemaVersion(1, 0),
            "test.pkg",
            "Test Package",
            facts: new[] { new KnowledgeFact("fact.b", "concept.a", "p", "other") });
        Assert.NotEqual(first, second);
    }
    [Fact]
    public void ConceptsWithSameAliasesAreEqual()
    {
        var first = new KnowledgeConcept("concept.a", "a", new[] { "alias" });
        var second = new KnowledgeConcept("concept.a", "a", new[] { "alias" });
        Assert.Equal(first, second);
    }
    private static KnowledgePackage Create() =>
        new(
            new SchemaVersion(1, 0),
            "test.pkg",
            "Test Package",
            concepts: new[] { new KnowledgeConcept("concept.a", "a") },
            facts: new[] { new KnowledgeFact("fact.a", "concept.a", "p", "v") });
}