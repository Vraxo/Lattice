using Lattice.Core;
using Lattice.CSharp;

namespace Lattice.Desktop;

internal sealed class ConnectedWorkspace
{
    public ConnectedWorkspace(WorkspaceRoot root, WorkspaceFileLocator? fileLocator = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        Root = root;
        FileLocator = fileLocator ?? new WorkspaceFileLocator(root);
        ReadFileTool = new ReadFileTool(root);
        FileSearchTool = new FileSearchTool(root);
        ListDirectoryTool = new ListDirectoryTool(root);
        Registry = new ToolRegistry([FileSearchTool, ReadFileTool, ListDirectoryTool]);
        Policy = ToolPermissionPolicy.ReadOnlyOnly;
        Toolset = WorkspaceToolset.FileTools;
        QuestionAnswerer = new WorkspaceQuestionAnswerer(Registry, Policy, Toolset);
        CSharpStructureInspector = new CSharpWorkspaceStructureInspector(root);
    }

    public WorkspaceRoot Root { get; }

    public WorkspaceFileLocator FileLocator { get; }

    public ReadFileTool ReadFileTool { get; }

    public FileSearchTool FileSearchTool { get; }

    public ListDirectoryTool ListDirectoryTool { get; }

    public ToolRegistry Registry { get; }

    public ToolPermissionPolicy Policy { get; }

    public WorkspaceToolset Toolset { get; }

    public WorkspaceQuestionAnswerer QuestionAnswerer { get; }

    public CSharpWorkspaceStructureInspector CSharpStructureInspector { get; }
}
