using System.Text;

namespace Lattice.Core;

public sealed class ListDirectoryTool : ITool
{
    public const int MaxEntries = 1000;
    private const string PathParameter = "path";
    private readonly WorkspaceRoot _root;

    public ListDirectoryTool(WorkspaceRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        _root = root;
    }

    public static ToolId Id { get; } = new("files.list");

    public ToolDescriptor Descriptor { get; } = new(
        Id,
        "Lists entries in a directory inside the workspace.",
        "A newline-separated listing, with a trailing slash on directories.",
        ToolSideEffect.ReadOnly,
        [new ToolParameter(PathParameter, ToolParameterType.String, required: false)]);

    public ToolResult Execute(ArgumentBag arguments, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        cancellationToken.ThrowIfCancellationRequested();
        string requested = ".";
        if (arguments.TryGetValue(PathParameter, out ArgumentValue? value))
        {
            if (value.Type != ToolParameterType.String)
            {
                return ToolResult.Failure(new Error(
                    FileToolErrorCodes.ReadFailed,
                    $"Argument '{PathParameter}' must be a string."));
            }

            requested = value.AsString();
        }

        Result<string> resolved = WorkspaceFileAccess.ResolveReadableDirectory(_root, requested);
        if (!resolved.IsSuccess)
        {
            return ToolResult.Failure(resolved.Error!);
        }

        List<string> entries = [];
        foreach ((string entryPath, bool isDirectory) in WorkspaceFileAccess.EnumerateDirectoryEntriesSafely(resolved.Value))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string name = Path.GetFileName(entryPath);
            entries.Add(isDirectory ? name + "/" : name);
        }

        entries.Sort(StringComparer.Ordinal);
        StringBuilder builder = new();
        int shown = Math.Min(entries.Count, MaxEntries);
        for (int i = 0; i < shown; i++)
        {
            if (i > 0)
            {
                builder.Append('\n');
            }

            builder.Append(entries[i]);
        }

        if (entries.Count > MaxEntries)
        {
            builder.Append('\n').Append($"... {entries.Count - MaxEntries} more entries not shown");
        }

        return ToolResult.Success(ArgumentValue.FromString(builder.ToString()));
    }
}
