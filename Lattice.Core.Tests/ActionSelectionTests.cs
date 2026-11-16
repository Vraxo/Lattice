namespace Lattice.Core.Tests;

public sealed class ActionSelectionTests
{
    [Fact]
    public void SelectedRequiresProposalAndReason()
    {
        FluentActions.Invoking(() => ActionSelection.Selected(null!, "reason")).Should().ThrowExactly<ArgumentNullException>();
        FluentActions.Invoking(() => ActionSelection.Selected(new RespondProposal("x"), "  ")).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void BlockedRequiresProposalAndReason()
    {
        FluentActions.Invoking(() => ActionSelection.Blocked(null!, "reason")).Should().ThrowExactly<ArgumentNullException>();
        FluentActions.Invoking(() => ActionSelection.Blocked(new RespondProposal("x"), string.Empty)).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void SelectedReportsSelectedKind()
    {
        ActionSelection selection = ActionSelection.Selected(new RespondProposal("done"), "rule applied");
        selection.IsSelected.Should().BeTrue();
        selection.Kind.Should().Be(ActionSelectionKind.Selected);
        selection.RejectedRules.Should().BeEmpty();
    }

    [Fact]
    public void BlockedReportsBlockedKind()
    {
        ActionSelection selection = ActionSelection.Blocked(new RespondProposal("blocked"), "no rule");
        selection.IsSelected.Should().BeFalse();
        selection.Kind.Should().Be(ActionSelectionKind.Blocked);
    }

    [Fact]
    public void SelectionsWithSameContentAreEqual()
    {
        ActionSelection first = ActionSelection.Selected(new RespondProposal("done"), "reason");
        ActionSelection second = ActionSelection.Selected(new RespondProposal("done"), "reason");
        second.Should().Be(first);
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
        second.Should().NotBe(first);
    }
}