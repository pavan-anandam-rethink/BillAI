using BillingService.SharedKernel.Primitives;

namespace BillingService.Architecture.XUnit.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Success_DoesNotCarryError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_RequiresError()
    {
        var error = Error.Validation("billing.validation", "Validation failed.");

        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void FailedGenericResult_DoesNotExposeValue()
    {
        var result = Result<int>.Failure(Error.NotFound("billing.not_found", "Missing."));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}
