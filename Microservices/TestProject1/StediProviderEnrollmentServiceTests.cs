using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Services.Payment;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Rethink.Services.Domain.Interfaces;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ClearingHouseService.Tests.Service
{
    public class StediProviderEnrollmentServiceTests
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<IKeyVaultProviderService> _keyVaultMock;
        private readonly Mock<ILogger<StediProviderEnrollmentService>> _loggerMock;

        public StediProviderEnrollmentServiceTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            _keyVaultMock = new Mock<IKeyVaultProviderService>();
            _loggerMock = new Mock<ILogger<StediProviderEnrollmentService>>();

            _configurationMock.Setup(x => x["Clearinghouses:Stedi:EnrollmenUrl"])
                .Returns("https://test-url");

            _configurationMock.Setup(x => x["Clearinghouses:Stedi:ApiKey"])
                .Returns("test-api-key");
        }

        private StediProviderEnrollmentService CreateService(HttpResponseMessage response)
        {
            var handlerMock = new Mock<HttpMessageHandler>();

            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            var httpClient = new HttpClient(handlerMock.Object);

            return new StediProviderEnrollmentService(
                httpClient,
                _configurationMock.Object,
                _loggerMock.Object,
                _keyVaultMock.Object);
        }

        [Fact]
        public async Task VerifyProviderEnrollmentAsync_ReturnsFalse_WhenNpiDoesNotExist()
        {
            var json = @"{
              ""Items"": [
                {
                  ""Provider"": {
                    ""Npi"": ""9999999999""
                  }
                }
              ]
            }";

            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var service = CreateService(response);

            var result = await service.VerifyProviderEnrollmentAsync("1234567890");

            Assert.False(result);
        }

        [Fact]
        public async Task VerifyProviderEnrollmentAsync_ReturnsFalse_WhenItemsEmpty()
        {
            var json = @"{ ""Items"": [] }";

            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var service = CreateService(response);

            var result = await service.VerifyProviderEnrollmentAsync("1234567890");

            Assert.False(result);
        }

        [Fact]
        public async Task VerifyProviderEnrollmentAsync_ReturnsFalse_WhenResponseNull()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };

            var service = CreateService(response);

            var result = await service.VerifyProviderEnrollmentAsync("1234567890");

            Assert.False(result);
        }

        [Fact]
        public async Task VerifyProviderEnrollmentAsync_ReturnsFalse_WhenExceptionOccurs()
        {
            var handlerMock = new Mock<HttpMessageHandler>();

            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException());

            var httpClient = new HttpClient(handlerMock.Object);

            var service = new StediProviderEnrollmentService(
                httpClient,
                _configurationMock.Object,
                _loggerMock.Object,
                _keyVaultMock.Object);

            var result = await service.VerifyProviderEnrollmentAsync("123");

            Assert.False(result);
        }

        [Fact]
        public async Task VerifyProviderEnrollmentAsync_LogsInformation()
        {
            var json = @"{
              ""Items"": [
                {
                  ""Provider"": {
                    ""Npi"": ""1234567890""
                  }
                }
              ]
            }";

            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var service = CreateService(response);

            await service.VerifyProviderEnrollmentAsync("1234567890");

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task VerifyProviderEnrollmentAsync_ReturnsTrue_WhenNpiExists()
        {
            // Arrange local test server
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:5055/");
            listener.Start();

            var serverTask = Task.Run(async () =>
            {
                var context = await listener.GetContextAsync();

                var responseJson = @"{
                    ""Items"": [
                        {
                            ""Provider"": {
                                ""Npi"": ""1234567890""
                            }
                        }
                    ]
                }";

                var buffer = Encoding.UTF8.GetBytes(responseJson);

                context.Response.ContentLength64 = buffer.Length;
                await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();
            });

            _configurationMock.Setup(x => x["Clearinghouses:Stedi:EnrollmenUrl"])
                .Returns("http://localhost:5055/");

            _configurationMock.Setup(x => x["Clearinghouses:Stedi:ApiKey"])
                .Returns("test-key");

            var httpClient = new HttpClient();

            var service = new StediProviderEnrollmentService(
                httpClient,
                _configurationMock.Object,
                _loggerMock.Object,
                _keyVaultMock.Object);

            // Act
            var result = await service.VerifyProviderEnrollmentAsync("1234567890");

            // Cleanup
            listener.Stop();

            // Assert
            Assert.True(result);
        }
    }
}