using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models.Claims;
using BillingService.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Models.Claim;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;


namespace BillingService.XUnit.Integration.Tests
{
    public class ClearingHouseControllerTest
    {
        private readonly Mock<ICHService> _mockCHService = new();
        private readonly Mock<IClaimManagerService> _mockClaimManagerService = new();
        private readonly Mock<IPaymentPostingService> _mockPaymentPostingService = new();
        private readonly Mock<ILogger<ClearingHouseController>> _mockLogger = new();
        private readonly Mock<IMapper> _mockMapper = new();
        private ClearingHouseController CreateController()
        {
            return new ClearingHouseController(
                _mockCHService.Object,
                _mockClaimManagerService.Object,
                _mockPaymentPostingService.Object,
                _mockLogger.Object,
                _mockMapper.Object

            );
        }

        [Fact]
        public async Task GenerateEDIData_ReturnsOk_WhenEDIGeneratedSuccessfully()
        {
            // Arrange
            var claimModel = new ClearingHouseClaimModel();
            var expectedEdiData = "ISA*00*          *00*          *ZZ*TEST*ZZ*RECVR*";

            _mockClaimManagerService.Setup(s => s.GenerateEdi(claimModel))
                .ReturnsAsync(expectedEdiData);

            var controller = CreateController();

            // Act
            var result = await controller.GenerateEDIData(claimModel);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedEdiData, okResult.Value);
        }

        [Fact]
        public async Task GenerateEDIData_ReturnsBadRequest_WhenInvalidEdiData()
        {
            // Arrange
            var claimModel = new ClearingHouseClaimModel();
            var invalidEdiData = "NOTVALID";

            _mockClaimManagerService.Setup(s => s.GenerateEdi(claimModel))
                .ReturnsAsync(invalidEdiData);

            var controller = CreateController();

            // Act
            var result = await controller.GenerateEDIData(claimModel);

            // Assert
            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Failed to Generate EDI data", badResult.Value);
        }

        [Fact]
        public async Task GenerateEDIData_ReturnsBadRequest_WhenServiceThrowsException()
        {
            // Arrange
            var claimModel = new ClearingHouseClaimModel();

            _mockClaimManagerService.Setup(s => s.GenerateEdi(claimModel))
                .ThrowsAsync(new Exception("EDI generation failed"));

            var controller = CreateController();

            // Act
            var result = await controller.GenerateEDIData(claimModel);

            // Assert
            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("EDI generation failed", badResult.Value);
        }

        [Fact]
        public async Task UploadFileToBlobStorage_ReturnsTrue_WhenUploadSucceeds()
        {
            // Arrange
            var model = new ClaimUploadModelWithUserInfo { ClaimId = 1, FileName = "test.txt" };

            _mockCHService.Setup(s => s.UploadFileAsync(model))
                .ReturnsAsync(true);

            var controller = CreateController();

            // Act
            var result = await controller.UploadFileToBlobStorage(model);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UploadFileToBlobStorage_ReturnsFalse_WhenServiceThrowsException()
        {
            // Arrange
            var model = new ClaimUploadModelWithUserInfo { ClaimId = 1, FileName = "fail.txt" };

            _mockCHService.Setup(s => s.UploadFileAsync(model))
                .ThrowsAsync(new Exception("Upload failed"));

            var controller = CreateController();

            // Act
            var result = await controller.UploadFileToBlobStorage(model);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UploadERAErrorFileToBlobStorage_ReturnsTrue_WhenUploadSucceeds()
        {
            // Arrange
            var model = new ERAUploadModel { fileName = "errorfile.edi", PaymentIds = new List<int> { 1, 2 } };

            _mockCHService.Setup(s => s.UploadERAErrorFileAsync(model))
                .ReturnsAsync(true);

            var controller = CreateController();

            // Act
            var result = await controller.UploadERAErrorFileToBlobStorage(model);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UploadERAErrorFileToBlobStorage_ReturnsFalse_WhenServiceThrowsException()
        {
            // Arrange
            var model = new ERAUploadModel { fileName = "badfile.edi", PaymentIds = new List<int> { 1 } };

            _mockCHService.Setup(s => s.UploadERAErrorFileAsync(model))
                .ThrowsAsync(new Exception("Blob upload failed"));

            var controller = CreateController();

            // Act
            var result = await controller.UploadERAErrorFileToBlobStorage(model);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UploadEDIResponseFile_ReturnsTrue_WhenUploadSucceeds()
        {
            // Arrange
            var model = new DownloadSftpDataModel { FileName = "response.edi" };

            _mockCHService.Setup(s => s.UploadEDIResponseFile(model))
                .ReturnsAsync(true);

            var controller = CreateController();

            // Act
            var result = await controller.UploadEDIResponseFile(model);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UploadEDIResponseFile_ReturnsFalse_WhenServiceThrowsException()
        {
            // Arrange
            var model = new DownloadSftpDataModel { FileName = "response.edi" };

            _mockCHService.Setup(s => s.UploadEDIResponseFile(model))
                .ThrowsAsync(new Exception("SFTP upload failed"));

            var controller = CreateController();

            // Act
            var result = await controller.UploadEDIResponseFile(model);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GenerateERAErrorData_ReturnsOk_WhenServiceSucceeds()
        {
            // Arrange
            var model = new ERAUploadModel
            {
                accountInfoId = 1,
                PaymentIds = new List<int> { 2001 },
                data = new byte[] { 0x01, 0x02, 0x03 },
                fileName = "test.edi",
                containerName = "test-container",
                claimIdentifier = "CLM123"
            };
            var expected ="Error data generated";

            _mockPaymentPostingService.Setup(s => s.GetERAErrors(model))
                .ReturnsAsync(expected);

            var controller = CreateController();

            // Act
            var result = await controller.GenerateERAErrorData(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task GenerateERAErrorData_ReturnsBadRequest_WhenServiceThrowsException()
        {
            // Arrange
            var model = new ERAUploadModel();

            _mockPaymentPostingService.Setup(s => s.GetERAErrors(model))
                .ThrowsAsync(new Exception("ERA processing failed"));

            var controller = CreateController();

            // Act
            var result = await controller.GenerateERAErrorData(model);

            // Assert
            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("ERA processing failed", badResult.Value);
        }
    }
}
