using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClearingHouseService.Web.Controllers;
using ClearingHouseService.Web.Interface;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ClearingHouse
{
    public class ClearingHouseReferenceDataControllerTests
    {
        [Fact]
        public async Task GetPayersAsync_ReturnsFileStreamResult_WithExpectedContent()
        {
            // Arrange
            var mockService = new Mock<IClearingHouseReferenceDataProvider>();
            var expectedCsv = "payer1,payer2";
            mockService.Setup(s => s.GetPayersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedCsv);

            var controller = new ClearingHouseReferenceDataController(mockService.Object);

            // Act
            var result = await controller.GetPayersAsync("stedi", CancellationToken.None);

            // Assert
            var fileResult = Assert.IsType<FileStreamResult>(result);
            Assert.Equal("text/csv", fileResult.ContentType);
            Assert.Equal("stedi-payers.csv", fileResult.FileDownloadName);

            using var reader = new StreamReader(fileResult.FileStream, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            Assert.Equal(expectedCsv, content);
        }

        [Fact]
        public async Task GetEnrollments_ReturnsNotFoundResult()
        {
            // Arrange
            var mockService = new Mock<IClearingHouseReferenceDataProvider>();
            var controller = new ClearingHouseReferenceDataController(mockService.Object);

            // Act
            var result = await controller.GetEnrollments("stedi", CancellationToken.None);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}