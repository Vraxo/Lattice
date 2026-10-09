namespace Lattice.Core.Tests;

public sealed class ControlledLanguageParserTests
{
    [Fact]
    public void RejectsNullInput()
    {
        Result<IControlledStatement> result = ControlledLanguageParser.Parse(null!);
        Assert.False(result.IsSuccess);
        Assert.Equal(ControlledErrorKind.Empty, result.Error!.Code);
    }

    [Fact]
    public void RejectsWhitespaceInput()
    {
        Result<IControlledStatement> result = ControlledLanguageParser.Parse("   ");
        Assert.False(result.IsSuccess);
        Assert.Equal(ControlledErrorKind.Empty, result.Error!.Code);
    }

    [Fact]
    public void SpanCoversWholeInput()
    {
        const string input = "fact a | b | c";
        Result<IControlledStatement> result = ControlledLanguageParser.Parse(input);
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Span.Start);
        Assert.Equal(input.Length, result.Value.Span.Length);
    }

    [Fact]
    public void ParsesActionWithNoArguments()
    {
        Result<IControlledStatement> result = ControlledLanguageParser.Parse("action clock");
        Assert.True(result.IsSuccess);
        ActionStatement action = Assert.IsType<ActionStatement>(result.Value);
        Assert.Equal("clock", action.ToolId);
        Assert.Empty(action.Arguments);
    }

    [Fact]
    public void ActionStatementsWithSameContentAreEqual()
    {
        ActionStatement first = (ActionStatement)ControlledLanguageParser.Parse("action calculator | left=2").Value;
        ActionStatement second = (ActionStatement)ControlledLanguageParser.Parse("action calculator | left=2").Value;
        Assert.Equal(first, second);
    }

    [Fact]
    public void ActionStatementsWithDifferentArgumentsAreNotEqual()
    {
        ActionStatement first = (ActionStatement)ControlledLanguageParser.Parse("action calculator | left=2").Value;
        ActionStatement second = (ActionStatement)ControlledLanguageParser.Parse("action calculator | left=3").Value;
        Assert.NotEqual(first, second);
    }
}