namespace Lattice.Core;
/// <summary>
/// A requirement an edit must satisfy. Flags so constraints can be combined; an instruction with
/// <see cref="None"/> states no preservation requirement.
/// </summary>
[Flags]
public enum PreservationConstraint
{
    None = 0,
    /// <summary>The edit must not change the meaning of the content.</summary>
    PreserveMeaning = 1,
    /// <summary>The edit must change only the target, not surrounding content.</summary>
    RestrictToTarget = 2,
    /// <summary>Content outside the target must remain exactly as it was.</summary>
    PreserveUntouchedContent = 4,
}