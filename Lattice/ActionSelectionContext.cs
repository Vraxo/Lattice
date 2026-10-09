namespace Lattice.Core;
/// <summary>Inputs a policy rule may consult when deciding whether it applies.</summary>
public sealed record ActionSelectionContext
{
    public ActionSelectionContext(Session session, RequestInterpretation interpretation)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(interpretation);
        Session = session;
        Interpretation = interpretation;
    }
    public Session Session { get; }
    public RequestInterpretation Interpretation { get; }
}