namespace Lattice.Core.Tests;

public sealed class ArgumentValueTests
{
    [Fact]
    public void FromStringReportsStringTypeAndValue()
    {
        ArgumentValue value = ArgumentValue.FromString("hello");
        Assert.Equal(ToolParameterType.String, value.Type);
        Assert.Equal("hello", value.AsString());
    }

    [Fact]
    public void FromIntegerReportsIntegerTypeAndValue()
    {
        ArgumentValue value = ArgumentValue.FromInteger(7);
        Assert.Equal(ToolParameterType.Integer, value.Type);
        Assert.Equal(7, value.AsInteger());
    }

    [Fact]
    public void FromNumberReportsNumberTypeAndValue()
    {
        ArgumentValue value = ArgumentValue.FromNumber(1.5);
        Assert.Equal(ToolParameterType.Number, value.Type);
        Assert.Equal(1.5, value.AsNumber());
    }

    [Fact]
    public void FromBooleanReportsBooleanTypeAndValue()
    {
        ArgumentValue value = ArgumentValue.FromBoolean(true);
        Assert.Equal(ToolParameterType.Boolean, value.Type);
        Assert.True(value.AsBoolean());
    }

    [Fact]
    public void WrongAccessorThrows()
    {
        ArgumentValue value = ArgumentValue.FromInteger(1);
        Assert.Throws<InvalidOperationException>(value.AsString);
    }

    [Fact]
    public void ValuesWithSameTypeAndPayloadAreEqual()
    {
        Assert.Equal(ArgumentValue.FromInteger(3), ArgumentValue.FromInteger(3));
    }

    [Fact]
    public void ValuesWithDifferentPayloadAreNotEqual()
    {
        Assert.NotEqual(ArgumentValue.FromInteger(3), ArgumentValue.FromInteger(4));
    }
}