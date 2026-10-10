namespace Lattice.Core.Tests;

/// <summary>A disposable temporary directory used as a workspace root in tests.</summary>
internal sealed class TempWorkspace : IDisposable
{
    public TempWorkspace()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "lattice-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string WriteFile(string relativePath, string contents)
    {
        string full = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        File.WriteAllText(full, contents);
        return full;
    }

    public void WriteBytes(string relativePath, byte[] bytes)
    {
        string full = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, bytes);
    }

    public void CreateDirectory(string relativePath)
    {
        Directory.CreateDirectory(System.IO.Path.Combine(Path, relativePath));
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // Best effort; a leaked temp directory must not fail a test run.
        }
    }
}