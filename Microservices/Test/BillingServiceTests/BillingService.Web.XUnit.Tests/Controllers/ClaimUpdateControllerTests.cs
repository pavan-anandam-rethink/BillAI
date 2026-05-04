using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Web.Controllers;
using BillingService.Web.Helpers.HttpClients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace BillingService.Web.XUnit.Tests.Controllers
{
    public class ClaimUpdateControllerTests
    {
        private readonly Mock<IBaseHttpClient> _mockHttpClient;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IClaimUpdateService> _mockClaimUpdateService;
        private readonly ClaimUpdateController _controller;
        private readonly Mock<ILogger<ClaimUpdateController>> _mockLogger;
        public ClaimUpdateControllerTests()
        {
            _mockHttpClient = new Mock<IBaseHttpClient>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockClaimUpdateService = new Mock<IClaimUpdateService>();
            _mockLogger = new Mock<ILogger<ClaimUpdateController>>();
            _controller = new ClaimUpdateController(
                _mockHttpClient.Object,
                _mockConfiguration.Object,
                _mockClaimUpdateService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task UpdateClaimIfSecondaryFunderPresent_ReturnsOk_WithExpectedResult()
        {
            // Arrange
            var requestModel = new IdWithUserInfo
            {
                AccountInfoId = 1,
                MemberId = 2,
                Id = 3
            };

            var expectedResult = true;

            _mockClaimUpdateService
                .Setup(s => s.UpdateClaimSecondaryFunderOnRefresh(requestModel.AccountInfoId, requestModel.MemberId, requestModel.Id))
                .ReturnsAsync(new ClaimUpdateResult { Success = expectedResult });

            // Act
            var result = await _controller.UpdateClaimIfSecondaryFunderPresent(requestModel);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResult, ((ClaimUpdateResult)okResult.Value).Success);
        }

        [Fact]
        public async Task UpdateClaimIfSecondaryFunderPresent_WhenServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var requestModel = new IdWithUserInfo
            {
                AccountInfoId = 1,
                MemberId = 2,
                Id = 3
            };

            _mockClaimUpdateService
                .Setup(s => s.UpdateClaimSecondaryFunderOnRefresh(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Service failure"));

            // Act
            var result = await _controller.UpdateClaimIfSecondaryFunderPresent(requestModel);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Service failure", badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateClaimIfSecondaryFunderPresent_WhenModelIsNull_LogsErrorAndReturnsBadRequest()
        {
            // Act
            var result = await _controller.UpdateClaimIfSecondaryFunderPresent(null);

            // Assert - BadRequest
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.False(string.IsNullOrWhiteSpace(badRequestResult.Value?.ToString()));

            // Assert - LogError called
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString().Contains(
                            "ClaimUpdateController.UpdateClaimIfSecondaryFunderPresent -Failed to update claim secondary funder on refresh")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }


        [Fact]
        public async Task UpdateClaimIfSecondaryFunderPresent_ServiceReturnsFalse_ReturnsOkWithFalse()
        {
            // Arrange
            var requestModel = new IdWithUserInfo
            {
                AccountInfoId = 1,
                MemberId = 2,
                Id = 3
            };

            _mockClaimUpdateService
                .Setup(s => s.UpdateClaimSecondaryFunderOnRefresh(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new ClaimUpdateResult { Success = false });

            // Act
            var result = await _controller.UpdateClaimIfSecondaryFunderPresent(requestModel);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.False(((ClaimUpdateResult)okResult.Value).Success);
        }

        [Fact]
        public async Task UpdateClaimIfSecondaryFunderPresent_CallsServiceWithCorrectParameters()
        {
            // Arrange
            var requestModel = new IdWithUserInfo
            {
                AccountInfoId = 11,
                MemberId = 22,
                Id = 33
            };

            _mockClaimUpdateService
                .Setup(s => s.UpdateClaimSecondaryFunderOnRefresh(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new ClaimUpdateResult { Success = true });

            // Act
            await _controller.UpdateClaimIfSecondaryFunderPresent(requestModel);

            // Assert
            _mockClaimUpdateService.Verify(s =>
                s.UpdateClaimSecondaryFunderOnRefresh(
                    requestModel.AccountInfoId,
                    requestModel.MemberId,
                    requestModel.Id),
                Times.Once);
        }
    }
}
