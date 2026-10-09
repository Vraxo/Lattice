namespace Lattice.Core.Tests;
internal sealed class FakeRule : IActionRule
{
    private readonly ActionProposal? _proposal;
    public FakeRule(string id, int priority, ActionProposal? proposal)
    {
        Id = id;
        Priority = priority;
        _proposal = proposal;
    }
    public string Id { get; }
    public int Priority { get; }
    public ActionProposal? TryPropose(ActionSelectionContext context) => _proposal;
}