namespace Lattice.Core;

/// <summary>
/// Applies a single literal patch to a file inside the workspace and writes the result. Because
/// this tool has <see cref="ToolSideEffect.LocalWrite"/>, <see cref="ToolExecutor"/> refuses to
/// invoke it unless the active policy grants that side effect, so a denied write never reaches
/// this code and the file is left untouched.
/// </summary>
public sealed class WriteFileTool : ITool
{
    public const int MaxBytes = WorkspaceFileAccess.MaxBytes;
    public const string BackupSuffix = WorkspaceFileAccess.BackupSuffix;
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
        if (!ToolArguments.TryReadString(arguments, PathParameter, out string? requested)
            || !ToolArguments.TryReadString(arguments, FindParameter, out string? find)
            || !ToolArguments.TryReadString(arguments, ReplaceParameter, out string? replace))
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

        Result<string> resolved = WorkspaceFileAccess.ResolveReadableFile(_root, requested);
        if (!resolved.IsSuccess)
        {
            return ToolResult.Failure(resolved.Error!);
        }

        Result<string> read = WorkspaceFileAccess.Read(resolved.Value, requested);
        if (!read.IsSuccess)
        {
            return ToolResult.Failure(read.Error!);
        }

        string original = read.Value;
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
        Result write = WorkspaceFileAccess.Write(resolved.Value, requested, original, preview.PatchedText!);
        if (!write.IsSuccess)
        {
            return ToolResult.Failure(write.Error!);
        }

        return ToolResult.Success(ArgumentValue.FromString(diff));
    }
}