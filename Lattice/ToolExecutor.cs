namespace Lattice.Core;
public static class ToolExecutor
{
    public static ToolInvocation Execute(ITool tool, ArgumentBag arguments)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(arguments);
        var validation = ToolArgumentValidator.Validate(tool.Descriptor, arguments);
        if (!validation.IsSuccess)
        {
            return new ToolInvocation(tool.Descriptor.Id, arguments, ToolResult.Failure(validation.Error!));
        }
        var result = tool.Execute(arguments);
        return new ToolInvocation(tool.Descriptor.Id, arguments, result);
    }
}