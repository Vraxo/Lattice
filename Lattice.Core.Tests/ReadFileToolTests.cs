namespace Lattice.Core.Tests;
public sealed class ReadFileToolTests
{
    [Fact]
    public void ReadsExistingFile()
    {
        using var workspace = new TempWorkspace();
        workspace.WriteFile("a.txt", "hello");
        var tool = new ReadFileTool(new WorkspaceRoot(workspace.Path));
        var result = Execute(tool, "a.txt");
        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Output!.AsString());
    }
    [Fact]
    public void MissingFileFails()
    {
        using var workspace = new TempWorkspace();
        var tool = new ReadFileTool(new WorkspaceRoot(workspace.Path));
        var result = Execute(tool, "nope.txt");
        Assert.False(result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.PathNotFound, result.Error!.Code);
    }
    [Fact]
    public void DirectoryFails()
    {
        using var workspace = new TempWorkspace();
        workspace.CreateDirectory("sub");
        var tool = new ReadFileTool(new WorkspaceRoot(workspace.Path));
        var result = Execute(tool, "sub");
        Assert.False(result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.NotAFile, result.Error!.Code);
    }
    [Fact]
    public void TraversalIsRejected()
    {
        using var workspace = new TempWorkspace();
        var tool = new ReadFileTool(new WorkspaceRoot(workspace.Path));
        var result = Execute(tool, "../secrets.txt");
        Assert.False(result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.PathOutsideRoot, result.Error!.Code);
    }
    [Fact]
    public void OversizedFileFails()
    {
        using var workspace = new TempWorkspace();
        workspace.WriteBytes("big.txt", new byte[ReadFileTool.MaxBytes + 1]);
        var tool = new ReadFileTool(new WorkspaceRoot(workspace.Path));
        var result = Execute(tool, "big.txt");
        Assert.False(result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.FileTooLarge, result.Error!.Code);
    }
    [Fact]
    public void InvalidUtf8Fails()
    {
        using var workspace = new TempWorkspace();
        // Invalid UTF-8 that is not a byte-order mark. (0xFF 0xFE would be read as a
        // UTF-16 LE BOM, and the reader would switch encodings instead of failing.)
        workspace.WriteBytes("bad.txt", new byte[] { 0xC3, 0x28 });
        var tool = new ReadFileTool(new WorkspaceRoot(workspace.Path));
        var result = Execute(tool, "bad.txt");
        Assert.False(result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.EncodingInvalid, result.Error!.Code);
    }
    [Fact]
    public void MissingPathArgumentFails()
    {
        using var workspace = new TempWorkspace();
        var tool = new ReadFileTool(new WorkspaceRoot(workspace.Path));
        var result = tool.Execute(ArgumentBag.Empty);
        Assert.False(result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.ReadFailed, result.Error!.Code);
    }
    private static ToolResult Execute(ReadFileTool tool, string path)
    {
        var arguments = ArgumentBag.From(new[] { new ArgumentEntry("path", ArgumentValue.FromString(path)) });
        return tool.Execute(arguments);
    }
}