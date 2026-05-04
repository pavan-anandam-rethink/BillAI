using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Services.Billing;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing;

public class FeatureFlagServiceTests
{
    private readonly Mock<IKeyVaultProviderService> _keyVaultMock;
    private readonly Mock<ILogger<FeatureFlagService>> _loggerMock;
    private readonly FeatureFlagService _sut;

    public FeatureFlagServiceTests()
    {
        _keyVaultMock = new Mock<IKeyVaultProviderService>();
        _loggerMock = new Mock<ILogger<FeatureFlagService>>();
        _sut = new FeatureFlagService(_keyVaultMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task IsProviderEnrollmentValidationEnabledAsync_WhenKeyVaultReturnsTrue_ReturnsTrue()
    {
        _keyVaultMock
            .Setup(x => x.GetSecretAsync("EnableProviderEnrollmentValidation"))
            .ReturnsAsync("true");

        var result = await _sut.IsProviderEnrollmentValidationEnabledAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task IsProviderEnrollmentValidationEnabledAsync_WhenKeyVaultReturnsFalse_ReturnsFalse()
    {
        _keyVaultMock
            .Setup(x => x.GetSecretAsync("EnableProviderEnrollmentValidation"))
            .ReturnsAsync("false");

        var result = await _sut.IsProviderEnrollmentValidationEnabledAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task IsProviderEnrollmentValidationEnabledAsync_WhenKeyVaultThrows_ReturnsFalseDefault()
    {
        _keyVaultMock
            .Setup(x => x.GetSecretAsync("EnableProviderEnrollmentValidation"))
            .ThrowsAsync(new Exception("Key Vault unavailable"));

        var result = await _sut.IsProviderEnrollmentValidationEnabledAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task IsProviderEnrollmentValidationEnabledAsync_WhenKeyVaultReturnsInvalidValue_ReturnsFalseDefault()
    {
        _keyVaultMock
            .Setup(x => x.GetSecretAsync("EnableProviderEnrollmentValidation"))
            .ReturnsAsync("not-a-boolean");

        var result = await _sut.IsProviderEnrollmentValidationEnabledAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task IsProviderEnrollmentValidationEnabledAsync_WhenKeyVaultReturnsNull_ReturnsFalseDefault()
    {
        _keyVaultMock
            .Setup(x => x.GetSecretAsync("EnableProviderEnrollmentValidation"))
            .ReturnsAsync((string)null);

        var result = await _sut.IsProviderEnrollmentValidationEnabledAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task IsProviderEnrollmentValidationEnabledAsync_CallsKeyVaultEachTime()
    {
        _keyVaultMock
            .Setup(x => x.GetSecretAsync("EnableProviderEnrollmentValidation"))
            .ReturnsAsync("true");

        var result1 = await _sut.IsProviderEnrollmentValidationEnabledAsync();
        var result2 = await _sut.IsProviderEnrollmentValidationEnabledAsync();

        Assert.True(result1);
        Assert.True(result2);
        _keyVaultMock.Verify(
            x => x.GetSecretAsync("EnableProviderEnrollmentValidation"),
            Times.Exactly(2));
    }
}
