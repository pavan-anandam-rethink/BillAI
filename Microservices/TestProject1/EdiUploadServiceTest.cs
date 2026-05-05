using Billing.FolderStructure.Core.Enum;
using Billing.FolderStructure.Core.Models;
using Billing.FolderStructure.Core.Services;
using Billing.FolderStructure.Core.Utils;
using BillingService.Domain.Interfaces.Files;
using ClearingHouseService.Web.Interface;
using ClearingHouseService.Web.Models;
using ClearingHouseService.Web.Service;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Reflection;

namespace ClearingHouse
{
    public class EdiUploadServiceTest
    {
        private readonly Mock<ILogger<EdiUploadService>> _mockLogger;
        private readonly Mock<ICommon> _mockCommonService;
        private readonly Mock<IBillingBlobService> _mockBillingBlobService;
        private readonly Mock<IBillingFilePath> _mockBillingFilePath;
        private readonly Mock<IClearingHouseUploaderFactory> _mockClearingHouseUploaderFactory;
        private readonly Mock<IEdiFilesDownload> _mockEdiFilesDownload;
        private readonly Mock<IClaimSubmissionHandler> _mockClaimSubmissionHandler;
        private readonly EdiUploadService _ediUploadService;
        public EdiUploadServiceTest()
        {
            _mockLogger = new Mock<ILogger<EdiUploadService>>();
            _mockCommonService = new Mock<ICommon>();
            _mockBillingBlobService = new Mock<IBillingBlobService>();
            _mockBillingFilePath = new Mock<IBillingFilePath>();
            _mockClearingHouseUploaderFactory = new Mock<IClearingHouseUploaderFactory>();
            _mockEdiFilesDownload = new Mock<IEdiFilesDownload>();
            _mockClaimSubmissionHandler = new Mock<IClaimSubmissionHandler>();
            _ediUploadService = new EdiUploadService(
                _mockLogger.Object,
                _mockCommonService.Object,
                _mockBillingBlobService.Object,
                _mockBillingFilePath.Object,
                _mockClearingHouseUploaderFactory.Object,
                _mockEdiFilesDownload.Object,
                _mockClaimSubmissionHandler.Object
            );
        }


        [Fact]
        public async Task ProcessClaimAsync_InvalidClearingHouseDetails_ReturnsFailure()
        {
            // Arrange
            var funderId = 1;
            var ediData = "Sample EDI Data";
            var clearinghouseId = 123;

            // Simulating invalid clearing house details (missing URL)
            var clearingHouse = new ClearingHouseDetailsModel
            {
                Title = "Test Clearing House",
                UserName = "testuser",
                UserPassword = "testpassword",
                UrlLink = "",
                TaxId = "123456789"
            };

            _mockCommonService.Setup(x => x.GetclearinghouseNameById(clearinghouseId))
                .ReturnsAsync(clearingHouse);

            // Act
            var result = await _ediUploadService.ProcessClaimAsync(funderId, ediData, clearinghouseId);

            // Assert
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task UploadAsync_WhenFileTypeIs270AndNoClaim_ReturnsSuccessAndInvokesUploader()
        {
            // Arrange
            var ediData = "Sample EDI Data";
            var claimId = 1;
            var clearinghouseId = 10;

            var mockLogger = new Mock<ILogger<EdiUploadService>>();
            var mockCommon = new Mock<ICommon>();
            var mockBillingBlob = new Mock<IBillingBlobService>();
            var mockBillingFilePath = new Mock<IBillingFilePath>();
            var mockFactory = new Mock<IClearingHouseUploaderFactory>();
            var mockUploader = new Mock<IClearingHouseUploader>();
            var mockSubmissionHandler = new Mock<IClaimSubmissionHandler>();

            var transactionControl = new TransactionControlNumberModel
            {
                FileType = ((int)FileTypes.Type270).ToString(),
                ControlNumbers = new int?[] { 123 }
            };

            // When FileType == 270 the blob upload branch is skipped; we return null for claim fetch
            mockBillingFilePath.Setup(x => x.GetTransactionControlNumber(ediData)).ReturnsAsync(transactionControl);
            mockBillingFilePath.Setup(x => x.FetchClaimSubmissionDataForERA(It.IsAny<TransactionControlNumberModel>())).ReturnsAsync((ClaimSubmissionEntity)null);

            var clearingHouse = new ClearingHouseDetailsModel
            {
                Title = "Test Clearing House",
                UserName = "user",
                UserPassword = "pwd",
                UrlLink = "ftp://test",
                TaxId = "123"
            };

            mockCommon.Setup(x => x.GetclearinghouseNameById(clearinghouseId)).ReturnsAsync(clearingHouse);

            var expectedResult = OperationResult.Success("uploaded.edi");
            mockUploader.Setup(x => x.UploadFileToSftpAsync(It.IsAny<ClearingHouseDetailsModel>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<int>()))
                .ReturnsAsync(expectedResult);

            mockFactory.Setup(f => f.Create()).Returns(mockUploader.Object);

            var mockEdiFilesDownload = new Mock<IEdiFilesDownload>();
            var service = new EdiUploadService(
                mockLogger.Object,
                mockCommon.Object,
                mockBillingBlob.Object,
                mockBillingFilePath.Object,
                mockFactory.Object,
                mockEdiFilesDownload.Object,
                mockSubmissionHandler.Object);

            // Use reflection to invoke private UploadAsync
            var method = typeof(EdiUploadService).GetMethod("UploadAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            // Act
            var task = (Task<OperationResult>)method.Invoke(service, new object[] { clearinghouseId, claimId, ediData });
            var result = await task;

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal("uploaded.edi", result.FileName);

            mockFactory.Verify(f => f.Create(), Times.Once);
            mockUploader.Verify(u => u.UploadFileToSftpAsync(It.IsAny<ClearingHouseDetailsModel>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<int>()), Times.Once);
        }



        [Fact]
        public async Task UploadAsync_FileTypeNot270_ResultNull_ReturnsClaimEntityNotFound()
        {
            // Arrange
            var ediData = "EDI";
            var claimId = 2;
            var clearinghouseId = 10;

            var transactionControl = new TransactionControlNumberModel { FileType = "999", ControlNumbers = new int?[] { 111 } };
            _mockBillingFilePath.Setup(x => x.GetTransactionControlNumber(ediData)).ReturnsAsync(transactionControl);
            _mockBillingFilePath.Setup(x => x.FetchClaimSubmissionDataForERA(transactionControl)).ReturnsAsync((ClaimSubmissionEntity)null);
            _mockCommonService.Setup(x => x.GetclearinghouseNameById(clearinghouseId)).ReturnsAsync(new ClearingHouseDetailsModel { Title = "T" });

            var method = typeof(EdiUploadService).GetMethod("UploadAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            var task = (Task<OperationResult>)method.Invoke(_ediUploadService, new object[] { clearinghouseId, claimId, ediData });
            var result = await task;

            Assert.False(result.IsSuccess);
            Assert.Equal("Claim entity not found", result.ErrorMessage);
        }

        [Fact]
        public async Task UploadAsync_FileTypeNot270_WithResult_PerformsBlobUploadAndCallsUploader()
        {
            // Arrange
            var ediData = "EDI_DATA";
            var claimId = 3;
            var clearinghouseId = 10;

            var transactionControl = new TransactionControlNumberModel { FileType = "999", ControlNumbers = new int?[] { 222 } };
            var claimSubmission = new ClaimSubmissionEntity { Id = 555, Claim = new Rethink.Services.Common.Entities.Billing.Claim.ClaimEntity { AccountInfoId = 77 } };

            _mockBillingFilePath.Setup(x => x.GetTransactionControlNumber(ediData)).ReturnsAsync(transactionControl);
            _mockBillingFilePath.Setup(x => x.FetchClaimSubmissionDataForERA(transactionControl)).ReturnsAsync(claimSubmission);
            _mockBillingFilePath.Setup(x => x.CreateFolderPath(It.IsAny<BillingRequest>())).ReturnsAsync("container/path/file.edi");
            _mockBillingFilePath.Setup(x => x.SplitFilePath("container/path/file.edi")).ReturnsAsync(("container", "path/file.edi"));
            _mockCommonService.Setup(x => x.GetclearinghouseNameById(clearinghouseId)).ReturnsAsync(new ClearingHouseDetailsModel { Title = "TH" });

            var mockUploader = new Mock<IClearingHouseUploader>();
            var expected = OperationResult.Success("file.edi");
            mockUploader.Setup(u => u.UploadFileToSftpAsync(It.IsAny<ClearingHouseDetailsModel>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<int>())).ReturnsAsync(expected);
            _mockClearingHouseUploaderFactory.Setup(f => f.Create()).Returns(mockUploader.Object);

            var service = new EdiUploadService(_mockLogger.Object, _mockCommonService.Object, _mockBillingBlobService.Object, _mockBillingFilePath.Object, _mockClearingHouseUploaderFactory.Object, _mockEdiFilesDownload.Object, _mockClaimSubmissionHandler.Object);

            var method = typeof(EdiUploadService).GetMethod("UploadAsync", BindingFlags.Instance | BindingFlags.NonPublic);

            // Act
            var task = (Task<OperationResult>)method.Invoke(service, new object[] { clearinghouseId, claimId, ediData });
            var result = await task;

            // Assert
            Assert.True(result.IsSuccess);
            mockUploader.Verify(u => u.UploadFileToSftpAsync(It.IsAny<ClearingHouseDetailsModel>(), It.Is<string>(s => s.EndsWith(".edi")), It.IsAny<Stream>(), It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task UploadAsync_UploaderThrows_ExceptionPropagates()
        {
            // Arrange
            var ediData = "EDI";
            var claimId = 6;
            var clearinghouseId = 10;

            var transactionControl = new TransactionControlNumberModel { FileType = ((int)FileTypes.Type270).ToString(), ControlNumbers = new int?[] { 555 } };
            _mockBillingFilePath.Setup(x => x.GetTransactionControlNumber(ediData)).ReturnsAsync(transactionControl);
            _mockBillingFilePath.Setup(x => x.FetchClaimSubmissionDataForERA(transactionControl)).ReturnsAsync((ClaimSubmissionEntity)null);
            _mockCommonService.Setup(x => x.GetclearinghouseNameById(clearinghouseId)).ReturnsAsync(new ClearingHouseDetailsModel());

            var mockUploader = new Mock<IClearingHouseUploader>();
            mockUploader.Setup(u => u.UploadFileToSftpAsync(It.IsAny<ClearingHouseDetailsModel>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<int>())).ThrowsAsync(new InvalidOperationException("upload failed"));
            _mockClearingHouseUploaderFactory.Setup(f => f.Create()).Returns(mockUploader.Object);

            var service = new EdiUploadService(_mockLogger.Object, _mockCommonService.Object, _mockBillingBlobService.Object, _mockBillingFilePath.Object, _mockClearingHouseUploaderFactory.Object, _mockEdiFilesDownload.Object, _mockClaimSubmissionHandler.Object);

            var method = typeof(EdiUploadService).GetMethod("UploadAsync", BindingFlags.Instance | BindingFlags.NonPublic);

            // Act & Assert
            var task = (Task<OperationResult>)method.Invoke(service, new object[] { clearinghouseId, claimId, ediData });
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await task);
        }

        #region ValidateAllClearinghousesAsync Tests

        [Fact]
        public async Task ValidateAllClearinghousesAsync_AllValid_ReturnsSuccessResponse()
        {
            // Arrange
            _mockCommonService
                .Setup(x => x.GetclearinghouseNameById(1))
                .ReturnsAsync(new ClearingHouseDetailsModel { ClearingHouseId = 1, Title = "Availity" });
            _mockCommonService
                .Setup(x => x.GetclearinghouseNameById(8))
                .ReturnsAsync(new ClearingHouseDetailsModel { ClearingHouseId = 8, Title = "Stedi" });

            var validationResults = new List<ClearinghouseCredentialValidationResult>
            {
                new ClearinghouseCredentialValidationResult
                {
                    ClearinghouseName = "Availity",
                    ClearinghouseId = 1,
                    IsValid = true,
                    ValidatedAt = DateTime.UtcNow,
                    DurationMs = 1000
                },
                new ClearinghouseCredentialValidationResult
                {
                    ClearinghouseName = "Stedi",
                    ClearinghouseId = 8,
                    IsValid = true,
                    ValidatedAt = DateTime.UtcNow,
                    DurationMs = 1200
                }
            };

            var mockUploader = new Mock<IClearingHouseUploader>();
            mockUploader
                .Setup(x => x.ValidateMultipleClearinghousesAsync(It.IsAny<List<ClearingHouseDetailsModel>>()))
                .ReturnsAsync(validationResults);

            _mockClearingHouseUploaderFactory
                .Setup(f => f.Create())
                .Returns(mockUploader.Object);

            // Act
            var result = await _ediUploadService.ValidateAllClearinghousesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.AllValid);
            Assert.Equal(2, result.TotalClearinghouses);
            Assert.Equal(2, result.SuccessfulValidations);
            Assert.Equal(0, result.FailedValidations);
            Assert.Equal(2, result.Results.Count);
        }

        [Fact]
        public async Task ValidateAllClearinghousesAsync_SomeFailures_ReturnsPartialSuccess()
        {
            // Arrange
            _mockCommonService
                .Setup(x => x.GetclearinghouseNameById(1))
                .ReturnsAsync(new ClearingHouseDetailsModel { ClearingHouseId = 1, Title = "Availity" });
            _mockCommonService
                .Setup(x => x.GetclearinghouseNameById(8))
                .ReturnsAsync(new ClearingHouseDetailsModel { ClearingHouseId = 8, Title = "Stedi" });

            var validationResults = new List<ClearinghouseCredentialValidationResult>
            {
                new ClearinghouseCredentialValidationResult
                {
                    ClearinghouseName = "Availity",
                    ClearinghouseId = 1,
                    IsValid = false,
                    ErrorMessage = "Authentication failed - invalid credentials",
                    ValidatedAt = DateTime.UtcNow,
                    DurationMs = 2000
                },
                new ClearinghouseCredentialValidationResult
                {
                    ClearinghouseName = "Stedi",
                    ClearinghouseId = 8,
                    IsValid = true,
                    ValidatedAt = DateTime.UtcNow,
                    DurationMs = 1100
                }
            };

            var mockUploader = new Mock<IClearingHouseUploader>();
            mockUploader
                .Setup(x => x.ValidateMultipleClearinghousesAsync(It.IsAny<List<ClearingHouseDetailsModel>>()))
                .ReturnsAsync(validationResults);

            _mockClearingHouseUploaderFactory
                .Setup(f => f.Create())
                .Returns(mockUploader.Object);

            // Act
            var result = await _ediUploadService.ValidateAllClearinghousesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.False(result.AllValid);
            Assert.Equal(2, result.TotalClearinghouses);
            Assert.Equal(1, result.SuccessfulValidations);
            Assert.Equal(1, result.FailedValidations);
            Assert.Contains(result.Results, r => !r.IsValid && r.ClearinghouseName == "Availity");
            Assert.Contains(result.Results, r => r.IsValid && r.ClearinghouseName == "Stedi");
        }

        [Fact]
        public async Task ValidateAllClearinghousesAsync_NoClearinghouses_ReturnsAllValid()
        {
            // Arrange - GetclearinghouseNameById returns null for all enum values
            _mockCommonService
                .Setup(x => x.GetclearinghouseNameById(It.IsAny<int>()))
                .ReturnsAsync((ClearingHouseDetailsModel)null);

            // Act
            var result = await _ediUploadService.ValidateAllClearinghousesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.AllValid);
            Assert.Equal(0, result.TotalClearinghouses);
            Assert.Equal(0, result.SuccessfulValidations);
            Assert.Equal(0, result.FailedValidations);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task ValidateAllClearinghousesAsync_NullClearinghouses_ReturnsAllValid()
        {
            // Arrange - GetclearinghouseNameById returns null for all enum values
            _mockCommonService
                .Setup(x => x.GetclearinghouseNameById(It.IsAny<int>()))
                .ReturnsAsync((ClearingHouseDetailsModel)null);

            // Act
            var result = await _ediUploadService.ValidateAllClearinghousesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.AllValid);
            Assert.Equal(0, result.TotalClearinghouses);
        }

        #endregion
    }
}
