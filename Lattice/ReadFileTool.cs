namespace Lattice.Core;

public sealed class ReadFileTool : ITool
{
    public const int MaxBytes = WorkspaceFileAccess.MaxBytes;
    private const string PathParameter = "path";
    private readonly WorkspaceRoot _root;

    public ReadFileTool(WorkspaceRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        _root = root;
    }

    public static ToolId Id { get; } = new("files.read");

    public ToolDescriptor Descriptor { get; } = new(
        Id,
        "Reads a UTF-8 text file inside the workspace.",
        "The file contents as text.",
        ToolSideEffect.ReadOnly,
        [new ToolParameter(PathParameter, ToolParameterType.String)]);

    public ToolResult Execute(ArgumentBag arguments, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        cancellationToken.ThrowIfCancellationRequested();
        if (!arguments.TryGetValue(PathParameter, out ArgumentValue? value) || value.Type != ToolParameterType.String)
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.ReadFailed,
                $"Argument '{PathParameter}' must be a string."));
        }

        string requested = value.AsString();
        Result<string> resolved = WorkspaceFileAccess.ResolveReadableFile(_root, requested);
        if (!resolved.IsSuccess)
        {
            return ToolResult.Failure(resolved.Error!);
        }

        Result<string> read = WorkspaceFileAccess.Read(resolved.Value, requested);
        return read.IsSuccess
            ? ToolResult.Success(ArgumentValue.FromString(read.Value))
            : ToolResult.Failure(read.Error!);
    }
}
