namespace Lattice.Core.Tests;

public sealed class SessionTests
{
    [Fact]
    public void EmptySessionHasNoContent()
    {
        Session session = Session.Empty(SessionId.New());
        Assert.Empty(session.Messages);
        Assert.Empty(session.Facts);
        Assert.Empty(session.Goals);
        Assert.Empty(session.Observations);
        Assert.Empty(session.ToolInvocations);
    }

    [Fact]
    public void AddUserAssertionAddsUserAssertedFact()
    {
        Session session = Session.Empty(SessionId.New());
        Session updated = session.AddUserAssertion("The sky is blue.", "user:turn-1");
        Fact fact = Assert.Single(updated.Facts);
        Assert.Equal("The sky is blue.", fact.Content);
        Assert.Equal("user:turn-1", fact.Source);
        Assert.Equal(FactStatus.UserAsserted, fact.Status);
    }

    [Fact]
    public void AddUserAssertionLeavesOriginalSessionUnchanged()
    {
        Session session = Session.Empty(SessionId.New());
        session.AddUserAssertion("The sky is blue.", "user:turn-1");
        Assert.Empty(session.Facts);
    }

    [Fact]
    public void AddToolObservationAddsObservation()
    {
        Session session = Session.Empty(SessionId.New());
        Session updated = session.AddToolObservation("exit code 0", "tool:dotnet-test");
        Observation observation = Assert.Single(updated.Observations);
        Assert.Equal("exit code 0", observation.Content);
        Assert.Equal("tool:dotnet-test", observation.Source);
    }

    [Fact]
    public void AddToolObservationDoesNotCreateFact()
    {
        Session session = Session.Empty(SessionId.New());
        Session updated = session.AddToolObservation("exit code 0", "tool:dotnet-test");
        Assert.Empty(updated.Facts);
    }

    [Fact]
    public void AddToolObservationLeavesOriginalSessionUnchanged()
    {
        Session session = Session.Empty(SessionId.New());
        session.AddToolObservation("exit code 0", "tool:dotnet-test");
        Assert.Empty(session.Observations);
    }

    [Fact]
    public void AddMessageAppendsMessage()
    {
        Session session = Session.Empty(SessionId.New());
        Session updated = session.AddMessage(new Message(MessageRole.User, "hello"));
        Message message = Assert.Single(updated.Messages);
        Assert.Equal(MessageRole.User, message.Role);
        Assert.Equal("hello", message.Text);
    }

    [Fact]
    public void AddGoalAppendsGoal()
    {
        Session session = Session.Empty(SessionId.New());
        Goal goal = new(GoalId.New(), "Do it.", "It is done.", GoalStatus.Incomplete);
        Session updated = session.AddGoal(goal);
        Assert.Equal(goal, Assert.Single(updated.Goals));
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
        Assert.Equal(invocation, Assert.Single(updated.ToolInvocations));
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
        Assert.Empty(session.ToolInvocations);
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
        Assert.Equal(first, second);
    }

    [Fact]
    public void SessionsWithDifferentFactsAreNotEqual()
    {
        SessionId id = SessionId.New();
        Session first = new(id, facts: [new Fact(FactId.New(), "a", "s", FactStatus.UserAsserted)]);
        Session second = new(id, facts: [new Fact(FactId.New(), "b", "s", FactStatus.UserAsserted)]);
        Assert.NotEqual(first, second);
    }
}