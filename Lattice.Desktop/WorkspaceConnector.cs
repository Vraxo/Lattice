using System.Security;
using Lattice.Core;

namespace Lattice.Desktop;

internal static class WorkspaceConnector
{
    public static WorkspaceConnectionResult Connect(string? rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return WorkspaceConnectionResult.Failed("Enter a local folder path before connecting.");
        }

        string trimmed = rawPath.Trim();
        if (trimmed.Contains('\0', StringComparison.Ordinal))
        {
            return WorkspaceConnectionResult.Failed("The workspace path contains invalid characters.");
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(trimmed);
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or NotSupportedException
                or PathTooLongException
                or SecurityException)
        {
            return WorkspaceConnectionResult.Failed($"Workspace path '{trimmed}' is invalid: {exception.Message}");
        }

        if (File.Exists(fullPath) && !Directory.Exists(fullPath))
        {
            return WorkspaceConnectionResult.Failed(
                $"'{fullPath}' is a file, not a directory. Enter a local folder path.");
        }

        if (!Directory.Exists(fullPath))
        {
            return WorkspaceConnectionResult.Failed($"Workspace directory '{fullPath}' does not exist.");
        }

        try
        {
            return WorkspaceConnectionResult.Succeeded(new WorkspaceRoot(fullPath));
        }
        catch (Exception exception) when (
            exception is DirectoryNotFoundException
                or IOException
                or UnauthorizedAccessException
                or ArgumentException)
        {
            return WorkspaceConnectionResult.Failed(
                $"Could not open workspace '{fullPath}': {exception.Message}");
        }
    }
}
