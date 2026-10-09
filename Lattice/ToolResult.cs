namespace Lattice.Core;

public sealed record ToolResult
{
    private ToolResult(ArgumentValue? output, Error? error)
    {
        Output = output;
        Error = error;
    }

    public ArgumentValue? Output { get; }

    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public static ToolResult Success(ArgumentValue output)
    {
        ArgumentNullException.ThrowIfNull(output);
        return new(output, null);
    }

    public static ToolResult Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(null, error);
    }
}