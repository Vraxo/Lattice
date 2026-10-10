namespace Lattice.Core.Tests;

public sealed class WorkspaceRootTests
{
    [Fact]
    public void RejectsMissingDirectory()
    {
        string missing = Path.Combine(Path.GetTempPath(), "lattice-missing-" + Guid.NewGuid().ToString("N"));
        Assert.Throws<DirectoryNotFoundException>(() => new WorkspaceRoot(missing));
    }

    [Fact]
    public void RejectsEmptyPath()
    {
        Assert.Throws<ArgumentException>(() => new WorkspaceRoot("   "));
    }

    [Fact]
    public void ResolvesRelativePathInsideRoot()
    {
        using TempWorkspace workspace = new();
        WorkspaceRoot root = new(workspace.Path);
        Assert.True(root.TryResolve("sub/file.txt", out string? full));
        Assert.StartsWith(workspace.Path, full, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsAbsolutePath()
    {
        using TempWorkspace workspace = new();
        WorkspaceRoot root = new(workspace.Path);
        Assert.False(root.TryResolve(Path.Combine(workspace.Path, "file.txt"), out _));
    }

    [Fact]
    public void RejectsParentTraversal()
    {
        using TempWorkspace workspace = new();
        WorkspaceRoot root = new(workspace.Path);
        Assert.False(root.TryResolve("../escape.txt", out _));
    }

    [Fact]
    public void RejectsDeepTraversalThatEscapes()
    {
        using TempWorkspace workspace = new();
        WorkspaceRoot root = new(workspace.Path);
        Assert.False(root.TryResolve("sub/../../escape.txt", out _));
    }

    [Fact]
    public void RejectsSiblingDirectoryWithSharedPrefix()
    {
        using TempWorkspace workspace = new();
        WorkspaceRoot root = new(workspace.Path);

        // A naive StartsWith check would accept this because the sibling's name begins with
        // the root's name.
        Assert.False(root.TryResolve("../" + Path.GetFileName(workspace.Path) + "-evil/file.txt", out _));
    }

    [Fact]
    public void AllowsTraversalThatStaysInsideRoot()
    {
        using TempWorkspace workspace = new();
        workspace.CreateDirectory("sub");
        WorkspaceRoot root = new(workspace.Path);
        Assert.True(root.TryResolve("sub/../file.txt", out _));
    }

    [Fact]
    public void RejectsEmptyRelativePath()
    {
        using TempWorkspace workspace = new();
        WorkspaceRoot root = new(workspace.Path);
        Assert.False(root.TryResolve("   ", out _));
    }
}