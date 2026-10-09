using System.Globalization;
namespace Lattice.Core;
/// <summary>
/// Runs a single bounded turn: interprets one controlled statement, acts on it, records the
/// result in the session, and produces a response. Multi-statement planning is out of scope.
/// </summary>
public sealed class AgentLoop
{
    private readonly ToolRegistry _registry;
    private readonly ToolPermissionPolicy _policy;
    public AgentLoop(ToolRegistry registry, ToolPermissionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(policy);
        _registry = registry;
        _policy = policy;
    }
    public AgentTurnResult Run(Session session, IControlledStatement statement)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(statement);
        return statement switch
        {
            ActionStatement action => RunAction(session, action),
            FactStatement fact => RunFact(session, fact),
            GoalStatement goal => RunGoal(session, goal),
            ConstraintStatement => new AgentTurnResult(
                session,
                TurnOutcome.ConstraintNotSupported,
                "Constraints are not yet attached to a goal; no change was made."),
            _ => throw new InvalidOperationException(
                $"Unhandled statement type '{statement.GetType().Name}'."),
        };
    }
    private AgentTurnResult RunAction(Session session, ActionStatement action)
    {
        var toolId = new ToolId(action.ToolId);
        var arguments = ToArgumentBag(action.Arguments);
        if (!_registry.TryResolve(toolId, out var tool))
        {
            return new AgentTurnResult(
                session,
                TurnOutcome.ToolUnavailable,
                $"Tool '{action.ToolId}' is not available.");
        }
        if (IsDuplicate(session, toolId, arguments))
        {
            return new AgentTurnResult(
                session,
                TurnOutcome.DuplicateAction,
                $"The action for '{action.ToolId}' with the same arguments was already attempted.");
        }
        var invocation = ToolExecutor.Execute(tool, arguments, _policy);
        var updated = session.AddToolInvocation(invocation);
        return new AgentTurnResult(updated, Classify(invocation), Describe(action.ToolId, invocation), invocation);
    }
    private static AgentTurnResult RunFact(Session session, FactStatement fact)
    {
        var updated = session.AddUserAssertion($"{fact.Subject} {fact.Predicate} {fact.Value}", "controlled");
        return new AgentTurnResult(updated, TurnOutcome.FactRecorded, "Recorded the fact.");
    }
    private static AgentTurnResult RunGoal(Session session, GoalStatement goal)
    {
        var created = new Goal(GoalId.New(), goal.Description, goal.Completion, GoalStatus.Incomplete);
        var updated = session.AddGoal(created);
        return new AgentTurnResult(updated, TurnOutcome.GoalRecorded, "Recorded the goal.");
    }
    private static TurnOutcome Classify(ToolInvocation invocation)
    {
        if (invocation.Result.IsSuccess)
        {
            return TurnOutcome.ToolSucceeded;
        }
        return invocation.Result.Error!.Code switch
        {
            "tool.permission.denied" => TurnOutcome.PermissionDenied,
            "tool.argument.unknown" or "tool.argument.type" or "tool.argument.missing" => TurnOutcome.InvalidArguments,
            _ => TurnOutcome.ToolFailed,
        };
    }
    private static string Describe(string toolId, ToolInvocation invocation)
    {
        if (invocation.Result.IsSuccess)
        {
            return $"Tool '{toolId}' succeeded with result {invocation.Result.Output!.Type}.";
        }
        return invocation.Result.Error!.Code switch
        {
            "tool.permission.denied" => $"Tool '{toolId}' was denied by policy.",
            "tool.argument.unknown" or "tool.argument.type" or "tool.argument.missing" =>
                $"Tool '{toolId}' rejected its arguments.",
            _ => $"Tool '{toolId}' failed: {invocation.Result.Error.Message}",
        };
    }
    private static bool IsDuplicate(Session session, ToolId toolId, ArgumentBag arguments)
    {
        foreach (var existing in session.ToolInvocations)
        {
            if (existing.ToolId == toolId && existing.Arguments == arguments)
            {
                return true;
            }
        }
        return false;
    }
    private static ArgumentBag ToArgumentBag(IEnumerable<ActionArgument> arguments)
    {
        var entries = arguments.Select(ToEntry);
        return ArgumentBag.From(entries);
    }
    private static ArgumentEntry ToEntry(ActionArgument argument) => new(
        argument.Name,
        argument.Type switch
        {
            ToolParameterType.String => ArgumentValue.FromString(argument.Value),
            ToolParameterType.Integer => ArgumentValue.FromInteger(
                long.Parse(argument.Value, CultureInfo.InvariantCulture)),
            ToolParameterType.Number => ArgumentValue.FromNumber(
                double.Parse(argument.Value, CultureInfo.InvariantCulture)),
            ToolParameterType.Boolean => ArgumentValue.FromBoolean(bool.Parse(argument.Value)),
            _ => throw new InvalidOperationException($"Unhandled argument type '{argument.Type}'."),
        });
}