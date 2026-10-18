using System.Collections.Immutable;

namespace Lattice.Core;

public sealed class KnowledgeLookup
{
    private readonly KnowledgePackage _package;

    public KnowledgeLookup(KnowledgePackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        _package = package;
    }

    public string PackageId => _package.Id;

    public ConceptLookup FindById(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ImmutableArray<KnowledgeConcept> matches = [.. _package.Concepts.Where(concept => string.Equals(concept.Id, id, StringComparison.Ordinal))];
        return Resolve(matches);
    }

    public ConceptLookup FindByAlias(string alias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);
        ImmutableArray<KnowledgeConcept> matches = [.. _package.Concepts
            .Where(concept => concept.Aliases.Any(
                candidate => string.Equals(candidate, alias, StringComparison.OrdinalIgnoreCase))),];
        return Resolve(matches);
    }

    /// <summary>
    /// Ranks the package's concepts against <paramref name="query"/> using lexical evidence and
    /// returns the top <paramref name="limit"/> candidates with positive scores. Ordering is by
    /// descending score, then by concept id ordinal so ties are deterministic.
    /// </summary>
    /// <returns></returns>
    public ImmutableArray<RetrievalCandidate> Search(
        string query,
        LexicalRetrievalWeights? weights = null,
        int limit = 5)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        LexicalRetrievalWeights effective = weights ?? LexicalRetrievalWeights.Default;
        return
        [
            .. _package.Concepts
                .Select(concept => LexicalScorer.Score(concept, _package.Id, query, effective))
                .Where(candidate => candidate.Score > 0)
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.Concept.Id, StringComparer.Ordinal)
                .Take(limit),
        ];
    }

    /// <summary>
    /// Convenience wrapper over <see cref="Search"/> returning only the highest-ranked concept.
    /// Selection reuses the ranked implementation rather than duplicating retrieval logic.
    /// </summary>
    /// <returns></returns>
    public ConceptLookup FindBest(string query, LexicalRetrievalWeights? weights = null)
    {
        ImmutableArray<RetrievalCandidate> ranked = Search(query, weights, limit: 1);
        return ranked.IsEmpty
            ? ConceptLookup.NotFound(_package.Id)
            : ConceptLookup.Found(_package.Id, ranked[0].Concept);
    }

    private ConceptLookup Resolve(ImmutableArray<KnowledgeConcept> matches)
    {
        return matches.Length switch
        {
            0 => ConceptLookup.NotFound(_package.Id),
            1 => ConceptLookup.Found(_package.Id, matches[0]),
            _ => ConceptLookup.Ambiguous(_package.Id, matches),
        };
    }
}