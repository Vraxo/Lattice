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

        Assert.Equal(TurnOutcome.FactRecorded, first.Outcome);
        Assert.Single(first.Session.Facts);
        Assert.Equal(TurnOutcome.StepLimitExceeded, second.Outcome);
        Assert.Same(first.Session, second.Session);
        Assert.Single(second.Session.Facts);
        Assert.Equal(1, context.MaxSteps);
        Assert.Equal(1, context.StepsUsed);
        Assert.Equal(0, context.RemainingSteps);
        Assert.Contains("1-step limit", second.Response, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsNonPositiveMaximumSteps()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AgentRunContext(0));
    }

    private static IControlledStatement Parse(string text)
    {
        Result<IControlledStatement> parsed = ControlledLanguageParser.Parse(text);
        Assert.True(parsed.IsSuccess, parsed.Error?.Message);
        return parsed.Value;
    }
}
