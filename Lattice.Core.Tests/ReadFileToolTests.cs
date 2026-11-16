namespace Lattice.Core.Tests;

public sealed class ReadFileToolTests
{
    [Fact]
    public void ReadsExistingFile()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("a.txt", "hello");
        ReadFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "a.txt");
        result.IsSuccess.Should().BeTrue();
        result.Output!.AsString().Should().Be("hello");
    }

    [Fact]
    public void MissingFileFails()
    {
        using TempWorkspace workspace = new();
        ReadFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "nope.txt");
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(FileToolErrorCodes.PathNotFound);
    }

    [Fact]
    public void DirectoryFails()
    {
        using TempWorkspace workspace = new();
        workspace.CreateDirectory("sub");
        ReadFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "sub");
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(FileToolErrorCodes.NotAFile);
    }

    [Fact]
    public void TraversalIsRejected()
    {
        using TempWorkspace workspace = new();
        ReadFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "../secrets.txt");
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(FileToolErrorCodes.PathOutsideRoot);
    }

    [Fact]
    public void OversizedFileFails()
    {
        using TempWorkspace workspace = new();
        workspace.WriteBytes("big.txt", new byte[ReadFileTool.MaxBytes + 1]);
        ReadFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "big.txt");
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(FileToolErrorCodes.FileTooLarge);
    }

    [Fact]
    public void InvalidUtf8Fails()
    {
        using TempWorkspace workspace = new();

        // Invalid UTF-8 that is not a byte-order mark. (0xFF 0xFE would be read as a
        // UTF-16 LE BOM, and the reader would switch encodings instead of failing.)
        workspace.WriteBytes("bad.txt", [0xC3, 0x28]);
        ReadFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = Execute(tool, "bad.txt");
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(FileToolErrorCodes.EncodingInvalid);
    }

    [Fact]
    public void MissingPathArgumentFails()
    {
        using TempWorkspace workspace = new();
        ReadFileTool tool = new(new WorkspaceRoot(workspace.Path));
        ToolResult result = tool.Execute(ArgumentBag.Empty);
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(FileToolErrorCodes.ReadFailed);
    }

    private static ToolResult Execute(ReadFileTool tool, string path)
    {
        ArgumentBag arguments = ArgumentBag.From([new ArgumentEntry("path", ArgumentValue.FromString(path))]);
        return tool.Execute(arguments);
    }
}