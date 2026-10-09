namespace Lattice.Core.Tests;
public sealed class AgentLoopTests
{
    [Fact]
    public void SuccessfulControlledActionExecutesAndRecords()
    {
        var loop = CreateLoop(new CalculatorTool());
        var session = Session.Empty(SessionId.New());
        var result = loop.Run(session, Parse("action calculator | left=2 | right=3"));
        Assert.Equal(TurnOutcome.ToolSucceeded, result.Outcome);
        Assert.NotNull(result.Invocation);
        Assert.Equal(5, result.Invocation!.Result.Output!.AsInteger());
        Assert.Single(result.Session.ToolInvocations);
    }
    [Fact]
    public void UnavailableToolIsReportedAndNothingRecorded()
    {
        var loop = CreateLoop(new CalculatorTool());
        var session = Session.Empty(SessionId.New());
        var result = loop.Run(session, Parse("action missing | left=1"));
        Assert.Equal(TurnOutcome.ToolUnavailable, result.Outcome);
        Assert.Null(result.Invocation);
        Assert.Empty(result.Session.ToolInvocations);
    }
    [Fact]
    public void InvalidArgumentsAreReported()
    {
        var loop = CreateLoop(new CalculatorTool());
        var session = Session.Empty(SessionId.New());
        var result = loop.Run(session, Parse("action calculator | left=2"));
        Assert.Equal(TurnOutcome.InvalidArguments, result.Outcome);
        Assert.Single(result.Session.ToolInvocations);
        Assert.False(result.Invocation!.Result.IsSuccess);
    }
    [Fact]
    public void ToolFailureIsRecorded()
    {
        var loop = CreateLoop(new FailingTool());
        var session = Session.Empty(SessionId.New());
        var result = loop.Run(session, Parse("action failing"));
        Assert.Equal(TurnOutcome.ToolFailed, result.Outcome);
        Assert.Single(result.Session.ToolInvocations);
    }
    [Fact]
    public void DeniedPermissionIsReportedAndNotExecuted()
    {
        var tool = new WriteRecordingTool();
        var loop = CreateLoop(tool);
        var session = Session.Empty(SessionId.New());
        var result = loop.Run(session, Parse("action writer"));
        Assert.Equal(TurnOutcome.PermissionDenied, result.Outcome);
        Assert.False(tool.WasExecuted);
        Assert.Single(result.Session.ToolInvocations);
    }
    [Fact]
    public void RepeatedActionIsPrevented()
    {
        var loop = CreateLoop(new CalculatorTool());
        var session = Session.Empty(SessionId.New());
        var statement = Parse("action calculator | left=2 | right=3");
        var first = loop.Run(session, statement);
        var second = loop.Run(first.Session, statement);
        Assert.Equal(TurnOutcome.ToolSucceeded, first.Outcome);
        Assert.Equal(TurnOutcome.DuplicateAction, second.Outcome);
        Assert.Single(second.Session.ToolInvocations);
    }
    [Fact]
    public void FactStatementIsRecorded()
    {
        var loop = CreateLoop(new CalculatorTool());
        var session = Session.Empty(SessionId.New());
        var result = loop.Run(session, Parse("fact water | boils_at | 100C"));
        Assert.Equal(TurnOutcome.FactRecorded, result.Outcome);
        var fact = Assert.Single(result.Session.Facts);
        Assert.Equal(FactStatus.UserAsserted, fact.Status);
    }
    [Fact]
    public void GoalStatementIsRecorded()
    {
        var loop = CreateLoop(new CalculatorTool());
        var session = Session.Empty(SessionId.New());
        var result = loop.Run(session, Parse("goal Summarize the file. | A summary exists."));
        Assert.Equal(TurnOutcome.GoalRecorded, result.Outcome);
        var goal = Assert.Single(result.Session.Goals);
        Assert.Equal(GoalStatus.Incomplete, goal.Status);
    }
    [Fact]
    public void ConstraintStatementIsReportedUnsupported()
    {
        var loop = CreateLoop(new CalculatorTool());
        var session = Session.Empty(SessionId.New());
        var result = loop.Run(session, Parse("constraint Do not change other sections."));
        Assert.Equal(TurnOutcome.ConstraintNotSupported, result.Outcome);
        Assert.Empty(result.Session.Goals);
    }
    [Fact]
    public void OriginalSessionIsNotMutated()
    {
        var loop = CreateLoop(new CalculatorTool());
        var session = Session.Empty(SessionId.New());
        loop.Run(session, Parse("action calculator | left=2 | right=3"));
        Assert.Empty(session.ToolInvocations);
    }
    [Fact]
    public void NullStatementThrows()
    {
        var loop = CreateLoop(new CalculatorTool());
        Assert.Throws<ArgumentNullException>(() => loop.Run(Session.Empty(SessionId.New()), null!));
    }
    private static AgentLoop CreateLoop(params ITool[] tools) =>
        new(new ToolRegistry(tools), ToolPermissionPolicy.ReadOnlyOnly);
    private static IControlledStatement Parse(string input)
    {
        var result = ControlledLanguageParser.Parse(input);
        Assert.True(result.IsSuccess, result.Error?.Message);
        return result.Value;
    }
}