namespace Lattice.Core;
/// <summary>
/// Proposes another bounded step, optionally revising the current plan. The distinction
/// between continuing and replanning is not modelled separately in v1.
/// </summary>
public sealed record ContinueProposal : ActionProposal
{
    public ContinueProposal(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Reason = reason;
    }
    public string Reason { get; }
}