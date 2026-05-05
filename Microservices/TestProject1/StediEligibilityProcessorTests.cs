using Billing.FolderStructure.Core.Enum;
using Billing.FolderStructure.Core.Models;
using Billing.FolderStructure.Core.Services;
using Billing.FolderStructure.Core.Utils;
using BillingService.Domain.Interfaces.Billing;
using ClearingHouseService.Web.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Dtos.ClearingHouse;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models.EligibilityRequest;

namespace TestProject1
{
    public class StediEligibilityProcessorTests
    {
        private StediEligibilityProcessor CreateProcessor(
            Mock<IStediEligibilityClient>? stedi = null,
            Mock<IBillingBlobService>? billingBlob = null,
            Mock<IX12Parser<Eligibility271ParsedResponse>>? parser = null,
            Mock<IEligibility271Repository>? repository = null,
            Mock<IBillingFilePath>? billingFilePath = null)
        {
            stedi ??= new Mock<IStediEligibilityClient>();
            billingBlob ??= new Mock<IBillingBlobService>();
            parser ??= new Mock<IX12Parser<Eligibility271ParsedResponse>>();
            repository ??= new Mock<IEligibility271Repository>();
            billingFilePath ??= new Mock<IBillingFilePath>();

            var mockBlobBackup = new Mock<ICHService>();
            var mockConfiguration = new Mock<IConfiguration>();
            var mockLogger = new Mock<ILogger<StediEligibilityProcessor>>();

            return new StediEligibilityProcessor(
                stedi.Object,
                billingBlob.Object,
                parser.Object,
                repository.Object,
                billingFilePath.Object,
                billingBlob.Object,
                mockBlobBackup.Object,
                mockConfiguration.Object);
        }

        [Fact]
        public async Task ProcessAsync_WhenStediReturnsFailure_SavesFailureAndUploadsFailed()
        {
            // Arrange
            var mockStedi = new Mock<IStediEligibilityClient>();
            var mockBillingBlob = new Mock<IBillingBlobService>();
            var mockParser = new Mock<IX12Parser<Eligibility271ParsedResponse>>();
            var mockRepository = new Mock<IEligibility271Repository>();
            var mockBillingFilePath = new Mock<IBillingFilePath>();

            mockBillingFilePath
                .Setup(x => x.GetTransactionControlNumber(It.IsAny<string>()))
                .ReturnsAsync(new TransactionControlNumberModel { FileType = ((int)FileTypes.Type270).ToString(), ControlNumbers = new int?[] { 0 } });

            mockBillingFilePath
                .Setup(x => x.CreateFolderPath(It.IsAny<BillingRequest>()))
                .ReturnsAsync("container/path/file.edi");

            mockBillingFilePath
                .Setup(x => x.SplitFilePath(It.IsAny<string>()))
                .ReturnsAsync(("containerName", "fullFilePath/edi.edi"));

            mockBillingBlob
                .Setup(x => x.UploadIntoContainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
                .ReturnsAsync("uploaded.edi");

            mockStedi
                .Setup(s => s.Submit270Async(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Eligibility271ParsedResponse { IsSuccess = false, FailureResponse = "validation error" });

            Eligibility271ResponseEntity savedEntity = null!;
            mockRepository
                .Setup(r => r.SaveAsync(It.IsAny<Eligibility271ResponseEntity>(), It.IsAny<CancellationToken>()))
                .Callback<Eligibility271ResponseEntity, CancellationToken>((e, ct) => savedEntity = e)
                .Returns(Task.CompletedTask);

            var processor = CreateProcessor(mockStedi, mockBillingBlob, mockParser, mockRepository, mockBillingFilePath);

            var job = new StediEligibilityJobDTO
            {
                CorrelationId = Guid.NewGuid(),
                FunderId = 111,
                AccountId = 222,
                Edi270Request = "ISA*..."
            };

            // Act
            await processor.ProcessAsync(job, CancellationToken.None);

            // Assert
            mockStedi.Verify(s => s.Submit270Async(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            mockRepository.Verify(r => r.SaveAsync(It.IsAny<Eligibility271ResponseEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.NotNull(savedEntity);
            Assert.Equal("validation error", savedEntity.FailureResponse);
            mockBillingBlob.Verify(b => b.UploadIntoContainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ProcessAsync_WhenStediSuccessAndParserSucceeds_ParsesAndSaves()
        {
            // Arrange
            var mockStedi = new Mock<IStediEligibilityClient>();
            var mockBillingBlob = new Mock<IBillingBlobService>();
            var mockParser = new Mock<IX12Parser<Eligibility271ParsedResponse>>();
            var mockRepository = new Mock<IEligibility271Repository>();
            var mockBillingFilePath = new Mock<IBillingFilePath>();

            mockBillingFilePath
                .Setup(x => x.GetTransactionControlNumber(It.IsAny<string>()))
                .ReturnsAsync(new TransactionControlNumberModel { FileType = ((int)FileTypes.Type270).ToString(), ControlNumbers = new int?[] { 0 } });

            mockBillingFilePath
                .Setup(x => x.CreateFolderPath(It.IsAny<BillingRequest>()))
                .ReturnsAsync("container/path/file.edi");

            mockBillingFilePath
                .Setup(x => x.SplitFilePath(It.IsAny<string>()))
                .ReturnsAsync(("containerName", "fullFilePath/edi.edi"));

            mockBillingBlob
                .Setup(x => x.UploadIntoContainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
                .ReturnsAsync("uploaded.edi");

            var stediResponse = new Eligibility271ParsedResponse { IsSuccess = true, X12Response = "ISA~EB*1*30~DTP*472*RD8*20200101-20201231~" };
            mockStedi
                .Setup(s => s.Submit270Async(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stediResponse);

            var parsed = new Eligibility271ParsedResponse { IsSuccess = true, CoverageStatus = "Active" };
            mockParser
                .Setup(p => p.Parse(It.IsAny<string>()))
                .Returns(parsed);

            Eligibility271ResponseEntity savedEntity = null!;
            mockRepository
                .Setup(r => r.SaveAsync(It.IsAny<Eligibility271ResponseEntity>(), It.IsAny<CancellationToken>()))
                .Callback<Eligibility271ResponseEntity, CancellationToken>((e, ct) => savedEntity = e)
                .Returns(Task.CompletedTask);

            var processor = CreateProcessor(mockStedi, mockBillingBlob, mockParser, mockRepository, mockBillingFilePath);

            var job = new StediEligibilityJobDTO
            {
                CorrelationId = Guid.NewGuid(),
                FunderId = 111,
                AccountId = 222,
                Edi270Request = "ISA*...",
                EffectiveDate = DateTime.UtcNow
            };

            // Act
            await processor.ProcessAsync(job, CancellationToken.None);

            // Assert
            mockParser.Verify(p => p.Parse(It.IsAny<string>()), Times.Once);
            mockRepository.Verify(r => r.SaveAsync(It.IsAny<Eligibility271ResponseEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.NotNull(savedEntity);
            Assert.Equal(job.CorrelationId, savedEntity.TransactionControlNumber);
            Assert.Equal("Active", savedEntity.CoverageStatus);
        }

        [Fact]
        public async Task ProcessAsync_WhenParserThrows_SavesFailureResponse()
        {
            // Arrange
            var mockStedi = new Mock<IStediEligibilityClient>();
            var mockBillingBlob = new Mock<IBillingBlobService>();
            var mockParser = new Mock<IX12Parser<Eligibility271ParsedResponse>>();
            var mockRepository = new Mock<IEligibility271Repository>();
            var mockBillingFilePath = new Mock<IBillingFilePath>();

            mockBillingFilePath
                .Setup(x => x.GetTransactionControlNumber(It.IsAny<string>()))
                .ReturnsAsync(new TransactionControlNumberModel { FileType = ((int)FileTypes.Type270).ToString(), ControlNumbers = new int?[] { 0 } });

            mockBillingFilePath
                .Setup(x => x.CreateFolderPath(It.IsAny<BillingRequest>()))
                .ReturnsAsync("container/path/file.edi");

            mockBillingFilePath
                .Setup(x => x.SplitFilePath(It.IsAny<string>()))
                .ReturnsAsync(("containerName", "fullFilePath/edi.edi"));

            mockBillingBlob
                .Setup(x => x.UploadIntoContainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
                .ReturnsAsync("uploaded.edi");

            var stediResponse = new Eligibility271ParsedResponse { IsSuccess = true, X12Response = "ISA~EB*1*30~DTP*472*RD8*20200101-20201231~" };
            mockStedi
                .Setup(s => s.Submit270Async(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stediResponse);

            mockParser
                .Setup(p => p.Parse(It.IsAny<string>()))
                .Throws(new InvalidOperationException("parse failed"));

            Eligibility271ResponseEntity savedEntity = null!;
            mockRepository
                .Setup(r => r.SaveAsync(It.IsAny<Eligibility271ResponseEntity>(), It.IsAny<CancellationToken>()))
                .Callback<Eligibility271ResponseEntity, CancellationToken>((e, ct) => savedEntity = e)
                .Returns(Task.CompletedTask);

            var processor = CreateProcessor(mockStedi, mockBillingBlob, mockParser, mockRepository, mockBillingFilePath);

            var job = new StediEligibilityJobDTO
            {
                CorrelationId = Guid.NewGuid(),
                FunderId = 111,
                AccountId = 222,
                Edi270Request = "ISA*...",
                EffectiveDate = DateTime.UtcNow
            };

            // Act
            await processor.ProcessAsync(job, CancellationToken.None);

            // Assert
            mockRepository.Verify(r => r.SaveAsync(It.IsAny<Eligibility271ResponseEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.NotNull(savedEntity);
            Assert.False(string.IsNullOrEmpty(savedEntity.FailureResponse));
            Assert.Contains("Failed to parse 271 response", savedEntity.FailureResponse);
        }
    }
}
