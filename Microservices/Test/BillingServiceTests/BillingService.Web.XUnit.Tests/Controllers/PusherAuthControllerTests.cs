using BillingService.Web.Controllers;
using BillingService.Web.Servers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BillingService.Web.XUnit.Tests.Controllers
{
    public class PusherAuthControllerTests
    {
        private readonly Mock<IPusherNotificationServer> _mockPusherService;
        private readonly PusherAuthController _controller;

        public PusherAuthControllerTests()
        {
            _mockPusherService = new Mock<IPusherNotificationServer>();
            _controller = new PusherAuthController(_mockPusherService.Object);
        }

        [Fact]
        public void Authorize_ShouldReturnJsonResult_WithAuthObject()
        {
            // Arrange
            var channelName = "private-channel-123";
            var socketId = "987654";
            var expectedAuth = new { auth = "mocked-auth-value" };

            _mockPusherService
                .Setup(p => p.Authenticate(channelName, socketId))
                .Returns(expectedAuth);

            // Act
            var result = _controller.Authorize(channelName, socketId);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(expectedAuth, jsonResult.Value);

            _mockPusherService.Verify(
                p => p.Authenticate(channelName, socketId),
                Times.Once);
        }
    }
}
