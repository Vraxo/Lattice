using System.Collections.Immutable;
namespace Lattice.Core;
public sealed class RequestInterpreter
{
    private readonly IntentPatternCatalog _catalog;
    public RequestInterpreter(IntentPatternCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        _catalog = catalog;
    }
    public RequestInterpretation Interpret(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return RequestInterpretation.Unknown(input ?? string.Empty, "Input is empty.");
        }
        var tokens = input.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var matches = new List<(int Score, IntentMatch Match)>();
        foreach (var pattern in _catalog.Patterns)
        {
            if (TryMatch(pattern, tokens, out var match))
            {
                matches.Add((pattern.Literals.Length, match));
            }
        }
        if (matches.Count == 0)
        {
            return RequestInterpretation.Unknown(input, "No known pattern matched.");
        }
        var best = matches.Max(entry => entry.Score);
        var winners = matches
            .Where(entry => entry.Score == best)
            .Select(entry => entry.Match)
            .ToImmutableArray();
        return winners.Length == 1
            ? RequestInterpretation.Matched(input, winners[0])
            : RequestInterpretation.Ambiguous(input, winners);
    }
    private static bool TryMatch(IntentPattern pattern, string[] tokens, out IntentMatch match)
    {
        match = null!;
        if (tokens.Length < pattern.Literals.Length)
        {
            return false;
        }
        for (var i = 0; i < pattern.Literals.Length; i++)
        {
            if (!string.Equals(tokens[i], pattern.Literals[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        if (pattern.SlotName is null)
        {
            if (tokens.Length != pattern.Literals.Length)
            {
                return false;
            }
            match = new IntentMatch(pattern.Id, pattern.Intent, null);
            return true;
        }
        if (tokens.Length == pattern.Literals.Length)
        {
            // The slot must absorb at least one token; a missing value is not a match.
            return false;
        }
        var value = string.Join(' ', tokens, pattern.Literals.Length, tokens.Length - pattern.Literals.Length);
        match = new IntentMatch(pattern.Id, pattern.Intent, new IntentSlot(pattern.SlotName, value));
        return true;
    }
}