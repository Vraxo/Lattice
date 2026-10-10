using System.Text;

namespace Lattice.Core;

public sealed class FileSearchTool : ITool
{
    public const int MaxMatches = 100;
    public const int MaxScannedFileBytes = 262_144;
    public const int MaxLineLength = 200;
    private const string QueryParameter = "query";
    private const string PathParameter = "path";
    private static readonly string[] DefaultExclusions =
        [".git", ".vs", "artifacts", "bin", "obj"];

    private readonly WorkspaceRoot _root;
    private readonly HashSet<string> _exclusions;

    public FileSearchTool(WorkspaceRoot root, IEnumerable<string>? excludedDirectories = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        _root = root;
        _exclusions = new HashSet<string>(
            excludedDirectories ?? DefaultExclusions,
            StringComparer.OrdinalIgnoreCase);
    }

    public static ToolId Id { get; } = new("files.search");

    public ToolDescriptor Descriptor { get; } = new(
        Id,
        "Searches file contents inside the workspace, case-insensitively.",
        "Newline-separated matches as 'path:line: text'.",
        ToolSideEffect.ReadOnly,
        [
            new ToolParameter(QueryParameter, ToolParameterType.String),
            new ToolParameter(PathParameter, ToolParameterType.String, required: false),
        ]);

    public ToolResult Execute(ArgumentBag arguments, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (!arguments.TryGetValue(QueryParameter, out ArgumentValue? queryValue))
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.QueryEmpty,
                "A search query is required."));
        }

        if (queryValue.Type != ToolParameterType.String)
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.QueryInvalid,
                $"Argument '{QueryParameter}' must be a string."));
        }

        string query = queryValue.AsString();
        if (string.IsNullOrWhiteSpace(query))
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.QueryEmpty,
                "The search query must not be empty."));
        }

        string requested = ".";
        if (arguments.TryGetValue(PathParameter, out ArgumentValue? pathValue))
        {
            if (pathValue.Type != ToolParameterType.String)
            {
                return ToolResult.Failure(new Error(
                    FileToolErrorCodes.PathNotFound,
                    $"Argument '{PathParameter}' must be a string."));
            }

            requested = pathValue.AsString();
        }

        if (!_root.TryResolve(requested, out string? startPath))
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.PathOutsideRoot,
                $"Path '{requested}' is not inside the workspace root."));
        }

        if (!Directory.Exists(startPath))
        {
            string code = File.Exists(startPath) ? FileToolErrorCodes.NotADirectory : FileToolErrorCodes.PathNotFound;
            return ToolResult.Failure(new Error(code, $"Directory '{requested}' was not found."));
        }

        List<SearchMatch> matches = [];
        bool truncated = false;
        foreach (string file in EnumerateSearchableFiles(startPath))
        {
            if (matches.Count >= MaxMatches)
            {
                truncated = true;
                break;
            }

            CollectMatches(file, query, matches, ref truncated);
            if (truncated)
            {
                break;
            }
        }

        return ToolResult.Success(ArgumentValue.FromString(Format(matches, truncated)));
    }

    private IEnumerable<string> EnumerateSearchableFiles(string startPath)
    {
        EnumerationOptions options = new()
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.None,
        };
        foreach (string file in Directory.EnumerateFiles(startPath, "*", options))
        {
            if (!IsExcluded(file))
            {
                yield return file;
            }
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

    private void CollectMatches(string file, string query, List<SearchMatch> matches, ref bool truncated)
    {
        string text;
        try
        {
            FileInfo info = new(file);
            if (info.Length > MaxScannedFileBytes)
            {
                // Too large to search; skipped rather than failing the whole search.
                return;
            }

            text = File.ReadAllText(
                file,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));
        }
        catch (DecoderFallbackException)
        {
            return;
        }
        catch (IOException)
        {
            return;
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }

        string relative = Normalize(Path.GetRelativePath(_root.FullPath, file));
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (matches.Count >= MaxMatches)
            {
                truncated = true;
                return;
            }

            string line = lines[i].TrimEnd('\r');
            if (line.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(new SearchMatch(relative, i + 1, Truncate(line)));
            }
        }
    }

    private static string Truncate(string line)
    {
        return line.Length <= MaxLineLength ? line : line[..MaxLineLength] + "...";
    }

    private static string Normalize(string relativePath)
    {
        return relativePath.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
    }

    private static string Format(List<SearchMatch> matches, bool truncated)
    {
        StringBuilder builder = new();
        for (int i = 0; i < matches.Count; i++)
        {
            if (i > 0)
            {
                builder.Append('\n');
            }

            builder.Append(matches[i]);
        }

        if (truncated)
        {
            builder.Append('\n').Append($"... more than {MaxMatches} matches; results truncated");
        }

        return builder.ToString();
    }
}