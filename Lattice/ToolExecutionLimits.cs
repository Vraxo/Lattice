namespace Lattice.Core;

public sealed record ToolExecutionLimits
{
    public static ToolExecutionLimits Default { get; } = new(TimeSpan.FromSeconds(30));

    public ToolExecutionLimits(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive.");
        }

        Timeout = timeout;
    }

    public TimeSpan Timeout { get; }
}