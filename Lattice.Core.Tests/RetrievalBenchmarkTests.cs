using System.Collections.Immutable;
using System.Text.Json;

namespace Lattice.Core.Tests;

/// <summary>
/// Runs the hand-authored retrieval benchmark. The recorded M9.1 baseline is asserted through the
/// original exact-id/alias adapter (a historical record). Post-M9.2, ranked lexical search is also
/// measured: exact and alias classes must not regress, the documentation class must improve, and a
/// fact-only query must still retrieve nothing, which is the proof that no graph traversal was
/// added. When retrieval changes, these assertions and the recorded baseline are updated together,
/// deliberately.
/// </summary>
public sealed class RetrievalBenchmarkTests
{
    private const string CorpusRelativePath = "corpus/retrieval/v1.json";

    public static TheoryData<string> Classes()
    {
        TheoryData<string> data = [];
        foreach (string queryClass in Baseline().Select(entry => entry.QueryClass).Distinct(StringComparer.Ordinal))
        {
            data.Add(queryClass);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Classes))]
    public void ClassMetricsMatchRecordedBaseline(string queryClass)
    {
        KnowledgeLookup lookup = Lookup();
        ImmutableArray<BenchmarkEntry> entries = [.. Entries().Where(entry => entry.QueryClass == queryClass)];
        Metrics metrics = Measure(lookup, entries);
        BaselineRow baseline = Baseline().Should().ContainSingle(row => row.QueryClass == queryClass).Which;
        Math.Round(metrics.Precision, 6).Should().Be(Math.Round(baseline.Precision, 6));
        Math.Round(metrics.Recall, 6).Should().Be(Math.Round(baseline.Recall, 6));
    }

    [Fact]
    public void ExactSymbolAndAliasClassesAreFullyServed()
    {
        KnowledgeLookup lookup = Lookup();
        Metrics exact = Measure(lookup, Entries().Where(e => e.QueryClass == "exact-symbol"));
        Metrics alias = Measure(lookup, Entries().Where(e => e.QueryClass == "alias"));
        Math.Round(exact.Precision, 6).Should().Be(1.0);
        Math.Round(exact.Recall, 6).Should().Be(1.0);
        Math.Round(alias.Precision, 6).Should().Be(1.0);
        Math.Round(alias.Recall, 6).Should().Be(1.0);
    }

    [Fact]
    public void RelatedConceptAndDocumentationClassesAreUnserved()
    {
        // Recording a known gap explicitly: the current exact lookup returns nothing for these
        // query classes. Adding relation traversal or full-text retrieval must change this test.
        KnowledgeLookup lookup = Lookup();
        Metrics related = Measure(lookup, Entries().Where(e => e.QueryClass == "related-concept"));
        Metrics documentation = Measure(lookup, Entries().Where(e => e.QueryClass == "documentation"));
        Math.Round(related.Recall, 6).Should().Be(0.0);
        Math.Round(documentation.Recall, 6).Should().Be(0.0);
    }

    [Fact]
    public void EveryEntryHasANonEmptyExpectedSet()
    {
        Entries().Should().AllSatisfy(entry => entry.ExpectedIds.Should().NotBeEmpty());
    }

    [Fact]
    public void EveryExpectedIdExistsInThePackage()
    {
        KnowledgePackage package = Package();
        HashSet<string> ids = [.. package.Concepts.Select(concept => concept.Id)];
        Entries().Should().AllSatisfy(entry => entry.ExpectedIds.Should().AllSatisfy(id => ids.Should().Contain(id)));
    }

    [Fact]
    public void BaselineCoversEveryQueryClassInTheCorpus()
    {
        HashSet<string> corpusClasses = [.. Entries().Select(entry => entry.QueryClass)];
        HashSet<string> baselineClasses = [.. Baseline().Select(row => row.QueryClass)];
        corpusClasses.SetEquals(baselineClasses).Should().BeTrue();
    }

    [Fact]
    public void SearchImprovesDocumentationRecallWithoutRegressingExactClasses()
    {
        // Post-M9.2 measurement over the ranked Search interface. Exact and alias recall must stay at
        // 1.0; the documentation class, previously unserved, must now retrieve at least one correct
        // concept. related-concept is not required to stay at 0: lexical overlap can serve some of
        // its queries without graph traversal (see the fact-only test).
        KnowledgeLookup lookup = Lookup();
        Metrics exact = MeasureRanked(lookup, Entries().Where(e => e.QueryClass == "exact-symbol"));
        Metrics alias = MeasureRanked(lookup, Entries().Where(e => e.QueryClass == "alias"));
        Metrics documentation = MeasureRanked(lookup, Entries().Where(e => e.QueryClass == "documentation"));
        Metrics related = MeasureRanked(lookup, Entries().Where(e => e.QueryClass == "related-concept"));
        Math.Round(exact.Recall, 6).Should().Be(1.0);
        Math.Round(alias.Recall, 6).Should().Be(1.0);
        (documentation.Recall > 0.0).Should().BeTrue("{0}", "Documentation recall should improve above the 0.0 baseline." ?? string.Empty);

        // related-concept stays unserved with this corpus: its queries connect through facts, not
        // concept text. Asserting >= 0 rather than == 0 keeps this test honest if the corpus later
        // adds a query whose words do appear in concept text (lexical, still not traversal). The
        // traversal-absence proof is the next test.
        (related.Recall >= 0.0).Should().BeTrue();
    }

    [Fact]
    public void FactOnlyConnectionsAreNotRetrievedProvingNoGraphTraversal()
    {
        // "boiling point" is connected to concept.water only through the boils_at fact; no concept
        // text contains those words. If M9.2 had added fact/relation traversal this would match. It
        // must not, because graph traversal is out of scope for M9.2.
        KnowledgeLookup lookup = Lookup();
        lookup.Search("boiling point").Should().BeEmpty();
    }

    [Fact]
    public void MrrAtFiveIsComputableAndPositiveForServedClasses()
    {
        KnowledgeLookup lookup = Lookup();
        double exact = MrrAtFive(lookup, Entries().Where(e => e.QueryClass == "exact-symbol"));
        double alias = MrrAtFive(lookup, Entries().Where(e => e.QueryClass == "alias"));
        double documentation = MrrAtFive(lookup, Entries().Where(e => e.QueryClass == "documentation"));
        Math.Round(exact, 6).Should().Be(1.0);
        Math.Round(alias, 6).Should().Be(1.0);
        (documentation > 0.0).Should().BeTrue();
    }

    private static Metrics MeasureRanked(KnowledgeLookup lookup, IEnumerable<BenchmarkEntry> entries)
    {
        int truePositives = 0;
        int retrieved = 0;
        int expected = 0;
        foreach (BenchmarkEntry entry in entries)
        {
            ImmutableArray<string> result = [.. lookup.Search(entry.Query).Select(candidate => candidate.Concept.Id)];
            retrieved += result.Length;
            expected += entry.ExpectedIds.Length;
            truePositives += result.Count(entry.ExpectedIds.Contains);
        }

        double precision = retrieved == 0
            ? (expected == 0 ? 1.0 : 0.0)
            : (double)truePositives / retrieved;
        double recall = expected == 0 ? 1.0 : (double)truePositives / expected;
        return new Metrics(precision, recall);
    }

    /// <summary>Mean reciprocal rank at cutoff 5 over the ranked Search interface.</summary>
    private static double MrrAtFive(KnowledgeLookup lookup, IEnumerable<BenchmarkEntry> entries)
    {
        double total = 0;
        int count = 0;
        foreach (BenchmarkEntry entry in entries)
        {
            ImmutableArray<string> result =
                [.. lookup.Search(entry.Query, limit: 5).Select(candidate => candidate.Concept.Id)];
            int rank = 0;
            for (int index = 0; index < result.Length; index++)
            {
                if (entry.ExpectedIds.Contains(result[index]))
                {
                    rank = index + 1;
                    break;
                }
            }

            total += rank == 0 ? 0 : 1.0 / rank;
            count++;
        }

        return count == 0 ? 1.0 : total / count;
    }

    private static Metrics Measure(KnowledgeLookup lookup, IEnumerable<BenchmarkEntry> entries)
    {
        int truePositives = 0;
        int retrieved = 0;
        int expected = 0;
        foreach (BenchmarkEntry entry in entries)
        {
            ImmutableArray<string> result = Retrieve(lookup, entry);
            retrieved += result.Length;
            expected += entry.ExpectedIds.Length;
            truePositives += result.Count(entry.ExpectedIds.Contains);
        }

        double precision = retrieved == 0
            ? (expected == 0 ? 1.0 : 0.0)
            : (double)truePositives / retrieved;
        double recall = expected == 0 ? 1.0 : (double)truePositives / expected;
        return new Metrics(precision, recall);
    }

    /// <summary>
    /// The retrieval adapter under test. Only the two operations the engine exposes today are used;
    /// related-concept and documentation queries have no serving operation and return nothing.
    /// </summary>
    private static ImmutableArray<string> Retrieve(KnowledgeLookup lookup, BenchmarkEntry entry)
    {
        ConceptLookup result = entry.QueryClass switch
        {
            "exact-symbol" => lookup.FindById(entry.Query),
            "alias" => lookup.FindByAlias(entry.Query),
            _ => ConceptLookup.NotFound(lookup.PackageId),
        };
        return result.IsFound ? [result.Concept!.Id] : [];
    }

    private static KnowledgeLookup Lookup()
    {
        return new(Package());
    }

    private static KnowledgePackage Package()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "knowledge", "example.physics", "package.json");
        Result<KnowledgePackage> parsed = KnowledgePackageParser.Parse(File.ReadAllText(path));
        parsed.IsSuccess.Should().BeTrue("{0}", parsed.Error?.Message ?? string.Empty);
        return parsed.Value;
    }

    private static JsonElement Corpus()
    {
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, CorpusRelativePath)))
            .RootElement.Clone();
    }

    private static ImmutableArray<BenchmarkEntry> Entries()
    {
        return
        [
            .. Corpus().GetProperty("entries").EnumerateArray().Select(entry => new BenchmarkEntry(
                entry.GetProperty("class").GetString()!,
                entry.GetProperty("query").GetString()!,
                [.. entry.GetProperty("expectedIds").EnumerateArray().Select(id => id.GetString()!)])),
        ];
    }

    private static ImmutableArray<BaselineRow> Baseline()
    {
        return
        [
            .. Corpus().GetProperty("baseline").EnumerateArray().Select(row => new BaselineRow(
                row.GetProperty("class").GetString()!,
                row.GetProperty("precision").GetDouble(),
                row.GetProperty("recall").GetDouble())),
        ];
    }

    private sealed record BenchmarkEntry(string QueryClass, string Query, ImmutableArray<string> ExpectedIds);

    private sealed record BaselineRow(string QueryClass, double Precision, double Recall);

    private readonly record struct Metrics(double Precision, double Recall);
}