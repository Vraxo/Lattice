using System.Diagnostics;

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
            return Failure(
                tool,
                arguments,
                "tool.permission.denied",
                $"Tool '{tool.Descriptor.Id.Value}' requires {tool.Descriptor.SideEffect}, which is not permitted.");
        }

        Result validation = ToolArgumentValidator.Validate(tool.Descriptor, arguments);
        if (!validation.IsSuccess)
        {
            return new ToolInvocation(tool.Descriptor.Id, arguments, ToolResult.Failure(validation.Error!));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Failure(
                tool,
                arguments,
                ToolErrorCodes.Cancelled,
                $"Tool '{tool.Descriptor.Id.Value}' was cancelled before execution.");
        }

        TimeSpan timeout = tool.Descriptor.Limits.Timeout;
        using CancellationTokenSource timeoutSource = new(timeout);
        using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);
        if (cancellationToken.IsCancellationRequested)
        {
            return Failure(
                tool,
                arguments,
                ToolErrorCodes.Cancelled,
                $"Tool '{tool.Descriptor.Id.Value}' was cancelled before execution.");
        }

        if (timeoutSource.IsCancellationRequested)
        {
            return TimedOut(tool, arguments);
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        ToolResult result;
        try
        {
            result = tool.Execute(arguments, linkedSource.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Failure(
                tool,
                arguments,
                ToolErrorCodes.Cancelled,
                $"Tool '{tool.Descriptor.Id.Value}' was cancelled.");
        }
        catch (OperationCanceledException) when (
            timeoutSource.IsCancellationRequested || stopwatch.Elapsed >= timeout)
        {
            return TimedOut(tool, arguments);
        }

        // A cooperative tool may return a normal result just as the deadline expires. The
        // measured elapsed time supplements the timer signal, preventing a late result from being
        // reported as success when timer scheduling lags behind the deadline.
        if (cancellationToken.IsCancellationRequested)
        {
            return Failure(
                tool,
                arguments,
                ToolErrorCodes.Cancelled,
                $"Tool '{tool.Descriptor.Id.Value}' was cancelled.");
        }

        if (timeoutSource.IsCancellationRequested || stopwatch.Elapsed >= timeout)
        {
            return TimedOut(tool, arguments);
        }

        return new ToolInvocation(tool.Descriptor.Id, arguments, result);
    }

    private static ToolInvocation TimedOut(ITool tool, ArgumentBag arguments)
    {
        return Failure(
            tool,
            arguments,
            ToolErrorCodes.TimedOut,
            $"Tool '{tool.Descriptor.Id.Value}' exceeded its {tool.Descriptor.Limits.Timeout.TotalSeconds:0.###}s timeout.");
    }

    private static ToolInvocation Failure(ITool tool, ArgumentBag arguments, string code, string message)
    {
        return new ToolInvocation(
            tool.Descriptor.Id,
            arguments,
            ToolResult.Failure(new Error(code, message)));
    }
}
