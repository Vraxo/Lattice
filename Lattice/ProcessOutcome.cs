namespace Lattice.Core;
/// <summary>The observed result of running one process.</summary>
public sealed record ProcessOutcome
{
    public ProcessOutcome(int exitCode, string standardOutput, string standardError, bool timedOut)
    {
        ArgumentNullException.ThrowIfNull(standardOutput);
        ArgumentNullException.ThrowIfNull(standardError);
        ExitCode = exitCode;
        StandardOutput = standardOutput;
        StandardError = standardError;
        TimedOut = timedOut;
    }
    public int ExitCode { get; }
    public string StandardOutput { get; }
    public string StandardError { get; }
    public bool TimedOut { get; }
}