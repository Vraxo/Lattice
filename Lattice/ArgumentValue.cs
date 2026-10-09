namespace Lattice.Core;

public sealed record ArgumentValue
{
    private readonly string? _stringValue;
    private readonly long _integerValue;
    private readonly double _numberValue;
    private readonly bool _booleanValue;

    private ArgumentValue(
        ToolParameterType type,
        string? stringValue,
        long integerValue,
        double numberValue,
        bool booleanValue)
    {
        Type = type;
        _stringValue = stringValue;
        _integerValue = integerValue;
        _numberValue = numberValue;
        _booleanValue = booleanValue;
    }

    public ToolParameterType Type { get; }

    public static ArgumentValue FromString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(ToolParameterType.String, value, 0, 0, false);
    }

    public static ArgumentValue FromInteger(long value)
    {
        return new(ToolParameterType.Integer, null, value, 0, false);
    }

    public static ArgumentValue FromNumber(double value)
    {
        return new(ToolParameterType.Number, null, 0, value, false);
    }

    public static ArgumentValue FromBoolean(bool value)
    {
        return new(ToolParameterType.Boolean, null, 0, 0, value);
    }

    public string AsString()
    {
        return Type == ToolParameterType.String
            ? _stringValue!
            : throw new InvalidOperationException($"Argument value is {Type}, not {ToolParameterType.String}.");
    }

    public long AsInteger()
    {
        return Type == ToolParameterType.Integer
            ? _integerValue
            : throw new InvalidOperationException($"Argument value is {Type}, not {ToolParameterType.Integer}.");
    }

    public double AsNumber()
    {
        return Type == ToolParameterType.Number
            ? _numberValue
            : throw new InvalidOperationException($"Argument value is {Type}, not {ToolParameterType.Number}.");
    }

    public bool AsBoolean()
    {
        return Type == ToolParameterType.Boolean
            ? _booleanValue
            : throw new InvalidOperationException($"Argument value is {Type}, not {ToolParameterType.Boolean}.");
    }
}