using Lattice.Core;

namespace Lattice.Desktop;

internal sealed class ConnectedWorkspace
{
    public ConnectedWorkspace(WorkspaceRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        Root = root;
        ReadFileTool = new ReadFileTool(root);
        FileSearchTool = new FileSearchTool(root);
        ListDirectoryTool = new ListDirectoryTool(root);
        Registry = new ToolRegistry([FileSearchTool, ReadFileTool, ListDirectoryTool]);
        Policy = ToolPermissionPolicy.ReadOnlyOnly;
        Toolset = WorkspaceToolset.FileTools;
        QuestionAnswerer = new WorkspaceQuestionAnswerer(Registry, Policy, Toolset);
    }

    public WorkspaceRoot Root { get; }

    public ReadFileTool ReadFileTool { get; }

    public FileSearchTool FileSearchTool { get; }

    public ListDirectoryTool ListDirectoryTool { get; }

    public ToolRegistry Registry { get; }

    public ToolPermissionPolicy Policy { get; }

    public WorkspaceToolset Toolset { get; }

    public WorkspaceQuestionAnswerer QuestionAnswerer { get; }
}
