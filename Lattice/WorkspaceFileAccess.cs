using System.Text;
namespace Lattice.Core;
/// <summary>
/// The single read/modify/write path for text files inside a workspace. Both the literal patch tool
/// and the structured document tool go through it, so path confinement, size limits, encoding
/// handling, and the pre-write backup are enforced in exactly one place.
/// </summary>
/// <remarks>
/// Files are read as strict UTF-8: invalid byte sequences are rejected rather than replaced. A
/// byte-order mark is consumed on read and is <b>not</b> written back, so a file that began with a
/// BOM will not have one after an edit. This behaviour is deliberate and covered by tests; it is
/// not byte-level preservation.
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
    /// <summary>Writes a backup of <paramref name="original"/> before replacing the file.</summary>
    public static Result Write(string fullPath, string requested, string original, string updated)
    {
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
    /// Resolves a workspace-relative path and verifies it names an existing, non-directory file
    /// within the size limit.
    /// </summary>
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