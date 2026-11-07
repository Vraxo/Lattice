namespace Lattice.Core.Tests;

internal sealed class SymlinkFactAttribute : FactAttribute
{
    private static readonly string? SkipReason = ProbeSymlinkSupport();

    public SymlinkFactAttribute()
    {
        if (SkipReason is not null)
        {
            Skip = SkipReason;
        }
    }

    private static string? ProbeSymlinkSupport()
    {
        string probeDirectory = Path.Combine(
            Path.GetTempPath(),
            "lattice-symlink-probe-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(probeDirectory);
            string target = Path.Combine(probeDirectory, "target.txt");
            string link = Path.Combine(probeDirectory, "link.txt");
            File.WriteAllText(target, "probe");
            File.CreateSymbolicLink(link, target);
            return null;
        }
        catch (Exception exception) when (
            exception is IOException
                or UnauthorizedAccessException
                or PlatformNotSupportedException)
        {
            return $"Symbolic link creation is unavailable in this environment: {exception.Message}";
        }
        finally
        {
            try
            {
                if (Directory.Exists(probeDirectory))
                {
                    Directory.Delete(probeDirectory, recursive: true);
                }
            }
            catch (IOException)
            {
                // Best-effort cleanup of the capability probe directory.
            }
        }
    }
}
