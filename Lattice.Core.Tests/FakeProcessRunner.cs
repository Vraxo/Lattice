namespace Lattice.Core.Tests;
/// <summary>
/// A deterministic process runner for tests. It records the invocation and returns a canned
/// outcome, so timeout and nonzero-exit paths can be exercised without spawning a process.
/// </summary>
internal sealed class FakeProcessRunner : IProcessRunner
{
    private readonly ProcessOutcome _outcome;
    public FakeProcessRunner(ProcessOutcome outcome)
    {
        _outcome = outcome;
    }
    public string? LastExecutable { get; private set; }
    public IReadOnlyList<string> LastArguments { get; private set; } = [];
    public TimeSpan? LastTimeout { get; private set; }
    public string? LastWorkingDirectory { get; private set; }
    public ProcessOutcome Run(
        string executable,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        TimeSpan timeout)
    {
        LastExecutable = executable;
        LastArguments = arguments;
        LastTimeout = timeout;
        LastWorkingDirectory = workingDirectory;
        return _outcome;
    }
}