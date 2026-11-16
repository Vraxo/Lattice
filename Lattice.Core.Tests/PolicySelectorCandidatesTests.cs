namespace Lattice.Core.Tests;

public sealed class PolicySelectorCandidatesTests
{
    [Fact]
    public void SingleCandidateIsSelected()
    {
        RespondProposal proposal = new("only");
        ActionSelection selection = PolicySelector.Default.Select(Context(), [proposal]);
        selection.IsSelected.Should().BeTrue();
        selection.Proposal.Should().Be(proposal);
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
        selection.IsSelected.Should().BeFalse();
        AskUserProposal ask = selection.Proposal.Should().BeOfType<AskUserProposal>().Which;
        ask.Request.Kind.Should().Be(ClarificationKind.MultipleCandidates);
    }

    [Fact]
    public void EmptyCandidatesFallsBackToRules()
    {
        ActionSelectionContext context = Context(goals: [GoalWith(GoalStatus.Incomplete)], interpretation: Matched());
        ActionSelection selection = PolicySelector.Default.Select(context, []);
        selection.IsSelected.Should().BeTrue();
        selection.Proposal.Should().BeOfType<ContinueProposal>();
    }

    [Fact]
    public void NullCandidatesThrows()
    {
        FluentActions.Invoking(() => PolicySelector.Default.Select(Context(), null!)).Should().ThrowExactly<ArgumentNullException>();
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