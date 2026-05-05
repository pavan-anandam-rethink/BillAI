using BillingService.Web.Controllers;
using BillingService.Web.Helpers.HttpClients;
using BillingService.Web.Servers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Models.Claim;

namespace BillingService.Web.XUnit.Tests.Controllers
{
    public class NotifyClaimStatusControllerTests
    {
        private readonly Mock<IPusherNotificationServer> _mockPusherService;
        private readonly Mock<IBaseHttpClient> _mockHttpClient;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly NotifyClaimStatusController _controller;
        private readonly Mock<ILogger<NotifyClaimStatusController>> _mockLogger;

        public NotifyClaimStatusControllerTests()
        {
            _mockPusherService = new Mock<IPusherNotificationServer>();
            _mockHttpClient = new Mock<IBaseHttpClient>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<NotifyClaimStatusController>>();

            _controller = new NotifyClaimStatusController(
                _mockPusherService.Object,
                _mockHttpClient.Object,
                _mockConfiguration.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Notify_ShouldTriggerPusherEvent_AndReturnOk()
        {
            // Arrange
            var update = new ClaimStatusUpdate
            {
                AccountId = 100,
                UserId = 200,
                BatchId = "B123",
                ClaimId = 123,
                Total = 5,
                Status = "Approved",
                Message = "Claim approved successfully"
            };

            var expectedChannel = $"private-account-{update.AccountId}-user-{update.UserId}";
            var expectedEvent = "claim-status-updated";

            object capturedPayload = null;

            _mockPusherService
                .Setup(p => p.TriggerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .Callback<string, string, object>((ch, ev, data) =>
                {
                    Assert.Equal(expectedChannel, ch);
                    Assert.Equal(expectedEvent, ev);
                    capturedPayload = data;
                })
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Notify(update);

            // Assert
            Assert.IsType<OkResult>(result);
            _mockPusherService.Verify(p =>
                p.TriggerAsync(expectedChannel, expectedEvent, It.IsAny<object>()), Times.Once);

            Assert.NotNull(capturedPayload);
            var batchId = capturedPayload.GetType().GetProperty("batchId")?.GetValue(capturedPayload);
            var total = capturedPayload.GetType().GetProperty("total")?.GetValue(capturedPayload);
            var claimId = capturedPayload.GetType().GetProperty("claimId")?.GetValue(capturedPayload);
            var status = capturedPayload.GetType().GetProperty("status")?.GetValue(capturedPayload);
            var message = capturedPayload.GetType().GetProperty("message")?.GetValue(capturedPayload);

            Assert.Equal(update.BatchId, batchId);
            Assert.Equal(update.Total, total);
            Assert.Equal(update.ClaimId, claimId);
            Assert.Equal(update.Status, status);
            Assert.Equal(update.Message, message);
        }
    }
}
