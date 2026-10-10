namespace Lattice.Core.Tests;

internal sealed class FakeRule(string id, int priority, ActionProposal? proposal) : IActionRule
{
    private readonly ActionProposal? _proposal = proposal;

    public string Id { get; } = id;

    public int Priority { get; } = priority;

    public ActionProposal? TryPropose(ActionSelectionContext context)
    {
        return _proposal;
    }
}