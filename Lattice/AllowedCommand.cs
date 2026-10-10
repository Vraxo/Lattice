using System.Collections.Immutable;
namespace Lattice.Core;
/// <summary>A preconfigured executable and its fixed argument list.</summary>
public sealed record AllowedCommand
{
    public AllowedCommand(string executable, IEnumerable<string>? arguments = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        Executable = executable;
        Arguments = arguments?.ToImmutableArray() ?? [];
    }
    public string Executable { get; }
    public ImmutableArray<string> Arguments { get; }
    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct argument sets as unequal.
    public bool Equals(AllowedCommand? other) =>
        other is not null
        && Executable == other.Executable
        && Arguments.SequenceEqual(other.Arguments);
    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(Executable);
        foreach (string argument in Arguments)
        {
            hash.Add(argument);
        }
        return hash.ToHashCode();
    }
}