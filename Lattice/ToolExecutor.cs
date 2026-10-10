namespace Lattice.Core;

public static class ToolExecutor
{
    public static ToolInvocation Execute(
        ITool tool,
        ArgumentBag arguments,
        ToolPermissionPolicy policy,
        CancellationToken cancellationToken = default)
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

        if (cancellationToken.IsCancellationRequested)
        {
            return new ToolInvocation(
                tool.Descriptor.Id,
                arguments,
                ToolResult.Failure(new Error(
                    ToolErrorCodes.Cancelled,
                    $"Tool '{tool.Descriptor.Id.Value}' was cancelled before execution.")));
        }

        ToolResult result = tool.Execute(arguments, cancellationToken);
        return new ToolInvocation(tool.Descriptor.Id, arguments, result);
    }
}