namespace Lattice.Core;
public sealed record AskUserProposal : ActionProposal
{
    public AskUserProposal(ClarificationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Request = request;
    }
    public ClarificationRequest Request { get; }
}