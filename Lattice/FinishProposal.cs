namespace Lattice.Core;
public sealed record FinishProposal : ActionProposal
{
    public FinishProposal(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Reason = reason;
    }
    public string Reason { get; }
}