namespace Lattice.Core;
/// <summary>
/// The tool identifiers a <see cref="WorkspaceQuestionAnswerer"/> uses. Injected rather than
/// referenced directly so the answerer does not dispatch on specific tool names.
/// </summary>
public sealed record WorkspaceToolset(ToolId Search, ToolId Read, ToolId List)
{
    public static WorkspaceToolset FileTools { get; } =
        new(FileSearchTool.Id, ReadFileTool.Id, ListDirectoryTool.Id);
}