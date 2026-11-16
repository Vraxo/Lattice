namespace Lattice.Core.Tests;

public sealed class GoalTests
{
    [Fact]
    public void IncompleteGoalReportsIncompleteStatus()
    {
        Goal goal = new(GoalId.New(), "Summarize the file.", "A summary is produced.", GoalStatus.Incomplete);
        goal.Status.Should().Be(GoalStatus.Incomplete);
        goal.Constraints.Should().BeEmpty();
    }

    [Fact]
    public void CompletedGoalReportsCompletedStatus()
    {
        Goal goal = new(GoalId.New(), "Summarize the file.", "A summary is produced.", GoalStatus.Completed);
        goal.Status.Should().Be(GoalStatus.Completed);
    }

    [Fact]
    public void BlockedGoalReportsBlockedStatus()
    {
        Goal goal = new(GoalId.New(), "Summarize the file.", "A summary is produced.", GoalStatus.Blocked);
        goal.Status.Should().Be(GoalStatus.Blocked);
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
        goal.Constraints.Should().Equal(new[] { first, second });
    }

    [Fact]
    public void GoalsWithSameComponentsAreEqual()
    {
        GoalId id = GoalId.New();
        Constraint[] constraints = [new Constraint("Be concise.")];
        Goal first = new(id, "Answer the question.", "An answer is given.", GoalStatus.Incomplete, constraints);
        Goal second = new(id, "Answer the question.", "An answer is given.", GoalStatus.Incomplete, constraints);
        second.Should().Be(first);
    }

    [Fact]
    public void GoalsWithDifferentStatusAreNotEqual()
    {
        GoalId id = GoalId.New();
        Goal incomplete = new(id, "Answer the question.", "An answer is given.", GoalStatus.Incomplete);
        Goal completed = new(id, "Answer the question.", "An answer is given.", GoalStatus.Completed);
        completed.Should().NotBe(incomplete);
    }

    [Fact]
    public void RejectsEmptyDescription()
    {
        FluentActions.Invoking(() => new Goal(GoalId.New(), string.Empty, "condition", GoalStatus.Incomplete)).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void RejectsEmptyCompletionCondition()
    {
        FluentActions.Invoking(() => new Goal(GoalId.New(), "description", string.Empty, GoalStatus.Incomplete)).Should().ThrowExactly<ArgumentException>();
    }
}