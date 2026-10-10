namespace Lattice.Core.Tests;

internal sealed class ThrowingProcessRunner : IProcessRunner
{
    public ProcessOutcome Run(
        string executable,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Simulated start failure.");
    }
}