namespace Lattice.Core.Tests;
public sealed class ConceptLookupTests
{
    [Fact]
    public void FoundCarriesConceptAndProvenance()
    {
        var concept = new KnowledgeConcept("concept.a", "a");
        var lookup = ConceptLookup.Found("test.pkg", concept);
        Assert.True(lookup.IsFound);
        Assert.Equal("test.pkg", lookup.PackageId);
        Assert.Equal(concept, lookup.Concept);
        Assert.Empty(lookup.Candidates);
    }
    [Fact]
    public void NotFoundCarriesProvenanceAndNoConcept()
    {
        var lookup = ConceptLookup.NotFound("test.pkg");
        Assert.False(lookup.IsFound);
        Assert.Equal(KnowledgeMatchKind.NotFound, lookup.Kind);
        Assert.Null(lookup.Concept);
        Assert.Empty(lookup.Candidates);
    }
    [Fact]
    public void AmbiguousCarriesCandidates()
    {
        var first = new KnowledgeConcept("concept.a", "a");
        var second = new KnowledgeConcept("concept.b", "b");
        var lookup = ConceptLookup.Ambiguous("test.pkg", new[] { first, second });
        Assert.Equal(KnowledgeMatchKind.Ambiguous, lookup.Kind);
        Assert.Null(lookup.Concept);
        Assert.Equal(new[] { first, second }, lookup.Candidates);
    }
    [Fact]
    public void AmbiguousRejectsFewerThanTwoCandidates()
    {
        var concept = new KnowledgeConcept("concept.a", "a");
        Assert.Throws<ArgumentException>(() => ConceptLookup.Ambiguous("test.pkg", new[] { concept }));
    }
    [Fact]
    public void LookupsWithSameContentAreEqual()
    {
        var concept = new KnowledgeConcept("concept.a", "a");
        Assert.Equal(ConceptLookup.Found("test.pkg", concept), ConceptLookup.Found("test.pkg", concept));
    }
}