namespace Lattice.Core;

/// <summary>
/// Tracks the maximum number of turns allowed in one logical agent run. Each call to
/// <see cref="AgentLoop.Run(Session, AgentRunContext, IControlledStatement, CancellationToken)"/>
/// or its capability-request overload consumes one step, including blocked and failed turns.
/// Create a fresh context for each run and pass the same instance to each turn; the counter is
/// atomic so the limit also holds when callers race turns against the same context.
/// </summary>
public sealed class AgentRunContext
{
    private int _stepsUsed;

    public AgentRunContext(int maxSteps)
    {
        if (maxSteps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSteps), "Maximum steps must be positive.");
        }

        MaxSteps = maxSteps;
    }

    public int MaxSteps { get; }

    public int StepsUsed => Volatile.Read(ref _stepsUsed);

    public int RemainingSteps => Math.Max(0, MaxSteps - StepsUsed);

    internal bool TryConsumeStep()
    {
        while (true)
        {
            int current = Volatile.Read(ref _stepsUsed);
            if (current >= MaxSteps)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref _stepsUsed, current + 1, current) == current)
            {
                return true;
            }
        }
    }
}
