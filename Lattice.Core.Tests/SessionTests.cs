namespace Lattice.Core.Tests;

public sealed class SessionTests
{
    [Fact]
    public void EmptySessionHasNoContent()
    {
        Session session = Session.Empty(SessionId.New());
        session.Messages.Should().BeEmpty();
        session.Facts.Should().BeEmpty();
        session.Goals.Should().BeEmpty();
        session.Observations.Should().BeEmpty();
        session.ToolInvocations.Should().BeEmpty();
    }

    [Fact]
    public void AddUserAssertionAddsUserAssertedFact()
    {
        Session session = Session.Empty(SessionId.New());
        Session updated = session.AddUserAssertion("The sky is blue.", "user:turn-1");
        Fact fact = updated.Facts.Should().ContainSingle().Which;
        fact.Content.Should().Be("The sky is blue.");
        fact.Source.Should().Be("user:turn-1");
        fact.Status.Should().Be(FactStatus.UserAsserted);
    }

    [Fact]
    public void AddUserAssertionLeavesOriginalSessionUnchanged()
    {
        Session session = Session.Empty(SessionId.New());
        session.AddUserAssertion("The sky is blue.", "user:turn-1");
        session.Facts.Should().BeEmpty();
    }

    [Fact]
    public void AddToolObservationAddsObservation()
    {
        Session session = Session.Empty(SessionId.New());
        Session updated = session.AddToolObservation("exit code 0", "tool:dotnet-test");
        Observation observation = updated.Observations.Should().ContainSingle().Which;
        observation.Content.Should().Be("exit code 0");
        observation.Source.Should().Be("tool:dotnet-test");
    }

    [Fact]
    public void AddToolObservationDoesNotCreateFact()
    {
        Session session = Session.Empty(SessionId.New());
        Session updated = session.AddToolObservation("exit code 0", "tool:dotnet-test");
        updated.Facts.Should().BeEmpty();
    }

    [Fact]
    public void AddToolObservationLeavesOriginalSessionUnchanged()
    {
        Session session = Session.Empty(SessionId.New());
        session.AddToolObservation("exit code 0", "tool:dotnet-test");
        session.Observations.Should().BeEmpty();
    }

    [Fact]
    public void AddMessageAppendsMessage()
    {
        Session session = Session.Empty(SessionId.New());
        Session updated = session.AddMessage(new Message(MessageRole.User, "hello"));
        Message message = updated.Messages.Should().ContainSingle().Which;
        message.Role.Should().Be(MessageRole.User);
        message.Text.Should().Be("hello");
    }

    [Fact]
    public void AddGoalAppendsGoal()
    {
        Session session = Session.Empty(SessionId.New());
        Goal goal = new(GoalId.New(), "Do it.", "It is done.", GoalStatus.Incomplete);
        Session updated = session.AddGoal(goal);
        updated.Goals.Should().ContainSingle().Which.Should().Be(goal);
    }

    [Fact]
    public void AddToolInvocationAppendsInvocation()
    {
        Session session = Session.Empty(SessionId.New());
        ToolInvocation invocation = new(
            CalculatorTool.Id,
            ArgumentBag.Empty,
            ToolResult.Success(ArgumentValue.FromInteger(3)));
        Session updated = session.AddToolInvocation(invocation);
        updated.ToolInvocations.Should().ContainSingle().Which.Should().Be(invocation);
    }

    [Fact]
    public void AddToolInvocationLeavesOriginalSessionUnchanged()
    {
        Session session = Session.Empty(SessionId.New());
        ToolInvocation invocation = new(
            CalculatorTool.Id,
            ArgumentBag.Empty,
            ToolResult.Success(ArgumentValue.FromInteger(3)));
        session.AddToolInvocation(invocation);
        session.ToolInvocations.Should().BeEmpty();
    }

    [Fact]
    public void SessionsWithSameContentAreEqual()
    {
        SessionId id = SessionId.New();
        Session first = new(
            id,
            messages: [new Message(MessageRole.User, "hello")],
            facts: [new Fact(FactId.New(), "content", "source", FactStatus.UserAsserted)]);

        // Rebuild from the same items to prove equality is by value, not by backing array.
        Session second = new(
            id,
            messages: [.. first.Messages],
            facts: [.. first.Facts]);
        second.Should().Be(first);
    }

    [Fact]
    public void SessionsWithDifferentFactsAreNotEqual()
    {
        SessionId id = SessionId.New();
        Session first = new(id, facts: [new Fact(FactId.New(), "a", "s", FactStatus.UserAsserted)]);
        Session second = new(id, facts: [new Fact(FactId.New(), "b", "s", FactStatus.UserAsserted)]);
        second.Should().NotBe(first);
    }
}