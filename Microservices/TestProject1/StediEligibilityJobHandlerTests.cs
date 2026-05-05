using ClearingHouseService.Web.Interface;
using ClearingHouseService.Web.Service.Handler;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Dtos.ClearingHouse;

namespace TestProject1
{
    public class StediEligibilityJobHandlerTests
    {
        [Fact]
        public async Task HandleAsync_CallsProcessor_WithProvidedJobAndToken_AndLogs()
        {
            // Arrange
            var mockProcessor = new Mock<IStediEligibilityProcessor>();
            var mockLogger = new Mock<ILogger<StediEligibilityJobHandler>>();

            mockProcessor
                .Setup(p => p.ProcessAsync(It.IsAny<StediEligibilityJobDTO>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var handler = new StediEligibilityJobHandler(mockProcessor.Object, mockLogger.Object);

            var job = new StediEligibilityJobDTO
            {
                CorrelationId = Guid.NewGuid(),
                FunderId = 100,
                MemberId = 200
            };

            using var cts = new CancellationTokenSource();
            var token = cts.Token;

            // Act
            await handler.HandleAsync(job, token);

            // Assert
            mockProcessor.Verify(p => p.ProcessAsync(job, token), Times.Once);

            // Verify logger was used for start and completion messages
            mockLogger.Verify(l => l.Log(
                Microsoft.Extensions.Logging.LogLevel.Information,
                It.IsAny<Microsoft.Extensions.Logging.EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Calling STEDI eligibilityHandler")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

            mockLogger.Verify(l => l.Log(
                Microsoft.Extensions.Logging.LogLevel.Information,
                It.IsAny<Microsoft.Extensions.Logging.EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("STEDI eligibility job completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_PropagatesException_WhenProcessorThrows()
        {
            // Arrange
            var mockProcessor = new Mock<IStediEligibilityProcessor>();
            var mockLogger = new Mock<ILogger<StediEligibilityJobHandler>>();

            mockProcessor
                .Setup(p => p.ProcessAsync(It.IsAny<StediEligibilityJobDTO>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("processor failed"));

            var handler = new StediEligibilityJobHandler(mockProcessor.Object, mockLogger.Object);

            var job = new StediEligibilityJobDTO
            {
                CorrelationId = Guid.NewGuid(),
                FunderId = 1,
                MemberId = 2
            };

            using var cts = new CancellationTokenSource();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(job, cts.Token));

            mockProcessor.Verify(p => p.ProcessAsync(job, cts.Token), Times.Once);
        }
    }
}
