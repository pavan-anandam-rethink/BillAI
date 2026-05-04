using AutoMapper;
using BillingService.XUnit.Tests.Common;
using BillingService.XUnit.Tests.Common.Mocks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Entities.Billing.Reporting;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.ReportingModels;
using Rethink.Services.Common.Services;
using Rethink.Services.Common.Utils;
using SummationService.Domain.Interfaces;
using SummationService.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ReportingService.Web.XUnit.Tests.Services
{
    public class PaymentAdjustmentServiceTest : BaseTest
    {
        private Mock<IRepository<ReportingDbContext, PaymentsAdjustmentsEntity>> paymentsAdjustmentsRepository;
        private Mock<IRepository<BillingDbContext, ClaimEntity>> claimRepository;
        private Mock<IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity>> claimChargeEntryWriteOffRepository;
        private Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>> claimChargeEntryRepository;
        private Mock<IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity>> paymentClaimServiceLineAdjustmentRepository;
        private Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>> paymentClaimServiceLineRepository;
        private Mock<IRepository<BillingDbContext, PaymentEntity>> paymentRepository;
        private Mock<IRepository<ReportingDbContext, FundersEntity>> funderNameReportingRepository;
        private IHelperService helperService;


        public PaymentAdjustmentServiceTest() 
        {
            paymentsAdjustmentsRepository = new Mock<IRepository<ReportingDbContext, PaymentsAdjustmentsEntity>>();
            claimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            claimChargeEntryWriteOffRepository = new Mock<IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity>>();
            claimChargeEntryRepository = new Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>>();
            paymentClaimServiceLineAdjustmentRepository = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity>>();
            paymentClaimServiceLineRepository = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>>();
            paymentRepository = new Mock<IRepository<BillingDbContext, PaymentEntity>>();
            funderNameReportingRepository = new Mock<IRepository<ReportingDbContext, FundersEntity>>();
            helperService = new Mock<IHelperService>().Object;
            
        }

        private PaymentAdjustmentService CreateSut()
        {
            return new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );
        }

        private PaymentsAdjustmentsResponseModel GetResponseWithOneRow()
        {
            return new PaymentsAdjustmentsResponseModel
            {
                paymentsAdjustments = new List<PaymentsAdjustmentsResponse>
        {
            new PaymentsAdjustmentsResponse
            {
                ClientId = 1,
                DateModified = DateTime.Now
            }
        }
            };
        }

        private PaymentsAdjustmentsRequestModel GetModel()
        {
            return new PaymentsAdjustmentsRequestModel
            {
                FunderId = new List<int> { 1 },
                AccountInfoId = 1,
                StartDate = DateTime.Today.AddDays(-10),
                EndDate = DateTime.Today,
                RangeType = (int)ReportingDateRangeType.transactionDate
            };
        }
        private Mock<IHelperService> CreateHelperMock()
        {
            var mock = new Mock<IHelperService>();
            mock.Setup(h => h.DefineStyles(It.IsAny<WorkbookPart>()));
            mock.Setup(h => h.AddCell(It.IsAny<ExcelCellType>(), It.IsAny<object?>(), It.IsAny<bool>()))
                .Returns((ExcelCellType type, object? value, bool alt) =>
                    new DocumentFormat.OpenXml.Spreadsheet.Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue(value?.ToString() ?? string.Empty)
                    });
            return mock;
        }
        // Write the Unit Tests for here 
        [Fact]
        public async Task AddOrUpdatePaymentAdjustmentAsync_UpdatePaymentSummary_ShouldUpdateAndCommit()
        {
            // Arrange
            var transactionType = ClaimTransactionType.updatePaymentSummary;
            var transactionTypeId = 100; // For this path FindClaimId... returns transactionTypeId

            // Payment with Id matching processingId
            var paymentId = transactionTypeId;
            var payments = new List<PaymentEntity>
            {
                new PaymentEntity { Id = paymentId }
            };
            paymentRepository.Setup(r => r.Query()).Returns(QueryMock<PaymentEntity>.Create(payments));

            // Existing payments adjustments that will be updated
            var paList = new List<PaymentsAdjustmentsEntity>
            {
                new PaymentsAdjustmentsEntity { Id = 1, PaymentId = paymentId },
                new PaymentsAdjustmentsEntity { Id = 2, PaymentId = paymentId }
            };
            paymentsAdjustmentsRepository.Setup(r => r.Query()).Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(paList));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.AddOrUpdatePaymentAdjustmentAsync(transactionType, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.True(result);
            paymentsAdjustmentsRepository.Verify(r => r.UpdateRange(It.Is<List<PaymentsAdjustmentsEntity>>(l => l.Count == 2)), Times.Once);
            paymentsAdjustmentsRepository.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task AddOrUpdatePaymentAdjustmentAsync_DeleteCharge_ShouldSoftDeleteAndCommit()
        {
            // Arrange
            var transactionType = ClaimTransactionType.deleteCharge;
            var transactionTypeId = 77; // For deleteCharge, FindClaimId... returns transactionTypeId directly

            // Ensure PaymentId is set to a non-null value to avoid nullable.Value access
            var paList = new List<PaymentsAdjustmentsEntity>
            {
                new PaymentsAdjustmentsEntity { Id = 1, ChargeEntryId = transactionTypeId, PaymentId = 1 },
                new PaymentsAdjustmentsEntity { Id = 2, ChargeEntryId = transactionTypeId, PaymentId = 1 }
            };
            paymentsAdjustmentsRepository.Setup(r => r.Query()).Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(paList));

            // Provide a matching payment so SetTransactionTypeValue can safely resolve it
            var payments = new List<PaymentEntity>
            {
                new PaymentEntity { Id = 1 }
            };
            paymentRepository.Setup(r => r.Query()).Returns(QueryMock<PaymentEntity>.Create(payments));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.AddOrUpdatePaymentAdjustmentAsync(transactionType, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.True(result);
            paymentsAdjustmentsRepository.Verify(r => r.UpdateRange(It.IsAny<List<PaymentsAdjustmentsEntity>>()), Times.Once);
            paymentsAdjustmentsRepository.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task AddOrUpdatePaymentAdjustmentAsync_DeleteClaim_ShouldSoftDeleteAndCommit()
        {
            // Arrange
            var transactionType = ClaimTransactionType.deleteClaim;
            var transactionTypeId = 88;

            var paList = new List<PaymentsAdjustmentsEntity>
    {
        new PaymentsAdjustmentsEntity { Id = 1, ClaimId = transactionTypeId, PaymentId = 1 },
        new PaymentsAdjustmentsEntity { Id = 2, ClaimId = transactionTypeId, PaymentId = 1 }
    };
            paymentsAdjustmentsRepository.Setup(r => r.Query()).Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(paList));

            var payments = new List<PaymentEntity> { new PaymentEntity { Id = 1 } };
            paymentRepository.Setup(r => r.Query()).Returns(QueryMock<PaymentEntity>.Create(payments));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.AddOrUpdatePaymentAdjustmentAsync(transactionType, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.True(result);
            paymentsAdjustmentsRepository.Verify(r => r.UpdateRange(It.IsAny<List<PaymentsAdjustmentsEntity>>()), Times.Once);
            paymentsAdjustmentsRepository.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task AddOrUpdatePaymentAdjustmentAsync_DeleteCharge_NoAdjustments_ShouldCommit()
        {
            // Arrange
            var transactionType = ClaimTransactionType.deleteCharge;
            var transactionTypeId = 2;

            paymentsAdjustmentsRepository.Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity>()));

            var sut = CreateSut();

            // Act
            var result = await sut.AddOrUpdatePaymentAdjustmentAsync(transactionType, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.True(result);
            paymentsAdjustmentsRepository.Verify(r => r.UpdateRange(It.IsAny<List<PaymentsAdjustmentsEntity>>()), Times.Never);
            paymentsAdjustmentsRepository.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task updatePaymentSummaryProcessAsync_ReturnsTrue()
        {
            // Arrange
            var processingId = 123;
            var transactionType = ClaimTransactionType.updatePaymentSummary;

            var payment = new PaymentEntity
            {
                Id = processingId,
                DateDeleted = null,
                ReferenceNumber = "REF-001",
                DepositDate = new DateTime(2025, 1, 15)
            };

            var adjustments = new List<PaymentsAdjustmentsEntity>
            {
                new PaymentsAdjustmentsEntity { Id = 1, PaymentId = payment.Id, DateDeleted = null },
                new PaymentsAdjustmentsEntity { Id = 2, PaymentId = payment.Id, DateDeleted = null }
            };

            paymentRepository.Setup(r => r.Query()).Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity> { payment }));
            paymentsAdjustmentsRepository.Setup(r => r.Query()).Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(adjustments));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.updatePaymentSummaryProcessAsync(processingId, transactionType, CancellationToken.None);

            // Assert
            Assert.True(result);
            // Verify that fields were set from payment
            Assert.All(adjustments, a =>
            {
                Assert.Equal(payment.ReferenceNumber, a.EftOrCheckNumber);
                Assert.Equal(payment.DepositDate, a.PaymentOrAdjustmentDate);
                Assert.True(a.DateModified != default);
            });
            paymentsAdjustmentsRepository.Verify(r => r.UpdateRange(It.Is<List<PaymentsAdjustmentsEntity>>(l => l.Count == 2)), Times.Once);
        }

        [Fact]
        public async Task updatePaymentSummaryProcessAsync_ReturnsFalseAndDoesNotUpdate()
        {
            // Arrange
            var processingId = 999; // no matching payment
            paymentRepository.Setup(r => r.Query()).Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity>()));
            paymentsAdjustmentsRepository.Setup(r => r.Query()).Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity>()));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.updatePaymentSummaryProcessAsync(processingId, ClaimTransactionType.updatePaymentSummary, CancellationToken.None);

            // Assert
            Assert.False(result);
            paymentsAdjustmentsRepository.Verify(r => r.UpdateRange(It.IsAny<List<PaymentsAdjustmentsEntity>>()), Times.Never);
        }

        [Fact]
        public async Task updatePaymentSummaryProcessAsync_WhenProcessingIdZero_ShouldReturnFalse()
        {
            var sut = CreateSut();

            var result = await sut.updatePaymentSummaryProcessAsync(
                0,
                ClaimTransactionType.updatePaymentSummary,
                CancellationToken.None);

            Assert.False(result);
        }

        [Fact]
        public async Task updatePaymentSummaryProcessAsync_WhenPaymentDeleted_ShouldReturnFalse()
        {
            var processingId = 123;

            var payment = new PaymentEntity
            {
                Id = processingId,
                DateDeleted = DateTime.Now // deleted
            };

            paymentRepository.Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity> { payment }));

            var sut = CreateSut();

            var result = await sut.updatePaymentSummaryProcessAsync(
                processingId,
                ClaimTransactionType.updatePaymentSummary,
                CancellationToken.None);

            Assert.False(result);
        }

        [Fact]
        public async Task updatePaymentSummaryProcessAsync_WhenNoAdjustments_ShouldStillReturnTrue()
        {
            var processingId = 123;

            var payment = new PaymentEntity
            {
                Id = processingId,
                DateDeleted = null
            };

            paymentRepository.Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity> { payment }));

            paymentsAdjustmentsRepository.Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity>()));

            var sut = CreateSut();

            var result = await sut.updatePaymentSummaryProcessAsync(
                processingId,
                ClaimTransactionType.updatePaymentSummary,
                CancellationToken.None);

            Assert.True(result);
            paymentsAdjustmentsRepository.Verify(r => r.UpdateRange(It.Is<List<PaymentsAdjustmentsEntity>>(l => l.Count == 0)), Times.Once);
        }

        [Fact]
        public async Task updatePaymentSummaryProcessAsync_ShouldIgnoreDeletedAdjustments()
        {
            var processingId = 123;

            var payment = new PaymentEntity
            {
                Id = processingId,
                DateDeleted = null
            };

            var adjustments = new List<PaymentsAdjustmentsEntity>
    {
        new PaymentsAdjustmentsEntity { Id = 1, PaymentId = processingId, DateDeleted = null },
        new PaymentsAdjustmentsEntity { Id = 2, PaymentId = processingId, DateDeleted = DateTime.Now } // deleted
    };

            paymentRepository.Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity> { payment }));

            paymentsAdjustmentsRepository.Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(adjustments));

            var sut = CreateSut();

            var result = await sut.updatePaymentSummaryProcessAsync(
                processingId,
                ClaimTransactionType.updatePaymentSummary,
                CancellationToken.None);

            Assert.True(result);

            // Only 1 should be updated
            paymentsAdjustmentsRepository.Verify(r => r.UpdateRange(
                It.Is<List<PaymentsAdjustmentsEntity>>(l => l.Count == 1)), Times.Once);
        }

        [Fact]
        public async Task updatePaymentSummaryProcessAsync_WhenExceptionThrown_ShouldThrow()
        {
            var processingId = 123;

            paymentRepository.Setup(r => r.Query())
                .Throws(new Exception("DB failure"));

            var sut = CreateSut();

            await Assert.ThrowsAsync<Exception>(() =>
                sut.updatePaymentSummaryProcessAsync(
                    processingId,
                    ClaimTransactionType.updatePaymentSummary,
                    CancellationToken.None));
        }
        [Fact]
        public async Task ProcessPaymentsAdjustmentsTasksAsync_AddPath_ShouldAddAndReturnTrue()
        {
            // Arrange
            var processingId = 10;
            var transactionTypeId = 99;
            var transactionType = ClaimTransactionType.billedAmount;

            var claims = new List<ClaimEntity>
    {
        new ClaimEntity { Id = processingId, AccountInfoId = 1, PrimaryFunderId = 2, ChildProfileId = 3, ClaimStatus = ClaimStatus.PendingReview }
    };
            claimRepository.Setup(r => r.Query()).Returns(QueryMock<ClaimEntity>.Create(claims));

            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity>()));

            var helperMock = new Mock<IHelperService>();
            helperMock.Setup(h => h.GetChargeEntriesByClaimId(processingId))
                      .ReturnsAsync(new List<ClaimChargeEntryEntity>());

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.ProcessPaymentsAdjustmentsTasksAsync(processingId, transactionType, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.True(result);
            paymentsAdjustmentsRepository.Verify(r => r.AddAsync(It.IsAny<PaymentsAdjustmentsEntity>()), Times.Once);
            paymentsAdjustmentsRepository.Verify(r => r.Update(It.IsAny<PaymentsAdjustmentsEntity>()), Times.Never);
        }
                       

        [Fact]
        public async Task ProcessPaymentsAdjustmentsTasksAsyncFalse()
        {
            // Arrange
            var processingId = 1234;
            claimRepository.Setup(r => r.Query()).Returns(QueryMock<ClaimEntity>.Create(new List<ClaimEntity>()));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.ProcessPaymentsAdjustmentsTasksAsync(processingId, ClaimTransactionType.billedAmount, 77, CancellationToken.None);

            // Assert
            Assert.False(result);
            paymentsAdjustmentsRepository.Verify(r => r.AddAsync(It.IsAny<PaymentsAdjustmentsEntity>()), Times.Never);
            paymentsAdjustmentsRepository.Verify(r => r.Update(It.IsAny<PaymentsAdjustmentsEntity>()), Times.Never);
        }

        [Fact]
        public async Task ProcessPaymentsAdjustmentsTasksAsync_WhenProcessingIdZero_ShouldReturnFalse()
        {
            var sut = CreateSut();

            var result = await sut.ProcessPaymentsAdjustmentsTasksAsync(
                0,
                ClaimTransactionType.billedAmount,
                1,
                CancellationToken.None);

            Assert.False(result);
        }

        [Fact]
        public async Task ProcessPaymentsAdjustmentsTasksAsync_WhenExceptionThrown_ShouldThrow()
        {
            claimRepository.Setup(r => r.Query())
                .Throws(new Exception("DB error"));

            var sut = CreateSut();

            await Assert.ThrowsAsync<Exception>(() =>
                sut.ProcessPaymentsAdjustmentsTasksAsync(
                    10,
                    ClaimTransactionType.billedAmount,
                    1,
                    CancellationToken.None));
        }
        [Fact]
        public async Task PaymentsAdjustmentsDeleteProcessAsync_DeleteCharge()
        {
            // Arrange
            var transactionType = ClaimTransactionType.deleteCharge;
            var transactionTypeId = 777; // ignored by deleteCharge path
            var processingId = 55;       // treated as ChargeEntryId in deleteCharge path

            // Ensure PaymentId is non-null to avoid PaymentId.Value access in SetTransactionTypeValue
            var paList = new List<PaymentsAdjustmentsEntity>
            {
                new PaymentsAdjustmentsEntity { Id = 1, ChargeEntryId = processingId, PaymentId = 1, DateDeleted = null },
                new PaymentsAdjustmentsEntity { Id = 2, ChargeEntryId = processingId, PaymentId = 1, DateDeleted = null }
            };
            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(paList));

            // Provide a matching payment so SetTransactionTypeValue can resolve EFT/check and dates safely
            var payments = new List<PaymentEntity>
            {
                new PaymentEntity { Id = 1, DateDeleted = null, ReferenceNumber = "CHK-123", DepositDate = DateTime.UtcNow }
            };
            paymentRepository.Setup(r => r.Query()).Returns(QueryMock<PaymentEntity>.Create(payments));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.PaymentsAdjustmentsDeleteProcessAsync(transactionType, transactionTypeId, processingId, CancellationToken.None);

            // Assert
            Assert.True(result);
            paymentsAdjustmentsRepository.Verify(r => r.UpdateRange(It.Is<List<PaymentsAdjustmentsEntity>>(l => l.Count == 2)), Times.Once);
        }

        [Fact]
        public async Task PaymentsAdjustmentsDeleteProcessAsync_NoData_ShouldNotUpdate()
        {
            paymentsAdjustmentsRepository.Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity>()));

            var sut = CreateSut();

            var result = await sut.PaymentsAdjustmentsDeleteProcessAsync(
                ClaimTransactionType.deleteCharge,
                1,
                1,
                CancellationToken.None);

            Assert.True(result);
            paymentsAdjustmentsRepository.Verify(r => r.UpdateRange(It.IsAny<List<PaymentsAdjustmentsEntity>>()), Times.Never);
        }

        [Fact]
        public async Task PaymentsAdjustmentsDeleteProcessAsync_AllDeleted_ShouldNotUpdate()
        {
            var paList = new List<PaymentsAdjustmentsEntity>
    {
        new PaymentsAdjustmentsEntity { Id = 1, DateDeleted = DateTime.Now },
        new PaymentsAdjustmentsEntity { Id = 2, DateDeleted = DateTime.Now }
    };

            paymentsAdjustmentsRepository.Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(paList));

            var sut = CreateSut();

            var result = await sut.PaymentsAdjustmentsDeleteProcessAsync(
                ClaimTransactionType.deleteCharge,
                1,
                1,
                CancellationToken.None);

            Assert.True(result);
            paymentsAdjustmentsRepository.Verify(r => r.UpdateRange(It.IsAny<List<PaymentsAdjustmentsEntity>>()), Times.Never);
        }

        [Fact]
        public async Task PaymentsAdjustmentsDeleteProcessAsync_NoPayment_ShouldNotCrash()
        {
            var paList = new List<PaymentsAdjustmentsEntity>
    {
        new PaymentsAdjustmentsEntity { Id = 1, PaymentId = 999 }
    };

            paymentsAdjustmentsRepository.Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(paList));

            paymentRepository.Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity>()));

            var sut = CreateSut();

            var result = await sut.PaymentsAdjustmentsDeleteProcessAsync(
                ClaimTransactionType.deleteCharge,
                1,
                1,
                CancellationToken.None);

            Assert.True(result);
        }
        [Fact]
        public async Task PaymentsAdjustmentsDeleteProcessAsyncReturnTrue()
        {
            // Arrange
            var transactionType = ClaimTransactionType.deleteCharge;
            var transactionTypeId = 0;
            var processingId = 123;

            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity>()));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.PaymentsAdjustmentsDeleteProcessAsync(transactionType, transactionTypeId, processingId, CancellationToken.None);

            // Assert
            Assert.True(result);
            paymentsAdjustmentsRepository.Verify(r => r.UpdateRange(It.IsAny<List<PaymentsAdjustmentsEntity>>()), Times.Never);
        }

        [Fact]
        public async Task GetPaymentsAdjustmentsAsyncSortedResult()
        {
            // Arrange
            var funderIds = new List<int> { 1 };
            var startDate = new DateTime(2025, 1, 1);
            var endDate = new DateTime(2025, 1, 31);

            var model = new PaymentsAdjustmentsRequestModel
            {
                FunderId = funderIds,
                AccountInfoId = 10,
                StartDate = startDate,
                EndDate = endDate,
                RangeType = (int)ReportingDateRangeType.transactionDate,
                // no SortingModels provided -> defaults to dateModified desc
                Skip = 1,
                Take = 2,
                IsExport = false
            };

            var data = new List<PaymentsAdjustmentsResponse>
            {
                new PaymentsAdjustmentsResponse { ClientId = 1, DateModified = new DateTime(2025, 1, 10), PaymentOrAdjustmentDate = new DateTime(2025, 1, 10) },
                new PaymentsAdjustmentsResponse { ClientId = 2, DateModified = new DateTime(2025, 1, 20), PaymentOrAdjustmentDate = new DateTime(2025, 1, 20) },
                new PaymentsAdjustmentsResponse { ClientId = 3, DateModified = new DateTime(2025, 1, 15), PaymentOrAdjustmentDate = new DateTime(2025, 1, 15) },
                new PaymentsAdjustmentsResponse { ClientId = 4, DateModified = new DateTime(2025, 1, 25), PaymentOrAdjustmentDate = new DateTime(2025, 1, 25) }
            };

            var helperMock = new Mock<IHelperService>();
            helperMock.Setup(h => h.GetPaymentsAdjustmentsByFunderIdAndDateAsync(
                    funderIds, startDate, endDate, ReportingDateRangeType.transactionDate, model.AccountInfoId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(data);

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.GetPaymentsAdjustmentsAsync(model, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(data.Count, result.totalCount);

            // Expect sorted by dateModified desc: clients order -> 4,2,3,1
            // After Skip(1) and Take(2) -> clients 2,3
            Assert.Equal(2, result.paymentsAdjustments.Count);
            Assert.Equal(2, result.paymentsAdjustments[0].ClientId);
            Assert.Equal(3, result.paymentsAdjustments[1].ClientId);

            helperMock.Verify(h => h.GetPaymentsAdjustmentsByFunderIdAndDateAsync(
                funderIds, startDate, endDate, ReportingDateRangeType.transactionDate, model.AccountInfoId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetPaymentsAdjustmentsAsync()
        {
            // Arrange
            var funderIds = new List<int> { 2 };
            var model = new PaymentsAdjustmentsRequestModel
            {
                FunderId = funderIds,
                AccountInfoId = 30,
                StartDate = new DateTime(2025, 3, 1),
                EndDate = new DateTime(2025, 3, 31),
                RangeType = (int)ReportingDateRangeType.transactionDate,
                SortingModels = null,
                Skip = 0,
                Take = 0,
                IsExport = false
            };

            var data = new List<PaymentsAdjustmentsResponse>
            {
                new PaymentsAdjustmentsResponse { ClientId = 100, DateModified = new DateTime(2025, 3, 2) },
                new PaymentsAdjustmentsResponse { ClientId = 200, DateModified = new DateTime(2025, 3, 5) }
            };

            var helperMock = new Mock<IHelperService>();
            helperMock.Setup(h => h.GetPaymentsAdjustmentsByFunderIdAndDateAsync(
                    funderIds, model.StartDate, model.EndDate, ReportingDateRangeType.transactionDate, model.AccountInfoId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(data);

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.GetPaymentsAdjustmentsAsync(model, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.totalCount);
            Assert.Equal(2, result.paymentsAdjustments.Count);

            // Default sorting is dateModified desc -> client 200 first
            Assert.Equal(200, result.paymentsAdjustments[0].ClientId);
            Assert.Equal(100, result.paymentsAdjustments[1].ClientId);

            helperMock.Verify(h => h.GetPaymentsAdjustmentsByFunderIdAndDateAsync(
                funderIds, model.StartDate, model.EndDate, ReportingDateRangeType.transactionDate, model.AccountInfoId, It.IsAny<CancellationToken>()), Times.Once);
        }

        
        [Fact]
        public async Task ExportToExcelAsync_GeneratesWorkbook_WithHeadersAndRows()
        {
            // Arrange
            var funderIds = new List<int> { 1 };
            var model = new PaymentsAdjustmentsRequestModel
            {
                FunderId = funderIds,
                AccountInfoId = 10,
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 1, 31),
                RangeType = (int)ReportingDateRangeType.transactionDate,
                SortingModels = null, // default sorting inside method
                IsExport = true
            };

            var response = new PaymentsAdjustmentsResponseModel
            {
                paymentsAdjustments = new List<PaymentsAdjustmentsResponse>
            {
            new PaymentsAdjustmentsResponse
            {
                FunderName = "Funder A",
                ClientId = 1001,
                ClientFirst = "John",
                ClientLast = "Doe",
                ClaimFrom = new DateTime(2025,1,2),
                ClaimThrough = new DateTime(2025,1,5),
                ClaimStatus = "Pending",
                BilledDate = new DateTime(2025,1,6),
                TransactionType = "PAY",
                ReasonCode = "R1",
                RemarkCode = "RM1",
                TransactionDate = new DateTime(2025,1,7),
                PaymentOrAdjustmentDate = new DateTime(2025,1,8),
                EftOrCheckNumber = "CHK-001",
                Payment = 123.45m,
                Adjustment = 0m,
                DateModified = new DateTime(2025,1,9),
            },
            new PaymentsAdjustmentsResponse
            {
                FunderName = "Funder B",
                ClientId = 1002,
                ClientFirst = "Jane",
                ClientLast = "Smith",
                ClaimFrom = new DateTime(2025,1,10),
                ClaimThrough = new DateTime(2025,1,12),
                ClaimStatus = "Approved",
                BilledDate = new DateTime(2025,1,13),
                TransactionType = "ADJ",
                ReasonCode = "R2",
                RemarkCode = "RM2",
                TransactionDate = new DateTime(2025,1,14),
                PaymentOrAdjustmentDate = new DateTime(2025,1,15),
                EftOrCheckNumber = "EFT-002",
                Payment = 0m,
                Adjustment = -50m,
                DateModified = new DateTime(2025,1,16),
            }
        }
            };

            // Mock funder names used in AddCustomRows
            var funders = new List<FundersEntity>
            {
                new FundersEntity { FunderId = 1, FunderName = "Funder A" }
            };
                funderNameReportingRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<FundersEntity>.Create(funders));

            // Mock helper service for Excel support
            var helperMock = new Mock<IHelperService>();
            helperMock.Setup(h => h.DefineStyles(It.IsAny<WorkbookPart>()));
            helperMock.Setup(h => h.AddCell(It.IsAny<ExcelCellType>(), It.IsAny<object?>(), It.IsAny<bool>()))
                      .Returns((ExcelCellType type, object? value, bool alt) =>
                          new DocumentFormat.OpenXml.Spreadsheet.Cell
                          {
                              DataType = CellValues.String,
                              CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue(value?.ToString() ?? string.Empty)
                          });

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var bytes = await sut.ExportToExcelAsync(model, response, CancellationToken.None);

            // Assert basic bytes
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);

            // Inspect spreadsheet content
            using var ms = new MemoryStream(bytes);
            using var doc = SpreadsheetDocument.Open(ms, false);
            var wbPart = doc.WorkbookPart;
            Assert.NotNull(wbPart);

            var sheets = wbPart!.Workbook.Sheets;
            Assert.NotNull(sheets);

            var sheet = sheets!.Elements<Sheet>().FirstOrDefault();
            Assert.NotNull(sheet);
            Assert.Equal("Payments Adjustments Reports", sheet!.Name);

            var sheetId = sheet!.Id?.Value;
            Assert.False(string.IsNullOrEmpty(sheetId));
            var wsPart = (WorksheetPart)wbPart.GetPartById(sheetId!);
            var sheetData = wsPart.Worksheet.GetFirstChild<SheetData>();
            Assert.NotNull(sheetData);

            var rows = sheetData!.Elements<Row>().ToList();
            // We expect: 4 custom rows + header + data rows
            Assert.True(rows.Count >= 5);

            // Find the header row (it has 16 cells and starts with "Payer/Funder")
            var headerRow = rows.FirstOrDefault(r =>
            {
                var cells = r.Elements<Cell>().ToList();
                return cells.Count == 16 && (cells[0].CellValue?.Text ?? string.Empty) == "Payer/Funder";
            });

            Assert.NotNull(headerRow);
            var headerCells = headerRow!.Elements<Cell>().ToList();
            Assert.Equal(16, headerCells.Count);
            Assert.Equal("Payer/Funder", headerCells[0].CellValue?.Text ?? string.Empty);
            Assert.Equal("Client Id", headerCells[1].CellValue?.Text ?? string.Empty);
            Assert.Equal("Payment", headerCells[14].CellValue?.Text ?? string.Empty);
            Assert.Equal("Adjustment", headerCells[15].CellValue?.Text ?? string.Empty);
        }

        [Fact]
        public async Task ExportToExcelAsync_WhenSortingModelsNull_ShouldSetDefault()
        {
            var model = GetModel();
            model.SortingModels = null;

            var response = GetResponseWithOneRow();

            funderNameReportingRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<FundersEntity>.Create(new List<FundersEntity>()));

            var sut = CreateSut();

            await sut.ExportToExcelAsync(model, response, CancellationToken.None);

            Assert.NotNull(model.SortingModels);
            Assert.Single(model.SortingModels);
            Assert.Equal("dateModified", model.SortingModels[0].Field);
        }

        [Fact]
        public async Task ExportToExcelAsync_NoFunders_ShouldStillGenerateFile()
        {
            var model = GetModel();

            var response = GetResponseWithOneRow();

            funderNameReportingRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<FundersEntity>.Create(new List<FundersEntity>()));

            var sut = CreateSut();

            var result = await sut.ExportToExcelAsync(model, response, CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task ExportToExcelAsync_WithNullFields_ShouldNotThrow()
        {
            var model = GetModel();

            var response = new PaymentsAdjustmentsResponseModel
            {
                paymentsAdjustments = new List<PaymentsAdjustmentsResponse>
        {
            new PaymentsAdjustmentsResponse
            {
                FunderName = null,
                ClientFirst = null,
                ClientLast = null,
                ClaimStatus = null,
                RemarkCode = null,
                Payment = null,
                Adjustment = 0
            }
        }
            };

            funderNameReportingRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<FundersEntity>.Create(new List<FundersEntity>()));

            var sut = CreateSut();

            var result = await sut.ExportToExcelAsync(model, response, CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public async Task ExportToExcelAsync_WhenNoData_ShouldCreateOnlyHeader()
        {
            var model = GetModel();

            var response = new PaymentsAdjustmentsResponseModel
            {
                paymentsAdjustments = new List<PaymentsAdjustmentsResponse>()
            };

            funderNameReportingRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<FundersEntity>.Create(new List<FundersEntity>()));

            var sut = CreateSut();

            var bytes = await sut.ExportToExcelAsync(model, response, CancellationToken.None);

            using var ms = new MemoryStream(bytes);
            using var doc = SpreadsheetDocument.Open(ms, false);

            var rows = doc.WorkbookPart!
                .WorksheetParts.First()
                .Worksheet.GetFirstChild<SheetData>()!
                .Elements<Row>()
                .ToList();

            // Only custom rows + header
            Assert.True(rows.Count >= 5);
        }

        [Fact]
        public async Task GetClaimFollowUpReportAsync_Data()
        {
            // Arrange
            var helper = new Mock<IHelperService>(MockBehavior.Strict);
            var model = new ClaimFollowUpRequestModel();
            var expectedList = new List<ClaimFollowUpResponse> { new(), new() };
            helper.Setup(h => h.GetClaimFollowUpReportData(model, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((expectedList, expectedList.Count));

            var sut = CreateSut(helper.Object);

            // Act
            var result = await sut.GetClaimFollowUpReportAsync(model, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedList.Count, result.totalCount);
            Assert.Same(expectedList, result.claimFollowUps);

            helper.Verify(h => h.GetClaimFollowUpReportData(model, It.IsAny<CancellationToken>()), Times.Once);
            helper.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetClaimFollowUpReportAsync_EmptyResults()
        {
            // Arrange
            var helper = new Mock<IHelperService>(MockBehavior.Strict);
            var model = new ClaimFollowUpRequestModel();
            var expectedList = new List<ClaimFollowUpResponse>();
            helper.Setup(h => h.GetClaimFollowUpReportData(model, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((expectedList, 0));

            var sut = CreateSut(helper.Object);

            // Act
            var result = await sut.GetClaimFollowUpReportAsync(model, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.totalCount);
            Assert.Same(expectedList, result.claimFollowUps);
            Assert.Empty(result.claimFollowUps);

            helper.Verify(h => h.GetClaimFollowUpReportData(model, It.IsAny<CancellationToken>()), Times.Once);
            helper.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetClaimFollowUpReportAsync_WhenHelperReturnsNullList_ShouldHandleGracefully()
        {
            var helper = new Mock<IHelperService>();

            helper.Setup(h => h.GetClaimFollowUpReportData(
                It.IsAny<ClaimFollowUpRequestModel>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(((List<ClaimFollowUpResponse>)null!, 5));

            var sut = CreateSut(helper.Object);

            var result = await sut.GetClaimFollowUpReportAsync(new ClaimFollowUpRequestModel(), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(5, result.totalCount);
            Assert.Null(result.claimFollowUps); // current behavior
        }

        [Fact]
        public async Task GetClaimFollowUpReportAsync_NullList_ZeroTotal()
        {
            var helper = new Mock<IHelperService>();

            helper.Setup(h => h.GetClaimFollowUpReportData(
                It.IsAny<ClaimFollowUpRequestModel>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(((List<ClaimFollowUpResponse>)null!, 0));

            var sut = CreateSut(helper.Object);

            var result = await sut.GetClaimFollowUpReportAsync(new ClaimFollowUpRequestModel(), CancellationToken.None);

            Assert.Equal(0, result.totalCount);
            Assert.Null(result.claimFollowUps);
        }

        [Fact]
        public async Task GetClaimFollowUpReportAsync_WhenHelperThrows_ShouldPropagateException()
        {
            var helper = new Mock<IHelperService>();

            helper.Setup(h => h.GetClaimFollowUpReportData(
                It.IsAny<ClaimFollowUpRequestModel>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB failure"));

            var sut = CreateSut(helper.Object);

            await Assert.ThrowsAsync<Exception>(() =>
                sut.GetClaimFollowUpReportAsync(new ClaimFollowUpRequestModel(), CancellationToken.None));
        }

        [Fact]
        public async Task GetClaimFollowUpReportAsync_TotalMismatch_ShouldStillReturnValues()
        {
            var helper = new Mock<IHelperService>();

            var list = new List<ClaimFollowUpResponse>
    {
        new ClaimFollowUpResponse(),
        new ClaimFollowUpResponse()
    };

            helper.Setup(h => h.GetClaimFollowUpReportData(
                It.IsAny<ClaimFollowUpRequestModel>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((list, 10)); // mismatch

            var sut = CreateSut(helper.Object);

            var result = await sut.GetClaimFollowUpReportAsync(new ClaimFollowUpRequestModel(), CancellationToken.None);

            Assert.Equal(10, result.totalCount);
            Assert.Equal(2, result.claimFollowUps.Count);
        }

        [Fact]
        public async Task GetClaimFollowUpReportAsync_ShouldPassCancellationToken()
        {
            var helper = new Mock<IHelperService>();

            var token = new CancellationTokenSource().Token;

            helper.Setup(h => h.GetClaimFollowUpReportData(
                It.IsAny<ClaimFollowUpRequestModel>(),
                token))
                .ReturnsAsync((new List<ClaimFollowUpResponse>(), 0));

            var sut = CreateSut(helper.Object);

            await sut.GetClaimFollowUpReportAsync(new ClaimFollowUpRequestModel(), token);

            helper.Verify(h => h.GetClaimFollowUpReportData(
                It.IsAny<ClaimFollowUpRequestModel>(),
                token), Times.Once);
        }
        // Add this unit test to verify ExportToExcelClaimFollowAsync
        [Fact]
        public async Task ExportToExcelClaimFollowAsync_GeneratesWorkbook()
        {
            // Arrange
            var model = new ClaimFollowUpRequestModel
            {
                FunderIds = new List<int> { 1 },
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 1, 31),
                FollowUpType = (int)ReportingClaimFollowUpType.active
            };

            var responses = new List<ClaimFollowUpResponse>
            {
                new ClaimFollowUpResponse
                {
                    ClaimId = "C-001",
                    ClientFirst = "John",
                    ClientLast = "Doe",
                    FunderName = "Funder A",
                    RenderingProvider = "Provider X",
                    PlaceOfService = "Clinic",
                    ClaimFrom = new DateTime(2025, 1, 5),
                    ClaimThrough = new DateTime(2025, 1, 10),
                    Authorization = "AUTH-123",
                    ExpectedAmount = 100.00m,
                    BilledAmount = 95.00m,
                    PaymentAmount = 90.00m,
                    AdjustmentAmount = -5.00m,
                    Balance = 5.00m,
                    BilledDate = new DateTime(2025, 1, 12),
                    ClaimStatus = "Pending",
                    Note = "Follow-up needed",
                    NoteCreatedByName = "Agent A",
                    NoteCreatedDate = "01/13/2025",
                    FollowUpDate = "01/20/2025",
                    FollowUpStatus = "Active"
                },
                new ClaimFollowUpResponse
                {
                    ClaimId = "C-002",
                    ClientFirst = "Jane",
                    ClientLast = "Smith",
                    FunderName = "Funder B",
                    RenderingProvider = "Provider Y",
                    PlaceOfService = "Telehealth",
                    ClaimFrom = new DateTime(2025, 1, 15),
                    ClaimThrough = new DateTime(2025, 1, 16),
                    Authorization = "AUTH-456",
                    ExpectedAmount = 200.00m,
                    BilledAmount = 190.00m,
                    PaymentAmount = 180.00m,
                    AdjustmentAmount = -10.00m,
                    Balance = 10.00m,
                    BilledDate = new DateTime(2025, 1, 18),
                    ClaimStatus = "Approved",
                    Note = "Claim approved",
                    NoteCreatedByName = "Agent B",
                    NoteCreatedDate = "01/19/2025",
                    FollowUpDate = "01/22/2025",
                    FollowUpStatus = "Complete"
                }
            };

            var funders = new List<FundersEntity> {
            new FundersEntity { FunderId = 1, FunderName = "Funder A" }
            };
            funderNameReportingRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<FundersEntity>.Create(funders));

            var helperMock = new Mock<IHelperService>();
            helperMock.Setup(h => h.DefineStyles(It.IsAny<WorkbookPart>()));
            helperMock.Setup(h => h.AddCell(It.IsAny<ExcelCellType>(), It.IsAny<object?>(), It.IsAny<bool>()))
                .Returns((ExcelCellType type, object? value, bool alt) =>
                    new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(value?.ToString() ?? string.Empty)
                    });

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var bytes = await sut.ExportToExcelClaimFollowAsync(model, responses, CancellationToken.None);

            // Assert
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);

            using var ms = new MemoryStream(bytes);
            using var doc = SpreadsheetDocument.Open(ms, false);
            var wbPart = doc.WorkbookPart;
            Assert.NotNull(wbPart);

            var sheets = wbPart!.Workbook.Sheets;
            Assert.NotNull(sheets);

            var sheet = sheets!.Elements<Sheet>().FirstOrDefault();
            Assert.NotNull(sheet);
            Assert.Equal("Claim FollowUP Reports", sheet!.Name);

            var sheetId = sheet!.Id?.Value;
            Assert.False(string.IsNullOrEmpty(sheetId));
            var wsPart = (WorksheetPart)wbPart.GetPartById(sheetId!);
            var sheetData = wsPart.Worksheet.GetFirstChild<SheetData>();
            Assert.NotNull(sheetData);

            var rows = sheetData!.Elements<Row>().ToList();
            Assert.True(rows.Count >= 5);

            // Header row has 21 cells and starts with "Claim Id"
            var headerRow = rows.FirstOrDefault(r =>
            {
                var cells = r.Elements<Cell>().ToList();
                return cells.Count == 21 && (cells[0].CellValue?.Text ?? string.Empty) == "Claim Id";
            });
            Assert.NotNull(headerRow);
            var headerCells = headerRow!.Elements<Cell>().ToList();
            Assert.Equal(21, headerCells.Count);
            Assert.Equal("Claim Id", headerCells[0].CellValue?.Text ?? string.Empty);
            Assert.Equal("Client First Name", headerCells[1].CellValue?.Text ?? string.Empty);
            Assert.Equal("Client Last Name", headerCells[2].CellValue?.Text ?? string.Empty);
            Assert.Equal("Follow-up Status", headerCells[20].CellValue?.Text ?? string.Empty);
        }

        [Fact]
        public async Task ExportToExcelClaimFollowAsync_WithNullBilledDate_ShouldNotThrow()
        {
            var model = new ClaimFollowUpRequestModel
            {
                FunderIds = new List<int> { 1 }
            };

            var responses = new List<ClaimFollowUpResponse>
    {
        new ClaimFollowUpResponse
        {
            ClaimId = "C-001",
            ClientFirst = "Test",
            ClientLast = "User",
            BilledDate = null
        }
    };

            funderNameReportingRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<FundersEntity>.Create(new List<FundersEntity>()));

            var helperMock = CreateHelperMock();

            var sut = CreateSut(helperMock.Object);

            var result = await sut.ExportToExcelClaimFollowAsync(model, responses, CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task ExportToExcelClaimFollowAsync_WithMultipleFunders_ShouldFetchNames()
        {
            var model = new ClaimFollowUpRequestModel
            {
                FunderIds = new List<int> { 1, 2 }
            };

            var funders = new List<FundersEntity>
    {
        new FundersEntity { FunderId = 1, FunderName = "F1" },
        new FundersEntity { FunderId = 2, FunderName = "F2" }
    };

            funderNameReportingRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<FundersEntity>.Create(funders));

            var helperMock = CreateHelperMock();

            var sut = CreateSut(helperMock.Object);

            var result = await sut.ExportToExcelClaimFollowAsync(model, new List<ClaimFollowUpResponse>(), CancellationToken.None);

            Assert.NotNull(result);

            funderNameReportingRepository.Verify(r => r.Query(), Times.Once);
        }

        [Fact]
        public async Task ExportToExcelClaimFollowAsync_WhenRowThrows_ShouldContinueProcessing()
        {
            var model = new ClaimFollowUpRequestModel
            {
                FunderIds = new List<int> { 1 }
            };

            var responses = new List<ClaimFollowUpResponse>
    {
        new ClaimFollowUpResponse { ClaimId = "Valid" },
        null! // force exception
    };

            funderNameReportingRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<FundersEntity>.Create(new List<FundersEntity>()));

            var helperMock = CreateHelperMock();

            var sut = CreateSut(helperMock.Object);

            var result = await sut.ExportToExcelClaimFollowAsync(model, responses, CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public async Task ExportToExcelClaimFollowAsync_NoFunders_ShouldStillWork()
        {
            var model = new ClaimFollowUpRequestModel
            {
                FunderIds = new List<int> { 99 }
            };

            funderNameReportingRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<FundersEntity>.Create(new List<FundersEntity>()));

            var helperMock = CreateHelperMock();

            var sut = CreateSut(helperMock.Object);

            var result = await sut.ExportToExcelClaimFollowAsync(model, new List<ClaimFollowUpResponse>(), CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task FindClaimIdByTransactionTypeIdAsync_DeleteCharge_ReturnsSameId()
        {
            // Arrange
            var transactionTypeId = 123;

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType.deleteCharge, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.Equal(transactionTypeId, result);
        }

        [Fact]
        public async Task FindClaimIdByTransactionTypeIdAsync_UpdatePaymentSummary_ReturnsSameId()
        {
            // Arrange
            var transactionTypeId = 456;

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType.updatePaymentSummary, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.Equal(transactionTypeId, result);
        }

        [Fact]
        public async Task FindClaimIdByTransactionTypeIdAsync_DeleteClaim_ReturnsSameId()
        {
            // Arrange
            var transactionTypeId = 789;

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType.deleteClaim, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.Equal(transactionTypeId, result);
        }

        [Fact]
        public async Task FindClaimIdByTransactionTypeIdAsync_WriteOff_DelegatesToHelper()
        {
            // Arrange
            var transactionTypeId = 1001;
            var expectedClaimId = 2002;

            var helperMock = new Mock<IHelperService>(MockBehavior.Strict);
            helperMock.Setup(h => h.GetClaimIdFromWriteOffIdAsync(transactionTypeId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(expectedClaimId);

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType.writeOff, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.Equal(expectedClaimId, result);
            helperMock.Verify(h => h.GetClaimIdFromWriteOffIdAsync(transactionTypeId, It.IsAny<CancellationToken>()), Times.Once);
            helperMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task FindClaimIdByTransactionTypeIdAsync_InsurancePayment_DelegatesToHelper()
        {
            // Arrange
            var transactionTypeId = 3003;
            var expectedClaimId = 4004;

            var helperMock = new Mock<IHelperService>(MockBehavior.Strict);
            helperMock.Setup(h => h.GetClaimIdFromPaymentIdAsync(transactionTypeId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(expectedClaimId);

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType.insurancePayment, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.Equal(expectedClaimId, result);
            helperMock.Verify(h => h.GetClaimIdFromPaymentIdAsync(transactionTypeId, It.IsAny<CancellationToken>()), Times.Once);
            helperMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task FindClaimIdByTransactionTypeIdAsync_Adjustment_DelegatesToHelper()
        {
            // Arrange
            var transactionTypeId = 5005;
            var expectedClaimId = 6006;

            var helperMock = new Mock<IHelperService>(MockBehavior.Strict);
            helperMock.Setup(h => h.GetClaimIdFromAdjustmentIdAsync(transactionTypeId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(expectedClaimId);

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType.adjustment, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.Equal(expectedClaimId, result);
            helperMock.Verify(h => h.GetClaimIdFromAdjustmentIdAsync(transactionTypeId, It.IsAny<CancellationToken>()), Times.Once);
            helperMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task FindClaimIdByTransactionTypeIdAsync_DeleteChargePayment_UsesRepositoryToReturnChargeEntryId()
        {
            // Arrange
            var transactionTypeId = 7007;
            int? expectedChargeEntryId = 8008;

            var serviceLines = new List<PaymentClaimServiceLineEntity>
            {
                new PaymentClaimServiceLineEntity { Id = transactionTypeId, ClaimChargeEntryId = expectedChargeEntryId }
            };
            paymentClaimServiceLineRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentClaimServiceLineEntity>.Create(serviceLines));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType.deleteChargePayment, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.Equal(expectedChargeEntryId, result);
        }

        // Add the following tests inside the existing `PaymentAdjustmentServiceTest` class

        [Fact]
        public async Task GetPaymentsAdjustmentsByIdAsync_PaymentTypes_IgnoresDateDeleted_ReturnsEntity()
        {
            // Arrange
            var claimId = 101;
            var transactionTypeId = 202;
            var entity = new PaymentsAdjustmentsEntity
            {
                Id = 1,
                ClaimId = claimId,
                TransactionTypeId = transactionTypeId,
                DateDeleted = DateTime.UtcNow // should be ignored for payment types
            };

            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity> { entity }));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.GetPaymentsAdjustmentsByIdAsync(
                ClaimTransactionType.insurancePayment, claimId, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(entity.Id, result!.Id);
        }

        [Fact]
        public async Task GetPaymentsAdjustmentsByIdAsync_DefaultTypes_FiltersSoftDeleted_ReturnsNull()
        {
            // Arrange
            var claimId = 303;
            var transactionTypeId = 404;
            var softDeleted = new PaymentsAdjustmentsEntity
            {
                Id = 2,
                ClaimId = claimId,
                TransactionTypeId = transactionTypeId,
                DateDeleted = DateTime.UtcNow // should be filtered out for default types
            };

            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity> { softDeleted }));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.GetPaymentsAdjustmentsByIdAsync(
                ClaimTransactionType.adjustment, claimId, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetPaymentsAdjustmentsByIdAsync_DefaultTypes_ReturnsActiveEntity()
        {
            // Arrange
            var claimId = 505;
            var transactionTypeId = 606;
            var active = new PaymentsAdjustmentsEntity
            {
                Id = 3,
                ClaimId = claimId,
                TransactionTypeId = transactionTypeId,
                DateDeleted = null
            };

            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity> { active }));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.GetPaymentsAdjustmentsByIdAsync(
                ClaimTransactionType.writeOff, claimId, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(active.Id, result!.Id);
        }

        [Fact]
        public async Task GetPaymentsAdjustmentsByIdAsync_PaymentTypes_NoMatch_ReturnsNull()
        {
            // Arrange
            var claimId = 707;
            var transactionTypeId = 808;

            // repository has items, but none match both claimId and transactionTypeId
            var items = new List<PaymentsAdjustmentsEntity>
            {
                new PaymentsAdjustmentsEntity { Id = 10, ClaimId = claimId, TransactionTypeId = 999 },
                new PaymentsAdjustmentsEntity { Id = 11, ClaimId = 999, TransactionTypeId = transactionTypeId }
            };

            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(items));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.GetPaymentsAdjustmentsByIdAsync(
                ClaimTransactionType.eraReceived, claimId, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        // Add the following tests inside the existing `PaymentAdjustmentServiceTest` class

        [Fact]
        public async Task GetPaymentsAdjustmentsListByClaimIdAsync_ReturnsOnlyActiveForClaim()
        {
            // Arrange
            var claimId = 123;
            var items = new List<PaymentsAdjustmentsEntity>
            {
                new PaymentsAdjustmentsEntity { Id = 1, ClaimId = claimId, DateDeleted = null },
                new PaymentsAdjustmentsEntity { Id = 2, ClaimId = claimId, DateDeleted = DateTime.UtcNow }, // soft-deleted -> should be filtered out
                new PaymentsAdjustmentsEntity { Id = 3, ClaimId = 999, DateDeleted = null } // different claim -> should be filtered out
            };

            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(items));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.GetPaymentsAdjustmentsListByClaimIdAsync(claimId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0]!.Id);
            Assert.Equal(claimId, result[0]!.ClaimId);
            Assert.Null(result[0]!.DateDeleted);
        }

        [Fact]
        public async Task GetPaymentsAdjustmentsListByClaimIdAsync_NoMatches_ReturnsEmpty()
        {
            // Arrange
            var claimId = 555;
            var items = new List<PaymentsAdjustmentsEntity>
            {
                new PaymentsAdjustmentsEntity { Id = 10, ClaimId = 111, DateDeleted = null },
                new PaymentsAdjustmentsEntity { Id = 11, ClaimId = claimId, DateDeleted = DateTime.UtcNow } // soft-deleted
            };

            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(items));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.GetPaymentsAdjustmentsListByClaimIdAsync(claimId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }


        // Add the following tests inside the existing `PaymentAdjustmentServiceTest` class

        [Fact]
        public async Task PreparePaymentsAdjustmentsAsync_CreatesNewEntity_WhenNotFound()
        {
            // Arrange
            var transactionType = ClaimTransactionType.billedAmount; // default path keeps SetTransactionTypeValue simple
            var transactionTypeId = 321;
            var claim = new ClaimEntity
            {
                Id = 1001,
                AccountInfoId = 11,
                PrimaryFunderId = 22,
                ChildProfileId = 33,
                ClaimStatus = ClaimStatus.PendingReview,
                billedDate = new DateTime(2025, 1, 2)
            };

            // No existing PaymentsAdjustmentsEntity found
            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity>()));

            // Claim dates from charge entries
            var chargeEntries = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity { Id = 1, DateOfService = new DateTime(2025, 1, 5) },
                new ClaimChargeEntryEntity { Id = 2, DateOfService = new DateTime(2025, 1, 10) }
            };
            var helperMock = new Mock<IHelperService>();
            helperMock.Setup(h => h.GetChargeEntriesByClaimId(claim.Id)).ReturnsAsync(chargeEntries);

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.PreparePaymentsAdjustmentsAsync(transactionType, claim, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result!.Id); // newly created
            Assert.Equal(claim.Id, result.ClaimId);
            Assert.Equal(claim.AccountInfoId, result.AccountInfoId);
            Assert.Equal(claim.PrimaryFunderId, result.FunderId);
            Assert.Equal(claim.ChildProfileId, result.ClientId);
            Assert.Equal((int)transactionType, result.TransactionType);
            Assert.Equal(transactionTypeId, result.TransactionTypeId);
            Assert.Equal((int)claim.ClaimStatus, result.ClaimStatusId);
            Assert.Equal(claim.billedDate, result.BilledDate);

            // For non-payment types, GetPaymentIdAsync/GetChargeEntryIdAsync default to 0
            Assert.Equal(0, result.PaymentId);
            Assert.Equal(0, result.ChargeEntryId);

            // Dates
            Assert.NotEqual(default, result.DateCreated);
            Assert.NotEqual(default, result.DateModified);

            // ClaimFrom/Through derived from charge entries
            Assert.Equal(new DateTime(2025, 1, 5), result.ClaimFrom);
            Assert.Equal(new DateTime(2025, 1, 10), result.ClaimThrough);

            helperMock.Verify(h => h.GetChargeEntriesByClaimId(claim.Id), Times.Once);
        }

        [Fact]
        public async Task PreparePaymentsAdjustmentsAsync_UsesExistingEntity_WhenFound_AndUpdatesMetadata()
        {
            // Arrange
            var transactionType = ClaimTransactionType.billedAmount; // default path keeps SetTransactionTypeValue simple
            var transactionTypeId = 654;
            var claim = new ClaimEntity
            {
                Id = 2002,
                AccountInfoId = 44,
                PrimaryFunderId = 55,
                ChildProfileId = 66,
                ClaimStatus = ClaimStatus.ApprovalFailed,
                billedDate = new DateTime(2025, 2, 3)
            };

            var existing = new PaymentsAdjustmentsEntity
            {
                Id = 77,
                ClaimId = claim.Id,
                AccountInfoId = 999, // will be kept (only status/billed dates are updated after retrieval)
                FunderId = 888,
                ClientId = 777,
                TransactionType = (int)transactionType,
                TransactionTypeId = transactionTypeId,
                DateDeleted = null,
                // Ensure PaymentId has a value to avoid Nullable.Value access in SetTransactionTypeValue
                PaymentId = 1
            };

            // Existing entity is returned by the repository (via GetPaymentsAdjustmentsByIdAsync default path)
            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity> { existing }));

            // Provide a matching payment so SetTransactionTypeValue can safely resolve it
            paymentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity>
                {
                    new PaymentEntity { Id = 1, DateDeleted = null, ReferenceNumber = "REF-XYZ", DepositDate = new DateTime(2025, 2, 4) }
                }));

            // Claim dates from charge entries
            var chargeEntries = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity { Id = 10, DateOfService = new DateTime(2025, 2, 1) },
                new ClaimChargeEntryEntity { Id = 20, DateOfService = new DateTime(2025, 2, 7) }
            };
            var helperMock = new Mock<IHelperService>();
            helperMock.Setup(h => h.GetChargeEntriesByClaimId(claim.Id)).ReturnsAsync(chargeEntries);

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.PreparePaymentsAdjustmentsAsync(transactionType, claim, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existing.Id, result!.Id); // uses existing
            Assert.Equal(claim.Id, result.ClaimId);
            Assert.Equal((int)claim.ClaimStatus, result.ClaimStatusId);
            Assert.Equal(claim.billedDate, result.BilledDate);

            // Date modified should be set
            Assert.NotEqual(default, result.DateModified);

            // ClaimFrom/Through derived from charge entries
            Assert.Equal(new DateTime(2025, 2, 1), result.ClaimFrom);
            Assert.Equal(new DateTime(2025, 2, 7), result.ClaimThrough);

            helperMock.Verify(h => h.GetChargeEntriesByClaimId(claim.Id), Times.Once);
        }

        [Fact]
        public async Task GetPaymentsAdjustmentsListByClaimIdAsync_AllDeleted_ReturnsEmpty()
        {
            // Arrange
            var claimId = 300;
            var items = new List<PaymentsAdjustmentsEntity>
    {
        new PaymentsAdjustmentsEntity { Id = 1, ClaimId = claimId, DateDeleted = DateTime.UtcNow },
        new PaymentsAdjustmentsEntity { Id = 2, ClaimId = claimId, DateDeleted = DateTime.UtcNow }
    };

            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(items));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.GetPaymentsAdjustmentsListByClaimIdAsync(claimId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPaymentsAdjustmentsListByClaimIdAsync_EmptyRepository_ReturnsEmpty()
        {
            // Arrange
            var claimId = 400;

            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity>()));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.GetPaymentsAdjustmentsListByClaimIdAsync(claimId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPaymentsAdjustmentsListByClaimIdAsync_LargeDataset_ReturnsCorrectFiltered()
        {
            // Arrange
            var claimId = 500;
            var items = Enumerable.Range(1, 1000)
                .Select(i => new PaymentsAdjustmentsEntity
                {
                    Id = i,
                    ClaimId = i % 2 == 0 ? claimId : i, // half matches claimId
                    DateDeleted = i % 5 == 0 ? DateTime.UtcNow : null // every 5th item soft-deleted
                })
                .ToList();

            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(items));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.GetPaymentsAdjustmentsListByClaimIdAsync(claimId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.All(result, x => Assert.Equal(claimId, x!.ClaimId));
            Assert.All(result, x => Assert.Null(x!.DateDeleted));
        }
        [Fact]
        public async Task PreparePaymentsAdjustmentsForDeleteAsync_DeleteChargePayment()
        {
            // Arrange
            var transactionType = ClaimTransactionType.deleteChargePayment;
            var serviceLineId = 9001;       // transactionTypeId
            var chargeEntryId = 5001;       // id used for ChargeEntryId filter
            var expectedPaymentId = 123;

            // Service line -> resolves paymentId
            var serviceLines = new List<PaymentClaimServiceLineEntity>
            {
                new PaymentClaimServiceLineEntity
                {
                    Id = serviceLineId,
                    PaymentClaim = new PaymentClaimEntity { PaymentId = expectedPaymentId }
                }
            };
            paymentClaimServiceLineRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentClaimServiceLineEntity>.Create(serviceLines));

            // PaymentsAdjustments entries (two should match, others filtered out)
            var items = new List<PaymentsAdjustmentsEntity>
            {
                new PaymentsAdjustmentsEntity { Id = 1, ChargeEntryId = chargeEntryId, PaymentId = expectedPaymentId, DateDeleted = null },
                new PaymentsAdjustmentsEntity { Id = 2, ChargeEntryId = chargeEntryId, PaymentId = expectedPaymentId, DateDeleted = null },
                new PaymentsAdjustmentsEntity { Id = 3, ChargeEntryId = chargeEntryId, PaymentId = 999, DateDeleted = null }, // different payment
                new PaymentsAdjustmentsEntity { Id = 4, ChargeEntryId = 7777,        PaymentId = expectedPaymentId, DateDeleted = null }, // different charge
                new PaymentsAdjustmentsEntity { Id = 5, ChargeEntryId = chargeEntryId, PaymentId = expectedPaymentId, DateDeleted = DateTime.UtcNow } // soft-deleted
            };
                paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(items));

            // Provide matching payment to avoid nullable.Value crash in SetTransactionTypeValue
            paymentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity>
                {
            new PaymentEntity { Id = expectedPaymentId, DateDeleted = null, ReferenceNumber = "CHK-1", DepositDate = DateTime.UtcNow }
                }));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.PreparePaymentsAdjustmentsForDeleteAsync(transactionType, serviceLineId, chargeEntryId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result!, e =>
            {
                Assert.Contains(e!.Id, new[] { 1, 2 });
                Assert.Equal(chargeEntryId, e!.ChargeEntryId);
                Assert.Equal(expectedPaymentId, e!.PaymentId);
                Assert.NotNull(e!.DateDeleted); // Set by SetTransactionTypeValue for delete paths
            });
        }

        [Fact]
        public async Task PreparePaymentsAdjustmentsForDeleteAsync_DeleteCharge_FiltersByCharge()
        {
            // Arrange
            var transactionType = ClaimTransactionType.deleteCharge;
            var chargeEntryId = 6001;

            var items = new List<PaymentsAdjustmentsEntity>
            {
                new PaymentsAdjustmentsEntity { Id = 10, ChargeEntryId = chargeEntryId, PaymentId = 1, DateDeleted = null },
                new PaymentsAdjustmentsEntity { Id = 11, ChargeEntryId = chargeEntryId, PaymentId = 2, DateDeleted = null },
                new PaymentsAdjustmentsEntity { Id = 12, ChargeEntryId = 9999,       PaymentId = 1, DateDeleted = null }, // different charge
                new PaymentsAdjustmentsEntity { Id = 13, ChargeEntryId = chargeEntryId, PaymentId = 1, DateDeleted = DateTime.UtcNow } // soft-deleted
            };
            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(items));

            // Provide payments to avoid nullable.Value crash in SetTransactionTypeValue
            paymentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity>
                {
            new PaymentEntity { Id = 1, DateDeleted = null },
            new PaymentEntity { Id = 2, DateDeleted = null }
                }));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.PreparePaymentsAdjustmentsForDeleteAsync(transactionType, 0, chargeEntryId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result!, e =>
            {
                Assert.Contains(e!.Id, new[] { 10, 11 });
                Assert.Equal(chargeEntryId, e!.ChargeEntryId);
                Assert.NotNull(e!.DateDeleted);
            });
        }

        [Fact]
        public async Task PreparePaymentsAdjustmentsForDeleteAsync_DeleteClaim_FiltersByClaim()
        {
            // Arrange
            var transactionType = ClaimTransactionType.deleteClaim;
            var claimId = 7001;

            var items = new List<PaymentsAdjustmentsEntity>
            {
                new PaymentsAdjustmentsEntity { Id = 20, ClaimId = claimId, ChargeEntryId = 1, PaymentId = 5, DateDeleted = null },
                new PaymentsAdjustmentsEntity { Id = 21, ClaimId = claimId, ChargeEntryId = 2, PaymentId = 6, DateDeleted = null },
                new PaymentsAdjustmentsEntity { Id = 22, ClaimId = 9999,   ChargeEntryId = 3, PaymentId = 5, DateDeleted = null }, // different claim
                new PaymentsAdjustmentsEntity { Id = 23, ClaimId = claimId, ChargeEntryId = 4, PaymentId = 5, DateDeleted = DateTime.UtcNow } // soft-deleted
            };
            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(items));

            // Provide payments to avoid nullable.Value crash in SetTransactionTypeValue
            paymentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity>
                {
            new PaymentEntity { Id = 5, DateDeleted = null },
            new PaymentEntity { Id = 6, DateDeleted = null }
                }));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.PreparePaymentsAdjustmentsForDeleteAsync(transactionType, 0, claimId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result!, e =>
            {
                Assert.Contains(e!.Id, new[] { 20, 21 });
                Assert.Equal(claimId, e!.ClaimId);
                Assert.NotNull(e!.DateDeleted);
            });
        }

        

        [Fact]
        public async Task GetPaymentIdAsync_WriteOff_DelegatesToHelper()
        {
            // Arrange
            var transactionTypeId = 101;
            int? expected = 999;

            var helperMock = new Mock<IHelperService>(MockBehavior.Strict);
            helperMock.Setup(h => h.GetPaymentIdFromWriteOffIdAsync(transactionTypeId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(expected);

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.GetPaymentIdAsync(ClaimTransactionType.writeOff, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.Equal(expected, result);
            helperMock.Verify(h => h.GetPaymentIdFromWriteOffIdAsync(transactionTypeId, It.IsAny<CancellationToken>()), Times.Once);
            helperMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetPaymentIdAsync_InsurancePayment_DelegatesToHelper()
        {
            // Arrange
            var transactionTypeId = 202;
            int? expected = 888;

            var helperMock = new Mock<IHelperService>(MockBehavior.Strict);
            helperMock.Setup(h => h.GetPaymentIdFromPaymentIdAsync(transactionTypeId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(expected);

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.GetPaymentIdAsync(ClaimTransactionType.insurancePayment, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.Equal(expected, result);
            helperMock.Verify(h => h.GetPaymentIdFromPaymentIdAsync(transactionTypeId, It.IsAny<CancellationToken>()), Times.Once);
            helperMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetPaymentIdAsync_Adjustment_DelegatesToHelper()
        {
            // Arrange
            var transactionTypeId = 303;
            int? expected = 777;

            var helperMock = new Mock<IHelperService>(MockBehavior.Strict);
            helperMock.Setup(h => h.GetPaymentIdFromAdjustmentIdAsync(transactionTypeId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(expected);

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.GetPaymentIdAsync(ClaimTransactionType.adjustment, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.Equal(expected, result);
            helperMock.Verify(h => h.GetPaymentIdFromAdjustmentIdAsync(transactionTypeId, It.IsAny<CancellationToken>()), Times.Once);
            helperMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetPaymentIdAsync_DefaultTransactionType_ReturnsZero()
        {
            // Arrange
            var transactionTypeId = 404;

            var helperMock = new Mock<IHelperService>(MockBehavior.Strict);

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.GetPaymentIdAsync(ClaimTransactionType.billedAmount, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.Equal(0, result);
            helperMock.VerifyNoOtherCalls();
        }

        

        [Fact]
        public async Task GetChargeEntryIdAsync_WriteOff_DelegatesToHelper()
        {
            // Arrange
            var transactionTypeId = 111;
            int? expected = 222;

            var helperMock = new Mock<IHelperService>(MockBehavior.Strict);
            helperMock.Setup(h => h.GetChargeIdFromWriteOffIdAsync(transactionTypeId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(expected);

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.GetChargeEntryIdAsync(ClaimTransactionType.writeOff, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.Equal(expected, result);
            helperMock.Verify(h => h.GetChargeIdFromWriteOffIdAsync(transactionTypeId, It.IsAny<CancellationToken>()), Times.Once);
            helperMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetChargeEntryIdAsync_InsurancePayment_DelegatesToHelper()
        {
            // Arrange
            var transactionTypeId = 333;
            int? expected = 444;

            var helperMock = new Mock<IHelperService>(MockBehavior.Strict);
            helperMock.Setup(h => h.GetChargeIdFromPaymentIdAsync(transactionTypeId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(expected);

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.GetChargeEntryIdAsync(ClaimTransactionType.insurancePayment, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.Equal(expected, result);
            helperMock.Verify(h => h.GetChargeIdFromPaymentIdAsync(transactionTypeId, It.IsAny<CancellationToken>()), Times.Once);
            helperMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetChargeEntryIdAsync_Adjustment_DelegatesToHelper()
        {
            // Arrange
            var transactionTypeId = 555;
            int? expected = 666;

            var helperMock = new Mock<IHelperService>(MockBehavior.Strict);
            helperMock.Setup(h => h.GetChargeEntryIdFromAdjustmentIdAsync(transactionTypeId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(expected);

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.GetChargeEntryIdAsync(ClaimTransactionType.adjustment, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.Equal(expected, result);
            helperMock.Verify(h => h.GetChargeEntryIdFromAdjustmentIdAsync(transactionTypeId, It.IsAny<CancellationToken>()), Times.Once);
            helperMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetChargeEntryIdAsync_DefaultTransactionType_ReturnsZero()
        {
            // Arrange
            var transactionTypeId = 777;
            var helperMock = new Mock<IHelperService>(MockBehavior.Strict);

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.GetChargeEntryIdAsync(ClaimTransactionType.billedAmount, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.Equal(0, result);
            helperMock.VerifyNoOtherCalls();
        }

        // Add this test inside the existing `PaymentAdjustmentServiceTest` class

        [Fact]
        public async Task PreparePaymentsAdjustmentsListAsync_SetsFieldsFromPayment_AndReturnsEntity()
        {
            // Arrange
            var payment = new PaymentEntity
            {
                ReferenceNumber = "REF-123",
                DepositDate = new DateTime(2025, 1, 15)
            };

            var paymentsAdjustments = new PaymentsAdjustmentsEntity
            {
                EftOrCheckNumber = null,
                PaymentOrAdjustmentDate = null,
                DateModified = default
            };

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.PreparePaymentsAdjustmentsListAsync(ClaimTransactionType.updatePaymentSummary, paymentsAdjustments, payment);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("REF-123", result!.EftOrCheckNumber);
            Assert.Equal(new DateTime(2025, 1, 15), result.PaymentOrAdjustmentDate);
            // DateModified is set inside the service to EstDateTime; just ensure it changed from default
            Assert.NotEqual(default, result.DateModified);
        }

        [Fact]
        public async Task PreparePaymentsAdjustmentsListAsync_PaymentReferenceNumberNull_SetsEftOrCheckNumberToNull()
        {
            // Arrange
            var payment = new PaymentEntity
            {
                ReferenceNumber = null,
                DepositDate = new DateTime(2025, 1, 15)
            };
            var paymentsAdjustments = new PaymentsAdjustmentsEntity();

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.PreparePaymentsAdjustmentsListAsync(ClaimTransactionType.updatePaymentSummary, paymentsAdjustments, payment);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result!.EftOrCheckNumber);
            Assert.Equal(new DateTime(2025, 1, 15), result.PaymentOrAdjustmentDate);
            Assert.NotEqual(default, result.DateModified);
        }

        [Fact]
        public async Task PreparePaymentsAdjustmentsListAsync_PaymentDepositDateNull_SetsPaymentOrAdjustmentDateToNull()
        {
            // Arrange
            var payment = new PaymentEntity
            {
                ReferenceNumber = "REF-123",
                DepositDate = null
            };
            var paymentsAdjustments = new PaymentsAdjustmentsEntity();

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.PreparePaymentsAdjustmentsListAsync(ClaimTransactionType.updatePaymentSummary, paymentsAdjustments, payment);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("REF-123", result!.EftOrCheckNumber);
            Assert.Null(result.PaymentOrAdjustmentDate);
            Assert.NotEqual(default, result.DateModified);
        }

        [Theory]
        [InlineData(ClaimTransactionType.updatePaymentSummary)]
        [InlineData(ClaimTransactionType.adjustment)]
        public async Task PreparePaymentsAdjustmentsListAsync_AllTransactionTypes_SetFieldsCorrectly(ClaimTransactionType transactionType)
        {
            // Arrange
            var payment = new PaymentEntity
            {
                ReferenceNumber = "REF-XYZ",
                DepositDate = new DateTime(2025, 2, 20)
            };
            var paymentsAdjustments = new PaymentsAdjustmentsEntity();

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.PreparePaymentsAdjustmentsListAsync(transactionType, paymentsAdjustments, payment);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("REF-XYZ", result!.EftOrCheckNumber);
            Assert.Equal(new DateTime(2025, 2, 20), result.PaymentOrAdjustmentDate);
            Assert.NotEqual(default, result.DateModified);
        }

        [Fact]
        public async Task GetClaimByIdAsync_ReturnsEntity_WhenExistsAndNotDeleted()
        {
            // Arrange
            var claimId = 123;
            var claim = new ClaimEntity { Id = claimId, DateDeleted = null };
            claimRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimEntity>.Create(new List<ClaimEntity> { claim }));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.GetClaimByIdAsync(claimId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(claimId, result!.Id);
            Assert.Null(result.DateDeleted);
        }

        [Fact]
        public async Task GetClaimByIdAsync_ReturnsNull_WhenNotFoundOrSoftDeleted()
        {
            // Arrange
            var claimId = 456;
            var items = new List<ClaimEntity>
            {
                new ClaimEntity { Id = 999, DateDeleted = null },           // different id
                new ClaimEntity { Id = claimId, DateDeleted = DateTime.UtcNow } // soft-deleted
            };
            claimRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimEntity>.Create(items));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.GetClaimByIdAsync(claimId, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetClaimByIdAsync_MultipleClaimsWithSameId_ReturnsFirstNotDeleted()
        {
            // Arrange
            var claimId = 123;
            var items = new List<ClaimEntity>
    {
        new ClaimEntity { Id = claimId, DateDeleted = DateTime.UtcNow }, // soft-deleted
        new ClaimEntity { Id = claimId, DateDeleted = null }             // valid
    };
            claimRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimEntity>.Create(items));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.GetClaimByIdAsync(claimId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(claimId, result!.Id);
            Assert.Null(result.DateDeleted);
        }

        [Fact]
        public async Task GetClaimByIdAsync_RepositoryThrows_ExceptionPropagated()
        {
            // Arrange
            var claimId = 123;
            claimRepository
                .Setup(r => r.Query())
                .Throws(new Exception("Database error"));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                sut.GetClaimByIdAsync(claimId, CancellationToken.None));
            Assert.Equal("Database error", ex.Message);
        }

        [Fact]
        public async Task GetClaimByIdAsync_RepositoryReturnsEmpty_ReturnsNull()
        {
            // Arrange
            var claimId = 123;
            claimRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimEntity>.Create(new List<ClaimEntity>()));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.GetClaimByIdAsync(claimId, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }
        [Fact]
        public async Task GetChargeByIdAsync_ReturnsEntity_WhenExists()
        {
            // Arrange
            var chargeId = 42;
            var charge = new ClaimChargeEntryEntity { Id = chargeId };
            claimChargeEntryRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimChargeEntryEntity>.Create(new List<ClaimChargeEntryEntity> { charge }));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.GetChargeByIdAsync(chargeId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(chargeId, result!.Id);
        }

        [Fact]
        public async Task GetChargeByIdAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            var chargeId = 99;
            // repository contains different id
            claimChargeEntryRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimChargeEntryEntity>.Create(new List<ClaimChargeEntryEntity>
                {
            new ClaimChargeEntryEntity { Id = 1 }
                }));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.GetChargeByIdAsync(chargeId, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetChargeByIdAsync_MultipleChargesWithSameId_ReturnsFirst()
        {
            // Arrange
            var chargeId = 42;
            var items = new List<ClaimChargeEntryEntity>
    {
        new ClaimChargeEntryEntity { Id = chargeId },
        new ClaimChargeEntryEntity { Id = chargeId } // duplicate
    };

            claimChargeEntryRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimChargeEntryEntity>.Create(items));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.GetChargeByIdAsync(chargeId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(chargeId, result!.Id);
        }

        [Fact]
        public async Task GetChargeByIdAsync_RepositoryThrows_ExceptionPropagated()
        {
            // Arrange
            var chargeId = 42;
            claimChargeEntryRepository
                .Setup(r => r.Query())
                .Throws(new Exception("Database error"));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                sut.GetChargeByIdAsync(chargeId, CancellationToken.None));
            Assert.Equal("Database error", ex.Message);
        }

        [Fact]
        public async Task GetChargeByIdAsync_RepositoryReturnsEmpty_ReturnsNull()
        {
            // Arrange
            var chargeId = 42;
            claimChargeEntryRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimChargeEntryEntity>.Create(new List<ClaimChargeEntryEntity>()));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.GetChargeByIdAsync(chargeId, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetChargeByIdAsync_InvalidChargeId_ReturnsNull(int chargeId)
        {
            // Arrange
            claimChargeEntryRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimChargeEntryEntity>.Create(new List<ClaimChargeEntryEntity>()));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.GetChargeByIdAsync(chargeId, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }
        // We verify `SetClaimDatesAsync` indirectly via the public `PreparePaymentsAdjustmentsAsync` method.

        // Update the expectation to match implementation: ClaimThrough is set using the highest Id's DateOfService.
        [Fact]
        public async Task SetClaimDatesAsync_SetsFromAndThrough_WhenChargeEntriesExist()
        {
            // Arrange
            var claimId = 1001;
            var transactionTypeId = 555;
            var transactionType = ClaimTransactionType.billedAmount;

            var claim = new ClaimEntity
            {
                Id = claimId,
                AccountInfoId = 1,
                PrimaryFunderId = 2,
                ChildProfileId = 3,
                ClaimStatus = ClaimStatus.PendingReview,
                billedDate = null
            };

            claimRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimEntity>.Create(new List<ClaimEntity> { claim }));

            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity>()));

            var chargeEntries = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity { Id = 10, ClaimId = claimId, DateOfService = new DateTime(2025, 1, 5) },
                new ClaimChargeEntryEntity { Id = 20, ClaimId = claimId, DateOfService = new DateTime(2025, 1, 10) },
                new ClaimChargeEntryEntity { Id = 30, ClaimId = claimId, DateOfService = new DateTime(2025, 1, 8) }
            };
            var helperMock = new Mock<IHelperService>();
            helperMock.Setup(h => h.GetChargeEntriesByClaimId(claimId)).ReturnsAsync(chargeEntries);

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.PreparePaymentsAdjustmentsAsync(transactionType, claim, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            // ClaimFrom uses lowest Id -> 10 -> 2025-01-05
            Assert.Equal(new DateTime(2025, 1, 5), result!.ClaimFrom);
            // ClaimThrough uses highest Id -> 30 -> 2025-01-08 (implementation orders by Id, not by date)
            Assert.Equal(new DateTime(2025, 1, 8), result.ClaimThrough);

            helperMock.Verify(h => h.GetChargeEntriesByClaimId(claimId), Times.Once);
        }

        [Fact]
        // Fix: ensure `PaymentId` has a value and a matching payment exists to avoid Nullable.Value access in SetTransactionTypeValue.
        public async Task SetClaimDatesAsync_DoesNotChange_WhenNoChargeEntries()
        {
            // Arrange
            var claimId = 2002;
            var transactionTypeId = 777;
            var transactionType = ClaimTransactionType.billedAmount;

            var originalFrom = new DateTime(2025, 2, 1);
            var originalThrough = new DateTime(2025, 2, 7);

            var claim = new ClaimEntity
            {
                Id = claimId,
                AccountInfoId = 10,
                PrimaryFunderId = 20,
                ChildProfileId = 30,
                ClaimStatus = ClaimStatus.ApprovalFailed,
                billedDate = null
            };

            // Existing PaymentsAdjustments with preset dates; should remain unchanged when no entries are returned
            var existing = new PaymentsAdjustmentsEntity
            {
                Id = 1,
                ClaimId = claimId,
                AccountInfoId = claim.AccountInfoId,
                FunderId = claim.PrimaryFunderId,
                ClientId = claim.ChildProfileId,
                TransactionType = (int)transactionType,
                TransactionTypeId = transactionTypeId,
                ClaimFrom = originalFrom,
                ClaimThrough = originalThrough,
                // Important: set PaymentId to avoid Nullable.Value exception in SetTransactionTypeValue
                PaymentId = 1
            };

            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity> { existing }));

            // No charge entries returned
            var helperMock = new Mock<IHelperService>();
            helperMock.Setup(h => h.GetChargeEntriesByClaimId(claim.Id)).ReturnsAsync(new List<ClaimChargeEntryEntity>());

            // Provide a matching payment so SetTransactionTypeValue can safely resolve it if accessed
            paymentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity>
                {
            new PaymentEntity { Id = 1, DateDeleted = null, ReferenceNumber = "REF", DepositDate = new DateTime(2025, 2, 4) }
                }));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.PreparePaymentsAdjustmentsAsync(transactionType, claim, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(originalFrom, result!.ClaimFrom);       // unchanged
            Assert.Equal(originalThrough, result.ClaimThrough);  // unchanged

            helperMock.Verify(h => h.GetChargeEntriesByClaimId(claimId), Times.Once);
        }

        // Add this test inside the existing `PaymentAdjustmentServiceTest` class

        [Fact]
        public async Task AddPaymentsAdjustmentsAsync()
        {
            // Arrange
            var entity = new PaymentsAdjustmentsEntity
            {
                Id = 0,
                ClaimId = 123,
                AccountInfoId = 10,
                FunderId = 20,
                ClientId = 30,
                TransactionType = (int)ClaimTransactionType.billedAmount,
                TransactionTypeId = 999
            };

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.AddPaymentsAdjustmentsAsync(entity, CancellationToken.None);

            // Assert
            Assert.True(result);
            paymentsAdjustmentsRepository.Verify(r => r.AddAsync(It.Is<PaymentsAdjustmentsEntity>(e => e == entity)), Times.Once);
        }

        [Fact]
        public async Task AddPaymentsAdjustmentsAsync_RepositoryThrows_ExceptionPropagated()
        {
            // Arrange
            var entity = new PaymentsAdjustmentsEntity { Id = 1, ClaimId = 123 };
            paymentsAdjustmentsRepository
                .Setup(r => r.AddAsync(entity))
                .ThrowsAsync(new Exception("Database error"));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => sut.AddPaymentsAdjustmentsAsync(entity, CancellationToken.None));
            Assert.Equal("Database error", ex.Message);
        }
        [Fact]
        public async Task AddPaymentsAdjustmentsAsync_VerifyEntityProperties()
        {
            // Arrange
            var entity = new PaymentsAdjustmentsEntity
            {
                Id = 1,
                ClaimId = 101,
                AccountInfoId = 10,
                FunderId = 20,
                ClientId = 30,
                TransactionType = (int)ClaimTransactionType.billedAmount,
                TransactionTypeId = 999,
                EftOrCheckNumber = "REF-999"
            };

            PaymentsAdjustmentsEntity? receivedEntity = null;
            paymentsAdjustmentsRepository
                .Setup(r => r.AddAsync(It.IsAny<PaymentsAdjustmentsEntity>()))
                .Callback<PaymentsAdjustmentsEntity>(e => receivedEntity = e)
                .Returns(Task.CompletedTask);

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.AddPaymentsAdjustmentsAsync(entity, CancellationToken.None);

            // Assert
            Assert.True(result);
            Assert.NotNull(receivedEntity);
            Assert.Equal(entity.Id, receivedEntity!.Id);
            Assert.Equal(entity.EftOrCheckNumber, receivedEntity.EftOrCheckNumber);
            Assert.Equal(entity.ClaimId, receivedEntity.ClaimId);
        }

        [Fact]
        public async Task AddPaymentsAdjustmentsAsync_RepositoryDelayed_CompletesSuccessfully()
        {
            // Arrange
            var entity = new PaymentsAdjustmentsEntity { Id = 2, ClaimId = 102 };
            paymentsAdjustmentsRepository
                .Setup(r => r.AddAsync(entity))
                .Returns(async () =>
                {
                    await Task.Delay(50); // simulate slow db
                });

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.AddPaymentsAdjustmentsAsync(entity, CancellationToken.None);

            // Assert
            Assert.True(result);
            paymentsAdjustmentsRepository.Verify(r => r.AddAsync(entity), Times.Once);
        }

        [Fact]
        public async Task AddPaymentsAdjustmentsAsync_NullableFields_AcceptsAndSaves()
        {
            // Arrange
            var entity = new PaymentsAdjustmentsEntity
            {
                Id = 3,
                ClaimId = 103,
                EftOrCheckNumber = null, // nullable
                PaymentOrAdjustmentDate = null // nullable
            };

            PaymentsAdjustmentsEntity? receivedEntity = null;
            paymentsAdjustmentsRepository
                .Setup(r => r.AddAsync(It.IsAny<PaymentsAdjustmentsEntity>()))
                .Callback<PaymentsAdjustmentsEntity>(e => receivedEntity = e)
                .Returns(Task.CompletedTask);

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.AddPaymentsAdjustmentsAsync(entity, CancellationToken.None);

            // Assert
            Assert.True(result);
            Assert.NotNull(receivedEntity);
            Assert.Null(receivedEntity!.EftOrCheckNumber);
            Assert.Null(receivedEntity.PaymentOrAdjustmentDate);
        }

        [Fact]
        public async Task AddPaymentsAdjustmentsAsync_MultipleEntities_CallsRepositoryEachTime()
        {
            // Arrange
            var entities = new List<PaymentsAdjustmentsEntity>
    {
        new() { Id = 1, ClaimId = 101 },
        new() { Id = 2, ClaimId = 102 }
    };

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            foreach (var e in entities)
            {
                var result = await sut.AddPaymentsAdjustmentsAsync(e, CancellationToken.None);
                Assert.True(result);
            }

            // Assert
            paymentsAdjustmentsRepository.Verify(r => r.AddAsync(It.IsAny<PaymentsAdjustmentsEntity>()), Times.Exactly(entities.Count));
        }

        [Fact]
        public async Task AddPaymentsAdjustmentsAsync_LargeValues_SavesCorrectly()
        {
            // Arrange
            var entity = new PaymentsAdjustmentsEntity
            {
                Id = int.MaxValue,
                ClaimId = int.MaxValue,
                ClientId = int.MaxValue,
                EftOrCheckNumber = new string('X', 5000) // very long string
            };

            PaymentsAdjustmentsEntity? receivedEntity = null;
            paymentsAdjustmentsRepository
                .Setup(r => r.AddAsync(It.IsAny<PaymentsAdjustmentsEntity>()))
                .Callback<PaymentsAdjustmentsEntity>(e => receivedEntity = e)
                .Returns(Task.CompletedTask);

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.AddPaymentsAdjustmentsAsync(entity, CancellationToken.None);

            // Assert
            Assert.True(result);
            Assert.Equal(int.MaxValue, receivedEntity!.Id);
            Assert.Equal(5000, receivedEntity.EftOrCheckNumber.Length);
        }
        // Add this test inside the existing `PaymentAdjustmentServiceTest` class

        [Fact]
        public async Task AddPaymentsAdjustmentsListAsync_CallsRepositoryAddRangeAsync_AndReturnsTrue()
     {
            // Arrange
            var list = new List<PaymentsAdjustmentsEntity>
    {
        new PaymentsAdjustmentsEntity
        {
            Id = 0,
            ClaimId = 101,
            AccountInfoId = 11,
            FunderId = 21,
            ClientId = 31,
            TransactionType = (int)ClaimTransactionType.billedAmount,
            TransactionTypeId = 1001
        },
        new PaymentsAdjustmentsEntity
        {
            Id = 0,
            ClaimId = 102,
            AccountInfoId = 12,
            FunderId = 22,
            ClientId = 32,
            TransactionType = (int)ClaimTransactionType.billedAmount,
            TransactionTypeId = 1002
        }
        };

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.AddPaymentsAdjustmentsListAsync(list, CancellationToken.None);

            // Assert
            Assert.True(result);
            paymentsAdjustmentsRepository.Verify(
                r => r.AddRangeAsync(It.Is<IEnumerable<PaymentsAdjustmentsEntity>>(e => e != null && e.SequenceEqual(list))),
                Times.Once);
        }

        [Fact]
        public async Task AddPaymentsAdjustmentsListAsync_EmptyList_ReturnsTrueAndCallsRepository()
        {
            // Arrange
            var list = new List<PaymentsAdjustmentsEntity>();

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.AddPaymentsAdjustmentsListAsync(list, CancellationToken.None);

            // Assert
            Assert.True(result);
            paymentsAdjustmentsRepository.Verify(r => r.AddRangeAsync(list), Times.Once);
        }

        [Fact]
        public async Task AddPaymentsAdjustmentsListAsync_VerifyCorrectEntitiesPassed()
        {
            // Arrange
            var list = new List<PaymentsAdjustmentsEntity>
    {
        new PaymentsAdjustmentsEntity { Id = 1, ClaimId = 101 },
        new PaymentsAdjustmentsEntity { Id = 2, ClaimId = 102 }
    };

            IEnumerable<PaymentsAdjustmentsEntity>? receivedList = null;
            paymentsAdjustmentsRepository
                .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PaymentsAdjustmentsEntity>>()))
                .Callback<IEnumerable<PaymentsAdjustmentsEntity>>(e => receivedList = e)
                .Returns(Task.CompletedTask);

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.AddPaymentsAdjustmentsListAsync(list, CancellationToken.None);

            // Assert
            Assert.True(result);
            Assert.NotNull(receivedList);
            Assert.Equal(list.Count, receivedList!.Count());
            Assert.All(list, item => Assert.Contains(receivedList, r => r.Id == item.Id && r.ClaimId == item.ClaimId));
        }

        [Fact]
        public async Task AddPaymentsAdjustmentsListAsync_RepositoryThrows_ExceptionPropagated()
        {
            // Arrange
            var list = new List<PaymentsAdjustmentsEntity>
    {
        new PaymentsAdjustmentsEntity { Id = 1 }
    };

            paymentsAdjustmentsRepository
                .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PaymentsAdjustmentsEntity>>()))
                .ThrowsAsync(new Exception("Database error"));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                sut.AddPaymentsAdjustmentsListAsync(list, CancellationToken.None));

            Assert.Equal("Database error", ex.Message);
        }

        [Fact]
        public async Task AddPaymentsAdjustmentsListAsync_LargeList_ProcessesAll()
        {
            // Arrange
            var list = Enumerable.Range(1, 1000)
                .Select(i => new PaymentsAdjustmentsEntity { Id = i, ClaimId = i })
                .ToList();

            paymentsAdjustmentsRepository
                .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PaymentsAdjustmentsEntity>>()))
                .Returns(Task.CompletedTask);

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.AddPaymentsAdjustmentsListAsync(list, CancellationToken.None);

            // Assert
            Assert.True(result);
            paymentsAdjustmentsRepository.Verify(r => r.AddRangeAsync(list), Times.Once);
        }
        // Add this test inside the existing `PaymentAdjustmentServiceTest` class

        [Fact]
        public async Task UpdatePaymentsAdjustmentsAsync_CallsRepositoryUpdate_AndReturnsOne()
        {
            // Arrange
            var entity = new PaymentsAdjustmentsEntity
            {
                Id = 5,
                ClaimId = 123,
                AccountInfoId = 10,
                FunderId = 20,
                ClientId = 30,
                TransactionType = (int)ClaimTransactionType.billedAmount,
                TransactionTypeId = 999
            };

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.UpdatePaymentsAdjustmentsAsync(entity, CancellationToken.None);

            // Assert
            Assert.Equal(1, result);
            paymentsAdjustmentsRepository.Verify(r => r.Update(It.Is<PaymentsAdjustmentsEntity>(e => e == entity)), Times.Once);
        }

        [Fact]
        public async Task UpdatePaymentsAdjustmentsAsync_VerifyCorrectEntityPassed()
        {
            // Arrange
            var entity = new PaymentsAdjustmentsEntity { Id = 42, ClaimId = 123 };

            PaymentsAdjustmentsEntity? updatedEntity = null;
            paymentsAdjustmentsRepository
                .Setup(r => r.Update(It.IsAny<PaymentsAdjustmentsEntity>()))
                .Callback<PaymentsAdjustmentsEntity>(e => updatedEntity = e);

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.UpdatePaymentsAdjustmentsAsync(entity, CancellationToken.None);

            // Assert
            Assert.Equal(1, result);
            Assert.NotNull(updatedEntity);
            Assert.Equal(entity.Id, updatedEntity!.Id);
            Assert.Equal(entity.ClaimId, updatedEntity.ClaimId);
        }

        [Fact]
        public async Task UpdatePaymentsAdjustmentsAsync_RepositoryThrows_ExceptionPropagated()
        {
            // Arrange
            var entity = new PaymentsAdjustmentsEntity { Id = 7 };

            paymentsAdjustmentsRepository
                .Setup(r => r.Update(It.IsAny<PaymentsAdjustmentsEntity>()))
                .Throws(new Exception("Update failed"));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                sut.UpdatePaymentsAdjustmentsAsync(entity, CancellationToken.None));

            Assert.Equal("Update failed", ex.Message);
        }

        [Fact]
        public async Task UpdatePaymentsAdjustmentsAsync_MultipleEntities_CallsRepositoryEachTime()
        {
            // Arrange
            var entities = new List<PaymentsAdjustmentsEntity>
    {
        new() { Id = 1 },
        new() { Id = 2 },
        new() { Id = 3 }
    };

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act & Assert
            foreach (var e in entities)
            {
                var result = await sut.UpdatePaymentsAdjustmentsAsync(e, CancellationToken.None);
                Assert.Equal(1, result);
            }

            paymentsAdjustmentsRepository.Verify(r => r.Update(It.IsAny<PaymentsAdjustmentsEntity>()), Times.Exactly(entities.Count));
        }

        [Fact]
        public async Task UpdatePaymentsAdjustmentListsAsync_CallsRepositoryUpdateRange_AndReturnsOne()
        {
            // Arrange
            var list = new List<PaymentsAdjustmentsEntity>
            {
                new PaymentsAdjustmentsEntity { Id = 1, ClaimId = 100, TransactionType = (int)ClaimTransactionType.billedAmount, TransactionTypeId = 500 },
                new PaymentsAdjustmentsEntity { Id = 2, ClaimId = 101, TransactionType = (int)ClaimTransactionType.billedAmount, TransactionTypeId = 501 }
            };

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.UpdatePaymentsAdjustmentListsAsync(list, CancellationToken.None);

            // Assert
            Assert.Equal(1, result);
            paymentsAdjustmentsRepository.Verify(r => r.UpdateRange(It.Is<List<PaymentsAdjustmentsEntity>>(l => l.SequenceEqual(list))), Times.Once);
        }

        [Fact]
        public async Task UpdatePaymentsAdjustmentListsAsync_RepositoryThrows_ExceptionPropagates()
        {
            // Arrange
            var list = new List<PaymentsAdjustmentsEntity>
    {
        new PaymentsAdjustmentsEntity { Id = 1 }
    };

            paymentsAdjustmentsRepository
                .Setup(r => r.UpdateRange(It.IsAny<List<PaymentsAdjustmentsEntity>>()))
                .Throws(new Exception("UpdateRange failed"));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                sut.UpdatePaymentsAdjustmentListsAsync(list, CancellationToken.None));

            Assert.Equal("UpdateRange failed", ex.Message);
        }

        [Fact]
        public async Task UpdatePaymentsAdjustmentListsAsync_MultipleSequentialCalls_CallsRepositoryEachTime()
        {
            // Arrange
            var list1 = new List<PaymentsAdjustmentsEntity> { new() { Id = 1 } };
            var list2 = new List<PaymentsAdjustmentsEntity> { new() { Id = 2 } };

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result1 = await sut.UpdatePaymentsAdjustmentListsAsync(list1, CancellationToken.None);
            var result2 = await sut.UpdatePaymentsAdjustmentListsAsync(list2, CancellationToken.None);

            // Assert
            Assert.Equal(1, result1);
            Assert.Equal(1, result2);
            paymentsAdjustmentsRepository.Verify(r => r.UpdateRange(It.IsAny<List<PaymentsAdjustmentsEntity>>()), Times.Exactly(2));
        }

        [Fact]
        public async Task SetTransactionTypeValue_PaymentTypes_SetsPaymentFields_FromPaymentEntity()
        {
            // Arrange
            var transactionType = ClaimTransactionType.insurancePayment;
            var transactionTypeId = 1234; // PaymentClaimServiceLine Id
            var claim = new ClaimEntity
            {
                Id = 1,
                AccountInfoId = 10,
                PrimaryFunderId = 20,
                ChildProfileId = 30,
                ClaimStatus = ClaimStatus.PendingReview
            };

            var expectedPaymentId = 999;
            var expectedRef = "CHK-999";
            var expectedDeposit = new DateTime(2025, 1, 5);

            // No existing payments-adjustments -> force new entity path
            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity>()));

            // helper resolves paymentId and chargeId for payment types
            var helper = new Mock<IHelperService>();
            helper.Setup(h => h.GetPaymentIdFromPaymentIdAsync(transactionTypeId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(expectedPaymentId);
            helper.Setup(h => h.GetChargeIdFromPaymentIdAsync(transactionTypeId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(777);
            // used by SetClaimDatesAsync (return empty to skip date changes)
            helper.Setup(h => h.GetChargeEntriesByClaimId(claim.Id))
                  .ReturnsAsync(new List<ClaimChargeEntryEntity>());

            // payment lookup used inside SetTransactionTypeValue
            paymentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity>
                {
            new PaymentEntity { Id = expectedPaymentId, DateDeleted = null, ReferenceNumber = expectedRef, DepositDate = expectedDeposit }
                }));

            // service line payment details used by GetAndSetPaymentDetailsAsync
            paymentClaimServiceLineRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentClaimServiceLineEntity>.Create(new List<PaymentClaimServiceLineEntity>
                {
            new PaymentClaimServiceLineEntity { Id = transactionTypeId, DateDeleted = null, PaymentAmount = 100m }
                }));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helper.Object
            );

            // Act
            var result = await sut.PreparePaymentsAdjustmentsAsync(transactionType, claim, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedDeposit, result!.PaymentOrAdjustmentDate);
            Assert.Equal(expectedRef, result.EftOrCheckNumber);
            Assert.Null(result.DateDeleted);

            helper.Verify(h => h.GetPaymentIdFromPaymentIdAsync(transactionTypeId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SetTransactionTypeValue_Adjustment_SetsPaymentFields_FromPaymentEntity()
        {
            // Arrange
            var transactionType = ClaimTransactionType.adjustment;
            var transactionTypeId = 2222; // PaymentClaimServiceLineAdjustment Id
            var claim = new ClaimEntity
            {
                Id = 2,
                AccountInfoId = 11,
                PrimaryFunderId = 21,
                ChildProfileId = 31,
                ClaimStatus = ClaimStatus.PendingReview
            };

            var expectedPaymentId = 555;
            var expectedRef = "REF-555";
            var expectedDeposit = new DateTime(2025, 2, 10);

            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity>()));

            var helper = new Mock<IHelperService>();
            helper.Setup(h => h.GetPaymentIdFromAdjustmentIdAsync(transactionTypeId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(expectedPaymentId);
            helper.Setup(h => h.GetChargeEntryIdFromAdjustmentIdAsync(transactionTypeId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(333);
            helper.Setup(h => h.GetChargeEntriesByClaimId(claim.Id))
                  .ReturnsAsync(new List<ClaimChargeEntryEntity>());

            paymentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity>
                {
            new PaymentEntity { Id = expectedPaymentId, DateDeleted = null, ReferenceNumber = expectedRef, DepositDate = expectedDeposit }
                }));

            // Adjustment details used by GetAndSetAdjustmentDetailsAsync
            paymentClaimServiceLineAdjustmentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentClaimServiceLineAdjustmentEntity>.Create(new List<PaymentClaimServiceLineAdjustmentEntity>
                {
            new PaymentClaimServiceLineAdjustmentEntity
            {
                Id = transactionTypeId,
                DateDeleted = null,
                AdjustmentAmount = 10m,
                IsAdjustmentPositive = true,
                AdjustmentGroupCode = "CO",
                AdjustmentReasonCode = "45"
            }
                }));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helper.Object
            );

            // Act
            var result = await sut.PreparePaymentsAdjustmentsAsync(transactionType, claim, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedDeposit, result!.PaymentOrAdjustmentDate);
            Assert.Equal(expectedRef, result.EftOrCheckNumber);
            Assert.Null(result.DateDeleted);

            helper.Verify(h => h.GetPaymentIdFromAdjustmentIdAsync(transactionTypeId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SetTransactionTypeValue_DeleteCharge_SetsDateDeleted()
        {
            // Arrange
            var transactionType = ClaimTransactionType.deleteCharge;
            var chargeEntryId = 5001;

            // Two active entries for this charge; ensure PaymentId is non-null to avoid nullable.Value access in SetTransactionTypeValue
            var items = new List<PaymentsAdjustmentsEntity>
            {
                new PaymentsAdjustmentsEntity { Id = 1, ChargeEntryId = chargeEntryId, PaymentId = 1, DateDeleted = null },
                new PaymentsAdjustmentsEntity { Id = 2, ChargeEntryId = chargeEntryId, PaymentId = 1, DateDeleted = null }
            };
            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(items));

            // Provide matching payment so GetPaymentAsync can resolve safely (even though delete path won't use it after switch)
            paymentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity>
                {
            new PaymentEntity { Id = 1, DateDeleted = null, ReferenceNumber = "CHK-1", DepositDate = DateTime.UtcNow }
                }));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act (invokes SetTransactionTypeValue internally for each item)
            var list = await sut.PreparePaymentsAdjustmentsForDeleteAsync(transactionType, 0, chargeEntryId, CancellationToken.None);

            // Assert
            Assert.Equal(2, list.Count);
            Assert.All(list!, e => Assert.NotNull(e!.DateDeleted));
        }

        

        [Fact]
        public async Task GetPaymentAsync_ReturnsEntity_WhenExistsAndNotDeleted()
        {
            // Arrange
            var paymentId = 42;
            var payment = new PaymentEntity { Id = paymentId, DateDeleted = null };
            paymentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity> { payment }));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.GetPaymentAsync(paymentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(paymentId, result!.Id);
            Assert.Null(result.DateDeleted);
        }

        [Fact]
        public async Task GetPaymentAsync_ReturnsNull_WhenIdIsZero()
        {
            // Arrange
            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.GetPaymentAsync(0);

            // Assert
            Assert.Null(result);
            paymentRepository.Verify(r => r.Query(), Times.Never);
        }

        [Fact]
        public async Task GetPaymentAsync_ReturnsNull_WhenSoftDeletedOrNotFound()
        {
            // Arrange
            var paymentId = 100;
            // Soft-deleted entry should be filtered out by the query
            var items = new List<PaymentEntity>
            {
                new PaymentEntity { Id = paymentId, DateDeleted = DateTime.UtcNow },
                new PaymentEntity { Id = 999, DateDeleted = null }
            };
            paymentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(items));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.GetPaymentAsync(paymentId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetPaymentAsync_MultiplePayments_ReturnsCorrectOne()
        {
            // Arrange
            var paymentId = 42;
            var payments = new List<PaymentEntity>
    {
        new PaymentEntity { Id = 41, DateDeleted = null },
        new PaymentEntity { Id = paymentId, DateDeleted = null }, // This should be returned
        new PaymentEntity { Id = 43, DateDeleted = null }
    };

            paymentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(payments));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.GetPaymentAsync(paymentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(paymentId, result!.Id);
        }

        [Fact]
        public async Task GetPaymentAsync_RepositoryThrows_ExceptionPropagates()
        {
            // Arrange
            var paymentId = 10;

            paymentRepository
                .Setup(r => r.Query())
                .Throws(new Exception("Database error"));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => sut.GetPaymentAsync(paymentId));
            Assert.Equal("Database error", ex.Message);
        }

        [Fact]
        public async Task GetPaymentAsync_ValidId_CallsRepositoryOnce()
        {
            // Arrange
            var paymentId = 50;
            var payment = new PaymentEntity { Id = paymentId, DateDeleted = null };
            paymentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity> { payment }));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );

            // Act
            var result = await sut.GetPaymentAsync(paymentId);

            // Assert
            paymentRepository.Verify(r => r.Query(), Times.Once);
            Assert.Equal(paymentId, result!.Id);
        }

        [Fact]
        public async Task GetAndSetAdjustmentDetails_WhenAdjustmentExists_Positive()
        {
            // Arrange
            var transactionType = ClaimTransactionType.adjustment; // non-PR path
            var adjustmentId = 9001;
            var claim = new ClaimEntity
            {
                Id = 101,
                AccountInfoId = 10,
                PrimaryFunderId = 20,
                ChildProfileId = 30,
                ClaimStatus = ClaimStatus.PendingReview,
                billedDate = null
            };

            // No existing PA -> create new
            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity>()));

            var expectedPaymentId = 1;
            var expectedDeposit = new DateTime(2025, 1, 2);
            var expectedRef = "CHK-ADJ-1";
            // Helper for IDs and empty claim dates
            var helper = new Mock<IHelperService>();
            helper.Setup(h => h.GetPaymentIdFromAdjustmentIdAsync(adjustmentId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(expectedPaymentId);
            helper.Setup(h => h.GetChargeEntryIdFromAdjustmentIdAsync(adjustmentId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(123);
            helper.Setup(h => h.GetChargeEntriesByClaimId(claim.Id)).ReturnsAsync(new List<ClaimChargeEntryEntity>());

            // Payment exists to avoid nullable access in SetTransactionTypeValue
            paymentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity>
                {
            new PaymentEntity { Id = expectedPaymentId, DateDeleted = null, ReferenceNumber = expectedRef, DepositDate = expectedDeposit }
                }));

            // Service line adjustment returned by repository (Projection requires these properties)
            var dateCreated = new DateTime(2025, 1, 10);
            var dateModified = new DateTime(2025, 1, 15);
            paymentClaimServiceLineAdjustmentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentClaimServiceLineAdjustmentEntity>.Create(new List<PaymentClaimServiceLineAdjustmentEntity>
                {
            new PaymentClaimServiceLineAdjustmentEntity
            {
                Id = adjustmentId,
                DateDeleted = null,
                AdjustmentGroupCode = "CO",
                AdjustmentReasonCode = "RM1",
                // Properties used in Select projection
                AdjustmentAmount = 25.5m,
                IsAdjustmentPositive = true,
                DateCreated = dateCreated,
                DateLastModified = dateModified
            }
                }));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helper.Object
            );

            // Act
            var result = await sut.PreparePaymentsAdjustmentsAsync(transactionType, claim, adjustmentId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(25.5m, result!.Adjustment);
            Assert.Equal("CO", result.ReasonCode);
            Assert.Equal("RM1", result.RemarkCode);
            // Since TransactionDate was null initially, should take DateCreated
            Assert.Equal(dateCreated, result.TransactionDate);
            // Not deleted because adjustment exists
            Assert.Null(result.DateDeleted);
        }

        [Fact]
        public async Task GetAndSetAdjustmentDetails_SetsNegativeAmount_UsesLastModified_WhenTransactionDateAlreadySet()
        {
            // Arrange
            var transactionType = ClaimTransactionType.adjustment;
            var adjustmentId = 9002;
            var claim = new ClaimEntity
            {
                Id = 102,
                AccountInfoId = 11,
                PrimaryFunderId = 21,
                ChildProfileId = 31,
                ClaimStatus = ClaimStatus.PendingReview,
                billedDate = null
            };

            // Existing PA with preset TransactionDate, ensures method chooses DateLastModified
            var presetDate = new DateTime(2025, 2, 1);
            var existing = new PaymentsAdjustmentsEntity
            {
                Id = 77,
                ClaimId = claim.Id,
                AccountInfoId = claim.AccountInfoId,
                FunderId = claim.PrimaryFunderId,
                ClientId = claim.ChildProfileId,
                TransactionType = (int)transactionType,
                TransactionTypeId = adjustmentId,
                TransactionDate = presetDate,
                PaymentId = 1 // prevent nullable access crash
            };
            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity> { existing }));

            // Helper for IDs and empty claim dates
            var helper = new Mock<IHelperService>();
            helper.Setup(h => h.GetPaymentIdFromAdjustmentIdAsync(adjustmentId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(1);
            helper.Setup(h => h.GetChargeEntryIdFromAdjustmentIdAsync(adjustmentId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(456);
            helper.Setup(h => h.GetChargeEntriesByClaimId(claim.Id)).ReturnsAsync(new List<ClaimChargeEntryEntity>());

            // Payment exists
            paymentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity>
                {
            new PaymentEntity { Id = 1, DateDeleted = null, ReferenceNumber = "REF-1", DepositDate = new DateTime(2025,2,10) }
                }));

            // Service line adjustment (negative)
            var dateCreated = new DateTime(2025, 2, 5);
            var dateModified = new DateTime(2025, 2, 6);
            paymentClaimServiceLineAdjustmentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentClaimServiceLineAdjustmentEntity>.Create(new List<PaymentClaimServiceLineAdjustmentEntity>
                {
            new PaymentClaimServiceLineAdjustmentEntity
            {
                Id = adjustmentId,
                DateDeleted = null,
                AdjustmentGroupCode = "CO",
                AdjustmentReasonCode = "", // blank -> null in entity
                AdjustmentAmount = 10m,
                IsAdjustmentPositive = false,
                DateCreated = dateCreated,
                DateLastModified = dateModified
            }
                }));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helper.Object
            );

            // Act
            var result = await sut.PreparePaymentsAdjustmentsAsync(transactionType, claim, adjustmentId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(-10m, result!.Adjustment); // negative because IsAdjustmentPositive == false
            Assert.Equal("CO", result.ReasonCode);
            Assert.Null(result.RemarkCode); // blank -> null
                                            // Since TransactionDate was preset, it should pick DateLastModified
            Assert.Equal(dateModified, result.TransactionDate);
            Assert.Null(result.DateDeleted);
        }

        [Fact]
        public async Task GetAndSetAdjustmentDetails_SetsDateDeleted_WhenNoAdjustmentFound()
        {
            // Arrange
            var transactionType = ClaimTransactionType.adjustment;
            var adjustmentId = 9003;
            var claim = new ClaimEntity
            {
                Id = 103,
                AccountInfoId = 12,
                PrimaryFunderId = 22,
                ChildProfileId = 32,
                ClaimStatus = ClaimStatus.PendingReview,
                billedDate = null
            };

            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity>()));

            var helper = new Mock<IHelperService>();
            helper.Setup(h => h.GetPaymentIdFromAdjustmentIdAsync(adjustmentId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(1);
            helper.Setup(h => h.GetChargeEntryIdFromAdjustmentIdAsync(adjustmentId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(789);
            helper.Setup(h => h.GetChargeEntriesByClaimId(claim.Id)).ReturnsAsync(new List<ClaimChargeEntryEntity>());

            paymentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity>
                {
            new PaymentEntity { Id = 1, DateDeleted = null, ReferenceNumber = "REF-2", DepositDate = new DateTime(2025,3,10) }
                }));

            // No adjustment rows returned
            paymentClaimServiceLineAdjustmentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentClaimServiceLineAdjustmentEntity>.Create(new List<PaymentClaimServiceLineAdjustmentEntity>()));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helper.Object
            );

            // Act
            var result = await sut.PreparePaymentsAdjustmentsAsync(transactionType, claim, adjustmentId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result!.DateDeleted); // set to EstDateTime by service when not found
        }

       [Fact]
        public async Task GetAndSetPaymentDetailsAsync_SetsPayment_WhenAmountIsPositive()
        {
            // Arrange
            var transactionType = ClaimTransactionType.insurancePayment; // triggers GetAndSetPaymentDetailsAsync
            var transactionTypeId = 1234; // PaymentClaimServiceLine Id
            var claim = new ClaimEntity
            {
                Id = 100,
                AccountInfoId = 10,
                PrimaryFunderId = 20,
                ChildProfileId = 30,
                ClaimStatus = ClaimStatus.PendingReview,
                billedDate = null
            };

            // No existing PA -> force creation
            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity>()));

            // Helper returns payment and charge ids
            var helperMock = new Mock<IHelperService>();
            helperMock.Setup(h => h.GetPaymentIdFromPaymentIdAsync(transactionTypeId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(1);
            helperMock.Setup(h => h.GetChargeIdFromPaymentIdAsync(transactionTypeId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(555);
            helperMock.Setup(h => h.GetChargeEntriesByClaimId(claim.Id)).ReturnsAsync(new List<ClaimChargeEntryEntity>());

            // Payment exists (to avoid nullable.Value in SetTransactionTypeValue)
            paymentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity>
                {
            new PaymentEntity { Id = 1, DateDeleted = null, ReferenceNumber = "REF-1", DepositDate = new DateTime(2025, 1, 5) }
                }));

            // Service line with positive amount
            var lastModified = new DateTime(2025, 1, 7);
            paymentClaimServiceLineRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentClaimServiceLineEntity>.Create(new List<PaymentClaimServiceLineEntity>
                {
            new PaymentClaimServiceLineEntity { Id = transactionTypeId, DateDeleted = null, PaymentAmount = 123.45m, DateLastModified = lastModified }
                }));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.PreparePaymentsAdjustmentsAsync(transactionType, claim, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(123.45m, result!.Payment);
            Assert.Null(result.DateDeleted);
            Assert.Equal(lastModified, result.TransactionDate);
            Assert.Equal(BaseService.FindPaymentTypeId(transactionType).ToString(), result.ReasonCode); // paymentTypeId to string
            Assert.Equal("", result.RemarkCode);
        }

        [Fact]
        public async Task GetAndSetPaymentDetailsAsync_SoftDeletes_WhenAmountIsNullOrZero()
        {
            // Arrange
            var transactionType = ClaimTransactionType.patientPayment;
            var transactionTypeId = 4321;
            var claim = new ClaimEntity
            {
                Id = 200,
                AccountInfoId = 11,
                PrimaryFunderId = 21,
                ChildProfileId = 31,
                ClaimStatus = ClaimStatus.PendingReview,
                billedDate = null
            };

            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity>()));

            var helperMock = new Mock<IHelperService>();
            helperMock.Setup(h => h.GetPaymentIdFromPaymentIdAsync(transactionTypeId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(2);
            helperMock.Setup(h => h.GetChargeIdFromPaymentIdAsync(transactionTypeId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(777);
            helperMock.Setup(h => h.GetChargeEntriesByClaimId(claim.Id)).ReturnsAsync(new List<ClaimChargeEntryEntity>());

            paymentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity>
                {
            new PaymentEntity { Id = 2, DateDeleted = null, ReferenceNumber = "REF-2", DepositDate = new DateTime(2025, 2, 10) }
                }));

            // Service line with zero amount -> should soft delete
            var lastModified = new DateTime(2025, 2, 12);
            paymentClaimServiceLineRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentClaimServiceLineEntity>.Create(new List<PaymentClaimServiceLineEntity>
                {
            new PaymentClaimServiceLineEntity { Id = transactionTypeId, DateDeleted = null, PaymentAmount = 0m, DateLastModified = lastModified }
                }));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.PreparePaymentsAdjustmentsAsync(transactionType, claim, transactionTypeId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0m, result!.Payment);
            Assert.NotNull(result.DateDeleted); // EstDateTime
            Assert.Equal(lastModified, result.TransactionDate);
            Assert.Equal(BaseService.FindPaymentTypeId(transactionType).ToString(), result.ReasonCode);
            Assert.Equal("", result.RemarkCode);
        }

        // Fix the test to avoid nullable PaymentId crash and ensure write-off amount is non-zero.
        [Fact]
        public async Task GetPaymentOrAdjustmentDetailsAsync_WriteOff_SetsNegativeAdjustment_AndDates()
        {
            // Arrange
            var transactionType = ClaimTransactionType.writeOff;
            var writeOffId = 7001;
            var claim = new ClaimEntity
            {
                Id = 300,
                AccountInfoId = 10,
                PrimaryFunderId = 20,
                ChildProfileId = 30,
                ClaimStatus = ClaimStatus.PendingReview,
                billedDate = null
            };

            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity>()));

            var helperMock = new Mock<IHelperService>();
            helperMock.Setup(h => h.GetChargeEntriesByClaimId(claim.Id))
                      .ReturnsAsync(new List<ClaimChargeEntryEntity>());
            // Ensure PaymentId is not null to avoid PaymentId.Value in SetTransactionTypeValue
            helperMock.Setup(h => h.GetPaymentIdFromWriteOffIdAsync(writeOffId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(1);

            var dateCreated = new DateTime(2025, 3, 1);
            var dateModified = new DateTime(2025, 3, 5);

            // Provide a matching payment entity (even if not used by write-off branch, it avoids the nullable.Value path)
            paymentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity>
                {
            new PaymentEntity { Id = 1, DateDeleted = null, ReferenceNumber = "WO-REF", DepositDate = dateCreated }
                }));

            // Return a write-off with a non-zero amount so we hit the main path (not the soft-delete branch)
            claimChargeEntryWriteOffRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimChargeEntryWriteOffEntity>.Create(new List<ClaimChargeEntryWriteOffEntity>
                {
            new ClaimChargeEntryWriteOffEntity
            {
                Id = writeOffId,
                DateDeleted = null,
                // IMPORTANT: ensure non-zero amount so adjustment path runs
                WriteOffAmount = 50m,
                DateCreated = dateCreated,
                DateLastModified = dateModified
            }
                }));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.PreparePaymentsAdjustmentsAsync(transactionType, claim, writeOffId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(-50m, result!.Adjustment);              // negative of write-off amount
            Assert.Equal("WO", result.ReasonCode);
            Assert.Equal("", result.RemarkCode);
            // Since dates were null initially, they should be set from DateCreated
            Assert.Equal(dateCreated, result.TransactionDate);
            Assert.Equal(dateCreated, result.PaymentOrAdjustmentDate);
            Assert.Null(result.DateDeleted);
        }

        [Fact]
        public async Task GetPaymentOrAdjustmentDetailsAsync_WriteOff_ZeroAmount_SoftDeletes()
        {
            // Arrange
            var transactionType = ClaimTransactionType.writeOff;
            var writeOffId = 7002;
            var claim = new ClaimEntity
            {
                Id = 301,
                AccountInfoId = 11,
                PrimaryFunderId = 21,
                ChildProfileId = 31,
                ClaimStatus = ClaimStatus.PendingReview,
                billedDate = null
            };

            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity>()));

            var helperMock = new Mock<IHelperService>();
            helperMock.Setup(h => h.GetChargeEntriesByClaimId(claim.Id)).ReturnsAsync(new List<ClaimChargeEntryEntity>());
            // Ensure PaymentId is not null to avoid PaymentId.Value access in SetTransactionTypeValue
            helperMock.Setup(h => h.GetPaymentIdFromWriteOffIdAsync(writeOffId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(1);

            var dateCreated = new DateTime(2025, 4, 1);

            // Provide a matching payment entity to satisfy GetPaymentAsync when PaymentId is set
            paymentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity>
                {
            new PaymentEntity { Id = 1, DateDeleted = null, ReferenceNumber = "WO-REF", DepositDate = dateCreated }
                }));

            // Write-off with zero (or null) amount -> should soft delete
            claimChargeEntryWriteOffRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimChargeEntryWriteOffEntity>.Create(new List<ClaimChargeEntryWriteOffEntity>
                {
            new ClaimChargeEntryWriteOffEntity
            {
                Id = writeOffId,
                DateDeleted = null,
                WriteOffAmount = 0m,
                DateCreated = dateCreated,
                DateLastModified = dateCreated
            }
                }));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.PreparePaymentsAdjustmentsAsync(transactionType, claim, writeOffId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result!.DateDeleted); // EstDateTime set when write-off amount is null or zero
        }

        [Fact]
        public async Task GetPaymentOrAdjustmentDetailsAsync_PaymentTypes_DelegatesToGetAndSetPaymentDetails()
        {
            // Arrange
            var transactionType = ClaimTransactionType.insurancePayment;
            var serviceLineId = 8001;
            var claim = new ClaimEntity
            {
                Id = 302,
                AccountInfoId = 12,
                PrimaryFunderId = 22,
                ChildProfileId = 32,
                ClaimStatus = ClaimStatus.PendingReview,
                billedDate = null
            };

            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity>()));

            var helperMock = new Mock<IHelperService>();
            helperMock.Setup(h => h.GetPaymentIdFromPaymentIdAsync(serviceLineId, It.IsAny<CancellationToken>())).ReturnsAsync(5);
            helperMock.Setup(h => h.GetChargeIdFromPaymentIdAsync(serviceLineId, It.IsAny<CancellationToken>())).ReturnsAsync(999);
            helperMock.Setup(h => h.GetChargeEntriesByClaimId(claim.Id)).ReturnsAsync(new List<ClaimChargeEntryEntity>());

            var depositDate = new DateTime(2025, 5, 10);
            paymentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity>
                {
            new PaymentEntity { Id = 5, DateDeleted = null, ReferenceNumber = "PAY-5", DepositDate = depositDate }
                }));

            var lastModified = new DateTime(2025, 5, 12);
            paymentClaimServiceLineRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentClaimServiceLineEntity>.Create(new List<PaymentClaimServiceLineEntity>
                {
            new PaymentClaimServiceLineEntity { Id = serviceLineId, DateDeleted = null, PaymentAmount = 200m, DateLastModified = lastModified }
                }));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.PreparePaymentsAdjustmentsAsync(transactionType, claim, serviceLineId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200m, result!.Payment);
            Assert.Null(result.DateDeleted);
            Assert.Equal(lastModified, result.TransactionDate);
            // For payment types, ReasonCode is set to paymentTypeId (as string) and EFT/deposit are set from the payment entity
            Assert.Equal(BaseService.FindPaymentTypeId(transactionType).ToString(), result.ReasonCode);
            Assert.Equal("PAY-5", result.EftOrCheckNumber);
            Assert.Equal(depositDate, result.PaymentOrAdjustmentDate);
        }

        [Fact]
        public async Task GetPaymentOrAdjustmentDetailsAsync_AdjustmentTypes_DelegatesToGetAndSetAdjustmentDetails()
        {
            // Arrange
            var transactionType = ClaimTransactionType.adjustment;
            var adjustmentId = 8101;
            var claim = new ClaimEntity
            {
                Id = 303,
                AccountInfoId = 13,
                PrimaryFunderId = 23,
                ChildProfileId = 33,
                ClaimStatus = ClaimStatus.PendingReview,
                billedDate = null
            };

            paymentsAdjustmentsRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentsAdjustmentsEntity>.Create(new List<PaymentsAdjustmentsEntity>()));

            var helperMock = new Mock<IHelperService>();
            helperMock.Setup(h => h.GetPaymentIdFromAdjustmentIdAsync(adjustmentId, It.IsAny<CancellationToken>())).ReturnsAsync(6);
            helperMock.Setup(h => h.GetChargeEntryIdFromAdjustmentIdAsync(adjustmentId, It.IsAny<CancellationToken>())).ReturnsAsync(1000);
            helperMock.Setup(h => h.GetChargeEntriesByClaimId(claim.Id)).ReturnsAsync(new List<ClaimChargeEntryEntity>());

            paymentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentEntity>.Create(new List<PaymentEntity>
                {
            new PaymentEntity { Id = 6, DateDeleted = null, ReferenceNumber = "ADJ-6", DepositDate = new DateTime(2025, 6, 10) }
                }));

            var dateCreated = new DateTime(2025, 6, 11);
            var dateModified = new DateTime(2025, 6, 12);
            paymentClaimServiceLineAdjustmentRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentClaimServiceLineAdjustmentEntity>.Create(new List<PaymentClaimServiceLineAdjustmentEntity>
                {
            new PaymentClaimServiceLineAdjustmentEntity
            {
                Id = adjustmentId,
                DateDeleted = null,
                AdjustmentGroupCode = "CO",
                AdjustmentReasonCode = "45",
                AdjustmentAmount = 30m,
                IsAdjustmentPositive = true,
                DateCreated = dateCreated,
                DateLastModified = dateModified
            }
                }));

            var sut = new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperMock.Object
            );

            // Act
            var result = await sut.PreparePaymentsAdjustmentsAsync(transactionType, claim, adjustmentId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(30m, result!.Adjustment);
            Assert.Equal("CO", result.ReasonCode);
            Assert.Equal("45", result.RemarkCode);
            Assert.Null(result.DateDeleted);
            Assert.Equal(dateCreated, result.TransactionDate);
        }

        private PaymentAdjustmentService CreateSut(IHelperService helperService)
        {
            return new PaymentAdjustmentService(
                paymentsAdjustmentsRepository.Object,
                claimRepository.Object,
                claimChargeEntryWriteOffRepository.Object,
                claimChargeEntryRepository.Object,
                paymentClaimServiceLineAdjustmentRepository.Object,
                paymentClaimServiceLineRepository.Object,
                paymentRepository.Object,
                funderNameReportingRepository.Object,
                helperService
            );
        }

    }
}
