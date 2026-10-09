namespace Lattice.Core;
public sealed record AgentTurnResult
{
    public AgentTurnResult(
        Session session,
        TurnOutcome outcome,
        string response,
        ToolInvocation? invocation = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(response);
        Session = session;
        Outcome = outcome;
        Response = response;
        Invocation = invocation;
    }
    public Session Session { get; }
    public TurnOutcome Outcome { get; }
    public string Response { get; }
    public ToolInvocation? Invocation { get; }
}