using System.Text;

namespace Lattice.Core;

/// <summary>
/// The single read/modify/write path for text files inside a workspace. Both the literal patch tool
/// and the structured document tool go through it, so path confinement, size limits, encoding
/// handling, and the pre-write backup are enforced in exactly one place.
/// </summary>
/// <remarks>
/// Reads are permissive but explicit: a file is decoded using its detected byte-order mark so that
/// a BOM-marked file can be inspected. Writes are guarded — an edit is refused unless the original
/// bytes are UTF-8 with no byte-order mark, so an ordinary edit can never silently change a file's
/// encoding or mark. See <see cref="EnsureEncodingIsPreservable"/>.
/// </remarks>
internal static class WorkspaceFileAccess
{
    public const int MaxBytes = 262_144;
    public const string BackupSuffix = ".lattice-backup";
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>Reads the file, or returns a typed error describing why it could not be read.</summary>
    /// <returns></returns>
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
    /// <remarks>
    /// The original file's bytes are classified before anything is written. Only UTF-8 with no
    /// byte-order mark is edited; every other recognised encoding is refused with
    /// <see cref="FileToolErrorCodes.EncodingPreservationUnsupported"/> so that an ordinary edit can
    /// never silently change a file's encoding or byte-order mark. The refusal happens before the
    /// backup is created and before the target is touched, so a rejected edit leaves the original
    /// byte-for-byte unchanged.
    /// </remarks>
    /// <returns></returns>
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
    /// Classifies the file's bytes and succeeds only when an edit can preserve the original
    /// encoding. Reads bytes rather than text so the classification is of the file as it exists on
    /// disk, not of a possibly already-transcoded string.
    /// </summary>
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

    /// <summary>
    /// Resolves a workspace-relative path and verifies it names an existing, non-directory file
    /// within the size limit.
    /// </summary>
    /// <returns></returns>
    public static Result<string> ResolveReadableFile(WorkspaceRoot root, string requested)
    {
        if (!root.TryResolve(requested, out string? fullPath))
        {
            return Result<string>.Failure(new Error(
                FileToolErrorCodes.PathOutsideRoot,
                $"Path '{requested}' is not inside the workspace root."));
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

        if (new FileInfo(fullPath).Length > MaxBytes)
        {
            return Result<string>.Failure(new Error(
                FileToolErrorCodes.FileTooLarge,
                $"File '{requested}' exceeds the {MaxBytes}-byte limit."));
        }

        return Result<string>.Success(fullPath);
    }
}