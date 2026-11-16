namespace Lattice.Core.Tests;

public sealed class ActionProposalValidatorTests
{
    private static ToolCatalog Catalog()
    {
        return new([new CalculatorTool().Descriptor]);
    }

    [Fact]
    public void ValidInvokePasses()
    {
        ArgumentBag arguments = ArgumentBag.From(
        [
            new ArgumentEntry("left", ArgumentValue.FromInteger(2)),
            new ArgumentEntry("right", ArgumentValue.FromInteger(3)),
        ]);
        Result result = ActionProposalValidator.Validate(
            new InvokeToolProposal(CalculatorTool.Id, arguments),
            Catalog());
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void UnknownToolFails()
    {
        Result result = ActionProposalValidator.Validate(
            new InvokeToolProposal(new ToolId("missing"), ArgumentBag.Empty),
            Catalog());
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("action.tool.unknown");
    }

    [Fact]
    public void InvalidArgumentsFail()
    {
        Result result = ActionProposalValidator.Validate(
            new InvokeToolProposal(CalculatorTool.Id, ArgumentBag.Empty),
            Catalog());
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("tool.argument.missing");
    }

    [Fact]
    public void AskUserPasses()
    {
        AskUserProposal proposal = new(
            new ClarificationRequest(ClarificationKind.MissingValue, "What topic?"));
        ActionProposalValidator.Validate(proposal, Catalog()).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void RespondPasses()
    {
        ActionProposalValidator.Validate(new RespondProposal("done"), Catalog()).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ContinuePasses()
    {
        ActionProposalValidator.Validate(new ContinueProposal("more to do"), Catalog()).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void FinishPasses()
    {
        ActionProposalValidator.Validate(new FinishProposal("goal met"), Catalog()).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void NullProposalThrows()
    {
        FluentActions.Invoking(() => ActionProposalValidator.Validate(null!, Catalog())).Should().ThrowExactly<ArgumentNullException>();
    }
}