namespace Lattice.Core;

/// <summary>
/// A directory that file tools are confined to. Resolution rejects absolute paths and any
/// path that would escape the root.
/// </summary>
/// <remarks>
/// <see cref="TryResolve"/> enforces lexical containment within <see cref="FullPath"/>.
/// Shared workspace operations in <see cref="WorkspaceFileAccess"/> layer conservative static
/// symlink and reparse-point checks on top of lexical resolution. Those checks inspect
/// filesystem metadata at access/enumeration time and are not race-resistant against a path
/// component being replaced concurrently with a link (TOCTOU).
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

    public bool IsLinkOrReparsePoint => WorkspaceFileAccess.IsLinkOrReparsePoint(FullPath);

    public bool TryResolve(string relativePath, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(relativePath) || relativePath.Contains('\0', StringComparison.Ordinal))
        {
            return false;
        }

        if (Path.IsPathRooted(relativePath))
        {
            return false;
        }

        string candidate;
        string relative;
        try
        {
            candidate = Path.GetFullPath(Path.Combine(FullPath, relativePath));
            relative = Path.GetRelativePath(FullPath, candidate);
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or NotSupportedException
                or PathTooLongException)
        {
            return false;
        }

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
