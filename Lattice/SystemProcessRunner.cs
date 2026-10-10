using System.Diagnostics;
namespace Lattice.Core;
/// <summary>
/// Runs a process via <see cref="Process"/> with captured output and a hard timeout. On timeout
/// the entire process tree is terminated, so a hung child does not outlive the call.
/// </summary>
public sealed class SystemProcessRunner : IProcessRunner
{
    public ProcessOutcome Run(
        string executable,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        TimeSpan timeout)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
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
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        bool exited = process.WaitForExit((int)timeout.TotalMilliseconds);
        if (!exited)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // The process exited between the timeout check and the kill.
            }
            process.WaitForExit();
        }
        Task.WaitAll(stdout, stderr);
        int exitCode = exited ? process.ExitCode : -1;
        return new ProcessOutcome(exitCode, stdout.Result, stderr.Result, !exited);
    }
}