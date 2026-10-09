namespace Lattice.Core.Tests;
public sealed class ToolResultTests
{
    [Fact]
    public void SuccessExposesOutputAndNoError()
    {
        var result = ToolResult.Success(ArgumentValue.FromInteger(3));
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(ArgumentValue.FromInteger(3), result.Output);
    }
    [Fact]
    public void FailureExposesErrorAndNoOutput()
    {
        var error = new Error("tool.failure", "failed");
        var result = ToolResult.Failure(error);
        Assert.False(result.IsSuccess);
        Assert.Same(error, result.Error);
        Assert.Null(result.Output);
    }
    [Fact]
    public void SuccessRejectsNullOutput()
    {
        Assert.Throws<ArgumentNullException>(() => ToolResult.Success(null!));
    }
    [Fact]
    public void FailureRejectsNullError()
    {
        Assert.Throws<ArgumentNullException>(() => ToolResult.Failure(null!));
    }
    [Fact]
    public void ResultsWithSameComponentsAreEqual()
    {
        Assert.Equal(
            ToolResult.Success(ArgumentValue.FromInteger(1)),
            ToolResult.Success(ArgumentValue.FromInteger(1)));
    }
}