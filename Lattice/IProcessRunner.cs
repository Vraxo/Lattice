namespace Lattice.Core;

/// <summary>
/// Runs a preconfigured executable and captures its output. Abstracted so the guarded command
/// tool can be tested deterministically for timeout, cancellation, and nonzero exit.
/// </summary>
public interface IProcessRunner
{
    ProcessOutcome Run(
        string executable,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}