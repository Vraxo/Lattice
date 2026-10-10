using System.Collections.Immutable;

namespace Lattice.Core;

public sealed class FinishWhenAllGoalsCompletedRule : IActionRule
{
    public string Id => "finish.all-goals-completed";

    public int Priority => 100;

    public ActionProposal? TryPropose(ActionSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ImmutableArray<Goal> goals = context.Session.Goals;
        if (goals.IsDefaultOrEmpty)
        {
            return null;
        }

        foreach (Goal goal in goals)
        {
            if (goal.Status != GoalStatus.Completed)
            {
                return null;
            }
        }

        return new FinishProposal("All goals are complete.");
    }
}