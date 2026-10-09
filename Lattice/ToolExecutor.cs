namespace Lattice.Core;

public static class ToolExecutor
{
    public static ToolInvocation Execute(ITool tool, ArgumentBag arguments, ToolPermissionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(policy);
        if (!policy.IsAllowed(tool.Descriptor.SideEffect))
        {
            return new ToolInvocation(
                tool.Descriptor.Id,
                arguments,
                ToolResult.Failure(new Error(
                    "tool.permission.denied",
                    $"Tool '{tool.Descriptor.Id.Value}' requires {tool.Descriptor.SideEffect}, which is not permitted.")));
        }

        Result validation = ToolArgumentValidator.Validate(tool.Descriptor, arguments);
        if (!validation.IsSuccess)
        {
            return new ToolInvocation(tool.Descriptor.Id, arguments, ToolResult.Failure(validation.Error!));
        }

        ToolResult result = tool.Execute(arguments);
        return new ToolInvocation(tool.Descriptor.Id, arguments, result);
    }
}