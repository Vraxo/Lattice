namespace Lattice.Core.Tests;

public sealed class AgentRunContextTests
{
    [Fact]
    public void MaximumStepsExhaustsWithoutApplyingAnotherStateChange()
    {
        AgentRunContext context = new(maxSteps: 1);
        AgentLoop loop = new(new ToolRegistry([new CalculatorTool()]), ToolPermissionPolicy.ReadOnlyOnly);
        Session original = Session.Empty(SessionId.New());
        IControlledStatement firstStatement = Parse("fact water | boils_at | 100C");
        IControlledStatement secondStatement = Parse("fact ice | melts_at | 0C");

        AgentTurnResult first = loop.Run(original, context, firstStatement);
        AgentTurnResult second = loop.Run(first.Session, context, secondStatement);

        first.Outcome.Should().Be(TurnOutcome.FactRecorded);
        first.Session.Facts.Should().ContainSingle();
        second.Outcome.Should().Be(TurnOutcome.StepLimitExceeded);
        second.Session.Should().BeSameAs(first.Session);
        second.Session.Facts.Should().ContainSingle();
        context.MaxSteps.Should().Be(1);
        context.StepsUsed.Should().Be(1);
        context.RemainingSteps.Should().Be(0);
        second.Response.Should().Contain("1-step limit");
    }

    [Fact]
    public void RejectsNonPositiveMaximumSteps()
    {
        FluentActions.Invoking(() => new AgentRunContext(0)).Should().ThrowExactly<ArgumentOutOfRangeException>();
    }

    private static IControlledStatement Parse(string text)
    {
        Result<IControlledStatement> parsed = ControlledLanguageParser.Parse(text);
        parsed.IsSuccess.Should().BeTrue("{0}", parsed.Error?.Message ?? string.Empty);
        return parsed.Value;
    }
}
