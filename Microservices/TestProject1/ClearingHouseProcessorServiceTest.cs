using BillingService.Domain.Models.Claims;
using ClearingHouseService.Web.Interface;
using ClearingHouseService.Web.Service;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ClearingHouseProcessorServiceTest
{
    public class ClearingHouseProcessorServiceTests
    {
        private readonly Mock<ICommon> _common = new(MockBehavior.Strict);
        private readonly Mock<ILogger<ClearingHouseProcessorService>> _logger = new();

        private ClearingHouseProcessorService CreateSut()
            => new ClearingHouseProcessorService(_common.Object, _logger.Object);

        // ---------- GenerateEDI ----------

        [Fact]
        public async Task GenerateEDI_WhenCommonReturnsSuccess_ReturnsResult_AndLogsInfo()
        {
            var sut = CreateSut();
            var dto = new ClearingHouseClaimModel { claimId = 101, clearinghouseId = 10 };

            _common.Setup(x => x.GenerateEDIData(dto))
                   .ReturnsAsync((true, "EDI_DATA"));

            var result = await sut.GenerateEDI(dto);

            Assert.True(result.success);
            Assert.Equal("EDI_DATA", result.result);

            _common.Verify(x => x.GenerateEDIData(dto), Times.Once());

            _logger.VerifyLog(LogLevel.Information, Times.Once(),
                "Generating EDI for ClaimId:");
        }

        [Fact]
        public async Task GenerateEDI_WhenCommonThrows_ReturnsFalseAndMessage_AndLogsError()
        {
            var sut = CreateSut();
            var dto = new ClearingHouseClaimModel { claimId = 201, clearinghouseId = 20 };

            _common.Setup(x => x.GenerateEDIData(dto))
                   .ThrowsAsync(new Exception("boom"));

            var result = await sut.GenerateEDI(dto);

            Assert.False(result.success);
            Assert.Equal("boom", result.result);

            _common.Verify(x => x.GenerateEDIData(dto), Times.Once());

            _logger.VerifyLog(LogLevel.Error, Times.Once(),
                "Error generating EDI for ClaimId:");
        }

        // ---------- UploadfileToBlobStorage ----------

        [Fact]
        public async Task UploadfileToBlobStorage_WhenCommonReturnsSuccess_ReturnsResult_AndLogsInfo()
        {
            var sut = CreateSut();
            var files = new ClaimUploadModelWithUserInfo
            {
                FileName = "abc.txt",
                ClaimId = 555
            };

            _common.Setup(x => x.UploadfileToBlobStorage(files))
                   .ReturnsAsync((true, "OK"));

            var result = await sut.UploadfileToBlobStorage(files);

            Assert.True(result.success);
            Assert.Equal("OK", result.result);

            _common.Verify(x => x.UploadfileToBlobStorage(files), Times.Once());

            _logger.VerifyLog(LogLevel.Information, Times.Once(),
                "Uploading file to Blob Storage for FileName:");
        }

        [Fact]
        public async Task UploadfileToBlobStorage_WhenCommonThrows_ReturnsFalseAndMessage_AndLogsError()
        {
            var sut = CreateSut();
            var files = new ClaimUploadModelWithUserInfo
            {
                FileName = "abc.txt",
                ClaimId = 555
            };

            _common.Setup(x => x.UploadfileToBlobStorage(files))
                   .ThrowsAsync(new Exception("upload failed"));

            var result = await sut.UploadfileToBlobStorage(files);

            Assert.False(result.success);
            Assert.Equal("upload failed", result.result);

            _common.Verify(x => x.UploadfileToBlobStorage(files), Times.Once());

            _logger.VerifyLog(LogLevel.Error, Times.Once(),
                "Error uploading file to Blob Storage for FileName:");
        }

        // ---------- UploadSFTPfilesToBlobStorage ----------

        [Fact]
        public async Task UploadSFTPfilesToBlobStorage_WhenCommonReturnsResponse_LogsStartAndCompleted_AndReturnsResponse()
        {
            var sut = CreateSut();
            var fileStreams = new DownloadSftpDataModel
            {
                clearingHouseId = 99,
                FileName = "file.edi",
                Title = "t1"
            };

            _common.Setup(x => x.UploadSFTPfilesToBlobStorage(fileStreams))
                   .ReturnsAsync((true, "UPLOADED"));

            var result = await sut.UploadSFTPfilesToBlobStorage(fileStreams);

            Assert.True(result.success);
            Assert.Equal("UPLOADED", result.result);

            _common.Verify(x => x.UploadSFTPfilesToBlobStorage(fileStreams), Times.Once());

            _logger.VerifyLog(LogLevel.Information, Times.Once(),
                "Starting upload of SFTP file to Blob Storage.");

            _logger.VerifyLog(LogLevel.Information, Times.Once(),
                "Completed upload of SFTP file to Blob Storage.");
        }

        [Fact]
        public async Task UploadSFTPfilesToBlobStorage_WhenCommonThrows_LogsStartAndError_AndReturnsFalseWithMessage()
        {
            var sut = CreateSut();
            var fileStreams = new DownloadSftpDataModel
            {
                clearingHouseId = 99,
                FileName = "file.edi",
                Title = "t1"
            };

            _common.Setup(x => x.UploadSFTPfilesToBlobStorage(fileStreams))
                   .ThrowsAsync(new Exception("sftp boom"));

            var result = await sut.UploadSFTPfilesToBlobStorage(fileStreams);

            Assert.False(result.success);
            Assert.Equal("sftp boom", result.result);

            _common.Verify(x => x.UploadSFTPfilesToBlobStorage(fileStreams), Times.Once());

            _logger.VerifyLog(LogLevel.Information, Times.Once(),
                "Starting upload of SFTP file to Blob Storage.");

            _logger.VerifyLog(LogLevel.Error, Times.Once(),
                "Error uploading SFTP file to Blob Storage.");
        }
    }

    internal static class LoggerMoqExtensions
    {
        public static void VerifyLog<T>(
            this Mock<ILogger<T>> logger,
            LogLevel level,
            Times times,
            string containsMessage)
        {
            logger.Verify(x => x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString() != null && v.ToString()!.Contains(containsMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                times);
        }
    }
}
