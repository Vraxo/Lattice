namespace Lattice.Core.Tests;

public sealed class ControlledLanguageParserTests
{
    [Fact]
    public void RejectsNullInput()
    {
        Result<IControlledStatement> result = ControlledLanguageParser.Parse(null!);
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(ControlledErrorKind.Empty);
    }

    [Fact]
    public void RejectsWhitespaceInput()
    {
        Result<IControlledStatement> result = ControlledLanguageParser.Parse("   ");
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(ControlledErrorKind.Empty);
    }

    [Fact]
    public void SpanCoversWholeInput()
    {
        const string input = "fact a | b | c";
        Result<IControlledStatement> result = ControlledLanguageParser.Parse(input);
        result.IsSuccess.Should().BeTrue();
        result.Value.Span.Start.Should().Be(0);
        result.Value.Span.Length.Should().Be(input.Length);
    }

    [Fact]
    public void ParsesActionWithNoArguments()
    {
        Result<IControlledStatement> result = ControlledLanguageParser.Parse("action clock");
        result.IsSuccess.Should().BeTrue();
        ActionStatement action = result.Value.Should().BeOfType<ActionStatement>().Which;
        action.ToolId.Should().Be("clock");
        action.Arguments.Should().BeEmpty();
    }

    [Fact]
    public void ActionStatementsWithSameContentAreEqual()
    {
        ActionStatement first = (ActionStatement)ControlledLanguageParser.Parse("action calculator | left=2").Value;
        ActionStatement second = (ActionStatement)ControlledLanguageParser.Parse("action calculator | left=2").Value;
        second.Should().Be(first);
    }

    [Fact]
    public void ActionStatementsWithDifferentArgumentsAreNotEqual()
    {
        ActionStatement first = (ActionStatement)ControlledLanguageParser.Parse("action calculator | left=2").Value;
        ActionStatement second = (ActionStatement)ControlledLanguageParser.Parse("action calculator | left=3").Value;
        second.Should().NotBe(first);
    }
}