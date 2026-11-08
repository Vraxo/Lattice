using System.Collections.Immutable;

namespace Lattice.Desktop;

internal sealed record RecentFileContext(
    RecentFileContextKind Kind,
    string? SingleRelativePath,
    ImmutableArray<string> CandidateRelativePaths)
{
    public static RecentFileContext None { get; } = new(RecentFileContextKind.None, null, []);

    public static RecentFileContext Single(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        return new RecentFileContext(RecentFileContextKind.SingleFile, relativePath, [relativePath]);
    }

    public static RecentFileContext Multiple(ImmutableArray<string> relativePaths)
    {
        return relativePaths.IsDefaultOrEmpty
            ? None
            : new RecentFileContext(RecentFileContextKind.MultipleFiles, null, relativePaths);
    }
}
