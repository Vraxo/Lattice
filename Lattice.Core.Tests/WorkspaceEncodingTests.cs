using System.Text;

namespace Lattice.Core.Tests;

/// <summary>
/// Pins the encoding guarantees at the file-editing boundary, exercised through the public tools a
/// caller actually uses. Reads are permissive: a file with a byte-order mark can still be inspected.
/// Edits are guarded: an edit is refused unless the original bytes are UTF-8 with no byte-order
/// mark, so an ordinary edit can never silently change a file's encoding or mark. A refused edit
/// leaves the original byte-for-byte unchanged and neither creates nor replaces a backup.
/// </summary>
public sealed class WorkspaceEncodingTests : IDisposable
{
    private readonly TempWorkspace _workspace = new();

    public void Dispose()
    {
        _workspace.Dispose();
    }

    // --- Reads are permissive, so a marked file can still be inspected. ---
    [Fact]
    public void Utf8FileWithoutBomReadsUnchanged()
    {
        _workspace.WriteFile("a.md", "plain\n");
        ToolInvocation invocation = Read("a.md");
        Assert.True(invocation.Result.IsSuccess);
        Assert.Equal("plain\n", invocation.Result.Output!.AsString());
    }

    [Fact]
    public void Utf8BomIsConsumedOnRead()
    {
        WriteBytes("a.md", [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("plain\n")]);
        ToolInvocation invocation = Read("a.md");
        Assert.True(invocation.Result.IsSuccess);
        Assert.Equal("plain\n", invocation.Result.Output!.AsString());
        Assert.DoesNotContain('\uFEFF', invocation.Result.Output.AsString());
    }

    [Fact]
    public void Utf16LeWithBomDecodesSuccessfully()
    {
        // Reading a marked file into a string is safe and useful: the engine can inspect a file it
        // will later refuse to edit. Rejection is a property of the write path, not the read path.
        WriteBytes("a.md", [0xFF, 0xFE, .. Encoding.Unicode.GetBytes("wide\n")]);
        ToolInvocation invocation = Read("a.md");
        Assert.True(invocation.Result.IsSuccess);
        Assert.Equal("wide\n", invocation.Result.Output!.AsString());
    }

    [Fact]
    public void GenuinelyInvalidUtf8IsRejected()
    {
        WriteBytes("a.md", [0xC3, 0x28]);
        ToolInvocation invocation = Read("a.md");
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.EncodingInvalid, invocation.Result.Error!.Code);
    }

    // --- Edits are guarded: only BOM-free UTF-8 is written back. ---
    [Fact]
    public void Utf8WithoutBomIsEditedAndTheResultHasNoBom()
    {
        _workspace.WriteFile("a.md", "old\n");
        ToolInvocation invocation = Patch("a.md", "old", "new");
        Assert.True(invocation.Result.IsSuccess);
        byte[] bytes = File.ReadAllBytes(PathOf("a.md"));
        Assert.False(HasUtf8Bom(bytes));
        Assert.Equal("new\n", Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void Utf8BomIsRejectedOnEditAndFileIsUnchanged()
    {
        byte[] before = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("old\n")];
        WriteBytes("a.md", before);
        ToolInvocation invocation = Patch("a.md", "old", "new");
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.EncodingPreservationUnsupported, invocation.Result.Error!.Code);
        AssertUnchanged("a.md", before);
    }

    [Fact]
    public void Utf16LeIsRejectedOnEditAndFileIsUnchanged()
    {
        byte[] before = [0xFF, 0xFE, .. Encoding.Unicode.GetBytes("old\n")];
        WriteBytes("a.md", before);
        ToolInvocation invocation = Patch("a.md", "old", "new");
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.EncodingPreservationUnsupported, invocation.Result.Error!.Code);
        AssertUnchanged("a.md", before);
    }

    [Fact]
    public void Utf16BeIsRejectedOnEditAndFileIsUnchanged()
    {
        byte[] before = [0xFE, 0xFF, .. Encoding.BigEndianUnicode.GetBytes("old\n")];
        WriteBytes("a.md", before);
        ToolInvocation invocation = Patch("a.md", "old", "new");
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.EncodingPreservationUnsupported, invocation.Result.Error!.Code);
        AssertUnchanged("a.md", before);
    }

    [Fact]
    public void Utf32LeIsRejectedOnEditAndFileIsUnchanged()
    {
        byte[] before = [0xFF, 0xFE, 0x00, 0x00, .. Encoding.UTF32.GetBytes("old\n")];
        WriteBytes("a.md", before);
        ToolInvocation invocation = Patch("a.md", "old", "new");
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.EncodingPreservationUnsupported, invocation.Result.Error!.Code);
        AssertUnchanged("a.md", before);
    }

    [Fact]
    public void Utf32BeIsRejectedOnEditAndFileIsUnchanged()
    {
        UTF32Encoding utf32Be = new(bigEndian: true, byteOrderMark: false);
        byte[] before = [0x00, 0x00, 0xFE, 0xFF, .. utf32Be.GetBytes("old\n")];
        WriteBytes("a.md", before);
        ToolInvocation invocation = Patch("a.md", "old", "new");
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.EncodingPreservationUnsupported, invocation.Result.Error!.Code);
        AssertUnchanged("a.md", before);
    }

    [Fact]
    public void ARejectedEditCreatesNoBackup()
    {
        WriteBytes("a.md", [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("old\n")]);
        ToolInvocation invocation = Patch("a.md", "old", "new");
        Assert.False(invocation.Result.IsSuccess);
        Assert.False(File.Exists(PathOf("a.md" + WriteFileTool.BackupSuffix)));
    }

    [Fact]
    public void ARejectedEditDoesNotReplaceAnExistingBackup()
    {
        WriteBytes("a.md", [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("old\n")]);
        _workspace.WriteFile("a.md" + WriteFileTool.BackupSuffix, "prior backup");
        ToolInvocation invocation = Patch("a.md", "old", "new");
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal("prior backup", File.ReadAllText(PathOf("a.md" + WriteFileTool.BackupSuffix)));
    }

    // --- The structured document tool inherits the same guard through the shared helper. ---
    [Fact]
    public void MarkdownEditRejectsAUtf8BomFileAndLeavesItUnchanged()
    {
        byte[] before = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("# A\nold\n")];
        WriteBytes("a.md", before);
        ToolInvocation invocation = MarkdownEdit("a.md", "A", "new\n", ContentHash.Of("old\n"));
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.EncodingPreservationUnsupported, invocation.Result.Error!.Code);
        Assert.NotEqual(DocumentErrorCodes.StaleContent, invocation.Result.Error.Code);
        AssertUnchanged("a.md", before);
    }

    [Fact]
    public void MarkdownEditRejectsAUtf16FileAndLeavesItUnchanged()
    {
        byte[] before = [0xFF, 0xFE, .. Encoding.Unicode.GetBytes("# A\nold\n")];
        WriteBytes("a.md", before);
        ToolInvocation invocation = MarkdownEdit("a.md", "A", "new\n", ContentHash.Of("old\n"));
        Assert.False(invocation.Result.IsSuccess);
        Assert.Equal(FileToolErrorCodes.EncodingPreservationUnsupported, invocation.Result.Error!.Code);
        AssertUnchanged("a.md", before);
    }

    private string PathOf(string relative)
    {
        return Path.Combine(_workspace.Path, relative);
    }

    private void WriteBytes(string relative, byte[] bytes)
    {
        string full = PathOf(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, bytes);
    }

    private void AssertUnchanged(string relative, byte[] before)
    {
        byte[] after = File.ReadAllBytes(PathOf(relative));
        Assert.True(before.SequenceEqual(after), "The file's bytes changed even though the edit was refused.");
    }

    private ToolInvocation Read(string relative)
    {
        ReadFileTool tool = new(new WorkspaceRoot(_workspace.Path));
        return ToolExecutor.Execute(
            tool,
            ArgumentBag.From([new ArgumentEntry("path", ArgumentValue.FromString(relative))]),
            ToolPermissionPolicy.ReadOnlyOnly);
    }

    private ToolInvocation Patch(string relative, string find, string replace)
    {
        WriteFileTool tool = new(new WorkspaceRoot(_workspace.Path));
        return ToolExecutor.Execute(
            tool,
            ArgumentBag.From(
            [
                new ArgumentEntry("path", ArgumentValue.FromString(relative)),
                new ArgumentEntry("find", ArgumentValue.FromString(find)),
                new ArgumentEntry("replace", ArgumentValue.FromString(replace)),
            ]),
            ToolPermissionPolicy.AllowAll);
    }

    private ToolInvocation MarkdownEdit(string relative, string heading, string replacement, string expectedHash)
    {
        MarkdownEditTool tool = new(new WorkspaceRoot(_workspace.Path));
        return ToolExecutor.Execute(
            tool,
            ArgumentBag.From(
            [
                new ArgumentEntry("path", ArgumentValue.FromString(relative)),
                new ArgumentEntry("heading", ArgumentValue.FromString(heading)),
                new ArgumentEntry("replacement", ArgumentValue.FromString(replacement)),
                new ArgumentEntry("expectedHash", ArgumentValue.FromString(expectedHash)),
            ]),
            ToolPermissionPolicy.AllowAll);
    }

    private static bool HasUtf8Bom(byte[] bytes)
    {
        return bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
    }
}