namespace Lattice.Core;

public interface ITool
{
    ToolDescriptor Descriptor { get; }

    /// <summary>
    /// Executes the tool. Implementations must observe <paramref name="cancellationToken"/>
    /// during long-running work so caller cancellation and descriptor timeouts can stop execution
    /// cooperatively. <see cref="ToolExecutor"/> reports a timeout only after the tool returns or
    /// observes cancellation; it does not abandon a still-running tool on another thread.
    /// </summary>
    /// <returns></returns>
    ToolResult Execute(ArgumentBag arguments, CancellationToken cancellationToken = default);
}
