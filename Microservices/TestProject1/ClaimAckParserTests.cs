using Billing.FolderStructure.Core.Enum;
using Billing.FolderStructure.Core.Models;
using Billing.FolderStructure.Core.Services;
using Billing.FolderStructure.Core.Utils;
using BillingService.Domain.Interfaces.Files;
using EdiFabric.Core.Model.Edi;
using EdiFabric.Templates.Hipaa5010;
using EraParserService.Domain.Services;
using EraParserService.Domain.Services.EdiParsers.Edi277;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.Validation;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Handlers;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;


namespace ClearingHouse
{
    public class ClaimAckParserTests
    {
        private readonly Mock<IRepository<BillingDbContext, PaymentEraUploadEntity>> _paymentEraUploadRepositoryMock;
        private readonly Mock<IBillingBlobService> _billingBlobServiceMock;
        private readonly Mock<ILoggerFactory> _loggerFactoryMock;
        private readonly Mock<ILogger> _loggerMock;
        private readonly Mock<IRepository<BillingDbContext, ClaimValidationErrorEntity>> _claimValidationErrorRepositoryMock;
        private readonly Mock<IRepository<BillingDbContext, ClaimErrorMessageEntity>> _claimErrorMessageRepositoryMock;
        private readonly Mock<IRepository<BillingDbContext, ClaimEntity>> _claimRepositoryMock;
        private readonly Mock<IRepository<BillingDbContext, ClaimSubmissionEntity>> _claimSubmissionRepositoryMock;
        private readonly Mock<IRepository<BillingDbContext, ExternalCodeEntity>> _externalCodeRepositoryMock;
        private readonly Mock<IRepository<BillingDbContext, ClearingHouseResponseDetailsEntity>> _clearingHouseResponseRepositoryMock;
        private readonly Mock<IBaseClaimService> _claimServiceMock;
        private readonly Mock<IBillingFilePath> _billingFilePathMock;
        private readonly Mock<IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity>> _claimSubmissionFunderSequenceMock;
        private readonly Mock<IEdiFilesDownload> _ediFilesDownloadMock;

        private readonly ClaimAckParser _sut;

        private const string FileIdentifier = "path/to/partner/filename.277";
        private const string ContainerName = "billing-container";
        private const string FullFilePath = "full/file/path.277";

        public ClaimAckParserTests()
        {
            _paymentEraUploadRepositoryMock = new Mock<IRepository<BillingDbContext, PaymentEraUploadEntity>>();
            _billingBlobServiceMock = new Mock<IBillingBlobService>();
            _loggerFactoryMock = new Mock<ILoggerFactory>();
            _loggerMock = new Mock<ILogger>();
            _claimValidationErrorRepositoryMock = new Mock<IRepository<BillingDbContext, ClaimValidationErrorEntity>>();
            _claimErrorMessageRepositoryMock = new Mock<IRepository<BillingDbContext, ClaimErrorMessageEntity>>();
            _claimRepositoryMock = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            _claimSubmissionRepositoryMock = new Mock<IRepository<BillingDbContext, ClaimSubmissionEntity>>();
            _externalCodeRepositoryMock = new Mock<IRepository<BillingDbContext, ExternalCodeEntity>>();
            _clearingHouseResponseRepositoryMock = new Mock<IRepository<BillingDbContext, ClearingHouseResponseDetailsEntity>>();
            _claimServiceMock = new Mock<IBaseClaimService>();
            _billingFilePathMock = new Mock<IBillingFilePath>();
            _claimSubmissionFunderSequenceMock = new Mock<IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity>>();
            _ediFilesDownloadMock = new Mock<IEdiFilesDownload>();

            _loggerFactoryMock
                .Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(_loggerMock.Object);

            _billingFilePathMock
                .Setup(x => x.CreateFolderPath(It.IsAny<BillingRequest>()))
                .ReturnsAsync($"{ContainerName}/{FullFilePath}");

            _billingFilePathMock
                .Setup(x => x.SplitFilePath(It.IsAny<string>()))
                .ReturnsAsync((ContainerName, FullFilePath));

            _billingBlobServiceMock
                .Setup(x => x.Update277DetailedReportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<string>());

            _billingBlobServiceMock
                .Setup(x => x.Update277DailySummaryReportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
                .Returns(Task.CompletedTask);

            _sut = new ClaimAckParser(
                _paymentEraUploadRepositoryMock.Object,
                _billingBlobServiceMock.Object,
                _loggerFactoryMock.Object,
                _claimValidationErrorRepositoryMock.Object,
                _claimErrorMessageRepositoryMock.Object,
                _claimRepositoryMock.Object,
                _claimSubmissionRepositoryMock.Object,
                _externalCodeRepositoryMock.Object,
                _clearingHouseResponseRepositoryMock.Object,
                _claimServiceMock.Object,
                _billingFilePathMock.Object,
                _claimSubmissionFunderSequenceMock.Object,
                _ediFilesDownloadMock.Object);
        }

        private EdiDownloadData BuildEdiDownloadData(string fileIdentifier = FileIdentifier) =>
            new EdiDownloadData
            {
                FileIdentifier = fileIdentifier,
                EdiData = "raw-edi-data",
                ClearingHouseId = 1,
                DownloadDateTime = DateTime.UtcNow
            };

        private ClaimSubmissionEntity BuildClaimSubmission(int accountInfoId = 42, int claimId = 10, int submissionId = 1) =>
            new ClaimSubmissionEntity
            {
                Id = submissionId,
                ClaimId = claimId,
                Claim = new ClaimEntity { Id = claimId, AccountInfoId = accountInfoId }
            };

        private void SetupAlreadyProcessed(bool alreadyProcessed)
        {
            var data = alreadyProcessed
                ? new List<ClearingHouseResponseDetailsEntity>
                  {
                      new ClearingHouseResponseDetailsEntity { FileIdentifier = FileIdentifier }
                  }.AsQueryable()
                : Enumerable.Empty<ClearingHouseResponseDetailsEntity>().AsQueryable();

            _clearingHouseResponseRepositoryMock
                .Setup(x => x.Query())
                .Returns(data);
        }

        [Fact]
        public async Task ParseAsync_Always_UploadsDetailedReport()
        {
            // Arrange
            SetupAlreadyProcessed(true); // early exit after blob calls
            var ediDownloadData = BuildEdiDownloadData();
            var claimSubmission = BuildClaimSubmission();

            // Act
            await _sut.ParseAsync(ediDownloadData, new List<IEdiItem>(), Array.Empty<byte>(), claimSubmission, FileIdentifier);

            // Assert
            _billingBlobServiceMock.Verify(x =>
                x.Update277DetailedReportAsync(
                    BillingConstants.BillingContainerName,
                    ediDownloadData.EdiData,
                    FullFilePath),
                Times.Once);
        }

        [Fact]
        public async Task ParseAsync_Always_UpdatesDailySummaryReport()
        {
            // Arrange
            SetupAlreadyProcessed(true);
            var ediDownloadData = BuildEdiDownloadData();
            var claimSubmission = BuildClaimSubmission();

            // Act
            await _sut.ParseAsync(ediDownloadData, new List<IEdiItem>(), Array.Empty<byte>(), claimSubmission, FileIdentifier);

            // Assert
            _billingBlobServiceMock.Verify(x =>
                x.Update277DailySummaryReportAsync(
                    BillingConstants.BillingContainerName,
                    ediDownloadData.EdiData,
                    FullFilePath,
                    It.IsAny<List<string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ParseAsync_Always_CallsCreateFolderPathTwice()
        {
            // Arrange — once for Detailed, once for Daily
            SetupAlreadyProcessed(true);
            var ediDownloadData = BuildEdiDownloadData();

            // Act
            await _sut.ParseAsync(ediDownloadData, new List<IEdiItem>(), Array.Empty<byte>(), BuildClaimSubmission(), FileIdentifier);

            // Assert
            _billingFilePathMock.Verify(x => x.CreateFolderPath(It.IsAny<BillingRequest>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ParseAsync_WhenAlreadyProcessed_DoesNotIterateEdiItems()
        {
            // Arrange
            SetupAlreadyProcessed(true);
            var ediDownloadData = BuildEdiDownloadData();

            var ts277Mock = new Mock<TS277A>();
            var ediItems = new List<IEdiItem> { ts277Mock.Object };

            // Act
            await _sut.ParseAsync(ediDownloadData, ediItems, Array.Empty<byte>(), BuildClaimSubmission(), FileIdentifier);

            // Assert — no claim status work performed
            _claimServiceMock.Verify(x =>
                x.UpdateClaimStatus(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<ClaimStatus>()),
                Times.Never);
        }

        [Fact]
        public async Task ParseAsync_WhenNotAlreadyProcessed_LogsParsingMessage()
        {
            // Arrange
            SetupAlreadyProcessed(false);
            var ediDownloadData = BuildEdiDownloadData();

            // Act
            await _sut.ParseAsync(ediDownloadData, new List<IEdiItem>(), Array.Empty<byte>(), BuildClaimSubmission(), FileIdentifier);

            // Assert
            _loggerMock.Verify(x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Parsing 277 file")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ParseAsync_WhenClaimSubmissionIsNull_UsesZeroAccountInfoId()
        {
            // Arrange
            SetupAlreadyProcessed(true);
            var ediDownloadData = BuildEdiDownloadData();

            BillingRequest capturedRequest = null!;
            _billingFilePathMock
                .Setup(x => x.CreateFolderPath(It.IsAny<BillingRequest>()))
                .Callback<BillingRequest>(r => capturedRequest = r)
                .ReturnsAsync($"{ContainerName}/{FullFilePath}");

            // Act
            await _sut.ParseAsync(ediDownloadData, new List<IEdiItem>(), Array.Empty<byte>(), null!, FileIdentifier);

            // Assert
            Assert.Equal(0, capturedRequest.AccountInfoId);
        }

        [Fact]
        public async Task ParseAsync_WhenClaimSubmissionIsNull_TransactionNumberIsNull()
        {
            // Arrange
            SetupAlreadyProcessed(true);
            var ediDownloadData = BuildEdiDownloadData();

            BillingRequest capturedRequest = null!;
            _billingFilePathMock
                .Setup(x => x.CreateFolderPath(It.IsAny<BillingRequest>()))
                .Callback<BillingRequest>(r => capturedRequest = r)
                .ReturnsAsync($"{ContainerName}/{FullFilePath}");

            // Act
            await _sut.ParseAsync(ediDownloadData, new List<IEdiItem>(), Array.Empty<byte>(), null!, FileIdentifier);

            // Assert
            Assert.Null(capturedRequest.TransactionNumber);
        }

        [Fact]
        public async Task ParseAsync_BuildsBillingRequest_WithDailySubFolder()
        {
            // Arrange
            SetupAlreadyProcessed(true);
            var ediDownloadData = BuildEdiDownloadData();
            var claimSubmission = BuildClaimSubmission();

            var capturedRequests = new List<BillingRequest>();
            _billingFilePathMock
                .Setup(x => x.CreateFolderPath(It.IsAny<BillingRequest>()))
                .Callback<BillingRequest>(r => capturedRequests.Add(r))
                .ReturnsAsync($"{ContainerName}/{FullFilePath}");

            // Act
            await _sut.ParseAsync(ediDownloadData, new List<IEdiItem>(), Array.Empty<byte>(), claimSubmission, FileIdentifier);

            // Assert — second call uses Daily sub-folder
            Assert.Equal(BlobFolderNames.Daily.ToString(), capturedRequests[1].SubFolderName);
        }

        [Fact]
        public async Task ParseAsync_BuildsBillingRequest_WithCorrectClearingHouseId()
        {
            // Arrange
            SetupAlreadyProcessed(true);
            var ediDownloadData = BuildEdiDownloadData();
            ediDownloadData.ClearingHouseId = 5;

            BillingRequest capturedRequest = null!;
            _billingFilePathMock
                .Setup(x => x.CreateFolderPath(It.IsAny<BillingRequest>()))
                .Callback<BillingRequest>(r => capturedRequest = r)
                .ReturnsAsync($"{ContainerName}/{FullFilePath}");

            // Act
            await _sut.ParseAsync(ediDownloadData, new List<IEdiItem>(), Array.Empty<byte>(), BuildClaimSubmission(), FileIdentifier);

            // Assert
            Assert.Equal(5, capturedRequest.ClearingHouseId);
        }

        [Fact]
        public async Task ParseAsync_BuildsBillingRequest_FileNameIsLastSegmentOfIdentifier()
        {
            // Arrange
            SetupAlreadyProcessed(true);
            var ediDownloadData = BuildEdiDownloadData();

            BillingRequest capturedRequest = null!;
            _billingFilePathMock
                .Setup(x => x.CreateFolderPath(It.IsAny<BillingRequest>()))
                .Callback<BillingRequest>(r => capturedRequest = r)
                .ReturnsAsync($"{ContainerName}/{FullFilePath}");

            // Act
            await _sut.ParseAsync(ediDownloadData, new List<IEdiItem>(), Array.Empty<byte>(), BuildClaimSubmission(), FileIdentifier);

            // Assert — "filename.277" is the last segment of "path/to/partner/filename.277"
            Assert.Equal("filename.277", capturedRequest.FieldIdentifier);
        }

        [Fact]
        public async Task ParseAsync_WhenNoTS277AItems_DoesNotCallClaimService()
        {
            // Arrange
            SetupAlreadyProcessed(false);
            var ediDownloadData = BuildEdiDownloadData();

            // ediItems contains no TS277A instances
            var ediItems = new List<IEdiItem> { new Mock<IEdiItem>().Object };

            // Act
            await _sut.ParseAsync(ediDownloadData, ediItems, Array.Empty<byte>(), BuildClaimSubmission(), FileIdentifier);

            // Assert
            _claimServiceMock.Verify(x =>
                x.UpdateClaimStatus(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<ClaimStatus>()),
                Times.Never);
        }

        [Fact]
        public async Task ParseAsync_WhenEdiItemsListIsEmpty_DoesNotThrow()
        {
            // Arrange
            SetupAlreadyProcessed(false);
            var ediDownloadData = BuildEdiDownloadData();

            // Act & Assert
            var exception = await Record.ExceptionAsync(() =>
                _sut.ParseAsync(ediDownloadData, new List<IEdiItem>(), Array.Empty<byte>(), BuildClaimSubmission(), FileIdentifier));

            Assert.Null(exception);
        }

        [Fact]
        public async Task ParseAsync_WhenPatientLoopIsNull_LogsMissingPatientLoop()
        {
            // Arrange
            SetupAlreadyProcessed(false);
            var ediDownloadData = BuildEdiDownloadData();

            var loop2200D = new List<Loop_2200D_277A>();
            var loop2000D = new List<Loop_2000D_277A>(); // empty — triggers "missing" branch
            var loop2000C = new Loop_2000C_277A { Loop2000D = loop2000D };
            var loop2000B = new Loop_2000B_277A { Loop2000C = new List<Loop_2000C_277A> { loop2000C } };
            var loop2000A = new Loop_2000A_277A { Loop2000B = loop2000B };
            var ts277 = new TS277A { Loop2000A = loop2000A };

            var ediItems = new List<IEdiItem> { ts277 };

            // Act
            await _sut.ParseAsync(ediDownloadData, ediItems, Array.Empty<byte>(), BuildClaimSubmission(), FileIdentifier);

            // Assert
            _loggerMock.Verify(x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Patient loop is missing")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ParseAsync_WhenPatientLoopIsEmpty_DoesNotCallHandleClaimStatus()
        {
            // Arrange
            SetupAlreadyProcessed(false);
            var ediDownloadData = BuildEdiDownloadData();

            var loop2000C = new Loop_2000C_277A { Loop2000D = new List<Loop_2000D_277A>() };
            var loop2000B = new Loop_2000B_277A { Loop2000C = new List<Loop_2000C_277A> { loop2000C } };
            var loop2000A = new Loop_2000A_277A { Loop2000B = loop2000B };
            var ts277 = new TS277A { Loop2000A = loop2000A };

            // Act
            await _sut.ParseAsync(ediDownloadData, new List<IEdiItem> { ts277 }, Array.Empty<byte>(), BuildClaimSubmission(), FileIdentifier);

            // Assert
            _claimServiceMock.Verify(x =>
                x.UpdateClaimStatus(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<ClaimStatus>()),
                Times.Never);
        }

        [Fact]
        public async Task ParseAsync_WhenPatientHasClaimStatusLoop_InvokesClaimStatusHandlingForEachClaim()
        {
            // Arrange
            SetupAlreadyProcessed(false);
            var ediDownloadData = BuildEdiDownloadData();
            var claimSubmission = BuildClaimSubmission();

            // Build a reject STC so HandleRejectStatus is exercised without needing real EDI objects
            var stc = new STC_ClaimLevelStatusInformation_2
            {
                HealthCareClaimStatus_01 = new C043_HealthCareClaimStatus_2
                {
                    HealthCareClaimStatusCategoryCode_01 = "A3",
                    StatusCode_02 = "18"
                }
            };

            var trn = new TRN_ClaimStatusTrackingNumber_2 { CurrentTransactionTraceNumber_02 = "SUB-001" };

            var claimStatusLoop1 = new Loop_2200D_277A
            {
                STC_ClaimLevelStatusInformation = new List<STC_ClaimLevelStatusInformation_2> { stc },
                TRN_ClaimStatusTrackingNumber = trn
            };
            var claimStatusLoop2 = new Loop_2200D_277A
            {
                STC_ClaimLevelStatusInformation = new List<STC_ClaimLevelStatusInformation_2> { stc },
                TRN_ClaimStatusTrackingNumber = trn
            };

            var patient = new Loop_2000D_277A
            {
                Loop2200D = new List<Loop_2200D_277A> { claimStatusLoop1, claimStatusLoop2 }
            };

            var loop2000C = new Loop_2000C_277A { Loop2000D = new List<Loop_2000D_277A> { patient } };
            var loop2000B = new Loop_2000B_277A { Loop2000C = new List<Loop_2000C_277A> { loop2000C } };
            var loop2000A = new Loop_2000A_277A { Loop2000B = loop2000B };
            var ts277 = new TS277A { Loop2000A = loop2000A };

            // No submission found → error path, but proves iteration happens twice
            _claimSubmissionRepositoryMock
                .Setup(x => x.Query())
                .Returns(Enumerable.Empty<ClaimSubmissionEntity>().AsQueryable());

            _billingFilePathMock
                .Setup(x => x.GetTransactionControlNumber(It.IsAny<string>()))
                .ReturnsAsync(new TransactionControlNumberModel());

            _billingFilePathMock
                .Setup(x => x.FetchClaimSubmissionDataForERA(It.IsAny<TransactionControlNumberModel>()))
                .ReturnsAsync((ClaimSubmissionEntity)null!);

            _billingBlobServiceMock
                .Setup(x => x.UploadIntoContainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.IO.MemoryStream>()))
                .ReturnsAsync(string.Empty);

            _billingBlobServiceMock
                .Setup(x => x.DeleteBlobFromContainerAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _ediFilesDownloadMock
                .Setup(x => x.SaveClaimEdiFilePath(It.IsAny<BillingRequest>(), It.IsAny<string>(), It.IsAny<ClaimSubmissionEntity>()))
                .ReturnsAsync(true);

            // Act
            await _sut.ParseAsync(ediDownloadData, new List<IEdiItem> { ts277 }, Array.Empty<byte>(), claimSubmission, FileIdentifier);

            // Assert — error logged twice (once per claim status)
            _loggerMock.Verify(x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("not found")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task ParseAsync_WhenMultipleTS277AItems_ProcessesEachTransaction()
        {
            // Arrange
            SetupAlreadyProcessed(false);
            var ediDownloadData = BuildEdiDownloadData();
            var claimSubmission = BuildClaimSubmission();

            static TS277A BuildEmptyTs277()
            {
                var loop2000C = new Loop_2000C_277A { Loop2000D = new List<Loop_2000D_277A>() };
                var loop2000B = new Loop_2000B_277A { Loop2000C = new List<Loop_2000C_277A> { loop2000C } };
                var loop2000A = new Loop_2000A_277A { Loop2000B = loop2000B };
                return new TS277A { Loop2000A = loop2000A };
            }

            var ediItems = new List<IEdiItem> { BuildEmptyTs277(), BuildEmptyTs277() };

            // Act
            await _sut.ParseAsync(ediDownloadData, ediItems, Array.Empty<byte>(), claimSubmission, FileIdentifier);

            // Assert — "Patient loop is missing" logged for each TS277A with an empty patient loop
            _loggerMock.Verify(x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Patient loop is missing")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Exactly(2));
        }
    }
}
