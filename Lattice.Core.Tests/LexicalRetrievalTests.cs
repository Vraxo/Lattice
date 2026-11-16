namespace Lattice.Core.Tests;

/// <summary>
/// Tests the ranked lexical retrieval added in M9.2: explainable scoring, deterministic ordering,
/// source attribution, exact-match non-regression, and that the documentation query class improves
/// while no graph traversal is introduced.
/// </summary>
public sealed class LexicalRetrievalTests
{
    [Fact]
    public void ExactIdRanksFirstWithHighestScore()
    {
        KnowledgeLookup lookup = Lookup();
        System.Collections.Immutable.ImmutableArray<RetrievalCandidate> ranked = lookup.Search("concept.water");
        RetrievalCandidate top = ranked[0];
        top.Concept.Id.Should().Be("concept.water");
        (ranked.Length == 1 || ranked[0].Score > ranked[1].Score).Should().BeTrue();
    }

    [Fact]
    public void AliasQueryStillResolves()
    {
        KnowledgeLookup lookup = Lookup();
        ConceptLookup best = lookup.FindBest("H2O");
        best.IsFound.Should().BeTrue();
        best.Concept!.Id.Should().Be("concept.water");
    }

    [Fact]
    public void DocumentationQueryNowRetrievesTheRightConcept()
    {
        // Previously unserved (recall 0.0 in the M9.1 baseline). Lexical match over the definition
        // must now return concept.water for its own definition words.
        KnowledgeLookup lookup = Lookup();
        ConceptLookup best = lookup.FindBest("liquid commonly denoted");
        best.IsFound.Should().BeTrue();
        best.Concept!.Id.Should().Be("concept.water");
    }

    [Fact]
    public void ScoreEqualsSumOfFactorContributions()
    {
        KnowledgeLookup lookup = Lookup();
        RetrievalCandidate top = lookup.Search("liquid commonly denoted")[0];
        double expected = top.Factors.Sum(factor => factor.Weight * factor.RawScore);
        Math.Round(top.Score, 6).Should().Be(Math.Round(expected, 6));
    }

    [Fact]
    public void RankingIsExplainedByFactors()
    {
        KnowledgeLookup lookup = Lookup();
        RetrievalCandidate top = lookup.Search("liquid commonly denoted")[0];
        top.Factors.Should().BeNotEmpty();
        top.Factors.Where(factor => factor.RawScore > 0).Should().NotBeEmpty();
        top.MatchedField.Should().Be("definition");
        top.MatchedText.Should().ContainEquivalentOf("liquid");
    }

    [Fact]
    public void CandidateCarriesSourceAttribution()
    {
        KnowledgeLookup lookup = Lookup();
        RetrievalCandidate top = lookup.Search("concept.steam")[0];
        top.Source.Should().Be("example.physics:concept.steam");
    }

    [Fact]
    public void OrderingIsDeterministicForTiedScores()
    {
        KnowledgeLookup lookup = Lookup();
        System.Collections.Immutable.ImmutableArray<RetrievalCandidate> first = lookup.Search("water");
        System.Collections.Immutable.ImmutableArray<RetrievalCandidate> second = lookup.Search("water");
        first.Select(c => c.Concept.Id).SequenceEqual(second.Select(c => c.Concept.Id)).Should().BeTrue();
    }

    [Fact]
    public void TiesBreakByConceptIdOrdinal()
    {
        KnowledgeLookup lookup = new(new KnowledgePackage(
            new SchemaVersion(1, 0),
            "p",
            "P",
            concepts:
            [
                new KnowledgeConcept("concept.zebra", "zebra"),
                new KnowledgeConcept("concept.alpha", "alpha"),
            ]));

        // "a" is a substring of both preferred names only if present; use a query matching both ids.
        System.Collections.Immutable.ImmutableArray<RetrievalCandidate> ranked = lookup.Search("concept.");
        ranked[0].Concept.Id.Should().Be("concept.alpha");
        ranked[1].Concept.Id.Should().Be("concept.zebra");
    }

    [Fact]
    public void LimitIsHonored()
    {
        KnowledgeLookup lookup = Lookup();
        (lookup.Search("water", limit: 1).Length <= 1).Should().BeTrue();
    }

    [Fact]
    public void NonPositiveLimitIsRejected()
    {
        KnowledgeLookup lookup = Lookup();
        FluentActions.Invoking(() => lookup.Search("water", limit: 0)).Should().ThrowExactly<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WeightsAreConfigurableAndChangeRanking()
    {
        // concept.a reaches "shared" only through an alias; concept.b declares it as its preferred
        // name. Default weights favour the preferred-name match; raising the alias weight flips it.
        KnowledgeLookup lookup = new(new KnowledgePackage(
            new SchemaVersion(1, 0),
            "p",
            "P",
            concepts:
            [
                new KnowledgeConcept("concept.a", "alpha", ["shared"]),
                new KnowledgeConcept("concept.b", "shared"),
            ]));
        System.Collections.Immutable.ImmutableArray<RetrievalCandidate> baseline = lookup.Search("shared");
        baseline[0].Concept.Id.Should().Be("concept.b");
        System.Collections.Immutable.ImmutableArray<RetrievalCandidate> tuned = lookup.Search(
            "shared",
            new LexicalRetrievalWeights(aliasExact: 1000, preferredExact: 1));
        tuned[0].Concept.Id.Should().Be("concept.a");
    }

    private static KnowledgeLookup Lookup()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "knowledge", "example.physics", "package.json");
        Result<KnowledgePackage> parsed = KnowledgePackageParser.Parse(File.ReadAllText(path));
        parsed.IsSuccess.Should().BeTrue("{0}", parsed.Error?.Message ?? string.Empty);
        return new KnowledgeLookup(parsed.Value);
    }
}