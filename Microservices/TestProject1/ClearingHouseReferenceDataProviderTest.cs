using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ClearingHouseService.Web.Service;
using Rethink.Services.Domain.Interfaces;

public class ClearingHouseReferenceDataProviderTest
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<IKeyVaultProviderService> _mockKeyVault;
    private readonly ClearingHouseReferenceDataProvider _provider;

    public ClearingHouseReferenceDataProviderTest()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockKeyVault = new Mock<IKeyVaultProviderService>();

        _mockConfig.Setup(c => c["Clearinghouses:Stedi:BaseUrl"]).Returns("https://healthcare.us.stedi.com");
        _mockConfig.Setup(c => c["Clearinghouses:Stedi:ApiKey"]).Returns("GQrIqW2.zFxGOATKnCQjcrYw2u34uUL6");
        _mockConfig.Setup(c => c["Clearinghouses:Stedi:GetPayersUrl"]).Returns("/2024-04-01/payers/csv");
        _mockConfig.Setup(c => c["Clearinghouses:Stedi:GetEnrollmenUrl"]).Returns("/2024-09-01/enrollments/{enrollmentId}");

        _mockKeyVault.Setup(k => k.GetSecretAsync(It.IsAny<string>()))
            .ReturnsAsync((string secretName) => secretName switch
            {
                "https://healthcare.us.stedi.com" => "https://healthcare.us.stedi.com",
                "GQrIqW2.zFxGOATKnCQjcrYw2u34uUL6" => "GQrIqW2.zFxGOATKnCQjcrYw2u34uUL6",
                "/2024-04-01/payers/csv" => "/2024-04-01/payers/csv",
                "/2024-09-01/enrollments/{enrollmentId}" => "/2024-09-01/enrollments/{enrollmentId}",
                _ => secretName
            });

        _provider = new ClearingHouseReferenceDataProvider(_mockConfig.Object, _mockKeyVault.Object);
    }

    [Fact]
    public async Task GetPayersAsync_ReturnsString()
    {
        // Arrange
        var ct = CancellationToken.None;

        // Act
        var result = await _provider.GetPayersAsync(ct);

        // Assert
        Assert.NotNull(result);
        // Additional asserts can be added based on expected behavior
    }
   
}
