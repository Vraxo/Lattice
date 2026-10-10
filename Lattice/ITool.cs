namespace Lattice.Core;

public interface ITool
{
    ToolDescriptor Descriptor { get; }

    /// <summary>
    /// Executes the tool. <paramref name="cancellationToken"/> is optional for callers that have
    /// no cancellation source, but implementations that can observe it should.
    /// </summary>
    /// <returns></returns>
    ToolResult Execute(ArgumentBag arguments, CancellationToken cancellationToken = default);
}