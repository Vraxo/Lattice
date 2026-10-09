namespace Lattice.Core.Tests;

public sealed class ConceptLookupTests
{
    [Fact]
    public void FoundCarriesConceptAndProvenance()
    {
        KnowledgeConcept concept = new("concept.a", "a");
        ConceptLookup lookup = ConceptLookup.Found("test.pkg", concept);
        Assert.True(lookup.IsFound);
        Assert.Equal("test.pkg", lookup.PackageId);
        Assert.Equal(concept, lookup.Concept);
        Assert.Empty(lookup.Candidates);
    }

    [Fact]
    public void NotFoundCarriesProvenanceAndNoConcept()
    {
        ConceptLookup lookup = ConceptLookup.NotFound("test.pkg");
        Assert.False(lookup.IsFound);
        Assert.Equal(KnowledgeMatchKind.NotFound, lookup.Kind);
        Assert.Null(lookup.Concept);
        Assert.Empty(lookup.Candidates);
    }

    [Fact]
    public void AmbiguousCarriesCandidates()
    {
        KnowledgeConcept first = new("concept.a", "a");
        KnowledgeConcept second = new("concept.b", "b");
        ConceptLookup lookup = ConceptLookup.Ambiguous("test.pkg", [first, second]);
        Assert.Equal(KnowledgeMatchKind.Ambiguous, lookup.Kind);
        Assert.Null(lookup.Concept);
        Assert.Equal(new[] { first, second }, lookup.Candidates);
    }

    [Fact]
    public void AmbiguousRejectsFewerThanTwoCandidates()
    {
        KnowledgeConcept concept = new("concept.a", "a");
        Assert.Throws<ArgumentException>(() => ConceptLookup.Ambiguous("test.pkg", [concept]));
    }

    [Fact]
    public void LookupsWithSameContentAreEqual()
    {
        KnowledgeConcept concept = new("concept.a", "a");
        Assert.Equal(ConceptLookup.Found("test.pkg", concept), ConceptLookup.Found("test.pkg", concept));
    }
}