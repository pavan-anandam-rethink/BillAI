using ClearingHouseService.Web.Interface;
using ClearingHouseService.Web.Service;
using ClearingHouseService.Web.Service.Handler;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Interfaces;

namespace ClearingHouse
{
    public class ClaimSubmissionHandlerTest
    {
        private readonly Mock<IClaimRepository> _mockClaimRepository;
        private readonly Mock<ILogger<ClaimSubmissionHandler>> _mockLogger;
        private readonly ClaimSubmissionHandler _handler;

        public ClaimSubmissionHandlerTest()
        {
            _mockClaimRepository = new Mock<IClaimRepository>();
            _mockLogger = new Mock<ILogger<ClaimSubmissionHandler>>();
            _handler = new ClaimSubmissionHandler(
                _mockClaimRepository.Object,
                _mockLogger.Object
            );
        }

        #region Success Scenarios

        [Fact]
        public async Task HandleUploadResultAsync_WhenUploadSucceeds_ShouldUpdateStatusToPending()
        {
            // Arrange
            var claimId = 123;
            var result = OperationResult.Success("test-file.edi");

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    claimId,
                    ClaimStatus.Pending,
                    null))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockClaimRepository.Verify(x => x.UpdateClaimDetailsAsync(
                claimId,
                ClaimStatus.Pending,
                null), Times.Once);

            _mockClaimRepository.Verify(x => x.SaveClaimValidationErrorAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<ClaimErrorSource>()), Times.Never);
        }

        [Fact]
        public async Task HandleUploadResultAsync_WhenUploadSucceeds_ShouldLogInformation()
        {
            // Arrange
            var claimId = 456;
            var result = OperationResult.Success("uploaded-file.edi");

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    It.IsAny<int>(),
                    It.IsAny<ClaimStatus>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("successfully uploaded") && v.ToString()!.Contains(claimId.ToString())),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Failure Scenarios - AuthFailure

        [Fact]
        public async Task HandleUploadResultAsync_WhenAuthFailure_ShouldUpdateStatusToSubmissionFailed()
        {
            // Arrange
            var claimId = 789;
            var result = OperationResult.Fail(ErrorType.AuthFailure, "Authentication failed");
            var expectedErrorMessage = "Clearing House - Authentication failure";

            _mockClaimRepository.Setup(x => x.GetErrorMessageAsync(ClaimErrorNumber.ClearingHouseAuthenticationFailure))
                .ReturnsAsync((string?)null);

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    claimId,
                    ClaimStatus.SubmissionFailed,
                    expectedErrorMessage))
                .Returns(Task.CompletedTask);

            _mockClaimRepository.Setup(x => x.GetErrorMessageIdAsync(ClaimErrorNumber.ClearingHouseAuthenticationFailure))
                .ReturnsAsync((int?)null);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockClaimRepository.Verify(x => x.UpdateClaimDetailsAsync(
                claimId,
                ClaimStatus.SubmissionFailed,
                expectedErrorMessage), Times.Once);
        }

        [Fact]
        public async Task HandleUploadResultAsync_WhenAuthFailure_WithDatabaseErrorMessage_ShouldUseDbMessage()
        {
            // Arrange
            var claimId = 100;
            var result = OperationResult.Fail(ErrorType.AuthFailure, "Auth error");
            var dbErrorMessage = "Database error message for auth failure";

            _mockClaimRepository.Setup(x => x.GetErrorMessageAsync(ClaimErrorNumber.ClearingHouseAuthenticationFailure))
                .ReturnsAsync(dbErrorMessage);

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    claimId,
                    ClaimStatus.SubmissionFailed,
                    dbErrorMessage))
                .Returns(Task.CompletedTask);

            _mockClaimRepository.Setup(x => x.GetErrorMessageIdAsync(ClaimErrorNumber.ClearingHouseAuthenticationFailure))
                .ReturnsAsync((int?)null);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockClaimRepository.Verify(x => x.UpdateClaimDetailsAsync(
                claimId,
                ClaimStatus.SubmissionFailed,
                dbErrorMessage), Times.Once);
        }

        #endregion

        #region Failure Scenarios - ConnectionFailure

        [Fact]
        public async Task HandleUploadResultAsync_WhenConnectionFailure_ShouldMapToConnectionIssueErrorNumber()
        {
            // Arrange
            var claimId = 200;
            var result = OperationResult.Fail(ErrorType.ConnectionFailure, "Connection refused");

            _mockClaimRepository.Setup(x => x.GetErrorMessageAsync(ClaimErrorNumber.ClearingHouseConnectionIssue))
                .ReturnsAsync((string?)null);

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    It.IsAny<int>(),
                    It.IsAny<ClaimStatus>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockClaimRepository.Setup(x => x.GetErrorMessageIdAsync(ClaimErrorNumber.ClearingHouseConnectionIssue))
                .ReturnsAsync((int?)null);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockClaimRepository.Verify(x => x.GetErrorMessageAsync(ClaimErrorNumber.ClearingHouseConnectionIssue), Times.Once);
        }

        #endregion

        #region Failure Scenarios - Timeout

        [Fact]
        public async Task HandleUploadResultAsync_WhenTimeout_ShouldMapToConnectionIssueErrorNumber()
        {
            // Arrange
            var claimId = 300;
            var result = OperationResult.Fail(ErrorType.Timeout, "Request timed out");

            _mockClaimRepository.Setup(x => x.GetErrorMessageAsync(ClaimErrorNumber.ClearingHouseConnectionIssue))
                .ReturnsAsync((string?)null);

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    It.IsAny<int>(),
                    It.IsAny<ClaimStatus>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockClaimRepository.Setup(x => x.GetErrorMessageIdAsync(ClaimErrorNumber.ClearingHouseConnectionIssue))
                .ReturnsAsync((int?)null);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockClaimRepository.Verify(x => x.GetErrorMessageAsync(ClaimErrorNumber.ClearingHouseConnectionIssue), Times.Once);
        }

        #endregion

        #region Failure Scenarios - UploadFailed

        [Fact]
        public async Task HandleUploadResultAsync_WhenUploadFailed_ShouldMapToUploadFailedErrorNumber()
        {
            // Arrange
            var claimId = 400;
            var result = OperationResult.Fail(ErrorType.UploadFailed, "SFTP upload failed");

            _mockClaimRepository.Setup(x => x.GetErrorMessageAsync(ClaimErrorNumber.ClearingHouseUploadFailed))
                .ReturnsAsync((string?)null);

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    It.IsAny<int>(),
                    It.IsAny<ClaimStatus>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockClaimRepository.Setup(x => x.GetErrorMessageIdAsync(ClaimErrorNumber.ClearingHouseUploadFailed))
                .ReturnsAsync((int?)null);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockClaimRepository.Verify(x => x.GetErrorMessageAsync(ClaimErrorNumber.ClearingHouseUploadFailed), Times.Once);
        }

        #endregion

        #region Failure Scenarios - FileGenerationFailed

        [Fact]
        public async Task HandleUploadResultAsync_WhenFileGenerationFailed_ShouldMapToUploadFailedErrorNumber()
        {
            // Arrange
            var claimId = 500;
            var result = OperationResult.Fail(ErrorType.FileGenerationFailed, "EDI file generation failed");

            _mockClaimRepository.Setup(x => x.GetErrorMessageAsync(ClaimErrorNumber.ClearingHouseUploadFailed))
                .ReturnsAsync((string?)null);

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    It.IsAny<int>(),
                    It.IsAny<ClaimStatus>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockClaimRepository.Setup(x => x.GetErrorMessageIdAsync(ClaimErrorNumber.ClearingHouseUploadFailed))
                .ReturnsAsync((int?)null);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockClaimRepository.Verify(x => x.GetErrorMessageAsync(ClaimErrorNumber.ClearingHouseUploadFailed), Times.Once);
        }

        #endregion

        #region Failure Scenarios - InvalidClearingHouseConfig

        [Fact]
        public async Task HandleUploadResultAsync_WhenInvalidConfig_ShouldMapToDetailsMissingErrorNumber()
        {
            // Arrange
            var claimId = 600;
            var result = OperationResult.Fail(ErrorType.InvalidClearingHouseConfig, "Missing SFTP credentials");

            _mockClaimRepository.Setup(x => x.GetErrorMessageAsync(ClaimErrorNumber.ClearingHouseDetailsMissing))
                .ReturnsAsync((string?)null);

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    It.IsAny<int>(),
                    It.IsAny<ClaimStatus>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockClaimRepository.Setup(x => x.GetErrorMessageIdAsync(ClaimErrorNumber.ClearingHouseDetailsMissing))
                .ReturnsAsync((int?)null);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockClaimRepository.Verify(x => x.GetErrorMessageAsync(ClaimErrorNumber.ClearingHouseDetailsMissing), Times.Once);
        }

        #endregion

        #region Failure Scenarios - ClaimNotFound

        [Fact]
        public async Task HandleUploadResultAsync_WhenClaimNotFound_ShouldMapToUnknownErrorNumber()
        {
            // Arrange
            var claimId = 700;
            var result = OperationResult.Fail(ErrorType.ClaimNotFound, "Claim not found in database");

            _mockClaimRepository.Setup(x => x.GetErrorMessageAsync(ClaimErrorNumber.Unknown))
                .ReturnsAsync((string?)null);

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    It.IsAny<int>(),
                    It.IsAny<ClaimStatus>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockClaimRepository.Setup(x => x.GetErrorMessageIdAsync(ClaimErrorNumber.Unknown))
                .ReturnsAsync((int?)null);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockClaimRepository.Verify(x => x.GetErrorMessageAsync(ClaimErrorNumber.Unknown), Times.Once);
        }

        #endregion

        #region Failure Scenarios - ValidationFailed

        [Fact]
        public async Task HandleUploadResultAsync_WhenValidationFailed_ShouldMapToUploadFailedErrorNumber()
        {
            // Arrange
            var claimId = 800;
            var result = OperationResult.Fail(ErrorType.ValidationFailed, "EDI validation failed");

            _mockClaimRepository.Setup(x => x.GetErrorMessageAsync(ClaimErrorNumber.ClearingHouseUploadFailed))
                .ReturnsAsync((string?)null);

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    It.IsAny<int>(),
                    It.IsAny<ClaimStatus>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockClaimRepository.Setup(x => x.GetErrorMessageIdAsync(ClaimErrorNumber.ClearingHouseUploadFailed))
                .ReturnsAsync((int?)null);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockClaimRepository.Verify(x => x.GetErrorMessageAsync(ClaimErrorNumber.ClearingHouseUploadFailed), Times.Once);
        }

        #endregion

        #region Failure Scenarios - Unknown Error Type

        [Fact]
        public async Task HandleUploadResultAsync_WhenUnknownErrorType_ShouldMapToUploadFailedErrorNumber()
        {
            // Arrange
            var claimId = 900;
            var result = OperationResult.Fail(ErrorType.Unknown, "Unknown error occurred");

            _mockClaimRepository.Setup(x => x.GetErrorMessageAsync(ClaimErrorNumber.ClearingHouseUploadFailed))
                .ReturnsAsync((string?)null);

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    It.IsAny<int>(),
                    It.IsAny<ClaimStatus>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockClaimRepository.Setup(x => x.GetErrorMessageIdAsync(ClaimErrorNumber.ClearingHouseUploadFailed))
                .ReturnsAsync((int?)null);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockClaimRepository.Verify(x => x.GetErrorMessageAsync(ClaimErrorNumber.ClearingHouseUploadFailed), Times.Once);
        }

        #endregion

        #region Claim Validation Error Saving

        [Fact]
        public async Task HandleUploadResultAsync_WhenFailure_ShouldSaveClaimValidationError()
        {
            // Arrange
            var claimId = 1000;
            var errorMessageId = 5001;
            var claimSubmissionId = 9999;
            var result = OperationResult.Fail(ErrorType.UploadFailed, "Upload failed");
            var errorMessage = "Clearing House - Upload failed";

            _mockClaimRepository.Setup(x => x.GetErrorMessageAsync(ClaimErrorNumber.ClearingHouseUploadFailed))
                .ReturnsAsync((string?)null);

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    It.IsAny<int>(),
                    It.IsAny<ClaimStatus>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockClaimRepository.Setup(x => x.GetErrorMessageIdAsync(ClaimErrorNumber.ClearingHouseUploadFailed))
                .ReturnsAsync(errorMessageId);

            _mockClaimRepository.Setup(x => x.GetLatestClaimSubmissionIdAsync(claimId))
                .ReturnsAsync(claimSubmissionId);

            _mockClaimRepository.Setup(x => x.SaveClaimValidationErrorAsync(
                    claimId,
                    claimSubmissionId,
                    errorMessageId,
                    errorMessage,
                    ClaimErrorSource.Billing))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockClaimRepository.Verify(x => x.SaveClaimValidationErrorAsync(
                claimId,
                claimSubmissionId,
                errorMessageId,
                errorMessage,
                ClaimErrorSource.Billing), Times.Once);
        }

        [Fact]
        public async Task HandleUploadResultAsync_WhenErrorMessageIdNotFound_ShouldNotSaveValidationError()
        {
            // Arrange
            var claimId = 1100;
            var result = OperationResult.Fail(ErrorType.UploadFailed, "Upload failed");

            _mockClaimRepository.Setup(x => x.GetErrorMessageAsync(ClaimErrorNumber.ClearingHouseUploadFailed))
                .ReturnsAsync((string?)null);

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    It.IsAny<int>(),
                    It.IsAny<ClaimStatus>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockClaimRepository.Setup(x => x.GetErrorMessageIdAsync(ClaimErrorNumber.ClearingHouseUploadFailed))
                .ReturnsAsync((int?)null);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockClaimRepository.Verify(x => x.SaveClaimValidationErrorAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<ClaimErrorSource>()), Times.Never);
        }

        [Fact]
        public async Task HandleUploadResultAsync_WhenNoClaimSubmission_ShouldUseZeroAsSubmissionId()
        {
            // Arrange
            var claimId = 1200;
            var errorMessageId = 5002;
            var result = OperationResult.Fail(ErrorType.ConnectionFailure, "Connection error");
            var errorMessage = "Clearing House - Connection issue";

            _mockClaimRepository.Setup(x => x.GetErrorMessageAsync(ClaimErrorNumber.ClearingHouseConnectionIssue))
                .ReturnsAsync((string?)null);

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    It.IsAny<int>(),
                    It.IsAny<ClaimStatus>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockClaimRepository.Setup(x => x.GetErrorMessageIdAsync(ClaimErrorNumber.ClearingHouseConnectionIssue))
                .ReturnsAsync(errorMessageId);

            _mockClaimRepository.Setup(x => x.GetLatestClaimSubmissionIdAsync(claimId))
                .ReturnsAsync((int?)null);

            _mockClaimRepository.Setup(x => x.SaveClaimValidationErrorAsync(
                    claimId,
                    0,
                    errorMessageId,
                    errorMessage,
                    ClaimErrorSource.Billing))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockClaimRepository.Verify(x => x.SaveClaimValidationErrorAsync(
                claimId,
                0,
                errorMessageId,
                errorMessage,
                ClaimErrorSource.Billing), Times.Once);
        }

        [Fact]
        public async Task HandleUploadResultAsync_WhenSaveValidationErrorThrows_ShouldLogErrorAndContinue()
        {
            // Arrange
            var claimId = 1300;
            var errorMessageId = 5003;
            var result = OperationResult.Fail(ErrorType.Timeout, "Timeout occurred");

            _mockClaimRepository.Setup(x => x.GetErrorMessageAsync(ClaimErrorNumber.ClearingHouseConnectionIssue))
                .ReturnsAsync((string?)null);

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    It.IsAny<int>(),
                    It.IsAny<ClaimStatus>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockClaimRepository.Setup(x => x.GetErrorMessageIdAsync(ClaimErrorNumber.ClearingHouseConnectionIssue))
                .ReturnsAsync(errorMessageId);

            _mockClaimRepository.Setup(x => x.GetLatestClaimSubmissionIdAsync(claimId))
                .ReturnsAsync(1);

            _mockClaimRepository.Setup(x => x.SaveClaimValidationErrorAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<ClaimErrorSource>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert - should not throw
            await _handler.HandleUploadResultAsync(claimId, result);

            // Verify that error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to save claim validation error")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Logging Verification

        [Fact]
        public async Task HandleUploadResultAsync_WhenFailure_ShouldLogWarning()
        {
            // Arrange
            var claimId = 1400;
            var result = OperationResult.Fail(ErrorType.UploadFailed, "Upload failed message");

            _mockClaimRepository.Setup(x => x.GetErrorMessageAsync(It.IsAny<ClaimErrorNumber>()))
                .ReturnsAsync((string?)null);

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    It.IsAny<int>(),
                    It.IsAny<ClaimStatus>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockClaimRepository.Setup(x => x.GetErrorMessageIdAsync(It.IsAny<ClaimErrorNumber>()))
                .ReturnsAsync((int?)null);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("upload failed") && v.ToString()!.Contains(claimId.ToString())),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Default Error Messages

        [Theory]
        [InlineData(ErrorType.AuthFailure, "Clearing House - Authentication failure")]
        [InlineData(ErrorType.ConnectionFailure, "Clearing House - Connection issue")]
        [InlineData(ErrorType.Timeout, "Clearing House - Connection issue")]
        [InlineData(ErrorType.UploadFailed, "Clearing House - Upload failed")]
        [InlineData(ErrorType.FileGenerationFailed, "Clearing House - Upload failed")]
        [InlineData(ErrorType.InvalidClearingHouseConfig, "Clearing House - Configuration missing")]
        [InlineData(ErrorType.ClaimNotFound, "Claim not found")]
        [InlineData(ErrorType.ValidationFailed, "Clearing House - Upload failed")]
        public async Task HandleUploadResultAsync_WhenDbMessageIsNull_ShouldUseDefaultMessage(
            ErrorType errorType,
            string expectedDefaultMessage)
        {
            // Arrange
            var claimId = 1500;
            var result = OperationResult.Fail(errorType, "Original error message");

            _mockClaimRepository.Setup(x => x.GetErrorMessageAsync(It.IsAny<ClaimErrorNumber>()))
                .ReturnsAsync((string?)null);

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    claimId,
                    ClaimStatus.SubmissionFailed,
                    expectedDefaultMessage))
                .Returns(Task.CompletedTask);

            _mockClaimRepository.Setup(x => x.GetErrorMessageIdAsync(It.IsAny<ClaimErrorNumber>()))
                .ReturnsAsync((int?)null);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockClaimRepository.Verify(x => x.UpdateClaimDetailsAsync(
                claimId,
                ClaimStatus.SubmissionFailed,
                expectedDefaultMessage), Times.Once);
        }

        [Fact]
        public async Task HandleUploadResultAsync_WhenUnknownErrorAndNoMessage_ShouldUseOriginalMessage()
        {
            // Arrange
            var claimId = 1600;
            var originalMessage = "Custom error message from source";
            var result = OperationResult.Fail(ErrorType.Unknown, originalMessage);

            _mockClaimRepository.Setup(x => x.GetErrorMessageAsync(ClaimErrorNumber.ClearingHouseUploadFailed))
                .ReturnsAsync((string?)null);

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    claimId,
                    ClaimStatus.SubmissionFailed,
                    originalMessage))
                .Returns(Task.CompletedTask);

            _mockClaimRepository.Setup(x => x.GetErrorMessageIdAsync(ClaimErrorNumber.ClearingHouseUploadFailed))
                .ReturnsAsync((int?)null);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockClaimRepository.Verify(x => x.UpdateClaimDetailsAsync(
                claimId,
                ClaimStatus.SubmissionFailed,
                originalMessage), Times.Once);
        }

        [Fact]
        public async Task HandleUploadResultAsync_WhenUnknownErrorAndNullMessage_ShouldUseFallbackMessage()
        {
            // Arrange
            var claimId = 1700;
            var result = OperationResult.Fail(ErrorType.Unknown, null!);

            _mockClaimRepository.Setup(x => x.GetErrorMessageAsync(ClaimErrorNumber.ClearingHouseUploadFailed))
                .ReturnsAsync((string?)null);

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    claimId,
                    ClaimStatus.SubmissionFailed,
                    "Clearing House - Upload failed"))
                .Returns(Task.CompletedTask);

            _mockClaimRepository.Setup(x => x.GetErrorMessageIdAsync(ClaimErrorNumber.ClearingHouseUploadFailed))
                .ReturnsAsync((int?)null);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockClaimRepository.Verify(x => x.UpdateClaimDetailsAsync(
                claimId,
                ClaimStatus.SubmissionFailed,
                "Clearing House - Upload failed"), Times.Once);
        }

        #endregion

        #region Error Type Mapping Verification

        [Theory]
        [InlineData(ErrorType.AuthFailure, ClaimErrorNumber.ClearingHouseAuthenticationFailure)]
        [InlineData(ErrorType.ConnectionFailure, ClaimErrorNumber.ClearingHouseConnectionIssue)]
        [InlineData(ErrorType.Timeout, ClaimErrorNumber.ClearingHouseConnectionIssue)]
        [InlineData(ErrorType.UploadFailed, ClaimErrorNumber.ClearingHouseUploadFailed)]
        [InlineData(ErrorType.FileGenerationFailed, ClaimErrorNumber.ClearingHouseUploadFailed)]
        [InlineData(ErrorType.InvalidClearingHouseConfig, ClaimErrorNumber.ClearingHouseDetailsMissing)]
        [InlineData(ErrorType.ClaimNotFound, ClaimErrorNumber.Unknown)]
        [InlineData(ErrorType.ValidationFailed, ClaimErrorNumber.ClearingHouseUploadFailed)]
        [InlineData(ErrorType.Unknown, ClaimErrorNumber.ClearingHouseUploadFailed)]
        public async Task HandleUploadResultAsync_ShouldMapErrorTypeToCorrectClaimErrorNumber(
            ErrorType errorType,
            ClaimErrorNumber expectedErrorNumber)
        {
            // Arrange
            var claimId = 1800;
            var result = OperationResult.Fail(errorType, "Error message");

            _mockClaimRepository.Setup(x => x.GetErrorMessageAsync(expectedErrorNumber))
                .ReturnsAsync((string?)null);

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    It.IsAny<int>(),
                    It.IsAny<ClaimStatus>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockClaimRepository.Setup(x => x.GetErrorMessageIdAsync(expectedErrorNumber))
                .ReturnsAsync((int?)null);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockClaimRepository.Verify(x => x.GetErrorMessageAsync(expectedErrorNumber), Times.Once);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task HandleUploadResultAsync_WithZeroClaimId_ShouldStillProcess()
        {
            // Arrange
            var claimId = 0;
            var result = OperationResult.Success("file.edi");

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    claimId,
                    ClaimStatus.Pending,
                    null))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockClaimRepository.Verify(x => x.UpdateClaimDetailsAsync(
                claimId,
                ClaimStatus.Pending,
                null), Times.Once);
        }

        [Fact]
        public async Task HandleUploadResultAsync_WithNegativeClaimId_ShouldStillProcess()
        {
            // Arrange
            var claimId = -1;
            var result = OperationResult.Success("file.edi");

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    claimId,
                    ClaimStatus.Pending,
                    null))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockClaimRepository.Verify(x => x.UpdateClaimDetailsAsync(
                claimId,
                ClaimStatus.Pending,
                null), Times.Once);
        }

        [Fact]
        public async Task HandleUploadResultAsync_WithLargeClaimId_ShouldProcess()
        {
            // Arrange
            var claimId = int.MaxValue;
            var result = OperationResult.Success("file.edi");

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    claimId,
                    ClaimStatus.Pending,
                    null))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockClaimRepository.Verify(x => x.UpdateClaimDetailsAsync(
                claimId,
                ClaimStatus.Pending,
                null), Times.Once);
        }

        [Fact]
        public async Task HandleUploadResultAsync_WhenSuccessWithEmptyFileName_ShouldStillSucceed()
        {
            // Arrange
            var claimId = 1900;
            var result = OperationResult.Success(string.Empty);

            _mockClaimRepository.Setup(x => x.UpdateClaimDetailsAsync(
                    claimId,
                    ClaimStatus.Pending,
                    null))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.HandleUploadResultAsync(claimId, result);

            // Assert
            _mockClaimRepository.Verify(x => x.UpdateClaimDetailsAsync(
                claimId,
                ClaimStatus.Pending,
                null), Times.Once);
        }

        #endregion
    }
}
