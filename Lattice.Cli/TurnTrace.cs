using System.Text;
using Lattice.Core;

namespace Lattice.Cli;

/// <summary>Renders concise, human-readable diagnostics for one turn.</summary>
internal static class TurnTrace
{
    public static string DescribeStatement(IControlledStatement statement)
    {
        return statement switch
        {
            FactStatement fact => $"fact: {fact.Subject} | {fact.Predicate} | {fact.Value}",
            GoalStatement goal => $"goal: {goal.Description} | {goal.Completion}",
            ConstraintStatement constraint => $"constraint: {constraint.Description}",
            ActionStatement action => DescribeAction(action),
            _ => $"statement: {statement.GetType().Name}",
        };
    }

    public static string DescribeInvocation(ToolInvocation invocation)
    {
        StringBuilder builder = new();
        builder.Append("tool: ").Append(invocation.ToolId.Value);
        if (!invocation.Arguments.Entries.IsDefaultOrEmpty)
        {
            builder.Append(" (");
            builder.Append(string.Join(
                ", ",
                invocation.Arguments.Entries.Select(entry => $"{entry.Name}={Format(entry.Value)}")));
            builder.Append(')');
        }

        builder.Append(" -> ");
        builder.Append(invocation.Result.IsSuccess
            ? Format(invocation.Result.Output!)
            : $"{invocation.Result.Error!.Code}: {invocation.Result.Error.Message}");
        return builder.ToString();
    }

    private static string DescribeAction(ActionStatement action)
    {
        if (action.Arguments.IsDefaultOrEmpty)
        {
            return $"action: {action.ToolId}";
        }

        string arguments = string.Join(", ", action.Arguments.Select(a => $"{a.Name}={a.Value}"));
        return $"action: {action.ToolId} ({arguments})";
    }

    private static string Format(ArgumentValue value)
    {
        return value.Type switch
        {
            ToolParameterType.String => $"\"{value.AsString()}\"",
            ToolParameterType.Integer => value.AsInteger().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ToolParameterType.Number => value.AsNumber().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ToolParameterType.Boolean => value.AsBoolean() ? "true" : "false",
            _ => value.Type.ToString(),
        };
    }
}