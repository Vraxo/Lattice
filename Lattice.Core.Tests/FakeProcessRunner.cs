namespace Lattice.Core.Tests;

/// <summary>
/// A deterministic process runner for tests. It records the invocation and returns a canned
/// outcome, so timeout, cancellation, and nonzero-exit paths can be exercised without spawning
/// a process.
/// </summary>
internal sealed class FakeProcessRunner(ProcessOutcome outcome, bool observeCancellation = false) : IProcessRunner
{
    private readonly ProcessOutcome _outcome = outcome;
    private readonly bool _observeCancellation = observeCancellation;

    public string? LastExecutable { get; private set; }

    public IReadOnlyList<string> LastArguments { get; private set; } = [];

    public TimeSpan? LastTimeout { get; private set; }

    public string? LastWorkingDirectory { get; private set; }

    public bool ObservedCancellation { get; private set; }

    public ProcessOutcome Run(
        string executable,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        LastExecutable = executable;
        LastArguments = arguments;
        LastTimeout = timeout;
        LastWorkingDirectory = workingDirectory;
        if (_observeCancellation && cancellationToken.IsCancellationRequested)
        {
            ObservedCancellation = true;
            return new ProcessOutcome(-1, string.Empty, string.Empty, timedOut: false, cancelled: true);
        }

        return _outcome;
    }
}