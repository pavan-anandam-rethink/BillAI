using Billing.FolderStructure.Core.Models;
using Billing.FolderStructure.Core.Utils;
using BillingService.Domain.Services.Files;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using System;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Files
{
    public class EdiFilesDownloadTest
    {
        private readonly Mock<IBillingFilePath> _mockBillingFilePath;
        private readonly Mock<ILogger<EdiFilesDownload>> _mockLogger;
        private readonly EdiFilesDownload _sut;

        public EdiFilesDownloadTest()
        {
            _mockBillingFilePath = new Mock<IBillingFilePath>();
            _mockLogger = new Mock<ILogger<EdiFilesDownload>>();
            _sut = new EdiFilesDownload(_mockBillingFilePath.Object, _mockLogger.Object);
        }

        #region SaveClaimEdiFilePath Tests

        [Fact]
        public async Task SaveClaimEdiFilePath_Should_Return_True_On_Success()
        {
            // Arrange
            var billingRequest = CreateBillingRequest();
            var fullFilePath = "container/path/to/file.edi";

            _mockBillingFilePath.Setup(x => x.GetEdiFileType(It.IsAny<BillingRequest>()))
                .ReturnsAsync("837");
            _mockBillingFilePath.Setup(x => x.AddOrUpdateBlobFilePath(It.IsAny<ClaimEdiFilesModel>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.SaveClaimEdiFilePath(billingRequest, fullFilePath);

            // Assert
            Assert.True(result);
            _mockBillingFilePath.Verify(x => x.GetEdiFileType(billingRequest), Times.Once);
            _mockBillingFilePath.Verify(x => x.AddOrUpdateBlobFilePath(It.IsAny<ClaimEdiFilesModel>()), Times.Once);
        }

        [Fact]
        public async Task SaveClaimEdiFilePath_Should_Return_False_When_AddOrUpdateBlobFilePath_Throws()
        {
            // Arrange
            var billingRequest = CreateBillingRequest();
            var fullFilePath = "container/path/to/file.edi";

            _mockBillingFilePath.Setup(x => x.GetEdiFileType(It.IsAny<BillingRequest>()))
                .ReturnsAsync("837");
            _mockBillingFilePath.Setup(x => x.AddOrUpdateBlobFilePath(It.IsAny<ClaimEdiFilesModel>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _sut.SaveClaimEdiFilePath(billingRequest, fullFilePath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SaveClaimEdiFilePath_Should_Use_Unknown_FileType_When_Data_Is_Null()
        {
            // Arrange
            var billingRequest = CreateBillingRequest();
            billingRequest.Data = null;
            billingRequest.FieldIdentifier = "segment/837/detail";
            var fullFilePath = "container/path/to/file.edi";

            _mockBillingFilePath.Setup(x => x.AddOrUpdateBlobFilePath(It.IsAny<ClaimEdiFilesModel>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.SaveClaimEdiFilePath(billingRequest, fullFilePath);

            // Assert
            Assert.True(result);
            _mockBillingFilePath.Verify(x => x.GetEdiFileType(It.IsAny<BillingRequest>()), Times.Never);
            _mockBillingFilePath.Verify(x => x.AddOrUpdateBlobFilePath(
                It.Is<ClaimEdiFilesModel>(m => m.FileType == "837")), Times.Once);
        }

        [Fact]
        public async Task SaveClaimEdiFilePath_Should_Use_Unknown_FileType_When_Data_Is_Empty()
        {
            // Arrange
            var billingRequest = CreateBillingRequest();
            billingRequest.Data = Array.Empty<byte>();
            billingRequest.FieldIdentifier = "segment/837/detail";
            var fullFilePath = "container/path/to/file.edi";

            _mockBillingFilePath.Setup(x => x.AddOrUpdateBlobFilePath(It.IsAny<ClaimEdiFilesModel>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.SaveClaimEdiFilePath(billingRequest, fullFilePath);

            // Assert
            Assert.True(result);
            _mockBillingFilePath.Verify(x => x.GetEdiFileType(It.IsAny<BillingRequest>()), Times.Never);
            _mockBillingFilePath.Verify(x => x.AddOrUpdateBlobFilePath(
                It.Is<ClaimEdiFilesModel>(m => m.FileType == "837")), Times.Once);
        }

        [Fact]
        public async Task SaveClaimEdiFilePath_Should_Fallback_To_FieldIdentifier_When_FileType_Is_Unknown()
        {
            // Arrange
            var billingRequest = CreateBillingRequest();
            billingRequest.FieldIdentifier = "part0/837/part2";
            var fullFilePath = "container/path/to/file.edi";

            _mockBillingFilePath.Setup(x => x.GetEdiFileType(It.IsAny<BillingRequest>()))
                .ReturnsAsync("Unknown");
            _mockBillingFilePath.Setup(x => x.AddOrUpdateBlobFilePath(It.IsAny<ClaimEdiFilesModel>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.SaveClaimEdiFilePath(billingRequest, fullFilePath);

            // Assert
            Assert.True(result);
            _mockBillingFilePath.Verify(x => x.GetEdiFileType(billingRequest), Times.Once);
            _mockBillingFilePath.Verify(x => x.AddOrUpdateBlobFilePath(
                It.Is<ClaimEdiFilesModel>(m => m.FileType == "837")), Times.Once);
        }

        [Fact]
        public async Task SaveClaimEdiFilePath_Should_Include_ClaimSubmission_Data_When_Provided()
        {
            // Arrange
            var billingRequest = CreateBillingRequest();
            var fullFilePath = "container/path/to/file.edi";
            var claimSubmission = CreateClaimSubmission();

            _mockBillingFilePath.Setup(x => x.GetEdiFileType(It.IsAny<BillingRequest>()))
                .ReturnsAsync("835");
            _mockBillingFilePath.Setup(x => x.AddOrUpdateBlobFilePath(It.IsAny<ClaimEdiFilesModel>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.SaveClaimEdiFilePath(billingRequest, fullFilePath, claimSubmission);

            // Assert
            Assert.True(result);
            _mockBillingFilePath.Verify(x => x.AddOrUpdateBlobFilePath(It.Is<ClaimEdiFilesModel>(m =>
                m.MemberId == claimSubmission.Claim.MemberId &&
                m.ClaimSubmissionId == claimSubmission.Id &&
                m.ClaimId == claimSubmission.Claim.Id)), Times.Once);
        }

        [Fact]
        public async Task SaveClaimEdiFilePath_Should_Set_Zero_For_ClaimSubmission_Fields_When_Not_Provided()
        {
            // Arrange
            var billingRequest = CreateBillingRequest();
            var fullFilePath = "container/path/to/file.edi";

            _mockBillingFilePath.Setup(x => x.GetEdiFileType(It.IsAny<BillingRequest>()))
                .ReturnsAsync("837");
            _mockBillingFilePath.Setup(x => x.AddOrUpdateBlobFilePath(It.IsAny<ClaimEdiFilesModel>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.SaveClaimEdiFilePath(billingRequest, fullFilePath, null);

            // Assert
            Assert.True(result);
            _mockBillingFilePath.Verify(x => x.AddOrUpdateBlobFilePath(It.Is<ClaimEdiFilesModel>(m =>
                m.MemberId == 0 &&
                m.ClaimSubmissionId == null &&
                m.ClaimId == 0)), Times.Once);
        }

        [Fact]
        public async Task SaveClaimEdiFilePath_Should_Map_BillingRequest_Fields_To_Model()
        {
            // Arrange
            var billingRequest = CreateBillingRequest();
            var fullFilePath = "container/path/to/file.edi";
            var claimSubmission = CreateClaimSubmission();

            _mockBillingFilePath.Setup(x => x.GetEdiFileType(It.IsAny<BillingRequest>()))
                .ReturnsAsync("837");
            _mockBillingFilePath.Setup(x => x.AddOrUpdateBlobFilePath(It.IsAny<ClaimEdiFilesModel>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.SaveClaimEdiFilePath(billingRequest, fullFilePath, claimSubmission);

            // Assert
            Assert.True(result);
            _mockBillingFilePath.Verify(x => x.AddOrUpdateBlobFilePath(It.Is<ClaimEdiFilesModel>(m =>
                m.AccountInfoId == billingRequest.AccountInfoId &&
                m.FileType == "837" &&
                m.BlobFilePath == fullFilePath &&
                m.MemberId == claimSubmission.Claim.MemberId &&
                m.ClaimSubmissionId == claimSubmission.Id &&
                m.ClaimId == claimSubmission.Claim.Id)), Times.Once);
        }

        [Fact]
        public async Task SaveClaimEdiFilePath_Should_Use_FieldIdentifier_Second_Part_When_Data_Is_Null()
        {
            // Arrange
            var billingRequest = CreateBillingRequest();
            billingRequest.FieldIdentifier = "segment/999/detail";
            billingRequest.Data = null;
            var fullFilePath = "container/path/to/file.edi";

            _mockBillingFilePath.Setup(x => x.AddOrUpdateBlobFilePath(It.IsAny<ClaimEdiFilesModel>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.SaveClaimEdiFilePath(billingRequest, fullFilePath);

            // Assert
            Assert.True(result);
            _mockBillingFilePath.Verify(x => x.AddOrUpdateBlobFilePath(
                It.Is<ClaimEdiFilesModel>(m => m.FileType == "999")), Times.Once);
        }

        [Fact]
        public async Task SaveClaimEdiFilePath_Should_Include_PaymentId_When_Provided()
        {
            // Arrange
            var billingRequest = CreateBillingRequest();
            billingRequest.PaymentId = 999;
            var fullFilePath = "container/path/to/file.edi";

            _mockBillingFilePath.Setup(x => x.GetEdiFileType(It.IsAny<BillingRequest>()))
                .ReturnsAsync("835");
            _mockBillingFilePath.Setup(x => x.AddOrUpdateBlobFilePath(It.IsAny<ClaimEdiFilesModel>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.SaveClaimEdiFilePath(billingRequest, fullFilePath);

            // Assert
            Assert.True(result);
            _mockBillingFilePath.Verify(x => x.AddOrUpdateBlobFilePath(
                It.Is<ClaimEdiFilesModel>(m => m.PaymentId == 999)), Times.Once);
        }

        [Fact]
        public async Task SaveClaimEdiFilePath_Should_Return_False_When_GetEdiFileType_Throws()
        {
            // Arrange
            var billingRequest = CreateBillingRequest();
            var fullFilePath = "container/path/to/file.edi";

            _mockBillingFilePath.Setup(x => x.GetEdiFileType(It.IsAny<BillingRequest>()))
                .ThrowsAsync(new Exception("Service unavailable"));

            // Act
            var result = await _sut.SaveClaimEdiFilePath(billingRequest, fullFilePath);

            // Assert
            Assert.False(result);
            _mockBillingFilePath.Verify(x => x.AddOrUpdateBlobFilePath(It.IsAny<ClaimEdiFilesModel>()), Times.Never);
        }

        #endregion

        #region Helper Methods

        private BillingRequest CreateBillingRequest()
        {
            return new BillingRequest
            {
                FieldIdentifier = "segment/837/detail",
                AccountInfoId = 123,
                ClearingHouseTitle = "Test Clearing House",
                ClearingHouseId = 456,
                Data = new byte[] { 1, 2, 3, 4, 5 }
            };
        }

        private ClaimSubmissionEntity CreateClaimSubmission()
        {
            return new ClaimSubmissionEntity
            {
                Id = 789,
                ClaimId = 101,
                Claim = new ClaimEntity
                {
                    Id = 101,
                    MemberId = 202,
                    AccountInfoId = 123
                }
            };
        }

        #endregion
    }
}