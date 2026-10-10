namespace Lattice.Core.Tests;

internal sealed class RecordingTool : ITool
{
    public bool WasExecuted { get; private set; }

    public ToolDescriptor Descriptor { get; } = new(
        new ToolId("recording"),
        "Records whether it was executed.",
        "A boolean.",
        ToolSideEffect.ReadOnly,
        [new ToolParameter("value", ToolParameterType.Integer)]);

    public ToolResult Execute(ArgumentBag arguments, CancellationToken cancellationToken = default)
    {
        WasExecuted = true;
        return ToolResult.Success(ArgumentValue.FromBoolean(true));
    }
}