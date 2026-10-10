namespace Lattice.Core;

public static class CommandErrorCodes
{
    public const string CommandNotAllowed = "command.not-allowed";
    public const string CommandNameMissing = "command.name-missing";
    public const string CommandTimedOut = "command.timed-out";
    public const string CommandFailedToStart = "command.failed-to-start";
    public const string OutputTruncated = "command.output-truncated";
    public const string CommandCancelled = "command.cancelled";
}