namespace Lattice.Core.Tests;
internal sealed class FailingTool : ITool
{
    public ToolDescriptor Descriptor { get; } = new(
        new ToolId("failing"),
        "Always fails.",
        "Never produced.",
        ToolSideEffect.ReadOnly);
    public ToolResult Execute(ArgumentBag arguments) =>
        ToolResult.Failure(new Error("failing.tool", "The tool always fails."));
}