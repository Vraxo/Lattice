namespace Lattice.Core.Tests;

public sealed class ToolArgumentValidatorTests
{
    private static readonly ToolDescriptor Descriptor = new(
        new ToolId("calculator"),
        "Adds two integers.",
        "An integer sum.",
        ToolSideEffect.ReadOnly,
        [
            new ToolParameter("left", ToolParameterType.Integer),
            new ToolParameter("right", ToolParameterType.Integer),
            new ToolParameter("label", ToolParameterType.String, required: false),
        ]);

    [Fact]
    public void ValidArgumentsPass()
    {
        Result result = ToolArgumentValidator.Validate(Descriptor, TwoIntegers());
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void UnknownArgumentFails()
    {
        ArgumentBag arguments = ArgumentBag.From(
        [
            new ArgumentEntry("left", ArgumentValue.FromInteger(1)),
            new ArgumentEntry("right", ArgumentValue.FromInteger(2)),
            new ArgumentEntry("extra", ArgumentValue.FromInteger(3)),
        ]);
        Result result = ToolArgumentValidator.Validate(Descriptor, arguments);
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("tool.argument.unknown");
    }

    [Fact]
    public void TypeMismatchFails()
    {
        ArgumentBag arguments = ArgumentBag.From(
        [
            new ArgumentEntry("left", ArgumentValue.FromString("one")),
            new ArgumentEntry("right", ArgumentValue.FromInteger(2)),
        ]);
        Result result = ToolArgumentValidator.Validate(Descriptor, arguments);
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("tool.argument.type");
    }

    [Fact]
    public void MissingRequiredArgumentFails()
    {
        ArgumentBag arguments = ArgumentBag.From([new ArgumentEntry("left", ArgumentValue.FromInteger(1))]);
        Result result = ToolArgumentValidator.Validate(Descriptor, arguments);
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("tool.argument.missing");
    }

    [Fact]
    public void MissingOptionalArgumentPasses()
    {
        Result result = ToolArgumentValidator.Validate(Descriptor, TwoIntegers());
        result.IsSuccess.Should().BeTrue();
    }

    private static ArgumentBag TwoIntegers()
    {
        return ArgumentBag.From(
    [
        new ArgumentEntry("left", ArgumentValue.FromInteger(1)),
        new ArgumentEntry("right", ArgumentValue.FromInteger(2)),
    ]);
    }
}