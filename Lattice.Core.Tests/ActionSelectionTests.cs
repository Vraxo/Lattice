namespace Lattice.Core.Tests;

public sealed class ActionSelectionTests
{
    [Fact]
    public void SelectedRequiresProposalAndReason()
    {
        Assert.Throws<ArgumentNullException>(
            () => ActionSelection.Selected(null!, "reason"));
        Assert.Throws<ArgumentException>(
            () => ActionSelection.Selected(new RespondProposal("x"), "  "));
    }

    [Fact]
    public void BlockedRequiresProposalAndReason()
    {
        Assert.Throws<ArgumentNullException>(
            () => ActionSelection.Blocked(null!, "reason"));
        Assert.Throws<ArgumentException>(
            () => ActionSelection.Blocked(new RespondProposal("x"), string.Empty));
    }

    [Fact]
    public void SelectedReportsSelectedKind()
    {
        ActionSelection selection = ActionSelection.Selected(new RespondProposal("done"), "rule applied");
        Assert.True(selection.IsSelected);
        Assert.Equal(ActionSelectionKind.Selected, selection.Kind);
        Assert.Empty(selection.RejectedRules);
    }

    [Fact]
    public void BlockedReportsBlockedKind()
    {
        ActionSelection selection = ActionSelection.Blocked(new RespondProposal("blocked"), "no rule");
        Assert.False(selection.IsSelected);
        Assert.Equal(ActionSelectionKind.Blocked, selection.Kind);
    }

    [Fact]
    public void SelectionsWithSameContentAreEqual()
    {
        ActionSelection first = ActionSelection.Selected(new RespondProposal("done"), "reason");
        ActionSelection second = ActionSelection.Selected(new RespondProposal("done"), "reason");
        Assert.Equal(first, second);
    }

    [Fact]
    public void SelectionsWithDifferentRejectedRulesAreNotEqual()
    {
        ActionSelection first = ActionSelection.Selected(
            new RespondProposal("done"),
            "reason",
            [new RejectedRule("a", 10)]);
        ActionSelection second = ActionSelection.Selected(
            new RespondProposal("done"),
            "reason",
            [new RejectedRule("b", 10)]);
        Assert.NotEqual(first, second);
    }
}