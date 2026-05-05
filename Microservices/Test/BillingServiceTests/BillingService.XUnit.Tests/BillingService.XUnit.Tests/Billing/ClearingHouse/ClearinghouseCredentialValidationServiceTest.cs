using BillingService.Domain.Services.Billing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Clearinghouse;

public class ClearinghouseCredentialValidationServiceTest
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IKeyVaultProviderService> _keyVaultProviderServiceMock;
    private readonly Mock<ILogger<ClearinghouseCredentialValidationService>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

    private const string BaseUrl = "https://clearinghouse-api.example.com";
    private const string ValidationEndpoint = "api/validate-credentials";
    private const string ApiKey = "test-api-key-12345";

    public ClearinghouseCredentialValidationServiceTest()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
        _keyVaultProviderServiceMock = new Mock<IKeyVaultProviderService>();
        _loggerMock = new Mock<ILogger<ClearinghouseCredentialValidationService>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        SetupConfiguration();
        SetupKeyVaultSecrets();
    }

    private void SetupConfiguration()
    {
        _configurationMock.Setup(c => c["Clearinghouses:BaseUrl"]).Returns("BaseUrlSecretKey");
        _configurationMock.Setup(c => c["Clearinghouses:ValidationEndpoint"]).Returns("ValidationEndpointSecretKey");
        _configurationMock.Setup(c => c["Clearinghouses:ApiKey"]).Returns("ApiKeySecretKey");
    }

    private void SetupKeyVaultSecrets()
    {
        _keyVaultProviderServiceMock.Setup(k => k.GetSecretAsync("BaseUrlSecretKey"))
            .ReturnsAsync(BaseUrl);
        _keyVaultProviderServiceMock.Setup(k => k.GetSecretAsync("ValidationEndpointSecretKey"))
            .ReturnsAsync(ValidationEndpoint);
        _keyVaultProviderServiceMock.Setup(k => k.GetSecretAsync("ApiKeySecretKey"))
            .ReturnsAsync(ApiKey);
    }

    private void SetupHttpClient(HttpResponseMessage response)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient("ClearingHouseService"))
            .Returns(httpClient);
    }

    private void SetupHttpClientWithException(Exception exception)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient("ClearingHouseService"))
            .Returns(httpClient);
    }

    private ClearinghouseCredentialValidationService CreateService()
    {
        return new ClearinghouseCredentialValidationService(
            _httpClientFactoryMock.Object,
            _configurationMock.Object,
            _keyVaultProviderServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ValidateAllClearinghousesAsync_WhenAllClearinghousesValid_ReturnsSuccessResult()
    {
        // Arrange
        var apiResponse = new
        {
            AllValid = true,
            TotalClearinghouses = 5,
            SuccessfulValidations = 5,
            FailedValidations = 0
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(apiResponse))
        };

        SetupHttpClient(response);
        var service = CreateService();

        // Act
        var result = await service.ValidateAllClearinghousesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.AllValid);
        Assert.Equal(5, result.TotalClearinghouses);
        Assert.Equal(5, result.SuccessfulValidations);
        Assert.Equal(0, result.FailedValidations);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAllClearinghousesAsync_WhenSomeValidationsFail_ReturnsPartialResult()
    {
        // Arrange
        var apiResponse = new
        {
            AllValid = false,
            TotalClearinghouses = 5,
            SuccessfulValidations = 3,
            FailedValidations = 2
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(apiResponse))
        };

        SetupHttpClient(response);
        var service = CreateService();

        // Act
        var result = await service.ValidateAllClearinghousesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.AllValid);
        Assert.Equal(5, result.TotalClearinghouses);
        Assert.Equal(3, result.SuccessfulValidations);
        Assert.Equal(2, result.FailedValidations);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAllClearinghousesAsync_WhenAllValidationsFail_ReturnsFailedResult()
    {
        // Arrange
        var apiResponse = new
        {
            AllValid = false,
            TotalClearinghouses = 3,
            SuccessfulValidations = 0,
            FailedValidations = 3
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(apiResponse))
        };

        SetupHttpClient(response);
        var service = CreateService();

        // Act
        var result = await service.ValidateAllClearinghousesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.AllValid);
        Assert.Equal(3, result.TotalClearinghouses);
        Assert.Equal(0, result.SuccessfulValidations);
        Assert.Equal(3, result.FailedValidations);
    }

    [Fact]
    public async Task ValidateAllClearinghousesAsync_WhenNoClearinghouses_ReturnsEmptyResult()
    {
        // Arrange
        var apiResponse = new
        {
            AllValid = true,
            TotalClearinghouses = 0,
            SuccessfulValidations = 0,
            FailedValidations = 0
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(apiResponse))
        };

        SetupHttpClient(response);
        var service = CreateService();

        // Act
        var result = await service.ValidateAllClearinghousesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.AllValid);
        Assert.Equal(0, result.TotalClearinghouses);
        Assert.Equal(0, result.SuccessfulValidations);
        Assert.Equal(0, result.FailedValidations);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ValidateAllClearinghousesAsync_WhenApiReturnsErrorStatus_ReturnsFailedResultWithErrorMessage(HttpStatusCode statusCode)
    {
        // Arrange
        var response = new HttpResponseMessage(statusCode);
        SetupHttpClient(response);
        var service = CreateService();

        // Act
        var result = await service.ValidateAllClearinghousesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.AllValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains(statusCode.ToString(), result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAllClearinghousesAsync_WhenHttpRequestException_ReturnsFailedResultWithConnectionError()
    {
        // Arrange
        var exception = new HttpRequestException("Connection refused");
        SetupHttpClientWithException(exception);
        var service = CreateService();

        // Act
        var result = await service.ValidateAllClearinghousesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.AllValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Failed to connect to ClearingHouse service", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAllClearinghousesAsync_WhenGeneralException_ReturnsFailedResultWithErrorMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Unexpected error occurred");
        SetupHttpClientWithException(exception);
        var service = CreateService();

        // Act
        var result = await service.ValidateAllClearinghousesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.AllValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Error during clearinghouse validation", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAllClearinghousesAsync_WhenApiReturnsNullResponse_HandlesGracefully()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null")
        };

        SetupHttpClient(response);
        var service = CreateService();

        // Act
        var result = await service.ValidateAllClearinghousesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.AllValid);
        Assert.Equal(0, result.TotalClearinghouses);
        Assert.Equal(0, result.SuccessfulValidations);
        Assert.Equal(0, result.FailedValidations);
    }

    [Fact]
    public async Task ValidateAllClearinghousesAsync_WhenApiReturnsEmptyContent_HandlesGracefully()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };

        SetupHttpClient(response);
        var service = CreateService();

        // Act
        var result = await service.ValidateAllClearinghousesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.AllValid);
        Assert.Equal(0, result.TotalClearinghouses);
    }

    [Fact]
    public async Task ValidateAllClearinghousesAsync_WhenTimeoutException_ReturnsFailedResult()
    {
        // Arrange
        var exception = new TaskCanceledException("Request timed out");
        SetupHttpClientWithException(exception);
        var service = CreateService();

        // Act
        var result = await service.ValidateAllClearinghousesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.AllValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Constructor_WhenBaseUrlConfigurationMissing_ThrowsArgumentNullException()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Clearinghouses:BaseUrl"]).Returns((string)null);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ClearinghouseCredentialValidationService(
                _httpClientFactoryMock.Object,
                configMock.Object,
                _keyVaultProviderServiceMock.Object,
                _loggerMock.Object));

        Assert.Contains("Clearinghouses:BaseUrl", exception.Message);
    }

    [Fact]
    public void Constructor_WhenValidationEndpointConfigurationMissing_ThrowsArgumentNullException()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Clearinghouses:BaseUrl"]).Returns("BaseUrlKey");
        configMock.Setup(c => c["Clearinghouses:ValidationEndpoint"]).Returns((string)null);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ClearinghouseCredentialValidationService(
                _httpClientFactoryMock.Object,
                configMock.Object,
                _keyVaultProviderServiceMock.Object,
                _loggerMock.Object));

        Assert.Contains("Clearinghouses:ValidationEndpoint", exception.Message);
    }

    [Fact]
    public void Constructor_WhenApiKeyConfigurationMissing_ThrowsArgumentNullException()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Clearinghouses:BaseUrl"]).Returns("BaseUrlKey");
        configMock.Setup(c => c["Clearinghouses:ValidationEndpoint"]).Returns("EndpointKey");
        configMock.Setup(c => c["Clearinghouses:ApiKey"]).Returns((string)null);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ClearinghouseCredentialValidationService(
                _httpClientFactoryMock.Object,
                configMock.Object,
                _keyVaultProviderServiceMock.Object,
                _loggerMock.Object));

        Assert.Contains("Clearinghouses:ApiKey", exception.Message);
    }

    [Fact]
    public async Task ValidateAllClearinghousesAsync_VerifiesCorrectEndpointIsCalled()
    {
        // Arrange
        var apiResponse = new
        {
            AllValid = true,
            TotalClearinghouses = 1,
            SuccessfulValidations = 1,
            FailedValidations = 0
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(apiResponse))
        };

        HttpRequestMessage capturedRequest = null;
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, token) => capturedRequest = req)
            .ReturnsAsync(response);

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient("ClearingHouseService"))
            .Returns(httpClient);

        var service = CreateService();

        // Act
        await service.ValidateAllClearinghousesAsync();

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Contains(ValidationEndpoint, capturedRequest.RequestUri?.ToString());
    }

    [Fact]
    public async Task ValidateAllClearinghousesAsync_LogsInformationOnStart()
    {
        // Arrange
        var apiResponse = new
        {
            AllValid = true,
            TotalClearinghouses = 1,
            SuccessfulValidations = 1,
            FailedValidations = 0
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(apiResponse))
        };

        SetupHttpClient(response);
        var service = CreateService();

        // Act
        await service.ValidateAllClearinghousesAsync();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Starting clearinghouse credentials validation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateAllClearinghousesAsync_LogsInformationOnSuccess()
    {
        // Arrange
        var apiResponse = new
        {
            AllValid = true,
            TotalClearinghouses = 5,
            SuccessfulValidations = 5,
            FailedValidations = 0
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(apiResponse))
        };

        SetupHttpClient(response);
        var service = CreateService();

        // Act
        await service.ValidateAllClearinghousesAsync();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Clearinghouse validation completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateAllClearinghousesAsync_LogsWarningOnApiError()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        SetupHttpClient(response);
        var service = CreateService();

        // Act
        await service.ValidateAllClearinghousesAsync();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Clearinghouse validation API call failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateAllClearinghousesAsync_LogsErrorOnHttpRequestException()
    {
        // Arrange
        var exception = new HttpRequestException("Connection refused");
        SetupHttpClientWithException(exception);
        var service = CreateService();

        // Act
        await service.ValidateAllClearinghousesAsync();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to connect to ClearingHouse service")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateAllClearinghousesAsync_LogsErrorOnGeneralException()
    {
        // Arrange
        var exception = new InvalidOperationException("Unexpected error");
        SetupHttpClientWithException(exception);
        var service = CreateService();

        // Act
        await service.ValidateAllClearinghousesAsync();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error during clearinghouse credential validation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateAllClearinghousesAsync_WithLargeClearinghouseCount_ReturnsCorrectResult()
    {
        // Arrange
        var apiResponse = new
        {
            AllValid = true,
            TotalClearinghouses = 1000,
            SuccessfulValidations = 1000,
            FailedValidations = 0
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(apiResponse))
        };

        SetupHttpClient(response);
        var service = CreateService();

        // Act
        var result = await service.ValidateAllClearinghousesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.AllValid);
        Assert.Equal(1000, result.TotalClearinghouses);
        Assert.Equal(1000, result.SuccessfulValidations);
        Assert.Equal(0, result.FailedValidations);
    }

    [Fact]
    public async Task ValidateAllClearinghousesAsync_WithCaseInsensitivePropertyNames_DeserializesCorrectly()
    {
        // Arrange - using lowercase property names to test case insensitivity
        var jsonResponse = "{\"allvalid\":true,\"totalclearinghouses\":3,\"successfulvalidations\":2,\"failedvalidations\":1}";

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse)
        };

        SetupHttpClient(response);
        var service = CreateService();

        // Act
        var result = await service.ValidateAllClearinghousesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.AllValid);
        Assert.Equal(3, result.TotalClearinghouses);
        Assert.Equal(2, result.SuccessfulValidations);
        Assert.Equal(1, result.FailedValidations);
    }
}
