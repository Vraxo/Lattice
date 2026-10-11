namespace Lattice.Core;

/// <summary>
/// Replaces the content of a Markdown section identified by its heading. The heading line itself is
/// preserved; only the lines the heading governs are replaced. Because the tool has
/// <see cref="ToolSideEffect.LocalWrite"/>, <see cref="ToolExecutor"/> refuses to invoke it without
/// a granting policy.
/// </summary>
/// <remarks>
/// A caller must supply the hash of the section content it inspected. The tool recomputes that hash
/// against the file as it exists now and refuses the edit when they differ, so an edit prepared
/// against a stale view is never applied to changed content. The check is scoped to the target
/// section, so unrelated changes elsewhere in the file do not invalidate an otherwise valid edit.
/// </remarks>
public sealed class MarkdownEditTool : ITool
{
    private const string PathParameter = "path";
    private const string HeadingParameter = "heading";
    private const string LevelParameter = "level";
    private const string ReplacementParameter = "replacement";
    private const string ExpectedHashParameter = "expectedHash";
    private readonly WorkspaceRoot _root;

    public MarkdownEditTool(WorkspaceRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        _root = root;
    }

    public static ToolId Id { get; } = new("markdown.edit");

    public ToolDescriptor Descriptor { get; } = new(
        Id,
        "Replaces the content of a Markdown section, preserving its heading.",
        "A unified diff of the applied change.",
        ToolSideEffect.LocalWrite,
        [
            new ToolParameter(PathParameter, ToolParameterType.String),
            new ToolParameter(HeadingParameter, ToolParameterType.String),
            new ToolParameter(LevelParameter, ToolParameterType.Integer, required: false),
            new ToolParameter(ReplacementParameter, ToolParameterType.String),
            new ToolParameter(ExpectedHashParameter, ToolParameterType.String),
        ],
        tags: ["markdown", "edit", "prose", "document"]);

    public ToolResult Execute(ArgumentBag arguments, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (!ToolArguments.TryReadString(arguments, PathParameter, out string? requested)
            || !ToolArguments.TryReadString(arguments, HeadingParameter, out string? heading)
            || !ToolArguments.TryReadString(arguments, ReplacementParameter, out string? replacement)
            || !ToolArguments.TryReadString(arguments, ExpectedHashParameter, out string? expectedHash))
        {
            return ToolResult.Failure(new Error(
                FileToolErrorCodes.WriteFailed,
                "Arguments 'path', 'heading', 'replacement', and 'expectedHash' must all be strings."));
        }

        if (expectedHash.Length == 0)
        {
            return ToolResult.Failure(new Error(
                DocumentErrorCodes.ExpectedHashMissing,
                "Argument 'expectedHash' must not be empty; a stale edit cannot be detected without it."));
        }

        if (!TryReadLevel(arguments, out int? level, out Error? levelError))
        {
            return ToolResult.Failure(levelError!);
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
        MarkdownDocument document = MarkdownDocument.Parse(original);
        Result<MarkdownSection> section = document.FindSection(heading, level);
        if (!section.IsSuccess)
        {
            return ToolResult.Failure(section.Error!);
        }

        Result<string> currentContent = document.Text.GetText(section.Value.ContentRange);
        if (!currentContent.IsSuccess)
        {
            return ToolResult.Failure(currentContent.Error!);
        }

        if (!string.Equals(ContentHash.Of(currentContent.Value), expectedHash, StringComparison.Ordinal))
        {
            return ToolResult.Failure(new Error(
                DocumentErrorCodes.StaleContent,
                $"Section '{heading}' has changed since it was inspected; the edit was not applied."));
        }

        Result<TextDocument> edited = TextEditor.Replace(document.Text, section.Value.ContentRange, replacement);
        if (!edited.IsSuccess)
        {
            return ToolResult.Failure(edited.Error!);
        }

        string updated = edited.Value.ToText();
        string diff = UnifiedDiff.Create(original, updated);
        Result write = WorkspaceFileAccess.Write(resolved.Value, requested, original, updated);
        if (!write.IsSuccess)
        {
            return ToolResult.Failure(write.Error!);
        }

        return ToolResult.Success(ArgumentValue.FromString(diff));
    }

    private static bool TryReadLevel(ArgumentBag arguments, out int? level, out Error? error)
    {
        level = null;
        error = null;
        if (!ToolArguments.TryReadOptionalInteger(arguments, LevelParameter, out long? value))
        {
            error = new Error(
                DocumentErrorCodes.HeadingLevelInvalid,
                $"Argument '{LevelParameter}' must be an integer between 1 and 6.");
            return false;
        }

        if (value is null)
        {
            return true;
        }

        if (value is < 1 or > 6)
        {
            error = new Error(
                DocumentErrorCodes.HeadingLevelInvalid,
                $"Argument '{LevelParameter}' must be between 1 and 6.");
            return false;
        }

        level = (int)value.Value;
        return true;
    }
}