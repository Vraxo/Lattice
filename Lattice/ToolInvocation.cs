namespace Lattice.Core;

public sealed record ToolInvocation
{
    public ToolInvocation(ToolId toolId, ArgumentBag arguments, ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(result);
        ToolId = toolId;
        Arguments = arguments;
        Result = result;
    }

    public ToolId ToolId { get; }

    public ArgumentBag Arguments { get; }

    public ToolResult Result { get; }
}