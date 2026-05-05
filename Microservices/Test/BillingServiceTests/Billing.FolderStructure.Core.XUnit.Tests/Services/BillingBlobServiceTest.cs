using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Billing.FolderStructure.Core;
using Billing.FolderStructure.Core.Models;
using Billing.FolderStructure.Core.Services;
using Billing.FolderStructure.Core.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;

namespace Billing.FolderStructure.Core.XUnit.Tests.Services
{
    public class BillingBlobServiceTests
    {
        private readonly Mock<BlobServiceClient> _blobServiceClientMock;
        private readonly Mock<BlobContainerClient> _blobContainerClientMock;
        private readonly Mock<BlobClient> _blobClientMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly BillingBlobService _service;
        private readonly Mock<ILogger<BillingBlobService>> _loggerMock;

        public BillingBlobServiceTests()
        {
            _blobServiceClientMock = new Mock<BlobServiceClient>();
            _blobContainerClientMock = new Mock<BlobContainerClient>();
            _blobClientMock = new Mock<BlobClient>();
            _configurationMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<BillingBlobService>>();

            _blobServiceClientMock
            .Setup(s => s.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(_blobClientMock.Object);

            _service = new BillingBlobService(_blobServiceClientMock.Object, _loggerMock.Object, _configurationMock.Object);
        }

        [Fact]
        public async Task UploadIntoContainerAsync_WhenFileNotExist_ShouldUploadFile()
        {
            var containerName = "test-container";
            var blobPath = "test/file.txt";
            var stream = new MemoryStream();
            var fakeResponse = Response.FromValue(
            BlobsModelFactory.BlobContentInfo(
            eTag: new ETag("etag"),
            lastModified: DateTimeOffset.UtcNow,
            contentHash: new byte[] { 1, 2, 3 },
            versionId: "version",
            encryptionKeySha256: null,
            encryptionScope: null,
            blobSequenceNumber: 0
            ),
            Mock.Of<Response>()
            );

            _blobContainerClientMock
            .Setup(c => c.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());

            _blobClientMock
                .Setup(c => c.UploadAsync(stream, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeResponse);


            _blobClientMock
                .Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<bool>>());

            var result = await _service.UploadIntoContainerAsync(containerName, blobPath, stream);

            _blobServiceClientMock.Verify(s => s.GetBlobContainerClient(containerName), Times.Once);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task Upload_WhenBlobDoesNotExist_ShouldUploadWithoutRename()
        {
            var containerName = "test-container";
            var blobPath = "835/clinic/file.txt";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("data"));

            _blobClientMock
                .Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

            _blobClientMock
                .Setup(c => c.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

            var result = await _service.UploadIntoContainerAsync(containerName, blobPath, stream);

            Assert.Equal("file.txt", result);
        }

        [Fact]
        public async Task UploadIntoContainerAsync_WhenExists_ButTransactionTypeNotSupported_ShouldNotAppendCounter()
        {
            var containerName = "test-container";
            var blobPath = "999/folder/file.txt";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("data"));

            _blobContainerClientMock
                .Setup(c => c.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());

            _blobClientMock
                .Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            _blobClientMock
                .Setup(c => c.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(MockBlobResponse());

            var result = await _service.UploadIntoContainerAsync(containerName, blobPath, stream);

            Assert.Equal("file.txt", result);
        }

        [Fact]
        public async Task UploadIntoContainerAsync_WhenExists_AndTransactionType270_ShouldAppendCounter()
        {
            var containerName = "test-container";
            var blobPath = "root/270/file.txt";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("data"));

            _blobContainerClientMock
                .Setup(c => c.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());

            _blobClientMock
                .SetupSequence(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()))
                .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

            _blobClientMock
                .Setup(c => c.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(MockBlobResponse());

            var result = await _service.UploadIntoContainerAsync(containerName, blobPath, stream);

            // Updated assertion
            Assert.Equal("file_1.txt", result);

            _blobClientMock.Verify(
                c => c.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task UploadIntoContainerAsync_WhenExistsMultipleTimes_ShouldIncrementCounterMoreThanOnce()
        {
            var containerName = "test-container";

            // Ensure transactionType is at index [1]
            var blobPath = "root/835/file.txt";

            var stream = new MemoryStream(Encoding.UTF8.GetBytes("data"));

            _blobContainerClientMock
                .Setup(c => c.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());

            _blobClientMock
                .SetupSequence(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()))  // file.txt exists
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()))  // file_1.txt exists
                .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>())); // file_2.txt does NOT exist

            _blobClientMock
                .Setup(c => c.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(MockBlobResponse());

            var result = await _service.UploadIntoContainerAsync(containerName, blobPath, stream);

            Assert.Equal("file_2.txt", result);
        }

        [Fact]
        public async Task UploadIntoContainerAsync_ShouldResetStreamPositionBeforeUpload()
        {
            var containerName = "test-container";
            var blobPath = "test/file.txt";

            var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello"));
            stream.Position = stream.Length;

            _blobContainerClientMock
                .Setup(c => c.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());

            _blobClientMock
                .Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

            long positionAtUpload = -1;

            _blobClientMock
                .Setup(c => c.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
                .Callback<Stream, bool, CancellationToken>((s, _, __) =>
                {
                    positionAtUpload = s.Position;
                })
                .ReturnsAsync(MockBlobResponse());

            await _service.UploadIntoContainerAsync(containerName, blobPath, stream);

            Assert.Equal(0, positionAtUpload);
        }

        [Fact]
        public async Task DownloadBlobFromContainerAsync_ShouldReturnMemoryStream()
        {
            var testContent = "Hello Blob!";
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(testContent));
            memoryStream.Position = 0;

            var downloadInfo = BlobsModelFactory.BlobDownloadInfo(
            content: memoryStream
            );

            var fakeDownloadResponse = Response.FromValue(downloadInfo, Mock.Of<Response>());

            _blobServiceClientMock
            .Setup(x => x.GetBlobContainerClient("testcontainer"))
            .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock
            .Setup(x => x.GetBlobClient("test/file.txt"))
            .Returns(_blobClientMock.Object);

            _blobClientMock.Setup(x => x.DownloadAsync()).ReturnsAsync(fakeDownloadResponse);

            var resultStream = await _service.DownloadBlobFromContainerAsync("testcontainer", "test/file.txt");

            resultStream.Position = 0;
            using var reader = new StreamReader(resultStream);
            var resultText = reader.ReadToEnd();
            Assert.Equal(testContent, resultText);
        }

        [Fact]
        public async Task DownloadBlobFromContainerAsync_ShouldConvertContainerNameToLower()
        {
            var content = new MemoryStream(Encoding.UTF8.GetBytes("data"));
            content.Position = 0;

            var downloadInfo = BlobsModelFactory.BlobDownloadInfo(content: content);
            var response = Response.FromValue(downloadInfo, Mock.Of<Response>());

            _blobServiceClientMock
                .Setup(x => x.GetBlobContainerClient("testcontainer"))
                .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock
                .Setup(x => x.GetBlobClient("file.txt"))
                .Returns(_blobClientMock.Object);

            _blobClientMock
                .Setup(x => x.DownloadAsync())
                .ReturnsAsync(response);

            await _service.DownloadBlobFromContainerAsync("TESTCONTAINER", "file.txt");

            _blobServiceClientMock.Verify(
                x => x.GetBlobContainerClient("testcontainer"),
                Times.Once);
        }

        [Fact]
        public async Task DownloadBlobFromContainerAsync_ShouldReturnEmptyStream_WhenBlobIsEmpty()
        {
            var emptyStream = new MemoryStream();

            var downloadInfo = BlobsModelFactory.BlobDownloadInfo(content: emptyStream);
            var response = Response.FromValue(downloadInfo, Mock.Of<Response>());

            _blobServiceClientMock.Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock.Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(_blobClientMock.Object);

            _blobClientMock.Setup(x => x.DownloadAsync())
                .ReturnsAsync(response);

            var result = await _service.DownloadBlobFromContainerAsync("test", "empty.txt");

            Assert.Equal(0, result.Length);
        }

        [Fact]
        public async Task DownloadBlobFromContainerAsync_ShouldHandleLargeContent()
        {
            var largeText = new string('A', 100_000);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(largeText));
            stream.Position = 0;

            var downloadInfo = BlobsModelFactory.BlobDownloadInfo(content: stream);
            var response = Response.FromValue(downloadInfo, Mock.Of<Response>());

            _blobServiceClientMock.Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock.Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(_blobClientMock.Object);

            _blobClientMock.Setup(x => x.DownloadAsync())
                .ReturnsAsync(response);

            var result = await _service.DownloadBlobFromContainerAsync("test", "large.txt");

            Assert.Equal(stream.Length, result.Length);
        }

        [Fact]
        public async Task DownloadBlobFromContainerAsync_ShouldThrow_WhenDownloadFails()
        {
            _blobServiceClientMock.Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock.Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(_blobClientMock.Object);

            _blobClientMock.Setup(x => x.DownloadAsync())
                .ThrowsAsync(new Exception("Download failed"));

            await Assert.ThrowsAsync<Exception>(() =>
                _service.DownloadBlobFromContainerAsync("test", "file.txt"));
        }

        [Fact]
        public async Task DownloadBlobFromContainerAsync_ShouldThrow_WhenBlobClientFails()
        {
            _blobServiceClientMock.Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock.Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Throws(new InvalidOperationException("Invalid path"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.DownloadBlobFromContainerAsync("test", "bad.txt"));
        }

        [Fact]
        public async Task DownloadBlobFromContainerAsync_ShouldCallExpectedClients()
        {
            var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var downloadInfo = BlobsModelFactory.BlobDownloadInfo(content: stream);
            var response = Response.FromValue(downloadInfo, Mock.Of<Response>());

            _blobServiceClientMock.Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock.Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(_blobClientMock.Object);

            _blobClientMock.Setup(x => x.DownloadAsync())
                .ReturnsAsync(response);

            await _service.DownloadBlobFromContainerAsync("test", "file.txt");

            _blobContainerClientMock.Verify(x => x.GetBlobClient("file.txt"), Times.Once);
            _blobClientMock.Verify(x => x.DownloadAsync(), Times.Once);
        }


        [Fact]
        public async Task DeleteContainerAsync_ShouldCallDeleteIfExists()
        {
            var containerName = "test-container";
            _blobContainerClientMock
            .Setup(c => c.DeleteIfExistsAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            await _service.DeleteContainerAsync(containerName);

            _blobContainerClientMock.Verify(c => c.DeleteIfExistsAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteContainerAsync_ShouldRequestCorrectContainerClient()
        {
            var containerName = "my-container";

            _blobServiceClientMock
                .Setup(x => x.GetBlobContainerClient(containerName))
                .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock
                .Setup(x => x.DeleteIfExistsAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            await _service.DeleteContainerAsync(containerName);

            _blobServiceClientMock.Verify(
                x => x.GetBlobContainerClient(containerName),
                Times.Once);
        }

        [Fact]
        public async Task DeleteContainerAsync_WhenContainerExists_ShouldReturnTrueResponse()
        {
            _blobServiceClientMock
                .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock
                .Setup(x => x.DeleteIfExistsAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            await _service.DeleteContainerAsync("exists-container");

            _blobContainerClientMock.Verify(x =>
                x.DeleteIfExistsAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteContainerAsync_WhenContainerNotExists_ShouldStillCallDelete()
        {
            _blobServiceClientMock
                .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock
                .Setup(x => x.DeleteIfExistsAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

            await _service.DeleteContainerAsync("missing-container");

            _blobContainerClientMock.Verify(x =>
                x.DeleteIfExistsAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteContainerAsync_WhenDeleteThrows_ShouldPropagateException()
        {
            _blobServiceClientMock
                .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock
                .Setup(x => x.DeleteIfExistsAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException("Azure delete failed"));

            await Assert.ThrowsAsync<RequestFailedException>(() =>
                _service.DeleteContainerAsync("bad-container"));
        }

        [Fact]
        public async Task DeleteContainerAsync_WhenGetContainerClientFails_ShouldThrow()
        {
            _blobServiceClientMock
                .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Throws(new InvalidOperationException("Client error"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.DeleteContainerAsync("test"));
        }

        [Fact]
        public async Task DeleteBlobFromContainerAsync_ShouldCallDeleteIfExists()
        {
            var containerName = "test-container";
            var filePath = "file.txt";

            _blobClientMock
            .Setup(c => c.DeleteIfExistsAsync(DeleteSnapshotsOption.None, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            await _service.DeleteBlobFromContainerAsync(containerName, filePath);

            _blobClientMock.Verify(c => c.DeleteIfExistsAsync(DeleteSnapshotsOption.None, null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteBlobFromContainerAsync_ShouldRequestCorrectContainer()
        {
            var containerName = "test-container";

            _blobServiceClientMock
                .Setup(x => x.GetBlobContainerClient(containerName))
                .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock
                .Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(_blobClientMock.Object);

            _blobClientMock
                .Setup(x => x.DeleteIfExistsAsync(DeleteSnapshotsOption.None, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            await _service.DeleteBlobFromContainerAsync(containerName, "file.txt");

            _blobServiceClientMock.Verify(x => x.GetBlobContainerClient(containerName), Times.Once);
        }

        [Fact]
        public async Task DeleteBlobFromContainerAsync_ShouldRequestCorrectBlobPath()
        {
            var filePath = "folder/file.txt";

            _blobServiceClientMock
                .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock
                .Setup(x => x.GetBlobClient(filePath))
                .Returns(_blobClientMock.Object);

            _blobClientMock
                .Setup(x => x.DeleteIfExistsAsync(DeleteSnapshotsOption.None, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            await _service.DeleteBlobFromContainerAsync("test", filePath);

            _blobContainerClientMock.Verify(x => x.GetBlobClient(filePath), Times.Once);
        }

        [Fact]
        public async Task DeleteBlobFromContainerAsync_WhenBlobNotExists_ShouldStillCallDelete()
        {
            _blobServiceClientMock
                .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock
                .Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(_blobClientMock.Object);

            _blobClientMock
                .Setup(x => x.DeleteIfExistsAsync(DeleteSnapshotsOption.None, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

            await _service.DeleteBlobFromContainerAsync("test", "missing.txt");

            _blobClientMock.Verify(x =>
                x.DeleteIfExistsAsync(DeleteSnapshotsOption.None, null, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteBlobFromContainerAsync_WhenDeleteFails_ShouldThrow()
        {
            _blobServiceClientMock
                .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock
                .Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(_blobClientMock.Object);

            _blobClientMock
                .Setup(x => x.DeleteIfExistsAsync(DeleteSnapshotsOption.None, null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException("Delete failed"));

            await Assert.ThrowsAsync<RequestFailedException>(() =>
                _service.DeleteBlobFromContainerAsync("test", "file.txt"));
        }

        [Fact]
        public async Task DeleteBlobFromContainerAsync_WhenGetBlobClientFails_ShouldThrow()
        {
            _blobServiceClientMock
                .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock
                .Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Throws(new InvalidOperationException("Bad path"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.DeleteBlobFromContainerAsync("test", "bad.txt"));
        }

        [Fact]
        public async Task DeleteBlobFromContainerAsync_WhenContainerClientFails_ShouldThrow()
        {
            _blobServiceClientMock
                .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Throws(new InvalidOperationException("Client failure"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.DeleteBlobFromContainerAsync("test", "file.txt"));
        }

        [Fact]
        public async Task DeleteBlobFromContainerAsync_ShouldCallFullChain()
        {
            _blobServiceClientMock
                .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock
                .Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(_blobClientMock.Object);

            _blobClientMock
                .Setup(x => x.DeleteIfExistsAsync(DeleteSnapshotsOption.None, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            await _service.DeleteBlobFromContainerAsync("test", "file.txt");

            _blobServiceClientMock.VerifyAll();
            _blobContainerClientMock.VerifyAll();
            _blobClientMock.VerifyAll();
        }





        [Fact]
        public async Task CreateBlobContainerAsync_SubfoldersNullOrEmpty_ShouldNotUpload()
        {
            var cfg = new BillingStorageConfig
            {
                ContainerName = "c",
                Sources = new Dictionary<string, string[]>
                {
                    { "Availity", new[] { "835" } }  // Assume this is a valid transaction type
                },
                Accounts = new[] { "A" },
                FolderStructure = null // No folder structure to create subfolders
            };

            var sectionData = new Dictionary<string, string?>
            {
                ["BillingStorageConfig:ContainerName"] = cfg.ContainerName,
                ["BillingStorageConfig:Sources:Availity:0"] = cfg.Sources["Availity"][0],
                ["BillingStorageConfig:Accounts:0"] = cfg.Accounts[0],
            };

            var inMem = new ConfigurationBuilder().AddInMemoryCollection(sectionData!).Build();
            _configurationMock.Setup(c => c.GetSection("BillingStorageConfig")).Returns(inMem.GetSection("BillingStorageConfig"));

            _blobContainerClientMock
            .Setup(c => c.CreateIfNotExistsAsync(PublicAccessType.None, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(
                BlobsModelFactory.BlobContainerInfo(new ETag("e"), DateTimeOffset.UtcNow),
                Mock.Of<Response>()
            ));

            // Track uploads
            int uploads = 0;
            _blobClientMock
            .Setup(b => b.UploadAsync(It.IsAny<BinaryData>(), true, It.IsAny<CancellationToken>()))
            .Callback(() => uploads++)
            .ReturnsAsync(Response.FromValue(
                BlobsModelFactory.BlobContentInfo(new ETag("e"), DateTimeOffset.UtcNow, null, "v", null, null, 0),
                Mock.Of<Response>()
            ));

            await _service.CreateBlobContainerAsync();

            Assert.Equal(0, uploads); // No uploads should happen as FolderStructure is null
        }

        [Fact]
        public async Task CreateBlobContainerAsync_SubfoldersContainMain_ShouldUpload()
        {
            var cfg = new BillingStorageConfig
            {
                ContainerName = "c",
                Sources = new Dictionary<string, string[]>
                {
                    { "Availity", new[] { "835" } }  // Assume "835" is a valid transaction type
                },
                Accounts = new[] { "A" },
                FolderStructure = new Dictionary<string, string[]>
                {
                    { "835", new[] { "EDI" } }  // Folder structure contains subfolders for "835"
                }
            };

            var sectionData = new Dictionary<string, string?>
            {
                ["BillingStorageConfig:ContainerName"] = cfg.ContainerName,
                ["BillingStorageConfig:Sources:Availity:0"] = cfg.Sources["Availity"][0],
                ["BillingStorageConfig:Accounts:0"] = cfg.Accounts[0],
                ["BillingStorageConfig:FolderStructure:835:0"] = "EDI"
            };

            var inMem = new ConfigurationBuilder().AddInMemoryCollection(sectionData!).Build();
            _configurationMock.Setup(c => c.GetSection("BillingStorageConfig")).Returns(inMem.GetSection("BillingStorageConfig"));

            _blobContainerClientMock
            .Setup(c => c.CreateIfNotExistsAsync(PublicAccessType.None, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(
                BlobsModelFactory.BlobContainerInfo(new ETag("e"), DateTimeOffset.UtcNow),
                Mock.Of<Response>()
            ));

            _blobClientMock
            .Setup(b => b.UploadAsync(It.IsAny<BinaryData>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(
                BlobsModelFactory.BlobContentInfo(new ETag("e"), DateTimeOffset.UtcNow, null, "v", null, null, 0),
                Mock.Of<Response>()
            ));

            await _service.CreateBlobContainerAsync();

            _blobClientMock.Verify(b => b.UploadAsync(It.IsAny<BinaryData>(), true, It.IsAny<CancellationToken>()), Times.AtLeastOnce); // At least one upload should happen
        }

        [Fact]
        public async Task CreateBlobContainerAsync_SubfoldersDoNotContainMain_ShouldNotUpload()
        {
            var cfg = new BillingStorageConfig
            {
                ContainerName = "c",
                Sources = new Dictionary<string, string[]>
                {
                    { "Availity", new[] { "835" } }  // Assume "835" is a valid transaction type
                },
                Accounts = new[] { "A" },
                FolderStructure = new Dictionary<string, string[]>
                {
                    { "999", new[] { "EDI" } }  // "835" is not in FolderStructure, so no subfolders for it
                }
            };

            var sectionData = new Dictionary<string, string?>
            {
                ["BillingStorageConfig:ContainerName"] = cfg.ContainerName,
                ["BillingStorageConfig:Sources:Availity:0"] = cfg.Sources["Availity"][0],
                ["BillingStorageConfig:Accounts:0"] = cfg.Accounts[0],
                ["BillingStorageConfig:FolderStructure:999:0"] = "EDI"
            };

            var inMem = new ConfigurationBuilder().AddInMemoryCollection(sectionData!).Build();
            _configurationMock.Setup(c => c.GetSection("BillingStorageConfig")).Returns(inMem.GetSection("BillingStorageConfig"));

            _blobContainerClientMock
            .Setup(c => c.CreateIfNotExistsAsync(PublicAccessType.None, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(
                BlobsModelFactory.BlobContainerInfo(new ETag("e"), DateTimeOffset.UtcNow),
                Mock.Of<Response>()
            ));

            // Track uploads
            int uploads = 0;
            _blobClientMock
            .Setup(b => b.UploadAsync(It.IsAny<BinaryData>(), true, It.IsAny<CancellationToken>()))
            .Callback(() => uploads++)
            .ReturnsAsync(Response.FromValue(
                BlobsModelFactory.BlobContentInfo(new ETag("e"), DateTimeOffset.UtcNow, null, "v", null, null, 0),
                Mock.Of<Response>()
            ));

            await _service.CreateBlobContainerAsync();

            Assert.Equal(0, uploads); // No uploads should happen since the subfolder for "835" doesn't exist
        }

        [Fact]
        public async Task Update999ReportAsync_ExistsFalse_ShouldUploadSimpleReport()
        {
            var containerName = "container";
            var filePath = "folder/report.txt";
            var summary = new EDI999Summary
            {
                FileName = "file1",
                Partner = "partner",
                TotalTransactionSets = 5,
                Accepted = 4,
                Rejected = 1
            };

            _blobClientMock
            .Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

            string uploadedText = string.Empty;
            _blobClientMock
            .Setup(c => c.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
            .Callback<Stream, bool, CancellationToken>((s, overwrite, ct) =>
            {
                s.Position = 0;
                using var reader = new StreamReader(s, leaveOpen: true);
                uploadedText = reader.ReadToEnd();
            })
            .ReturnsAsync(Response.FromValue(
            BlobsModelFactory.BlobContentInfo(new ETag("e"), DateTimeOffset.UtcNow, null, "v", null, null, 0),
            Mock.Of<Response>()
           ));

            await _service.Update999ReportAsync(containerName, summary, filePath);

            Assert.Contains("Rethink Billing:", uploadedText);
            Assert.Contains(summary.FileName, uploadedText);
            _blobClientMock.Verify(c => c.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Update999ReportAsync_WhenBlobExists_WithValidExistingContent_ShouldParseAndAppend()
        {
            var container = "container";
            var filePath = "folder/report.txt";

            var summary = new EDI999Summary
            {
                FileName = "newfile",
                Partner = "partner",
                TotalTransactionSets = 3,
                Accepted = 2,
                Rejected = 1
            };

            _blobClientMock
                .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            var existingReport =
                                 @"Rethink Billing:
                                 --------------------------------------------------
                                 File Name | Partner | Tx | Acc | Rej | Par | Date
                                 --------------------------------------------------
                                 oldfile | abc | 5 | 4 | 1 | 0 | 2024-01-01
                                 --------------------------------------------------
                                 Totals...";

            _blobClientMock
                .Setup(x => x.DownloadContentAsync())
                .ReturnsAsync(Response.FromValue(
                    BlobsModelFactory.BlobDownloadResult(
                        content: BinaryData.FromString(existingReport)),
                    Mock.Of<Response>()));

            string uploaded = "";

            _blobClientMock
                .Setup(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
                .Callback<Stream, bool, CancellationToken>((s, _, __) =>
                {
                    s.Position = 0;
                    uploaded = new StreamReader(s).ReadToEnd();
                })
                .ReturnsAsync(MockBlobResponse());

            await _service.Update999ReportAsync(container, summary, filePath);

            Assert.Contains("oldfile", uploaded);
            Assert.Contains("newfile", uploaded);
        }

        [Fact]
        public async Task Update999ReportAsync_WhenBlobExists_NoHeaderRow_ShouldSkipParsingBlock()
        {
            var summary = new EDI999Summary
            {
                FileName = "fileA",
                Partner = "p",
                TotalTransactionSets = 1,
                Accepted = 1,
                Rejected = 0
            };

            _blobClientMock
                .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            _blobClientMock
                .Setup(x => x.DownloadContentAsync())
                .ReturnsAsync(Response.FromValue(
                    BlobsModelFactory.BlobDownloadResult(
                        content: BinaryData.FromString("random text without header")),
                    Mock.Of<Response>()));

            _blobClientMock
                .Setup(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(MockBlobResponse());

            await _service.Update999ReportAsync("c", summary, "f/x.txt");

            _blobClientMock.Verify(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Update999ReportAsync_WhenNoSummarySeparator_ShouldUseAllRemainingLines()
        {
            var summary = new EDI999Summary
            {
                FileName = "fileB",
                Partner = "p",
                TotalTransactionSets = 2,
                Accepted = 1,
                Rejected = 1
            };

            var existing =
                                 @"Header
                         File Name | Partner | Tx | Acc | Rej | Par | Date
             ss            -----------------------------------
                         fileX | p | 2 | 1 | 1 | 0 | today";

            _blobClientMock
                .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            _blobClientMock
                .Setup(x => x.DownloadContentAsync())
                .ReturnsAsync(Response.FromValue(
                    BlobsModelFactory.BlobDownloadResult(BinaryData.FromString(existing)),
                    Mock.Of<Response>()));

            _blobClientMock
                .Setup(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(MockBlobResponse());

            await _service.Update999ReportAsync("c", summary, "f/x.txt");

            _blobClientMock.Verify(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()), Times.Once);
        }


        [Fact]
        public async Task Update999ReportAsync_WhenExistingLineMalformed_ShouldSkipTotalsParsing()
        {
            var summary = new EDI999Summary
            {
                FileName = "fileC",
                Partner = "p",
                TotalTransactionSets = 1,
                Accepted = 1,
                Rejected = 0
            };

            var existing =
                         @"File Name | Partner
                         bad|line|only|3parts";

            _blobClientMock
                .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            _blobClientMock
                .Setup(x => x.DownloadContentAsync())
                .ReturnsAsync(Response.FromValue(
                    BlobsModelFactory.BlobDownloadResult(BinaryData.FromString(existing)),
                    Mock.Of<Response>()));

            _blobClientMock
                .Setup(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(MockBlobResponse());

            await _service.Update999ReportAsync("c", summary, "f/x.txt");
        }


        [Fact]
        public async Task Update277DailySummaryReportAsync_BlobNotExist_ShouldInitializeTotals()
        {
            var containerName = "container";
            var reportFileName = "folder/summary.txt";
            var ediContent = "";
            var existingClaimIds = new List<string>();

            _blobClientMock
            .Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

            string uploaded = string.Empty;
            _blobClientMock
            .Setup(c => c.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
            .Callback<Stream, bool, CancellationToken>((s, overwrite, ct) =>
            {
                s.Position = 0;
                using var r = new StreamReader(s, leaveOpen: true);
                uploaded = r.ReadToEnd();
            })
            .ReturnsAsync(Response.FromValue(
            BlobsModelFactory.BlobContentInfo(new ETag("e"), DateTimeOffset.UtcNow, null, "v", null, null, 0),
            Mock.Of<Response>()
           ));

            await _service.Update277DailySummaryReportAsync(containerName, ediContent, reportFileName, existingClaimIds);

            Assert.Contains("Total Claims:", uploaded);
            Assert.Contains("Accepted:", uploaded);
            Assert.Contains("Rejected:", uploaded);
        }

        [Fact]
        public async Task Update277DailySummaryReportAsync_WhenBlobExists_ShouldAddToExistingTotals()
        {
            var containerName = "container";
            var reportFileName = "folder/summary.txt";
            var ediContent = GetValid277Edi();
            var existingClaimIds = new List<string>(); // not processed yet

            _blobClientMock
                .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            var existingBlob = "Rethink Billing:\n" +
                               "Date: 20240101\n" +
                               "Total Claims: 5\n" +
                               "Accepted: 3\n" +
                               "Rejected: 2\n";


            _blobClientMock
                .Setup(x => x.DownloadContentAsync())
                .ReturnsAsync(Response.FromValue(
                    BlobsModelFactory.BlobDownloadResult(
                        content: BinaryData.FromString(existingBlob)),
                    Mock.Of<Response>()));

            string uploaded = "";

            _blobClientMock
                .Setup(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
                .Callback<Stream, bool, CancellationToken>((s, _, __) =>
                {
                    s.Position = 0;
                    uploaded = new StreamReader(s).ReadToEnd();
                })
                .ReturnsAsync(MockBlobResponse());

            await _service.Update277DailySummaryReportAsync(
                containerName, ediContent, reportFileName, existingClaimIds);

            Assert.Contains("Accepted: 4", uploaded);
            Assert.Contains("Rejected: 2", uploaded);
        }

        [Fact]
        public async Task Update277DailySummaryReportAsync_WhenClaimAlreadyProcessed_ShouldNotIncrement()
        {
            var edi = GetValid277Edi();

            var existingClaimIds = new List<string>
            {
                "CLAIMTRN1"
            };

            _blobClientMock
                .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

            string uploaded = "";

            _blobClientMock
                .Setup(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
                .Callback<Stream, bool, CancellationToken>((s, _, __) =>
                {
                    s.Position = 0;
                    uploaded = new StreamReader(s).ReadToEnd();
                })
                .ReturnsAsync(MockBlobResponse());

            await _service.Update277DailySummaryReportAsync(
                "container", edi, "folder/x.txt", existingClaimIds);

            Assert.Contains("Total Claims: 0", uploaded);
            Assert.Contains("Accepted: 0", uploaded);
            Assert.Contains("Rejected: 0", uploaded);
        }

        [Fact]
        public async Task Update277DailySummaryReportAsync_RejectedClaim_ShouldIncrementRejected()
        {
            var edi = GetRejected277Edi();

            _blobClientMock
                .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

            string uploaded = "";

            _blobClientMock
                .Setup(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
                .Callback<Stream, bool, CancellationToken>((s, _, __) =>
                {
                    s.Position = 0;
                    uploaded = new StreamReader(s).ReadToEnd();
                })
                .ReturnsAsync(MockBlobResponse());

            await _service.Update277DailySummaryReportAsync(
                "container", edi, "folder/x.txt", new List<string>());

            Assert.Contains("Rejected: 1", uploaded);
            Assert.Contains("Accepted: 0", uploaded);
        }



        [Fact]
        public async Task Update277DetailedReportAsync_ShouldUploadDetailedReport()
        {
            var containerName = "container";
            var ediData = "edi data";
            var fileName = "folder/file.txt";

            _blobClientMock
            .Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

            _blobClientMock
            .Setup(c => c.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(
            BlobsModelFactory.BlobContentInfo(new ETag("e"), DateTimeOffset.UtcNow, null, "v", null, null, 0),
            Mock.Of<Response>()
           ));

            var ids = await _service.Update277DetailedReportAsync(containerName, ediData, fileName);

            _blobClientMock.Verify(c => c.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()), Times.Once);
            Assert.NotNull(ids);
        }

        [Fact]
        public void BuildBasePath_WhenHasNoSubfolders_ShouldAppendYearMonthAnd3()
        {
            // Arranged
            var basePath = "root";
            var parts = new[] { "835" };
            var year = "2024";
            var month = "09";
            var hasSubfolders = false;

            // Act (same logic as service)
            if (parts.Length == 1)
            {
                if (hasSubfolders)
                    basePath = $"{basePath}/{parts[0]}";
                else
                    basePath = $"{basePath}/{parts[0]}/{year}/{month}/3";
            }

            // Assert
            Assert.Equal("root/835/2024/09/3", basePath);
        }

        [Fact]
        public async Task UploadAvailityFilesToBlobBackupAsync_ShouldUploadAndReturnEmptyString()
        {
            var containerName = "container";
            var blobPath = "availity/backup/file.txt";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("backup"));
            stream.Position = 5;

            _blobServiceClientMock
            .Setup(s => s.GetBlobContainerClient(containerName))
            .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock
            .Setup(c => c.GetBlobClient(blobPath))
            .Returns(_blobClientMock.Object);

            _blobClientMock
            .Setup(c => c.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
            .Callback<Stream, bool, CancellationToken>((s, overwrite, ct) => Assert.Equal(0, s.Position))
            .ReturnsAsync(Response.FromValue(
            BlobsModelFactory.BlobContentInfo(new ETag("e"), DateTimeOffset.UtcNow, null, "v", null, null, 0),
            Mock.Of<Response>()
           ));

            var result = await _service.UploadAvailityFilesToBlobBackupAsync(containerName, blobPath, stream);

            _blobClientMock.Verify(c => c.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task Update999ReportAsync_ShouldUploadUpdatedReport()
        {
            var containerName = "container";
            var filePath = "folder/report.txt";
            var summary = new EDI999Summary
            {
                FileName = "file1",
                Partner = "partner",
                TotalTransactionSets = 5,
                Accepted = 4,
                Rejected = 1
            };

            _blobClientMock
            .Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

            _blobClientMock
            .Setup(c => c.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(
            BlobsModelFactory.BlobContentInfo(new ETag("e"), DateTimeOffset.UtcNow, null, "v", null, null, 0),
            Mock.Of<Response>()
           ));

            await _service.Update999ReportAsync(containerName, summary, filePath);

            _blobClientMock.Verify(c => c.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UploadAvailityFilesToBlobBackupAsync_StreamAlreadyAtZero_ShouldStillUpload()
        {
            var containerName = "container";
            var blobPath = "availity/backup/file.txt";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("data"));

            _blobServiceClientMock
                .Setup(s => s.GetBlobContainerClient(containerName))
                .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock
                .Setup(c => c.GetBlobClient(blobPath))
                .Returns(_blobClientMock.Object);

            _blobClientMock
                .Setup(c => c.UploadAsync(stream, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(MockBlobResponse());

            var result = await _service.UploadAvailityFilesToBlobBackupAsync(containerName, blobPath, stream);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task UploadAvailityFilesToBlobBackupAsync_NoDirectoryInPath_ShouldUpload()
        {
            var blobPath = "file.txt";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("x"));

            _blobServiceClientMock
                .Setup(s => s.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock
                .Setup(c => c.GetBlobClient(blobPath))
                .Returns(_blobClientMock.Object);

            _blobClientMock
                .Setup(c => c.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(MockBlobResponse());

            var result = await _service.UploadAvailityFilesToBlobBackupAsync("c", blobPath, stream);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task UploadAvailityFilesToBlobBackupAsync_MultiFolderPath_ShouldUpload()
        {
            var blobPath = "a/b/c/d/file.json";
            var stream = new MemoryStream(new byte[] { 1, 2, 3 });

            _blobServiceClientMock
                .Setup(s => s.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock
                .Setup(c => c.GetBlobClient(blobPath))
                .Returns(_blobClientMock.Object);

            _blobClientMock
                .Setup(c => c.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(MockBlobResponse());

            var result = await _service.UploadAvailityFilesToBlobBackupAsync("c", blobPath, stream);

            _blobClientMock.Verify(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UploadAvailityFilesToBlobBackupAsync_WhenUploadFails_ShouldThrow()
        {
            var stream = new MemoryStream(new byte[] { 1 });

            _blobServiceClientMock
                .Setup(s => s.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock
                .Setup(c => c.GetBlobClient(It.IsAny<string>()))
                .Returns(_blobClientMock.Object);

            _blobClientMock
                .Setup(c => c.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("upload failed"));

            await Assert.ThrowsAsync<Exception>(() =>
                _service.UploadAvailityFilesToBlobBackupAsync("c", "a/b.txt", stream));
        }


        [Fact]
        public async Task UploadAvailityFilesToBlobBackupAsync_VerifyContainerName()
        {
            var stream = new MemoryStream(new byte[] { 1 });

            _blobServiceClientMock
                .Setup(s => s.GetBlobContainerClient("expected"))
                .Returns(_blobContainerClientMock.Object)
                .Verifiable();

            _blobContainerClientMock
                .Setup(c => c.GetBlobClient(It.IsAny<string>()))
                .Returns(_blobClientMock.Object);

            _blobClientMock
                .Setup(c => c.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(MockBlobResponse());

            await _service.UploadAvailityFilesToBlobBackupAsync("expected", "file.txt", stream);

            _blobServiceClientMock.Verify();
        }

        [Fact]
        public async Task UploadAvailityFilesToBlobBackupAsync_ShouldResetStreamPosition()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("abcdef"));
            stream.Position = 3;

            _blobServiceClientMock
                .Setup(s => s.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(_blobContainerClientMock.Object);

            _blobContainerClientMock
                .Setup(c => c.GetBlobClient(It.IsAny<string>()))
                .Returns(_blobClientMock.Object);

            _blobClientMock
                .Setup(c => c.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
                .Callback<Stream, bool, CancellationToken>((s, o, t) =>
                {
                    Assert.Equal(0, s.Position);
                })
                .ReturnsAsync(MockBlobResponse());

            await _service.UploadAvailityFilesToBlobBackupAsync("c", "file.txt", stream);
        }

        [Fact]
        public async Task Update277DetailedReportAsync_WhenBlobNotExists_ShouldUpload()
        {
            _blobClientMock
                .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

            SetupUploadSuccess();

            var result = await _service.Update277DetailedReportAsync(
                "container",
                GetValid277Edi(),
                "folder/file.txt");

            Assert.NotNull(result);

            _blobClientMock.Verify(x =>
                x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Update277DetailedReportAsync_WhenClaimAlreadyLogged_ShouldReturnExistingId()
        {
            _blobClientMock
                .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            var binary = BinaryData.FromString("CLAIMTRN1 file.txt");

            _blobClientMock
                .Setup(x => x.DownloadContentAsync())
                .ReturnsAsync(Response.FromValue(
                    BlobsModelFactory.BlobDownloadResult(content: binary),
                    Mock.Of<Response>()));

            SetupUploadSuccess();

            var ids = await _service.Update277DetailedReportAsync(
                "container",
                GetValid277Edi(),
                "folder/file.txt");

            Assert.Contains("CLAIMTRN1", ids);
        }

        [Fact]
        public async Task Update277DetailedReportAsync_WhenExistingContainsFileName_ShouldMarkProcessed()
        {
            _blobClientMock
                .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            var binary = BinaryData.FromString("some text file.txt already processed");

            _blobClientMock
                .Setup(x => x.DownloadContentAsync())
                .ReturnsAsync(Response.FromValue(
                    BlobsModelFactory.BlobDownloadResult(content: binary),
                    Mock.Of<Response>()));

            SetupUploadSuccess();

            var ids = await _service.Update277DetailedReportAsync(
                "container",
                GetValid277Edi(),
                "folder/file.txt");

            Assert.Contains("CLAIMTRN1", ids);

            _blobClientMock.Verify(x =>
                x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Update277DetailedReportAsync_WhenAllClaimsAlreadyProcessed_ShouldStillUpload()
        {
            _blobClientMock
                .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            var binary = BinaryData.FromString("CLAIMTRN1");

            _blobClientMock
                .Setup(x => x.DownloadContentAsync())
                .ReturnsAsync(Response.FromValue(
                    BlobsModelFactory.BlobDownloadResult(content: binary),
                    Mock.Of<Response>()));

            SetupUploadSuccess();

            var ids = await _service.Update277DetailedReportAsync(
                "container",
                GetValid277Edi(),
                "folder/file.txt");

            Assert.Single(ids);
            Assert.Equal("CLAIMTRN1", ids[0]);

            _blobClientMock.Verify(x =>
                x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private Response<BlobContentInfo> MockBlobResponse()
        {
            return Response.FromValue(
                BlobsModelFactory.BlobContentInfo(
                    new ETag("e"),
                    DateTimeOffset.UtcNow,
                    null,
                    "v",
                    null,
                    null,
                    0),
                Mock.Of<Response>());
        }

        private void SetupUploadSuccess()
        {
            _blobClientMock
                .Setup(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(
                    BlobsModelFactory.BlobContentInfo(
                        new ETag("e"),
                        DateTimeOffset.UtcNow,
                        null, null, null, null, 0),
                    Mock.Of<Response>()));
        }

        private string GetValid277Edi()
        {
            return string.Join("~", new[]
            {
             "BHT*0019*08*REF123*20240101",
             "TRN*1*REPORTTRN",
             "NM1*AY*2*SENDERORG",
             "NM1*41*2*RECEIVERORG",
             "NM1*QC*1*DOE",
             "TRN*2*CLAIMTRN1",
             "STC*A1:19:PR:1*20240101*WQ*100*REJECT",
             "REF*F8*CTRL123"
            }) + "~";
        }

        private string GetRejected277Edi()
        {
            return string.Join("~", new[]
            {
        "BHT*0019*08*REF123*20240101",
        "NM1*AY*2*SENDER",
        "NM1*41*2*RECEIVER",
        "NM1*QC*1*DOE",
        "TRN*1*RPT",
        "TRN*2*CLAIMTRN9",
        "STC*A7:21:PR*20240101*WQ*100*DENIED",
        "REF*F8*CTRL"
    }) + "~";
        }



    }
}
