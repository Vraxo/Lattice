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

        ReadOnlySpan<char> cleaned = StripSentencePunctuation(input.AsSpan());
        TokenSpan[] tokens = Tokenize(cleaned);
        if (tokens.Length == 0)
        {
            return RequestInterpretation.Unknown(input, "No known pattern matched.");
        }

        List<(int Score, IntentMatch Match)> matches = [];
        foreach (IntentPattern pattern in _catalog.Patterns)
        {
            if (TryMatch(pattern, tokens, cleaned, out IntentMatch? match))
            {
                matches.Add((pattern.LiteralCount, match));
            }
        }

        int bestMatchScore = matches.Count > 0 ? matches.Max(entry => entry.Score) : -1;
        ImmutableArray<IntentPattern> missing =
            [.. _catalog.Patterns.Where(pattern => HasMissingValue(pattern, tokens, cleaned))];
        int bestMissingScore = missing.Length > 0 ? missing.Max(pattern => pattern.LiteralCount) : -1;

        if (bestMissingScore > bestMatchScore && bestMissingScore >= 0)
        {
            ImmutableArray<IntentPattern> missingWinners =
                [.. missing.Where(pattern => pattern.LiteralCount == bestMissingScore)];
            if (missingWinners.Length == 1)
            {
                IntentPattern pattern = missingWinners[0];
                IntentMatch match = new(pattern.Id, pattern.Intent, null);
                return RequestInterpretation.MissingValue(input, match, pattern.SlotName!);
            }

            IEnumerable<IntentMatch> candidates =
                missingWinners.Select(pattern => new IntentMatch(pattern.Id, pattern.Intent, null));
            return RequestInterpretation.Ambiguous(input, candidates);
        }

        if (matches.Count > 0)
        {
            ImmutableArray<IntentMatch> winners = [.. matches
                .Where(entry => entry.Score == bestMatchScore)
                .Select(entry => entry.Match),];
            return winners.Length == 1
                ? RequestInterpretation.Matched(input, winners[0])
                : RequestInterpretation.Ambiguous(input, winners);
        }

        return RequestInterpretation.Unknown(input, "No known pattern matched.");
    }

    private static bool HasMissingValue(
        IntentPattern pattern,
        TokenSpan[] tokens,
        ReadOnlySpan<char> cleaned)
    {
        return pattern.SlotName is not null
            && tokens.Length == pattern.LiteralCount
            && LeadsWithLiterals(pattern.Literals, tokens, cleaned)
            && EndsWithLiterals(pattern.SuffixLiterals, tokens, cleaned);
    }

    private static bool TryMatch(
        IntentPattern pattern,
        TokenSpan[] tokens,
        ReadOnlySpan<char> cleaned,
        out IntentMatch match)
    {
        match = null!;
        if (!LeadsWithLiterals(pattern.Literals, tokens, cleaned))
        {
            return false;
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

        if (tokens.Length <= pattern.LiteralCount)
        {
            // The slot must absorb at least one token; a missing value is reported separately.
            return false;
        }

        if (!EndsWithLiterals(pattern.SuffixLiterals, tokens, cleaned))
        {
            return false;
        }

        int firstSlotIndex = pattern.Literals.Length;
        int lastSlotIndex = tokens.Length - pattern.SuffixLiterals.Length - 1;
        int slotStart = tokens[firstSlotIndex].Start;
        int slotEnd = tokens[lastSlotIndex].End;
        string value = cleaned[slotStart..slotEnd].ToString();
        match = new IntentMatch(pattern.Id, pattern.Intent, new IntentSlot(pattern.SlotName, value));
        return true;
    }

    private static bool LeadsWithLiterals(
        ImmutableArray<string> literals,
        TokenSpan[] tokens,
        ReadOnlySpan<char> cleaned)
    {
        if (tokens.Length < literals.Length)
        {
            return false;
        }

        for (int i = 0; i < literals.Length; i++)
        {
            ReadOnlySpan<char> token = cleaned.Slice(tokens[i].Start, tokens[i].Length);
            if (!TokenEquals(token, literals[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool EndsWithLiterals(
        ImmutableArray<string> suffixLiterals,
        TokenSpan[] tokens,
        ReadOnlySpan<char> cleaned)
    {
        if (suffixLiterals.IsEmpty)
        {
            return true;
        }

        if (tokens.Length < suffixLiterals.Length)
        {
            return false;
        }

        int offset = tokens.Length - suffixLiterals.Length;
        for (int i = 0; i < suffixLiterals.Length; i++)
        {
            ReadOnlySpan<char> token = cleaned.Slice(tokens[offset + i].Start, tokens[offset + i].Length);
            if (!TokenEquals(token, suffixLiterals[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TokenEquals(ReadOnlySpan<char> token, string literal)
    {
        if (token.Length != literal.Length)
        {
            return false;
        }

        for (int i = 0; i < literal.Length; i++)
        {
            char left = NormalizeApostrophe(token[i]);
            char right = NormalizeApostrophe(literal[i]);
            if (char.ToUpperInvariant(left) != char.ToUpperInvariant(right))
            {
                return false;
            }
        }

        return true;
    }

    private static char NormalizeApostrophe(char ch) =>
        ch is '\u2018' or '\u2019' ? '\'' : ch;

    private static ReadOnlySpan<char> StripSentencePunctuation(ReadOnlySpan<char> input)
    {
        ReadOnlySpan<char> trimmed = input.Trim();
        while (trimmed.Length > 1 && trimmed[^1] is '?' or '!')
        {
            trimmed = trimmed[..^1].TrimEnd();
        }

        if (trimmed.Length <= 1 || trimmed[^1] != '.')
        {
            return trimmed;
        }

        int lastWhitespace = -1;
        for (int i = trimmed.Length - 1; i >= 0; i--)
        {
            if (char.IsWhiteSpace(trimmed[i]))
            {
                lastWhitespace = i;
                break;
            }
        }

        int lastTokenStart = lastWhitespace + 1;
        ReadOnlySpan<char> lastToken = trimmed[lastTokenStart..];
        if (lastToken.Length > 1 && !IsAllDots(lastToken))
        {
            while (trimmed.Length > lastTokenStart + 1 && trimmed[^1] == '.')
            {
                trimmed = trimmed[..^1];
            }

            trimmed = trimmed.TrimEnd();
        }

        return trimmed;
    }

    private static bool IsAllDots(ReadOnlySpan<char> span)
    {
        foreach (char ch in span)
        {
            if (ch != '.')
            {
                return false;
            }
        }

        return true;
    }

    private static TokenSpan[] Tokenize(ReadOnlySpan<char> span)
    {
        List<TokenSpan> tokens = [];
        int index = 0;
        while (index < span.Length)
        {
            while (index < span.Length && char.IsWhiteSpace(span[index]))
            {
                index++;
            }

            if (index >= span.Length)
            {
                break;
            }

            int start = index;
            while (index < span.Length && !char.IsWhiteSpace(span[index]))
            {
                index++;
            }

            tokens.Add(new TokenSpan(start, index - start));
        }

        return [.. tokens];
    }

    private readonly record struct TokenSpan(int Start, int Length)
    {
        public int End => Start + Length;
    }
}
