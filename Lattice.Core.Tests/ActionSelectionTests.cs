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
        var selection = ActionSelection.Selected(new RespondProposal("done"), "rule applied");
        Assert.True(selection.IsSelected);
        Assert.Equal(ActionSelectionKind.Selected, selection.Kind);
        Assert.Empty(selection.RejectedRules);
    }
    [Fact]
    public void BlockedReportsBlockedKind()
    {
        var selection = ActionSelection.Blocked(new RespondProposal("blocked"), "no rule");
        Assert.False(selection.IsSelected);
        Assert.Equal(ActionSelectionKind.Blocked, selection.Kind);
    }
    [Fact]
    public void SelectionsWithSameContentAreEqual()
    {
        var first = ActionSelection.Selected(new RespondProposal("done"), "reason");
        var second = ActionSelection.Selected(new RespondProposal("done"), "reason");
        Assert.Equal(first, second);
    }
    [Fact]
    public void SelectionsWithDifferentRejectedRulesAreNotEqual()
    {
        var first = ActionSelection.Selected(
            new RespondProposal("done"),
            "reason",
            new[] { new RejectedRule("a", 10) });
        var second = ActionSelection.Selected(
            new RespondProposal("done"),
            "reason",
            new[] { new RejectedRule("b", 10) });
        Assert.NotEqual(first, second);
    }
}