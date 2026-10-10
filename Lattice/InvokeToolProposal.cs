namespace Lattice.Core;

public sealed record InvokeToolProposal : ActionProposal
{
    public InvokeToolProposal(ToolId toolId, ArgumentBag arguments)
    {
        if (string.IsNullOrWhiteSpace(toolId.Value))
        {
            throw new ArgumentException("Tool id must not be empty.", nameof(toolId));
        }

        ArgumentNullException.ThrowIfNull(arguments);
        ToolId = toolId;
        Arguments = arguments;
    }

    public ToolId ToolId { get; }

    public ArgumentBag Arguments { get; }
}