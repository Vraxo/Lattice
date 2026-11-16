using System.Text;

namespace Lattice.Core.Tests;

/// <summary>
/// Tests the shared write guard directly, independent of any tool. The guard is the single place
/// where encoding preservation is enforced, so its own behaviour is pinned here. Both
/// <see cref="WriteFileTool"/> and <see cref="MarkdownEditTool"/> inherit this behaviour; their own
/// tests demonstrate that inheritance.
/// </summary>
public sealed class WorkspaceFileAccessEncodingTests : IDisposable
{
    private readonly TempWorkspace _workspace = new();

    public void Dispose()
    {
        _workspace.Dispose();
    }

    [Fact]
    public void WriteSucceedsForUtf8WithoutBom()
    {
        string path = _workspace.WriteFile("a.txt", "old\n");
        Result result = WorkspaceFileAccess.Write(path, "a.txt", "old\n", "new\n");
        result.IsSuccess.Should().BeTrue();
        File.ReadAllText(path).Should().Be("new\n");
    }

    [Fact]
    public void WriteRefusesUtf8WithBomBeforeTouchingAnything()
    {
        byte[] before = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("old\n")];
        string path = WriteBytes("a.txt", before);
        Result result = WorkspaceFileAccess.Write(path, "a.txt", "old\n", "new\n");
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(FileToolErrorCodes.EncodingPreservationUnsupported);
        before.SequenceEqual(File.ReadAllBytes(path)).Should().BeTrue();
        File.Exists(path + WorkspaceFileAccess.BackupSuffix).Should().BeFalse();
    }

    [Fact]
    public void WriteRefusesInvalidUtf8WithTheEncodingInvalidError()
    {
        string path = WriteBytes("a.txt", [0xC3, 0x28]);
        Result result = WorkspaceFileAccess.Write(path, "a.txt", "ignored", "ignored");
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(FileToolErrorCodes.EncodingInvalid);
    }

    [Fact]
    public void WriteCreatesABackupForUtf8WithoutBom()
    {
        string path = _workspace.WriteFile("a.txt", "old\n");
        Result result = WorkspaceFileAccess.Write(path, "a.txt", "old\n", "new\n");
        result.IsSuccess.Should().BeTrue();
        File.ReadAllText(path + WorkspaceFileAccess.BackupSuffix).Should().Be("old\n");
    }

    private string WriteBytes(string relative, byte[] bytes)
    {
        string full = Path.Combine(_workspace.Path, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, bytes);
        return full;
    }
}