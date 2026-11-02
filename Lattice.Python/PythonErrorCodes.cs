namespace Lattice.Python;

/// <summary>Stable error codes returned by the Python syntax adapter.</summary>
public static class PythonErrorCodes
{
    public const string SourceRequired = "python.source-required";
    public const string SourceTooLarge = "python.source-too-large";
    public const string SourceEncodingInvalid = "python.source-encoding-invalid";
    public const string InterpreterFailedToStart = "python.interpreter-failed-to-start";
    public const string ParserFailed = "python.parser-failed";
    public const string ParserTimedOut = "python.parser-timed-out";
    public const string ParserCancelled = "python.parser-cancelled";
    public const string InvalidParserResponse = "python.invalid-parser-response";
}
