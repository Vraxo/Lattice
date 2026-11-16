namespace Lattice.Core.Tests;

public sealed class WorkspaceRootTests
{
    [Fact]
    public void RejectsMissingDirectory()
    {
        string missing = Path.Combine(Path.GetTempPath(), "lattice-missing-" + Guid.NewGuid().ToString("N"));
        FluentActions.Invoking(() => new WorkspaceRoot(missing)).Should().ThrowExactly<DirectoryNotFoundException>();
    }

    [Fact]
    public void RejectsEmptyPath()
    {
        FluentActions.Invoking(() => new WorkspaceRoot("   ")).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void ResolvesRelativePathInsideRoot()
    {
        using TempWorkspace workspace = new();
        WorkspaceRoot root = new(workspace.Path);
        root.TryResolve("sub/file.txt", out string? full).Should().BeTrue();
        full.Should().StartWith(workspace.Path);
    }

    [Fact]
    public void RejectsAbsolutePath()
    {
        using TempWorkspace workspace = new();
        WorkspaceRoot root = new(workspace.Path);
        root.TryResolve(Path.Combine(workspace.Path, "file.txt"), out _).Should().BeFalse();
    }

    [Fact]
    public void RejectsParentTraversal()
    {
        using TempWorkspace workspace = new();
        WorkspaceRoot root = new(workspace.Path);
        root.TryResolve("../escape.txt", out _).Should().BeFalse();
    }

    [Fact]
    public void RejectsDeepTraversalThatEscapes()
    {
        using TempWorkspace workspace = new();
        WorkspaceRoot root = new(workspace.Path);
        root.TryResolve("sub/../../escape.txt", out _).Should().BeFalse();
    }

    [Fact]
    public void RejectsSiblingDirectoryWithSharedPrefix()
    {
        using TempWorkspace workspace = new();
        WorkspaceRoot root = new(workspace.Path);

        // A naive StartsWith check would accept this because the sibling's name begins with
        // the root's name.
        root.TryResolve("../" + Path.GetFileName(workspace.Path) + "-evil/file.txt", out _).Should().BeFalse();
    }

    [Fact]
    public void AllowsTraversalThatStaysInsideRoot()
    {
        using TempWorkspace workspace = new();
        workspace.CreateDirectory("sub");
        WorkspaceRoot root = new(workspace.Path);
        root.TryResolve("sub/../file.txt", out _).Should().BeTrue();
    }

    [Fact]
    public void RejectsEmptyRelativePath()
    {
        using TempWorkspace workspace = new();
        WorkspaceRoot root = new(workspace.Path);
        root.TryResolve("   ", out _).Should().BeFalse();
    }
}