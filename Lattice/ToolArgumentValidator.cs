namespace Lattice.Core;
public static class ToolArgumentValidator
{
    public static Result Validate(ToolDescriptor descriptor, ArgumentBag arguments)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(arguments);
        foreach (var entry in arguments.Entries)
        {
            if (!TryFindParameter(descriptor, entry.Name, out var parameter))
            {
                return Result.Failure(
                    new Error("tool.argument.unknown", $"Unknown argument '{entry.Name}'."));
            }
            if (parameter.Type != entry.Value.Type)
            {
                return Result.Failure(
                    new Error(
                        "tool.argument.type",
                        $"Argument '{entry.Name}' expects {parameter.Type} but received {entry.Value.Type}."));
            }
        }
        foreach (var parameter in descriptor.Parameters)
        {
            if (parameter.Required && !arguments.TryGetValue(parameter.Name, out _))
            {
                return Result.Failure(
                    new Error("tool.argument.missing", $"Missing required argument '{parameter.Name}'."));
            }
        }
        return Result.Success();
    }
    private static bool TryFindParameter(ToolDescriptor descriptor, string name, out ToolParameter parameter)
    {
        foreach (var candidate in descriptor.Parameters)
        {
            if (string.Equals(candidate.Name, name, StringComparison.Ordinal))
            {
                parameter = candidate;
                return true;
            }
        }
        parameter = null!;
        return false;
    }
}