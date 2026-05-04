using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models.Claims;
using BillingService.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Models.Claim;

namespace BillingService.Web.XUnit.Tests.Controllers
{
    public class ClearingHouseControllerTest
    {
        private readonly Mock<ICHService> _mockCHService;
        private readonly Mock<IClaimManagerService> _mockClaimManagerService;
        private readonly Mock<IPaymentPostingService> _mockPaymentPostingService;
        private readonly Mock<ILogger<ClearingHouseController>> _mockLogger;
        private readonly ClearingHouseController _controller;
    
        private readonly Mock<IMapper> _mockMapper;

        public ClearingHouseControllerTest()
        {
            _mockCHService = new Mock<ICHService>();
            _mockClaimManagerService = new Mock<IClaimManagerService>();
            _mockPaymentPostingService = new Mock<IPaymentPostingService>();
            _mockLogger = new Mock<ILogger<ClearingHouseController>>();
            _mockMapper = new Mock<IMapper>();

            _controller = new ClearingHouseController(
                _mockCHService.Object,
                _mockClaimManagerService.Object,
                _mockPaymentPostingService.Object,
                _mockLogger.Object,
                _mockMapper.Object
            );
        }

        [Fact]
        public async Task GenerateEDIData_ShouldReturnOk_WhenEdiStartsWithISA()
        {
            // Arrange
            var model = new ClearingHouseClaimModel();
            _mockClaimManagerService
                .Setup(x => x.GenerateEdi(model))
                .ReturnsAsync("ISA12345");

            // Act
            var result = await _controller.GenerateEDIData(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("ISA12345", okResult.Value);
        }

        [Fact]
        public async Task GenerateEDIData_ShouldReturnBadRequest_WhenInvalidEdi()
        {
            var model = new ClearingHouseClaimModel();
            _mockClaimManagerService
                .Setup(x => x.GenerateEdi(model))
                .ReturnsAsync("INVALID");

            var result = await _controller.GenerateEDIData(model);

            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Failed to Generate EDI data", badResult.Value);
        }

        [Fact]
        public async Task UploadFileToBlobStorage_ShouldReturnTrue_WhenUploadSucceeds()
        {
            var model = new ClaimUploadModelWithUserInfo();
            _mockCHService
                .Setup(x => x.UploadFileAsync(model))
                .ReturnsAsync(true);

            var result = await _controller.UploadFileToBlobStorage(model);

            Assert.True(result);
        }

        [Fact]
        public async Task UploadFileToBlobStorage_ShouldReturnFalse_WhenExceptionThrown()
        {
            var model = new ClaimUploadModelWithUserInfo();
            _mockCHService
                .Setup(x => x.UploadFileAsync(model))
                .ThrowsAsync(new Exception("Upload failed"));

            var result = await _controller.UploadFileToBlobStorage(model);

            Assert.False(result);
        }

        [Fact]
        public async Task UploadERAErrorFileToBlobStorage_ReturnsTrue_WhenServiceSucceeds()
        {
            // Arrange
            var model = new ERAUploadModel
            {
                PaymentIds = new List<int> { 1, 2 },
                fileName = "error.edi"
            };

            _mockCHService
                .Setup(x => x.UploadERAErrorFileAsync(model))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UploadERAErrorFileToBlobStorage(model);

            // Assert
            Assert.True(result);

            // Verify service call
            _mockCHService.Verify(
                x => x.UploadERAErrorFileAsync(model),
                Times.Once);

            // Verify LogInformation (upload start or success)
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString().Contains("Uploading ERA error file to blob storage")
                        || v.ToString().Contains("Successfully uploaded ERA error file to blob storage")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }


        [Fact]
        public async Task UploadEDIResponseFile_ShouldReturnTrue_WhenSuccess()
        {
            var model = new DownloadSftpDataModel();
            _mockCHService
                .Setup(x => x.UploadEDIResponseFile(model))
                .ReturnsAsync(true);

            var result = await _controller.UploadEDIResponseFile(model);

            Assert.True(result);
        }

        [Fact]
        public async Task UploadEDIResponseFile_ShouldReturnFalse_WhenExceptionThrown()
        {
            var model = new DownloadSftpDataModel();
            _mockCHService
                .Setup(x => x.UploadEDIResponseFile(model))
                .ThrowsAsync(new Exception("SFTP upload failed"));

            var result = await _controller.UploadEDIResponseFile(model);

            Assert.False(result);
        }

        [Fact]
        public async Task GenerateERAErrorData_ShouldReturnOk_WhenSuccessful()
        {
            var model = new ERAUploadModel();
            var expected = "Some ERA Error Result";

            _mockPaymentPostingService
                .Setup(x => x.GetERAErrors(model))
                .ReturnsAsync(expected);

            var result = await _controller.GenerateERAErrorData(model);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task GenerateERAErrorData_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            var model = new ERAUploadModel();

            _mockPaymentPostingService
                .Setup(x => x.GetERAErrors(model))
                .ThrowsAsync(new Exception("Error generating ERA data"));

            var result = await _controller.GenerateERAErrorData(model);

            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Error generating ERA data", badResult.Value);
        }
    }
}
