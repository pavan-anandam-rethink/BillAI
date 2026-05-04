using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models.Claims;
using Microsoft.Extensions.Logging;
using Moq;
using BillingService.Web.Controllers;
using Xunit;
using System.Threading.Tasks;
using System;
using Rethink.Services.Common.Models.Claim;

namespace BillingService.XUnit.Tests.Billing.ClearingHouse
{
    public class ClearingHouseControllerTests
    {
        readonly Mock<ICHService> _mockClearingHouseService;
        private readonly Mock<IClaimManagerService> _mockClaimManagerService;
        private readonly Mock<IPaymentPostingService> _mockPaymentPostingService;
        private readonly Mock<ILogger<ClearingHouseController>> _mockLogger;

        private readonly ClearingHouseController _controller;

        public ClearingHouseControllerTests()
        {
            _mockClearingHouseService = new Mock<ICHService>();
            _mockClaimManagerService = new Mock<IClaimManagerService>();
            _mockPaymentPostingService = new Mock<IPaymentPostingService>();
            _mockLogger = new Mock<ILogger<ClearingHouseController>>();

            _controller = new ClearingHouseController(
                _mockClearingHouseService.Object,
                _mockClaimManagerService.Object,
                _mockPaymentPostingService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task UploadFileToBlobStorage_ShouldReturnTrue_WhenUploadIsSuccessful()
        {
            // Arrange
            var filesWithUserInfo = new ClaimUploadModelWithUserInfo
            {
                ClaimId = 2551,
                FileName = "testfile.edi",
                FileMimeType = "application/edi",
                Data = new byte[] { 0x00, 0x01, 0x02 } 
            };

            _mockClearingHouseService
                .Setup(x => x.UploadFileAsync(It.Is<ClaimUploadModelWithUserInfo>(y =>
                    y.ClaimId == filesWithUserInfo.ClaimId &&
                    y.FileName == filesWithUserInfo.FileName &&
                    y.FileMimeType == filesWithUserInfo.FileMimeType &&
                    y.Data.Length == filesWithUserInfo.Data.Length))) 
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UploadFileToBlobStorage(filesWithUserInfo);

            // Assert
            Assert.True(result);
            _mockClearingHouseService.Verify(
                x => x.UploadFileAsync(It.IsAny<ClaimUploadModelWithUserInfo>()),
                Times.Once
            );
        }

        [Fact]
        public async Task UploadFileToBlobStorage_ReturnsFalse_AndLogsError_WhenExceptionThrown()
        {
            // Arrange
            var model = new ClaimUploadModelWithUserInfo
            {
                ClaimId = 2551,
                FileName = "testfile.edi",
                FileMimeType = "application/edi",
                Data = new byte[] { 0x00, 0x01, 0x02 }
            };

            var exception = new Exception("Test exception");

            _mockClearingHouseService.Setup(s => s.UploadFileAsync(It.IsAny<ClaimUploadModelWithUserInfo>()))
                        .ThrowsAsync(exception);

            // Act
            var result = await _controller.UploadFileToBlobStorage(model);

            // Assert
            Assert.False(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error at uploading EDI file to Azure blob storage")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task UploadFileToBlobStorage_ReturnsFalse_WhenModelIsNull()
        {
            // Arrange
            ClaimUploadModelWithUserInfo model = null;

            // Act
            var result = await _controller.UploadFileToBlobStorage(model);

            // Assert
            Assert.False(result); 
        }

        [Fact]
        public async Task UploadERAErrorFileToBlobStorage_ReturnsTrue_WhenServiceSucceeds()
        {
            // Arrange
            var model = new ERAUploadModel
            {
                accountInfoId = 18421,
                containerName = "billing-container",
                fileName = "testfile.edi",
                PaymentIds = { 123 },
                data = new byte[] { 0x00, 0x01, 0x02 }
            };

            _mockClearingHouseService
                .Setup(s => s.UploadERAErrorFileAsync(model))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UploadERAErrorFileToBlobStorage(model);

            // Assert
            Assert.True(result);
            _mockClearingHouseService.Verify(s => s.UploadERAErrorFileAsync(model), Times.Once);
        }

    }
}
