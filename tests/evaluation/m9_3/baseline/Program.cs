using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Lattice.Core;

// M9.3 evaluation-only baseline harness. This project is deliberately outside Lattice.slnx and
// outside the normal test execution path. It calls the real M9.2 KnowledgeLookup.Search and uses
// reflection only to observe the internal LexicalScorer tokenizer/factors for overlap diagnostics.
// It does not modify production retrieval behavior.

string repoRoot = args.Length > 0 ? Path.GetFullPath(args[0]) : Directory.GetCurrentDirectory();
string outputPath = args.Length > 1
    ? Path.GetFullPath(args[1])
    : Path.Combine(repoRoot, "artifacts", "evaluation", "m9_3", "baseline.json");

string fixturePath = Path.Combine(repoRoot, "tests", "corpus", "retrieval", "paraphrases", "v1.json");
string corpusPath = Path.Combine(repoRoot, "tests", "corpus", "retrieval", "v1.json");
string physicsPath = Path.Combine(repoRoot, "knowledge", "example.physics", "package.json");

using JsonDocument fixture = JsonDocument.Parse(File.ReadAllText(fixturePath));
using JsonDocument corpus = JsonDocument.Parse(File.ReadAllText(corpusPath));
using JsonDocument physics = JsonDocument.Parse(File.ReadAllText(physicsPath));

JsonElement fixtureRoot = fixture.RootElement;
if (fixtureRoot.GetProperty("reviewStatus").GetString() != "human-reviewed")
{
    throw new InvalidOperationException("The paraphrase fixture must be human-reviewed before baseline measurement.");
}

KnowledgePackage paraphrasePackage = LoadConcepts(fixtureRoot, "M9.3 documentation paraphrase pilot");
KnowledgePackage physicsPackage = LoadConcepts(physics.RootElement, "Basic Physics Example");
KnowledgeLookup paraphraseLookup = new(paraphrasePackage);
KnowledgeLookup physicsLookup = new(physicsPackage);

Assembly coreAssembly = typeof(KnowledgeLookup).Assembly;
Type scorerType = coreAssembly.GetType("Lattice.Core.LexicalScorer")
    ?? throw new InvalidOperationException("Lattice.Core.LexicalScorer was not found.");
MethodInfo scoreMethod = scorerType.GetMethod("Score", BindingFlags.Public | BindingFlags.Static)
    ?? throw new InvalidOperationException("LexicalScorer.Score was not found.");
MethodInfo tokenizeMethod = scorerType.GetMethod("Tokenize", BindingFlags.NonPublic | BindingFlags.Static)
    ?? throw new InvalidOperationException("LexicalScorer.Tokenize was not found.");

List<object> paraphraseRows = [];
List<string> lowOverlapIds = [];
foreach (JsonElement entry in fixtureRoot.GetProperty("entries").EnumerateArray())
{
    string id = entry.GetProperty("id").GetString()!;
    string query = entry.GetProperty("query").GetString()!;
    string[] expectedIds = entry.GetProperty("expectedIds").EnumerateArray().Select(value => value.GetString()!).ToArray();
    if (expectedIds.Length != 1)
    {
        throw new InvalidOperationException($"M9.3 pilot entry {id} must have exactly one relevant target.");
    }

    string expectedId = expectedIds[0];
    KnowledgeConcept target = paraphrasePackage.Concepts.Single(concept => concept.Id == expectedId);
    string[] queryTokens = Tokenize(query.Trim());
    string[][] fieldTokens = Fields(target).Select(field => Tokenize(field)).ToArray();
    HashSet<string> allFieldTokens = fieldTokens.SelectMany(tokens => tokens).ToHashSet(StringComparer.Ordinal);
    string[] matchedTokens = queryTokens.Where(allFieldTokens.Contains).ToArray();
    RetrievalCandidate scored = Score(target, paraphrasePackage.Id, query);
    double overlapRatio = scored.Factors.Single(factor => factor.Name == "token-overlap").RawScore;
    double recomputed = queryTokens.Length == 0 ? 0.0 : (double)matchedTokens.Length / queryTokens.Length;
    if (Math.Abs(overlapRatio - recomputed) > 1e-12)
    {
        throw new InvalidOperationException($"Overlap diagnostic disagrees with LexicalScorer for {id}.");
    }

    ImmutableArray<RetrievalCandidate> top = paraphraseLookup.Search(query, limit: 5);
    int rank = RankOf(top, expectedId);
    bool lowOverlap = overlapRatio <= 0.25;
    if (lowOverlap)
    {
        lowOverlapIds.Add(id);
    }

    paraphraseRows.Add(new
    {
        id,
        query,
        expectedId,
        queryTokens,
        queryTokenCount = queryTokens.Length,
        matchedTokens,
        matchedTokenCount = matchedTokens.Length,
        overlapRatio,
        lowOverlap,
        lexicalScore = scored.Score,
        lexicalFactors = scored.Factors.Select(factor => new { factor.Name, factor.Weight, factor.RawScore, factor.Contribution }),
        top5 = top.Select(candidate => new { candidate.Concept.Id, candidate.Source, candidate.Score }),
        rank = rank == 0 ? (int?)null : rank,
        reciprocalRank = rank == 0 ? 0.0 : 1.0 / rank,
        hitAt5 = rank > 0,
    });
}

int lowOverlapTargetCount = paraphraseRows
    .Where(row => (bool)row.GetType().GetProperty("lowOverlap")!.GetValue(row)!)
    .Select(row => (string)row.GetType().GetProperty("expectedId")!.GetValue(row)!)
    .Distinct(StringComparer.Ordinal)
    .Count();
// The subset-size gate is asserted after the JSON is written so a failure still
// leaves the per-query overlap diagnostics available for inspection.
bool subsetGatePass = lowOverlapIds.Count >= 20 && lowOverlapTargetCount >= 5;

List<object> corpusRows = [];
foreach (JsonElement entry in corpus.RootElement.GetProperty("entries").EnumerateArray())
{
    string id = entry.GetProperty("id").GetString()!;
    string query = entry.GetProperty("query").GetString()!;
    string queryClass = entry.GetProperty("class").GetString()!;
    string[] expectedIds = entry.GetProperty("expectedIds").EnumerateArray().Select(value => value.GetString()!).ToArray();
    ImmutableArray<RetrievalCandidate> top = physicsLookup.Search(query, limit: 5);
    int rank = RankOfAny(top, expectedIds);
    corpusRows.Add(new
    {
        id,
        query,
        queryClass,
        expectedIds,
        top5 = top.Select(candidate => new { candidate.Concept.Id, candidate.Source, candidate.Score }),
        rank = rank == 0 ? (int?)null : rank,
        reciprocalRank = rank == 0 ? 0.0 : 1.0 / rank,
        hitAt5 = rank > 0,
    });
}

List<object> exactControls = [];
foreach (JsonElement entry in corpus.RootElement.GetProperty("entries").EnumerateArray())
{
    string queryClass = entry.GetProperty("class").GetString()!;
    if (queryClass is "exact-symbol" or "alias")
    {
        string id = entry.GetProperty("id").GetString()!;
        string query = entry.GetProperty("query").GetString()!;
        string expectedId = entry.GetProperty("expectedIds")[0].GetString()!;
        ImmutableArray<RetrievalCandidate> top = physicsLookup.Search(query, limit: 5);
        int rank = RankOf(top, expectedId);
        exactControls.Add(new
        {
            id,
            query,
            queryClass,
            expectedId,
            rank = rank == 0 ? (int?)null : rank,
            reciprocalRank = rank == 0 ? 0.0 : 1.0 / rank,
            hitAt5 = rank > 0,
            top1Exact = top.Length > 0 && top[0].Concept.Id == expectedId,
        });
    }
}

List<object> generatedExactControls = [];
foreach ((KnowledgePackage package, KnowledgeLookup lookup) in new[] { (paraphrasePackage, paraphraseLookup), (physicsPackage, physicsLookup) })
{
    foreach (KnowledgeConcept concept in package.Concepts)
    {
        foreach ((string kind, string query) in ExactQueries(concept))
        {
            ImmutableArray<RetrievalCandidate> top = lookup.Search(query, limit: 5);
            int rank = RankOf(top, concept.Id);
            generatedExactControls.Add(new
            {
                packageId = package.Id,
                conceptId = concept.Id,
                kind,
                query,
                rank = rank == 0 ? (int?)null : rank,
                reciprocalRank = rank == 0 ? 0.0 : 1.0 / rank,
                hitAt5 = rank > 0,
                top1Exact = top.Length > 0 && top[0].Concept.Id == concept.Id,
            });
        }
    }
}

// Warm up, then measure the real lexical Search path. This is operational evidence, not a gate.
List<string> allQueries = paraphraseRows.Select(row => (string)row.GetType().GetProperty("query")!.GetValue(row)!).ToList();
allQueries.AddRange(corpusRows.Select(row => (string)row.GetType().GetProperty("query")!.GetValue(row)!));
foreach (string query in allQueries.Take(10))
{
    paraphraseLookup.Search(query, limit: 5);
    physicsLookup.Search(query, limit: 5);
}

List<double> lexicalMilliseconds = [];
foreach (string query in allQueries)
{
    long before = Stopwatch.GetTimestamp();
    paraphraseLookup.Search(query, limit: 5);
    physicsLookup.Search(query, limit: 5);
    long after = Stopwatch.GetTimestamp();
    lexicalMilliseconds.Add((after - before) * 1000.0 / Stopwatch.Frequency);
}

lexicalMilliseconds.Sort();

object result = new
{
    generator = "Lattice.M93.Baseline",
    implementation = "Lattice.Core KnowledgeLookup.Search + LexicalScorer (reflection for diagnostics only)",
    dotnetVersion = Environment.Version.ToString(),
    runtimeIdentifier = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier,
    fixtureSha256 = Sha256(fixturePath),
    corpusSha256 = Sha256(corpusPath),
    physicsPackageSha256 = Sha256(physicsPath),
    fixtureReviewStatus = fixtureRoot.GetProperty("reviewStatus").GetString(),
    lowOverlapThreshold = 0.25,
    lowOverlapIds,
    lowOverlapSubsetGate = new
    {
        requiredQueries = 20,
        requiredTargets = 5,
        actualQueries = lowOverlapIds.Count,
        actualTargets = lowOverlapTargetCount,
        pass = subsetGatePass,
    },
    paraphrase = new
    {
        packageId = paraphrasePackage.Id,
        conceptCount = paraphrasePackage.Concepts.Length,
        queryCount = paraphraseRows.Count,
        all = Metrics(paraphraseRows),
        lowOverlap = Metrics(paraphraseRows.Where(row => (bool)row.GetType().GetProperty("lowOverlap")!.GetValue(row)!)),
        rows = paraphraseRows,
    },
    existingBenchmark = new
    {
        packageId = physicsPackage.Id,
        queryCount = corpusRows.Count,
        all = Metrics(corpusRows),
        byClass = corpusRows
            .GroupBy(row => (string)row.GetType().GetProperty("queryClass")!.GetValue(row)!)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new { queryClass = group.Key, metrics = Metrics(group) }),
        rows = corpusRows,
    },
    exactMatchControls = new
    {
        existing = new { rows = exactControls, metrics = Metrics(exactControls) },
        generatedIdentifierNameAlias = new { rows = generatedExactControls, metrics = Metrics(generatedExactControls) },
    },
    operational = new
    {
        lexicalSearchMilliseconds = new
        {
            samples = lexicalMilliseconds.Count,
            median = Percentile(lexicalMilliseconds, 0.50),
            p95 = Percentile(lexicalMilliseconds, 0.95),
            max = lexicalMilliseconds.Count == 0 ? 0.0 : lexicalMilliseconds[^1],
        },
        workingSetBytes = Environment.WorkingSet,
        managedHeapBytes = GC.GetTotalMemory(forceFullCollection: true),
        processorCount = Environment.ProcessorCount,
    },
};

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Wrote {outputPath}");
Console.WriteLine($"Low-overlap queries: {lowOverlapIds.Count}/{paraphraseRows.Count}");
Console.WriteLine(JsonSerializer.Serialize(new
{
    paraphraseAll = Metrics(paraphraseRows),
    paraphraseLowOverlap = Metrics(paraphraseRows.Where(row => (bool)row.GetType().GetProperty("lowOverlap")!.GetValue(row)!)),
    existingAll = Metrics(corpusRows),
    exactExisting = Metrics(exactControls),
    exactGenerated = Metrics(generatedExactControls),
}, new JsonSerializerOptions { WriteIndented = true }));

if (!subsetGatePass)
{
    throw new InvalidOperationException(
        $"The frozen low-overlap subset must contain at least 20 queries and 5 targets; got {lowOverlapIds.Count} queries and {lowOverlapTargetCount} targets. Diagnostics were written to {outputPath}.");
}

KnowledgePackage LoadConcepts(JsonElement root, string name)
{
    List<KnowledgeConcept> concepts = [];
    foreach (JsonElement concept in root.GetProperty("concepts").EnumerateArray())
    {
        string[] aliases = concept.TryGetProperty("aliases", out JsonElement aliasElement)
            ? aliasElement.EnumerateArray().Select(value => value.GetString()!).ToArray()
            : [];
        string? definition = concept.TryGetProperty("definition", out JsonElement definitionElement)
            ? definitionElement.GetString()
            : null;
        concepts.Add(new KnowledgeConcept(
            concept.GetProperty("id").GetString()!,
            concept.GetProperty("preferred").GetString()!,
            aliases,
            definition));
    }

    string packageId = root.TryGetProperty("packageId", out JsonElement packageIdElement)
        ? packageIdElement.GetString()!
        : root.GetProperty("id").GetString()!;
    return new KnowledgePackage(new SchemaVersion(1, 0), packageId, name, concepts: concepts);
}

string[] Tokenize(string text)
{
    object? result = tokenizeMethod.Invoke(null, [text]);
    return ((System.Collections.IEnumerable)result!).Cast<string>().ToArray();
}

RetrievalCandidate Score(KnowledgeConcept concept, string packageId, string query)
{
    return (RetrievalCandidate)scoreMethod.Invoke(null, [concept, packageId, query, LexicalRetrievalWeights.Default])!;
}

static IEnumerable<string> Fields(KnowledgeConcept concept)
{
    if (concept.Definition is not null)
    {
        yield return concept.Definition;
    }

    yield return concept.Preferred;
    foreach (string alias in concept.Aliases)
    {
        yield return alias;
    }

    yield return concept.Id;
}

static IEnumerable<(string Kind, string Query)> ExactQueries(KnowledgeConcept concept)
{
    yield return ("id", concept.Id);
    yield return ("preferred", concept.Preferred);
    foreach (string alias in concept.Aliases)
    {
        yield return ("alias", alias);
    }
}

static int RankOf(ImmutableArray<RetrievalCandidate> candidates, string expectedId)
{
    for (int index = 0; index < candidates.Length; index++)
    {
        if (candidates[index].Concept.Id == expectedId)
        {
            return index + 1;
        }
    }

    return 0;
}

static int RankOfAny(ImmutableArray<RetrievalCandidate> candidates, string[] expectedIds)
{
    for (int index = 0; index < candidates.Length; index++)
    {
        if (expectedIds.Contains(candidates[index].Concept.Id))
        {
            return index + 1;
        }
    }

    return 0;
}

static object Metrics(IEnumerable<object> rows)
{
    List<object> materialized = rows.ToList();
    int count = materialized.Count;
    int hits = materialized.Count(row => (bool)row.GetType().GetProperty("hitAt5")!.GetValue(row)!);
    double mrr = count == 0 ? 0.0 : materialized.Sum(row => (double)row.GetType().GetProperty("reciprocalRank")!.GetValue(row)!) / count;
    return new
    {
        queryCount = count,
        hitsAt5 = hits,
        recallAt5 = count == 0 ? 0.0 : (double)hits / count,
        mrrAt5 = mrr,
    };
}

static double Percentile(List<double> sorted, double percentile)
{
    if (sorted.Count == 0)
    {
        return 0.0;
    }

    double index = (sorted.Count - 1) * percentile;
    int lower = (int)Math.Floor(index);
    int upper = (int)Math.Ceiling(index);
    return sorted[lower] + ((sorted[upper] - sorted[lower]) * (index - lower));
}

static string Sha256(string path)
{
    using FileStream stream = File.OpenRead(path);
    return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
}
