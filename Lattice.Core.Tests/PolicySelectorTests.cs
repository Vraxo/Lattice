namespace Lattice.Core.Tests;

public sealed class PolicySelectorTests
{
    [Fact]
    public void AllGoalsCompletedFinishes()
    {
        ActionSelectionContext context = Context(
            goals: [GoalWith(GoalStatus.Completed)],
            interpretation: Matched());
        ActionSelection selection = PolicySelector.Default.Select(context);
        Assert.True(selection.IsSelected);
        Assert.IsType<FinishProposal>(selection.Proposal);
    }

    [Fact]
    public void UnresolvedInterpretationAsksUser()
    {
        ActionSelectionContext context = Context(interpretation: Unknown());
        ActionSelection selection = PolicySelector.Default.Select(context);
        Assert.True(selection.IsSelected);
        Assert.IsType<AskUserProposal>(selection.Proposal);
    }

    [Fact]
    public void IncompleteGoalContinues()
    {
        ActionSelectionContext context = Context(
            goals: [GoalWith(GoalStatus.Incomplete)],
            interpretation: Matched());
        ActionSelection selection = PolicySelector.Default.Select(context);
        Assert.True(selection.IsSelected);
        Assert.IsType<ContinueProposal>(selection.Proposal);
    }

    [Fact]
    public void BlockedGoalWithNoOtherRuleIsBlocked()
    {
        // A blocked goal matches no rule: finish needs all completed, continue needs incomplete.
        ActionSelectionContext context = Context(
            goals: [GoalWith(GoalStatus.Blocked)],
            interpretation: Matched());
        ActionSelection selection = PolicySelector.Default.Select(context);
        Assert.False(selection.IsSelected);
        Assert.Equal(ActionSelectionKind.Blocked, selection.Kind);
        AskUserProposal ask = Assert.IsType<AskUserProposal>(selection.Proposal);
        Assert.Equal(ClarificationKind.NoEligibleAction, ask.Request.Kind);
    }

    [Fact]
    public void NoGoalsAndMatchedRequestIsBlockedNotCrashed()
    {
        ActionSelectionContext context = Context(interpretation: Matched());
        ActionSelection selection = PolicySelector.Default.Select(context);
        Assert.False(selection.IsSelected);
        Assert.IsType<AskUserProposal>(selection.Proposal);
    }

    [Fact]
    public void InterpretationIsClarifiedBeforeContinuingIncompleteGoal()
    {
        ActionSelectionContext context = Context(
            goals: [GoalWith(GoalStatus.Incomplete)],
            interpretation: Unknown());
        ActionSelection selection = PolicySelector.Default.Select(context);

        // Clarify (priority 90) outranks continue (priority 50).
        Assert.IsType<AskUserProposal>(selection.Proposal);
    }

    [Fact]
    public void CompletionOutranksClarification()
    {
        ActionSelectionContext context = Context(
            goals: [GoalWith(GoalStatus.Completed)],
            interpretation: Unknown());
        ActionSelection selection = PolicySelector.Default.Select(context);

        // Finish (priority 100) outranks clarify (priority 90).
        Assert.IsType<FinishProposal>(selection.Proposal);
    }

    [Fact]
    public void HigherPriorityRuleWins()
    {
        PolicySelector selector = new(
        [
            new FakeRule("low", 10, new RespondProposal("low")),
            new FakeRule("high", 20, new RespondProposal("high")),
        ]);
        ActionSelection selection = selector.Select(Context());
        Assert.Equal(new RespondProposal("high"), selection.Proposal);
    }

    [Fact]
    public void RejectedRulesAreRecorded()
    {
        PolicySelector selector = new(
        [
            new FakeRule("low", 10, new RespondProposal("low")),
            new FakeRule("high", 20, new RespondProposal("high")),
        ]);
        ActionSelection selection = selector.Select(Context());
        RejectedRule rejected = Assert.Single(selection.RejectedRules);
        Assert.Equal("low", rejected.RuleId);
        Assert.Equal(10, rejected.Priority);
    }

    [Fact]
    public void SamePriorityTieIsBrokenDeterministicallyById()
    {
        PolicySelector selector = new(
        [
            new FakeRule("b", 10, new RespondProposal("b")),
            new FakeRule("a", 10, new RespondProposal("a")),
        ]);
        ActionSelection selection = selector.Select(Context());
        Assert.Equal(new RespondProposal("a"), selection.Proposal);
    }

    [Fact]
    public void InapplicableRulesAreSkipped()
    {
        PolicySelector selector = new(
        [
            new FakeRule("never", 100, null),
            new FakeRule("always", 10, new RespondProposal("always")),
        ]);
        ActionSelection selection = selector.Select(Context());
        Assert.Equal(new RespondProposal("always"), selection.Proposal);
    }

    [Fact]
    public void NoRulesProducesBlockedSelection()
    {
        PolicySelector selector = new([]);
        ActionSelection selection = selector.Select(Context());
        Assert.False(selection.IsSelected);
        Assert.IsType<AskUserProposal>(selection.Proposal);
    }

    [Fact]
    public void IdenticalStateGivesIdenticalSelection()
    {
        ActionSelectionContext context = Context(
            goals: [GoalWith(GoalStatus.Incomplete)],
            interpretation: Matched());
        ActionSelection first = PolicySelector.Default.Select(context);
        ActionSelection second = PolicySelector.Default.Select(context);
        Assert.Equal(first, second);
    }

    [Fact]
    public void DuplicateRuleIdsAreRejected()
    {
        IActionRule[] rules =
        [
            new FakeRule("dup", 10, null),
            new FakeRule("dup", 20, null),
        ];
        Assert.Throws<ArgumentException>(() => new PolicySelector(rules));
    }

    [Fact]
    public void EmptyRuleIdIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => new PolicySelector([new FakeRule("  ", 10, null)]));
    }

    [Fact]
    public void InvokeToolProposalCanBeSelectedByARule()
    {
        ArgumentBag arguments = ArgumentBag.From(
        [
            new ArgumentEntry("left", ArgumentValue.FromInteger(2)),
            new ArgumentEntry("right", ArgumentValue.FromInteger(3)),
        ]);
        PolicySelector selector = new(
        [
            new FakeRule("invoke", 10, new InvokeToolProposal(CalculatorTool.Id, arguments)),
        ]);
        ActionSelection selection = selector.Select(Context());
        Assert.IsType<InvokeToolProposal>(selection.Proposal);
    }

    [Fact]
    public void NullContextThrows()
    {
        Assert.Throws<ArgumentNullException>(() => PolicySelector.Default.Select(null!));
    }

    private static ActionSelectionContext Context(
        IEnumerable<Goal>? goals = null,
        RequestInterpretation? interpretation = null)
    {
        Session session = new(SessionId.New(), goals: goals);
        return new ActionSelectionContext(session, interpretation ?? Matched());
    }

    private static RequestInterpretation Matched()
    {
        return RequestInterpretation.Matched(
            "what is gravity",
            new IntentMatch("question.what-is", RequestIntentKind.Question, new IntentSlot("topic", "gravity")));
    }

    private static RequestInterpretation Unknown()
    {
        return RequestInterpretation.Unknown("hello there", "No known pattern matched.");
    }

    private static Goal GoalWith(GoalStatus status)
    {
        return new(GoalId.New(), "Do it.", "It is done.", status);
    }
}