using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Moq;
using Rethink.Services.Common.Factories;
using Rethink.Services.Domain.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Common
{
    public class BlobProcessingServiceTests
    {
        private readonly Mock<IBlobConnectionFactory> _mockConnectionFactory;
        private readonly BlobProcessingService _service;

        public BlobProcessingServiceTests()
        {
            _mockConnectionFactory = new Mock<IBlobConnectionFactory>();
            _service = new BlobProcessingService(_mockConnectionFactory.Object);
        }

        [Fact]
        public async Task CreateBlobContainerAsync_CallsCreateBlobContainerAsync()
        {
            // Arrange
            var containerName = "test-container";
            var mockBlobClient = CreateMockBlobServiceClient();
            _mockConnectionFactory.Setup(f => f.CreateBlobClient())
                .Returns(mockBlobClient.Object);

            // Act
            var result = await _service.CreateBlobContainerAsync(containerName);

            // Assert
            Assert.NotNull(result);
            _mockConnectionFactory.Verify(f => f.CreateBlobClient(), Times.Once);

        }

        [Fact]
        public async Task UploadIntoContainerAsync_UploadsBlob()
        {
            // Arrange
            var containerName = "test-container";
            var fileName = "test-file.txt";
            var stream = new MemoryStream(new byte[] { 1, 2, 3 });

            var mockBlobServiceClient = new Mock<BlobServiceClient>();
            var mockContainerClient = CreateMockContainerClient();
            var mockBlobClient = CreateMockBlobClient(containerName);

            mockBlobServiceClient.Setup(c => c.GetBlobContainerClient(containerName))
                .Returns(mockContainerClient.Object);
            mockContainerClient.Setup(c => c.GetBlobClient(fileName))
                .Returns(mockBlobClient.Object);

            _mockConnectionFactory.Setup(f => f.CreateBlobClient())
                .Returns(mockBlobServiceClient.Object);

            // Act
            var result = await _service.UploadIntoContainerAsync(containerName, fileName, stream);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DownloadBlobFromContainerAsync_ShouldReturnCorrectStream()
        {
            // Arrange
            var containerName = "test-container";
            var fileName = "file.txt";
            var fileContent = "Hello World";

            var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
            inputStream.Position = 0;

            var factoryMock = new Mock<IBlobConnectionFactory>();
            var serviceClientMock = new Mock<BlobServiceClient>();
            var containerClientMock = new Mock<BlobContainerClient>();
            var blobClientMock = new Mock<BlobClient>();

            factoryMock.Setup(x => x.CreateBlobClient())
                       .Returns(serviceClientMock.Object);

            serviceClientMock.Setup(x => x.GetBlobContainerClient(containerName.ToLowerInvariant()))
                             .Returns(containerClientMock.Object);

            containerClientMock.Setup(x => x.GetBlobClient(fileName))
                               .Returns(blobClientMock.Object);

            var blobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(
                content: inputStream,
                contentType: "text/plain"
            );

            var response = Response.FromValue(blobDownloadInfo, new Mock<Response>().Object);

            blobClientMock.Setup(x => x.DownloadAsync())
            .ReturnsAsync(response); 
                
            var service = new BlobProcessingService(factoryMock.Object);

            // Act
            var result = await service.DownloadBlobFromContainerAsync(containerName, fileName);

            result.Position = 0;
            var resultText = await new StreamReader(result).ReadToEndAsync();

            // Assert
            Assert.Equal(fileContent, resultText);
        }

        [Fact]
        public async Task DeleteBlobFromContainerAsync_DeletesBlob()
        {
            // Arrange
            var containerName = "test-container";
            var fileName = "test-file.txt";
            var mockBlobClient = CreateMockBlobServiceClient();
            var mockContainerClient = CreateMockContainerClient();

            _mockConnectionFactory.Setup(f => f.CreateBlobClient()).Returns(mockBlobClient.Object);
            mockBlobClient.Setup(c => c.GetBlobContainerClient(containerName)).Returns(mockContainerClient.Object);
            mockContainerClient.Setup(c => c.GetBlobClient(fileName)).Returns(CreateMockBlobClient(fileName).Object);

            // Act
            await _service.DeleteBlobFromContainerAsync(containerName, fileName);

            // Assert
            mockContainerClient.Verify(c => c.GetBlobClient(fileName), Times.Once);
        }

        [Fact]
        public async Task DeleteContainerAsync_CallsDeleteBlobContainerAsync()
        {
            // Arrange
            var containerName = "test-container";
            var mockBlobClient = CreateMockBlobServiceClient();
            _mockConnectionFactory.Setup(f => f.CreateBlobClient()).Returns(mockBlobClient.Object);

            // Act
            var result = await _service.DeleteContainerAsync(containerName);

            // Assert
            Assert.Null(result);
            mockBlobClient.Verify(c => c.DeleteBlobContainerAsync(containerName, null, default), Times.Once);
        }


        private Mock<BlobServiceClient> CreateMockBlobServiceClient()
        {
            var mock = new Mock<BlobServiceClient>();

            var mockResponse = new Mock<Response<BlobContainerClient>>();
            mock.Setup(c => c.CreateBlobContainerAsync(
                    It.IsAny<string>(),
                    It.IsAny<PublicAccessType>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<CancellationToken>()
                ))
                .ReturnsAsync(mockResponse.Object);

            return mock;
        }


        private Mock<BlobContainerClient> CreateMockContainerClient()
        {
            var mock = new Mock<BlobContainerClient>();

            mock.Setup(c => c.CreateIfNotExistsAsync(
                    It.IsAny<PublicAccessType>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<BlobContainerEncryptionScopeOptions>(),
                    It.IsAny<CancellationToken>()
                ))
                .ReturnsAsync((Response<BlobContainerInfo>)null);

            return mock;
        }

        private Mock<BlobClient> CreateMockBlobClient(string blobName)
        {
            var mock = new Mock<BlobClient>();
            return mock;
        }
    }

}
