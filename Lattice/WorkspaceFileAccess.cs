using System.Text;

namespace Lattice.Core;

/// <summary>
/// The single read/enumerate/modify/write boundary for files and directories inside a workspace.
/// Workspace tools route path validation, static symlink/reparse-point checks, size limits,
/// encoding checks, and pre-write backups through this class so the safety boundary is enforced
/// in one place.
/// </summary>
/// <remarks>
/// Threat model for link handling: direct file/directory resolution rejects paths when the
/// workspace root or any root-to-target segment is a symbolic link or reparse point, and
/// directory/search traversal skips linked files and linked directories rather than following
/// them. This is a conservative static check performed before opening or traversing entries; it
/// is not race-resistant against an attacker concurrently replacing a directory or file with a
/// link between check and use.
/// </remarks>
internal static class WorkspaceFileAccess
{
    public const int MaxBytes = 262_144;
    public const string BackupSuffix = ".lattice-backup";
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>Reads the file, or returns a typed error describing why it could not be read.</summary>
    public static Result<string> Read(string fullPath, string requested)
    {
        try
        {
            return Result<string>.Success(File.ReadAllText(fullPath, StrictUtf8));
        }
        catch (DecoderFallbackException)
        {
            return Result<string>.Failure(new Error(
                FileToolErrorCodes.EncodingInvalid,
                $"File '{requested}' is not valid UTF-8."));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Result<string>.Failure(new Error(
                FileToolErrorCodes.ReadFailed,
                $"File '{requested}' could not be read: {exception.Message}"));
        }
    }

    /// <summary>
    /// Writes a backup of <paramref name="original"/> before replacing the file.
    /// </summary>
    public static Result Write(string fullPath, string requested, string original, string updated)
    {
        Result encoding = EnsureEncodingIsPreservable(fullPath, requested);
        if (!encoding.IsSuccess)
        {
            return encoding;
        }

        try
        {
            // Backup first: if the target write fails partway, the original is still recoverable.
            File.WriteAllText(fullPath + BackupSuffix, original, StrictUtf8);
            File.WriteAllText(fullPath, updated, StrictUtf8);
            return Result.Success();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Result.Failure(new Error(
                FileToolErrorCodes.WriteFailed,
                $"File '{requested}' could not be written: {exception.Message}"));
        }
    }

    /// <summary>
    /// Resolves a workspace-relative path and verifies it names an existing, non-linked,
    /// non-directory file within the size limit.
    /// </summary>
    public static Result<string> ResolveReadableFile(WorkspaceRoot root, string requested)
    {
        if (!root.TryResolve(requested, out string? fullPath))
        {
            return Result<string>.Failure(new Error(
                FileToolErrorCodes.PathOutsideRoot,
                $"Path '{requested}' is not inside the workspace root."));
        }

        if (ContainsLinkOrReparsePoint(root, fullPath, requested))
        {
            return Result<string>.Failure(new Error(
                FileToolErrorCodes.ReparsePointNotAllowed,
                $"Path '{requested}' traverses a symbolic link or reparse point."));
        }

        if (Directory.Exists(fullPath))
        {
            return Result<string>.Failure(new Error(
                FileToolErrorCodes.NotAFile,
                $"Path '{requested}' is a directory."));
        }

        if (!File.Exists(fullPath))
        {
            return Result<string>.Failure(new Error(
                FileToolErrorCodes.PathNotFound,
                $"File '{requested}' was not found."));
        }

        FileInfo info = new(fullPath);
        if (info.Length > MaxBytes)
        {
            return Result<string>.Failure(new Error(
                FileToolErrorCodes.FileTooLarge,
                $"File '{requested}' is {info.Length} bytes; the limit is {MaxBytes}."));
        }

        return Result<string>.Success(fullPath);
    }

    /// <summary>
    /// Resolves a workspace-relative path and verifies it names an existing, non-linked directory.
    /// </summary>
    public static Result<string> ResolveReadableDirectory(WorkspaceRoot root, string requested)
    {
        if (!root.TryResolve(requested, out string? fullPath))
        {
            return Result<string>.Failure(new Error(
                FileToolErrorCodes.PathOutsideRoot,
                $"Path '{requested}' is not inside the workspace root."));
        }

        if (ContainsLinkOrReparsePoint(root, fullPath, requested))
        {
            return Result<string>.Failure(new Error(
                FileToolErrorCodes.ReparsePointNotAllowed,
                $"Path '{requested}' traverses a symbolic link or reparse point."));
        }

        if (!Directory.Exists(fullPath))
        {
            string code = File.Exists(fullPath) ? FileToolErrorCodes.NotADirectory : FileToolErrorCodes.PathNotFound;
            return Result<string>.Failure(new Error(code, $"Directory '{requested}' was not found."));
        }

        return Result<string>.Success(fullPath);
    }

    /// <summary>
    /// Enumerates immediate entries in <paramref name="directoryPath"/>, skipping symbolic links
    /// and reparse points rather than following them.
    /// </summary>
    public static IEnumerable<(string FullPath, bool IsDirectory)> EnumerateDirectoryEntriesSafely(string directoryPath)
    {
        if (IsLinkOrReparsePoint(directoryPath))
        {
            yield break;
        }

        EnumerationOptions options = new()
        {
            RecurseSubdirectories = false,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
        };

        IEnumerable<FileSystemInfo> entries;
        try
        {
            entries = new DirectoryInfo(directoryPath).EnumerateFileSystemInfos("*", options);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (FileSystemInfo entry in entries)
        {
            if (IsEntryLinkOrReparsePoint(entry))
            {
                continue;
            }

            yield return (entry.FullName, entry is DirectoryInfo);
        }
    }

    /// <summary>
    /// Recursively enumerates non-linked files below <paramref name="directoryPath"/>, skipping
    /// linked files and linked directories without traversing into them.
    /// </summary>
    public static IEnumerable<string> EnumerateFilesRecursivelySafely(
        string directoryPath,
        Func<string, bool>? shouldDescendIntoDirectory = null)
    {
        List<string> subdirectories = [];
        foreach ((string fullPath, bool isDirectory) in EnumerateDirectoryEntriesSafely(directoryPath))
        {
            if (isDirectory)
            {
                if (shouldDescendIntoDirectory is null || shouldDescendIntoDirectory(fullPath))
                {
                    subdirectories.Add(fullPath);
                }
            }
            else
            {
                yield return fullPath;
            }
        }

        foreach (string subdirectory in subdirectories)
        {
            foreach (string nestedFile in EnumerateFilesRecursivelySafely(subdirectory, shouldDescendIntoDirectory))
            {
                yield return nestedFile;
            }
        }
    }

    internal static bool ContainsLinkOrReparsePoint(
        WorkspaceRoot root,
        string fullPath,
        string? requestedRelativePath = null)
    {
        if (IsLinkOrReparsePoint(root.FullPath))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(requestedRelativePath)
            && RawSegmentsTraverseLink(root.FullPath, requestedRelativePath))
        {
            return true;
        }

        string relative = Path.GetRelativePath(root.FullPath, fullPath);
        if (relative == ".")
        {
            return false;
        }

        string current = root.FullPath;
        string[] segments = relative.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);
        foreach (string segment in segments)
        {
            current = Path.Combine(current, segment);
            if (IsLinkOrReparsePoint(current))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsLinkOrReparsePoint(string path)
    {
        try
        {
            FileInfo fileInfo = new(path);
            if (fileInfo.LinkTarget is not null)
            {
                return true;
            }

            DirectoryInfo directoryInfo = new(path);
            if (directoryInfo.LinkTarget is not null)
            {
                return true;
            }

            FileAttributes attributes = File.GetAttributes(path);
            return (attributes & FileAttributes.ReparsePoint) != 0;
        }
        catch (Exception exception) when (
            exception is FileNotFoundException
                or DirectoryNotFoundException
                or IOException
                or UnauthorizedAccessException
                or ArgumentException
                or NotSupportedException)
        {
            return false;
        }
    }

    private static bool RawSegmentsTraverseLink(string rootFullPath, string requestedRelativePath)
    {
        List<string> activeSegments = [];
        string[] rawSegments = requestedRelativePath.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);
        foreach (string segment in rawSegments)
        {
            if (segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                if (activeSegments.Count == 0)
                {
                    return true;
                }

                activeSegments.RemoveAt(activeSegments.Count - 1);
                continue;
            }

            activeSegments.Add(segment);
            string candidate = activeSegments.Aggregate(rootFullPath, Path.Combine);
            if (IsLinkOrReparsePoint(candidate))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsEntryLinkOrReparsePoint(FileSystemInfo entry)
    {
        try
        {
            if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                return true;
            }

            if (entry.LinkTarget is not null)
            {
                return true;
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return true;
        }

        return IsLinkOrReparsePoint(entry.FullName);
    }

    private static Result EnsureEncodingIsPreservable(string fullPath, string requested)
    {
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(fullPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Result.Failure(new Error(
                FileToolErrorCodes.ReadFailed,
                $"File '{requested}' could not be read: {exception.Message}"));
        }

        FileEncodingKind kind = FileEncodingDetector.Detect(bytes);
        if (kind == FileEncodingKind.Utf8NoBom)
        {
            return Result.Success();
        }

        string description = kind switch
        {
            FileEncodingKind.Utf8WithBom => "UTF-8 with a byte-order mark",
            FileEncodingKind.Utf16Le => "UTF-16 little-endian",
            FileEncodingKind.Utf16Be => "UTF-16 big-endian",
            FileEncodingKind.Utf32Le => "UTF-32 little-endian",
            FileEncodingKind.Utf32Be => "UTF-32 big-endian",
            _ => "an unrecognised encoding",
        };
        if (kind == FileEncodingKind.Unknown)
        {
            return Result.Failure(new Error(
                FileToolErrorCodes.EncodingInvalid,
                $"File '{requested}' is not valid UTF-8."));
        }

        return Result.Failure(new Error(
            FileToolErrorCodes.EncodingPreservationUnsupported,
            $"File '{requested}' is {description}; editing it could change its encoding, so the edit was refused."));
    }
}
