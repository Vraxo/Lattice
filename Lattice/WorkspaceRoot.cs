namespace Lattice.Core;

/// <summary>
/// A directory that file tools are confined to. Resolution rejects absolute paths and any
/// path that would escape the root.
/// </summary>
/// <remarks>
/// The containment check is lexical. A symbolic link inside the root that points outside it
/// is not detected; resolving links is deferred until there is a demonstrated need.
/// </remarks>
public sealed class WorkspaceRoot
{
    public WorkspaceRoot(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        string full = Path.GetFullPath(rootPath);
        if (!Directory.Exists(full))
        {
            throw new DirectoryNotFoundException($"Workspace root '{full}' does not exist.");
        }

        FullPath = full;
    }

    public string FullPath { get; }

    public bool TryResolve(string relativePath, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        if (Path.IsPathRooted(relativePath))
        {
            return false;
        }

        string candidate = Path.GetFullPath(Path.Combine(FullPath, relativePath));
        string relative = Path.GetRelativePath(FullPath, candidate);

        // A path that escapes the root yields "..", "../x", or (across volumes) a rooted path.
        if (Path.IsPathRooted(relative))
        {
            return false;
        }

        if (relative == ".."
            || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
        {
            return false;
        }

        fullPath = candidate;
        return true;
    }
}