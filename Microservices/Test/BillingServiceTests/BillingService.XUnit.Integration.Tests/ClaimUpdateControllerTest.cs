using AutoFixture;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Web.Controllers;
using BillingService.Web.Helpers.HttpClients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Integration.Tests
{
    public class ClaimUpdateControllerTests
    {
        private readonly IFixture _fixture;
        private readonly Mock<IClaimUpdateService> _mockClaimUpdateService;
        private readonly Mock<IBaseHttpClient> _mockHttpClient;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly ClaimUpdateController _controller;
        private readonly Mock<ILogger<ClaimUpdateController>> _loggerMock;

        public ClaimUpdateControllerTests()
        {
            _fixture = new Fixture();
            _mockClaimUpdateService = new Mock<IClaimUpdateService>();
            _mockHttpClient = new Mock<IBaseHttpClient>();
            _mockConfig = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<ClaimUpdateController>>();
            _controller = new ClaimUpdateController(
                _mockHttpClient.Object,
                _mockConfig.Object,
                _mockClaimUpdateService.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task UpdateClaimIfSecondaryFunderPresent_ShouldReturnOk_WhenServiceReturnsTrue()
        {
            // Arrange
            var model = _fixture.Create<IdWithUserInfo>();
            _mockClaimUpdateService
                .Setup(x => x.UpdateClaimSecondaryFunderOnRefresh(model.AccountInfoId, model.MemberId, model.Id))
                .ReturnsAsync(new ClaimUpdateResult { Success = true });

            // Act
            var result = await _controller.UpdateClaimIfSecondaryFunderPresent(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var claimUpdateResult = Assert.IsType<ClaimUpdateResult>(okResult.Value);
            Assert.True(claimUpdateResult.Success);
        }

        [Fact]
        public async Task UpdateClaimIfSecondaryFunderPresent_ShouldReturnBadRequest_WhenServiceThrowsException()
        {
            // Arrange
            var model = _fixture.Create<IdWithUserInfo>();
            _mockClaimUpdateService
                .Setup(x => x.UpdateClaimSecondaryFunderOnRefresh(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.UpdateClaimIfSecondaryFunderPresent(model);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Service error", badRequestResult.Value);
        }
    }
}
