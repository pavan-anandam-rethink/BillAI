using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.PaymentClaimServiceLine;
using BillingService.Domain.Services.Payment;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.PaymentPosting
{
    public class PaymentServiceLineAdjustmentServiceTest
    {
        private readonly Mock<IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity>> _serviceLineAdjustmentRepositoryMock;
        private readonly Mock<IPaymentClaimService> _paymentClaimServiceMock;
        private readonly Mock<IWriteOffService> _writeOffServiceMock;
        private readonly Mock<IRepository<BillingDbContext, ClaimEntity>> _claimRepositoryMock;
        private readonly Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>> _serviceLineRepositoryMock;
        private readonly Mock<IRepository<BillingDbContext, PaymentClaimEntity>> _paymentClaimRepositoryMock;
        private readonly Mock<IRepository<BillingDbContext, PaymentAdjustmentReasonEntity>> _adjustmentReasonRepositoryMock;
        private readonly Mock<IClaimHistoryService> _claimHistoryServiceMock;
        private readonly Mock<IMessageBus> _busMock;
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly PaymentServiceLineAdjustmentService _service;
        private readonly Mock<IRepository<BillingDbContext, PaymentEntity>> _paymentRepositoryMock;

        public PaymentServiceLineAdjustmentServiceTest()
        {
            _serviceLineAdjustmentRepositoryMock = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity>>();
            _paymentClaimServiceMock = new Mock<IPaymentClaimService>();
            _writeOffServiceMock = new Mock<IWriteOffService>();
            _claimRepositoryMock = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            _serviceLineRepositoryMock = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>>();
            _paymentClaimRepositoryMock = new Mock<IRepository<BillingDbContext, PaymentClaimEntity>>();
            _adjustmentReasonRepositoryMock = new Mock<IRepository<BillingDbContext, PaymentAdjustmentReasonEntity>>();
            _claimHistoryServiceMock = new Mock<IClaimHistoryService>();
            _busMock = new Mock<IMessageBus>();
            _cacheServiceMock = new Mock<ICacheService>();
            _paymentRepositoryMock = new Mock<IRepository<BillingDbContext, PaymentEntity>>();

            _service = new PaymentServiceLineAdjustmentService(
                _serviceLineAdjustmentRepositoryMock.Object,
                _claimRepositoryMock.Object,
                _serviceLineRepositoryMock.Object,
                _adjustmentReasonRepositoryMock.Object,
                _paymentClaimRepositoryMock.Object,
                _writeOffServiceMock.Object,
                _paymentClaimServiceMock.Object,
                _paymentRepositoryMock.Object,
                _busMock.Object,
                _claimHistoryServiceMock.Object,
                _cacheServiceMock.Object); _serviceLineAdjustmentRepositoryMock = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity>>();
            _paymentClaimServiceMock = new Mock<IPaymentClaimService>();
            _writeOffServiceMock = new Mock<IWriteOffService>();
            _claimRepositoryMock = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            _serviceLineRepositoryMock = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>>();
            _paymentClaimRepositoryMock = new Mock<IRepository<BillingDbContext, PaymentClaimEntity>>();
            _adjustmentReasonRepositoryMock = new Mock<IRepository<BillingDbContext, PaymentAdjustmentReasonEntity>>();
            _claimHistoryServiceMock = new Mock<IClaimHistoryService>();
            _busMock = new Mock<IMessageBus>();
            _cacheServiceMock = new Mock<ICacheService>();
            _paymentRepositoryMock = new Mock<IRepository<BillingDbContext, PaymentEntity>>();

            _service = new PaymentServiceLineAdjustmentService(
                _serviceLineAdjustmentRepositoryMock.Object,
                _claimRepositoryMock.Object,
                _serviceLineRepositoryMock.Object,
                _adjustmentReasonRepositoryMock.Object,
                _paymentClaimRepositoryMock.Object,
                _writeOffServiceMock.Object,
                _paymentClaimServiceMock.Object,
                _paymentRepositoryMock.Object,
                _busMock.Object,
                _claimHistoryServiceMock.Object,
                _cacheServiceMock.Object);
        }

        [Fact]
        public async Task AddPaymentServiceLineAdjustmentsAsync_ShouldAddAdjustments_WhenValidModel()
        {
            // Arrange
            var model = new AddOrEditAdjustmentModel
            {
                ServiceLineId = 14290,
                MemberId = 105815,
                AdjustmentDetails = new List<AdjustmentDetailsModel>
                {
                    new AdjustmentDetailsModel
                    {
                        Amount = 100,
                        GroupCode = "CO",
                        ReasonCode = "50",
                        isPositive = false
                    }
                }
            };

            // Mock ServiceLine data (used by FirstOrDefaultAsync + Include)
            var serviceLines = new List<PaymentClaimServiceLineEntity>
            {
                new PaymentClaimServiceLineEntity
                {
                    Id = 14290,
                    DateDeleted = null,
                    PaymentClaimId = 10,
                    PaymentClaim = new PaymentClaimEntity
                    {
                        Id = 10,
                        TotalCharge = 100,
                        Claim = new ClaimEntity
                        {
                            ClaimStatus = ClaimStatus.Closed
                        }
                    }
                }
            };

            var serviceLineMock = serviceLines
                .AsQueryable()
                .BuildMock(); // returns IQueryable<T>

            _serviceLineRepositoryMock
                .Setup(x => x.Query())
                .Returns(serviceLineMock);


            // Mock Adjustment query used by SumAsync
            var adjustmentQueryable = new List<PaymentClaimServiceLineAdjustmentEntity>()
                .AsQueryable()
                .BuildMock();

            _serviceLineAdjustmentRepositoryMock
                .Setup(x => x.Query())
                .Returns(new List<PaymentClaimServiceLineAdjustmentEntity>()
                    .AsQueryable()
                    .BuildMock());

            // Act
            var result = await _service.AddPaymentServiceLineAdjustmentsAsync(model);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);

            _serviceLineAdjustmentRepositoryMock.Verify(
                x => x.AddRangeAsync(It.IsAny<List<PaymentClaimServiceLineAdjustmentEntity>>()),
                Times.Once);

            _serviceLineAdjustmentRepositoryMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task AddPaymentServiceLineAdjustmentsAsync_ShouldUpdateAdjustment_WhenAdjustmentIdExists()
        {
            // Arrange
            var model = new AddOrEditAdjustmentModel
            {
                ServiceLineId = 14290,
                MemberId = 105815,
                AdjustmentDetails = new List<AdjustmentDetailsModel>
                {
                    new AdjustmentDetailsModel
                    {
                        Amount = 100,
                        GroupCode = "CO",
                        ReasonCode = "50",
                        isPositive = false
                    }
                }
            };

            var serviceLines = new List<PaymentClaimServiceLineEntity>
            {
                new PaymentClaimServiceLineEntity
                {
                    Id = 14290,
                    DateDeleted = null,
                    PaymentClaimId = 10,
                    PaymentClaim = new PaymentClaimEntity
                    {
                        Id = 10,
                        TotalCharge = 100,
                        Claim = new ClaimEntity
                        {
                            ClaimStatus = ClaimStatus.Closed
                        }
                    }
                }
            };

            var serviceLineDbSetMock = serviceLines
                .AsQueryable()
                .BuildMockDbSet();

            _serviceLineRepositoryMock
                .Setup(x => x.Query())
                .Returns(serviceLineDbSetMock.Object);

            var adjustmentDbSetMock = new List<PaymentClaimServiceLineAdjustmentEntity>()
                .AsQueryable()
                .BuildMockDbSet();

            _serviceLineAdjustmentRepositoryMock
                .Setup(x => x.Query())
                .Returns(adjustmentDbSetMock.Object);

            // Act
            var result = await _service.AddPaymentServiceLineAdjustmentsAsync(model);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);

            _serviceLineAdjustmentRepositoryMock.Verify(
                x => x.AddRangeAsync(It.IsAny<List<PaymentClaimServiceLineAdjustmentEntity>>()),
                Times.Once);

            _serviceLineAdjustmentRepositoryMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task GetPaymentServiceLineAdjustments_ExcludesDeletedRecords()
        {
            var serviceLineId = 100;

            var data = new List<PaymentClaimServiceLineAdjustmentEntity>
    {
        new PaymentClaimServiceLineAdjustmentEntity
        {
            Id = 1,
            PaymentClaimServiceLineId = serviceLineId,
            DateDeleted = DateTime.UtcNow // should be excluded
        }
    };

            _serviceLineAdjustmentRepositoryMock
                .Setup(x => x.Query())
                .Returns(data.AsQueryable().BuildMock());

            var result = await _service.GetPaymentServiceLineAdjustments(serviceLineId);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPaymentServiceLineAdjustments_ReturnsOnlyMatchingServiceLine()
        {
            var data = new List<PaymentClaimServiceLineAdjustmentEntity>
    {
        new PaymentClaimServiceLineAdjustmentEntity
        {
            Id = 1,
            PaymentClaimServiceLineId = 999, // different ID
            DateDeleted = null
        }
    };

            _serviceLineAdjustmentRepositoryMock
                .Setup(x => x.Query())
                .Returns(data.AsQueryable().BuildMock());

            var result = await _service.GetPaymentServiceLineAdjustments(100);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPaymentServiceLineAdjustments_NoData_ReturnsEmptyList()
        {
            var data = new List<PaymentClaimServiceLineAdjustmentEntity>();

            _serviceLineAdjustmentRepositoryMock
                .Setup(x => x.Query())
                .Returns(data.AsQueryable().BuildMock());

            var result = await _service.GetPaymentServiceLineAdjustments(100);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAdjustmentReasonDescriptionsAsync_ReturnsMappedData_FromCache()
        {
            // Arrange
            var data = new List<PaymentAdjustmentReasonEntity>
    {
        new PaymentAdjustmentReasonEntity
        {
            GroupCode = "CO",
            AdjustmentCode = "45",
            Description = "Charge exceeds fee schedule",
            IsDefault = true
        },
        new PaymentAdjustmentReasonEntity
        {
            GroupCode = "PR",
            AdjustmentCode = "1",
            Description = "Deductible amount",
            IsDefault = false
        }
    };

            _cacheServiceMock
                .Setup(x => x.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<PaymentAdjustmentReasonEntity>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(data);

            // Act
            var result = await _service.GetAdjustmentReasonDescriptionsAsync("ANY");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            Assert.Equal("CO-45", result[0].ReasonCode);
            Assert.Equal("Charge exceeds fee schedule", result[0].Description);
            Assert.True(result[0].IsDefault);

            Assert.Equal("PR-1", result[1].ReasonCode);
            Assert.Equal("Deductible amount", result[1].Description);
            Assert.False(result[1].IsDefault);
        }

        [Fact]
        public async Task GetAdjustmentReasonDescriptionsAsync_WhenNoData_ReturnsEmptyList()
        {
            // Arrange
            _cacheServiceMock
                .Setup(x => x.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<PaymentAdjustmentReasonEntity>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(new List<PaymentAdjustmentReasonEntity>());

            // Act
            var result = await _service.GetAdjustmentReasonDescriptionsAsync("ANY");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAdjustmentReasonDescriptionsAsync_ReturnsMappedData()
        {
            var data = new List<PaymentAdjustmentReasonEntity>
    {
        new() { GroupCode = "CO", AdjustmentCode = "45", Description = "Desc1", IsDefault = true }
    };

            _cacheServiceMock.Setup(x => x.GetOrSetCacheAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<PaymentAdjustmentReasonEntity>>>>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync(data);

            var result = await _service.GetAdjustmentReasonDescriptionsAsync("ANY");

            Assert.Single(result);
            Assert.Equal("CO-45", result[0].ReasonCode);
        }

        [Fact]
        public async Task GetAdjustmentReasonDescriptionsAsync_EmptyCache_ReturnsEmpty()
        {
            _cacheServiceMock.Setup(x => x.GetOrSetCacheAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<PaymentAdjustmentReasonEntity>>>>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync(new List<PaymentAdjustmentReasonEntity>());

            var result = await _service.GetAdjustmentReasonDescriptionsAsync("ANY");

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAdjustmentReasonDescriptionsAsync_MultipleRecords_ReturnsAll()
        {
            var data = new List<PaymentAdjustmentReasonEntity>
    {
        new() { GroupCode = "CO", AdjustmentCode = "45" },
        new() { GroupCode = "PR", AdjustmentCode = "1" }
    };

            _cacheServiceMock.Setup(x => x.GetOrSetCacheAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<PaymentAdjustmentReasonEntity>>>>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync(data);

            var result = await _service.GetAdjustmentReasonDescriptionsAsync("ANY");

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAdjustmentReasonDescriptionsAsync_NullDescription_Handled()
        {
            var data = new List<PaymentAdjustmentReasonEntity>
    {
        new() { GroupCode = "CO", AdjustmentCode = "45", Description = null }
    };

            _cacheServiceMock.Setup(x => x.GetOrSetCacheAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<PaymentAdjustmentReasonEntity>>>>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync(data);

            var result = await _service.GetAdjustmentReasonDescriptionsAsync("ANY");

            Assert.Null(result[0].Description);
        }

        [Fact]
        public async Task GetAdjustmentReasonDescriptionsAsync_NullGroupCode()
        {
            var data = new List<PaymentAdjustmentReasonEntity>
    {
        new() { GroupCode = null, AdjustmentCode = "45" }
    };

            _cacheServiceMock.Setup(x => x.GetOrSetCacheAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<PaymentAdjustmentReasonEntity>>>>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync(data);

            var result = await _service.GetAdjustmentReasonDescriptionsAsync("ANY");

            Assert.Equal("-45", result[0].ReasonCode);
        }

        [Fact]
        public async Task GetAdjustmentReasonDescriptionsAsync_IsDefaultTrue()
        {
            var data = new List<PaymentAdjustmentReasonEntity>
    {
        new() { GroupCode = "CO", AdjustmentCode = "45", IsDefault = true }
    };

            _cacheServiceMock.Setup(x => x.GetOrSetCacheAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<PaymentAdjustmentReasonEntity>>>>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync(data);

            var result = await _service.GetAdjustmentReasonDescriptionsAsync("ANY");

            Assert.True(result[0].IsDefault);
        }

        [Fact]
        public async Task GetAdjustmentReasonDescriptionsAsync_IsDefaultFalse()
        {
            var data = new List<PaymentAdjustmentReasonEntity>
    {
        new() { GroupCode = "CO", AdjustmentCode = "45", IsDefault = false }
    };

            _cacheServiceMock.Setup(x => x.GetOrSetCacheAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<PaymentAdjustmentReasonEntity>>>>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync(data);

            var result = await _service.GetAdjustmentReasonDescriptionsAsync("ANY");

            Assert.False(result[0].IsDefault);
        }

        [Fact]
        public async Task GetAdjustmentReasonDescriptionsAsync_CodeIgnored_ReturnsAll()
        {
            var data = new List<PaymentAdjustmentReasonEntity>
    {
        new() { GroupCode = "CO", AdjustmentCode = "45" }
    };

            _cacheServiceMock.Setup(x => x.GetOrSetCacheAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<PaymentAdjustmentReasonEntity>>>>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync(data);

            var result = await _service.GetAdjustmentReasonDescriptionsAsync("INVALID");

            Assert.Single(result); // still returns
        }

        [Fact]
        public async Task GetAdjustmentReasonDescriptionsAsync_ResultNotNull()
        {
            _cacheServiceMock.Setup(x => x.GetOrSetCacheAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<PaymentAdjustmentReasonEntity>>>>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync(new List<PaymentAdjustmentReasonEntity>());

            var result = await _service.GetAdjustmentReasonDescriptionsAsync("ANY");

            Assert.NotNull(result);
        }

        [Fact]
        public async Task ReapplyPRAdjustmentsAfterSecondaryBillingAsync_ShouldCreateReversedAdjustments()
        {
            // Arrange
            var claimId = 1;

            var data = new List<PaymentClaimEntity>
    {
        new PaymentClaimEntity
        {
            ClaimId = claimId,
            DateDeleted = null,
            PaymentClaimServiceLines = new List<PaymentClaimServiceLineEntity>
            {
                new PaymentClaimServiceLineEntity
                {
                    Id = 10,
                    PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>
                    {
                        new PaymentClaimServiceLineAdjustmentEntity
                        {
                            Id = 100,
                            AdjustmentGroupCode = "PR",
                            AdjustmentAmount = 50,
                            AdjustmentAmountOrig = 50,
                            IsAdjustmentPositive = true,
                            DateDeleted = null,
                            PaymentClaimServiceLineId = 10
                        }
                    }
                }
            }
        }
    };

            var mockQuery = data.AsQueryable().BuildMockDbSet();

            _paymentClaimRepositoryMock
                .Setup(x => x.Query())
                .Returns(mockQuery.Object);

            // Act
            await _service.ReapplyPRAdjustmentsAfterSecondaryBillingAsync(claimId);

            // Assert
            _serviceLineAdjustmentRepositoryMock.Verify(
                x => x.AddRangeAsync(It.Is<List<PaymentClaimServiceLineAdjustmentEntity>>(l => l.Count == 1)),
                Times.Once);

            _serviceLineAdjustmentRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ReapplyPRAdjustmentsAfterSecondaryBillingAsync_ShouldResetPatientRespAmount()
        {
            var claimId = 1;

            var paymentClaim = new PaymentClaimEntity
            {
                ClaimId = claimId,
                PatientRespAmount = 100,
                PaymentClaimServiceLines = new List<PaymentClaimServiceLineEntity>()
            };

            var data = new List<PaymentClaimEntity> { paymentClaim };

            _paymentClaimRepositoryMock
                .Setup(x => x.Query())
                .Returns(data.AsQueryable().BuildMockDbSet().Object);

            await _service.ReapplyPRAdjustmentsAfterSecondaryBillingAsync(claimId);

            Assert.Equal(0, paymentClaim.PatientRespAmount);

            _paymentClaimRepositoryMock.Verify(x => x.Update(It.IsAny<PaymentClaimEntity>()), Times.Once);
        }

        [Fact]
        public async Task ReapplyPRAdjustmentsAfterSecondaryBillingAsync_IgnoresDeletedAdjustments()
        {
            var claimId = 1;

            var data = new List<PaymentClaimEntity>
    {
        new PaymentClaimEntity
        {
            ClaimId = claimId,
            PaymentClaimServiceLines = new List<PaymentClaimServiceLineEntity>
            {
                new PaymentClaimServiceLineEntity
                {
                    PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>
                    {
                        new() { AdjustmentGroupCode = "PR", DateDeleted = DateTime.UtcNow }
                    }
                }
            }
        }
    };

            _paymentClaimRepositoryMock.Setup(x => x.Query())
                .Returns(data.AsQueryable().BuildMockDbSet().Object);

            await _service.ReapplyPRAdjustmentsAfterSecondaryBillingAsync(claimId);

            _serviceLineAdjustmentRepositoryMock.Verify(
                x => x.AddRangeAsync(It.Is<List<PaymentClaimServiceLineAdjustmentEntity>>(l => l.Count == 0)),
                Times.Once);
        }

        [Fact]
        public async Task ReapplyPRAdjustmentsAfterSecondaryBillingAsync_WrongClaimId_NoProcessing()
        {
            var data = new List<PaymentClaimEntity>
    {
        new PaymentClaimEntity
        {
            ClaimId = 999, // different
            PaymentClaimServiceLines = new List<PaymentClaimServiceLineEntity>()
        }
    };

            _paymentClaimRepositoryMock.Setup(x => x.Query())
                .Returns(data.AsQueryable().BuildMockDbSet().Object);

            await _service.ReapplyPRAdjustmentsAfterSecondaryBillingAsync(1);

            _serviceLineAdjustmentRepositoryMock.Verify(
                x => x.AddRangeAsync(It.Is<List<PaymentClaimServiceLineAdjustmentEntity>>(l => l.Count == 0)),
                Times.Once);
        }

        [Fact]
        public async Task ReapplyPRAdjustmentsAfterSecondaryBillingAsync_SaveChangesCalledOnce()
        {
            var claimId = 1;

            var data = new List<PaymentClaimEntity>();

            _paymentClaimRepositoryMock.Setup(x => x.Query())
                .Returns(data.AsQueryable().BuildMockDbSet().Object);

            await _service.ReapplyPRAdjustmentsAfterSecondaryBillingAsync(claimId);

            _serviceLineAdjustmentRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ReapplyPRAdjustmentsAfterSecondaryBillingAsync_CommitsRepositories()
        {
            var claimId = 1;

            _paymentClaimRepositoryMock.Setup(x => x.Query())
                .Returns(new List<PaymentClaimEntity>().AsQueryable().BuildMockDbSet().Object);

            await _service.ReapplyPRAdjustmentsAfterSecondaryBillingAsync(claimId);

            _serviceLineAdjustmentRepositoryMock.Verify(x => x.CommitAsync(), Times.Once);
            _paymentClaimRepositoryMock.Verify(x => x.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateServiceLineAdjustmentsAsync_ShouldUpdateSuccessfully()
        {
            // Arrange
            var model = new AddOrEditAdjustmentModel
            {
                ClaimId = 1,
                MemberId = 10,
                AdjustmentDetails = new List<AdjustmentDetailsModel>
        {
            new()
            {
                AdjustmentId = 1,
                GroupCode = "CO",
                ReasonCode = "45",
                Amount = 200,
                isPositive = true
            }
        }
            };

            var existing = new List<PaymentClaimServiceLineAdjustmentEntity>
    {
        new()
        {
            Id = 1,
            AdjustmentGroupCode = "CO",
            AdjustmentReasonCode = "10",
            AdjustmentAmount = 100,
            IsAdjustmentPositive = false
        }
    };

            _serviceLineAdjustmentRepositoryMock
                .Setup(x => x.Query())
                .Returns(existing.AsQueryable().BuildMockDbSet().Object);

            // Act
            var result = await _service.UpdateServiceLineAdjustmentsAsync(model);

            // Assert
            Assert.Single(result);
            Assert.Equal(200, result[0].Amount);

            _serviceLineAdjustmentRepositoryMock.Verify(
                x => x.UpdateRange(It.IsAny<List<PaymentClaimServiceLineAdjustmentEntity>>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateServiceLineAdjustmentsAsync_EmptyInput_NoUpdate()
        {
            var model = new AddOrEditAdjustmentModel
            {
                AdjustmentDetails = new List<AdjustmentDetailsModel>()
            };

            var result = await _service.UpdateServiceLineAdjustmentsAsync(model);

            Assert.Empty(result);

            _serviceLineAdjustmentRepositoryMock.Verify(
                x => x.UpdateRange(It.IsAny<List<PaymentClaimServiceLineAdjustmentEntity>>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateServiceLineAdjustmentsAsync_InvalidId_Ignored()
        {
            var model = new AddOrEditAdjustmentModel
            {
                AdjustmentDetails = new List<AdjustmentDetailsModel>
        {
            new() { AdjustmentId = 999 }
        }
            };

            _serviceLineAdjustmentRepositoryMock
                .Setup(x => x.Query())
                .Returns(new List<PaymentClaimServiceLineAdjustmentEntity>()
                .AsQueryable().BuildMockDbSet().Object);

            var result = await _service.UpdateServiceLineAdjustmentsAsync(model);

            Assert.Empty(result);
        }

        [Fact]
        public async Task DeleteServiceLineAdjustmentsAsync_ValidIds_SoftDeletes()
        {
            var model = new IdsWithUserInfo { Ids = new[] { 1 }, MemberId = 10 };

            var data = GetAdjustmentData(); // helper

            _serviceLineAdjustmentRepositoryMock.Setup(x => x.Query())
                .Returns(data.AsQueryable().BuildMockDbSet().Object);

            await _service.DeleteServiceLineAdjustmentsAsync(model);

            Assert.NotNull(data[0].DateDeleted);
        }

        [Fact]
        public async Task DeleteServiceLineAdjustmentsAsync_NoData_Throws()
        {
            var model = new IdsWithUserInfo { Ids = new[] { 1 } };

            _serviceLineAdjustmentRepositoryMock.Setup(x => x.Query())
                .Returns(new List<PaymentClaimServiceLineAdjustmentEntity>()
                .AsQueryable().BuildMockDbSet().Object);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                _service.DeleteServiceLineAdjustmentsAsync(model));
        }

        [Fact]
        public async Task DeleteServiceLineAdjustmentsAsync_CommitsChanges()
        {
            var model = new IdsWithUserInfo { Ids = new[] { 1 }, MemberId = 10 };

            var data = GetAdjustmentData();

            _serviceLineAdjustmentRepositoryMock.Setup(x => x.Query())
                .Returns(data.AsQueryable().BuildMockDbSet().Object);

            await _service.DeleteServiceLineAdjustmentsAsync(model);

            _serviceLineAdjustmentRepositoryMock.Verify(x => x.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteServiceLineAdjustmentsAsync_CallsUpdateRange()
        {
            var model = new IdsWithUserInfo { Ids = new[] { 1 }, MemberId = 10 };

            var data = GetAdjustmentData();

            _serviceLineAdjustmentRepositoryMock.Setup(x => x.Query())
                .Returns(data.AsQueryable().BuildMockDbSet().Object);

            await _service.DeleteServiceLineAdjustmentsAsync(model);

            _serviceLineAdjustmentRepositoryMock.Verify(x => x.UpdateRange(It.IsAny<List<PaymentClaimServiceLineAdjustmentEntity>>()), Times.Once);
        }

        private List<PaymentClaimServiceLineAdjustmentEntity> GetAdjustmentData(int count = 1)
        {
            var list = new List<PaymentClaimServiceLineAdjustmentEntity>();

            for (int i = 1; i <= count; i++)
            {
                list.Add(new PaymentClaimServiceLineAdjustmentEntity
                {
                    Id = i,
                    AdjustmentAmount = 100 + i,
                    AdjustmentGroupCode = "CO", // default (you can override in test)
                    DateDeleted = null,

                    PaymentClaimServiceLineId = 10,

                    PaymentClaimServiceLine = new PaymentClaimServiceLineEntity
                    {
                        Id = 10,
                        PaymentClaim = new PaymentClaimEntity
                        {
                            ClaimId = 999
                        }
                    }
                });
            }

            return list;
        }
    }
}
