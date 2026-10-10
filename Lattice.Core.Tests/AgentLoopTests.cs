namespace Lattice.Core.Tests;

public sealed class AgentLoopTests
{
    [Fact]
    public void SuccessfulControlledActionExecutesAndRecords()
    {
        AgentLoop loop = CreateLoop(new CalculatorTool());
        Session session = Session.Empty(SessionId.New());
        AgentTurnResult result = loop.Run(session, Parse("action calculator | left=2 | right=3"));
        Assert.Equal(TurnOutcome.ToolSucceeded, result.Outcome);
        Assert.NotNull(result.Invocation);
        Assert.Equal(5, result.Invocation!.Result.Output!.AsInteger());
        Assert.Single(result.Session.ToolInvocations);
    }

    [Fact]
    public void UnavailableToolIsReportedAndNothingRecorded()
    {
        AgentLoop loop = CreateLoop(new CalculatorTool());
        Session session = Session.Empty(SessionId.New());
        AgentTurnResult result = loop.Run(session, Parse("action missing | left=1"));
        Assert.Equal(TurnOutcome.ToolUnavailable, result.Outcome);
        Assert.Null(result.Invocation);
        Assert.Empty(result.Session.ToolInvocations);
    }

    [Fact]
    public void InvalidArgumentsAreReported()
    {
        AgentLoop loop = CreateLoop(new CalculatorTool());
        Session session = Session.Empty(SessionId.New());
        AgentTurnResult result = loop.Run(session, Parse("action calculator | left=2"));
        Assert.Equal(TurnOutcome.InvalidArguments, result.Outcome);
        Assert.Single(result.Session.ToolInvocations);
        Assert.False(result.Invocation!.Result.IsSuccess);
    }

    [Fact]
    public void ToolFailureIsRecorded()
    {
        AgentLoop loop = CreateLoop(new FailingTool());
        Session session = Session.Empty(SessionId.New());
        AgentTurnResult result = loop.Run(session, Parse("action failing"));
        Assert.Equal(TurnOutcome.ToolFailed, result.Outcome);
        Assert.Single(result.Session.ToolInvocations);
    }

    [Fact]
    public void DeniedPermissionIsReportedAndNotExecuted()
    {
        WriteRecordingTool tool = new();
        AgentLoop loop = CreateLoop(tool);
        Session session = Session.Empty(SessionId.New());
        AgentTurnResult result = loop.Run(session, Parse("action writer"));
        Assert.Equal(TurnOutcome.PermissionDenied, result.Outcome);
        Assert.False(tool.WasExecuted);
        Assert.Single(result.Session.ToolInvocations);
    }

    [Fact]
    public void RepeatedActionIsPrevented()
    {
        AgentLoop loop = CreateLoop(new CalculatorTool());
        Session session = Session.Empty(SessionId.New());
        IControlledStatement statement = Parse("action calculator | left=2 | right=3");
        AgentTurnResult first = loop.Run(session, statement);
        AgentTurnResult second = loop.Run(first.Session, statement);
        Assert.Equal(TurnOutcome.ToolSucceeded, first.Outcome);
        Assert.Equal(TurnOutcome.DuplicateAction, second.Outcome);
        Assert.Single(second.Session.ToolInvocations);
    }

    [Fact]
    public void FactStatementIsRecorded()
    {
        AgentLoop loop = CreateLoop(new CalculatorTool());
        Session session = Session.Empty(SessionId.New());
        AgentTurnResult result = loop.Run(session, Parse("fact water | boils_at | 100C"));
        Assert.Equal(TurnOutcome.FactRecorded, result.Outcome);
        Fact fact = Assert.Single(result.Session.Facts);
        Assert.Equal(FactStatus.UserAsserted, fact.Status);
    }

    [Fact]
    public void GoalStatementIsRecorded()
    {
        AgentLoop loop = CreateLoop(new CalculatorTool());
        Session session = Session.Empty(SessionId.New());
        AgentTurnResult result = loop.Run(session, Parse("goal Summarize the file. | A summary exists."));
        Assert.Equal(TurnOutcome.GoalRecorded, result.Outcome);
        Goal goal = Assert.Single(result.Session.Goals);
        Assert.Equal(GoalStatus.Incomplete, goal.Status);
    }

    [Fact]
    public void ConstraintStatementIsReportedUnsupported()
    {
        AgentLoop loop = CreateLoop(new CalculatorTool());
        Session session = Session.Empty(SessionId.New());
        AgentTurnResult result = loop.Run(session, Parse("constraint Do not change other sections."));
        Assert.Equal(TurnOutcome.ConstraintNotSupported, result.Outcome);
        Assert.Empty(result.Session.Goals);
    }

    [Fact]
    public void OriginalSessionIsNotMutated()
    {
        AgentLoop loop = CreateLoop(new CalculatorTool());
        Session session = Session.Empty(SessionId.New());
        loop.Run(session, Parse("action calculator | left=2 | right=3"));
        Assert.Empty(session.ToolInvocations);
    }

    [Fact]
    public void NullStatementThrows()
    {
        AgentLoop loop = CreateLoop(new CalculatorTool());

        // Cast disambiguates the null literal between the statement and request overloads.
        Assert.Throws<ArgumentNullException>(
            () => loop.Run(Session.Empty(SessionId.New()), (IControlledStatement)null!));
    }

    [Fact]
    public void NullCapabilityRequestThrows()
    {
        AgentLoop loop = CreateLoop(new CalculatorTool());
        Assert.Throws<ArgumentNullException>(
            () => loop.Run(Session.Empty(SessionId.New()), (CapabilityRequest)null!));
    }

    private static AgentLoop CreateLoop(params ITool[] tools)
    {
        return new(new ToolRegistry(tools), ToolPermissionPolicy.ReadOnlyOnly);
    }

    private static IControlledStatement Parse(string input)
    {
        Result<IControlledStatement> result = ControlledLanguageParser.Parse(input);
        Assert.True(result.IsSuccess, result.Error?.Message);
        return result.Value;
    }
}