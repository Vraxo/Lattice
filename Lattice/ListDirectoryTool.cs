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

        if (!_root.TryResolve(requested, out string? fullPath))
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.PathOutsideRoot,
                $"Path '{requested}' is not inside the workspace root."));
        }

        if (!Directory.Exists(fullPath))
        {
            string code = File.Exists(fullPath) ? FileToolErrorCodes.NotADirectory : FileToolErrorCodes.PathNotFound;
            return ToolResult.Failure(new Error(code, $"Directory '{requested}' was not found."));
        }

        string[] entries = [.. Directory.EnumerateFileSystemEntries(fullPath)
            .Select(Format)
            .OrderBy(entry => entry, StringComparer.Ordinal),];
        StringBuilder builder = new();
        int shown = Math.Min(entries.Length, MaxEntries);
        for (int i = 0; i < shown; i++)
        {
            if (i > 0)
            {
                builder.Append('\n');
            }

            builder.Append(entries[i]);
        }

        if (entries.Length > MaxEntries)
        {
            builder.Append('\n').Append($"... {entries.Length - MaxEntries} more entries not shown");
        }

        return ToolResult.Success(ArgumentValue.FromString(builder.ToString()));
    }

    private static string Format(string entryPath)
    {
        string name = Path.GetFileName(entryPath);
        return Directory.Exists(entryPath) ? name + "/" : name;
    }
}