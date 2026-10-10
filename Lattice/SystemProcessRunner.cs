using System.Diagnostics;
namespace Lattice.Core;
/// <summary>
/// Runs a process via <see cref="Process"/> with captured output, a hard timeout, and cooperative
/// cancellation. On timeout or cancellation the entire process tree is terminated, so a hung child
/// does not outlive the call.
/// </summary>
public sealed class SystemProcessRunner : IProcessRunner
{
    public ProcessOutcome Run(
        string executable,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
        if (cancellationToken.IsCancellationRequested)
        {
            return new ProcessOutcome(-1, string.Empty, string.Empty, timedOut: false, cancelled: true);
        }
        ProcessStartInfo startInfo = new()
        {
            FileName = executable,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }
        using Process process = new() { StartInfo = startInfo };
        process.Start();
        // The cancellation token is deliberately not passed to the reads. Reading buffered output
        // is not the cancellable operation; the process is. Killing the process closes the pipes,
        // which completes these reads normally. Passing the token here makes the reads throw
        // TaskCanceledException and turns a clean cancellation into an AggregateException.
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        bool cancelled = false;
        using CancellationTokenRegistration registration = cancellationToken.Register(() =>
        {
            cancelled = true;
            TryKill(process);
        });
        if (cancellationToken.IsCancellationRequested)
        {
            cancelled = true;
            TryKill(process);
        }
        bool exited = process.WaitForExit((int)timeout.TotalMilliseconds);
        if (!exited)
        {
            TryKill(process);
            process.WaitForExit();
        }
        string output = CollectOutput(stdout);
        string error = CollectOutput(stderr);
        int exitCode = exited ? process.ExitCode : -1;
        // A cancellation that arrives just as the process exits is still a cancellation.
        cancelled |= cancellationToken.IsCancellationRequested;
        return new ProcessOutcome(
            exitCode,
            output,
            error,
            timedOut: !exited && !cancelled,
            cancelled: cancelled);
    }
    /// <summary>
    /// Collects a stream read result without letting a faulted or cancelled read escalate. A
    /// killed process can fault its output reads, and that must not become the caller's exception.
    /// </summary>
    private static string CollectOutput(Task<string> read)
    {
        try
        {
            return read.GetAwaiter().GetResult();
        }
        catch (Exception exception) when (exception is IOException
            or ObjectDisposedException
            or OperationCanceledException
            or AggregateException)
        {
            return string.Empty;
        }
    }
    /// <summary>
    /// Terminates the process tree, tolerating the several ways this can fail when the process
    /// has already exited. A failed kill must not escalate into an exception for the caller.
    /// </summary>
    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException
            or NotSupportedException
            or System.ComponentModel.Win32Exception
            or AggregateException)
        {
            // The process was already gone, or the platform refused the kill. Either way there is
            // nothing further to do; the outcome records the timeout/cancellation.
        }
    }
}