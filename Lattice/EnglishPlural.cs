namespace Lattice.Core;
/// <summary>
/// Formats a non-negative count with an English noun, choosing the singular form for exactly one.
/// Used when filling response-template slots so that generated text is grammatical without each
/// template handling pluralization itself.
/// </summary>
public static class EnglishPlural
{
    public static string Of(int count, string singular, string plural)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentException.ThrowIfNullOrWhiteSpace(singular);
        ArgumentException.ThrowIfNullOrWhiteSpace(plural);
        return count == 1 ? $"1 {singular}" : $"{count} {plural}";
    }
}