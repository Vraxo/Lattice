using System.Collections.Immutable;

namespace Lattice.Core;
/// <summary>
/// A discovered capability: a ready-to-execute proposal plus the descriptor evidence that
/// caused it to be selected as a candidate.
/// </summary>
public sealed record CapabilityCandidate
{
    public CapabilityCandidate(InvokeToolProposal proposal, int score, ImmutableArray<string> matchedOn)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        Proposal = proposal;
        Score = score;
        MatchedOn = matchedOn;
    }

    public InvokeToolProposal Proposal { get; }

    public int Score { get; }

    /// <summary>Gets the descriptor fields that matched, for inspection and diagnostics.</summary>
    public ImmutableArray<string> MatchedOn { get; }

    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct matched-on sets as unequal.
    public bool Equals(CapabilityCandidate? other)
    {
        if (other is null)
        {
            return false;
        }

        return Proposal == other.Proposal
            && Score == other.Score
            && MatchedOn.SequenceEqual(other.MatchedOn);
    }

    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(Proposal);
        hash.Add(Score);
        foreach (string item in MatchedOn)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }
}