namespace Lattice.Core;
public sealed class ClarifyUnresolvedInterpretationRule : IActionRule
{
    public string Id => "clarify.unresolved-interpretation";
    public int Priority => 90;
    public ActionProposal? TryPropose(ActionSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = ClarificationGenerator.TryCreate(context.Interpretation);
        return request is null ? null : new AskUserProposal(request);
    }
}