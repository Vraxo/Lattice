using System.Collections.Immutable;

namespace Lattice.Core;

public sealed record IntentPattern
{
    public IntentPattern(
        string id,
        RequestIntentKind intent,
        ImmutableArray<string> literals,
        string? slotName)
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

        foreach (string literal in literals)
        {
            if (string.IsNullOrWhiteSpace(literal) || literal.Contains('{') || literal.Contains('}'))
            {
                throw new ArgumentException($"Literal token '{literal}' is not valid.", nameof(literals));
            }
        }

        if (slotName is not null && string.IsNullOrWhiteSpace(slotName))
        {
            throw new ArgumentException("Slot name must not be empty when present.", nameof(slotName));
        }

        Id = id;
        Intent = intent;
        Literals = literals;
        SlotName = slotName;
    }

    public string Id { get; }

    public RequestIntentKind Intent { get; }

    public ImmutableArray<string> Literals { get; }

    public string? SlotName { get; }

    /// <summary>
    /// Splits a template such as <c>what is {topic}</c> into literal tokens and an optional
    /// trailing slot name. At most one slot is allowed and it must be the final token.
    /// </summary>
    /// <returns></returns>
    public static bool TryParseTemplate(
        string template,
        out ImmutableArray<string> literals,
        out string? slotName,
        out string? error)
    {
        literals = [];
        slotName = null;
        error = null;
        if (string.IsNullOrWhiteSpace(template))
        {
            error = "Template must not be empty.";
            return false;
        }

        string[] tokens = template.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        ImmutableArray<string>.Builder literalBuilder = ImmutableArray.CreateBuilder<string>();
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

                if (i != tokens.Length - 1)
                {
                    error = "A slot must be the final token of a pattern.";
                    return false;
                }

                slotName = name;
                continue;
            }

            literalBuilder.Add(token);
        }

        if (literalBuilder.Count == 0)
        {
            error = "A pattern must have at least one literal token.";
            return false;
        }

        literals = literalBuilder.ToImmutable();
        return true;
    }

    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct literal sets as unequal.
    public bool Equals(IntentPattern? other)
    {
        if (other is null)
        {
            return false;
        }

        return Id == other.Id
            && Intent == other.Intent
            && SlotName == other.SlotName
            && Literals.SequenceEqual(other.Literals);
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

        return hash.ToHashCode();
    }
}