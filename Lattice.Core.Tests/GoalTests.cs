namespace Lattice.Core.Tests;
public sealed class GoalTests
{
    [Fact]
    public void IncompleteGoalReportsIncompleteStatus()
    {
        var goal = new Goal(GoalId.New(), "Summarize the file.", "A summary is produced.", GoalStatus.Incomplete);
        Assert.Equal(GoalStatus.Incomplete, goal.Status);
        Assert.Empty(goal.Constraints);
    }
    [Fact]
    public void CompletedGoalReportsCompletedStatus()
    {
        var goal = new Goal(GoalId.New(), "Summarize the file.", "A summary is produced.", GoalStatus.Completed);
        Assert.Equal(GoalStatus.Completed, goal.Status);
    }
    [Fact]
    public void BlockedGoalReportsBlockedStatus()
    {
        var goal = new Goal(GoalId.New(), "Summarize the file.", "A summary is produced.", GoalStatus.Blocked);
        Assert.Equal(GoalStatus.Blocked, goal.Status);
    }
    [Fact]
    public void ConstraintsArePreservedInOrder()
    {
        var first = new Constraint("Do not change other sections.");
        var second = new Constraint("Keep the original tone.");
        var goal = new Goal(
            GoalId.New(),
            "Edit the section.",
            "The section is rewritten.",
            GoalStatus.Incomplete,
            new[] { first, second });
        Assert.Equal(new[] { first, second }, goal.Constraints);
    }
    [Fact]
    public void GoalsWithSameComponentsAreEqual()
    {
        var id = GoalId.New();
        var constraints = new[] { new Constraint("Be concise.") };
        var first = new Goal(id, "Answer the question.", "An answer is given.", GoalStatus.Incomplete, constraints);
        var second = new Goal(id, "Answer the question.", "An answer is given.", GoalStatus.Incomplete, constraints);
        Assert.Equal(first, second);
    }
    [Fact]
    public void GoalsWithDifferentStatusAreNotEqual()
    {
        var id = GoalId.New();
        var incomplete = new Goal(id, "Answer the question.", "An answer is given.", GoalStatus.Incomplete);
        var completed = new Goal(id, "Answer the question.", "An answer is given.", GoalStatus.Completed);
        Assert.NotEqual(incomplete, completed);
    }
    [Fact]
    public void RejectsEmptyDescription()
    {
        Assert.Throws<ArgumentException>(
            () => new Goal(GoalId.New(), string.Empty, "condition", GoalStatus.Incomplete));
    }
    [Fact]
    public void RejectsEmptyCompletionCondition()
    {
        Assert.Throws<ArgumentException>(
            () => new Goal(GoalId.New(), "description", string.Empty, GoalStatus.Incomplete));
    }
}