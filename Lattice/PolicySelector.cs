using System.Collections.Immutable;
namespace Lattice.Core;
public sealed class PolicySelector
{
    private readonly ImmutableArray<IActionRule> _rules;
    public PolicySelector(IEnumerable<IActionRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var array = rules.ToImmutableArray();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var rule in array)
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
    public static PolicySelector Default { get; } = new(new IActionRule[]
    {
        new FinishWhenAllGoalsCompletedRule(),
        new ClarifyUnresolvedInterpretationRule(),
        new ContinueWhenGoalIncompleteRule(),
    });
    public ActionSelection Select(ActionSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        // Deterministic order: highest priority first, then rule id, so a tie is stable.
        var ordered = _rules
            .OrderByDescending(rule => rule.Priority)
            .ThenBy(rule => rule.Id, StringComparer.Ordinal);
        var applicable = new List<(IActionRule Rule, ActionProposal Proposal)>();
        foreach (var rule in ordered)
        {
            var proposal = rule.TryPropose(context);
            if (proposal is not null)
            {
                applicable.Add((rule, proposal));
            }
        }
        if (applicable.Count == 0)
        {
            var request = new ClarificationRequest(
                ClarificationKind.NoEligibleAction,
                "No action is currently eligible. Could you provide more direction?");
            return ActionSelection.Blocked(
                new AskUserProposal(request),
                "No rule applied to the current state.");
        }
        var (chosenRule, chosenProposal) = applicable[0];
        var rejected = applicable
            .Skip(1)
            .Select(entry => new RejectedRule(entry.Rule.Id, entry.Rule.Priority));
        return ActionSelection.Selected(
            chosenProposal,
            $"Rule '{chosenRule.Id}' (priority {chosenRule.Priority}) applied.",
            rejected);
    }
}