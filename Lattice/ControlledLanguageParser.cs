using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Lattice.Core;

public static partial class ControlledLanguageParser
{
    public static Result<IControlledStatement> Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Fail(ControlledErrorKind.Empty, "Statement is empty.");
        }

        SourceSpan span = new(0, input.Length);
        string[] segments = input.Split('|');
        string first = segments[0].Trim();
        int spaceIndex = IndexOfWhitespace(first);
        string keyword = spaceIndex < 0 ? first : first[..spaceIndex];
        string rest = spaceIndex < 0 ? string.Empty : first[(spaceIndex + 1)..].Trim();
        List<string> fields = [rest];
        for (int i = 1; i < segments.Length; i++)
        {
            fields.Add(segments[i].Trim());
        }

        return keyword switch
        {
            "fact" => ParseFact(fields, span),
            "goal" => ParseGoal(fields, span),
            "constraint" => ParseConstraint(fields, span),
            "action" => ParseAction(fields, span),
            _ => Fail(ControlledErrorKind.UnknownKeyword, $"Unknown keyword '{keyword}'."),
        };
    }

    private static Result<IControlledStatement> ParseFact(IReadOnlyList<string> fields, SourceSpan span)
    {
        if (fields.Count < 3)
        {
            return Fail(ControlledErrorKind.MissingField, "A fact requires subject, predicate, and value.");
        }

        if (fields.Count > 3)
        {
            return Fail(ControlledErrorKind.ExtraField, "A fact takes exactly three fields.");
        }

        int empty = FirstEmpty(fields);
        if (empty >= 0)
        {
            return Fail(ControlledErrorKind.EmptyField, $"Fact field {empty + 1} must not be empty.");
        }

        return Result<IControlledStatement>.Success(new FactStatement(fields[0], fields[1], fields[2], span));
    }

    private static Result<IControlledStatement> ParseGoal(IReadOnlyList<string> fields, SourceSpan span)
    {
        if (fields.Count < 2)
        {
            return Fail(ControlledErrorKind.MissingField, "A goal requires a description and a completion condition.");
        }

        if (fields.Count > 2)
        {
            return Fail(ControlledErrorKind.ExtraField, "A goal takes exactly two fields.");
        }

        int empty = FirstEmpty(fields);
        if (empty >= 0)
        {
            return Fail(ControlledErrorKind.EmptyField, $"Goal field {empty + 1} must not be empty.");
        }

        return Result<IControlledStatement>.Success(new GoalStatement(fields[0], fields[1], span));
    }

    private static Result<IControlledStatement> ParseConstraint(IReadOnlyList<string> fields, SourceSpan span)
    {
        if (fields.Count > 1)
        {
            return Fail(ControlledErrorKind.ExtraField, "A constraint takes exactly one field.");
        }

        if (fields[0].Length == 0)
        {
            return Fail(ControlledErrorKind.EmptyField, "Constraint description must not be empty.");
        }

        return Result<IControlledStatement>.Success(new ConstraintStatement(fields[0], span));
    }

    private static Result<IControlledStatement> ParseAction(IReadOnlyList<string> fields, SourceSpan span)
    {
        string toolId = fields[0];
        if (toolId.Length == 0 || !ToolIdPattern().IsMatch(toolId))
        {
            return Fail(ControlledErrorKind.InvalidToolId, $"Tool id '{toolId}' is not valid.");
        }

        ImmutableArray<ActionArgument>.Builder arguments = ImmutableArray.CreateBuilder<ActionArgument>();
        HashSet<string> seen = new(StringComparer.Ordinal);
        for (int i = 1; i < fields.Count; i++)
        {
            string field = fields[i];
            int separator = field.IndexOf('=');
            if (separator < 0)
            {
                return Fail(ControlledErrorKind.MalformedArgument, $"Argument '{field}' is not name=value.");
            }

            string name = field[..separator].Trim();
            string value = field[(separator + 1)..].Trim();
            if (name.Length == 0 || value.Length == 0)
            {
                return Fail(ControlledErrorKind.MalformedArgument, $"Argument '{field}' must have a non-empty name and value.");
            }

            if (!seen.Add(name))
            {
                return Fail(ControlledErrorKind.DuplicateArgument, $"Duplicate argument '{name}'.");
            }

            arguments.Add(new ActionArgument(name, TypeOf(value), value));
        }

        return Result<IControlledStatement>.Success(new ActionStatement(toolId, arguments, span));
    }

    private static ToolParameterType TypeOf(string value)
    {
        if (value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            return ToolParameterType.Boolean;
        }

        if (IsInteger(value))
        {
            return ToolParameterType.Integer;
        }

        if (IsNumber(value))
        {
            return ToolParameterType.Number;
        }

        return ToolParameterType.String;
    }

    private static bool IsInteger(string value)
    {
        int index = value[0] == '-' ? 1 : 0;
        if (index >= value.Length)
        {
            return false;
        }

        for (; index < value.Length; index++)
        {
            if (!char.IsAsciiDigit(value[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsNumber(string value)
    {
        int index = value[0] == '-' ? 1 : 0;
        int digitsBefore = 0;
        while (index < value.Length && char.IsAsciiDigit(value[index]))
        {
            index++;
            digitsBefore++;
        }

        if (digitsBefore == 0 || index >= value.Length || value[index] != '.')
        {
            return false;
        }

        index++;
        int digitsAfter = 0;
        while (index < value.Length && char.IsAsciiDigit(value[index]))
        {
            index++;
            digitsAfter++;
        }

        return digitsAfter > 0 && index == value.Length;
    }

    private static int FirstEmpty(IReadOnlyList<string> fields)
    {
        for (int i = 0; i < fields.Count; i++)
        {
            if (fields[i].Length == 0)
            {
                return i;
            }
        }

        return -1;
    }

    private static int IndexOfWhitespace(string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            if (char.IsWhiteSpace(value[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static Result<IControlledStatement> Fail(string kind, string message)
    {
        return Result<IControlledStatement>.Failure(new Error(kind, message));
    }

    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex ToolIdPattern();
}