namespace Lattice.Core;

public sealed record ToolExecutionLimits
{
    public static ToolExecutionLimits Default { get; } = new(TimeSpan.FromSeconds(30));

    public ToolExecutionLimits(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero || timeout.TotalMilliseconds > uint.MaxValue - 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeout),
                "Timeout must be positive and within the cancellation timer range.");
        }

        Timeout = timeout;
    }

    public TimeSpan Timeout { get; }
}
