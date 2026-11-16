namespace Lattice.Core.Tests;

public sealed class ArgumentValueTests
{
    [Fact]
    public void FromStringReportsStringTypeAndValue()
    {
        ArgumentValue value = ArgumentValue.FromString("hello");
        value.Type.Should().Be(ToolParameterType.String);
        value.AsString().Should().Be("hello");
    }

    [Fact]
    public void FromIntegerReportsIntegerTypeAndValue()
    {
        ArgumentValue value = ArgumentValue.FromInteger(7);
        value.Type.Should().Be(ToolParameterType.Integer);
        value.AsInteger().Should().Be(7);
    }

    [Fact]
    public void FromNumberReportsNumberTypeAndValue()
    {
        ArgumentValue value = ArgumentValue.FromNumber(1.5);
        value.Type.Should().Be(ToolParameterType.Number);
        value.AsNumber().Should().Be(1.5);
    }

    [Fact]
    public void FromBooleanReportsBooleanTypeAndValue()
    {
        ArgumentValue value = ArgumentValue.FromBoolean(true);
        value.Type.Should().Be(ToolParameterType.Boolean);
        value.AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void WrongAccessorThrows()
    {
        ArgumentValue value = ArgumentValue.FromInteger(1);
        FluentActions.Invoking(() => value.AsString()).Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void ValuesWithSameTypeAndPayloadAreEqual()
    {
        ArgumentValue.FromInteger(3).Should().Be(ArgumentValue.FromInteger(3));
    }

    [Fact]
    public void ValuesWithDifferentPayloadAreNotEqual()
    {
        ArgumentValue.FromInteger(4).Should().NotBe(ArgumentValue.FromInteger(3));
    }
}