namespace Lattice.Core;

public sealed class ContinueWhenGoalIncompleteRule : IActionRule
{
    public string Id => "continue.incomplete-goal";

    public int Priority => 50;

    public ActionProposal? TryPropose(ActionSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        foreach (Goal goal in context.Session.Goals)
        {
            if (goal.Status == GoalStatus.Incomplete)
            {
                return new ContinueProposal("An incomplete goal remains.");
            }
        }

        return null;
    }
}