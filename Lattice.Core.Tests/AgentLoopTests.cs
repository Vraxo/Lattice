namespace Lattice.Core.Tests;

public sealed class AgentLoopTests
{
    [Fact]
    public void SuccessfulControlledActionExecutesAndRecords()
    {
        AgentLoop loop = CreateLoop(new CalculatorTool());
        Session session = Session.Empty(SessionId.New());
        AgentTurnResult result = loop.Run(session, Parse("action calculator | left=2 | right=3"));
        result.Outcome.Should().Be(TurnOutcome.ToolSucceeded);
        result.Invocation.Should().BeNotNull();
        result.Invocation!.Result.Output!.AsInteger().Should().Be(5);
        result.Session.ToolInvocations.Should().ContainSingle();
    }

    [Fact]
    public void UnavailableToolIsReportedAndNothingRecorded()
    {
        AgentLoop loop = CreateLoop(new CalculatorTool());
        Session session = Session.Empty(SessionId.New());
        AgentTurnResult result = loop.Run(session, Parse("action missing | left=1"));
        result.Outcome.Should().Be(TurnOutcome.ToolUnavailable);
        result.Invocation.Should().BeNull();
        result.Session.ToolInvocations.Should().BeEmpty();
    }

    [Fact]
    public void InvalidArgumentsAreReported()
    {
        AgentLoop loop = CreateLoop(new CalculatorTool());
        Session session = Session.Empty(SessionId.New());
        AgentTurnResult result = loop.Run(session, Parse("action calculator | left=2"));
        result.Outcome.Should().Be(TurnOutcome.InvalidArguments);
        result.Session.ToolInvocations.Should().ContainSingle();
        result.Invocation!.Result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ToolFailureIsRecorded()
    {
        AgentLoop loop = CreateLoop(new FailingTool());
        Session session = Session.Empty(SessionId.New());
        AgentTurnResult result = loop.Run(session, Parse("action failing"));
        result.Outcome.Should().Be(TurnOutcome.ToolFailed);
        result.Session.ToolInvocations.Should().ContainSingle();
    }

    [Fact]
    public void DeniedPermissionIsReportedAndNotExecuted()
    {
        WriteRecordingTool tool = new();
        AgentLoop loop = CreateLoop(tool);
        Session session = Session.Empty(SessionId.New());
        AgentTurnResult result = loop.Run(session, Parse("action writer"));
        result.Outcome.Should().Be(TurnOutcome.PermissionDenied);
        tool.WasExecuted.Should().BeFalse();
        result.Session.ToolInvocations.Should().ContainSingle();
    }

    [Fact]
    public void RepeatedActionIsPrevented()
    {
        AgentLoop loop = CreateLoop(new CalculatorTool());
        Session session = Session.Empty(SessionId.New());
        AgentRunContext context = new(maxSteps: 2);
        IControlledStatement statement = Parse("action calculator | left=2 | right=3");
        AgentTurnResult first = loop.Run(session, context, statement);
        AgentTurnResult second = loop.Run(first.Session, context, statement);
        first.Outcome.Should().Be(TurnOutcome.ToolSucceeded);
        second.Outcome.Should().Be(TurnOutcome.DuplicateAction);
        second.Session.ToolInvocations.Should().ContainSingle();
        context.StepsUsed.Should().Be(2);
        context.RemainingSteps.Should().Be(0);
    }

    [Fact]
    public void FactStatementIsRecorded()
    {
        AgentLoop loop = CreateLoop(new CalculatorTool());
        Session session = Session.Empty(SessionId.New());
        AgentTurnResult result = loop.Run(session, Parse("fact water | boils_at | 100C"));
        result.Outcome.Should().Be(TurnOutcome.FactRecorded);
        Fact fact = result.Session.Facts.Should().ContainSingle().Which;
        fact.Status.Should().Be(FactStatus.UserAsserted);
    }

    [Fact]
    public void GoalStatementIsRecorded()
    {
        AgentLoop loop = CreateLoop(new CalculatorTool());
        Session session = Session.Empty(SessionId.New());
        AgentTurnResult result = loop.Run(session, Parse("goal Summarize the file. | A summary exists."));
        result.Outcome.Should().Be(TurnOutcome.GoalRecorded);
        Goal goal = result.Session.Goals.Should().ContainSingle().Which;
        goal.Status.Should().Be(GoalStatus.Incomplete);
    }

    [Fact]
    public void ConstraintStatementIsReportedUnsupported()
    {
        AgentLoop loop = CreateLoop(new CalculatorTool());
        Session session = Session.Empty(SessionId.New());
        AgentTurnResult result = loop.Run(session, Parse("constraint Do not change other sections."));
        result.Outcome.Should().Be(TurnOutcome.ConstraintNotSupported);
        result.Session.Goals.Should().BeEmpty();
    }

    [Fact]
    public void OriginalSessionIsNotMutated()
    {
        AgentLoop loop = CreateLoop(new CalculatorTool());
        Session session = Session.Empty(SessionId.New());
        loop.Run(session, Parse("action calculator | left=2 | right=3"));
        session.ToolInvocations.Should().BeEmpty();
    }

    [Fact]
    public void NullStatementThrows()
    {
        AgentLoop loop = CreateLoop(new CalculatorTool());

        // Cast disambiguates the null literal between the statement and request overloads.
        FluentActions.Invoking(() => loop.Run(Session.Empty(SessionId.New()), (IControlledStatement)null!)).Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void NullCapabilityRequestThrows()
    {
        AgentLoop loop = CreateLoop(new CalculatorTool());
        FluentActions.Invoking(() => loop.Run(Session.Empty(SessionId.New()), (CapabilityRequest)null!)).Should().ThrowExactly<ArgumentNullException>();
    }

    private static AgentLoop CreateLoop(params ITool[] tools)
    {
        return new(new ToolRegistry(tools), ToolPermissionPolicy.ReadOnlyOnly);
    }

    private static IControlledStatement Parse(string input)
    {
        Result<IControlledStatement> result = ControlledLanguageParser.Parse(input);
        result.IsSuccess.Should().BeTrue("{0}", result.Error?.Message ?? string.Empty);
        return result.Value;
    }
}
