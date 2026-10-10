using System.Collections.Immutable;

namespace Lattice.Core;

public sealed record ActionSelection
{
    private ActionSelection(
        ActionSelectionKind kind,
        ActionProposal proposal,
        string reason,
        ImmutableArray<RejectedRule> rejectedRules)
    {
        Kind = kind;
        Proposal = proposal;
        Reason = reason;
        RejectedRules = rejectedRules;
    }

    public ActionSelectionKind Kind { get; }

    public ActionProposal Proposal { get; }

    public string Reason { get; }

    public ImmutableArray<RejectedRule> RejectedRules { get; }

    public bool IsSelected => Kind == ActionSelectionKind.Selected;

    public static ActionSelection Selected(
        ActionProposal proposal,
        string reason,
        IEnumerable<RejectedRule>? rejectedRules = null)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return new ActionSelection(
            ActionSelectionKind.Selected,
            proposal,
            reason,
            rejectedRules?.ToImmutableArray() ?? []);
    }

    public static ActionSelection Blocked(ActionProposal proposal, string reason)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return new ActionSelection(ActionSelectionKind.Blocked, proposal, reason, []);
    }

    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct rejected sets as unequal.
    public bool Equals(ActionSelection? other)
    {
        if (other is null)
        {
            return false;
        }

        return Kind == other.Kind
            && Proposal == other.Proposal
            && Reason == other.Reason
            && RejectedRules.SequenceEqual(other.RejectedRules);
    }

    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(Kind);
        hash.Add(Proposal);
        hash.Add(Reason);
        foreach (RejectedRule rejected in RejectedRules)
        {
            hash.Add(rejected);
        }

        return hash.ToHashCode();
    }
}