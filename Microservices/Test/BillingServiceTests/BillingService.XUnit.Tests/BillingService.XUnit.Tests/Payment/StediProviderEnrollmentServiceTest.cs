using BillingService.Domain.Models;
using BillingService.Domain.Services.Payment;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace BillingService.XUnit.Tests.Payment
{
    public class StediProviderEnrollmentServiceTest : IDisposable
    {
        private readonly Mock<IConfiguration> _configuration;
        private readonly Mock<ILogger<StediProviderEnrollmentService>> _logger;
        private readonly Mock<IKeyVaultProviderService> _keyVaultProviderService;
        private readonly WireMockServer _server;

        public StediProviderEnrollmentServiceTest()
        {
            _configuration = new Mock<IConfiguration>();
            _logger = new Mock<ILogger<StediProviderEnrollmentService>>();
            _keyVaultProviderService = new Mock<IKeyVaultProviderService>();
            _server = WireMockServer.Start();
        }

        public void Dispose()
        {
            _server?.Stop();
            _server?.Dispose();
        }

        private StediProviderEnrollmentService CreateService(string enrollmentUrl = null, string apiKey = "test-stedi-api-key")
        {
            var url = enrollmentUrl ?? $"{_server.Url}/enrollments";

            _configuration.Setup(x => x["Clearinghouses:Stedi:EnrollmenUrl"]).Returns(url);
            _configuration.Setup(x => x["Clearinghouses:Stedi:ApiKey"]).Returns(apiKey);

            return new StediProviderEnrollmentService(
                new HttpClient(),
                _configuration.Object,
                _logger.Object,
                _keyVaultProviderService.Object);
        }

        [Fact]
        public void Constructor_ShouldInitializeAllFields()
        {
            // Act
            var service = CreateService();

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_ShouldReadEnrollmentUrlFromConfiguration()
        {
            // Act
            CreateService();

            // Assert
            _configuration.Verify(x => x["Clearinghouses:Stedi:EnrollmenUrl"], Times.Once);
        }

        [Fact]
        public void Constructor_ShouldReadApiKeyFromConfiguration()
        {
            // Act
            CreateService();

            // Assert
            _configuration.Verify(x => x["Clearinghouses:Stedi:ApiKey"], Times.Once);
        }

        [Fact]
        public void Constructor_ShouldHandleNullConfigurationValues()
        {
            // Arrange
            var config = new Mock<IConfiguration>();
            config.Setup(x => x["Clearinghouses:Stedi:EnrollmenUrl"]).Returns((string)null);
            config.Setup(x => x["Clearinghouses:Stedi:ApiKey"]).Returns((string)null);

            // Act
            var service = new StediProviderEnrollmentService(
                new HttpClient(),
                config.Object,
                _logger.Object,
                _keyVaultProviderService.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public async Task VerifyProviderEnrollmentAsync_ShouldReturnTrue_WhenProviderNpiIsFound()
        {
            // Arrange
            var providerNpi = "1234567890";

            var enrollmentResponse = new EnrollmentResponse
            {
                Items = new List<EnrollmentItem>
                {
                    new EnrollmentItem
                    {
                        Id = "enrollment-1",
                        Provider = new Providers
                        {
                            Npi = "1234567890",
                            Name = "Test Provider"
                        },
                        Status = "LIVE"
                    }
                }
            };

            var responseJson = JsonSerializer.Serialize(enrollmentResponse);

            _server.Given(
                Request.Create()
                    .WithPath("/enrollments")
                    .WithParam("status", "LIVE")
                    .UsingGet())
                .RespondWith(
                    Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(responseJson));

            var service = CreateService();

            // Act
            var result = await service.VerifyProviderEnrollmentAsync(providerNpi);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task VerifyProviderEnrollmentAsync_ShouldReturnFalse_WhenProviderNpiIsNotFound()
        {
            // Arrange
            var providerNpi = "0000000000";

            var enrollmentResponse = new EnrollmentResponse
            {
                Items = new List<EnrollmentItem>
                {
                    new EnrollmentItem
                    {
                        Id = "enrollment-1",
                        Provider = new Providers
                        {
                            Npi = "1234567890",
                            Name = "Other Provider"
                        },
                        Status = "LIVE"
                    }
                }
            };

            var responseJson = JsonSerializer.Serialize(enrollmentResponse);

            _server.Given(
                Request.Create()
                    .WithPath("/enrollments")
                    .WithParam("status", "LIVE")
                    .UsingGet())
                .RespondWith(
                    Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(responseJson));

            var service = CreateService();

            // Act
            var result = await service.VerifyProviderEnrollmentAsync(providerNpi);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task VerifyProviderEnrollmentAsync_ShouldReturnFalse_WhenItemsListIsEmpty()
        {
            // Arrange
            var enrollmentResponse = new EnrollmentResponse
            {
                Items = new List<EnrollmentItem>()
            };

            var responseJson = JsonSerializer.Serialize(enrollmentResponse);

            _server.Given(
                Request.Create()
                    .WithPath("/enrollments")
                    .WithParam("status", "LIVE")
                    .UsingGet())
                .RespondWith(
                    Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(responseJson));

            var service = CreateService();

            // Act
            var result = await service.VerifyProviderEnrollmentAsync("1234567890");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task VerifyProviderEnrollmentAsync_ShouldReturnTrue_WhenMultipleProvidersAndOneMatches()
        {
            // Arrange
            var providerNpi = "9999999999";

            var enrollmentResponse = new EnrollmentResponse
            {
                Items = new List<EnrollmentItem>
                {
                    new EnrollmentItem
                    {
                        Id = "enrollment-1",
                        Provider = new Providers { Npi = "1111111111", Name = "Provider A" },
                        Status = "LIVE"
                    },
                    new EnrollmentItem
                    {
                        Id = "enrollment-2",
                        Provider = new Providers { Npi = "9999999999", Name = "Provider B" },
                        Status = "LIVE"
                    }
                }
            };

            var responseJson = JsonSerializer.Serialize(enrollmentResponse);

            _server.Given(
                Request.Create()
                    .WithPath("/enrollments")
                    .WithParam("status", "LIVE")
                    .UsingGet())
                .RespondWith(
                    Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(responseJson));

            var service = CreateService();

            // Act
            var result = await service.VerifyProviderEnrollmentAsync(providerNpi);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task VerifyProviderEnrollmentAsync_ShouldReturnFalse_WhenResponseIsNullObject()
        {
            // Arrange
            _server.Given(
                Request.Create()
                    .WithPath("/enrollments")
                    .WithParam("status", "LIVE")
                    .UsingGet())
                .RespondWith(
                    Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody("null"));

            var service = CreateService();

            // Act
            var result = await service.VerifyProviderEnrollmentAsync("1234567890");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task VerifyProviderEnrollmentAsync_ShouldReturnFalse_WhenExceptionIsThrown()
        {
            // Arrange - use unreachable URL to force exception
            var service = CreateService("https://unreachable-host-99999.invalid/enrollments");

            // Act
            var result = await service.VerifyProviderEnrollmentAsync("1234567890");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task VerifyProviderEnrollmentAsync_ShouldLogInformation_WhenCalled()
        {
            // Arrange - unreachable URL triggers catch, but LogInformation happens before the HTTP call
            var service = CreateService("https://unreachable-host-99999.invalid/enrollments");

            // Act
            await service.VerifyProviderEnrollmentAsync("1234567890");

            // Assert
            _logger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task VerifyProviderEnrollmentAsync_ShouldLogError_WhenExceptionIsThrown()
        {
            // Arrange
            var service = CreateService("https://unreachable-host-99999.invalid/enrollments");

            // Act
            await service.VerifyProviderEnrollmentAsync("1234567890");

            // Assert
            _logger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task VerifyProviderEnrollmentAsync_ShouldCallLogInformationWithNpi()
        {
            // Arrange
            var providerNpi = "9876543210";
            var service = CreateService("https://unreachable-host-99999.invalid/enrollments");

            // Act
            await service.VerifyProviderEnrollmentAsync(providerNpi);

            // Assert
            _logger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains(providerNpi)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task VerifyProviderEnrollmentAsync_ExceptionPath_ShouldLogBothInfoAndError()
        {
            // Arrange
            var service = CreateService("https://unreachable-host-99999.invalid/enrollments");

            // Act
            var result = await service.VerifyProviderEnrollmentAsync("testNpi");

            // Assert
            Assert.False(result);
            _logger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => true),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _logger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}
