using System.Collections.Immutable;
namespace Lattice.Core;
public sealed record ToolPermissionPolicy
{
    private readonly ImmutableArray<ToolSideEffect> _granted;
    private ToolPermissionPolicy(ImmutableArray<ToolSideEffect> granted)
    {
        _granted = granted;
    }
    public static ToolPermissionPolicy ReadOnlyOnly { get; } =
        new(ImmutableArray.Create(ToolSideEffect.ReadOnly));
    public static ToolPermissionPolicy AllowAll { get; } =
        new(ImmutableArray.Create(
            ToolSideEffect.ReadOnly,
            ToolSideEffect.LocalWrite,
            ToolSideEffect.CommandExecution,
            ToolSideEffect.ExternalSideEffect));
    public static ToolPermissionPolicy Grant(IEnumerable<ToolSideEffect> sideEffects)
    {
        ArgumentNullException.ThrowIfNull(sideEffects);
        var normalized = sideEffects
            .Distinct()
            .OrderBy(sideEffect => (int)sideEffect)
            .ToImmutableArray();
        return new(normalized);
    }
    public bool IsAllowed(ToolSideEffect sideEffect) => _granted.Contains(sideEffect);
    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct grants as unequal.
    public bool Equals(ToolPermissionPolicy? other) =>
        other is not null && _granted.SequenceEqual(other._granted);
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var sideEffect in _granted)
        {
            hash.Add(sideEffect);
        }
        return hash.ToHashCode();
    }
}