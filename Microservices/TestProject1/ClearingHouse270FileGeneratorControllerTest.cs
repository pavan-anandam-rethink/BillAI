using ClearingHouseService.Web.Controllers;
using ClearingHouseService.Web.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Dtos.ClearingHouse;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models.EligibilityRequest;

namespace ClearingHouse
{
    public class ClearingHouse270FileGeneratorControllerTest
    {
        [Fact]
        public async Task Upload270EdiData_ReturnsOk_WhenEdiGeneratedAndEnqueued()
        {
            // Arrange
            var mockProcessor = new Mock<IClearingHouseProcessorFor270Edi>();
            var mockLogger = new Mock<ILogger<ClearingHouse270FileGeneratorController>>();
            var mockQueue = new Mock<IBackgroundJobQueue>();

            mockProcessor
                .Setup(p => p.Generate270EDIData(It.IsAny<Eligibility270Request>()))
                .ReturnsAsync((true, "ISA*GENERATED*EDI"));

            mockQueue
                .Setup(q => q.EnqueueAsync(It.IsAny<StediEligibilityJobDTO>()))
                .Returns(new ValueTask())
                .Verifiable();

            var controller = new ClearingHouse270FileGeneratorController(
                mockProcessor.Object,
                mockLogger.Object,
                mockQueue.Object);

            var request = new Eligibility270Request
            {
                FunderId = 123,
                MemberId = 456,
                AccountInfoId = 789,
                FunderName = "TestFunder"
            };

            // Act
            var result = await controller.Upload270EdiData(request);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("270 EDI file successfully processed", ok.Value?.ToString());
            mockQueue.Verify();
        }

        [Fact]
        public async Task Upload270EdiData_ReturnsBadRequest_WhenEdiGenerationFails()
        {
            // Arrange
            var mockProcessor = new Mock<IClearingHouseProcessorFor270Edi>();
            var mockLogger = new Mock<ILogger<ClearingHouse270FileGeneratorController>>();
            var mockQueue = new Mock<IBackgroundJobQueue>();

            mockProcessor
                .Setup(p => p.Generate270EDIData(It.IsAny<Eligibility270Request>()))
                .ReturnsAsync((false, string.Empty));

            var controller = new ClearingHouse270FileGeneratorController(
                mockProcessor.Object,
                mockLogger.Object,
                mockQueue.Object);

            var request = new Eligibility270Request
            {
                FunderId = 10,
                MemberId = 20,
                AccountInfoId = 30
            };

            // Act
            var result = await controller.Upload270EdiData(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Failed to generate EDI", badRequest.Value?.ToString());
        }

        [Fact]
        public async Task Upload270EdiData_ReturnsInternalServerError_OnException()
        {
            // Arrange
            var mockProcessor = new Mock<IClearingHouseProcessorFor270Edi>();
            var mockLogger = new Mock<ILogger<ClearingHouse270FileGeneratorController>>();
            var mockQueue = new Mock<IBackgroundJobQueue>();

            mockProcessor
                .Setup(p => p.Generate270EDIData(It.IsAny<Eligibility270Request>()))
                .ThrowsAsync(new InvalidOperationException("boom"));

            var controller = new ClearingHouse270FileGeneratorController(
                mockProcessor.Object,
                mockLogger.Object,
                mockQueue.Object);

            var request = new Eligibility270Request
            {
                FunderId = 1,
                MemberId = 2,
                AccountInfoId = 3
            };

            // Act
            var result = await controller.Upload270EdiData(request);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            Assert.Contains("Internal server error", objectResult.Value?.ToString());
        }
    }
}
