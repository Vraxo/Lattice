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
        selection.IsSelected.Should().BeTrue();
        selection.Proposal.Should().BeOfType<FinishProposal>();
    }

    [Fact]
    public void UnresolvedInterpretationAsksUser()
    {
        ActionSelectionContext context = Context(interpretation: Unknown());
        ActionSelection selection = PolicySelector.Default.Select(context);
        selection.IsSelected.Should().BeTrue();
        selection.Proposal.Should().BeOfType<AskUserProposal>();
    }

    [Fact]
    public void IncompleteGoalContinues()
    {
        ActionSelectionContext context = Context(
            goals: [GoalWith(GoalStatus.Incomplete)],
            interpretation: Matched());
        ActionSelection selection = PolicySelector.Default.Select(context);
        selection.IsSelected.Should().BeTrue();
        selection.Proposal.Should().BeOfType<ContinueProposal>();
    }

    [Fact]
    public void BlockedGoalWithNoOtherRuleIsBlocked()
    {
        // A blocked goal matches no rule: finish needs all completed, continue needs incomplete.
        ActionSelectionContext context = Context(
            goals: [GoalWith(GoalStatus.Blocked)],
            interpretation: Matched());
        ActionSelection selection = PolicySelector.Default.Select(context);
        selection.IsSelected.Should().BeFalse();
        selection.Kind.Should().Be(ActionSelectionKind.Blocked);
        AskUserProposal ask = selection.Proposal.Should().BeOfType<AskUserProposal>().Which;
        ask.Request.Kind.Should().Be(ClarificationKind.NoEligibleAction);
    }

    [Fact]
    public void NoGoalsAndMatchedRequestIsBlockedNotCrashed()
    {
        ActionSelectionContext context = Context(interpretation: Matched());
        ActionSelection selection = PolicySelector.Default.Select(context);
        selection.IsSelected.Should().BeFalse();
        selection.Proposal.Should().BeOfType<AskUserProposal>();
    }

    [Fact]
    public void InterpretationIsClarifiedBeforeContinuingIncompleteGoal()
    {
        ActionSelectionContext context = Context(
            goals: [GoalWith(GoalStatus.Incomplete)],
            interpretation: Unknown());
        ActionSelection selection = PolicySelector.Default.Select(context);

        // Clarify (priority 90) outranks continue (priority 50).
        selection.Proposal.Should().BeOfType<AskUserProposal>();
    }

    [Fact]
    public void CompletionOutranksClarification()
    {
        ActionSelectionContext context = Context(
            goals: [GoalWith(GoalStatus.Completed)],
            interpretation: Unknown());
        ActionSelection selection = PolicySelector.Default.Select(context);

        // Finish (priority 100) outranks clarify (priority 90).
        selection.Proposal.Should().BeOfType<FinishProposal>();
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
        selection.Proposal.Should().Be(new RespondProposal("high"));
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
        RejectedRule rejected = selection.RejectedRules.Should().ContainSingle().Which;
        rejected.RuleId.Should().Be("low");
        rejected.Priority.Should().Be(10);
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
        selection.Proposal.Should().Be(new RespondProposal("a"));
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
        selection.Proposal.Should().Be(new RespondProposal("always"));
    }

    [Fact]
    public void NoRulesProducesBlockedSelection()
    {
        PolicySelector selector = new([]);
        ActionSelection selection = selector.Select(Context());
        selection.IsSelected.Should().BeFalse();
        selection.Proposal.Should().BeOfType<AskUserProposal>();
    }

    [Fact]
    public void IdenticalStateGivesIdenticalSelection()
    {
        ActionSelectionContext context = Context(
            goals: [GoalWith(GoalStatus.Incomplete)],
            interpretation: Matched());
        ActionSelection first = PolicySelector.Default.Select(context);
        ActionSelection second = PolicySelector.Default.Select(context);
        second.Should().Be(first);
    }

    [Fact]
    public void DuplicateRuleIdsAreRejected()
    {
        IActionRule[] rules =
        [
            new FakeRule("dup", 10, null),
            new FakeRule("dup", 20, null),
        ];
        FluentActions.Invoking(() => new PolicySelector(rules)).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void EmptyRuleIdIsRejected()
    {
        FluentActions.Invoking(() => new PolicySelector([new FakeRule("  ", 10, null)])).Should().ThrowExactly<ArgumentException>();
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
        selection.Proposal.Should().BeOfType<InvokeToolProposal>();
    }

    [Fact]
    public void NullContextThrows()
    {
        FluentActions.Invoking(() => PolicySelector.Default.Select(null!)).Should().ThrowExactly<ArgumentNullException>();
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