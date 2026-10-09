namespace Lattice.Core.Tests;
internal sealed class WriteRecordingTool : ITool
{
    public bool WasExecuted { get; private set; }
    public ToolDescriptor Descriptor { get; } = new(
        new ToolId("writer"),
        "Records whether it was executed.",
        "A boolean.",
        ToolSideEffect.LocalWrite);
    public ToolResult Execute(ArgumentBag arguments)
    {
        WasExecuted = true;
        return ToolResult.Success(ArgumentValue.FromBoolean(true));
    }
}