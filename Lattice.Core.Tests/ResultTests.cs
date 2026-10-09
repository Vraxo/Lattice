namespace Lattice.Core.Tests;

public sealed class ResultTests
{
    [Fact]
    public void NonGenericSuccessIsSuccessfulWithNoError()
    {
        Result result = Result.Success();
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
    }

    [Fact]
    public void NonGenericFailureCarriesError()
    {
        Error error = new("test.failure", "Something expected went wrong.");
        Result result = Result.Failure(error);
        Assert.False(result.IsSuccess);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void NonGenericFailureRejectsNullError()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
    }

    [Fact]
    public void GenericSuccessExposesValue()
    {
        Result<int> result = Result<int>.Success(42);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFailureExposesErrorAndValueAccessThrows()
    {
        Error error = new("test.failure", "boom");
        Result<string> result = Result<string>.Failure(error);
        Assert.False(result.IsSuccess);
        Assert.Same(error, result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void GenericSuccessRejectsNullValueForReferenceType()
    {
        Assert.Throws<ArgumentNullException>(() => Result<string>.Success(null!));
    }
}