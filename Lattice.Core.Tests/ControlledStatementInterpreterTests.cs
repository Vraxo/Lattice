namespace Lattice.Core.Tests;

public sealed class ControlledStatementInterpreterTests
{
    [Fact]
    public void ActionBecomesInvokeCandidateWithoutStateChange()
    {
        Session session = Session.Empty(SessionId.New());
        InterpretedStatement interpreted = ControlledStatementInterpreter.Interpret(session, Parse("action calculator | left=2 | right=3"));
        interpreted.IsResolved.Should().BeFalse();
        InvokeToolProposal candidate = interpreted.Candidates.Should().ContainSingle().Which.Should().BeOfType<InvokeToolProposal>().Which;
        candidate.ToolId.Value.Should().Be("calculator");
        interpreted.Session.ToolInvocations.Should().BeEmpty();
    }

    [Fact]
    public void FactIsResolvedAndAppliesStateChange()
    {
        Session session = Session.Empty(SessionId.New());
        InterpretedStatement interpreted = ControlledStatementInterpreter.Interpret(session, Parse("fact water | boils_at | 100C"));
        interpreted.IsResolved.Should().BeTrue();
        interpreted.ResolvedOutcome.Should().Be(TurnOutcome.FactRecorded);
        interpreted.Session.Facts.Should().ContainSingle();
        interpreted.Candidates.Should().BeEmpty();
    }

    [Fact]
    public void GoalIsResolvedAndAppliesStateChange()
    {
        Session session = Session.Empty(SessionId.New());
        InterpretedStatement interpreted = ControlledStatementInterpreter.Interpret(session, Parse("goal Do it. | It is done."));
        interpreted.IsResolved.Should().BeTrue();
        interpreted.ResolvedOutcome.Should().Be(TurnOutcome.GoalRecorded);
        interpreted.Session.Goals.Should().ContainSingle();
    }

    [Fact]
    public void ConstraintIsResolvedAsUnsupported()
    {
        Session session = Session.Empty(SessionId.New());
        InterpretedStatement interpreted = ControlledStatementInterpreter.Interpret(session, Parse("constraint Be brief."));
        interpreted.IsResolved.Should().BeTrue();
        interpreted.ResolvedOutcome.Should().Be(TurnOutcome.ConstraintNotSupported);
        interpreted.Session.Should().Be(session);
    }

    [Fact]
    public void OriginalSessionIsNotMutated()
    {
        Session session = Session.Empty(SessionId.New());
        ControlledStatementInterpreter.Interpret(session, Parse("fact a | b | c"));
        session.Facts.Should().BeEmpty();
    }

    private static IControlledStatement Parse(string input)
    {
        Result<IControlledStatement> result = ControlledLanguageParser.Parse(input);
        result.IsSuccess.Should().BeTrue("{0}", result.Error?.Message ?? string.Empty);
        return result.Value;
    }
}