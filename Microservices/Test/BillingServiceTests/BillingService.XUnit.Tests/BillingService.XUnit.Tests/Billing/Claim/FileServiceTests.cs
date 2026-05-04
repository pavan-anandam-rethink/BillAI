using System;
using Moq;
using Xunit;
using BillingService.Domain.Services.Files;
using BillingService.Domain.Interfaces.Files;

namespace BillingService.Domain.Tests.Services.Files
{
    public class FileServiceTests
    {
        private readonly Mock<IFileManagerService> _fileManagerMock;
        private readonly FileService _service;

        public FileServiceTests()
        {
            _fileManagerMock = new Mock<IFileManagerService>();
            _service = new FileService(_fileManagerMock.Object);
        }

        [Fact]
        public void PrepareFolderForEncounterAttachmentFile_ReturnsNull_WhenFolderOrFileEmpty()
        {
            var result1 = _service.PrepareFolderForEncounterAttachmentFile(1, "", "", false);
            var result2 = _service.PrepareFolderForEncounterAttachmentFile(1, "file.txt", "", false);
            var result3 = _service.PrepareFolderForEncounterAttachmentFile(1, "", "folder", false);

            Assert.Null(result1);
            Assert.Null(result2);
            Assert.Null(result3);

            _fileManagerMock.Verify(
                x => x.BuildBaseFilePathForEncounterAttachment(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()),
                Times.Never);
        }

        [Fact]
        public void PrepareFolderForEncounterAttachmentFile_ReturnsPath_WithoutClaimIdentifier()
        {
            _fileManagerMock
                .Setup(x => x.BuildBaseFilePathForEncounterAttachment(
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>()))
                .Returns<int, string, string, int?>((accountId, folderName, subFolderName, encounterId) =>
                    $"encounter://{accountId}/{folderName}/{subFolderName}");

            var result = _service.PrepareFolderForEncounterAttachmentFile(42, "file.txt", "attachments", false);

            Assert.NotNull(result);
            Assert.StartsWith("encounter://42/attachments/", result);

            _fileManagerMock.Verify(x => x.BuildBaseFilePathForEncounterAttachment(
                42,
                "attachments",
                It.Is<string>(s => s.Contains("_") && s.Length > 0), // timestamp_guid
                null), Times.Once);
        }

        [Fact]
        public void PrepareFolderForEncounterAttachmentFile_ReturnsPath_WithClaimIdentifier()
        {
            _fileManagerMock
                .Setup(x => x.BuildBaseFilePathForEncounterAttachment(
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>()))
                .Returns<int, string, string, int?>((accountId, folderName, subFolderName, encounterId) =>
                    $"encounter://{accountId}/{folderName}/{subFolderName}");

            var claimId = "CLAIM123";
            var result = _service.PrepareFolderForEncounterAttachmentFile(7, "doc.pdf", "docs", true, referenceId: 99, claimidentifier: claimId);

            Assert.NotNull(result);
            Assert.StartsWith($"encounter://7/docs/{claimId}_", result);

            _fileManagerMock.Verify(x => x.BuildBaseFilePathForEncounterAttachment(
                7,
                "docs",
                It.Is<string>(s => s.StartsWith($"{claimId}_") && s.Contains("_")), // CLAIMID_timestamp_guid
                null), Times.Once);
        }

        [Fact]
        public void PrepareFolderForERAErrorFile_ReturnsPath_UsesCurrentDateFolder()
        {
            _fileManagerMock
                .Setup(x => x.BuildBaseFilePathForERAErrors(
                    It.IsAny<int>(),
                    It.IsAny<string>()))
                .Returns<int, string>((accountId, folderName) =>
                    $"era://{accountId}/{folderName}");

            var accountId = 1001;
            var result = _service.PrepareFolderForERAErrorFile(accountId);

            Assert.NotNull(result);
            Assert.StartsWith($"era://{accountId}/", result);

            _fileManagerMock.Verify(x => x.BuildBaseFilePathForERAErrors(
                accountId,
                It.Is<string>(s => s.Length == 7 || s.Length == 8 || s.Length == 9)), // "yyyMMdd" (note: service uses "yyyMMdd")
                Times.Once);
        }
    }
}