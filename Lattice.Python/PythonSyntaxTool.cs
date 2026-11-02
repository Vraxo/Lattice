using System.Text;
using System.Text.Json;
using Lattice.Core;

namespace Lattice.Python;

/// <summary>
/// Reports Python syntax diagnostics for source text by invoking a configured Python interpreter's
/// standard-library AST parser. The supplied source is passed as base64 data and is never executed.
/// </summary>
public sealed class PythonSyntaxTool : ITool
{
    public const string SourceParameter = "source";
    public const int MaxSourceUtf8Bytes = 8192;

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private const string ParserScript = """
        import ast
        import base64
        import json
        import sys

        source = base64.b64decode(sys.argv[1], validate=True).decode("utf-8")
        version = ".".join(str(part) for part in sys.version_info[:3])
        try:
            ast.parse(source, filename="<source>", mode="exec")
        except SyntaxError as error:
            response = {
                "valid": False,
                "version": version,
                "line": error.lineno or 1,
                "column": error.offset or 1,
                "message": error.msg,
            }
        else:
            response = {"valid": True, "version": version}
        print(json.dumps(response, ensure_ascii=True, separators=(",", ":")))
        """;

    private readonly string _executable;
    private readonly string _workingDirectory;
    private readonly IProcessRunner _processRunner;
    private readonly TimeSpan _timeout;

    /// <summary>Creates the adapter with an explicitly configured Python executable.</summary>
    /// <param name="executable">Python interpreter executable or absolute path.</param>
    /// <param name="workingDirectory">Existing working directory for the parser process.</param>
    /// <param name="processRunner">Process runner; defaults to the system implementation.</param>
    /// <param name="timeout">Parser timeout; defaults to five seconds.</param>
    public PythonSyntaxTool(
        string executable,
        string? workingDirectory = null,
        IProcessRunner? processRunner = null,
        TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        if (timeout is { } configuredTimeout && configuredTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive.");
        }

        _executable = executable;
        _workingDirectory = Path.GetFullPath(workingDirectory ?? Environment.CurrentDirectory);
        if (!Directory.Exists(_workingDirectory))
        {
            throw new DirectoryNotFoundException($"Working directory '{_workingDirectory}' does not exist.");
        }

        _processRunner = processRunner ?? new SystemProcessRunner();
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
        Descriptor = new ToolDescriptor(
            Id,
            "Checks Python source syntax with the standard-library AST parser; it does not execute the source.",
            "A validity line followed by Python version and, when invalid, syntax diagnostic location and message.",
            ToolSideEffect.CommandExecution,
            [new ToolParameter(SourceParameter, ToolParameterType.String)],
            new ToolExecutionLimits(_timeout),
            aliases: ["python-ast", "python-syntax"],
            tags: ["python", "syntax", "parse", "inspect"]);
    }

    /// <summary>Gets the stable identifier used for discovery and registration.</summary>
    public static ToolId Id { get; } = new("python.syntax");

    /// <summary>Gets the generic capability metadata used for discovery and permission checks.</summary>
    public ToolDescriptor Descriptor { get; }

    /// <summary>Runs syntax-only parsing for a source string.</summary>
    public ToolResult Execute(ArgumentBag arguments, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (cancellationToken.IsCancellationRequested)
        {
            return Failure(PythonErrorCodes.ParserCancelled, "Python syntax inspection was cancelled before parsing.");
        }

        if (!arguments.TryGetValue(SourceParameter, out ArgumentValue? sourceValue)
            || sourceValue.Type != ToolParameterType.String)
        {
            return Failure(PythonErrorCodes.SourceRequired, "Argument 'source' must be a string.");
        }

        string source = sourceValue.AsString();
        if (source.Length > MaxSourceUtf8Bytes)
        {
            return Failure(
                PythonErrorCodes.SourceTooLarge,
                $"Source exceeds the {MaxSourceUtf8Bytes}-byte UTF-8 limit.");
        }

        byte[] sourceBytes;
        try
        {
            sourceBytes = StrictUtf8.GetBytes(source);
        }
        catch (EncoderFallbackException)
        {
            return Failure(PythonErrorCodes.SourceEncodingInvalid, "Source contains invalid Unicode and cannot be encoded as UTF-8.");
        }

        if (sourceBytes.Length > MaxSourceUtf8Bytes)
        {
            return Failure(
                PythonErrorCodes.SourceTooLarge,
                $"Source exceeds the {MaxSourceUtf8Bytes}-byte UTF-8 limit.");
        }

        string encodedSource = Convert.ToBase64String(sourceBytes);
        ProcessOutcome outcome;
        try
        {
            outcome = _processRunner.Run(
                _executable,
                ["-I", "-S", "-c", ParserScript, encodedSource],
                _workingDirectory,
                _timeout,
                cancellationToken);
        }
        catch (Exception exception) when (exception is IOException
            or InvalidOperationException
            or System.ComponentModel.Win32Exception
            or ArgumentException)
        {
            return Failure(
                PythonErrorCodes.InterpreterFailedToStart,
                $"Python interpreter could not be started: {exception.Message}");
        }

        if (outcome.Cancelled)
        {
            return Failure(PythonErrorCodes.ParserCancelled, "Python syntax inspection was cancelled.");
        }

        if (outcome.TimedOut)
        {
            return Failure(PythonErrorCodes.ParserTimedOut, $"Python syntax inspection exceeded {_timeout.TotalSeconds:0.###} seconds.");
        }

        if (outcome.ExitCode != 0)
        {
            return Failure(
                PythonErrorCodes.ParserFailed,
                $"Python AST parser exited with code {outcome.ExitCode}.");
        }

        return FormatResponse(outcome.StandardOutput);
    }

    private static ToolResult FormatResponse(string output)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(output);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("valid", out JsonElement validElement)
                || (validElement.ValueKind != JsonValueKind.True && validElement.ValueKind != JsonValueKind.False)
                || !root.TryGetProperty("version", out JsonElement versionElement)
                || versionElement.ValueKind != JsonValueKind.String)
            {
                return Failure(PythonErrorCodes.InvalidParserResponse, "Python AST parser returned an invalid response.");
            }

            string version = versionElement.GetString()!;
            if (string.IsNullOrWhiteSpace(version)
                || version.Length > 32
                || !version.All(character => char.IsAsciiDigit(character) || character == '.'))
            {
                return Failure(PythonErrorCodes.InvalidParserResponse, "Python AST parser returned an invalid version.");
            }

            bool isValid = validElement.GetBoolean();
            if (isValid)
            {
                return ToolResult.Success(ArgumentValue.FromString($"valid (Python {version})"));
            }

            if (!root.TryGetProperty("line", out JsonElement lineElement)
                || !lineElement.TryGetInt32(out int line)
                || !root.TryGetProperty("column", out JsonElement columnElement)
                || !columnElement.TryGetInt32(out int column)
                || !root.TryGetProperty("message", out JsonElement messageElement)
                || messageElement.ValueKind != JsonValueKind.String)
            {
                return Failure(PythonErrorCodes.InvalidParserResponse, "Python AST parser returned an incomplete syntax diagnostic.");
            }

            string message = messageElement.GetString()!;
            if (line < 1 || column < 1 || message.Length > 1000)
            {
                return Failure(PythonErrorCodes.InvalidParserResponse, "Python AST parser returned an invalid syntax diagnostic.");
            }

            message = string.Concat(message.Select(character => char.IsControl(character) ? ' ' : character));
            return ToolResult.Success(ArgumentValue.FromString(
                $"invalid (Python {version}){Environment.NewLine}SyntaxError at {line}:{column}: {message}"));
        }
        catch (JsonException)
        {
            return Failure(PythonErrorCodes.InvalidParserResponse, "Python AST parser returned malformed JSON.");
        }
    }

    private static ToolResult Failure(string code, string message)
    {
        return ToolResult.Failure(new Error(code, message));
    }
}
