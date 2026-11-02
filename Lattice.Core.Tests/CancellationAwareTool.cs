namespace Lattice.Core.Tests;

internal sealed class CancellationAwareTool(TimeSpan timeout) : ITool, IDisposable
{
    private readonly ManualResetEventSlim _started = new();

    public ToolDescriptor Descriptor { get; } = new ToolDescriptor(
            new ToolId("cancellation-aware"),
            "Waits for executor cancellation.",
            "A boolean.",
            ToolSideEffect.ReadOnly,
            limits: new ToolExecutionLimits(timeout));

    public bool WasExecuted { get; private set; }

    public bool CancellationObserved { get; private set; }

    public WaitHandle Started => _started.WaitHandle;

    public ToolResult Execute(ArgumentBag arguments, CancellationToken cancellationToken = default)
    {
        WasExecuted = true;
        _started.Set();
        CancellationObserved = cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(2));
        cancellationToken.ThrowIfCancellationRequested();
        return ToolResult.Success(ArgumentValue.FromBoolean(true));
    }

    public void Dispose()
    {
        _started.Dispose();
    }
}
