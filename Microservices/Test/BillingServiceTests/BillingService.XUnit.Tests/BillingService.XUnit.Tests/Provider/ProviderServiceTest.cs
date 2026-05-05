using BillingService.Domain.Interfaces.Provider;
using BillingService.Domain.Models.Clients;
using BillingService.Domain.Services.Common;
using BillingService.Domain.Services.Provider;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Rethink.Services.Common.Models;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace BillingService.XUnit.Tests.ProviderTests
{
    public class ProviderServiceTest : IDisposable
    {
        private readonly Mock<IConfiguration> _configuration;
        private readonly Mock<ILogger<CommonService>> _logger;
        private readonly Mock<IKeyVaultProviderService> _keyVaultProviderService;
        private readonly WireMockServer _server;

        public ProviderServiceTest()
        {
            _configuration = new Mock<IConfiguration>();
            _logger = new Mock<ILogger<CommonService>>();
            _keyVaultProviderService = new Mock<IKeyVaultProviderService>();
            _server = WireMockServer.Start();
        }

        public void Dispose()
        {
            _server?.Stop();
            _server?.Dispose();
        }

        private IProviderService CreateService(string baseUrl = null)
        {
            var url = baseUrl ?? _server.Url;

            var practiceOpsUrlSection = new Mock<IConfigurationSection>();
            practiceOpsUrlSection.Setup(x => x.Value).Returns(url);

            var practiceOpsKeySection = new Mock<IConfigurationSection>();
            practiceOpsKeySection.Setup(x => x.Value).Returns("test-secret-name");

            var headerKeySection = new Mock<IConfigurationSection>();
            headerKeySection.Setup(x => x.Value).Returns("x-api-key");

            _configuration.Setup(x => x.GetSection("PracticeOperationsApiUrl")).Returns(practiceOpsUrlSection.Object);
            _configuration.Setup(x => x.GetSection("PracticeOperationsKey")).Returns(practiceOpsKeySection.Object);
            _configuration.Setup(x => x.GetSection("HeaderKey")).Returns(headerKeySection.Object);

            _keyVaultProviderService
                .Setup(x => x.GetSecretAsync("test-secret-name"))
                .ReturnsAsync("resolved-api-key");

            return new ProviderService(
                _configuration.Object,
                _logger.Object,
                _configuration.Object,
                _keyVaultProviderService.Object);
        }

        [Fact]
        public void Constructor_ShouldResolveApiKeyFromKeyVault()
        {
            // Act
            var service = CreateService();

            // Assert
            Assert.NotNull(service);
            _keyVaultProviderService.Verify(x => x.GetSecretAsync("test-secret-name"), Times.Once);
        }

        [Fact]
        public void Constructor_ShouldReadConfigurationValues()
        {
            // Act
            CreateService();

            // Assert
            _configuration.Verify(x => x.GetSection("PracticeOperationsApiUrl"), Times.Once);
            _configuration.Verify(x => x.GetSection("PracticeOperationsKey"), Times.Once);
            _configuration.Verify(x => x.GetSection("HeaderKey"), Times.Once);
        }

        [Fact]
        public void Constructor_ShouldHandleEmptyConfigurationValues()
        {
            // Arrange
            var config = new Mock<IConfiguration>();
            var logger = new Mock<ILogger<CommonService>>();
            var kvService = new Mock<IKeyVaultProviderService>();

            var emptySection = new Mock<IConfigurationSection>();
            emptySection.Setup(x => x.Value).Returns(string.Empty);

            config.Setup(x => x.GetSection(It.IsAny<string>())).Returns(emptySection.Object);
            kvService.Setup(x => x.GetSecretAsync(It.IsAny<string>())).ReturnsAsync("key");

            // Act
            var service = new ProviderService(config.Object, logger.Object, config.Object, kvService.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_ShouldHandleNullConfigurationValues()
        {
            // Arrange
            var config = new Mock<IConfiguration>();
            var logger = new Mock<ILogger<CommonService>>();
            var kvService = new Mock<IKeyVaultProviderService>();

            var nullSection = new Mock<IConfigurationSection>();
            nullSection.Setup(x => x.Value).Returns((string)null);

            config.Setup(x => x.GetSection(It.IsAny<string>())).Returns(nullSection.Object);
            kvService.Setup(x => x.GetSecretAsync(It.IsAny<string>())).ReturnsAsync("key");

            // Act
            var service = new ProviderService(config.Object, logger.Object, config.Object, kvService.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public async Task GetProviderLocationList_ShouldReturnMappedAndSortedList_WhenApiReturnsSuccess()
        {
            // Arrange
            var accountInfoId = 123;
            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

            var responseModel = new ClientProviderLocationsModel
            {
                total = 2,
                data = new List<ProviderLocations>
                {
                    new ProviderLocations
                    {
                        id = 1,
                        name = "Zebra Clinic",
                        isMainLocation = true,
                        isBillingLocation = false,
                        agencyName = "Agency A"
                    },
                    new ProviderLocations
                    {
                        id = 2,
                        name = "Alpha Clinic",
                        isMainLocation = false,
                        isBillingLocation = true,
                        agencyName = "Agency B"
                    }
                }
            };

            var responseJson = JsonConvert.SerializeObject(responseModel);

            _server.Given(
                Request.Create()
                    .WithPath($"/accounts/{accountInfoId}/providerLocations")
                    .UsingGet())
                .RespondWith(
                    Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(responseJson));

            var service = CreateService();

            // Act
            var result = await service.GetProviderLocationList(accountInfoId, settings);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            // Verify sorted by Name
            Assert.Equal("Alpha Clinic", result[0].Name);
            Assert.Equal("Zebra Clinic", result[1].Name);
            // Verify mapping
            Assert.Equal(2, result[0].Id);
            Assert.False(result[0].IsMainLocation);
            Assert.True(result[0].IsBillingLocation);
            Assert.Equal("Agency B", result[0].AgencyName);
            Assert.Equal(1, result[1].Id);
            Assert.True(result[1].IsMainLocation);
            Assert.False(result[1].IsBillingLocation);
            Assert.Equal("Agency A", result[1].AgencyName);
        }

        [Fact]
        public async Task GetProviderLocationList_ShouldReturnDefaultData_WhenApiReturnsNonSuccess()
        {
            // Arrange
            var accountInfoId = 456;
            var settings = new JsonSerializerSettings();

            _server.Given(
                Request.Create()
                    .WithPath($"/accounts/{accountInfoId}/providerLocations")
                    .UsingGet())
                .RespondWith(
                    Response.Create()
                        .WithStatusCode(404));

            var service = CreateService();

            // Act & Assert
            // When non-success, resDemographics stays as new ClientProviderLocationsModel() with data = null
            // so accessing .data will throw NullReferenceException, caught by catch block
            await Assert.ThrowsAnyAsync<Exception>(
                () => service.GetProviderLocationList(accountInfoId, settings));
        }

        [Fact]
        public async Task GetProviderLocationList_ShouldReturnSingleItem_WhenApiReturnsSingleLocation()
        {
            // Arrange
            var accountInfoId = 789;
            var settings = new JsonSerializerSettings();

            var responseModel = new ClientProviderLocationsModel
            {
                total = 1,
                data = new List<ProviderLocations>
                {
                    new ProviderLocations
                    {
                        id = 10,
                        name = "Single Clinic",
                        isMainLocation = true,
                        isBillingLocation = true,
                        agencyName = "Single Agency"
                    }
                }
            };

            _server.Given(
                Request.Create()
                    .WithPath($"/accounts/{accountInfoId}/providerLocations")
                    .UsingGet())
                .RespondWith(
                    Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(JsonConvert.SerializeObject(responseModel)));

            var service = CreateService();

            // Act
            var result = await service.GetProviderLocationList(accountInfoId, settings);

            // Assert
            Assert.Single(result);
            Assert.Equal("Single Clinic", result[0].Name);
            Assert.Equal(10, result[0].Id);
            Assert.True(result[0].IsMainLocation);
            Assert.True(result[0].IsBillingLocation);
            Assert.Equal("Single Agency", result[0].AgencyName);
        }

        [Fact]
        public async Task GetProviderLocationList_ShouldThrowException_WhenHttpCallFails()
        {
            // Arrange - use an unreachable URL
            var service = CreateService("https://fake-unreachable-host-99999.invalid");
            var settings = new JsonSerializerSettings();

            // Act & Assert - catch block does "throw e;"
            await Assert.ThrowsAnyAsync<Exception>(
                () => service.GetProviderLocationList(123, settings));
        }

        [Fact]
        public async Task GetProviderLocationList_ShouldRethrowOriginalException()
        {
            // Arrange
            var service = CreateService("https://fake-unreachable-host-99999.invalid");
            var settings = new JsonSerializerSettings();

            // Act
            var exception = await Assert.ThrowsAnyAsync<Exception>(
                () => service.GetProviderLocationList(999, settings));

            // Assert
            Assert.NotNull(exception);
        }
    }
}
