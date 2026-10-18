namespace Lattice.Core;
/// <summary>
/// Weights for the lexical scoring factors. All weights are non-negative and at least one is
/// positive. Defaults are ordered by strength of evidence: an exact id is the strongest signal, an
/// exact alias the weakest of the exact matches, and token/substring matches are weaker still.
/// </summary>
public sealed record LexicalRetrievalWeights
{
    public static LexicalRetrievalWeights Default { get; } = new();

    public LexicalRetrievalWeights(
        double idExact = 100,
        double preferredExact = 90,
        double aliasExact = 80,
        double tokenOverlap = 40,
        double substring = 20)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(idExact);
        ArgumentOutOfRangeException.ThrowIfNegative(preferredExact);
        ArgumentOutOfRangeException.ThrowIfNegative(aliasExact);
        ArgumentOutOfRangeException.ThrowIfNegative(tokenOverlap);
        ArgumentOutOfRangeException.ThrowIfNegative(substring);
        if (idExact + preferredExact + aliasExact + tokenOverlap + substring <= 0)
        {
            throw new ArgumentException("At least one retrieval weight must be positive.");
        }

        IdExact = idExact;
        PreferredExact = preferredExact;
        AliasExact = aliasExact;
        TokenOverlap = tokenOverlap;
        Substring = substring;
    }

    public double IdExact { get; }

    public double PreferredExact { get; }

    public double AliasExact { get; }

    public double TokenOverlap { get; }

    public double Substring { get; }
}