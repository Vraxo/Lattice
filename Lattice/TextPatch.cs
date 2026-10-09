namespace Lattice.Core;
/// <summary>
/// A single literal text substitution: replace the unique occurrence of <see cref="Find"/>
/// with <see cref="Replace"/>. Matching is exact and case-sensitive.
/// </summary>
public sealed record TextPatch
{
    public TextPatch(string find, string replace)
    {
        ArgumentException.ThrowIfNullOrEmpty(find);
        ArgumentNullException.ThrowIfNull(replace);
        Find = find;
        Replace = replace;
    }
    public string Find { get; }
    public string Replace { get; }
}