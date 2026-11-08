using System.Collections.Immutable;

namespace Lattice.Core;

/// <summary>
/// Locates non-linked workspace files by exact, case-insensitive basename without reading file
/// contents. Reuses <see cref="WorkspaceFileAccess"/> so workspace confinement and static
/// symlink/reparse-point protections remain in one place.
/// </summary>
public sealed class WorkspaceFileLocator
{
    public const int DefaultMaxReturnedMatches = 100;
    public const int DefaultMaxCountedMatches = 500;
    public const int DefaultMaxScannedFiles = 50_000;

    public static ImmutableArray<string> DefaultExcludedDirectories { get; } =
        [".git", ".vs", "artifacts", "bin", "obj"];

    private readonly WorkspaceRoot _root;
    private readonly HashSet<string> _exclusions;
    private readonly int _maxReturnedMatches;
    private readonly int _maxCountedMatches;
    private readonly int _maxScannedFiles;

    public WorkspaceFileLocator(
        WorkspaceRoot root,
        IEnumerable<string>? excludedDirectories = null,
        int maxReturnedMatches = DefaultMaxReturnedMatches,
        int maxCountedMatches = DefaultMaxCountedMatches,
        int maxScannedFiles = DefaultMaxScannedFiles)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxReturnedMatches, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCountedMatches, maxReturnedMatches);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxScannedFiles, 1);

        _root = root;
        _exclusions = new HashSet<string>(
            excludedDirectories ?? DefaultExcludedDirectories,
            StringComparer.OrdinalIgnoreCase);
        _maxReturnedMatches = maxReturnedMatches;
        _maxCountedMatches = maxCountedMatches;
        _maxScannedFiles = maxScannedFiles;
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="candidate"/> is a bare filename with no
    /// directory separators, drive/root prefix, or relative navigation segment.
    /// </summary>
    public static bool IsBareFileName(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        string trimmed = candidate.Trim();
        if (trimmed is "." or "..")
        {
            return false;
        }

        if (trimmed.Contains('\0', StringComparison.Ordinal)
            || trimmed.Contains('/', StringComparison.Ordinal)
            || trimmed.Contains('\\', StringComparison.Ordinal)
            || trimmed.Contains(':', StringComparison.Ordinal))
        {
            return false;
        }

        return !Path.IsPathRooted(trimmed);
    }

    /// <summary>
    /// Checks immediate non-linked files in the workspace root for a single case-insensitive
    /// basename match so root-level precedence is consistent across Windows and POSIX filesystems.
    /// </summary>
    public Result<string?> TryFindSingleRootFile(string fileName)
    {
        if (!IsBareFileName(fileName))
        {
            return Result<string?>.Failure(new Error(
                FileToolErrorCodes.QueryInvalid,
                $"'{fileName}' is not a bare filename."));
        }

        Result<string> resolvedRoot = WorkspaceFileAccess.ResolveReadableDirectory(_root, ".");
        if (!resolvedRoot.IsSuccess)
        {
            return Result<string?>.Failure(resolvedRoot.Error!);
        }

        string target = fileName.Trim();
        List<string> rootMatches = [];
        foreach ((string fullPath, bool isDirectory) in WorkspaceFileAccess.EnumerateDirectoryEntriesSafely(resolvedRoot.Value))
        {
            if (isDirectory || IsExcluded(fullPath))
            {
                continue;
            }

            string entryName = Path.GetFileName(fullPath);
            if (entryName.Equals(target, StringComparison.OrdinalIgnoreCase))
            {
                rootMatches.Add(entryName);
            }
        }

        return rootMatches.Count == 1
            ? Result<string?>.Success(rootMatches[0])
            : Result<string?>.Success(null);
    }

    /// <summary>
    /// Searches the connected workspace for files whose basename matches <paramref name="fileName"/>
    /// exactly (case-insensitively), returning workspace-relative paths without reading file bytes.
    /// </summary>
    public Result<WorkspaceFileLookupResult> FindByFileName(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsBareFileName(fileName))
        {
            return Result<WorkspaceFileLookupResult>.Failure(new Error(
                FileToolErrorCodes.QueryInvalid,
                $"'{fileName}' is not a bare filename."));
        }

        Result<string> resolvedRoot = WorkspaceFileAccess.ResolveReadableDirectory(_root, ".");
        if (!resolvedRoot.IsSuccess)
        {
            return Result<WorkspaceFileLookupResult>.Failure(resolvedRoot.Error!);
        }

        string target = fileName.Trim();
        List<string> matches = [];
        int totalMatches = 0;
        int scannedFiles = 0;
        bool hasMoreMatches = false;

        ScanDirectory(
            resolvedRoot.Value,
            target,
            matches,
            ref totalMatches,
            ref scannedFiles,
            ref hasMoreMatches,
            cancellationToken);

        matches.Sort(CompareRelativePathsDeterministically);
        return Result<WorkspaceFileLookupResult>.Success(new WorkspaceFileLookupResult(
            target,
            [.. matches],
            totalMatches,
            hasMoreMatches));
    }

    private void ScanDirectory(
        string directoryPath,
        string targetFileName,
        List<string> matches,
        ref int totalMatches,
        ref int scannedFiles,
        ref bool hasMoreMatches,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        List<string> files = [];
        List<string> subdirectories = [];
        foreach ((string fullPath, bool isDirectory) in WorkspaceFileAccess.EnumerateDirectoryEntriesSafely(directoryPath))
        {
            if (IsExcluded(fullPath))
            {
                continue;
            }

            if (isDirectory)
            {
                subdirectories.Add(fullPath);
            }
            else
            {
                files.Add(fullPath);
            }
        }

        files.Sort(ComparePathsDeterministically);
        subdirectories.Sort(ComparePathsDeterministically);

        foreach (string filePath in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (scannedFiles >= _maxScannedFiles)
            {
                hasMoreMatches = true;
                return;
            }

            scannedFiles++;
            if (!Path.GetFileName(filePath).Equals(targetFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string relative = NormalizeRelativePath(Path.GetRelativePath(_root.FullPath, filePath));
            if (!_root.TryResolve(relative, out _)
                || WorkspaceFileAccess.ContainsLinkOrReparsePoint(_root, filePath, relative))
            {
                continue;
            }

            if (totalMatches >= _maxCountedMatches)
            {
                hasMoreMatches = true;
                return;
            }

            totalMatches++;
            if (matches.Count < _maxReturnedMatches)
            {
                matches.Add(relative);
            }
        }

        foreach (string subdirectory in subdirectories)
        {
            if (hasMoreMatches)
            {
                return;
            }

            ScanDirectory(
                subdirectory,
                targetFileName,
                matches,
                ref totalMatches,
                ref scannedFiles,
                ref hasMoreMatches,
                cancellationToken);
        }
    }

    private bool IsExcluded(string fullPath)
    {
        if (_exclusions.Count == 0)
        {
            return false;
        }

        string relative = Path.GetRelativePath(_root.FullPath, fullPath);
        foreach (string segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (_exclusions.Contains(segment))
            {
                return true;
            }
        }

        return false;
    }

    private static int ComparePathsDeterministically(string left, string right)
    {
        string leftName = Path.GetFileName(left);
        string rightName = Path.GetFileName(right);
        int insensitive = StringComparer.OrdinalIgnoreCase.Compare(leftName, rightName);
        return insensitive != 0
            ? insensitive
            : StringComparer.Ordinal.Compare(leftName, rightName);
    }

    private static int CompareRelativePathsDeterministically(string left, string right)
    {
        int insensitive = StringComparer.OrdinalIgnoreCase.Compare(left, right);
        return insensitive != 0
            ? insensitive
            : StringComparer.Ordinal.Compare(left, right);
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        return relativePath
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
    }
}
