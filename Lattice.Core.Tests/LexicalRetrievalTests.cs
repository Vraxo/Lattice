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
        Assert.Equal("concept.water", top.Concept.Id);
        Assert.True(ranked.Length == 1 || ranked[0].Score > ranked[1].Score);
    }

    [Fact]
    public void AliasQueryStillResolves()
    {
        KnowledgeLookup lookup = Lookup();
        ConceptLookup best = lookup.FindBest("H2O");
        Assert.True(best.IsFound);
        Assert.Equal("concept.water", best.Concept!.Id);
    }

    [Fact]
    public void DocumentationQueryNowRetrievesTheRightConcept()
    {
        // Previously unserved (recall 0.0 in the M9.1 baseline). Lexical match over the definition
        // must now return concept.water for its own definition words.
        KnowledgeLookup lookup = Lookup();
        ConceptLookup best = lookup.FindBest("liquid commonly denoted");
        Assert.True(best.IsFound);
        Assert.Equal("concept.water", best.Concept!.Id);
    }

    [Fact]
    public void ScoreEqualsSumOfFactorContributions()
    {
        KnowledgeLookup lookup = Lookup();
        RetrievalCandidate top = lookup.Search("liquid commonly denoted")[0];
        double expected = top.Factors.Sum(factor => factor.Weight * factor.RawScore);
        Assert.Equal(expected, top.Score, precision: 6);
    }

    [Fact]
    public void RankingIsExplainedByFactors()
    {
        KnowledgeLookup lookup = Lookup();
        RetrievalCandidate top = lookup.Search("liquid commonly denoted")[0];
        Assert.NotEmpty(top.Factors);
        Assert.Contains(top.Factors, factor => factor.RawScore > 0);
        Assert.Equal("definition", top.MatchedField);
        Assert.Contains("liquid", top.MatchedText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CandidateCarriesSourceAttribution()
    {
        KnowledgeLookup lookup = Lookup();
        RetrievalCandidate top = lookup.Search("concept.steam")[0];
        Assert.Equal("example.physics:concept.steam", top.Source);
    }

    [Fact]
    public void OrderingIsDeterministicForTiedScores()
    {
        KnowledgeLookup lookup = Lookup();
        System.Collections.Immutable.ImmutableArray<RetrievalCandidate> first = lookup.Search("water");
        System.Collections.Immutable.ImmutableArray<RetrievalCandidate> second = lookup.Search("water");
        Assert.True(first.Select(c => c.Concept.Id).SequenceEqual(second.Select(c => c.Concept.Id)));
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
        Assert.Equal("concept.alpha", ranked[0].Concept.Id);
        Assert.Equal("concept.zebra", ranked[1].Concept.Id);
    }

    [Fact]
    public void LimitIsHonored()
    {
        KnowledgeLookup lookup = Lookup();
        Assert.True(lookup.Search("water", limit: 1).Length <= 1);
    }

    [Fact]
    public void NonPositiveLimitIsRejected()
    {
        KnowledgeLookup lookup = Lookup();
        Assert.Throws<ArgumentOutOfRangeException>(() => lookup.Search("water", limit: 0));
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
        Assert.Equal("concept.b", baseline[0].Concept.Id);
        System.Collections.Immutable.ImmutableArray<RetrievalCandidate> tuned = lookup.Search(
            "shared",
            new LexicalRetrievalWeights(aliasExact: 1000, preferredExact: 1));
        Assert.Equal("concept.a", tuned[0].Concept.Id);
    }

    private static KnowledgeLookup Lookup()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "knowledge", "example.physics", "package.json");
        Result<KnowledgePackage> parsed = KnowledgePackageParser.Parse(File.ReadAllText(path));
        Assert.True(parsed.IsSuccess, parsed.Error?.Message);
        return new KnowledgeLookup(parsed.Value);
    }
}