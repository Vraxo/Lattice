namespace Lattice.Core.Tests;

public sealed class ActionProposalTests
{
    [Fact]
    public void InvokeToolProposalRejectsNullArguments()
    {
        FluentActions.Invoking(() => new InvokeToolProposal(CalculatorTool.Id, null!)).Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void InvokeToolProposalRejectsEmptyToolId()
    {
        FluentActions.Invoking(() => new InvokeToolProposal(default, ArgumentBag.Empty)).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void AskUserProposalRejectsNullRequest()
    {
        FluentActions.Invoking(() => new AskUserProposal(null!)).Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void RespondProposalRejectsEmptyText()
    {
        FluentActions.Invoking(() => new RespondProposal("   ")).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void ContinueProposalRejectsEmptyReason()
    {
        FluentActions.Invoking(() => new ContinueProposal(string.Empty)).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void FinishProposalRejectsEmptyReason()
    {
        FluentActions.Invoking(() => new FinishProposal(string.Empty)).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void ProposalsWithSameContentAreEqual()
    {
        InvokeToolProposal first = new(CalculatorTool.Id, ArgumentBag.Empty);
        InvokeToolProposal second = new(CalculatorTool.Id, ArgumentBag.Empty);
        second.Should().Be(first);
    }

    [Fact]
    public void ProposalsWithDifferentPayloadsAreNotEqual()
    {
        RespondProposal first = new("one");
        RespondProposal second = new("two");
        second.Should().NotBe(first);
    }

    [Fact]
    public void ProposalsOfDifferentTypesAreNotEqual()
    {
        ActionProposal first = new ContinueProposal("keep going");
        ActionProposal second = new FinishProposal("keep going");
        second.Should().NotBe(first);
    }
}