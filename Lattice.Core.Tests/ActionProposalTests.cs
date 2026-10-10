namespace Lattice.Core.Tests;

public sealed class ActionProposalTests
{
    [Fact]
    public void InvokeToolProposalRejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new InvokeToolProposal(CalculatorTool.Id, null!));
    }

    [Fact]
    public void InvokeToolProposalRejectsEmptyToolId()
    {
        Assert.Throws<ArgumentException>(() => new InvokeToolProposal(default, ArgumentBag.Empty));
    }

    [Fact]
    public void AskUserProposalRejectsNullRequest()
    {
        Assert.Throws<ArgumentNullException>(() => new AskUserProposal(null!));
    }

    [Fact]
    public void RespondProposalRejectsEmptyText()
    {
        Assert.Throws<ArgumentException>(() => new RespondProposal("   "));
    }

    [Fact]
    public void ContinueProposalRejectsEmptyReason()
    {
        Assert.Throws<ArgumentException>(() => new ContinueProposal(string.Empty));
    }

    [Fact]
    public void FinishProposalRejectsEmptyReason()
    {
        Assert.Throws<ArgumentException>(() => new FinishProposal(string.Empty));
    }

    [Fact]
    public void ProposalsWithSameContentAreEqual()
    {
        InvokeToolProposal first = new(CalculatorTool.Id, ArgumentBag.Empty);
        InvokeToolProposal second = new(CalculatorTool.Id, ArgumentBag.Empty);
        Assert.Equal(first, second);
    }

    [Fact]
    public void ProposalsWithDifferentPayloadsAreNotEqual()
    {
        RespondProposal first = new("one");
        RespondProposal second = new("two");
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ProposalsOfDifferentTypesAreNotEqual()
    {
        ActionProposal first = new ContinueProposal("keep going");
        ActionProposal second = new FinishProposal("keep going");
        Assert.NotEqual(first, second);
    }
}