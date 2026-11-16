namespace Lattice.Core.Tests;

public sealed class ConceptLookupTests
{
    [Fact]
    public void FoundCarriesConceptAndProvenance()
    {
        KnowledgeConcept concept = new("concept.a", "a");
        ConceptLookup lookup = ConceptLookup.Found("test.pkg", concept);
        lookup.IsFound.Should().BeTrue();
        lookup.PackageId.Should().Be("test.pkg");
        lookup.Concept.Should().Be(concept);
        lookup.Candidates.Should().BeEmpty();
    }

    [Fact]
    public void NotFoundCarriesProvenanceAndNoConcept()
    {
        ConceptLookup lookup = ConceptLookup.NotFound("test.pkg");
        lookup.IsFound.Should().BeFalse();
        lookup.Kind.Should().Be(KnowledgeMatchKind.NotFound);
        lookup.Concept.Should().BeNull();
        lookup.Candidates.Should().BeEmpty();
    }

    [Fact]
    public void AmbiguousCarriesCandidates()
    {
        KnowledgeConcept first = new("concept.a", "a");
        KnowledgeConcept second = new("concept.b", "b");
        ConceptLookup lookup = ConceptLookup.Ambiguous("test.pkg", [first, second]);
        lookup.Kind.Should().Be(KnowledgeMatchKind.Ambiguous);
        lookup.Concept.Should().BeNull();
        lookup.Candidates.Should().Equal(new[] { first, second });
    }

    [Fact]
    public void AmbiguousRejectsFewerThanTwoCandidates()
    {
        KnowledgeConcept concept = new("concept.a", "a");
        FluentActions.Invoking(() => ConceptLookup.Ambiguous("test.pkg", [concept])).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void LookupsWithSameContentAreEqual()
    {
        KnowledgeConcept concept = new("concept.a", "a");
        ConceptLookup.Found("test.pkg", concept).Should().Be(ConceptLookup.Found("test.pkg", concept));
    }
}