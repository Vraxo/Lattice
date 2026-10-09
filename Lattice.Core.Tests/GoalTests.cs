namespace Lattice.Core.Tests;

public sealed class GoalTests
{
    [Fact]
    public void IncompleteGoalReportsIncompleteStatus()
    {
        Goal goal = new(GoalId.New(), "Summarize the file.", "A summary is produced.", GoalStatus.Incomplete);
        Assert.Equal(GoalStatus.Incomplete, goal.Status);
        Assert.Empty(goal.Constraints);
    }

    [Fact]
    public void CompletedGoalReportsCompletedStatus()
    {
        Goal goal = new(GoalId.New(), "Summarize the file.", "A summary is produced.", GoalStatus.Completed);
        Assert.Equal(GoalStatus.Completed, goal.Status);
    }

    [Fact]
    public void BlockedGoalReportsBlockedStatus()
    {
        Goal goal = new(GoalId.New(), "Summarize the file.", "A summary is produced.", GoalStatus.Blocked);
        Assert.Equal(GoalStatus.Blocked, goal.Status);
    }

    [Fact]
    public void ConstraintsArePreservedInOrder()
    {
        Constraint first = new("Do not change other sections.");
        Constraint second = new("Keep the original tone.");
        Goal goal = new(
            GoalId.New(),
            "Edit the section.",
            "The section is rewritten.",
            GoalStatus.Incomplete,
            [first, second]);
        Assert.Equal(new[] { first, second }, goal.Constraints);
    }

    [Fact]
    public void GoalsWithSameComponentsAreEqual()
    {
        GoalId id = GoalId.New();
        Constraint[] constraints = [new Constraint("Be concise.")];
        Goal first = new(id, "Answer the question.", "An answer is given.", GoalStatus.Incomplete, constraints);
        Goal second = new(id, "Answer the question.", "An answer is given.", GoalStatus.Incomplete, constraints);
        Assert.Equal(first, second);
    }

    [Fact]
    public void GoalsWithDifferentStatusAreNotEqual()
    {
        GoalId id = GoalId.New();
        Goal incomplete = new(id, "Answer the question.", "An answer is given.", GoalStatus.Incomplete);
        Goal completed = new(id, "Answer the question.", "An answer is given.", GoalStatus.Completed);
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