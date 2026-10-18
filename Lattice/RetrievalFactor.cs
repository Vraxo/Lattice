namespace Lattice.Core;
/// <summary>
/// One named contribution to a retrieval candidate's ranking score. A factor's contribution is
/// <see cref="Weight"/> times <see cref="RawScore"/>; the candidate's total score is the sum of its
/// factors' contributions, so a ranking is explainable without re-deriving the arithmetic.
/// </summary>
public sealed record RetrievalFactor
{
    public RetrievalFactor(string name, double weight, double rawScore)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(weight);
        ArgumentOutOfRangeException.ThrowIfNegative(rawScore);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(rawScore, 1.0);
        Name = name;
        Weight = weight;
        RawScore = rawScore;
    }

    public string Name { get; }

    public double Weight { get; }

    /// <summary>Gets normalised match strength for this factor, in the range 0..1.</summary>
    public double RawScore { get; }

    public double Contribution => Weight * RawScore;
}