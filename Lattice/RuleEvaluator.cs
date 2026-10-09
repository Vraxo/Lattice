using System.Collections.Immutable;

namespace Lattice.Core;

public static class RuleEvaluator
{
    public static ImmutableArray<Derivation> Evaluate(
        IEnumerable<KnowledgeRule> rules,
        IEnumerable<KnowledgeFact> facts)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(facts);
        ImmutableArray<KnowledgeRule> ruleArray = [.. rules];
        List<KnowledgeFact> known = [.. facts];
        HashSet<string> seenTriples = new(known.Select(TripleKey), StringComparer.Ordinal);
        ImmutableArray<Derivation>.Builder derivations = ImmutableArray.CreateBuilder<Derivation>();

        // A concrete conclusion can be derived at most once, and each pass that changes
        // nothing ends evaluation, so this bound is generous rather than load-bearing.
        int maxIterations = ruleArray.Length + 1;
        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            bool changed = false;
            foreach (KnowledgeRule rule in ruleArray)
            {
                if (!TryMatchPremises(rule, known, out ImmutableArray<string> premiseFactIds))
                {
                    continue;
                }

                KnowledgePattern conclusion = rule.Conclusion;
                if (!seenTriples.Add(TripleKey(conclusion)))
                {
                    continue;
                }

                KnowledgeFact derivedFact = new(
                    $"derived.{rule.Id}",
                    conclusion.Subject,
                    conclusion.Predicate,
                    conclusion.Value,
                    $"rule:{rule.Id}");
                known.Add(derivedFact);
                derivations.Add(new Derivation(rule.Id, premiseFactIds, derivedFact));
                changed = true;
            }

            if (!changed)
            {
                break;
            }
        }

        return derivations.ToImmutable();
    }

    private static bool TryMatchPremises(
        KnowledgeRule rule,
        List<KnowledgeFact> known,
        out ImmutableArray<string> premiseFactIds)
    {
        ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>(rule.Premises.Length);
        foreach (KnowledgePattern premise in rule.Premises)
        {
            KnowledgeFact? match = known.FirstOrDefault(fact => Matches(fact, premise));
            if (match is null)
            {
                premiseFactIds = [];
                return false;
            }

            builder.Add(match.Id);
        }

        premiseFactIds = builder.ToImmutable();
        return true;
    }

    private static bool Matches(KnowledgeFact fact, KnowledgePattern pattern)
    {
        return string.Equals(fact.Subject, pattern.Subject, StringComparison.Ordinal)
        && string.Equals(fact.Predicate, pattern.Predicate, StringComparison.Ordinal)
        && string.Equals(fact.Value, pattern.Value, StringComparison.Ordinal);
    }

    private static string TripleKey(KnowledgeFact fact)
    {
        return TripleKey(fact.Subject, fact.Predicate, fact.Value);
    }

    private static string TripleKey(KnowledgePattern pattern)
    {
        return TripleKey(pattern.Subject, pattern.Predicate, pattern.Value);
    }

    private static string TripleKey(string subject, string predicate, string value)
    {
        return $"{subject}\u0001{predicate}\u0001{value}";
    }
}