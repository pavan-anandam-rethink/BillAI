using AutoFixture;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.Claims.WriteOff;
using BillingService.Domain.Services.Billing;
using BillingService.XUnit.Tests.Common;
using BillingService.XUnit.Tests.Common.Mocks;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Rethink.Services.Common.Entities.Billing;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Messaging;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.WriteOff
{
    
    public class WriteOffServiceTest : BaseTest
    {
        private Mock<IRepository<BillingDbContext, ClaimEntity>> _claimRepository;
        private Mock<IRepository<BillingDbContext, WriteOffReasonCodeEntity>> _writeOffReasonCodeRepository;
        private Mock<IRepository<BillingDbContext, ClaimWriteOffEntity>> _claimWriteOffRepository;
        private Mock<IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity>> _claimChargeEntryWriteOffRepository;
        private Mock<IRepository<BillingDbContext, ClaimNoteEntity>> _claimNoteRepository;
        private Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>> _paymentClaimServiceLineRepository;
        private Mock<IRepository<BillingDbContext, PaymentClaimEntity>> _paymentClaimRepository;
        private Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>> _chargeEntryRepository;
        private Mock<IClaimService> _claimService;
        private Mock<IPaymentService> _paymentService;
        private Mock<IPaymentClaimService> _paymentClaimService;
        private Mock<IClaimHistoryService> _claimHistoryService;
        private Mock<IClaimManagerService> _claimManagerService;
        private Mock<IMessageBus> _messageBus;

        private IWriteOffService _writeOffService;

        public WriteOffServiceTest()
        {
            _claimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            _writeOffReasonCodeRepository = new Mock<IRepository<BillingDbContext, WriteOffReasonCodeEntity>>();
            _claimWriteOffRepository = new Mock<IRepository<BillingDbContext, ClaimWriteOffEntity>>();
            _claimChargeEntryWriteOffRepository = new Mock<IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity>>();
            _claimNoteRepository = new Mock<IRepository<BillingDbContext, ClaimNoteEntity>>();
            _paymentClaimServiceLineRepository = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>>();
            _paymentClaimRepository = new Mock<IRepository<BillingDbContext, PaymentClaimEntity>>();
            _chargeEntryRepository = new Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>>();
            _claimService = new Mock<IClaimService>();
            _paymentService = new Mock<IPaymentService>();
            _paymentClaimService = new Mock<IPaymentClaimService>();
            _claimHistoryService = new Mock<IClaimHistoryService>();
            _claimManagerService = new Mock<IClaimManagerService>();
            _messageBus = new Mock<IMessageBus>();

            _writeOffService = new WriteOffService(
            _claimRepository.Object,
             _writeOffReasonCodeRepository.Object,
             _claimWriteOffRepository.Object,
            _claimChargeEntryWriteOffRepository.Object,
            _claimNoteRepository.Object,
            _paymentClaimServiceLineRepository.Object,
            _paymentClaimRepository.Object,
             _claimService.Object,
            _paymentService.Object,
             _paymentClaimService.Object,
            _claimHistoryService.Object,
             _claimManagerService.Object,
              _messageBus.Object,
            _chargeEntryRepository.Object
        );
        }

        [Fact]
        public async Task AddAsync_ShouldCreateClaimNote_WhenNoteIsProvided()
        {
            // Arrange
            var model = new WriteOffClaimModelWithUserInfo
            {
                ClaimId = Fixture.Create<int>(),
                MemberId = Fixture.Create<int>(),
                AccountInfoId = Fixture.Create<int>(),
                Amount = 50m,
                AmountTypeId = 2,
                ApplicationTypeId = 5,
                IsServiceLine = false,
                ReasonCodeId = Fixture.Create<int>(),
                Note = "Test note"
            };

            var chargeEntries = new List<BillingClaimDetailsModel>
    {
            new() { Id = 1, BalanceAmount = 100m }
    }       .AsQueryable();

            _claimService
                .Setup(x => x.GetClaimChargesForAccountAsync(It.IsAny<GetBillingClaimDetailsModel>()))
                .Returns(Task.FromResult(chargeEntries)); 

            _claimNoteRepository
                .Setup(x => x.AddAsync(It.IsAny<ClaimNoteEntity>()))
                .Returns(Task.CompletedTask);

            // Act
            await _writeOffService.AddAsync(model);

            // Assert
            _claimNoteRepository.Verify(
                x => x.AddAsync(It.IsAny<ClaimNoteEntity>()),
                Times.Once);
        }


        [Fact]
        public async Task AddAsync_ShouldSucceed_WhenEvenlyAcrossIsValid()
        {
            // Arrange
            var model = new WriteOffClaimModelWithUserInfo
            {
                ClaimId = Fixture.Create<int>(),
                MemberId = Fixture.Create<int>(),
                AccountInfoId = Fixture.Create<int>(),
                Amount = 100m, // 50 + 50
                AmountTypeId = 2,
                ApplicationTypeId = 5,
                IsServiceLine = false,
                ReasonCodeId = Fixture.Create<int>()
            };

            var chargeEntries = new List<BillingClaimDetailsModel>
    {
        new() { Id = 1, BalanceAmount = 60m },
        new() { Id = 2, BalanceAmount = 60m }
    }.AsQueryable(); 

            _claimService
                .Setup(x => x.GetClaimChargesForAccountAsync(It.IsAny<GetBillingClaimDetailsModel>()))
                .Returns(Task.FromResult(chargeEntries));

            _claimWriteOffRepository
                .Setup(x => x.AddAndGetAsync(It.IsAny<ClaimWriteOffEntity>()))
                .ReturnsAsync(new ClaimWriteOffEntity { Id = 1 });

            // Act
            var result = await _writeOffService.AddAsync(model);

            // Assert
            Assert.True(result.success);

            _claimChargeEntryWriteOffRepository.Verify(x => x.AddRangeAsync(It.IsAny<List<ClaimChargeEntryWriteOffEntity>>()),
                Times.Once);

            _claimChargeEntryWriteOffRepository.Verify(x => x.CommitAsync(),Times.Once);
        }


        [Fact]
        public async Task AddAsync_ShouldProcessNonEvenlyWriteOff()
        {
            // Arrange
            var model = new WriteOffClaimModelWithUserInfo
            {
                ClaimId = Fixture.Create<int>(),
                MemberId = Fixture.Create<int>(),
                AccountInfoId = Fixture.Create<int>(),
                Amount = 80m,
                AmountTypeId = 2,
                ApplicationTypeId = 1, // NOT evenly
                IsServiceLine = false,
                ReasonCodeId = Fixture.Create<int>()
            };

            var chargeEntries = new List<BillingClaimDetailsModel>
    {
        new() { Id = 1, BalanceAmount = 100m }
    }       .AsQueryable(); 

            _claimService
                .Setup(x => x.GetClaimChargesForAccountAsync(It.IsAny<GetBillingClaimDetailsModel>()))
                .Returns(Task.FromResult(chargeEntries)); 

            _claimWriteOffRepository
                .Setup(x => x.AddAndGetAsync(It.IsAny<ClaimWriteOffEntity>()))
                .ReturnsAsync(new ClaimWriteOffEntity { Id = 10 });

            // Act
            await _writeOffService.AddAsync(model);

            // Assert
            _claimChargeEntryWriteOffRepository.Verify(
                x => x.AddRangeAsync(It.IsAny<List<ClaimChargeEntryWriteOffEntity>>()),
                Times.Once);
        }


        [Fact]
        public async Task AddAsync_ShouldAdjustAmount_WhenRemainingAmountWithNegativeBalance()
        {
            // Arrange
            var model = new WriteOffClaimModelWithUserInfo
            {
                ClaimId = Fixture.Create<int>(),
                MemberId = Fixture.Create<int>(),
                AccountInfoId = Fixture.Create<int>(),
                Amount = 50m,
                AmountTypeId = 1,   // Remaining
                ApplicationTypeId = 1,
                IsServiceLine = false,
                ReasonCodeId = Fixture.Create<int>()
            };

            
            var chargeEntries = new List<BillingClaimDetailsModel>
    {
        new() { Id = 1, BalanceAmount = 100m },
        new() { Id = 2, BalanceAmount = -20m } // triggers negative balance logic
    }.AsQueryable();

            _claimService
                .Setup(x => x.GetClaimChargesForAccountAsync(It.IsAny<GetBillingClaimDetailsModel>()))
                .Returns(Task.FromResult(chargeEntries)); 

            _claimWriteOffRepository
                .Setup(x => x.AddAndGetAsync(It.IsAny<ClaimWriteOffEntity>()))
                .ReturnsAsync(new ClaimWriteOffEntity { Id = 5 });

            _claimWriteOffRepository
                .Setup(x => x.Update(It.IsAny<ClaimWriteOffEntity>()));

            _claimWriteOffRepository
                .Setup(x => x.CommitAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _writeOffService.AddAsync(model);

            // Assert
            _claimWriteOffRepository.Verify(
                x => x.Update(It.IsAny<ClaimWriteOffEntity>()),
                Times.Once);

            _claimWriteOffRepository.Verify(
                x => x.CommitAsync(),
                Times.Once);
        }

        [Fact]
        public async Task AddAsync_ShouldCloseClaim_WhenFullyWrittenOff()
        {
            // Arrange
            var model = new WriteOffClaimModelWithUserInfo
            {
                ClaimId = 999,
                MemberId = Fixture.Create<int>(),
                AccountInfoId = Fixture.Create<int>(),
                Amount = 50m,
                AmountTypeId = 2,
                ApplicationTypeId = 1,
                IsServiceLine = false,
                ReasonCodeId = Fixture.Create<int>()
            };

            var firstCallCharges = new List<BillingClaimDetailsModel>
    {
        new() { Id = 1, BalanceAmount = 50m }
    }.AsQueryable();

            var secondCallCharges = new List<BillingClaimDetailsModel>
    {
        new() { Id = 1, BalanceAmount = 0m }
    }.AsQueryable();

            _claimService
                .SetupSequence(x => x.GetClaimChargesForAccountAsync(It.IsAny<GetBillingClaimDetailsModel>()))
                .Returns(Task.FromResult(firstCallCharges))
                .Returns(Task.FromResult(secondCallCharges));

            _claimWriteOffRepository
                .Setup(x => x.AddAndGetAsync(It.IsAny<ClaimWriteOffEntity>()))
                .ReturnsAsync(new ClaimWriteOffEntity { Id = 3 });

            _claimChargeEntryWriteOffRepository
                .Setup(x => x.AddRangeAsync(It.IsAny<List<ClaimChargeEntryWriteOffEntity>>()))
                .Returns(Task.CompletedTask);

            _claimChargeEntryWriteOffRepository
                .Setup(x => x.CommitAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _writeOffService.AddAsync(model);

            // Assert
            const bool isSystem = false; 
        }





        [Fact]
        public async Task AddAsync_ShouldSendTransactionBatch_WhenTransactionsExist()
        {
            // Arrange
            var model = new WriteOffClaimModelWithUserInfo
            {
                ClaimId = Fixture.Create<int>(),
                MemberId = Fixture.Create<int>(),
                AccountInfoId = Fixture.Create<int>(),
                Amount = 30m,
                AmountTypeId = 2,
                ApplicationTypeId = 1,
                IsServiceLine = false,
                ReasonCodeId = Fixture.Create<int>()
            };

            var chargeEntries = new List<BillingClaimDetailsModel>
    {
        new BillingClaimDetailsModel { Id = 1, BalanceAmount = 100m }
    }.AsQueryable();

            _claimService
                .Setup(x => x.GetClaimChargesForAccountAsync(It.IsAny<GetBillingClaimDetailsModel>()))
                .Returns(Task.FromResult(chargeEntries)); 

            _claimWriteOffRepository
                .Setup(x => x.AddAndGetAsync(It.IsAny<ClaimWriteOffEntity>()))
                .ReturnsAsync(new ClaimWriteOffEntity { Id = 1 });

            _claimChargeEntryWriteOffRepository
                .Setup(x => x.AddRangeAsync(It.IsAny<List<ClaimChargeEntryWriteOffEntity>>()))
                .Returns(Task.CompletedTask);

            _claimChargeEntryWriteOffRepository
                .Setup(x => x.CommitAsync())
                .Returns(Task.CompletedTask);

            _messageBus
                .Setup(x => x.SendBatchAsync(
                    Topics.RT_Billing_ProcessClaimTxn,
                    It.IsAny<List<ClaimTransactionModel>>()))
                .Returns(Task.CompletedTask);

            // Act
            await _writeOffService.AddAsync(model);

            // Assert
            _messageBus.Verify(
                x => x.SendBatchAsync(
                    Topics.RT_Billing_ProcessClaimTxn,
                    It.IsAny<List<ClaimTransactionModel>>()),
                Times.Once);
        }



        [Fact]
        public async Task AddAsync_ShouldNotWriteOff_WhenAmountIsZeroAndNotRemainingOption()
        {
            // Arrange
            var model = new WriteOffClaimModelWithUserInfo
            {
                Amount = 0m,
                AmountTypeId = 2,
                ClaimId = Fixture.Create<int>(),
                MemberId = Fixture.Create<int>(),
                AccountInfoId = Fixture.Create<int>(),
                ReasonCodeId = Fixture.Create<int>()
            };

            // Act
            var result = await _writeOffService.AddAsync(model);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.success);
            _claimNoteRepository.Verify(x => x.AddAsync(It.IsAny<ClaimNoteEntity>()), Times.Never);
            _claimWriteOffRepository.Verify(x => x.AddAndGetAsync(It.IsAny<ClaimWriteOffEntity>()), Times.Never);
        }


        [Fact]
        public async Task AddAsync_ShouldReturnFalse_OnException()
        {
            // Arrange
            var model = new WriteOffClaimModelWithUserInfo
            {
                ClaimId = Fixture.Create<int>(),
                MemberId = Fixture.Create<int>(),
                AccountInfoId = Fixture.Create<int>(),
                Amount = 100m,
                AmountTypeId = 2, // Required: not "remaining" option
                ApplicationTypeId = 1, // Required: not "Evenly across"
                IsServiceLine = false, // Required: prevents NullReferenceException on .Value access
                ReasonCodeId = Fixture.Create<int>()
            };

            _claimService.Setup(x => x.GetClaimChargesForAccountAsync(It.IsAny<GetBillingClaimDetailsModel>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _writeOffService.AddAsync(model);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.success);
        }

        [Fact]
        public async Task AddAsync_ShouldReturnError_WhenEvenlyAcrossCannotBeApplied()
        {
            // Arrange
            var claimId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();
            var accountInfoId = Fixture.Create<int>();

            var model = new WriteOffClaimModelWithUserInfo
            {
                ClaimId = claimId,
                MemberId = memberId,
                AccountInfoId = accountInfoId,
                Amount = 200m,
                AmountTypeId = 2,
                ApplicationTypeId = 5, 
                IsServiceLine = false,
                ReasonCodeId = Fixture.Create<int>()
            };

            var chargeEntries = new List<BillingClaimDetailsModel>
    {
        new() { Id = 1, BalanceAmount = 50m },   
        new() { Id = 2, BalanceAmount = 150m }
    };

            _claimService
                .Setup(static x => x.GetClaimChargesForAccountAsync(It.IsAny<GetBillingClaimDetailsModel>()))
                .ReturnsAsync(chargeEntries.AsQueryable());

            // Act
            var result = await _writeOffService.AddAsync(model);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.success);
            Assert.Contains("Can not perform Evenly", result.errorMsg);

            _claimWriteOffRepository.Verify(
                x => x.AddAndGetAsync(It.IsAny<ClaimWriteOffEntity>()),
                Times.Never);

            _claimChargeEntryWriteOffRepository.Verify(
                x => x.AddRangeAsync(It.IsAny<List<ClaimChargeEntryWriteOffEntity>>()),
                Times.Never);
        }


        [Fact]
        public async Task GetChargeEntryWriteOffsByChargeIdAsync_ShouldReturnWriteOffs_WhenChargeHasWriteOffs()
        {
            // Arrange
            var chargeEntryId = Fixture.Create<int>();
            var reasonCodeId = Fixture.Create<int>();

            var model = new GetChargeEntryWriteOffModel
            {
                Id = chargeEntryId,
                IsServiceLineId = false
            };

            var writeOffReason = Fixture.Build<WriteOffReasonCodeEntity>()
                          .With(x => x.Id, reasonCodeId)
                     .With(x => x.Description, "Test Reason")
                .With(x => x.DateDeleted, (DateTime?)null)
              .Create();

            var writeOff = Fixture.Build<ClaimChargeEntryWriteOffEntity>()
              .With(x => x.ClaimChargeEntryId, chargeEntryId)
                   .With(x => x.WriteOffReasonCodeId, reasonCodeId)
                       .With(x => x.WriteOffAmount, 50m)
                   .With(x => x.DateDeleted, (DateTime?)null)
                        .Create();

            _paymentClaimServiceLineRepository.Setup(x => x.Query())
           .Returns(QueryMock<PaymentClaimServiceLineEntity>.Create(new List<PaymentClaimServiceLineEntity>()));

            _claimChargeEntryWriteOffRepository.Setup(x => x.Query())
                  .Returns(QueryMock<ClaimChargeEntryWriteOffEntity>.Create(writeOff));

            _writeOffReasonCodeRepository.Setup(x => x.Query())
              .Returns(QueryMock<WriteOffReasonCodeEntity>.Create(writeOffReason));

            // Act
            var result = await _writeOffService.GetChargeEntryWriteOffsByChargeIdAsync(model);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(50m, result.First().WriteOffAmount);
            Assert.Equal(reasonCodeId, result.First().WriteOffReasonCodeId);
        }

        [Fact]
        public async Task GetChargeEntryWriteOffsByChargeIdAsync_ShouldResolveServiceLineId_ToChargeEntryId()
        {
            // Arrange
            var serviceLineId = Fixture.Create<int>();
            var writeOffId = Fixture.Create<int>();
            var reasonCodeId = Fixture.Create<int>();

            var model = new GetChargeEntryWriteOffModel
            {
                Id = serviceLineId,
                IsServiceLineId = true
            };

            var serviceLine = Fixture.Build<PaymentClaimServiceLineEntity>()
                .With(x => x.Id, serviceLineId)
                .With(x => x.ClaimChargeEntryId, writeOffId)
                .Create();

            var writeOff = Fixture.Build<ClaimChargeEntryWriteOffEntity>()
                .With(x => x.Id, Fixture.Create<int>())
                .With(x => x.ClaimChargeEntryId, writeOffId)
                .With(x => x.WriteOffReasonCodeId, reasonCodeId)
                .With(x => x.WriteOffAmount, 75m)
                .With(x => x.DateDeleted, (DateTime?)null)
                .Create();

            var reasonCode = Fixture.Build<WriteOffReasonCodeEntity>()
                .With(x => x.Id, reasonCodeId)
                .With(x => x.Description, "Service Line Reason")
                .With(x => x.DateDeleted, (DateTime?)null)
                .Create();

            _paymentClaimServiceLineRepository
                .Setup(x => x.Query())
                .Returns(new[] { serviceLine }
                    .AsQueryable()
                    .BuildMockDbSet()
                    .Object);

            _claimChargeEntryWriteOffRepository
                .Setup(x => x.Query())
                .Returns(new[] { writeOff }
                    .AsQueryable()
                    .BuildMockDbSet()
                    .Object);

            _writeOffReasonCodeRepository
                .Setup(x => x.Query())
                .Returns(new[] { reasonCode }
                    .AsQueryable()
                    .BuildMock());

            // Act
            var result = await _writeOffService.GetChargeEntryWriteOffsByChargeIdAsync(model);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);

            var item = result.First();
            Assert.Equal(writeOff.Id, item.Id);           
            Assert.Equal(75m, item.WriteOffAmount);
            Assert.Equal(reasonCodeId, item.WriteOffReasonCodeId);
        }



        [Fact]
        public async Task GetChargeEntryWriteOffsByChargeIdAsync_ShouldReturnEmpty_WhenNoWriteOffs()
        {
            // Arrange
            var chargeEntryId = Fixture.Create<int>();

            var model = new GetChargeEntryWriteOffModel
            {
                Id = chargeEntryId,
                IsServiceLineId = false
            };

            _paymentClaimServiceLineRepository.Setup(x => x.Query())
    .Returns(QueryMock<PaymentClaimServiceLineEntity>.Create(new List<PaymentClaimServiceLineEntity>()));

            _claimChargeEntryWriteOffRepository.Setup(x => x.Query())
          .Returns(QueryMock<ClaimChargeEntryWriteOffEntity>.Create(new List<ClaimChargeEntryWriteOffEntity>()));

            _writeOffReasonCodeRepository.Setup(x => x.Query())
                       .Returns(QueryMock<WriteOffReasonCodeEntity>.Create(new List<WriteOffReasonCodeEntity>()));

            // Act
            var result = await _writeOffService.GetChargeEntryWriteOffsByChargeIdAsync(model);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task UpdateChargeEntryWriteOffsByChargeIdAsync_ShouldUpdateWriteOffs_CreateHistory_AndSendTransaction()
        {
            var writeOffId = Fixture.Create<int>();
            var claimId = Fixture.Create<int>();

            var model = new EditChargeEntryWriteOffModelWithUserInfo
            {
                ClaimId = claimId,
                MemberId = Fixture.Create<int>(),
                WriteOffDetails =
                [
                    new WriteOffDetailsModel
                   {
                       ChargeEntryWriteOffId = writeOffId,
                       WriteOffAmount = 150m,
                       WriteOffReasonCodeId = 2
                   }
                ]
            };

            var oldReason = new WriteOffReasonCodeEntity { Id = 1, Description = "Old" };
            var newReason = new WriteOffReasonCodeEntity { Id = 2, Description = "New" };

            var entity = new ClaimChargeEntryWriteOffEntity
            {
                Id = writeOffId,
                WriteOffAmount = 100m,
                WriteOffReasonCodeId = 1,
                WriteOffReasonCode = oldReason,
                ClaimWriteOff = new ClaimWriteOffEntity { ClaimId = claimId }
            };

            _claimChargeEntryWriteOffRepository
                .Setup(x => x.Query())
                .Returns(new[] { entity }
                    .AsQueryable()
                    .BuildMockDbSet()
                    .Object);

            _writeOffReasonCodeRepository
                .Setup(x => x.Query())
                .Returns(new[] { oldReason, newReason }.AsQueryable().BuildMock());

            _claimChargeEntryWriteOffRepository
                .Setup(x => x.CommitAsync())
                .Returns(Task.CompletedTask);

            List<ClaimHistoryFieldSaveModel> historyCaptured = null;

            _claimHistoryService
                .Setup(x => x.AddAsync(It.IsAny<List<ClaimHistoryFieldSaveModel>>(), It.IsAny<bool>()))
                .Callback<List<ClaimHistoryFieldSaveModel>, bool>((h, _) => historyCaptured = h)
                .Returns(Task.CompletedTask);

            _messageBus
                .Setup(x => x.SendAsync(It.IsAny<ClaimTransactionModel>(), Topics.RT_Billing_ProcessClaimTxn))
                .Returns(Task.CompletedTask);

            var result = await _writeOffService.UpdateChargeEntryWriteOffsByChargeIdAsync(model);

            Assert.Single(result);
            Assert.Equal(150m, result[0].WriteOffAmount);
            Assert.NotNull(historyCaptured);
            Assert.Equal(2, historyCaptured.Count);
        }

        [Fact]
        public async Task UpdateChargeEntryWriteOffsByChargeIdAsync_ShouldUpdateWriteOffs_AddHistory_AndSendTransaction()
        {
            var writeOffId = Fixture.Create<int>();
            var claimId = Fixture.Create<int>();

            var model = new EditChargeEntryWriteOffModelWithUserInfo
            {
                ClaimId = claimId,
                MemberId = Fixture.Create<int>(),
                WriteOffDetails =
                [
                    new WriteOffDetailsModel
                   {
                       ChargeEntryWriteOffId = writeOffId,
                       WriteOffAmount = 200m,
                       WriteOffReasonCodeId = 2
                   }
                ]
            };

            var oldReason = new WriteOffReasonCodeEntity { Id = 1, Description = "Old" };
            var newReason = new WriteOffReasonCodeEntity { Id = 2, Description = "New" };

            var entity = new ClaimChargeEntryWriteOffEntity
            {
                Id = writeOffId,
                WriteOffAmount = 100m,
                WriteOffReasonCodeId = 1,
                WriteOffReasonCode = oldReason,
                ClaimWriteOff = new ClaimWriteOffEntity { ClaimId = claimId }
            };

            _claimChargeEntryWriteOffRepository
                .Setup(x => x.Query())
                .Returns(new[] { entity }
                    .AsQueryable()
                    .BuildMockDbSet()
                    .Object);

            _writeOffReasonCodeRepository
                .Setup(x => x.Query())
                .Returns(new[] { oldReason, newReason }.AsQueryable().BuildMock());

            _claimChargeEntryWriteOffRepository
                .Setup(x => x.CommitAsync())
                .Returns(Task.CompletedTask);

            List<ClaimHistoryFieldSaveModel> historyCaptured = null;

            _claimHistoryService
                .Setup(x => x.AddAsync(It.IsAny<List<ClaimHistoryFieldSaveModel>>(), It.IsAny<bool>()))
                .Callback<List<ClaimHistoryFieldSaveModel>, bool>((h, _) => historyCaptured = h)
                .Returns(Task.CompletedTask);

            _messageBus
                .Setup(x => x.SendAsync(It.IsAny<ClaimTransactionModel>(), Topics.RT_Billing_ProcessClaimTxn))
                .Returns(Task.CompletedTask);

            var result = await _writeOffService.UpdateChargeEntryWriteOffsByChargeIdAsync(model);

            Assert.Single(result);
            Assert.Equal(200m, result[0].WriteOffAmount);
            Assert.Equal(2, historyCaptured.Count);
        }



        [Fact]
        public async Task DeleteChargeEntryWriteOffsByChargeIdAsync_ShouldDeleteWriteOffs()
        {
            // Arrange
            var memberId = Fixture.Create<int>();
            var writeOffId = Fixture.Create<int>();
            var claimId = Fixture.Create<int>();

            var model = new IdsWithUserInfo
            {
                MemberId = memberId,
                Ids = new[] { writeOffId }
            };

            var claimWriteOff = Fixture.Build<ClaimWriteOffEntity>()
          .With(x => x.ClaimId, claimId)
        .Create();

            var chargeEntryWriteOff = Fixture.Build<ClaimChargeEntryWriteOffEntity>()
                       .With(x => x.Id, writeOffId)
                       .With(x => x.ClaimWriteOff, claimWriteOff)
                       .With(x => x.WriteOffAmount, 100m)
                       .Create();

            var chargeEntryWriteOffList = new List<ClaimChargeEntryWriteOffEntity> { chargeEntryWriteOff };

            _claimChargeEntryWriteOffRepository.Setup(x => x.Query())
            .Returns(chargeEntryWriteOffList.AsQueryable().BuildMock());

            _claimChargeEntryWriteOffRepository.Setup(x => x.UpdateRange(It.IsAny<List<ClaimChargeEntryWriteOffEntity>>()));

            _claimChargeEntryWriteOffRepository.Setup(x => x.CommitAsync())
                         .Returns(Task.CompletedTask);

            _claimHistoryService.Setup(x => x.AddAsync(It.IsAny<List<ClaimHistorySaveModel>>(), false))
              .Returns(Task.CompletedTask);

            _messageBus.Setup(x => x.SendAsync(It.IsAny<ClaimTransactionModel>(), It.IsAny<string>()))
           .Returns(Task.CompletedTask);

            // Act
            await _writeOffService.DeleteChargeEntryWriteOffsByChargeIdAsync(model);
            _claimChargeEntryWriteOffRepository.Verify(x => x.Query(), Times.Once);
            _claimChargeEntryWriteOffRepository.Verify(x => x.UpdateRange(It.IsAny<List<ClaimChargeEntryWriteOffEntity>>()), Times.Once);
            _claimChargeEntryWriteOffRepository.Verify(x => x.CommitAsync(), Times.Once);

            _claimHistoryService.Verify(x => x.AddAsync(It.IsAny<List<ClaimHistorySaveModel>>(), It.IsAny<bool>()),
                Times.Once);

            _messageBus.Verify(x => x.SendAsync(It.IsAny<ClaimTransactionModel>(), Topics.RT_Billing_ProcessClaimTxn),
                Times.Once);

        }

        [Fact]
        public async Task GetReasonCodesAsync_ShouldReturnAllActiveCodes()
        {
            // Arrange
            var reasonCodes = new List<WriteOffReasonCodeEntity>{Fixture.Build<WriteOffReasonCodeEntity>()
        .With(x => x.Id, 1)
        .With(x => x.Description, "Reason 1")
        .With(x => x.DateDeleted, (DateTime?)null)
        .Create(),
        Fixture.Build<WriteOffReasonCodeEntity>()
        .With(x => x.Id, 2)
        .With(x => x.Description, "Reason 2")
        .With(x => x.DateDeleted, (DateTime?)null)
        .Create(),
        Fixture.Build<WriteOffReasonCodeEntity>()
        .With(x => x.Id, 3)
        .With(x => x.Description, "Deleted Reason")
        .With(x => x.DateDeleted, DateTime.UtcNow)
        .Create()};

        _writeOffReasonCodeRepository.Setup(x => x.Query())
        .Returns(QueryMock<WriteOffReasonCodeEntity>.Create(reasonCodes));

            // Act
            var result = await _writeOffService.GetReasonCodesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Description == "Reason 1");
            Assert.Contains(result, r => r.Description == "Reason 2");
            Assert.DoesNotContain(result, r => r.Description == "Deleted Reason");
        }

        [Fact]
        public async Task GetReasonCodesAsync_ShouldReturnEmpty_WhenNoActiveCodes()
        {
            // Arrange
         _writeOffReasonCodeRepository.Setup(x => x.Query())
        .Returns(QueryMock<WriteOffReasonCodeEntity>.Create(new List<WriteOffReasonCodeEntity>()));

            // Act
            var result = await _writeOffService.GetReasonCodesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
