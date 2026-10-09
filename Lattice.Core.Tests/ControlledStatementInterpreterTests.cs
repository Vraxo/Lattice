namespace Lattice.Core.Tests;
public sealed class ControlledStatementInterpreterTests
{
    [Fact]
    public void ActionBecomesInvokeCandidateWithoutStateChange()
    {
        var session = Session.Empty(SessionId.New());
        var interpreted = ControlledStatementInterpreter.Interpret(session, Parse("action calculator | left=2 | right=3"));
        Assert.False(interpreted.IsResolved);
        var candidate = Assert.IsType<InvokeToolProposal>(Assert.Single(interpreted.Candidates));
        Assert.Equal("calculator", candidate.ToolId.Value);
        Assert.Empty(interpreted.Session.ToolInvocations);
    }
    [Fact]
    public void FactIsResolvedAndAppliesStateChange()
    {
        var session = Session.Empty(SessionId.New());
        var interpreted = ControlledStatementInterpreter.Interpret(session, Parse("fact water | boils_at | 100C"));
        Assert.True(interpreted.IsResolved);
        Assert.Equal(TurnOutcome.FactRecorded, interpreted.ResolvedOutcome);
        Assert.Single(interpreted.Session.Facts);
        Assert.Empty(interpreted.Candidates);
    }
    [Fact]
    public void GoalIsResolvedAndAppliesStateChange()
    {
        var session = Session.Empty(SessionId.New());
        var interpreted = ControlledStatementInterpreter.Interpret(session, Parse("goal Do it. | It is done."));
        Assert.True(interpreted.IsResolved);
        Assert.Equal(TurnOutcome.GoalRecorded, interpreted.ResolvedOutcome);
        Assert.Single(interpreted.Session.Goals);
    }
    [Fact]
    public void ConstraintIsResolvedAsUnsupported()
    {
        var session = Session.Empty(SessionId.New());
        var interpreted = ControlledStatementInterpreter.Interpret(session, Parse("constraint Be brief."));
        Assert.True(interpreted.IsResolved);
        Assert.Equal(TurnOutcome.ConstraintNotSupported, interpreted.ResolvedOutcome);
        Assert.Equal(session, interpreted.Session);
    }
    [Fact]
    public void OriginalSessionIsNotMutated()
    {
        var session = Session.Empty(SessionId.New());
        ControlledStatementInterpreter.Interpret(session, Parse("fact a | b | c"));
        Assert.Empty(session.Facts);
    }
    private static IControlledStatement Parse(string input)
    {
        var result = ControlledLanguageParser.Parse(input);
        Assert.True(result.IsSuccess, result.Error?.Message);
        return result.Value;
    }
}