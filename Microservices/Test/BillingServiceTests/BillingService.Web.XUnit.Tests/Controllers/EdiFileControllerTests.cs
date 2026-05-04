using Billing.FolderStructure.Core.Models;
using Billing.FolderStructure.Core.Utils;
using BillingService.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BillingService.Web.XUnit.Tests.Controllers
{
    public class EdiFileControllerTests
    {
        private readonly Mock<IBillingFilePath> _billingFilePathMock;
        private readonly Mock<ILogger<EdiFileController>> _mockLogger;
        private readonly EdiFileController _controller;

        public EdiFileControllerTests()
        {
            _billingFilePathMock = new Mock<IBillingFilePath>();
            _mockLogger = new Mock<ILogger<EdiFileController>>();
            _controller = new EdiFileController(
                _billingFilePathMock.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task GetEdiFilesFromBlob_ReturnsOk_WhenEdiContentFound()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 1,
                ClaimSubmissionId = 100,
                ClaimId = 200,
                FileType = "837"
            };

            var expectedContent = "ISA*00*~ST*837*0001~CLM*12345*100~SE*3*0001~IEA*1*000000001~";

            _billingFilePathMock
                .Setup(s => s.GetEdiFilesFromBlob(model))
                .ReturnsAsync(expectedContent);

            // Act
            var result = await _controller.GetEdiFilesFromBlob(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedContent, okResult.Value);
        }

        [Fact]
        public async Task GetEdiFilesFromBlob_ReturnsOk_WhenNoMatchingRecordFound()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 1,
                ClaimSubmissionId = 999,
                FileType = "835"
            };

            _billingFilePathMock
                .Setup(s => s.GetEdiFilesFromBlob(model))
                .ReturnsAsync(string.Empty);

            // Act
            var result = await _controller.GetEdiFilesFromBlob(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(string.Empty, okResult.Value);
        }

        [Fact]
        public async Task GetEdiFilesFromBlob_ReturnsBadRequest_WhenExceptionThrown()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 1,
                FileType = "277"
            };

            _billingFilePathMock
                .Setup(s => s.GetEdiFilesFromBlob(It.IsAny<ClaimEdiFilesModel>()))
                .ThrowsAsync(new Exception("Blob storage unavailable"));

            // Act
            var result = await _controller.GetEdiFilesFromBlob(model);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Blob storage unavailable", badRequest.Value);
        }

        [Fact]
        public async Task GetEdiFilesFromBlob_ReturnsOk_WithPaymentIdFilter()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 5,
                PaymentId = 300,
                FileType = "835"
            };

            var expectedContent = "ISA*00*~ST*835*0001~CLP*CLM123~SE*3*0001~IEA*1*000000001~";

            _billingFilePathMock
                .Setup(s => s.GetEdiFilesFromBlob(model))
                .ReturnsAsync(expectedContent);

            // Act
            var result = await _controller.GetEdiFilesFromBlob(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedContent, okResult.Value);
        }

        [Fact]
        public async Task GetEdiFilesFromBlob_ReturnsOk_WithMinimalModel()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 10
            };

            var expectedContent = "EDI content";

            _billingFilePathMock
                .Setup(s => s.GetEdiFilesFromBlob(model))
                .ReturnsAsync(expectedContent);

            // Act
            var result = await _controller.GetEdiFilesFromBlob(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedContent, okResult.Value);
        }

        [Fact]
        public async Task GetEdiFilesFromBlob_CallsServiceExactlyOnce()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 1,
                FileType = "999"
            };

            _billingFilePathMock
                .Setup(s => s.GetEdiFilesFromBlob(model))
                .ReturnsAsync("content");

            // Act
            await _controller.GetEdiFilesFromBlob(model);

            // Assert
            _billingFilePathMock.Verify(s => s.GetEdiFilesFromBlob(model), Times.Once);
        }

    }
}
