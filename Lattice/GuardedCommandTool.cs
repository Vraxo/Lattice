using System.Collections.Immutable;
using System.Text;
namespace Lattice.Core;
/// <summary>
/// Runs a preconfigured command and returns its captured output. The tool accepts only a name
/// that was registered at construction; arbitrary executables and arguments cannot be supplied
/// through arguments, and the command carries <see cref="ToolSideEffect.CommandExecution"/>, so
/// <see cref="ToolExecutor"/> refuses to invoke it without a granting policy.
/// </summary>
public sealed class GuardedCommandTool : ITool
{
    public const int MaxOutputChars = 20_000;
    private const string NameParameter = "name";
    private readonly ImmutableDictionary<string, AllowedCommand> _commands;
    private readonly IProcessRunner _runner;
    private readonly WorkspaceRoot _root;
    private readonly TimeSpan _timeout;
    public GuardedCommandTool(
        WorkspaceRoot root,
        IEnumerable<KeyValuePair<string, AllowedCommand>> commands,
        IProcessRunner runner,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(runner);
        _commands = commands.ToImmutableDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.Ordinal);
        _runner = runner;
        _root = root;
        _timeout = timeout ?? ToolExecutionLimits.Default.Timeout;
    }
    public static ToolId Id { get; } = new("command.run");
    public ToolDescriptor Descriptor { get; } = new(
        Id,
        "Runs a preconfigured command inside the workspace and returns its output.",
        "A summary line, then the command's standard output and error.",
        ToolSideEffect.CommandExecution,
        [new ToolParameter(NameParameter, ToolParameterType.String)]);
    public ToolResult Execute(ArgumentBag arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (!arguments.TryGetValue(NameParameter, out ArgumentValue? nameValue)
            || nameValue.Type != ToolParameterType.String)
        {
            return ToolResult.Failure(new Error(
                CommandErrorCodes.CommandNameMissing,
                $"Argument '{NameParameter}' must be a string."));
        }
        string name = nameValue.AsString();
        if (!_commands.TryGetValue(name, out AllowedCommand? command))
        {
            return ToolResult.Failure(new Error(
                CommandErrorCodes.CommandNotAllowed,
                $"Command '{name}' is not in the allowlist."));
        }
        ProcessOutcome outcome;
        try
        {
            outcome = _runner.Run(command.Executable, command.Arguments, _root.FullPath, _timeout);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return ToolResult.Failure(new Error(
                CommandErrorCodes.CommandFailedToStart,
                $"Command '{name}' could not be started: {exception.Message}"));
        }
        if (outcome.TimedOut)
        {
            return ToolResult.Failure(new Error(
                CommandErrorCodes.CommandTimedOut,
                $"Command '{name}' exceeded the {_timeout.TotalSeconds:0.#}s timeout."));
        }
        return ToolResult.Success(ArgumentValue.FromString(Format(name, outcome)));
    }
    private static string Format(string name, ProcessOutcome outcome)
    {
        StringBuilder builder = new();
        builder.Append("command '").Append(name).Append("' exited with code ").Append(outcome.ExitCode);
        AppendSection(builder, "stdout", outcome.StandardOutput);
        AppendSection(builder, "stderr", outcome.StandardError);
        return builder.ToString();
    }
    private static void AppendSection(StringBuilder builder, string label, string content)
    {
        if (content.Length == 0)
        {
            return;
        }
        builder.Append('\n').Append(label).Append(":\n").Append(Truncate(content));
    }
    private static string Truncate(string content) =>
        content.Length <= MaxOutputChars ? content : content[..MaxOutputChars] + "\n... output truncated";
}