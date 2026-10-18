using System.Collections.Immutable;

namespace Lattice.Core;

/// <summary>
/// Scores one concept against a query using lexical evidence only. Each factor is independent and
/// reported in the result, so the ranking can be explained. Field order is fixed so that tie-breaks
/// within a candidate are deterministic.
/// </summary>
internal static class LexicalScorer
{
    public static RetrievalCandidate Score(
        KnowledgeConcept concept,
        string packageId,
        string query,
        LexicalRetrievalWeights weights)
    {
        string trimmed = query.Trim();
        ImmutableArray<string> queryTokens = Tokenize(trimmed);
        ImmutableArray<(string Name, string Text)> fields = Fields(concept);
        (double overlapRaw, string overlapField, string overlapText) = TokenOverlap(fields, queryTokens);
        (double substringRaw, string substringField, string substringText) = Substring(fields, trimmed);

        // The matched text explains the strongest textual signal; prefer the token match when it
        // exists, otherwise fall back to the substring match, then to the preferred name.
        (string matchedField, string matchedText) = overlapRaw > 0
            ? (overlapField, overlapText)
            : substringRaw > 0
                ? (substringField, substringText)
                : ("preferred", concept.Preferred);
        ImmutableArray<RetrievalFactor> factors =
        [
            new("id-exact", weights.IdExact, Exact(concept.Id, trimmed)),
            new("preferred-exact", weights.PreferredExact, Exact(concept.Preferred, trimmed)),
            new("alias-exact", weights.AliasExact, concept.Aliases.Any(alias => string.Equals(alias, trimmed, StringComparison.OrdinalIgnoreCase)) ? 1.0 : 0.0),
            new("token-overlap", weights.TokenOverlap, overlapRaw),
            new("substring", weights.Substring, substringRaw),
        ];
        return new RetrievalCandidate(concept, packageId, factors, matchedField, matchedText);
    }

    private static double Exact(string candidate, string query)
    {
        return string.Equals(candidate, query, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
    }

    private static ImmutableArray<(string Name, string Text)> Fields(KnowledgeConcept concept)
    {
        ImmutableArray<(string, string)>.Builder builder = ImmutableArray.CreateBuilder<(string, string)>();
        if (concept.Definition is not null)
        {
            builder.Add(("definition", concept.Definition));
        }

        builder.Add(("preferred", concept.Preferred));
        foreach (string alias in concept.Aliases)
        {
            builder.Add(("alias", alias));
        }

        builder.Add(("id", concept.Id));
        return builder.ToImmutable();
    }

    private static (double Raw, string Field, string Text) TokenOverlap(
        ImmutableArray<(string Name, string Text)> fields,
        ImmutableArray<string> queryTokens)
    {
        if (queryTokens.IsEmpty)
        {
            return (0, fields[0].Name, fields[0].Text);
        }

        HashSet<string>?[] tokenSets = [.. fields.Select(field => (HashSet<string>?)Tokenize(field.Text).ToHashSet(StringComparer.Ordinal))];
        int present = queryTokens.Count(token => tokenSets.Any(set => set!.Contains(token)));
        double raw = (double)present / queryTokens.Length;
        int bestIndex = 0;
        int bestCount = -1;
        for (int index = 0; index < fields.Length; index++)
        {
            int count = queryTokens.Count(token => tokenSets[index]!.Contains(token));
            if (count > bestCount)
            {
                bestCount = count;
                bestIndex = index;
            }
        }

        return (raw, fields[bestIndex].Name, fields[bestIndex].Text);
    }

    private static (double Raw, string Field, string Text) Substring(
        ImmutableArray<(string Name, string Text)> fields,
        string query)
    {
        if (query.Length == 0)
        {
            return (0, fields[0].Name, fields[0].Text);
        }

        foreach ((string name, string text) in fields)
        {
            if (text.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                return (1.0, name, text);
            }
        }

        return (0, fields[0].Name, fields[0].Text);
    }

    private static ImmutableArray<string> Tokenize(string text)
    {
        ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>();
        HashSet<string> seen = new(StringComparer.Ordinal);
        int start = -1;
        for (int index = 0; index <= text.Length; index++)
        {
            bool boundary = index == text.Length || !char.IsLetterOrDigit(text[index]);
            if (!boundary && start < 0)
            {
                start = index;
            }
            else if (boundary && start >= 0)
            {
                string token = text[start..index].ToLowerInvariant();
                if (seen.Add(token))
                {
                    builder.Add(token);
                }

                start = -1;
            }
        }

        return builder.ToImmutable();
    }
}