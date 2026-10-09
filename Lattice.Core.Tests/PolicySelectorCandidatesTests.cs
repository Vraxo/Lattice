namespace Lattice.Core.Tests;
public sealed class PolicySelectorCandidatesTests
{
    [Fact]
    public void SingleCandidateIsSelected()
    {
        var proposal = new RespondProposal("only");
        var selection = PolicySelector.Default.Select(Context(), new[] { proposal });
        Assert.True(selection.IsSelected);
        Assert.Equal(proposal, selection.Proposal);
    }
    [Fact]
    public void MultipleCandidatesAreBlockedNotArbitrarilyChosen()
    {
        var candidates = new ActionProposal[]
        {
            new RespondProposal("a"),
            new RespondProposal("b"),
        };
        var selection = PolicySelector.Default.Select(Context(), candidates);
        Assert.False(selection.IsSelected);
        var ask = Assert.IsType<AskUserProposal>(selection.Proposal);
        Assert.Equal(ClarificationKind.MultipleCandidates, ask.Request.Kind);
    }
    [Fact]
    public void EmptyCandidatesFallsBackToRules()
    {
        var context = Context(goals: new[] { GoalWith(GoalStatus.Incomplete) }, interpretation: Matched());
        var selection = PolicySelector.Default.Select(context, Array.Empty<ActionProposal>());
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
        var session = new Session(SessionId.New(), goals: goals);
        return new ActionSelectionContext(session, interpretation ?? Matched());
    }
    private static RequestInterpretation Matched() =>
        RequestInterpretation.Matched(
            "what is gravity",
            new IntentMatch("question.what-is", RequestIntentKind.Question, new IntentSlot("topic", "gravity")));
    private static Goal GoalWith(GoalStatus status) =>
        new(GoalId.New(), "Do it.", "It is done.", status);
}