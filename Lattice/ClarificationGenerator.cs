using System.Collections.Immutable;
namespace Lattice.Core;
public static class ClarificationGenerator
{
    /// <summary>
    /// Produces a clarifying question for an interpretation that is not confidently matched.
    /// Returns <see langword="null"/> when the interpretation needs no clarification.
    /// </summary>
    public static ClarificationRequest? TryCreate(RequestInterpretation interpretation)
    {
        ArgumentNullException.ThrowIfNull(interpretation);
        return interpretation.Kind switch
        {
            IntentMatchKind.Matched => null,
            IntentMatchKind.MissingValue => new ClarificationRequest(
                ClarificationKind.MissingValue,
                $"What {interpretation.MissingSlotName}?"),
            IntentMatchKind.Ambiguous => new ClarificationRequest(
                ClarificationKind.AmbiguousIntent,
                BuildAmbiguousQuestion(interpretation.Candidates)),
            IntentMatchKind.Unknown => new ClarificationRequest(
                ClarificationKind.UnknownIntent,
                "I did not understand that request. Could you rephrase it?"),
            _ => throw new InvalidOperationException($"Unhandled interpretation kind '{interpretation.Kind}'."),
        };
    }
    private static string BuildAmbiguousQuestion(ImmutableArray<IntentMatch> candidates)
    {
        var intents = candidates
            .Select(candidate => candidate.Intent)
            .Distinct()
            .OrderBy(intent => (int)intent)
            .ToImmutableArray();
        if (intents.Length < 2)
        {
            return "I found more than one possible reading. Could you be more specific?";
        }
        var phrases = intents.Select(Describe).ToImmutableArray();
        return phrases.Length == 2
            ? $"Did you mean {phrases[0]} or {phrases[1]}?"
            : $"Did you mean {string.Join(", ", phrases.Take(phrases.Length - 1))}, or {phrases[^1]}?";
    }
    private static string Describe(RequestIntentKind intent) => intent switch
    {
        RequestIntentKind.Question => "a question",
        RequestIntentKind.Explanation => "an explanation",
        RequestIntentKind.FindSymbol => "a symbol lookup",
        RequestIntentKind.InspectPath => "a file inspection",
        _ => "a request",
    };
}