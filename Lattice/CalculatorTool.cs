namespace Lattice.Core;

public sealed class CalculatorTool : ITool
{
    private const string LeftParameter = "left";
    private const string RightParameter = "right";

    public static ToolId Id { get; } = new("calculator");

    public ToolDescriptor Descriptor { get; } = new(
        Id,
        "Adds two integers.",
        "An integer sum.",
        ToolSideEffect.ReadOnly,
        [
            new ToolParameter(LeftParameter, ToolParameterType.Integer),
            new ToolParameter(RightParameter, ToolParameterType.Integer),
        ]);

    public ToolResult Execute(ArgumentBag arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (!arguments.TryGetValue(LeftParameter, out ArgumentValue? left) || left.Type != ToolParameterType.Integer)
        {
            return ToolResult.Failure(new Error("calculator.left", "Missing or invalid 'left' integer."));
        }

        if (!arguments.TryGetValue(RightParameter, out ArgumentValue? right) || right.Type != ToolParameterType.Integer)
        {
            return ToolResult.Failure(new Error("calculator.right", "Missing or invalid 'right' integer."));
        }

        return ToolResult.Success(ArgumentValue.FromInteger(left.AsInteger() + right.AsInteger()));
    }
}