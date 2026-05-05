using BillingService.Domain.Services.Files;
using Moq;
using Rethink.Services.Domain.Interfaces;
using System;
using System.IO;
using Twilio.Rest;
using Xunit;
using BillingService.Domain.Interfaces.Files;
using BillingService.Domain.Services.Files;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Files
{
    public class BlobManagerServiceTest
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IKeyVaultProviderService> _mockKeyVaultProviderService;

        public BlobManagerServiceTest()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockKeyVaultProviderService = new Mock<IKeyVaultProviderService>();

            _mockConfiguration.Setup(c => c["ConnectionStrings:BlobStorage:ConnectionString"])
            .Returns("BlobStorageConnectionStringKey");

            // Use a valid connection string format for Azure Storage Emulator
            _mockKeyVaultProviderService.Setup(k => k.GetSecretAsync("BlobStorageConnectionStringKey"))
                .ReturnsAsync("UseDevelopmentStorage=true");
        }

        #region Properties Tests

        [Fact]
        public void InvalidFileExtensions_ShouldReturnCorrectExtensions()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.InvalidFileExtensions;

            // Assert
            Assert.NotNull(result);
            Assert.Contains(".exe", result);
            Assert.Single(result);
        }

        [Fact]
        public void AcceptableImageFileExtensions_ShouldReturnCorrectExtensions()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.AcceptableImageFileExtensions;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Count);
            Assert.Contains(".gif", result);
            Assert.Contains(".png", result);
            Assert.Contains(".jpeg", result);
            Assert.Contains(".jpg", result);
        }

        [Fact]
        public void AcceptableVideoFileExtensions_ShouldReturnCorrectExtensions()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.AcceptableVideoFileExtensions;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(".mp4", result);
            Assert.Contains(".mov", result);
            Assert.Contains(".avi", result);
        }

        [Fact]
        public void AcceptableDocFileExtensions_ShouldReturnCorrectExtensions()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.AcceptableDocFileExtensions;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(".doc", result);
            Assert.Contains(".docx", result);
        }

        [Fact]
        public void AcceptablePrintableFileExtensions_ShouldReturnCorrectExtensions()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.AcceptablePrintableFileExtensions;

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains(".pdf", result);
        }

        #endregion

        #region BuildBaseFilePath Tests

        [Fact]
        public void BuildBaseFilePath_WithChildProfileId_ShouldReturnCorrectPath()
        {
            // Arrange
            var service = CreateService();
            int accountId = 123;
            int? childProfileId = 456;
            int? memberId = null;
            int folderId = 789;

            // Act
            var result = service.BuildBaseFilePath(accountId, childProfileId, memberId, folderId);

            // Assert
            Assert.Equal("UploadedFiles/123/456/789/", result);
        }

        [Fact]
        public void BuildBaseFilePath_WithMemberId_ShouldReturnStaffCabinetPath()
        {
            // Arrange
            var service = CreateService();
            int accountId = 123;
            int? childProfileId = null;
            int? memberId = 456;
            int folderId = 789;

            // Act
            var result = service.BuildBaseFilePath(accountId, childProfileId, memberId, folderId);

            // Assert
            Assert.Equal("UploadedFiles/123/StaffCabinet/456/789/", result);
        }

        [Fact]
        public void BuildBaseFilePath_WithNeitherChildProfileIdNorMemberId_ShouldReturnCompanyPath()
        {
            // Arrange
            var service = CreateService();
            int accountId = 123;
            int? childProfileId = null;
            int? memberId = null;
            int folderId = 789;

            // Act
            var result = service.BuildBaseFilePath(accountId, childProfileId, memberId, folderId);

            // Assert
            Assert.Equal("UploadedFiles/123/company/789/", result);
        }

        [Fact]
        public void BuildBaseFilePath_WithBothChildProfileIdAndMemberId_ShouldPrioritizeChildProfileId()
        {
            // Arrange
            var service = CreateService();
            int accountId = 123;
            int? childProfileId = 456;
            int? memberId = 999;
            int folderId = 789;

            // Act
            var result = service.BuildBaseFilePath(accountId, childProfileId, memberId, folderId);

            // Assert
            Assert.Equal("UploadedFiles/123/456/789/", result);
        }

        #endregion

        #region BuildBaseFilePathForEncounterAttachment Tests

        [Fact]
        public void BuildBaseFilePathForEncounterAttachment_ShouldReturnCorrectPath()
        {
            // Arrange
            var service = CreateService();
            int accountId = 123;
            string folderName = "Encounters";
            string subFolderName = "Attachments";

            // Act
            var result = service.BuildBaseFilePathForEncounterAttachment(accountId, folderName, subFolderName);

            // Assert
            Assert.Equal("UploadedFiles/123/Encounters/Attachments/", result);
        }

        [Fact]
        public void BuildBaseFilePathForEncounterAttachment_WithEncounterId_ShouldReturnCorrectPath()
        {
            // Arrange
            var service = CreateService();
            int accountId = 123;
            string folderName = "Encounters";
            string subFolderName = "Attachments";
            int? encounterId = 456;

            // Act
            var result = service.BuildBaseFilePathForEncounterAttachment(accountId, folderName, subFolderName, encounterId);

            // Assert
            // Note: The current implementation does not use encounterId in the path
            Assert.Equal("UploadedFiles/123/Encounters/Attachments/", result);
        }

        [Fact]
        public void BuildBaseFilePathForEncounterAttachment_WithEmptyFolderName_ShouldReturnPathWithEmptyFolder()
        {
            // Arrange
            var service = CreateService();
            int accountId = 123;
            string folderName = "";
            string subFolderName = "Attachments";

            // Act
            var result = service.BuildBaseFilePathForEncounterAttachment(accountId, folderName, subFolderName);

            // Assert
            Assert.Equal("UploadedFiles/123//Attachments/", result);
        }

        #endregion

        #region BuildBaseFilePathForERAErrors Tests

        [Fact]
        public void BuildBaseFilePathForERAErrors_ShouldReturnCorrectPath()
        {
            // Arrange
            var service = CreateService();
            int accountId = 123;
            string folderName = "ErrorFiles";

            // Act
            var result = service.BuildBaseFilePathForERAErrors(accountId, folderName);

            // Assert
            Assert.Equal("123/ErrorFiles/", result);
        }

        [Fact]
        public void BuildBaseFilePathForERAErrors_WithEmptyFolderName_ShouldReturnPathWithEmptyFolder()
        {
            // Arrange
            var service = CreateService();
            int accountId = 456;
            string folderName = "";

            // Act
            var result = service.BuildBaseFilePathForERAErrors(accountId, folderName);

            // Assert
            Assert.Equal("456//", result);
        }

        #endregion

        #region UploadFileAsync Tests

        [Fact]
        public async Task UploadFileAsync_WithBackslashInPath_ShouldThrowException()
        {
            // Arrange
            var service = CreateService();
            string path = "folder\\subfolder";
            string fileName = "test.txt";
            var fileStream = new MemoryStream(new byte[] { 1, 2, 3 });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
       service.UploadFileAsync(path, fileName, fileStream));
            Assert.Equal("Invalid Blob Path", exception.Message);
        }

        [Fact]
        public async Task UploadFileAsync_WithBytes_WithBackslashInPath_ShouldThrowException()
        {
            // Arrange
            var service = CreateService();
            string path = "folder\\subfolder";
            string fileName = "test.txt";
            byte[] fileBytes = new byte[] { 1, 2, 3 };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
             service.UploadFileAsync(path, fileName, fileBytes));
            Assert.Equal("Invalid Blob Path", exception.Message);
        }

        #endregion

        #region GetFileUrl Tests

        [Fact]
        public async Task GetFileUrl_WithEmptyPath_ShouldReturnNull()
        {
            // Arrange
            var service = CreateService();
            string fullPath = "";

            // Act
            var result = await service.GetFileUrl(fullPath);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetFileUrl_WithNullPath_ShouldReturnNull()
        {
            // Arrange
            var service = CreateService();
            string fullPath = null;

            // Act
            var result = await service.GetFileUrl(fullPath);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldRetrieveConnectionStringFromKeyVault()
        {
            // Arrange & Act
            var service = CreateService();

            // Assert
            _mockKeyVaultProviderService.Verify(
        k => k.GetSecretAsync("BlobStorageConnectionStringKey"),
      Times.Once);
        }

        [Fact]
        public void Constructor_ShouldReadConfigurationKey()
        {
            // Arrange & Act
            var service = CreateService();

            // Assert
            _mockConfiguration.Verify(
             c => c["ConnectionStrings:BlobStorage:ConnectionString"],
         Times.Once);
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public void BuildBaseFilePath_WithZeroAccountId_ShouldReturnPathWithZero()
        {
            // Arrange
            var service = CreateService();
            int accountId = 0;
            int? childProfileId = 1;
            int? memberId = null;
            int folderId = 2;

            // Act
            var result = service.BuildBaseFilePath(accountId, childProfileId, memberId, folderId);

            // Assert
            Assert.Equal("UploadedFiles/0/1/2/", result);
        }

        [Fact]
        public void BuildBaseFilePath_WithNegativeAccountId_ShouldReturnPathWithNegativeValue()
        {
            // Arrange
            var service = CreateService();
            int accountId = -1;
            int? childProfileId = null;
            int? memberId = null;
            int folderId = 1;

            // Act
            var result = service.BuildBaseFilePath(accountId, childProfileId, memberId, folderId);

            // Assert
            Assert.Equal("UploadedFiles/-1/company/1/", result);
        }

        [Fact]
        public void BuildBaseFilePathForERAErrors_WithLargeAccountId_ShouldReturnCorrectPath()
        {
            // Arrange
            var service = CreateService();
            int accountId = int.MaxValue;
            string folderName = "Errors";

            // Act
            var result = service.BuildBaseFilePathForERAErrors(accountId, folderName);

            // Assert
            Assert.Equal($"{int.MaxValue}/Errors/", result);
        }

        [Fact]
        public void BuildBaseFilePathForEncounterAttachment_WithSpecialCharactersInFolderName_ShouldReturnPathWithSpecialCharacters()
        {
            // Arrange
            var service = CreateService();
            int accountId = 123;
            string folderName = "Folder-With_Special.Characters";
            string subFolderName = "Sub-Folder";

            // Act
            var result = service.BuildBaseFilePathForEncounterAttachment(accountId, folderName, subFolderName);

            // Assert
            Assert.Equal("UploadedFiles/123/Folder-With_Special.Characters/Sub-Folder/", result);
        }

        #endregion

        #region File Extension Validation Tests

        [Theory]
        [InlineData(".exe")]
        public void InvalidFileExtensions_ShouldContainDangerousExtension(string extension)
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.InvalidFileExtensions;

            // Assert
            Assert.Contains(extension, result);
        }

        [Theory]
        [InlineData(".gif")]
        [InlineData(".png")]
        [InlineData(".jpeg")]
        [InlineData(".jpg")]
        public void AcceptableImageFileExtensions_ShouldContainImageExtension(string extension)
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.AcceptableImageFileExtensions;

            // Assert
            Assert.Contains(extension, result);
        }

        [Theory]
        [InlineData(".mp4")]
        [InlineData(".mov")]
        [InlineData(".avi")]
        public void AcceptableVideoFileExtensions_ShouldContainVideoExtension(string extension)
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.AcceptableVideoFileExtensions;

            // Assert
            Assert.Contains(extension, result);
        }

        [Theory]
        [InlineData(".doc")]
        [InlineData(".docx")]
        public void AcceptableDocFileExtensions_ShouldContainDocExtension(string extension)
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.AcceptableDocFileExtensions;

            // Assert
            Assert.Contains(extension, result);
        }

        [Theory]
        [InlineData(".pdf")]
        public void AcceptablePrintableFileExtensions_ShouldContainPdfExtension(string extension)
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.AcceptablePrintableFileExtensions;

            // Assert
            Assert.Contains(extension, result);
        }

        #endregion

        #region Multiple Path Scenarios Tests

        [Theory]
        [InlineData(1, 2, 3, "UploadedFiles/1/2/3/")]
        [InlineData(100, 200, 300, "UploadedFiles/100/200/300/")]
        [InlineData(999, 888, 777, "UploadedFiles/999/888/777/")]
        public void BuildBaseFilePath_WithChildProfileId_ShouldReturnExpectedPath(
   int accountId, int childProfileId, int folderId, string expectedPath)
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildBaseFilePath(accountId, childProfileId, null, folderId);

            // Assert
            Assert.Equal(expectedPath, result);
        }

        [Theory]
        [InlineData(1, 2, 3, "UploadedFiles/1/StaffCabinet/2/3/")]
        [InlineData(100, 200, 300, "UploadedFiles/100/StaffCabinet/200/300/")]
        public void BuildBaseFilePath_WithMemberId_ShouldReturnExpectedPath(
              int accountId, int memberId, int folderId, string expectedPath)
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildBaseFilePath(accountId, null, memberId, folderId);

            // Assert
            Assert.Equal(expectedPath, result);
        }

        [Theory]
        [InlineData(1, 2, "UploadedFiles/1/company/2/")]
        [InlineData(100, 200, "UploadedFiles/100/company/200/")]
        public void BuildBaseFilePath_WithCompanyPath_ShouldReturnExpectedPath(
            int accountId, int folderId, string expectedPath)
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.BuildBaseFilePath(accountId, null, null, folderId);

            // Assert
            Assert.Equal(expectedPath, result);
        }

        #endregion

        #region Helper Methods

        private BlobManagerService CreateService()
        {
            return new BlobManagerService(_mockConfiguration.Object, _mockKeyVaultProviderService.Object);
        }

        #endregion
    }
}
