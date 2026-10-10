namespace Lattice.Core.Tests;

public sealed class PolicySelectorCandidatesTests
{
    [Fact]
    public void SingleCandidateIsSelected()
    {
        RespondProposal proposal = new("only");
        ActionSelection selection = PolicySelector.Default.Select(Context(), [proposal]);
        Assert.True(selection.IsSelected);
        Assert.Equal(proposal, selection.Proposal);
    }

    [Fact]
    public void MultipleCandidatesAreBlockedNotArbitrarilyChosen()
    {
        ActionProposal[] candidates =
        [
            new RespondProposal("a"),
            new RespondProposal("b"),
        ];
        ActionSelection selection = PolicySelector.Default.Select(Context(), candidates);
        Assert.False(selection.IsSelected);
        AskUserProposal ask = Assert.IsType<AskUserProposal>(selection.Proposal);
        Assert.Equal(ClarificationKind.MultipleCandidates, ask.Request.Kind);
    }

    [Fact]
    public void EmptyCandidatesFallsBackToRules()
    {
        ActionSelectionContext context = Context(goals: [GoalWith(GoalStatus.Incomplete)], interpretation: Matched());
        ActionSelection selection = PolicySelector.Default.Select(context, []);
        Assert.True(selection.IsSelected);
        Assert.IsType<ContinueProposal>(selection.Proposal);
    }

    [Fact]
    public void NullCandidatesThrows()
    {
        Assert.Throws<ArgumentNullException>(
            () => PolicySelector.Default.Select(Context(), null!));
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

    private static Goal GoalWith(GoalStatus status)
    {
        return new(GoalId.New(), "Do it.", "It is done.", status);
    }
}