namespace Lattice.Core.Tests;
public sealed class PolicySelectorTests
{
    [Fact]
    public void AllGoalsCompletedFinishes()
    {
        var context = Context(
            goals: new[] { GoalWith(GoalStatus.Completed) },
            interpretation: Matched());
        var selection = PolicySelector.Default.Select(context);
        Assert.True(selection.IsSelected);
        Assert.IsType<FinishProposal>(selection.Proposal);
    }
    [Fact]
    public void UnresolvedInterpretationAsksUser()
    {
        var context = Context(interpretation: Unknown());
        var selection = PolicySelector.Default.Select(context);
        Assert.True(selection.IsSelected);
        Assert.IsType<AskUserProposal>(selection.Proposal);
    }
    [Fact]
    public void IncompleteGoalContinues()
    {
        var context = Context(
            goals: new[] { GoalWith(GoalStatus.Incomplete) },
            interpretation: Matched());
        var selection = PolicySelector.Default.Select(context);
        Assert.True(selection.IsSelected);
        Assert.IsType<ContinueProposal>(selection.Proposal);
    }
    [Fact]
    public void BlockedGoalWithNoOtherRuleIsBlocked()
    {
        // A blocked goal matches no rule: finish needs all completed, continue needs incomplete.
        var context = Context(
            goals: new[] { GoalWith(GoalStatus.Blocked) },
            interpretation: Matched());
        var selection = PolicySelector.Default.Select(context);
        Assert.False(selection.IsSelected);
        Assert.Equal(ActionSelectionKind.Blocked, selection.Kind);
        var ask = Assert.IsType<AskUserProposal>(selection.Proposal);
        Assert.Equal(ClarificationKind.NoEligibleAction, ask.Request.Kind);
    }
    [Fact]
    public void NoGoalsAndMatchedRequestIsBlockedNotCrashed()
    {
        var context = Context(interpretation: Matched());
        var selection = PolicySelector.Default.Select(context);
        Assert.False(selection.IsSelected);
        Assert.IsType<AskUserProposal>(selection.Proposal);
    }
    [Fact]
    public void InterpretationIsClarifiedBeforeContinuingIncompleteGoal()
    {
        var context = Context(
            goals: new[] { GoalWith(GoalStatus.Incomplete) },
            interpretation: Unknown());
        var selection = PolicySelector.Default.Select(context);
        // Clarify (priority 90) outranks continue (priority 50).
        Assert.IsType<AskUserProposal>(selection.Proposal);
    }
    [Fact]
    public void CompletionOutranksClarification()
    {
        var context = Context(
            goals: new[] { GoalWith(GoalStatus.Completed) },
            interpretation: Unknown());
        var selection = PolicySelector.Default.Select(context);
        // Finish (priority 100) outranks clarify (priority 90).
        Assert.IsType<FinishProposal>(selection.Proposal);
    }
    [Fact]
    public void HigherPriorityRuleWins()
    {
        var selector = new PolicySelector(new IActionRule[]
        {
            new FakeRule("low", 10, new RespondProposal("low")),
            new FakeRule("high", 20, new RespondProposal("high")),
        });
        var selection = selector.Select(Context());
        Assert.Equal(new RespondProposal("high"), selection.Proposal);
    }
    [Fact]
    public void RejectedRulesAreRecorded()
    {
        var selector = new PolicySelector(new IActionRule[]
        {
            new FakeRule("low", 10, new RespondProposal("low")),
            new FakeRule("high", 20, new RespondProposal("high")),
        });
        var selection = selector.Select(Context());
        var rejected = Assert.Single(selection.RejectedRules);
        Assert.Equal("low", rejected.RuleId);
        Assert.Equal(10, rejected.Priority);
    }
    [Fact]
    public void SamePriorityTieIsBrokenDeterministicallyById()
    {
        var selector = new PolicySelector(new IActionRule[]
        {
            new FakeRule("b", 10, new RespondProposal("b")),
            new FakeRule("a", 10, new RespondProposal("a")),
        });
        var selection = selector.Select(Context());
        Assert.Equal(new RespondProposal("a"), selection.Proposal);
    }
    [Fact]
    public void InapplicableRulesAreSkipped()
    {
        var selector = new PolicySelector(new IActionRule[]
        {
            new FakeRule("never", 100, null),
            new FakeRule("always", 10, new RespondProposal("always")),
        });
        var selection = selector.Select(Context());
        Assert.Equal(new RespondProposal("always"), selection.Proposal);
    }
    [Fact]
    public void NoRulesProducesBlockedSelection()
    {
        var selector = new PolicySelector(Array.Empty<IActionRule>());
        var selection = selector.Select(Context());
        Assert.False(selection.IsSelected);
        Assert.IsType<AskUserProposal>(selection.Proposal);
    }
    [Fact]
    public void IdenticalStateGivesIdenticalSelection()
    {
        var context = Context(
            goals: new[] { GoalWith(GoalStatus.Incomplete) },
            interpretation: Matched());
        var first = PolicySelector.Default.Select(context);
        var second = PolicySelector.Default.Select(context);
        Assert.Equal(first, second);
    }
    [Fact]
    public void DuplicateRuleIdsAreRejected()
    {
        var rules = new IActionRule[]
        {
            new FakeRule("dup", 10, null),
            new FakeRule("dup", 20, null),
        };
        Assert.Throws<ArgumentException>(() => new PolicySelector(rules));
    }
    [Fact]
    public void EmptyRuleIdIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => new PolicySelector(new IActionRule[] { new FakeRule("  ", 10, null) }));
    }
    [Fact]
    public void InvokeToolProposalCanBeSelectedByARule()
    {
        var arguments = ArgumentBag.From(new[]
        {
            new ArgumentEntry("left", ArgumentValue.FromInteger(2)),
            new ArgumentEntry("right", ArgumentValue.FromInteger(3)),
        });
        var selector = new PolicySelector(new IActionRule[]
        {
            new FakeRule("invoke", 10, new InvokeToolProposal(CalculatorTool.Id, arguments)),
        });
        var selection = selector.Select(Context());
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
        var session = new Session(SessionId.New(), goals: goals);
        return new ActionSelectionContext(session, interpretation ?? Matched());
    }
    private static RequestInterpretation Matched() =>
        RequestInterpretation.Matched(
            "what is gravity",
            new IntentMatch("question.what-is", RequestIntentKind.Question, new IntentSlot("topic", "gravity")));
    private static RequestInterpretation Unknown() =>
        RequestInterpretation.Unknown("hello there", "No known pattern matched.");
    private static Goal GoalWith(GoalStatus status) =>
        new(GoalId.New(), "Do it.", "It is done.", status);
}