namespace Lattice.Core.Tests;
public sealed class SessionTests
{
    [Fact]
    public void EmptySessionHasNoContent()
    {
        var session = Session.Empty(SessionId.New());
        Assert.Empty(session.Messages);
        Assert.Empty(session.Facts);
        Assert.Empty(session.Goals);
        Assert.Empty(session.Observations);
        Assert.Empty(session.ToolInvocations);
    }
    [Fact]
    public void AddUserAssertionAddsUserAssertedFact()
    {
        var session = Session.Empty(SessionId.New());
        var updated = session.AddUserAssertion("The sky is blue.", "user:turn-1");
        var fact = Assert.Single(updated.Facts);
        Assert.Equal("The sky is blue.", fact.Content);
        Assert.Equal("user:turn-1", fact.Source);
        Assert.Equal(FactStatus.UserAsserted, fact.Status);
    }
    [Fact]
    public void AddUserAssertionLeavesOriginalSessionUnchanged()
    {
        var session = Session.Empty(SessionId.New());
        session.AddUserAssertion("The sky is blue.", "user:turn-1");
        Assert.Empty(session.Facts);
    }
    [Fact]
    public void AddToolObservationAddsObservation()
    {
        var session = Session.Empty(SessionId.New());
        var updated = session.AddToolObservation("exit code 0", "tool:dotnet-test");
        var observation = Assert.Single(updated.Observations);
        Assert.Equal("exit code 0", observation.Content);
        Assert.Equal("tool:dotnet-test", observation.Source);
    }
    [Fact]
    public void AddToolObservationDoesNotCreateFact()
    {
        var session = Session.Empty(SessionId.New());
        var updated = session.AddToolObservation("exit code 0", "tool:dotnet-test");
        Assert.Empty(updated.Facts);
    }
    [Fact]
    public void AddToolObservationLeavesOriginalSessionUnchanged()
    {
        var session = Session.Empty(SessionId.New());
        session.AddToolObservation("exit code 0", "tool:dotnet-test");
        Assert.Empty(session.Observations);
    }
    [Fact]
    public void AddMessageAppendsMessage()
    {
        var session = Session.Empty(SessionId.New());
        var updated = session.AddMessage(new Message(MessageRole.User, "hello"));
        var message = Assert.Single(updated.Messages);
        Assert.Equal(MessageRole.User, message.Role);
        Assert.Equal("hello", message.Text);
    }
    [Fact]
    public void AddGoalAppendsGoal()
    {
        var session = Session.Empty(SessionId.New());
        var goal = new Goal(GoalId.New(), "Do it.", "It is done.", GoalStatus.Incomplete);
        var updated = session.AddGoal(goal);
        Assert.Equal(goal, Assert.Single(updated.Goals));
    }
    [Fact]
    public void AddToolInvocationAppendsInvocation()
    {
        var session = Session.Empty(SessionId.New());
        var invocation = new ToolInvocation(
            CalculatorTool.Id,
            ArgumentBag.Empty,
            ToolResult.Success(ArgumentValue.FromInteger(3)));
        var updated = session.AddToolInvocation(invocation);
        Assert.Equal(invocation, Assert.Single(updated.ToolInvocations));
    }
    [Fact]
    public void AddToolInvocationLeavesOriginalSessionUnchanged()
    {
        var session = Session.Empty(SessionId.New());
        var invocation = new ToolInvocation(
            CalculatorTool.Id,
            ArgumentBag.Empty,
            ToolResult.Success(ArgumentValue.FromInteger(3)));
        session.AddToolInvocation(invocation);
        Assert.Empty(session.ToolInvocations);
    }
    [Fact]
    public void SessionsWithSameContentAreEqual()
    {
        var id = SessionId.New();
        var first = new Session(
            id,
            messages: new[] { new Message(MessageRole.User, "hello") },
            facts: new[] { new Fact(FactId.New(), "content", "source", FactStatus.UserAsserted) });
        // Rebuild from the same items to prove equality is by value, not by backing array.
        var second = new Session(
            id,
            messages: first.Messages.ToArray(),
            facts: first.Facts.ToArray());
        Assert.Equal(first, second);
    }
    [Fact]
    public void SessionsWithDifferentFactsAreNotEqual()
    {
        var id = SessionId.New();
        var first = new Session(id, facts: new[] { new Fact(FactId.New(), "a", "s", FactStatus.UserAsserted) });
        var second = new Session(id, facts: new[] { new Fact(FactId.New(), "b", "s", FactStatus.UserAsserted) });
        Assert.NotEqual(first, second);
    }
}