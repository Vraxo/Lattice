using System.Collections.Immutable;
namespace Lattice.Core;
/// <summary>
/// A represented edit request: what transformation is wanted, what it applies to, what must be
/// preserved, and any parameters. Representing an operation does not imply the editor can perform
/// it; <see cref="Status"/> reports the difference and <see cref="RequireExecutable"/> refuses an
/// unsupported one so a caller cannot claim an operation it did not perform.
/// </summary>
public sealed record EditInstruction
{
    private EditInstruction(
        EditOperationKind operation,
        EditTarget target,
        PreservationConstraint constraints,
        ImmutableDictionary<string, string> parameters)
    {
        Operation = operation;
        Target = target;
        Constraints = constraints;
        Parameters = parameters;
    }
    public EditOperationKind Operation { get; }
    public EditTarget Target { get; }
    public PreservationConstraint Constraints { get; }
    public ImmutableDictionary<string, string> Parameters { get; }
    public EditInstructionStatus Status => EditOperationSupport.StatusOf(Operation);
    public bool IsExecutable => Status == EditInstructionStatus.Executable;
    public static EditInstruction Create(
        EditOperationKind operation,
        EditTarget target,
        PreservationConstraint constraints = PreservationConstraint.None,
        IReadOnlyDictionary<string, string>? parameters = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ImmutableDictionary<string, string>.Builder builder =
            ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        if (parameters is not null)
        {
            foreach (KeyValuePair<string, string> pair in parameters)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(pair.Key);
                ArgumentNullException.ThrowIfNull(pair.Value);
                builder[pair.Key] = pair.Value;
            }
        }
        return new EditInstruction(operation, target, constraints, builder.ToImmutable());
    }
    /// <summary>
    /// Succeeds only when the current editor can perform this operation. An unsupported operation is
    /// represented faithfully but refused, so it is never reported as applied.
    /// </summary>
    public Result RequireExecutable()
    {
        if (IsExecutable)
        {
            return Result.Success();
        }
        return Result.Failure(new Error(
            EditErrorCodes.OperationUnsupported,
            $"Edit operation '{Operation}' is not supported by the current editor; it was represented but not applied."));
    }
    // ImmutableDictionary<TKey, TValue> compares by reference to its backing store, so the record's
    // synthesized equality would treat two equal-but-distinct parameter maps as unequal. Compare
    // parameters by content instead.
    public bool Equals(EditInstruction? other)
    {
        if (other is null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        return Operation == other.Operation
            && Target == other.Target
            && Constraints == other.Constraints
            && ParametersEqual(Parameters, other.Parameters);
    }
    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(Operation);
        hash.Add(Target);
        hash.Add(Constraints);
        // Order by key so the hash is independent of dictionary enumeration order.
        foreach (KeyValuePair<string, string> pair in Parameters.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            hash.Add(pair.Key, StringComparer.Ordinal);
            hash.Add(pair.Value, StringComparer.Ordinal);
        }
        return hash.ToHashCode();
    }
    private static bool ParametersEqual(
        ImmutableDictionary<string, string> left,
        ImmutableDictionary<string, string> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }
        foreach (KeyValuePair<string, string> pair in left)
        {
            if (!right.TryGetValue(pair.Key, out string? value)
                || !string.Equals(pair.Value, value, StringComparison.Ordinal))
            {
                return false;
            }
        }
        return true;
    }
}