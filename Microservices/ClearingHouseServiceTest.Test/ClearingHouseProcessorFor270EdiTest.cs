using ClearingHouseService.Web.Interface;
using ClearingHouseService.Web.Service;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Models.EligibilityRequest;

namespace ClearingHouseService.Test
{
    public class ClearingHouseProcessorFor270EdiTest
    {
        private readonly Mock<ICommon> _mockCommonService;
        private readonly Mock<ILogger<ClearingHouseProcessorFor270Edi>> _mockLogger;
        private readonly ClearingHouseProcessorFor270Edi _processor;

        public ClearingHouseProcessorFor270EdiTest()
        {
            _mockCommonService = new Mock<ICommon>();
            _mockLogger = new Mock<ILogger<ClearingHouseProcessorFor270Edi>>();
            _processor = new ClearingHouseProcessorFor270Edi(_mockCommonService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Generate270EDIData_Success_ReturnsTrueResult()
        {
            // Arrange
            var eligibility270Request = new Eligibility270Request
            {
                FunderId = 1234,
                ClearingHouseId = 5678
            };

            var expectedResult = "validEdiData"; // Mocked result from Generate270EDIData
            _mockCommonService.Setup(x => x.Generate270EDIData(It.IsAny<Eligibility270Request>()))
                .ReturnsAsync((true, expectedResult));

            // Act
            var result = await _processor.Generate270EDIData(eligibility270Request);

            // Assert
            Assert.True(result.success);
            Assert.Equal(expectedResult, result.result);
        }

        [Fact]
        public async Task Generate270EDIData_Exception_ReturnsFalseAndLogsError()
        {
            // Arrange
            var eligibility270Request = new Eligibility270Request
            {
                FunderId = 1234,
                ClearingHouseId = 5678
            };

            var exceptionMessage = "Some error occurred";
            _mockCommonService.Setup(x => x.Generate270EDIData(It.IsAny<Eligibility270Request>()))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            var result = await _processor.Generate270EDIData(eligibility270Request);

            // Assert
            Assert.False(result.success);
            Assert.Equal(exceptionMessage, result.result);
        }
    }
}
