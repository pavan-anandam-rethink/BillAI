using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Models.Claim;
using SummationService.Domain.Interfaces;
using SummationService.Web.Controllers;

namespace SummationService.Web.XUnit.Tests.Controllers
{
    public class ClaimTransactionControllerTests
    {
        private readonly Mock<IClaimTransactionService> _mockClaimTransactionService;
        private readonly Mock<IChargeTransactionService> _mockChargeTransactionService;
        private readonly Mock<ILogger<ClaimTransactionController>> _mockLogger;
        private readonly ClaimTransactionController _controller;

        public ClaimTransactionControllerTests()
        {
            _mockClaimTransactionService = new Mock<IClaimTransactionService>();
            _mockChargeTransactionService = new Mock<IChargeTransactionService>();
            _mockLogger = new Mock<ILogger<ClaimTransactionController>>();

            _controller = new ClaimTransactionController(
                _mockClaimTransactionService.Object,
                _mockChargeTransactionService.Object,
                _mockLogger.Object
            );
        }

        #region AddOrUpdateClaimTransaction - Success Tests

        [Fact]
        public async Task AddOrUpdateClaimTransaction_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var model = new ClaimTransactionModel
            {
                TransactionType = (int)ClaimTransactionType.billedAmount,
                TransactionTypeId = 123
            };

            _mockClaimTransactionService
                .Setup(s => s.AddOrUpdateClaimTransactionAsync(
                    ClaimTransactionType.billedAmount,
                    123,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.AddOrUpdateClaimTransaction(model, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task AddOrUpdateClaimTransaction_CallsServiceWithCorrectParameters()
        {
            // Arrange
            var model = new ClaimTransactionModel
            {
                TransactionType = (int)ClaimTransactionType.insurancePayment,
                TransactionTypeId = 456
            };

            _mockClaimTransactionService
                .Setup(s => s.AddOrUpdateClaimTransactionAsync(
                    ClaimTransactionType.insurancePayment,
                    456,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await _controller.AddOrUpdateClaimTransaction(model, CancellationToken.None);

            // Assert
            _mockClaimTransactionService.Verify(
                s => s.AddOrUpdateClaimTransactionAsync(
                    ClaimTransactionType.insurancePayment,
                    456,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task AddOrUpdateClaimTransaction_LogsInformationOnStart()
        {
            // Arrange
            var model = new ClaimTransactionModel
            {
                TransactionType = (int)ClaimTransactionType.patientPayment,
                TransactionTypeId = 789
            };

            _mockClaimTransactionService
                .Setup(s => s.AddOrUpdateClaimTransactionAsync(
                    It.IsAny<ClaimTransactionType>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await _controller.AddOrUpdateClaimTransaction(model, CancellationToken.None);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Received AddOrUpdateClaimTransaction request")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task AddOrUpdateClaimTransaction_LogsInformationOnSuccess()
        {
            // Arrange
            var model = new ClaimTransactionModel
            {
                TransactionType = (int)ClaimTransactionType.adjustment,
                TransactionTypeId = 111
            };

            _mockClaimTransactionService
                .Setup(s => s.AddOrUpdateClaimTransactionAsync(
                    It.IsAny<ClaimTransactionType>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await _controller.AddOrUpdateClaimTransaction(model, CancellationToken.None);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Successfully processed AddOrUpdateClaimTransaction")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData(ClaimTransactionType.billedAmount, 1)]
        [InlineData(ClaimTransactionType.insurancePayment, 2)]
        [InlineData(ClaimTransactionType.patientPayment, 3)]
        [InlineData(ClaimTransactionType.adjustment, 4)]
        [InlineData(ClaimTransactionType.patientResponsibility, 5)]
        [InlineData(ClaimTransactionType.writeOff, 6)]
        [InlineData(ClaimTransactionType.otherPayment, 7)]
        [InlineData(ClaimTransactionType.deleteCharge, 8)]
        [InlineData(ClaimTransactionType.deleteChargePayment, 9)]
        [InlineData(ClaimTransactionType.deleteClaim, 10)]
        [InlineData(ClaimTransactionType.submitClaim, 11)]
        [InlineData(ClaimTransactionType.newDay, 12)]
        [InlineData(ClaimTransactionType.updatePaymentSummary, 13)]
        [InlineData(ClaimTransactionType.eraReceived, 14)]
        public async Task AddOrUpdateClaimTransaction_HandlesAllTransactionTypes(ClaimTransactionType transactionType, int id)
        {
            // Arrange
            var model = new ClaimTransactionModel
            {
                TransactionType = (int)transactionType,
                TransactionTypeId = id
            };

            _mockClaimTransactionService
                .Setup(s => s.AddOrUpdateClaimTransactionAsync(
                    transactionType,
                    id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.AddOrUpdateClaimTransaction(model, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            _mockClaimTransactionService.Verify(
                s => s.AddOrUpdateClaimTransactionAsync(
                    transactionType,
                    id,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task AddOrUpdateClaimTransaction_HandlesCancellationToken()
        {
            // Arrange
            var model = new ClaimTransactionModel
            {
                TransactionType = (int)ClaimTransactionType.billedAmount,
                TransactionTypeId = 999
            };

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            _mockClaimTransactionService
                .Setup(s => s.AddOrUpdateClaimTransactionAsync(
                    It.IsAny<ClaimTransactionType>(),
                    It.IsAny<int>(),
                    cancellationToken))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.AddOrUpdateClaimTransaction(model, cancellationToken);

            // Assert
            Assert.IsType<OkResult>(result);
            _mockClaimTransactionService.Verify(
                s => s.AddOrUpdateClaimTransactionAsync(
                    It.IsAny<ClaimTransactionType>(),
                    It.IsAny<int>(),
                    cancellationToken),
                Times.Once);
        }

        #endregion

        #region AddOrUpdateClaimTransaction - Error Tests

        [Fact]
        public async Task AddOrUpdateClaimTransaction_Returns500_OnException()
        {
            // Arrange
            var model = new ClaimTransactionModel
            {
                TransactionType = (int)ClaimTransactionType.billedAmount,
                TransactionTypeId = 123
            };

            _mockClaimTransactionService
                .Setup(s => s.AddOrUpdateClaimTransactionAsync(
                    It.IsAny<ClaimTransactionType>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test error"));

            // Act
            var result = await _controller.AddOrUpdateClaimTransaction(model, CancellationToken.None);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("Internal server error", statusCodeResult.Value);
        }

        [Fact]
        public async Task AddOrUpdateClaimTransaction_LogsError_OnException()
        {
            // Arrange
            var model = new ClaimTransactionModel
            {
                TransactionType = (int)ClaimTransactionType.insurancePayment,
                TransactionTypeId = 456
            };

            var expectedException = new Exception("Database connection failed");

            _mockClaimTransactionService
                .Setup(s => s.AddOrUpdateClaimTransactionAsync(
                    It.IsAny<ClaimTransactionType>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act
            await _controller.AddOrUpdateClaimTransaction(model, CancellationToken.None);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error adding claim details")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error occurred while processing AddOrUpdateClaimTransaction")),
                    It.Is<Exception>(ex => ex == expectedException),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task AddOrUpdateClaimTransaction_Returns500_OnNullReferenceException()
        {
            // Arrange
            var model = new ClaimTransactionModel
            {
                TransactionType = (int)ClaimTransactionType.patientPayment,
                TransactionTypeId = 789
            };

            _mockClaimTransactionService
                .Setup(s => s.AddOrUpdateClaimTransactionAsync(
                    It.IsAny<ClaimTransactionType>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NullReferenceException("Object reference not set"));

            // Act
            var result = await _controller.AddOrUpdateClaimTransaction(model, CancellationToken.None);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("Internal server error", statusCodeResult.Value);
        }

        [Fact]
        public async Task AddOrUpdateClaimTransaction_Returns500_OnArgumentException()
        {
            // Arrange
            var model = new ClaimTransactionModel
            {
                TransactionType = (int)ClaimTransactionType.adjustment,
                TransactionTypeId = 0
            };

            _mockClaimTransactionService
                .Setup(s => s.AddOrUpdateClaimTransactionAsync(
                    It.IsAny<ClaimTransactionType>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Invalid transaction type ID"));

            // Act
            var result = await _controller.AddOrUpdateClaimTransaction(model, CancellationToken.None);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task AddOrUpdateClaimTransaction_Returns500_OnOperationCanceledException()
        {
            // Arrange
            var model = new ClaimTransactionModel
            {
                TransactionType = (int)ClaimTransactionType.writeOff,
                TransactionTypeId = 555
            };

            _mockClaimTransactionService
                .Setup(s => s.AddOrUpdateClaimTransactionAsync(
                    It.IsAny<ClaimTransactionType>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException("Operation was cancelled"));

            // Act
            var result = await _controller.AddOrUpdateClaimTransaction(model, CancellationToken.None);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("Internal server error", statusCodeResult.Value);
        }

        #endregion

    }
}
