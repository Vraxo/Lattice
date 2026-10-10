using System.Collections.Immutable;

namespace Lattice.Core;

public sealed class PolicySelector
{
    private readonly ImmutableArray<IActionRule> _rules;

    public PolicySelector(IEnumerable<IActionRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ImmutableArray<IActionRule> array = [.. rules];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (IActionRule rule in array)
        {
            if (string.IsNullOrWhiteSpace(rule.Id))
            {
                throw new ArgumentException("Rule id must not be empty.", nameof(rules));
            }

            if (!seen.Add(rule.Id))
            {
                throw new ArgumentException($"Duplicate rule id: {rule.Id}", nameof(rules));
            }
        }

        _rules = array;
    }

    public static PolicySelector Default { get; } = new(
    [
        new FinishWhenAllGoalsCompletedRule(),
        new ClarifyUnresolvedInterpretationRule(),
        new ContinueWhenGoalIncompleteRule(),
    ]);

    /// <summary>
    /// Selects among explicitly supplied candidates. When more than one candidate is supplied
    /// and no rule prefers one, the selection is blocked rather than choosing arbitrarily.
    /// </summary>
    /// <returns></returns>
    public ActionSelection Select(ActionSelectionContext context, IEnumerable<ActionProposal> candidates)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(candidates);
        ImmutableArray<ActionProposal> list = [.. candidates];
        if (list.IsEmpty)
        {
            return Select(context);
        }

        if (list.Length == 1)
        {
            return ActionSelection.Selected(list[0], "The only supplied candidate was selected.");
        }

        ClarificationRequest request = new(
            ClarificationKind.MultipleCandidates,
            "More than one action could satisfy the request. Which one should I take?");
        return ActionSelection.Blocked(
            new AskUserProposal(request),
            $"No rule distinguished {list.Length} supplied candidates.");
    }

    /// <summary>
    /// Selects using the registered rules alone, with no externally supplied candidates.
    /// </summary>
    /// <returns></returns>
    public ActionSelection Select(ActionSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Deterministic order: highest priority first, then rule id, so a tie is stable.
        IOrderedEnumerable<IActionRule> ordered = _rules
            .OrderByDescending(rule => rule.Priority)
            .ThenBy(rule => rule.Id, StringComparer.Ordinal);
        List<(IActionRule Rule, ActionProposal Proposal)> applicable = [];
        foreach (IActionRule? rule in ordered)
        {
            ActionProposal? proposal = rule.TryPropose(context);
            if (proposal is not null)
            {
                applicable.Add((rule, proposal));
            }
        }

        if (applicable.Count == 0)
        {
            ClarificationRequest request = new(
                ClarificationKind.NoEligibleAction,
                "No action is currently eligible. Could you provide more direction?");
            return ActionSelection.Blocked(
                new AskUserProposal(request),
                "No rule applied to the current state.");
        }

        (IActionRule? chosenRule, ActionProposal? chosenProposal) = applicable[0];
        IEnumerable<RejectedRule> rejected = applicable
            .Skip(1)
            .Select(entry => new RejectedRule(entry.Rule.Id, entry.Rule.Priority));
        return ActionSelection.Selected(
            chosenProposal,
            $"Rule '{chosenRule.Id}' (priority {chosenRule.Priority}) applied.",
            rejected);
    }
}