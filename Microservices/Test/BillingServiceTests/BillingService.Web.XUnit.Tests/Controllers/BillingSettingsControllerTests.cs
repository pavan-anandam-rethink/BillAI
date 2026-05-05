using BillingService.Domain.Interfaces.BillingSettings;
using BillingService.Domain.Models.BillingSettings;
using BillingService.Web.Controllers.BillingSettings;
using BillingService.Web.Helpers.HttpClients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using BillingService.Domain.Models;

namespace BillingService.Web.XUnit.Tests.Controllers
{
    public class BillingSettingsControllerTests
    {
        private readonly Mock<IBillingSettingsService> _billingSettingsServiceMock;
        private readonly Mock<IBaseHttpClient> _httpClientMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ILogger<BillingSettingsController>> _loggerMock;
        private readonly BillingSettingsController _controller;

        public BillingSettingsControllerTests()
        {
            _billingSettingsServiceMock = new Mock<IBillingSettingsService>();
            _httpClientMock = new Mock<IBaseHttpClient>();
            _configurationMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<BillingSettingsController>>();

            _controller = new BillingSettingsController(
            _httpClientMock.Object,
            _configurationMock.Object,
            _billingSettingsServiceMock.Object,
            _loggerMock.Object);
        }

        #region GetFeatures Tests

        [Fact]
        public async Task GetFeatures_ReturnsOk_WithFeatureList_WhenValidAccountId()
        {
            // Arrange
            var accountId = 100;
            var expectedFeatures = new List<FeatureStatusDto>
        {
            new FeatureStatusDto { FeatureId = 1, FeatureName = "PatientInvoice", IsEnabled = true },
            new FeatureStatusDto { FeatureId = 2, FeatureName = "RevSpring", IsEnabled = false }
        };

            _billingSettingsServiceMock
              .Setup(s => s.GetFeaturesForAccountAsync(accountId))
              .ReturnsAsync(expectedFeatures);

            // Act
            var result = await _controller.GetBillingFeatures(accountId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualFeatures = Assert.IsType<List<FeatureStatusDto>>(okResult.Value);
            Assert.Equal(2, actualFeatures.Count);
            Assert.Equal(expectedFeatures, actualFeatures);

            _billingSettingsServiceMock.Verify(s => s.GetFeaturesForAccountAsync(accountId), Times.Once);
        }

        [Fact]
        public async Task GetFeatures_ReturnsOk_WithEmptyList_WhenNoFeaturesExist()
        {
            // Arrange
            var accountId = 100;
            var expectedFeatures = new List<FeatureStatusDto>();

            _billingSettingsServiceMock
             .Setup(s => s.GetFeaturesForAccountAsync(accountId))
             .ReturnsAsync(expectedFeatures);

            // Act
            var result = await _controller.GetBillingFeatures(accountId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualFeatures = Assert.IsType<List<FeatureStatusDto>>(okResult.Value);
            Assert.Empty(actualFeatures);

            _billingSettingsServiceMock.Verify(s => s.GetFeaturesForAccountAsync(accountId), Times.Once);
        }

        [Fact]
        public async Task GetFeatures_ReturnsBadRequest_WhenAccountIdIsNull()
        {
            // Arrange
            int? accountId = null;

            // Act
            var result = await _controller.GetBillingFeatures(accountId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("accountId is required and must be greater than zero.", badRequestResult.Value);

            _billingSettingsServiceMock.Verify(s => s.GetFeaturesForAccountAsync(It.IsAny<int>()),Times.Never);
        }

        [Fact]
        public async Task GetFeatures_ReturnsBadRequest_WhenAccountIdIsZero()
        {
            // Arrange
            int? accountId = 0;

            // Act
            var result = await _controller.GetBillingFeatures(accountId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("accountId is required and must be greater than zero.", badRequestResult.Value);

            _billingSettingsServiceMock.Verify(s => s.GetFeaturesForAccountAsync(It.IsAny<int>()),Times.Never);
        }

        [Fact]
        public async Task GetFeatures_ReturnsBadRequest_WhenAccountIdIsNegative()
        {
            // Arrange
            int? accountId = -5;

            // Act
            var result = await _controller.GetBillingFeatures(accountId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("accountId is required and must be greater than zero.", badRequestResult.Value);

            _billingSettingsServiceMock.Verify(
                s => s.GetFeaturesForAccountAsync(It.IsAny<int>()),
                Times.Never);
        }

        
        [Fact]
        public async Task GetFeatures_ReturnsStatusCode500_WhenUnhandledExceptionThrown()
        {
            // Arrange
            var accountId = 100;

            _billingSettingsServiceMock
           .Setup(s => s.GetFeaturesForAccountAsync(accountId))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _controller.GetBillingFeatures(accountId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An unexpected error occurred.", statusCodeResult.Value);

            _billingSettingsServiceMock.Verify(s => s.GetFeaturesForAccountAsync(accountId), Times.Once);

            _loggerMock.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("Unhandled error in GetFeatures")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetFeatures_LogsInformation_WhenCalledWithValidAccountId()
        {
            // Arrange
            var accountId = 100;
            var expectedFeatures = new List<FeatureStatusDto>
        {
            new FeatureStatusDto { FeatureId = 1, FeatureName = "Feature1", IsEnabled = true }
        };

            _billingSettingsServiceMock
             .Setup(s => s.GetFeaturesForAccountAsync(accountId))
            .ReturnsAsync(expectedFeatures);

            // Act
            await _controller.GetBillingFeatures(accountId);

            // Write Assert SIMPLE ASSERTS
            _loggerMock.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("GetBillingFeatures called")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

        }

        [Fact]
        public async Task GetFeatures_ReturnsOk_WithCorrectFeatureData()
        {
            // Arrange
            var accountId = 100;
            var expectedFeatures = new List<FeatureStatusDto>
        {
             new FeatureStatusDto { FeatureId = 1, FeatureName = "PatientInvoice", IsEnabled = true },
             new FeatureStatusDto { FeatureId = 2, FeatureName = "RevSpring", IsEnabled = false },
             new FeatureStatusDto { FeatureId = 3, FeatureName = "AutoClaim", IsEnabled = true }
        };

            _billingSettingsServiceMock
                .Setup(s => s.GetFeaturesForAccountAsync(accountId))
                 .ReturnsAsync(expectedFeatures);

            // Act
            var result = await _controller.GetBillingFeatures(accountId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualFeatures = Assert.IsType<List<FeatureStatusDto>>(okResult.Value);

            Assert.Equal(3, actualFeatures.Count);

            Assert.Equal(1, actualFeatures[0].FeatureId);
            Assert.Equal("PatientInvoice", actualFeatures[0].FeatureName);
            Assert.True(actualFeatures[0].IsEnabled);

            Assert.Equal(2, actualFeatures[1].FeatureId);
            Assert.Equal("RevSpring", actualFeatures[1].FeatureName);
            Assert.False(actualFeatures[1].IsEnabled);

            Assert.Equal(3, actualFeatures[2].FeatureId);
            Assert.Equal("AutoClaim", actualFeatures[2].FeatureName);
            Assert.True(actualFeatures[2].IsEnabled);
        }

        [Fact]
        public async Task GetFeatures_DoesNotLogWarningOrError_WhenSuccessful()
        {
            // Arrange
            var accountId = 100;
            var expectedFeatures = new List<FeatureStatusDto>
            {
               new FeatureStatusDto { FeatureId = 1, FeatureName = "Feature1", IsEnabled = true }
            };

            _billingSettingsServiceMock
            .Setup(s => s.GetFeaturesForAccountAsync(accountId))
                .ReturnsAsync(expectedFeatures);

            // Act
            await _controller.GetBillingFeatures(accountId);

            // Assert - Warning should never be logged on success
            _loggerMock.Verify(l => l.Log(LogLevel.Warning,It.IsAny<EventId>(),It.IsAny<It.IsAnyType>(),It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                    Times.Never);

            // Assert - Error should never be logged on success
            _loggerMock.Verify(l => l.Log(LogLevel.Error,It.IsAny<EventId>(),It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                    Times.Never);
        }

        [Fact]
        public async Task GetFeatures_DoesNotCallService_WhenValidationFails()
        {
            // Arrange & Act - null accountId
            await _controller.GetBillingFeatures(null);
            await _controller.GetBillingFeatures(0);
            await _controller.GetBillingFeatures(-1);

            // Assert - Service should never be called for invalid inputs
            _billingSettingsServiceMock.Verify(
                   s => s.GetFeaturesForAccountAsync(It.IsAny<int>()),
               Times.Never);
        }

        [Fact]
        public async Task GetFeatures_ReturnsNotFound_DoesNotExposeInternalError_WhenKeyNotFound()
        {
            // Arrange
            var accountId = 999;

            _billingSettingsServiceMock
           .Setup(s => s.GetFeaturesForAccountAsync(accountId))
                .ThrowsAsync(new KeyNotFoundException("Account with ID 999 not found."));

            // Act
            var result = await _controller.GetBillingFeatures(accountId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
            Assert.IsType<string>(notFoundResult.Value);
        }

        [Fact]
        public async Task GetFeatures_ReturnsStatusCode500_DoesNotExposeInternalError_WhenUnhandledException()
        {
            // Arrange
            var accountId = 100;

            _billingSettingsServiceMock
                .Setup(s => s.GetFeaturesForAccountAsync(accountId))
               .ThrowsAsync(new InvalidOperationException("SQL timeout occurred"));

            // Act
            var result = await _controller.GetBillingFeatures(accountId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            // Should NOT expose actual exception message
            Assert.Equal("An unexpected error occurred.", statusCodeResult.Value);
            Assert.DoesNotContain("SQL timeout", statusCodeResult.Value.ToString());
        }

        [Fact]
        public async Task GetFeatures_ReturnsNotFound_WhenKeyNotFoundExceptionThrown()
        {
            // Arrange
            var accountId = 999;
            var exceptionMessage = "Account with ID 999 not found.";

            _billingSettingsServiceMock
                .Setup(s => s.GetFeaturesForAccountAsync(accountId))
                .ThrowsAsync(new KeyNotFoundException(exceptionMessage));

            // Act
            var result = await _controller.GetBillingFeatures(accountId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(exceptionMessage, notFoundResult.Value);

            _billingSettingsServiceMock.Verify(s => s.GetFeaturesForAccountAsync(accountId), Times.Once);

            _loggerMock.Verify(l => l.Log(LogLevel.Warning, It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) =>
                        v.ToString().Contains("Account not found")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                        Times.Once);
        }
        #endregion

        [Fact]
        public async Task GetBillingSettingInformation_InvalidAccountId_ReturnsBadRequest()
        {
            // Arrange
            int invalidAccountId = 0; // or any value <= 0

            // Act
            var result = await _controller.GetBillingSettingInformation(invalidAccountId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("accountId is required and must be greater than zero.", badRequestResult.Value);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once); // Ensure the warning was logged
        }

        [Fact]
        public async Task GetBillingSettingInformation_ValidAccountId_ReturnsOk()
        {
            // Arrange
            int validAccountId = 123;
            var expectedSettings = new BillingSettingInformationModel()
            {
                // Populate this with expected properties (e.g., invoice settings, statement settings)
            };

            _billingSettingsServiceMock
                .Setup(s => s.GetBillingSettingInformationAsync(validAccountId))
                .ReturnsAsync(expectedSettings);

            // Act
            var result = await _controller.GetBillingSettingInformation(validAccountId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedSettings = Assert.IsType<BillingSettingInformationModel>(okResult.Value);
            Assert.Equal(expectedSettings, returnedSettings);

        }

        [Fact]
        public async Task GetDefaultBilling_InvalidAccountId_ReturnsBadRequest()
        {
            // Arrange
            int invalidAccountId = 0;

            // Act
            var result = await _controller.GetDefaultBilling(invalidAccountId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid accountId", badRequestResult.Value);
        }

        [Fact]
        public async Task GetDefaultBilling_ValidAccountId_ReturnsOk()
        {
            // Arrange
            int validAccountId = 123;

            var expectedResult = new BillingSettingInformationModel()
            {
               
            };

            _billingSettingsServiceMock
                .Setup(s => s.GetDefaultBillingFromMainLocationAsync(validAccountId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetDefaultBilling(validAccountId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedResult = Assert.IsType<BillingSettingInformationModel>(okResult.Value);
            Assert.Equal(expectedResult, returnedResult);
        }

        [Fact]
        public async Task SaveBillingSettings_InvalidAccountId_ReturnsBadRequest_AndLogsWarning()
        {
            // Arrange
            var request = new SaveBillingSettingRequest
            {
                AccountId = 0,
                CompanyName = "Acme"
            };
            var memberId = 42;
            // Act
            var result = await _controller.SaveBillingSettings(request, memberId);
            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("accountId is required and must be greater than zero.", badRequestResult.Value);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task SaveBillingSettings_InvalidModelState_ReturnsBadRequest_WithModelState()
        {
            // Arrange
            var request = new SaveBillingSettingRequest
            {
                AccountId = 123,
                CompanyName = null
            };
            var memberId = 1;

            _controller.ModelState.AddModelError("CompanyName", "The CompanyName field is required.");
            // Act
            var result = await _controller.SaveBillingSettings(request, memberId);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            //Assert
            var errors = Assert.IsType<SerializableError>(badRequestResult.Value);
            Assert.True(errors.ContainsKey("CompanyName"));
            var messages = Assert.IsType<string[]>(errors["CompanyName"]);
            Assert.Contains("The CompanyName field is required.", messages);
            _billingSettingsServiceMock.Verify(
                s => s.SaveBillingSettingInformationAsync(It.IsAny<SaveBillingSettingRequest>(), It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public async Task SaveBillingSettings_ServiceReturnsFailResult_ReturnsBadRequestWithError()
        {
            // Arrange
            var request = new SaveBillingSettingRequest
            {
                AccountId = 123,
                CompanyName = "Acme"
            };
            var memberId = 7;
            var errorMessage = "Failed to save settings.";
            var failResponse = ActionResponse.FailResult(errorMessage);
            _billingSettingsServiceMock
                .Setup(s => s.SaveBillingSettingInformationAsync(request, memberId))
                .ReturnsAsync(failResponse);
            // Act
            var result = await _controller.SaveBillingSettings(request, memberId);
            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(errorMessage, badRequestResult.Value);
            _billingSettingsServiceMock.Verify(s => s.SaveBillingSettingInformationAsync(request, memberId), Times.Once);
        }

        [Fact]
        public async Task SaveBillingSettings_ServiceReturnsSuccess_ReturnsOkWithResponse()
        {
            // Arrange
            var request = new SaveBillingSettingRequest
            {
                AccountId = 999,
                CompanyName = "Contoso"
            };
            var memberId = 99;
            var successResponse = ActionResponse.SuccessResult(new { Saved = true });
            _billingSettingsServiceMock
                .Setup(s => s.SaveBillingSettingInformationAsync(request, memberId))
                .ReturnsAsync(successResponse);
            // Act
            var result = await _controller.SaveBillingSettings(request, memberId);
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<ActionResponse>(okResult.Value);
            Assert.True(returned.Success);
            _billingSettingsServiceMock.Verify(s => s.SaveBillingSettingInformationAsync(request, memberId), Times.Once);
        }

        [Fact]
        public async Task SaveBillingSettings_ServiceThrowsException_ReturnsStatusCode500_AndLogsError()
        {
            // Arrange
            var request = new SaveBillingSettingRequest
            {
                AccountId = 555,
                CompanyName = "ErrorCo"
            };
            var memberId = 5;
            _billingSettingsServiceMock
                .Setup(s => s.SaveBillingSettingInformationAsync(request, memberId))
                .ThrowsAsync(new Exception("DB down"));
            // Act
            var result = await _controller.SaveBillingSettings(request, memberId);
            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An unexpected error occurred.", statusCodeResult.Value);
            _billingSettingsServiceMock.Verify(s => s.SaveBillingSettingInformationAsync(request, memberId), Times.Once);
            _loggerMock.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("Error saving/updating billing settings")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}
