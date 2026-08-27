using System;
using FluentAssertions;
using PesaGraph.Shared.Errors;
using Xunit;

namespace PesaGraph.UnitTests.Shared;

public class ErrorTests
{
    [Fact]
    public void None_ShouldHaveEmptyCodeAndDescription()
    {
        Error.None.Code.Should().BeEmpty();
        Error.None.Description.Should().BeEmpty();
        Error.None.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void NullValue_ShouldHaveExpectedCode()
    {
        Error.NullValue.Code.Should().Be("General.NullValue");
        Error.NullValue.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void Failure_ShouldCreateErrorWithFailureType()
    {
        var error = Error.Failure("Test.Code", "Test description.");

        error.Code.Should().Be("Test.Code");
        error.Description.Should().Be("Test description.");
        error.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void NotFound_ShouldCreateErrorWithNotFoundType()
    {
        var error = Error.NotFound("Entity.NotFound", "Not found.");

        error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void Validation_ShouldCreateErrorWithValidationType()
    {
        var error = Error.Validation("Field.Required", "Required.");

        error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Conflict_ShouldCreateErrorWithConflictType()
    {
        var error = Error.Conflict("Entity.Exists", "Already exists.");

        error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void Unauthorized_ShouldCreateErrorWithUnauthorizedType()
    {
        var error = Error.Unauthorized("Auth.Required", "Unauthorized.");

        error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public void Forbidden_ShouldCreateErrorWithForbiddenType()
    {
        var error = Error.Forbidden("Access.Denied", "Forbidden.");

        error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public void Equality_WithSameCodeAndType_ShouldBeEqual()
    {
        var a = Error.Failure("Code.A", "Description A.");
        var b = Error.Failure("Code.A", "Description A.");

        a.Should().Be(b);
    }
}
