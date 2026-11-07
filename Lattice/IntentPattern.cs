using System.Collections.Immutable;

namespace Lattice.Core;

public sealed record IntentPattern
{
    public IntentPattern(
        string id,
        RequestIntentKind intent,
        ImmutableArray<string> literals,
        string? slotName)
        : this(id, intent, literals, slotName, [])
    {
    }

    public IntentPattern(
        string id,
        RequestIntentKind intent,
        ImmutableArray<string> literals,
        string? slotName,
        ImmutableArray<string> suffixLiterals)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Pattern id must not be empty.", nameof(id));
        }

        if (intent == RequestIntentKind.Unknown)
        {
            throw new ArgumentException("A pattern must target a known intent.", nameof(intent));
        }

        if (literals.IsDefaultOrEmpty)
        {
            throw new ArgumentException("A pattern must have at least one literal token.", nameof(literals));
        }

        ValidateLiterals(literals, nameof(literals));

        if (slotName is not null && string.IsNullOrWhiteSpace(slotName))
        {
            throw new ArgumentException("Slot name must not be empty when present.", nameof(slotName));
        }

        ImmutableArray<string> normalizedSuffix = suffixLiterals.IsDefault ? [] : suffixLiterals;
        if (slotName is null && !normalizedSuffix.IsEmpty)
        {
            throw new ArgumentException("Suffix literals require a capture slot.", nameof(suffixLiterals));
        }

        ValidateLiterals(normalizedSuffix, nameof(suffixLiterals));

        Id = id;
        Intent = intent;
        Literals = literals;
        SlotName = slotName;
        SuffixLiterals = normalizedSuffix;
    }

    public string Id { get; }

    public RequestIntentKind Intent { get; }

    public ImmutableArray<string> Literals { get; }

    public string? SlotName { get; }

    public ImmutableArray<string> SuffixLiterals { get; }

    public int LiteralCount => Literals.Length + SuffixLiterals.Length;

    /// <summary>
    /// Splits a template such as <c>what is {topic}</c> into literal tokens and an optional
    /// trailing slot name. At most one slot is allowed and it must be the final token.
    /// </summary>
    public static bool TryParseTemplate(
        string template,
        out ImmutableArray<string> literals,
        out string? slotName,
        out string? error)
    {
        if (!TryParseTemplate(
            template,
            out literals,
            out slotName,
            out ImmutableArray<string> suffixLiterals,
            out error))
        {
            return false;
        }

        if (!suffixLiterals.IsEmpty)
        {
            literals = [];
            slotName = null;
            error = "A slot must be the final token of a pattern.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Splits a template such as <c>where does {symbol} appear</c> into leading literal tokens,
    /// an optional single capture slot, and optional trailing literal tokens.
    /// </summary>
    public static bool TryParseTemplate(
        string template,
        out ImmutableArray<string> literals,
        out string? slotName,
        out ImmutableArray<string> suffixLiterals,
        out string? error)
    {
        literals = [];
        slotName = null;
        suffixLiterals = [];
        error = null;
        if (string.IsNullOrWhiteSpace(template))
        {
            error = "Template must not be empty.";
            return false;
        }

        string[] tokens = template.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        ImmutableArray<string>.Builder prefixBuilder = ImmutableArray.CreateBuilder<string>();
        ImmutableArray<string>.Builder suffixBuilder = ImmutableArray.CreateBuilder<string>();
        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];
            if (token.StartsWith('{') || token.EndsWith('}'))
            {
                if (!token.StartsWith('{') || !token.EndsWith('}') || token.Length < 3)
                {
                    error = $"Token '{token}' is not a valid slot.";
                    return false;
                }

                string name = token[1..^1];
                if (name.Length == 0 || !char.IsAsciiLetter(name[0]))
                {
                    error = $"Slot name '{name}' must start with a letter.";
                    return false;
                }

                if (slotName is not null)
                {
                    error = "A pattern may contain at most one slot.";
                    return false;
                }

                if (prefixBuilder.Count == 0)
                {
                    error = "A pattern must start with at least one literal token before its slot.";
                    return false;
                }

                slotName = name;
                continue;
            }

            if (slotName is null)
            {
                prefixBuilder.Add(token);
            }
            else
            {
                suffixBuilder.Add(token);
            }
        }

        if (prefixBuilder.Count == 0)
        {
            error = "A pattern must have at least one literal token.";
            return false;
        }

        literals = prefixBuilder.ToImmutable();
        suffixLiterals = suffixBuilder.ToImmutable();
        return true;
    }

    public bool Equals(IntentPattern? other)
    {
        if (other is null)
        {
            return false;
        }

        return Id == other.Id
            && Intent == other.Intent
            && SlotName == other.SlotName
            && Literals.SequenceEqual(other.Literals)
            && SuffixLiterals.SequenceEqual(other.SuffixLiterals);
    }

    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(Id);
        hash.Add(Intent);
        hash.Add(SlotName);
        foreach (string literal in Literals)
        {
            hash.Add(literal);
        }

        foreach (string suffix in SuffixLiterals)
        {
            hash.Add(suffix);
        }

        return hash.ToHashCode();
    }

    private static void ValidateLiterals(ImmutableArray<string> tokens, string paramName)
    {
        foreach (string literal in tokens)
        {
            if (string.IsNullOrWhiteSpace(literal) || literal.Contains('{') || literal.Contains('}'))
            {
                throw new ArgumentException($"Literal token '{literal}' is not valid.", paramName);
            }
        }
    }
}
