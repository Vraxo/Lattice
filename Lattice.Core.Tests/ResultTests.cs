namespace Lattice.Core.Tests;

public sealed class ResultTests
{
    [Fact]
    public void NonGenericSuccessIsSuccessfulWithNoError()
    {
        Result result = Result.Success();
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void NonGenericFailureCarriesError()
    {
        Error error = new("test.failure", "Something expected went wrong.");
        Result result = Result.Failure(error);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeSameAs(error);
    }

    [Fact]
    public void NonGenericFailureRejectsNullError()
    {
        FluentActions.Invoking(() => Result.Failure(null!)).Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void GenericSuccessExposesValue()
    {
        Result<int> result = Result<int>.Success(42);
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void GenericFailureExposesErrorAndValueAccessThrows()
    {
        Error error = new("test.failure", "boom");
        Result<string> result = Result<string>.Failure(error);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeSameAs(error);
        FluentActions.Invoking(() => result.Value).Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void GenericSuccessRejectsNullValueForReferenceType()
    {
        FluentActions.Invoking(() => Result<string>.Success(null!)).Should().ThrowExactly<ArgumentNullException>();
    }
}