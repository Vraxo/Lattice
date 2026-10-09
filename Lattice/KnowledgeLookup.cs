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