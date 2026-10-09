using System.Collections.Immutable;

namespace Lattice.Core;

public sealed record Goal
{
    public Goal(
        GoalId id,
        string description,
        string completionCondition,
        GoalStatus status,
        IEnumerable<Constraint>? constraints = null)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Goal description must not be empty.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(completionCondition))
        {
            throw new ArgumentException("Completion condition must not be empty.", nameof(completionCondition));
        }

        Id = id;
        Description = description;
        CompletionCondition = completionCondition;
        Status = status;
        Constraints = constraints?.ToImmutableArray() ?? [];
    }

    public GoalId Id { get; }

    public string Description { get; }

    public string CompletionCondition { get; }

    public GoalStatus Status { get; }

    public ImmutableArray<Constraint> Constraints { get; }

    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct constraint sets as unequal.
    public bool Equals(Goal? other)
    {
        if (other is null)
        {
            return false;
        }

        return Id == other.Id
            && Description == other.Description
            && CompletionCondition == other.CompletionCondition
            && Status == other.Status
            && Constraints.SequenceEqual(other.Constraints);
    }

    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(Id);
        hash.Add(Description);
        hash.Add(CompletionCondition);
        hash.Add(Status);
        foreach (Constraint constraint in Constraints)
        {
            hash.Add(constraint);
        }

        return hash.ToHashCode();
    }
}