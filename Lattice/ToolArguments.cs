namespace Lattice.Core;
/// <summary>Small shared helpers for reading typed tool arguments.</summary>
internal static class ToolArguments
{
    public static bool TryReadString(ArgumentBag arguments, string name, out string value)
    {
        value = string.Empty;
        if (!arguments.TryGetValue(name, out ArgumentValue? argument)
            || argument.Type != ToolParameterType.String)
        {
            return false;
        }
        value = argument.AsString();
        return true;
    }
    /// <summary>Reads an optional string argument, yielding <see langword="null"/> when absent.</summary>
    public static bool TryReadOptionalString(ArgumentBag arguments, string name, out string? value)
    {
        value = null;
        if (!arguments.TryGetValue(name, out ArgumentValue? argument))
        {
            return true;
        }
        if (argument.Type != ToolParameterType.String)
        {
            return false;
        }
        value = argument.AsString();
        return true;
    }
    /// <summary>Reads an optional integer argument, yielding <see langword="null"/> when absent.</summary>
    public static bool TryReadOptionalInteger(ArgumentBag arguments, string name, out long? value)
    {
        value = null;
        if (!arguments.TryGetValue(name, out ArgumentValue? argument))
        {
            return true;
        }
        if (argument.Type != ToolParameterType.Integer)
        {
            return false;
        }
        value = argument.AsInteger();
        return true;
    }
}