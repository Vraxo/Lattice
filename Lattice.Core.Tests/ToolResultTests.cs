namespace Lattice.Core.Tests;

public sealed class ToolResultTests
{
    [Fact]
    public void SuccessExposesOutputAndNoError()
    {
        ToolResult result = ToolResult.Success(ArgumentValue.FromInteger(3));
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Output.Should().Be(ArgumentValue.FromInteger(3));
    }

    [Fact]
    public void FailureExposesErrorAndNoOutput()
    {
        Error error = new("tool.failure", "failed");
        ToolResult result = ToolResult.Failure(error);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeSameAs(error);
        result.Output.Should().BeNull();
    }

    [Fact]
    public void SuccessRejectsNullOutput()
    {
        FluentActions.Invoking(() => ToolResult.Success(null!)).Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void FailureRejectsNullError()
    {
        FluentActions.Invoking(() => ToolResult.Failure(null!)).Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void ResultsWithSameComponentsAreEqual()
    {
        ToolResult.Success(ArgumentValue.FromInteger(1)).Should().Be(ToolResult.Success(ArgumentValue.FromInteger(1)));
    }
}