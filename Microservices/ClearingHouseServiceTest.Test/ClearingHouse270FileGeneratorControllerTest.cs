using Billing.FolderStructure.Core.Services;
using ClearingHouseService.Web.Controllers;
using ClearingHouseService.Web.Interface;
using ClearingHouseService.Web.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models.EligibilityRequest;

namespace ClearingHouseService.Test
{
    public class ClearingHouse270FileGeneratorControllerTest
    {
        private readonly Mock<IClearingHouseProcessorFor270Edi> _mockClearingHouseProcessor;
        private readonly Mock<IEdiUploadService> _mockEdiUploadService;
        private readonly Mock<IBillingBlobService> _mockBillingBlobService;
        private readonly Mock<IRethinkMasterDataMicroServices> _mockReThinkServices;
        private readonly Mock<ILogger<ClearingHouse270FileGeneratorController>> _mockLogger;
        private readonly ClearingHouse270FileGeneratorController _controller;

        public ClearingHouse270FileGeneratorControllerTest()
        {
            _mockClearingHouseProcessor = new Mock<IClearingHouseProcessorFor270Edi>();
            _mockEdiUploadService = new Mock<IEdiUploadService>();
            _mockBillingBlobService = new Mock<IBillingBlobService>();
            _mockReThinkServices = new Mock<IRethinkMasterDataMicroServices>();
            _mockLogger = new Mock<ILogger<ClearingHouse270FileGeneratorController>>();

            _controller = new ClearingHouse270FileGeneratorController(
                _mockClearingHouseProcessor.Object,
                _mockEdiUploadService.Object,
                _mockBillingBlobService.Object,
                _mockReThinkServices.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Upload270EdiDataToSftp_SuccessfulUpload_ReturnsOk()
        {
            // Arrange
            var stream = new MemoryStream();
            var eligibility270Request = new Eligibility270Request
            {
                AccountInfoId = 1,
                FunderId = 1234
            };

            _mockReThinkServices.Setup(r => r.GetClearingHouseId(It.IsAny<int>()))
                .ReturnsAsync(1);

            _mockClearingHouseProcessor.Setup(c => c.Generate270EDIData(It.IsAny<Eligibility270Request>()))
                .ReturnsAsync((true, "validEdiData"));

            _mockClearingHouseProcessor.Setup(c => c.Generate270EDIData(It.IsAny<Eligibility270Request>()))
                .ReturnsAsync((true, "validEdiData"));

            _mockEdiUploadService.Setup(e => e.ProcessClaimAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(OperationResult.Success("validFileName"));

            // Act
            var result = await _controller.Upload270EdiDataToSftp(eligibility270Request);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("EDI file successfully uploaded for Funder Id: 1234", actionResult.Value);
        }

        [Fact]
        public async Task Upload270EdiDataToSftp_EdiGenerationFailure_ReturnsBadRequest()
        {
            // Arrange
            var eligibility270Request = new Eligibility270Request
            {
                AccountInfoId = 1,
                FunderId = 1234
            };

            _mockReThinkServices.Setup(r => r.GetClearingHouseId(It.IsAny<int>()))
                .ReturnsAsync(1);

            _mockClearingHouseProcessor.Setup(c => c.Generate270EDIData(It.IsAny<Eligibility270Request>()))
                .ReturnsAsync((false, string.Empty));

            // Act
            var result = await _controller.Upload270EdiDataToSftp(eligibility270Request);

            // Assert
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Failed to generate EDI for Funder Id: 1234", actionResult.Value);
        }

        [Fact]
        public async Task Upload270EdiDataToSftp_UploadFailure_ReturnsBadRequest()
        {
            // Arrange
            var eligibility270Request = new Eligibility270Request
            {
                AccountInfoId = 1,
                FunderId = 1234
            };

            _mockReThinkServices.Setup(r => r.GetClearingHouseId(It.IsAny<int>()))
                .ReturnsAsync(1);

            _mockClearingHouseProcessor.Setup(c => c.Generate270EDIData(It.IsAny<Eligibility270Request>()))
                .ReturnsAsync((true, "validEdiData"));

            _mockEdiUploadService.Setup(e => e.ProcessClaimAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(OperationResult.Fail("Upload failed"));

            // Act
            var result = await _controller.Upload270EdiDataToSftp(eligibility270Request);

            // Assert
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Failed to upload EDI file to SFTP folder for Funder Id: 1234", actionResult.Value);
        }

        [Fact]
        public async Task Upload270EdiDataToSftp_ExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            var eligibility270Request = new Eligibility270Request
            {
                AccountInfoId = 1,
                FunderId = 1234
            };

            _mockReThinkServices.Setup(r => r.GetClearingHouseId(It.IsAny<int>()))
                .Throws(new Exception("Unexpected error"));

            // Act
            var result = await _controller.Upload270EdiDataToSftp(eligibility270Request);

            // Assert
            var actionResult = Assert.IsType<ObjectResult>(result);  
            Assert.Equal(500, actionResult.StatusCode);  
            Assert.Equal("Internal server error", actionResult.Value);  
        }

    }
}
