using System.Text;

namespace Lattice.Core;

/// <summary>
/// Applies a single text patch to a file inside the workspace and writes the result. Because
/// this tool has <see cref="ToolSideEffect.LocalWrite"/>, <see cref="ToolExecutor"/> refuses to
/// invoke it unless the active policy grants that side effect, so a denied write never reaches
/// this code and the file is left untouched.
/// </summary>
public sealed class WriteFileTool : ITool
{
    public const int MaxBytes = 262_144;
    public const string BackupSuffix = ".lattice-backup";
    private const string PathParameter = "path";
    private const string FindParameter = "find";
    private const string ReplaceParameter = "replace";
    private readonly WorkspaceRoot _root;

    public WriteFileTool(WorkspaceRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        _root = root;
    }

    public static ToolId Id { get; } = new("files.write");

    public ToolDescriptor Descriptor { get; } = new(
        Id,
        "Applies a single literal patch to a file inside the workspace.",
        "A unified diff of the applied change.",
        ToolSideEffect.LocalWrite,
        [
            new ToolParameter(PathParameter, ToolParameterType.String),
            new ToolParameter(FindParameter, ToolParameterType.String),
            new ToolParameter(ReplaceParameter, ToolParameterType.String),
        ]);

    public ToolResult Execute(ArgumentBag arguments, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (!TryReadString(arguments, PathParameter, out string? requested)
            || !TryReadString(arguments, FindParameter, out string? find)
            || !TryReadString(arguments, ReplaceParameter, out string? replace))
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.WriteFailed,
                "Arguments 'path', 'find', and 'replace' must all be strings."));
        }

        if (find.Length == 0)
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.WriteFailed,
                "Argument 'find' must not be empty."));
        }

        if (!_root.TryResolve(requested, out string? fullPath))
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.PathOutsideRoot,
                $"Path '{requested}' is not inside the workspace root."));
        }

        if (Directory.Exists(fullPath))
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.NotAFile,
                $"Path '{requested}' is a directory."));
        }

        if (!File.Exists(fullPath))
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.PathNotFound,
                $"File '{requested}' was not found."));
        }

        if (new FileInfo(fullPath).Length > MaxBytes)
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.FileTooLarge,
                $"File '{requested}' exceeds the {MaxBytes}-byte limit."));
        }

        UTF8Encoding encoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        string original;
        try
        {
            original = File.ReadAllText(fullPath, encoding);
        }
        catch (DecoderFallbackException)
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.EncodingInvalid,
                $"File '{requested}' is not valid UTF-8."));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.ReadFailed,
                $"File '{requested}' could not be read: {exception.Message}"));
        }

        TextPatch patch = new(find, replace);
        PatchPreview preview = TextPatcher.Apply(original, patch);
        if (!preview.IsApplied)
        {
            string code = preview.Status == PatchStatus.ContextNotFound
                ? FileToolErrorCodes.PatchContextNotFound
                : FileToolErrorCodes.PatchContextAmbiguous;
            return ToolResult.Failure(new Error(
                code,
                $"Patch to '{requested}' was not applied ({preview.Status})."));
        }

        string diff = UnifiedDiff.Create(original, preview.PatchedText!);
        try
        {
            // Backup first: if the target write fails partway, the original is still recoverable.
            File.WriteAllText(fullPath + BackupSuffix, original, encoding);
            File.WriteAllText(fullPath, preview.PatchedText!, encoding);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.WriteFailed,
                $"File '{requested}' could not be written: {exception.Message}"));
        }

        return ToolResult.Success(ArgumentValue.FromString(diff));
    }

    private static bool TryReadString(ArgumentBag arguments, string name, out string value)
    {
        value = string.Empty;
        if (!arguments.TryGetValue(name, out ArgumentValue? argument) || argument.Type != ToolParameterType.String)
        {
            return false;
        }

        value = argument.AsString();
        return true;
    }
}