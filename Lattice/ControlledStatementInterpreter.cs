using System.Globalization;
namespace Lattice.Core;
/// <summary>
/// Translates an incoming controlled statement into an <see cref="InterpretedStatement"/>.
/// This is interpretation only: it applies state changes and produces typed action candidates.
/// It never decides what to execute — selection and execution belong to the controller.
/// </summary>
public static class ControlledStatementInterpreter
{
    public static InterpretedStatement Interpret(Session session, IControlledStatement statement)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(statement);
        return statement switch
        {
            ActionStatement action => InterpretAction(session, action),
            FactStatement fact => InterpretFact(session, fact),
            GoalStatement goal => InterpretGoal(session, goal),
            ConstraintStatement => InterpretedStatement.StateChange(
                session,
                TurnOutcome.ConstraintNotSupported,
                "Constraints are not yet attached to a goal; no change was made."),
            _ => throw new InvalidOperationException(
                $"Unhandled statement type '{statement.GetType().Name}'."),
        };
    }
    private static InterpretedStatement InterpretAction(Session session, ActionStatement action)
    {
        var proposal = new InvokeToolProposal(new ToolId(action.ToolId), ToArgumentBag(action.Arguments));
        return InterpretedStatement.Actionable(session, proposal);
    }
    private static InterpretedStatement InterpretFact(Session session, FactStatement fact)
    {
        var updated = session.AddUserAssertion($"{fact.Subject} {fact.Predicate} {fact.Value}", "controlled");
        return InterpretedStatement.StateChange(updated, TurnOutcome.FactRecorded, "Recorded the fact.");
    }
    private static InterpretedStatement InterpretGoal(Session session, GoalStatement goal)
    {
        var created = new Goal(GoalId.New(), goal.Description, goal.Completion, GoalStatus.Incomplete);
        var updated = session.AddGoal(created);
        return InterpretedStatement.StateChange(updated, TurnOutcome.GoalRecorded, "Recorded the goal.");
    }
    private static ArgumentBag ToArgumentBag(IEnumerable<ActionArgument> arguments) =>
        ArgumentBag.From(arguments.Select(ToEntry));
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