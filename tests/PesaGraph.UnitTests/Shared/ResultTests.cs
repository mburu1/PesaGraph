using System;
using FluentAssertions;
using PesaGraph.Shared.Errors;
using PesaGraph.Shared.Results;
using Xunit;

namespace PesaGraph.UnitTests.Shared;

public class ResultTests
{
    [Fact]
    public void Success_ShouldReturnSuccessResult()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_ShouldReturnFailureResult()
    {
        var error = Error.Failure("Test.Error", "Something went wrong.");
        var result = Result.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Success_Generic_ShouldReturnSuccessResultWithValue()
    {
        const string value = "hello";
        var result = Result.Success(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void Failure_Generic_ShouldReturnFailureResult()
    {
        var error = Error.NotFound("Entity.NotFound", "Entity was not found.");
        var result = Result.Failure<string>(error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Value_WhenResultIsFailure_ShouldThrowInvalidOperationException()
    {
        var error = Error.Failure("Test.Error", "Failed.");
        var result = Result.Failure<string>(error);

        var act = () => result.Value;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ImplicitConversion_FromNonNullValue_ShouldCreateSuccessResult()
    {
        Result<string> result = "test-value";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("test-value");
    }

    [Fact]
    public void ImplicitConversion_FromNullValue_ShouldCreateFailureResult()
    {
        Result<string> result = (string?)null;

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Error.NullValue);
    }

    [Fact]
    public void ImplicitConversion_FromError_ShouldCreateFailureResult()
    {
        var error = Error.Validation("Field.Required", "Field is required.");
        Result<int> result = error;

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void ValueOrDefault_WhenSuccess_ShouldReturnValue()
    {
        var result = Result.Success(42);

        result.ValueOrDefault.Should().Be(42);
    }

    [Fact]
    public void ValueOrDefault_WhenFailure_ShouldReturnDefault()
    {
        var result = Result.Failure<int>(Error.Failure("x", "x"));

        result.ValueOrDefault.Should().Be(0);
    }
}
