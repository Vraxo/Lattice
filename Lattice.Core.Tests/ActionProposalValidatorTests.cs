namespace Lattice.Core.Tests;
public sealed class ActionProposalValidatorTests
{
    private static ToolCatalog Catalog() => new(new[] { new CalculatorTool().Descriptor });
    [Fact]
    public void ValidInvokePasses()
    {
        var arguments = ArgumentBag.From(new[]
        {
            new ArgumentEntry("left", ArgumentValue.FromInteger(2)),
            new ArgumentEntry("right", ArgumentValue.FromInteger(3)),
        });
        var result = ActionProposalValidator.Validate(
            new InvokeToolProposal(CalculatorTool.Id, arguments),
            Catalog());
        Assert.True(result.IsSuccess);
    }
    [Fact]
    public void UnknownToolFails()
    {
        var result = ActionProposalValidator.Validate(
            new InvokeToolProposal(new ToolId("missing"), ArgumentBag.Empty),
            Catalog());
        Assert.False(result.IsSuccess);
        Assert.Equal("action.tool.unknown", result.Error!.Code);
    }
    [Fact]
    public void InvalidArgumentsFail()
    {
        var result = ActionProposalValidator.Validate(
            new InvokeToolProposal(CalculatorTool.Id, ArgumentBag.Empty),
            Catalog());
        Assert.False(result.IsSuccess);
        Assert.Equal("tool.argument.missing", result.Error!.Code);
    }
    [Fact]
    public void AskUserPasses()
    {
        var proposal = new AskUserProposal(
            new ClarificationRequest(ClarificationKind.MissingValue, "What topic?"));
        Assert.True(ActionProposalValidator.Validate(proposal, Catalog()).IsSuccess);
    }
    [Fact]
    public void RespondPasses()
    {
        Assert.True(ActionProposalValidator.Validate(new RespondProposal("done"), Catalog()).IsSuccess);
    }
    [Fact]
    public void ContinuePasses()
    {
        Assert.True(ActionProposalValidator.Validate(new ContinueProposal("more to do"), Catalog()).IsSuccess);
    }
    [Fact]
    public void FinishPasses()
    {
        Assert.True(ActionProposalValidator.Validate(new FinishProposal("goal met"), Catalog()).IsSuccess);
    }
    [Fact]
    public void NullProposalThrows()
    {
        Assert.Throws<ArgumentNullException>(() => ActionProposalValidator.Validate(null!, Catalog()));
    }
}