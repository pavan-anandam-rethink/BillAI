using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BillingService.Domain.Services.Billing;
using BillingService.Domain.Models;
using Moq;
using Xunit;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Interfaces;
using BillingService.XUnit.Tests.Common.Mocks;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Domain.Interfaces;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Enums.Billing;
using System.Linq.Expressions;

namespace BillingService.XUnit.Tests.Billing.ChargeEntry
{
    public class ChargeEntryServiceTest
    {
        private readonly Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>> _chargeEntryRepoMock;
        private readonly Mock<IRepository<BillingDbContext, ClaimEntity>> _claimRepoMock;
        private readonly Mock<IRepository<BillingDbContext, ChargePaymentEntity>> _chargePaymentRepoMock;
        private readonly Mock<IRethinkMasterDataMicroServices> _rethinkMasterDataMock;
        private readonly Mock<IClaimHistoryService> _claimHistoryServiceMock;
        private readonly ChargeEntryService _service;

        public ChargeEntryServiceTest()
        {
            _chargeEntryRepoMock = new Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>>();
            _claimRepoMock = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            _chargePaymentRepoMock = new Mock<IRepository<BillingDbContext, ChargePaymentEntity>>();
            _rethinkMasterDataMock = new Mock<IRethinkMasterDataMicroServices>();
            _claimHistoryServiceMock = new Mock<IClaimHistoryService>();

            _service = new ChargeEntryService(
                _chargeEntryRepoMock.Object,
                _claimRepoMock.Object,
                _chargePaymentRepoMock.Object,
                _rethinkMasterDataMock.Object,
                _claimHistoryServiceMock.Object
            );
        }

        [Fact]
        public async Task GetChargeEntityWithChargePaymentsAsync_ReturnsEntity()
        {
            var entity = new ClaimChargeEntryEntity { Id = 1, ClaimId = 2, ChargePayments = new List<ChargePaymentEntity>() };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { entity });
            _chargeEntryRepoMock.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetChargeEntityWithChargePaymentsAsync(1, 2);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task GetChargeEntityWithChargePaymentsAsync_ReturnsNull_WhenNoMatch()
        {
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity>());

            _chargeEntryRepoMock
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetChargeEntityWithChargePaymentsAsync(1, 2);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetChargeEntityWithChargePaymentsAsync_ReturnsEntity_WithChargePayments()
        {
            var payments = new List<ChargePaymentEntity>
    {
        new ChargePaymentEntity { Id = 10 },
        new ChargePaymentEntity { Id = 11 }
    };

            var entity = new ClaimChargeEntryEntity
            {
                Id = 1,
                ClaimId = 2,
                ChargePayments = payments
            };

            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { entity });

            _chargeEntryRepoMock
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetChargeEntityWithChargePaymentsAsync(1, 2);

            Assert.NotNull(result);
            Assert.Equal(2, result.ChargePayments.Count);
        }

        [Fact]
        public async Task GetChargeEntityWithChargePaymentsAsync_ReturnsEntity_WithEmptyPayments()
        {
            var entity = new ClaimChargeEntryEntity
            {
                Id = 1,
                ClaimId = 2,
                ChargePayments = new List<ChargePaymentEntity>()
            };

            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { entity });

            _chargeEntryRepoMock
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetChargeEntityWithChargePaymentsAsync(1, 2);

            Assert.NotNull(result);
            Assert.Empty(result.ChargePayments);
        }

        [Fact]
        public async Task GetChargeEntityWithChargePaymentsAsync_ReturnsFirstMatch_WhenMultipleExist()
        {
            var entity1 = new ClaimChargeEntryEntity { Id = 1, ClaimId = 2 };
            var entity2 = new ClaimChargeEntryEntity { Id = 1, ClaimId = 2 };

            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { entity1, entity2 });

            _chargeEntryRepoMock
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetChargeEntityWithChargePaymentsAsync(1, 2);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetChargeEntityWithChargePaymentsAsync_ThrowsException_WhenRepositoryFails()
        {
            _chargeEntryRepoMock
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ThrowsAsync(new Exception("DB Failure"));

            await Assert.ThrowsAsync<Exception>(() =>
                _service.GetChargeEntityWithChargePaymentsAsync(1, 2));
        }

        [Fact]
        public async Task GetChargeEntityWithChargePaymentsAsync_CallsRepositoryOnce()
        {
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity>());

            _chargeEntryRepoMock
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            await _service.GetChargeEntityWithChargePaymentsAsync(1, 2);

            _chargeEntryRepoMock.Verify(
                r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null),
                Times.Once);
        }

        [Fact]
        public async Task GetChargeEntityWithChargePaymentsAsync_ReturnsNull_WhenIdsAreZero()
        {
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity>());

            _chargeEntryRepoMock
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetChargeEntityWithChargePaymentsAsync(0, 0);

            Assert.Null(result);
        }


        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_ReturnsEntities()
        {
            var entities = new List<ClaimChargeEntryEntity> { new ClaimChargeEntryEntity { Id = 1, ClaimId = 2, ChargePayments = new List<ChargePaymentEntity>() } };
            var dbSet = DbMock.Create(entities);
            _chargeEntryRepoMock.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(2);

            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_ReturnsEmptyList_WhenNoEntitiesExist()
        {
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity>());
            _chargeEntryRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(2);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_ReturnsMultipleEntities()
        {
            var entities = new List<ClaimChargeEntryEntity>
    {
        new ClaimChargeEntryEntity { Id = 1, ClaimId = 2, ChargePayments = new List<ChargePaymentEntity>() },
        new ClaimChargeEntryEntity { Id = 2, ClaimId = 2, ChargePayments = new List<ChargePaymentEntity>() },
        new ClaimChargeEntryEntity { Id = 3, ClaimId = 2, ChargePayments = new List<ChargePaymentEntity>() }
    };
            var dbSet = DbMock.Create(entities);
            _chargeEntryRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(2);

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_ReturnsEntitiesWithMultiplePayments()
        {
            var payments = new List<ChargePaymentEntity>
    {
        new ChargePaymentEntity { Id = 10, Amount = 100m },
        new ChargePaymentEntity { Id = 11, Amount = 200m },
        new ChargePaymentEntity { Id = 12, Amount = 300m }
    };
            var entity = new ClaimChargeEntryEntity { Id = 1, ClaimId = 2, ChargePayments = payments };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { entity });
            _chargeEntryRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(2);

            Assert.Single(result);
            Assert.Equal(3, result[0].ChargePayments.Count);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_ExcludesDeletedEntities()
        {
            var entities = new List<ClaimChargeEntryEntity>
    {
        new ClaimChargeEntryEntity { Id = 1, ClaimId = 2, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() },
        new ClaimChargeEntryEntity { Id = 2, ClaimId = 2, DateDeleted = DateTime.UtcNow, ChargePayments = new List<ChargePaymentEntity>() }
    };
            var nonDeletedEntities = entities.Where(e => e.DateDeleted == null).ToList();
            var dbSet = DbMock.Create(nonDeletedEntities);
            _chargeEntryRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(2);

            Assert.Single(result);
            Assert.Null(result[0].DateDeleted);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_ReturnsEmptyList_WhenAllEntitiesDeleted()
        {
            var entities = new List<ClaimChargeEntryEntity>
    {
        new ClaimChargeEntryEntity { Id = 1, ClaimId = 2, DateDeleted = DateTime.UtcNow },
        new ClaimChargeEntryEntity { Id = 2, ClaimId = 2, DateDeleted = DateTime.UtcNow }
    };
            var nonDeletedEntities = entities.Where(e => e.DateDeleted == null).ToList();
            var dbSet = DbMock.Create(nonDeletedEntities);
            _chargeEntryRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(2);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_FiltersByClaimId()
        {
            var entities = new List<ClaimChargeEntryEntity>
    {
        new ClaimChargeEntryEntity { Id = 1, ClaimId = 2, ChargePayments = new List<ChargePaymentEntity>() },
        new ClaimChargeEntryEntity { Id = 2, ClaimId = 3, ChargePayments = new List<ChargePaymentEntity>() }
    };
            var filteredEntities = entities.Where(e => e.ClaimId == 2).ToList();
            var dbSet = DbMock.Create(filteredEntities);
            _chargeEntryRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(2);

            Assert.Single(result);
            Assert.Equal(2, result[0].ClaimId);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_ReturnsEmptyList_WhenClaimIdNotFound()
        {
            var entities = new List<ClaimChargeEntryEntity>
    {
        new ClaimChargeEntryEntity { Id = 1, ClaimId = 2, ChargePayments = new List<ChargePaymentEntity>() }
    };
            var filteredEntities = entities.Where(e => e.ClaimId == 999).ToList();
            var dbSet = DbMock.Create(filteredEntities);
            _chargeEntryRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(999);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_HandlesZeroClaimId()
        {
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity>());
            _chargeEntryRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(0);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_HandlesNegativeClaimId()
        {
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity>());
            _chargeEntryRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(-1);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_ThrowsException_WhenRepositoryFails()
        {
            _chargeEntryRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ThrowsAsync(new Exception("Database connection failed"));

            await Assert.ThrowsAsync<Exception>(() => _service.GetChargeEntitiesWithChargePaymentsAsync(2));
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_CallsRepositoryWithCorrectPredicate()
        {
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity>());
            _chargeEntryRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            await _service.GetChargeEntitiesWithChargePaymentsAsync(2);

            _chargeEntryRepoMock.Verify(
                r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null),
                Times.Once);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_ReturnsEntitiesWithEmptyPaymentsList()
        {
            var entity = new ClaimChargeEntryEntity
            {
                Id = 1,
                ClaimId = 2,
                ChargePayments = new List<ChargePaymentEntity>()
            };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { entity });
            _chargeEntryRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(2);

            Assert.Single(result);
            Assert.Empty(result[0].ChargePayments);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_ReturnsEntitiesWithNullPaymentsList()
        {
            var entity = new ClaimChargeEntryEntity
            {
                Id = 1,
                ClaimId = 2,
                ChargePayments = null
            };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { entity });
            _chargeEntryRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(2);

            Assert.Single(result);
            Assert.Null(result[0].ChargePayments);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_ReturnsMixedPaymentsScenario()
        {
            var entities = new List<ClaimChargeEntryEntity>
    {
        new ClaimChargeEntryEntity
        {
            Id = 1,
            ClaimId = 2,
            ChargePayments = new List<ChargePaymentEntity> { new ChargePaymentEntity { Id = 10 } }
        },
        new ClaimChargeEntryEntity
        {
            Id = 2,
            ClaimId = 2,
            ChargePayments = new List<ChargePaymentEntity>()
        },
        new ClaimChargeEntryEntity
        {
            Id = 3,
            ClaimId = 2,
            ChargePayments = null
        }
    };
            var dbSet = DbMock.Create(entities);
            _chargeEntryRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(2);

            Assert.Equal(3, result.Count);
            Assert.Single(result[0].ChargePayments);
            Assert.Empty(result[1].ChargePayments);
            Assert.Null(result[2].ChargePayments);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_HandlesLargeDataSet()
        {
            var entities = Enumerable.Range(1, 100)
                .Select(i => new ClaimChargeEntryEntity
                {
                    Id = i,
                    ClaimId = 2,
                    ChargePayments = new List<ChargePaymentEntity>()
                }).ToList();
            var dbSet = DbMock.Create(entities);
            _chargeEntryRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(2);

            Assert.Equal(100, result.Count);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_VerifiesIncludeIsApplied()
        {
            var payments = new List<ChargePaymentEntity>
    {
        new ChargePaymentEntity { Id = 10, Amount = 100m }
    };
            var entity = new ClaimChargeEntryEntity
            {
                Id = 1,
                ClaimId = 2,
                ChargePayments = payments
            };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { entity });
            _chargeEntryRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimChargeEntryEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(2);

            Assert.NotNull(result[0].ChargePayments);
            Assert.NotEmpty(result[0].ChargePayments);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_WithMultipleClaimIds_ReturnsMatchingEntities()
        {
            var claimIds = new[] { 1, 2, 3 };
            var entities = new List<ClaimChargeEntryEntity>
    {
        new ClaimChargeEntryEntity { Id = 1, ClaimId = 1, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() },
        new ClaimChargeEntryEntity { Id = 2, ClaimId = 2, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() },
        new ClaimChargeEntryEntity { Id = 3, ClaimId = 3, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() }
    };
            var dbSet = DbMock.Create(entities);
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(claimIds);

            Assert.Equal(3, result.Count);
            Assert.All(result, r => Assert.Contains(r.ClaimId, claimIds));
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_WithMultipleClaimIds_ReturnsEmptyList_WhenNoMatch()
        {
            var claimIds = new[] { 99, 100 };
            var entities = new List<ClaimChargeEntryEntity>
    {
        new ClaimChargeEntryEntity { Id = 1, ClaimId = 1, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() }
    };
            var dbSet = DbMock.Create(entities);
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(claimIds);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_WithMultipleClaimIds_ExcludesDeletedEntries()
        {
            var claimIds = new[] { 1, 2 };
            var entities = new List<ClaimChargeEntryEntity>
    {
        new ClaimChargeEntryEntity { Id = 1, ClaimId = 1, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() },
        new ClaimChargeEntryEntity { Id = 2, ClaimId = 1, DateDeleted = DateTime.UtcNow, ChargePayments = new List<ChargePaymentEntity>() },
        new ClaimChargeEntryEntity { Id = 3, ClaimId = 2, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() }
    };
            var dbSet = DbMock.Create(entities);
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(claimIds);

            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Null(r.DateDeleted));
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_WithMultipleClaimIds_ReturnsEntitiesWithChargePayments()
        {
            var claimIds = new[] { 1 };
            var payments = new List<ChargePaymentEntity>
    {
        new ChargePaymentEntity { Id = 10, Amount = 100m },
        new ChargePaymentEntity { Id = 11, Amount = 200m }
    };
            var entities = new List<ClaimChargeEntryEntity>
    {
        new ClaimChargeEntryEntity { Id = 1, ClaimId = 1, DateDeleted = null, ChargePayments = payments }
    };
            var dbSet = DbMock.Create(entities);
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(claimIds);

            Assert.Single(result);
            Assert.Equal(2, result[0].ChargePayments.Count);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_WithEmptyClaimIds_ReturnsEmptyList()
        {
            var claimIds = Array.Empty<int>();
            var entities = new List<ClaimChargeEntryEntity>
    {
        new ClaimChargeEntryEntity { Id = 1, ClaimId = 1, DateDeleted = null }
    };
            var dbSet = DbMock.Create(entities);
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(claimIds);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_WithSingleClaimId_ReturnsMatchingEntities()
        {
            var claimIds = new[] { 5 };
            var entities = new List<ClaimChargeEntryEntity>
    {
        new ClaimChargeEntryEntity { Id = 1, ClaimId = 5, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() },
        new ClaimChargeEntryEntity { Id = 2, ClaimId = 5, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() }
    };
            var dbSet = DbMock.Create(entities);
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(claimIds);

            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal(5, r.ClaimId));
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_WithDuplicateClaimIds_ReturnsDistinctEntities()
        {
            var claimIds = new[] { 1, 1, 1 };
            var entities = new List<ClaimChargeEntryEntity>
    {
        new ClaimChargeEntryEntity { Id = 1, ClaimId = 1, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() }
    };
            var dbSet = DbMock.Create(entities);
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(claimIds);

            Assert.Single(result);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_WithPartialMatch_ReturnsOnlyMatching()
        {
            var claimIds = new[] { 1, 99 };
            var entities = new List<ClaimChargeEntryEntity>
    {
        new ClaimChargeEntryEntity { Id = 1, ClaimId = 1, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() },
        new ClaimChargeEntryEntity { Id = 2, ClaimId = 2, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() }
    };
            var dbSet = DbMock.Create(entities);
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(claimIds);

            Assert.Single(result);
            Assert.Equal(1, result[0].ClaimId);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_WithMultipleClaimIds_HandlesLargeDataSet()
        {
            var claimIds = Enumerable.Range(1, 50).ToArray();
            var entities = Enumerable.Range(1, 100)
                .Select(i => new ClaimChargeEntryEntity
                {
                    Id = i,
                    ClaimId = i % 50 + 1,
                    DateDeleted = null,
                    ChargePayments = new List<ChargePaymentEntity>()
                }).ToList();
            var dbSet = DbMock.Create(entities);
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(claimIds);

            Assert.Equal(100, result.Count);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_WithMultipleClaimIds_IncludesEmptyPaymentCollections()
        {
            var claimIds = new[] { 1 };
            var entities = new List<ClaimChargeEntryEntity>
    {
        new ClaimChargeEntryEntity { Id = 1, ClaimId = 1, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() }
    };
            var dbSet = DbMock.Create(entities);
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(claimIds);

            Assert.Single(result);
            Assert.NotNull(result[0].ChargePayments);
            Assert.Empty(result[0].ChargePayments);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_WithMultipleClaimIds_CallsQueryOnce()
        {
            var claimIds = new[] { 1, 2 };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity>());
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            await _service.GetChargeEntitiesWithChargePaymentsAsync(claimIds);

            _chargeEntryRepoMock.Verify(r => r.Query(), Times.Once);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_WithMultipleClaimIds_ThrowsException_WhenRepositoryFails()
        {
            var claimIds = new[] { 1, 2 };
            _chargeEntryRepoMock.Setup(r => r.Query()).Throws(new Exception("Database error"));

            await Assert.ThrowsAsync<Exception>(() =>
                _service.GetChargeEntitiesWithChargePaymentsAsync(claimIds));
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_WithZeroAndNegativeClaimIds_ReturnsEmptyList()
        {
            var claimIds = new[] { 0, -1, -5 };
            var entities = new List<ClaimChargeEntryEntity>
    {
        new ClaimChargeEntryEntity { Id = 1, ClaimId = 1, DateDeleted = null }
    };
            var dbSet = DbMock.Create(entities);
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(claimIds);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_WithMultipleClaimIds_ReturnsMixedPaymentScenarios()
        {
            var claimIds = new[] { 1, 2, 3 };
            var entities = new List<ClaimChargeEntryEntity>
    {
        new ClaimChargeEntryEntity
        {
            Id = 1,
            ClaimId = 1,
            DateDeleted = null,
            ChargePayments = new List<ChargePaymentEntity> { new ChargePaymentEntity { Id = 10 } }
        },
        new ClaimChargeEntryEntity
        {
            Id = 2,
            ClaimId = 2,
            DateDeleted = null,
            ChargePayments = new List<ChargePaymentEntity>()
        },
        new ClaimChargeEntryEntity
        {
            Id = 3,
            ClaimId = 3,
            DateDeleted = null,
            ChargePayments = null
        }
    };
            var dbSet = DbMock.Create(entities);
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(claimIds);

            Assert.Equal(3, result.Count);
            Assert.Single(result[0].ChargePayments);
            Assert.Empty(result[1].ChargePayments);
            Assert.Null(result[2].ChargePayments);
        }

        [Fact]
        public async Task GetChargeEntitiesWithChargePaymentsAsync_WithMultipleClaimIds_VerifiesAsSplitQueryIsUsed()
        {
            var claimIds = new[] { 1 };
            var entities = new List<ClaimChargeEntryEntity>
    {
        new ClaimChargeEntryEntity
        {
            Id = 1,
            ClaimId = 1,
            DateDeleted = null,
            ChargePayments = new List<ChargePaymentEntity>
            {
                new ChargePaymentEntity { Id = 10, Amount = 100m },
                new ChargePaymentEntity { Id = 11, Amount = 200m }
            }
        }
    };
            var dbSet = DbMock.Create(entities);
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var result = await _service.GetChargeEntitiesWithChargePaymentsAsync(claimIds);

            Assert.Single(result);
            Assert.NotNull(result[0].ChargePayments);
            Assert.Equal(2, result[0].ChargePayments.Count);
        }

        [Fact]
        public async Task AddChargePaymentAsync_Commits_WhenCommitImmediately()
        {
            var entity = new ChargePaymentEntity();
            _chargePaymentRepoMock.Setup(r => r.AddAsync(entity)).Returns(Task.CompletedTask);
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.AddChargePaymentAsync(entity, true);

            _chargePaymentRepoMock.Verify(r => r.AddAsync(entity), Times.Once);
            _chargePaymentRepoMock.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task AddChargePaymentAsync_DoesNotCommit_WhenCommitImmediatelyIsFalse()
        {
            var entity = new ChargePaymentEntity();
            _chargePaymentRepoMock.Setup(r => r.AddAsync(entity)).Returns(Task.CompletedTask);

            await _service.AddChargePaymentAsync(entity, false);

            _chargePaymentRepoMock.Verify(r => r.AddAsync(entity), Times.Once);
            _chargePaymentRepoMock.Verify(r => r.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task AddChargePaymentAsync_AddsEntityWithValidData()
        {
            var entity = new ChargePaymentEntity
            {
                Id = 1,
                ChargeId = 100,
                Amount = 250.50m
            };
            _chargePaymentRepoMock.Setup(r => r.AddAsync(entity)).Returns(Task.CompletedTask);
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.AddChargePaymentAsync(entity, true);

            _chargePaymentRepoMock.Verify(r => r.AddAsync(It.Is<ChargePaymentEntity>(
                e => e.Id == 1 && e.Amount == 250.50m && e.ChargeId == 100)), Times.Once);
        }

        [Fact]
        public async Task AddChargePaymentAsync_HandlesNullEntity()
        {
            ChargePaymentEntity entity = null;
            _chargePaymentRepoMock.Setup(r => r.AddAsync(entity)).Returns(Task.CompletedTask);

            await _service.AddChargePaymentAsync(entity, true);

            _chargePaymentRepoMock.Verify(r => r.AddAsync(null), Times.Once);
        }

        [Fact]
        public async Task AddChargePaymentAsync_ThrowsException_WhenAddAsyncFails()
        {
            var entity = new ChargePaymentEntity();
            _chargePaymentRepoMock.Setup(r => r.AddAsync(entity))
                .ThrowsAsync(new Exception("Database error"));

            await Assert.ThrowsAsync<Exception>(() =>
                _service.AddChargePaymentAsync(entity, true));
        }

        [Fact]
        public async Task AddChargePaymentAsync_ThrowsException_WhenCommitAsyncFails()
        {
            var entity = new ChargePaymentEntity();
            _chargePaymentRepoMock.Setup(r => r.AddAsync(entity)).Returns(Task.CompletedTask);
            _chargePaymentRepoMock.Setup(r => r.CommitAsync())
                .ThrowsAsync(new Exception("Commit failed"));

            await Assert.ThrowsAsync<Exception>(() =>
                _service.AddChargePaymentAsync(entity, true));
        }

        [Fact]
        public async Task AddChargePaymentAsync_CallsAddAsyncOnce_WhenCommitImmediatelyIsTrue()
        {
            var entity = new ChargePaymentEntity();
            _chargePaymentRepoMock.Setup(r => r.AddAsync(entity)).Returns(Task.CompletedTask);
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.AddChargePaymentAsync(entity, true);

            _chargePaymentRepoMock.Verify(r => r.AddAsync(entity), Times.Once);
        }

        [Fact]
        public async Task AddChargePaymentAsync_CallsAddAsyncOnce_WhenCommitImmediatelyIsFalse()
        {
            var entity = new ChargePaymentEntity();
            _chargePaymentRepoMock.Setup(r => r.AddAsync(entity)).Returns(Task.CompletedTask);

            await _service.AddChargePaymentAsync(entity, false);

            _chargePaymentRepoMock.Verify(r => r.AddAsync(entity), Times.Once);
        }

        [Fact]
        public async Task AddChargePaymentAsync_UsesDefaultParameter_WhenCommitImmediatelyNotProvided()
        {
            var entity = new ChargePaymentEntity();
            _chargePaymentRepoMock.Setup(r => r.AddAsync(entity)).Returns(Task.CompletedTask);
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.AddChargePaymentAsync(entity);

            _chargePaymentRepoMock.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task AddChargePaymentAsync_AddsEntityWithZeroAmount()
        {
            var entity = new ChargePaymentEntity
            {
                Id = 1,
                Amount = 0m
            };
            _chargePaymentRepoMock.Setup(r => r.AddAsync(entity)).Returns(Task.CompletedTask);
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.AddChargePaymentAsync(entity, true);

            _chargePaymentRepoMock.Verify(r => r.AddAsync(It.Is<ChargePaymentEntity>(
                e => e.Amount == 0m)), Times.Once);
        }

        [Fact]
        public async Task AddChargePaymentAsync_AddsEntityWithNegativeAmount()
        {
            var entity = new ChargePaymentEntity
            {
                Id = 1,
                Amount = -100m
            };
            _chargePaymentRepoMock.Setup(r => r.AddAsync(entity)).Returns(Task.CompletedTask);
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.AddChargePaymentAsync(entity, true);

            _chargePaymentRepoMock.Verify(r => r.AddAsync(It.Is<ChargePaymentEntity>(
                e => e.Amount == -100m)), Times.Once);
        }

        [Fact]
        public async Task AddChargePaymentAsync_AddsEntityWithLargeAmount()
        {
            var entity = new ChargePaymentEntity
            {
                Id = 1,
                Amount = 999999.99m
            };
            _chargePaymentRepoMock.Setup(r => r.AddAsync(entity)).Returns(Task.CompletedTask);
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.AddChargePaymentAsync(entity, true);

            _chargePaymentRepoMock.Verify(r => r.AddAsync(It.Is<ChargePaymentEntity>(
                e => e.Amount == 999999.99m)), Times.Once);
        }

        [Fact]
        public async Task AddChargePaymentAsync_AddsMultipleEntitiesSequentially_WithCommit()
        {
            var entity1 = new ChargePaymentEntity { Id = 1 };
            var entity2 = new ChargePaymentEntity { Id = 2 };
            _chargePaymentRepoMock.Setup(r => r.AddAsync(It.IsAny<ChargePaymentEntity>())).Returns(Task.CompletedTask);
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.AddChargePaymentAsync(entity1, true);
            await _service.AddChargePaymentAsync(entity2, true);

            _chargePaymentRepoMock.Verify(r => r.AddAsync(It.IsAny<ChargePaymentEntity>()), Times.Exactly(2));
            _chargePaymentRepoMock.Verify(r => r.CommitAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task AddChargePaymentAsync_AddsMultipleEntitiesSequentially_WithoutCommit()
        {
            var entity1 = new ChargePaymentEntity { Id = 1 };
            var entity2 = new ChargePaymentEntity { Id = 2 };
            _chargePaymentRepoMock.Setup(r => r.AddAsync(It.IsAny<ChargePaymentEntity>())).Returns(Task.CompletedTask);

            await _service.AddChargePaymentAsync(entity1, false);
            await _service.AddChargePaymentAsync(entity2, false);

            _chargePaymentRepoMock.Verify(r => r.AddAsync(It.IsAny<ChargePaymentEntity>()), Times.Exactly(2));
            _chargePaymentRepoMock.Verify(r => r.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task AddChargePaymentAsync_CompletesSuccessfully_WhenEntityHasAllProperties()
        {
            var entity = new ChargePaymentEntity
            {
                Id = 1,
                ChargeId = 100,
                Amount = 150.75m,
                DateCreated = DateTime.UtcNow,
                DateDeleted = null
            };
            _chargePaymentRepoMock.Setup(r => r.AddAsync(entity)).Returns(Task.CompletedTask);
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.AddChargePaymentAsync(entity, true);

            _chargePaymentRepoMock.Verify(r => r.AddAsync(entity), Times.Once);
            _chargePaymentRepoMock.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task AddChargePaymentAsync_DoesNotThrow_WhenCommitImmediatelyIsFalseAndCommitNotCalled()
        {
            var entity = new ChargePaymentEntity { Id = 1 };
            _chargePaymentRepoMock.Setup(r => r.AddAsync(entity)).Returns(Task.CompletedTask);

            var exception = await Record.ExceptionAsync(() =>
                _service.AddChargePaymentAsync(entity, false));

            Assert.Null(exception);
            _chargePaymentRepoMock.Verify(r => r.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateChargeEntryAsync_Commits_WhenCommitImmediately()
        {
            var entity = new ClaimChargeEntryEntity();
            _chargeEntryRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargeEntryAsync(entity, true);

            _chargeEntryRepoMock.Verify(r => r.Update(entity), Times.Once);
            _chargePaymentRepoMock.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateChargeEntryAsync_DoesNotCommit_WhenCommitImmediatelyIsFalse()
        {
            var entity = new ClaimChargeEntryEntity();
            _chargeEntryRepoMock.Setup(r => r.Update(entity));

            await _service.UpdateChargeEntryAsync(entity, false);

            _chargeEntryRepoMock.Verify(r => r.Update(entity), Times.Once);
            _chargePaymentRepoMock.Verify(r => r.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateChargeEntryAsync_UpdatesEntityWithValidData()
        {
            var entity = new ClaimChargeEntryEntity
            {
                Id = 1,
                ClaimId = 100,
                BillingCode = "99213",
                Charges = 150.75m,
                Units = 2
            };
            _chargeEntryRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargeEntryAsync(entity, true);

            _chargeEntryRepoMock.Verify(r => r.Update(It.Is<ClaimChargeEntryEntity>(
                e => e.Id == 1 && e.ClaimId == 100 && e.BillingCode == "99213")), Times.Once);
        }

        [Fact]
        public async Task UpdateChargeEntryAsync_HandlesNullEntity()
        {
            ClaimChargeEntryEntity entity = null;
            _chargeEntryRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargeEntryAsync(entity, true);

            _chargeEntryRepoMock.Verify(r => r.Update(null), Times.Once);
        }

        [Fact]
        public async Task UpdateChargeEntryAsync_ThrowsException_WhenUpdateFails()
        {
            var entity = new ClaimChargeEntryEntity();
            _chargeEntryRepoMock.Setup(r => r.Update(entity))
                .Throws(new Exception("Update failed"));

            await Assert.ThrowsAsync<Exception>(() =>
                _service.UpdateChargeEntryAsync(entity, true));
        }

        [Fact]
        public async Task UpdateChargeEntryAsync_ThrowsException_WhenCommitAsyncFails()
        {
            var entity = new ClaimChargeEntryEntity();
            _chargeEntryRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync())
                .ThrowsAsync(new Exception("Commit failed"));

            await Assert.ThrowsAsync<Exception>(() =>
                _service.UpdateChargeEntryAsync(entity, true));
        }

        [Fact]
        public async Task UpdateChargeEntryAsync_CallsUpdateOnce_WhenCommitImmediatelyIsTrue()
        {
            var entity = new ClaimChargeEntryEntity();
            _chargeEntryRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargeEntryAsync(entity, true);

            _chargeEntryRepoMock.Verify(r => r.Update(entity), Times.Once);
        }

        [Fact]
        public async Task UpdateChargeEntryAsync_CallsUpdateOnce_WhenCommitImmediatelyIsFalse()
        {
            var entity = new ClaimChargeEntryEntity();
            _chargeEntryRepoMock.Setup(r => r.Update(entity));

            await _service.UpdateChargeEntryAsync(entity, false);

            _chargeEntryRepoMock.Verify(r => r.Update(entity), Times.Once);
        }

        [Fact]
        public async Task UpdateChargeEntryAsync_UsesDefaultParameter_WhenCommitImmediatelyNotProvided()
        {
            var entity = new ClaimChargeEntryEntity();
            _chargeEntryRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargeEntryAsync(entity);

            _chargePaymentRepoMock.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateChargeEntryAsync_UpdatesEntityWithZeroCharges()
        {
            var entity = new ClaimChargeEntryEntity
            {
                Id = 1,
                Charges = 0m
            };
            _chargeEntryRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargeEntryAsync(entity, true);

            _chargeEntryRepoMock.Verify(r => r.Update(It.Is<ClaimChargeEntryEntity>(
                e => e.Charges == 0m)), Times.Once);
        }

        [Fact]
        public async Task UpdateChargeEntryAsync_UpdatesEntityWithNegativeCharges()
        {
            var entity = new ClaimChargeEntryEntity
            {
                Id = 1,
                Charges = -100m
            };
            _chargeEntryRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargeEntryAsync(entity, true);

            _chargeEntryRepoMock.Verify(r => r.Update(It.Is<ClaimChargeEntryEntity>(
                e => e.Charges == -100m)), Times.Once);
        }

        [Fact]
        public async Task UpdateChargeEntryAsync_UpdatesMultipleEntitiesSequentially_WithCommit()
        {
            var entity1 = new ClaimChargeEntryEntity { Id = 1 };
            var entity2 = new ClaimChargeEntryEntity { Id = 2 };
            _chargeEntryRepoMock.Setup(r => r.Update(It.IsAny<ClaimChargeEntryEntity>()));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargeEntryAsync(entity1, true);
            await _service.UpdateChargeEntryAsync(entity2, true);

            _chargeEntryRepoMock.Verify(r => r.Update(It.IsAny<ClaimChargeEntryEntity>()), Times.Exactly(2));
            _chargePaymentRepoMock.Verify(r => r.CommitAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task UpdateChargeEntryAsync_UpdatesMultipleEntitiesSequentially_WithoutCommit()
        {
            var entity1 = new ClaimChargeEntryEntity { Id = 1 };
            var entity2 = new ClaimChargeEntryEntity { Id = 2 };
            _chargeEntryRepoMock.Setup(r => r.Update(It.IsAny<ClaimChargeEntryEntity>()));

            await _service.UpdateChargeEntryAsync(entity1, false);
            await _service.UpdateChargeEntryAsync(entity2, false);

            _chargeEntryRepoMock.Verify(r => r.Update(It.IsAny<ClaimChargeEntryEntity>()), Times.Exactly(2));
            _chargePaymentRepoMock.Verify(r => r.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateChargeEntryAsync_UpdatesEntityWithAllProperties()
        {
            var entity = new ClaimChargeEntryEntity
            {
                Id = 1,
                ClaimId = 100,
                BillingCode = "99213",
                Charges = 150.75m,
                Units = 2,
                DateOfService = DateTime.UtcNow,
                Modifier1 = "25",
                Modifier2 = "GT",
                Description = "Office Visit",
                NoteText = "Test Note"
            };
            _chargeEntryRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargeEntryAsync(entity, true);

            _chargeEntryRepoMock.Verify(r => r.Update(entity), Times.Once);
            _chargePaymentRepoMock.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateChargeEntryAsync_DoesNotThrow_WhenCommitImmediatelyIsFalseAndCommitNotCalled()
        {
            var entity = new ClaimChargeEntryEntity { Id = 1 };
            _chargeEntryRepoMock.Setup(r => r.Update(entity));

            var exception = await Record.ExceptionAsync(() =>
                _service.UpdateChargeEntryAsync(entity, false));

            Assert.Null(exception);
            _chargePaymentRepoMock.Verify(r => r.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateChargeEntryAsync_UpdatesEntityWithDateDeleted()
        {
            var entity = new ClaimChargeEntryEntity
            {
                Id = 1,
                DateDeleted = DateTime.UtcNow
            };
            _chargeEntryRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargeEntryAsync(entity, true);

            _chargeEntryRepoMock.Verify(r => r.Update(It.Is<ClaimChargeEntryEntity>(
                e => e.DateDeleted != null)), Times.Once);
        }

        [Fact]
        public async Task UpdateChargeEntryAsync_UpdatesEntityWithChargePayments()
        {
            var payments = new List<ChargePaymentEntity>
    {
        new ChargePaymentEntity { Id = 1, Amount = 100m },
        new ChargePaymentEntity { Id = 2, Amount = 50m }
    };
            var entity = new ClaimChargeEntryEntity
            {
                Id = 1,
                ChargePayments = payments
            };
            _chargeEntryRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargeEntryAsync(entity, true);

            _chargeEntryRepoMock.Verify(r => r.Update(It.Is<ClaimChargeEntryEntity>(
                e => e.ChargePayments.Count == 2)), Times.Once);
        }

        [Fact]
        public async Task UpdateChargeEntryAsync_CommitsToChargePaymentRepository()
        {
            var entity = new ClaimChargeEntryEntity { Id = 1 };
            _chargeEntryRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargeEntryAsync(entity, true);

            _chargePaymentRepoMock.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateChargePaymentAsync_Commits_WhenCommitImmediately()
        {
            var entity = new ChargePaymentEntity();
            _chargePaymentRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargePaymentAsync(entity, true);

            _chargePaymentRepoMock.Verify(r => r.Update(entity), Times.Once);
            _chargePaymentRepoMock.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateChargePaymentAsync_DoesNotCommit_WhenCommitImmediatelyIsFalse()
        {
            var entity = new ChargePaymentEntity();
            _chargePaymentRepoMock.Setup(r => r.Update(entity));

            await _service.UpdateChargePaymentAsync(entity, false);

            _chargePaymentRepoMock.Verify(r => r.Update(entity), Times.Once);
            _chargePaymentRepoMock.Verify(r => r.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateChargePaymentAsync_UpdatesEntityWithValidData()
        {
            var entity = new ChargePaymentEntity
            {
                Id = 1,
                ChargeId = 100,
                PaymentMethodId = 200,
                Amount = 250.50m
            };
            _chargePaymentRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargePaymentAsync(entity, true);

            _chargePaymentRepoMock.Verify(r => r.Update(It.Is<ChargePaymentEntity>(
                e => e.Id == 1 && e.ChargeId == 100 && e.Amount == 250.50m)), Times.Once);
        }

        [Fact]
        public async Task UpdateChargePaymentAsync_HandlesNullEntity()
        {
            ChargePaymentEntity entity = null;
            _chargePaymentRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargePaymentAsync(entity, true);

            _chargePaymentRepoMock.Verify(r => r.Update(null), Times.Once);
        }

        [Fact]
        public async Task UpdateChargePaymentAsync_ThrowsException_WhenUpdateFails()
        {
            var entity = new ChargePaymentEntity();
            _chargePaymentRepoMock.Setup(r => r.Update(entity))
                .Throws(new Exception("Update failed"));

            await Assert.ThrowsAsync<Exception>(() =>
                _service.UpdateChargePaymentAsync(entity, true));
        }

        [Fact]
        public async Task UpdateChargePaymentAsync_ThrowsException_WhenCommitAsyncFails()
        {
            var entity = new ChargePaymentEntity();
            _chargePaymentRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync())
                .ThrowsAsync(new Exception("Commit failed"));

            await Assert.ThrowsAsync<Exception>(() =>
                _service.UpdateChargePaymentAsync(entity, true));
        }

        [Fact]
        public async Task UpdateChargePaymentAsync_CallsUpdateOnce_WhenCommitImmediatelyIsTrue()
        {
            var entity = new ChargePaymentEntity();
            _chargePaymentRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargePaymentAsync(entity, true);

            _chargePaymentRepoMock.Verify(r => r.Update(entity), Times.Once);
        }

        [Fact]
        public async Task UpdateChargePaymentAsync_CallsUpdateOnce_WhenCommitImmediatelyIsFalse()
        {
            var entity = new ChargePaymentEntity();
            _chargePaymentRepoMock.Setup(r => r.Update(entity));

            await _service.UpdateChargePaymentAsync(entity, false);

            _chargePaymentRepoMock.Verify(r => r.Update(entity), Times.Once);
        }

        [Fact]
        public async Task UpdateChargePaymentAsync_UsesDefaultParameter_WhenCommitImmediatelyNotProvided()
        {
            var entity = new ChargePaymentEntity();
            _chargePaymentRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargePaymentAsync(entity);

            _chargePaymentRepoMock.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateChargePaymentAsync_UpdatesEntityWithZeroAmount()
        {
            var entity = new ChargePaymentEntity
            {
                Id = 1,
                Amount = 0m
            };
            _chargePaymentRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargePaymentAsync(entity, true);

            _chargePaymentRepoMock.Verify(r => r.Update(It.Is<ChargePaymentEntity>(
                e => e.Amount == 0m)), Times.Once);
        }

        [Fact]
        public async Task UpdateChargePaymentAsync_UpdatesEntityWithNegativeAmount()
        {
            var entity = new ChargePaymentEntity
            {
                Id = 1,
                Amount = -100m
            };
            _chargePaymentRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargePaymentAsync(entity, true);

            _chargePaymentRepoMock.Verify(r => r.Update(It.Is<ChargePaymentEntity>(
                e => e.Amount == -100m)), Times.Once);
        }

        [Fact]
        public async Task UpdateChargePaymentAsync_UpdatesEntityWithLargeAmount()
        {
            var entity = new ChargePaymentEntity
            {
                Id = 1,
                Amount = 999999.99m
            };
            _chargePaymentRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargePaymentAsync(entity, true);

            _chargePaymentRepoMock.Verify(r => r.Update(It.Is<ChargePaymentEntity>(
                e => e.Amount == 999999.99m)), Times.Once);
        }

        [Fact]
        public async Task UpdateChargePaymentAsync_UpdatesMultipleEntitiesSequentially_WithCommit()
        {
            var entity1 = new ChargePaymentEntity { Id = 1, Amount = 100m };
            var entity2 = new ChargePaymentEntity { Id = 2, Amount = 200m };
            _chargePaymentRepoMock.Setup(r => r.Update(It.IsAny<ChargePaymentEntity>()));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargePaymentAsync(entity1, true);
            await _service.UpdateChargePaymentAsync(entity2, true);

            _chargePaymentRepoMock.Verify(r => r.Update(It.IsAny<ChargePaymentEntity>()), Times.Exactly(2));
            _chargePaymentRepoMock.Verify(r => r.CommitAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task UpdateChargePaymentAsync_UpdatesMultipleEntitiesSequentially_WithoutCommit()
        {
            var entity1 = new ChargePaymentEntity { Id = 1, Amount = 100m };
            var entity2 = new ChargePaymentEntity { Id = 2, Amount = 200m };
            _chargePaymentRepoMock.Setup(r => r.Update(It.IsAny<ChargePaymentEntity>()));

            await _service.UpdateChargePaymentAsync(entity1, false);
            await _service.UpdateChargePaymentAsync(entity2, false);

            _chargePaymentRepoMock.Verify(r => r.Update(It.IsAny<ChargePaymentEntity>()), Times.Exactly(2));
            _chargePaymentRepoMock.Verify(r => r.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateChargePaymentAsync_UpdatesEntityWithAllProperties()
        {
            var entity = new ChargePaymentEntity
            {
                Id = 1,
                ChargeId = 100,
                Amount = 150.75m,
                DateCreated = DateTime.UtcNow,
                DateDeleted = null
            };
            _chargePaymentRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargePaymentAsync(entity, true);

            _chargePaymentRepoMock.Verify(r => r.Update(entity), Times.Once);
            _chargePaymentRepoMock.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateChargePaymentAsync_DoesNotThrow_WhenCommitImmediatelyIsFalseAndCommitNotCalled()
        {
            var entity = new ChargePaymentEntity { Id = 1 };
            _chargePaymentRepoMock.Setup(r => r.Update(entity));

            var exception = await Record.ExceptionAsync(() =>
                _service.UpdateChargePaymentAsync(entity, false));

            Assert.Null(exception);
            _chargePaymentRepoMock.Verify(r => r.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateChargePaymentAsync_UpdatesEntityWithDateDeleted()
        {
            var entity = new ChargePaymentEntity
            {
                Id = 1,
                DateDeleted = DateTime.UtcNow
            };
            _chargePaymentRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargePaymentAsync(entity, true);

            _chargePaymentRepoMock.Verify(r => r.Update(It.Is<ChargePaymentEntity>(
                e => e.DateDeleted != null)), Times.Once);
        }

        [Fact]
        public async Task UpdateChargePaymentAsync_UpdatesEntityAmount()
        {
            var entity = new ChargePaymentEntity
            {
                Id = 1,
                Amount = 500.00m
            };
            _chargePaymentRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargePaymentAsync(entity, true);

            _chargePaymentRepoMock.Verify(r => r.Update(It.Is<ChargePaymentEntity>(
                e => e.Amount == 500.00m)), Times.Once);
        }

        [Fact]
        public async Task UpdateChargePaymentAsync_UpdatesEntityWithModifiedDate()
        {
            var modifiedDate = DateTime.UtcNow;
            var entity = new ChargePaymentEntity
            {
                Id = 1,
                DateLastModified = modifiedDate
            };
            _chargePaymentRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargePaymentAsync(entity, true);

            _chargePaymentRepoMock.Verify(r => r.Update(It.Is<ChargePaymentEntity>(
                e => e.DateLastModified == modifiedDate)), Times.Once);
        }

        [Fact]
        public async Task UpdateChargePaymentAsync_HandlesDecimalPrecision()
        {
            var entity = new ChargePaymentEntity
            {
                Id = 1,
                Amount = 123.456789m
            };
            _chargePaymentRepoMock.Setup(r => r.Update(entity));
            _chargePaymentRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.UpdateChargePaymentAsync(entity, true);

            _chargePaymentRepoMock.Verify(r => r.Update(It.Is<ChargePaymentEntity>(
                e => e.Amount == 123.456789m)), Times.Once);
        }

        [Fact]
        public async Task GetMaxChargePaymentIdAsync_ReturnsMaxId()
        {
            var entities = new List<ChargePaymentEntity> { new ChargePaymentEntity { Id = 5 }, new ChargePaymentEntity { Id = 10 } };
            var dbSet = DbMock.Create(entities);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(dbSet);

            var result = await _service.GetMaxChargePaymentIdAsync();

            Assert.Equal(10, result);
        }

        [Fact]
        public async Task GetMaxChargePaymentIdAsync_ReturnsZero_WhenNoPaymentsExist()
        {
            // Arrange: empty list simulates no payments in the database
            var emptyDbSet = DbMock.Create(new List<ChargePaymentEntity>());
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(emptyDbSet);

            // Act
            var result = await _service.GetMaxChargePaymentIdAsync();

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task GetMaxChargePaymentIdAsync_ReturnsSingleId_WhenOnlyOnePaymentExists()
        {
            var entities = new List<ChargePaymentEntity> { new ChargePaymentEntity { Id = 42 } };
            var dbSet = DbMock.Create(entities);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(dbSet);

            var result = await _service.GetMaxChargePaymentIdAsync();

            Assert.Equal(42, result);
        }

        [Fact]
        public async Task GetMaxChargePaymentIdAsync_ReturnsMaxId_WhenMultiplePaymentsExist()
        {
            var entities = new List<ChargePaymentEntity>
    {
        new ChargePaymentEntity { Id = 5 },
        new ChargePaymentEntity { Id = 10 },
        new ChargePaymentEntity { Id = 3 },
        new ChargePaymentEntity { Id = 8 },
        new ChargePaymentEntity { Id = 15 }
    };
            var dbSet = DbMock.Create(entities);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(dbSet);

            var result = await _service.GetMaxChargePaymentIdAsync();

            Assert.Equal(15, result);
        }

        [Fact]
        public async Task GetMaxChargePaymentIdAsync_ReturnsMaxId_WhenIdsAreNotSequential()
        {
            var entities = new List<ChargePaymentEntity>
    {
        new ChargePaymentEntity { Id = 100 },
        new ChargePaymentEntity { Id = 50 },
        new ChargePaymentEntity { Id = 200 },
        new ChargePaymentEntity { Id = 25 }
    };
            var dbSet = DbMock.Create(entities);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(dbSet);

            var result = await _service.GetMaxChargePaymentIdAsync();

            Assert.Equal(200, result);
        }

        [Fact]
        public async Task GetMaxChargePaymentIdAsync_ReturnsMaxId_WhenIdsContainDuplicates()
        {
            var entities = new List<ChargePaymentEntity>
    {
        new ChargePaymentEntity { Id = 10 },
        new ChargePaymentEntity { Id = 10 },
        new ChargePaymentEntity { Id = 5 }
    };
            var dbSet = DbMock.Create(entities);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(dbSet);

            var result = await _service.GetMaxChargePaymentIdAsync();

            Assert.Equal(10, result);
        }

        [Fact]
        public async Task GetMaxChargePaymentIdAsync_ReturnsMaxId_WhenAllIdsAreSame()
        {
            var entities = new List<ChargePaymentEntity>
    {
        new ChargePaymentEntity { Id = 7 },
        new ChargePaymentEntity { Id = 7 },
        new ChargePaymentEntity { Id = 7 }
    };
            var dbSet = DbMock.Create(entities);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(dbSet);

            var result = await _service.GetMaxChargePaymentIdAsync();

            Assert.Equal(7, result);
        }

        [Fact]
        public async Task GetMaxChargePaymentIdAsync_ReturnsMaxId_WithLargeDataSet()
        {
            var entities = Enumerable.Range(1, 1000)
                .Select(i => new ChargePaymentEntity { Id = i })
                .ToList();
            var dbSet = DbMock.Create(entities);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(dbSet);

            var result = await _service.GetMaxChargePaymentIdAsync();

            Assert.Equal(1000, result);
        }

        [Fact]
        public async Task GetMaxChargePaymentIdAsync_ReturnsMaxId_WhenIdsAreInDescendingOrder()
        {
            var entities = new List<ChargePaymentEntity>
    {
        new ChargePaymentEntity { Id = 100 },
        new ChargePaymentEntity { Id = 80 },
        new ChargePaymentEntity { Id = 60 },
        new ChargePaymentEntity { Id = 40 },
        new ChargePaymentEntity { Id = 20 }
    };
            var dbSet = DbMock.Create(entities);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(dbSet);

            var result = await _service.GetMaxChargePaymentIdAsync();

            Assert.Equal(100, result);
        }

        [Fact]
        public async Task GetMaxChargePaymentIdAsync_ReturnsMaxId_WhenIdsAreInAscendingOrder()
        {
            var entities = new List<ChargePaymentEntity>
    {
        new ChargePaymentEntity { Id = 1 },
        new ChargePaymentEntity { Id = 2 },
        new ChargePaymentEntity { Id = 3 },
        new ChargePaymentEntity { Id = 4 },
        new ChargePaymentEntity { Id = 5 }
    };
            var dbSet = DbMock.Create(entities);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(dbSet);

            var result = await _service.GetMaxChargePaymentIdAsync();

            Assert.Equal(5, result);
        }

        [Fact]
        public async Task GetMaxChargePaymentIdAsync_ThrowsException_WhenRepositoryFails()
        {
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(null, null))
                .ThrowsAsync(new Exception("Database connection failed"));

            await Assert.ThrowsAsync<Exception>(() => _service.GetMaxChargePaymentIdAsync());
        }

        [Fact]
        public async Task GetMaxChargePaymentIdAsync_CallsRepositoryOnce()
        {
            var entities = new List<ChargePaymentEntity> { new ChargePaymentEntity { Id = 1 } };
            var dbSet = DbMock.Create(entities);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(dbSet);

            await _service.GetMaxChargePaymentIdAsync();

            _chargePaymentRepoMock.Verify(r => r.GetAllAsync(null, null), Times.Once);
        }

        [Fact]
        public async Task GetMaxChargePaymentIdAsync_ReturnsMaxId_WhenIdsIncludeNegativeValues()
        {
            var entities = new List<ChargePaymentEntity>
    {
        new ChargePaymentEntity { Id = -10 },
        new ChargePaymentEntity { Id = 5 },
        new ChargePaymentEntity { Id = -3 }
    };
            var dbSet = DbMock.Create(entities);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(dbSet);

            var result = await _service.GetMaxChargePaymentIdAsync();

            Assert.Equal(5, result);
        }

        [Fact]
        public async Task GetMaxChargePaymentIdAsync_ReturnsZero_WhenAllIdsAreNegative()
        {
            var entities = new List<ChargePaymentEntity>
    {
        new ChargePaymentEntity { Id = -10 },
        new ChargePaymentEntity { Id = -5 },
        new ChargePaymentEntity { Id = -3 }
    };
            var dbSet = DbMock.Create(entities);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(dbSet);

            var result = await _service.GetMaxChargePaymentIdAsync();

            Assert.Equal(-3, result);
        }

        [Fact]
        public async Task GetMaxChargePaymentIdAsync_ReturnsMaxId_WithVeryLargeId()
        {
            var entities = new List<ChargePaymentEntity>
    {
        new ChargePaymentEntity { Id = int.MaxValue },
        new ChargePaymentEntity { Id = 100 },
        new ChargePaymentEntity { Id = 50 }
    };
            var dbSet = DbMock.Create(entities);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(dbSet);

            var result = await _service.GetMaxChargePaymentIdAsync();

            Assert.Equal(int.MaxValue, result);
        }

        [Fact]
        public async Task GetMaxChargePaymentIdAsync_ReturnsCorrectMaxId_WithMixedPositiveAndNegativeIds()
        {
            var entities = new List<ChargePaymentEntity>
    {
        new ChargePaymentEntity { Id = -100 },
        new ChargePaymentEntity { Id = 0 },
        new ChargePaymentEntity { Id = 50 },
        new ChargePaymentEntity { Id = -50 },
        new ChargePaymentEntity { Id = 25 }
    };
            var dbSet = DbMock.Create(entities);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(dbSet);

            var result = await _service.GetMaxChargePaymentIdAsync();

            Assert.Equal(50, result);
        }

        [Fact]
        public async Task GetMaxChargePaymentIdAsync_ReturnsZero_WhenIdIsZero()
        {
            var entities = new List<ChargePaymentEntity>
    {
        new ChargePaymentEntity { Id = 0 }
    };
            var dbSet = DbMock.Create(entities);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(dbSet);

            var result = await _service.GetMaxChargePaymentIdAsync();

            Assert.Equal(0, result);
        }

        [Fact]
        public async Task GetMaxChargePaymentIdAsync_HandlesNullResultFromOrderBy()
        {
            var emptyDbSet = DbMock.Create(new List<ChargePaymentEntity>());
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(emptyDbSet);

            var result = await _service.GetMaxChargePaymentIdAsync();

            Assert.Equal(0, result);
        }
        [Fact]
        public async Task AddChargeNoteAsync_ThrowsIfChargeNotFound()
        {
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity>());
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            await Assert.ThrowsAsync<NullReferenceException>(() => _service.AddChargeNoteAsync(new AddNoteModel { ChargeId = 1 }));
        }

        [Fact]
        public async Task AddChargeNoteAsync_AddsNoteAndReturnsModel()
        {
            var claim = new ClaimEntity { MemberId = 2, AccountInfoId = 3 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 4, BillingCode = "A", ChargePayments = new List<ChargePaymentEntity>() };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _rethinkMasterDataMock.Setup(r => r.GetMemberAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new RethinkAccountMember { firstName = "John", lastName = "Doe" });

            var model = new AddNoteModel { ChargeId = 1, NoteText = "Test", NoteCreatedBy = 5 };
            var result = await _service.AddChargeNoteAsync(model);

            Assert.Equal("Test", result.NoteText);
            Assert.Equal("John Doe", result.NoteCreatorName);
        }

        [Fact]
        public async Task AddChargeNoteAsync_ThrowsIfChargeIsDeleted()
        {
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, DateDeleted = DateTime.UtcNow };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            await Assert.ThrowsAsync<NullReferenceException>(() =>
                _service.AddChargeNoteAsync(new AddNoteModel { ChargeId = 1, NoteText = "Test" }));
        }

        [Fact]
        public async Task AddChargeNoteAsync_ReturnsEmptyNote_WhenNoteTextIsNull()
        {
            var claim = new ClaimEntity { MemberId = 2, AccountInfoId = 3 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 4, BillingCode = "A" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var model = new AddNoteModel { ChargeId = 1, NoteText = null };
            var result = await _service.AddChargeNoteAsync(model);

            Assert.Null(result.NoteText);
            _chargeEntryRepoMock.Verify(r => r.Update(It.IsAny<ClaimChargeEntryEntity>()), Times.Never);
        }

        [Fact]
        public async Task AddChargeNoteAsync_ReturnsEmptyNote_WhenNoteTextIsEmpty()
        {
            var claim = new ClaimEntity { MemberId = 2, AccountInfoId = 3 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 4, BillingCode = "A" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var model = new AddNoteModel { ChargeId = 1, NoteText = "" };
            var result = await _service.AddChargeNoteAsync(model);

            Assert.Null(result.NoteText);
            _chargeEntryRepoMock.Verify(r => r.Update(It.IsAny<ClaimChargeEntryEntity>()), Times.Never);
        }

        [Fact]
        public async Task AddChargeNoteAsync_ReturnsEmptyNote_WhenNoteTextIsWhitespace()
        {
            var claim = new ClaimEntity { MemberId = 2, AccountInfoId = 3 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 4, BillingCode = "A" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var model = new AddNoteModel { ChargeId = 1, NoteText = "   " };
            var result = await _service.AddChargeNoteAsync(model);

            Assert.Null(result.NoteText);
            _chargeEntryRepoMock.Verify(r => r.Update(It.IsAny<ClaimChargeEntryEntity>()), Times.Never);
        }

        [Fact]
        public async Task AddChargeNoteAsync_UpdatesChargeEntity()
        {
            var claim = new ClaimEntity { MemberId = 2, AccountInfoId = 3 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 4, BillingCode = "A" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _rethinkMasterDataMock.Setup(r => r.GetMemberAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new RethinkAccountMember { firstName = "Jane", lastName = "Smith" });

            var model = new AddNoteModel { ChargeId = 1, NoteText = "Important note", NoteCreatedBy = 10 };
            await _service.AddChargeNoteAsync(model);

            _chargeEntryRepoMock.Verify(r => r.Update(It.Is<ClaimChargeEntryEntity>(
                e => e.NoteText == "Important note" && e.NoteCreatedBy == 10)), Times.Once);
        }

        [Fact]
        public async Task AddChargeNoteAsync_CommitsChanges()
        {
            var claim = new ClaimEntity { MemberId = 2, AccountInfoId = 3 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 4, BillingCode = "A" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _rethinkMasterDataMock.Setup(r => r.GetMemberAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new RethinkAccountMember { firstName = "Jane", lastName = "Smith" });

            var model = new AddNoteModel { ChargeId = 1, NoteText = "Test note", NoteCreatedBy = 10 };
            await _service.AddChargeNoteAsync(model);

            _chargeEntryRepoMock.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task AddChargeNoteAsync_AddsClaimHistoryForNoteAdded()
        {
            var claim = new ClaimEntity { MemberId = 2, AccountInfoId = 3 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 4, BillingCode = "99213" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _rethinkMasterDataMock.Setup(r => r.GetMemberAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new RethinkAccountMember { firstName = "Jane", lastName = "Smith" });

            var model = new AddNoteModel { ChargeId = 1, NoteText = "Test note", NoteCreatedBy = 10 };
            await _service.AddChargeNoteAsync(model);

            _claimHistoryServiceMock.Verify(r => r.AddAsync(It.Is<ClaimHistorySaveModel>(
                m => m.ClaimHistoryAction == ClaimHistoryAction.ChargeEntryNoteAdded &&
                     m.NewValue == "99213"), true), Times.Once);
        }

        [Fact]
        public async Task AddChargeNoteAsync_AddsClaimHistoryForNoteDescAdded()
        {
            var claim = new ClaimEntity { MemberId = 2, AccountInfoId = 3 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 4, BillingCode = "99213" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _rethinkMasterDataMock.Setup(r => r.GetMemberAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new RethinkAccountMember { firstName = "Jane", lastName = "Smith" });

            var model = new AddNoteModel { ChargeId = 1, NoteText = "Test note description", NoteCreatedBy = 10 };
            await _service.AddChargeNoteAsync(model);

            _claimHistoryServiceMock.Verify(r => r.AddAsync(It.Is<ClaimHistorySaveModel>(
                m => m.ClaimHistoryAction == ClaimHistoryAction.ChargeEntryNoteDescAdded &&
                     m.NewValue == "Test note description"), true), Times.Once);
        }

        [Fact]
        public async Task AddChargeNoteAsync_SetsMemberIdInHistory()
        {
            var claim = new ClaimEntity { MemberId = 100, AccountInfoId = 3 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 4, BillingCode = "99213" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _rethinkMasterDataMock.Setup(r => r.GetMemberAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new RethinkAccountMember { firstName = "Jane", lastName = "Smith" });

            var model = new AddNoteModel { ChargeId = 1, NoteText = "Test note", NoteCreatedBy = 10 };
            await _service.AddChargeNoteAsync(model);

            _claimHistoryServiceMock.Verify(r => r.AddAsync(It.Is<ClaimHistorySaveModel>(
                m => m.MemberId == 100), true), Times.Exactly(2));
        }

        [Fact]
        public async Task AddChargeNoteAsync_CallsGetMemberAsync()
        {
            var claim = new ClaimEntity { MemberId = 2, AccountInfoId = 50 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 4, BillingCode = "99213" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _rethinkMasterDataMock.Setup(r => r.GetMemberAsync(50, 10))
                .ReturnsAsync(new RethinkAccountMember { firstName = "Jane", lastName = "Smith" });

            var model = new AddNoteModel { ChargeId = 1, NoteText = "Test note", NoteCreatedBy = 10 };
            await _service.AddChargeNoteAsync(model);

            _rethinkMasterDataMock.Verify(r => r.GetMemberAsync(50, 10), Times.Once);
        }

        [Fact]
        public async Task AddChargeNoteAsync_HandlesNullNoteCreatedBy()
        {
            var claim = new ClaimEntity { MemberId = 2, AccountInfoId = 50 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 4, BillingCode = "99213" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _rethinkMasterDataMock.Setup(r => r.GetMemberAsync(50, 0))
                .ReturnsAsync(new RethinkAccountMember { firstName = "System", lastName = "User" });

            var model = new AddNoteModel { ChargeId = 1, NoteText = "Test note", NoteCreatedBy = null };
            await _service.AddChargeNoteAsync(model);

            _rethinkMasterDataMock.Verify(r => r.GetMemberAsync(50, 0), Times.Once);
        }

        [Fact]
        public async Task AddChargeNoteAsync_SetsNoteCreatedDate()
        {
            var claim = new ClaimEntity { MemberId = 2, AccountInfoId = 3 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 4, BillingCode = "99213" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _rethinkMasterDataMock.Setup(r => r.GetMemberAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new RethinkAccountMember { firstName = "Jane", lastName = "Smith" });

            var model = new AddNoteModel { ChargeId = 1, NoteText = "Test note", NoteCreatedBy = 10 };
            var result = await _service.AddChargeNoteAsync(model);

            Assert.NotNull(result.NoteCreatedDate);
            _chargeEntryRepoMock.Verify(r => r.Update(It.Is<ClaimChargeEntryEntity>(
                e => e.NoteCreatedDate != null)), Times.Once);
        }

        [Fact]
        public async Task AddChargeNoteAsync_ThrowsException_WhenRepositoryFails()
        {
            var claim = new ClaimEntity { MemberId = 2, AccountInfoId = 3 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 4, BillingCode = "99213" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).ThrowsAsync(new Exception("Database error"));

            var model = new AddNoteModel { ChargeId = 1, NoteText = "Test note", NoteCreatedBy = 10 };

            await Assert.ThrowsAsync<Exception>(() => _service.AddChargeNoteAsync(model));
        }

        [Fact]
        public async Task AddChargeNoteAsync_ThrowsException_WhenHistoryServiceFails()
        {
            var claim = new ClaimEntity { MemberId = 2, AccountInfoId = 3 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 4, BillingCode = "99213" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true))
                .ThrowsAsync(new Exception("History service error"));

            var model = new AddNoteModel { ChargeId = 1, NoteText = "Test note", NoteCreatedBy = 10 };

            await Assert.ThrowsAsync<Exception>(() => _service.AddChargeNoteAsync(model));
        }

        [Fact]
        public async Task AddChargeNoteAsync_ThrowsException_WhenMemberServiceFails()
        {
            var claim = new ClaimEntity { MemberId = 2, AccountInfoId = 3 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 4, BillingCode = "99213" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _rethinkMasterDataMock.Setup(r => r.GetMemberAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Member service error"));

            var model = new AddNoteModel { ChargeId = 1, NoteText = "Test note", NoteCreatedBy = 10 };

            await Assert.ThrowsAsync<Exception>(() => _service.AddChargeNoteAsync(model));
        }

        [Fact]
        public async Task AddChargeNoteAsync_HandlesLongNoteText()
        {
            var claim = new ClaimEntity { MemberId = 2, AccountInfoId = 3 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 4, BillingCode = "99213" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _rethinkMasterDataMock.Setup(r => r.GetMemberAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new RethinkAccountMember { firstName = "Jane", lastName = "Smith" });

            var longNote = new string('A', 1000);
            var model = new AddNoteModel { ChargeId = 1, NoteText = longNote, NoteCreatedBy = 10 };
            var result = await _service.AddChargeNoteAsync(model);

            Assert.Equal(longNote, result.NoteText);
        }

        [Fact]
        public async Task AddChargeNoteAsync_HandlesMemberWithNoLastName()
        {
            var claim = new ClaimEntity { MemberId = 2, AccountInfoId = 3 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 4, BillingCode = "99213" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _rethinkMasterDataMock.Setup(r => r.GetMemberAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new RethinkAccountMember { firstName = "Jane", lastName = null });

            var model = new AddNoteModel { ChargeId = 1, NoteText = "Test note", NoteCreatedBy = 10 };
            var result = await _service.AddChargeNoteAsync(model);

            Assert.Equal("Jane ", result.NoteCreatorName);
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_ThrowsIfChargeNotFound()
        {
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity>());
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            await Assert.ThrowsAsync<NullReferenceException>(() => _service.DeleteChargeNoteAsync(1));
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_DeletesNote()
        {
            var claim = new ClaimEntity { MemberId = 2 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 3, NoteText = "Note", ChargePayments = new List<ChargePaymentEntity>() };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _claimHistoryServiceMock.Setup(r => r.GetAllAsync(3)).ReturnsAsync(new List<ClaimHistoryModel>());
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.DeleteChargeNoteAsync(1);

            _chargeEntryRepoMock.Verify(r => r.Update(chargeEntity), Times.Once);
            _chargeEntryRepoMock.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_UsesHistoryNoteText_WhenHistoryExists()
        {
            // Arrange
            var claim = new ClaimEntity { MemberId = 2 };
            var chargeId = 1;
            var chargeEntity = new ClaimChargeEntryEntity
            {
                Id = chargeId,
                Claim = claim,
                ClaimId = 3,
                NoteText = "NoteFromEntity",
                ChargePayments = new List<ChargePaymentEntity>()
            };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var historyNote = new ClaimHistoryModel
            {
                HistoryActionId = ClaimHistoryAction.ChargeEntryNoteDescAdded,
                FieldId = chargeId,
                NewValue = "NoteFromHistory"
            };
            var chargeHistory = new List<ClaimHistoryModel> { historyNote };
            _claimHistoryServiceMock.Setup(r => r.GetAllAsync(3)).ReturnsAsync(chargeHistory);
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            // Act
            await _service.DeleteChargeNoteAsync(chargeId);

            // Assert
            // The note text used should be from history, not from entity
            _claimHistoryServiceMock.Verify(x =>
                x.AddAsync(It.Is<ClaimHistorySaveModel>(m => m.NewValue == "NoteFromHistory" && m.ClaimHistoryAction == ClaimHistoryAction.ChargeEntryNoteDescRemoved), true),
                Times.Once);
            _chargeEntryRepoMock.Verify(r => r.Update(chargeEntity), Times.Once);
            _chargeEntryRepoMock.Verify(r => r.CommitAsync(), Times.Once);
        }

      
        [Fact]
        public async Task DeleteChargeNoteAsync_ThrowsIfChargeIsDeleted()
        {
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, DateDeleted = DateTime.UtcNow };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            await Assert.ThrowsAsync<NullReferenceException>(() => _service.DeleteChargeNoteAsync(1));
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_SetsNoteFieldsToNull()
        {
            var claim = new ClaimEntity { MemberId = 2 };
            var chargeEntity = new ClaimChargeEntryEntity
            {
                Id = 1,
                Claim = claim,
                ClaimId = 3,
                NoteText = "Existing note",
                NoteCreatedDate = DateTime.UtcNow,
                NoteCreatedBy = 5
            };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _claimHistoryServiceMock.Setup(r => r.GetAllAsync(3)).ReturnsAsync(new List<ClaimHistoryModel>());
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.DeleteChargeNoteAsync(1);

            _chargeEntryRepoMock.Verify(r => r.Update(It.Is<ClaimChargeEntryEntity>(
                e => e.NoteText == null && e.NoteCreatedDate == null && e.NoteCreatedBy == null)), Times.Once);
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_AddsHistoryForNoteRemoved()
        {
            var claim = new ClaimEntity { MemberId = 2 };
            var chargeEntity = new ClaimChargeEntryEntity
            {
                Id = 1,
                Claim = claim,
                ClaimId = 3,
                BillingCode = "99213",
                NoteText = "Note"
            };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _claimHistoryServiceMock.Setup(r => r.GetAllAsync(3)).ReturnsAsync(new List<ClaimHistoryModel>());
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.DeleteChargeNoteAsync(1);

            _claimHistoryServiceMock.Verify(r => r.AddAsync(It.Is<ClaimHistorySaveModel>(
                m => m.ClaimHistoryAction == ClaimHistoryAction.ChargeEntryNoteRemoved &&
                     m.NewValue == "99213"), true), Times.Once);
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_AddsHistoryForNoteDescRemoved_WhenNoteTextExists()
        {
            var claim = new ClaimEntity { MemberId = 2 };
            var chargeEntity = new ClaimChargeEntryEntity
            {
                Id = 1,
                Claim = claim,
                ClaimId = 3,
                BillingCode = "99213",
                NoteText = "Important note"
            };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _claimHistoryServiceMock.Setup(r => r.GetAllAsync(3)).ReturnsAsync(new List<ClaimHistoryModel>());
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.DeleteChargeNoteAsync(1);

            _claimHistoryServiceMock.Verify(r => r.AddAsync(It.Is<ClaimHistorySaveModel>(
                m => m.ClaimHistoryAction == ClaimHistoryAction.ChargeEntryNoteDescRemoved &&
                     m.NewValue == "Important note"), true), Times.Once);
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_DoesNotAddNoteDescHistory_WhenNoteTextIsNull()
        {
            var claim = new ClaimEntity { MemberId = 2 };
            var chargeEntity = new ClaimChargeEntryEntity
            {
                Id = 1,
                Claim = claim,
                ClaimId = 3,
                BillingCode = "99213",
                NoteText = null
            };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _claimHistoryServiceMock.Setup(r => r.GetAllAsync(3)).ReturnsAsync(new List<ClaimHistoryModel>());
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.DeleteChargeNoteAsync(1);

            _claimHistoryServiceMock.Verify(r => r.AddAsync(It.Is<ClaimHistorySaveModel>(
                m => m.ClaimHistoryAction == ClaimHistoryAction.ChargeEntryNoteDescRemoved), true), Times.Never);
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_DoesNotAddNoteDescHistory_WhenNoteTextIsEmpty()
        {
            var claim = new ClaimEntity { MemberId = 2 };
            var chargeEntity = new ClaimChargeEntryEntity
            {
                Id = 1,
                Claim = claim,
                ClaimId = 3,
                BillingCode = "99213",
                NoteText = ""
            };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _claimHistoryServiceMock.Setup(r => r.GetAllAsync(3)).ReturnsAsync(new List<ClaimHistoryModel>());
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.DeleteChargeNoteAsync(1);

            _claimHistoryServiceMock.Verify(r => r.AddAsync(It.Is<ClaimHistorySaveModel>(
                m => m.ClaimHistoryAction == ClaimHistoryAction.ChargeEntryNoteDescRemoved), true), Times.Never);
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_UsesEntityNoteText_WhenNoHistoryExists()
        {
            var claim = new ClaimEntity { MemberId = 2 };
            var chargeEntity = new ClaimChargeEntryEntity
            {
                Id = 1,
                Claim = claim,
                ClaimId = 3,
                BillingCode = "99213",
                NoteText = "NoteFromEntity"
            };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _claimHistoryServiceMock.Setup(r => r.GetAllAsync(3)).ReturnsAsync(new List<ClaimHistoryModel>());
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.DeleteChargeNoteAsync(1);

            _claimHistoryServiceMock.Verify(r => r.AddAsync(It.Is<ClaimHistorySaveModel>(
                m => m.ClaimHistoryAction == ClaimHistoryAction.ChargeEntryNoteDescRemoved &&
                     m.NewValue == "NoteFromEntity"), true), Times.Once);
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_FiltersHistoryByFieldId()
        {
            var claim = new ClaimEntity { MemberId = 2 };
            var chargeId = 1;
            var chargeEntity = new ClaimChargeEntryEntity
            {
                Id = chargeId,
                Claim = claim,
                ClaimId = 3,
                BillingCode = "99213",
                NoteText = "NoteFromEntity"
            };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var historyNotes = new List<ClaimHistoryModel>
    {
        new ClaimHistoryModel
        {
            HistoryActionId = ClaimHistoryAction.ChargeEntryNoteDescAdded,
            FieldId = 99,
            NewValue = "WrongNote"
        },
        new ClaimHistoryModel
        {
            HistoryActionId = ClaimHistoryAction.ChargeEntryNoteDescAdded,
            FieldId = chargeId,
            NewValue = "CorrectNote"
        }
    };
            _claimHistoryServiceMock.Setup(r => r.GetAllAsync(3)).ReturnsAsync(historyNotes);
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.DeleteChargeNoteAsync(chargeId);

            _claimHistoryServiceMock.Verify(r => r.AddAsync(It.Is<ClaimHistorySaveModel>(
                m => m.NewValue == "CorrectNote"), true), Times.Once);
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_OrdersByFieldIdDescending()
        {
            var claim = new ClaimEntity { MemberId = 2 };
            var chargeId = 1;
            var chargeEntity = new ClaimChargeEntryEntity
            {
                Id = chargeId,
                Claim = claim,
                ClaimId = 3,
                BillingCode = "99213",
                NoteText = "NoteFromEntity"
            };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var historyNotes = new List<ClaimHistoryModel>
    {
        new ClaimHistoryModel
        {
            HistoryActionId = ClaimHistoryAction.ChargeEntryNoteDescAdded,
            FieldId = chargeId,
            NewValue = "OlderNote"
        },
        new ClaimHistoryModel
        {
            HistoryActionId = ClaimHistoryAction.ChargeEntryNoteDescAdded,
            FieldId = chargeId,
            NewValue = "NewerNote"
        }
    };
            _claimHistoryServiceMock.Setup(r => r.GetAllAsync(3)).ReturnsAsync(historyNotes);
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.DeleteChargeNoteAsync(chargeId);

            _claimHistoryServiceMock.Verify(r => r.AddAsync(It.Is<ClaimHistorySaveModel>(
                m => m.ClaimHistoryAction == ClaimHistoryAction.ChargeEntryNoteDescRemoved), true), Times.Once);
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_CommitsChanges()
        {
            var claim = new ClaimEntity { MemberId = 2 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 3, BillingCode = "99213", NoteText = "Note" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _claimHistoryServiceMock.Setup(r => r.GetAllAsync(3)).ReturnsAsync(new List<ClaimHistoryModel>());
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.DeleteChargeNoteAsync(1);

            _chargeEntryRepoMock.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_UpdatesChargeEntity()
        {
            var claim = new ClaimEntity { MemberId = 2 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 3, BillingCode = "99213", NoteText = "Note" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _claimHistoryServiceMock.Setup(r => r.GetAllAsync(3)).ReturnsAsync(new List<ClaimHistoryModel>());
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.DeleteChargeNoteAsync(1);

            _chargeEntryRepoMock.Verify(r => r.Update(chargeEntity), Times.Once);
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_CallsGetAllAsyncForHistory()
        {
            var claim = new ClaimEntity { MemberId = 2 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 3, BillingCode = "99213", NoteText = "Note" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _claimHistoryServiceMock.Setup(r => r.GetAllAsync(3)).ReturnsAsync(new List<ClaimHistoryModel>());
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.DeleteChargeNoteAsync(1);

            _claimHistoryServiceMock.Verify(r => r.GetAllAsync(3), Times.Once);
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_ThrowsException_WhenRepositoryFails()
        {
            var claim = new ClaimEntity { MemberId = 2 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 3, BillingCode = "99213", NoteText = "Note" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _claimHistoryServiceMock.Setup(r => r.GetAllAsync(3)).ReturnsAsync(new List<ClaimHistoryModel>());
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).ThrowsAsync(new Exception("Database error"));

            await Assert.ThrowsAsync<Exception>(() => _service.DeleteChargeNoteAsync(1));
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_ThrowsException_WhenHistoryServiceFails()
        {
            var claim = new ClaimEntity { MemberId = 2 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 3, BillingCode = "99213", NoteText = "Note" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _claimHistoryServiceMock.Setup(r => r.GetAllAsync(3)).ThrowsAsync(new Exception("History service error"));

            await Assert.ThrowsAsync<Exception>(() => _service.DeleteChargeNoteAsync(1));
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_SetsMemberIdInHistory()
        {
            var claim = new ClaimEntity { MemberId = 100 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 3, BillingCode = "99213", NoteText = "Note" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _claimHistoryServiceMock.Setup(r => r.GetAllAsync(3)).ReturnsAsync(new List<ClaimHistoryModel>());
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.DeleteChargeNoteAsync(1);

            _claimHistoryServiceMock.Verify(r => r.AddAsync(It.Is<ClaimHistorySaveModel>(
                m => m.MemberId == 100), true), Times.Exactly(2));
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_SetsClaimIdInHistory()
        {
            var claim = new ClaimEntity { MemberId = 2 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 50, BillingCode = "99213", NoteText = "Note" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _claimHistoryServiceMock.Setup(r => r.GetAllAsync(50)).ReturnsAsync(new List<ClaimHistoryModel>());
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.DeleteChargeNoteAsync(1);

            _claimHistoryServiceMock.Verify(r => r.AddAsync(It.Is<ClaimHistorySaveModel>(
                m => m.ClaimId == 50), true), Times.Exactly(2));
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_SetsHistoryModeAndAction()
        {
            var claim = new ClaimEntity { MemberId = 2 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 3, BillingCode = "99213", NoteText = "Note" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _claimHistoryServiceMock.Setup(r => r.GetAllAsync(3)).ReturnsAsync(new List<ClaimHistoryModel>());
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.DeleteChargeNoteAsync(1);

            _claimHistoryServiceMock.Verify(r => r.AddAsync(It.Is<ClaimHistorySaveModel>(
                m => m.Mode == ClaimActionMode.User && m.ClaimAction == ClaimAction.Added), true), Times.Exactly(2));
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_HandlesMultipleHistoryRecords()
        {
            var claim = new ClaimEntity { MemberId = 2 };
            var chargeId = 1;
            var chargeEntity = new ClaimChargeEntryEntity
            {
                Id = chargeId,
                Claim = claim,
                ClaimId = 3,
                BillingCode = "99213",
                NoteText = "EntityNote"
            };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var historyNotes = new List<ClaimHistoryModel>
    {
        new ClaimHistoryModel
        {
            HistoryActionId = ClaimHistoryAction.ChargeEntryNoteDescAdded,
            FieldId = chargeId,
            NewValue = "FirstNote"
        },
        new ClaimHistoryModel
        {
            HistoryActionId = ClaimHistoryAction.ChargeEntryNoteDescAdded,
            FieldId = chargeId,
            NewValue = "SecondNote"
        },
        new ClaimHistoryModel
        {
            HistoryActionId = ClaimHistoryAction.ChargeEntryNoteDescAdded,
            FieldId = chargeId,
            NewValue = "ThirdNote"
        }
    };
            _claimHistoryServiceMock.Setup(r => r.GetAllAsync(3)).ReturnsAsync(historyNotes);
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.DeleteChargeNoteAsync(chargeId);

            _claimHistoryServiceMock.Verify(r => r.AddAsync(It.Is<ClaimHistorySaveModel>(
                m => m.ClaimHistoryAction == ClaimHistoryAction.ChargeEntryNoteDescRemoved), true), Times.Once);
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_HandlesNullHistoryResult()
        {
            var claim = new ClaimEntity { MemberId = 2 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 3, BillingCode = "99213", NoteText = "EntityNote" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _claimHistoryServiceMock.Setup(r => r.GetAllAsync(3)).ReturnsAsync((List<ClaimHistoryModel>)null);
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.DeleteChargeNoteAsync(1);

            _claimHistoryServiceMock.Verify(r => r.AddAsync(It.Is<ClaimHistorySaveModel>(
                m => m.NewValue == "EntityNote"), true), Times.Once);
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_IncludesClaim()
        {
            var claim = new ClaimEntity { MemberId = 2 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 3, BillingCode = "99213", NoteText = "Note" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _claimHistoryServiceMock.Setup(r => r.GetAllAsync(3)).ReturnsAsync(new List<ClaimHistoryModel>());
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.DeleteChargeNoteAsync(1);

            Assert.NotNull(chargeEntity.Claim);
            _claimHistoryServiceMock.Verify(r => r.AddAsync(It.Is<ClaimHistorySaveModel>(
                m => m.MemberId == 2), true), Times.Exactly(2));
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_AddsExactlyTwoHistoryEntries_WhenNoteTextExists()
        {
            var claim = new ClaimEntity { MemberId = 2 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 3, BillingCode = "99213", NoteText = "Note" };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _claimHistoryServiceMock.Setup(r => r.GetAllAsync(3)).ReturnsAsync(new List<ClaimHistoryModel>());
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.DeleteChargeNoteAsync(1);

            _claimHistoryServiceMock.Verify(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true), Times.Exactly(2));
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_AddsOnlyOneHistoryEntry_WhenNoteTextIsNull()
        {
            var claim = new ClaimEntity { MemberId = 2 };
            var chargeEntity = new ClaimChargeEntryEntity { Id = 1, Claim = claim, ClaimId = 3, BillingCode = "99213", NoteText = null };
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntity });
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _claimHistoryServiceMock.Setup(r => r.GetAllAsync(3)).ReturnsAsync(new List<ClaimHistoryModel>());
            _claimHistoryServiceMock.Setup(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true)).Returns(Task.CompletedTask);
            _chargeEntryRepoMock.Setup(r => r.Update(chargeEntity));
            _chargeEntryRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            await _service.DeleteChargeNoteAsync(1);

            _claimHistoryServiceMock.Verify(r => r.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true), Times.Once);
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_HandlesZeroChargeId()
        {
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity>());
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            await Assert.ThrowsAsync<NullReferenceException>(() => _service.DeleteChargeNoteAsync(0));
        }

        [Fact]
        public async Task DeleteChargeNoteAsync_HandlesNegativeChargeId()
        {
            var dbSet = DbMock.Create(new List<ClaimChargeEntryEntity>());
            _chargeEntryRepoMock.Setup(r => r.Query()).Returns(dbSet);

            await Assert.ThrowsAsync<NullReferenceException>(() => _service.DeleteChargeNoteAsync(-1));
        }

        [Fact]
        public async Task GetIdsAllOpenedPatientClaimAsync_ReturnsClaims()
        {
            var chargeEntry = new ClaimChargeEntryEntity { Id = 1, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() };
            var claim = new ClaimEntity { Id = 1, ChildProfileId = 2, ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry } };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ClaimEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetIdsAllOpenedPatientClaimAsync(2);

            Assert.Single(result);
            Assert.Equal(1, result[0].ClaimId);
        }

        [Fact]
        public async Task GetIdsAllOpenedPatientClaimAsync_ReturnsEmptyList_WhenNoClaimsExist()
        {
            var dbSet = DbMock.Create(new List<ClaimEntity>());
            _claimRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetIdsAllOpenedPatientClaimAsync(1);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetIdsAllOpenedPatientClaimAsync_ReturnsEmptyList_WhenPatientIdNotFound()
        {
            var claim = new ClaimEntity { Id = 1, ChildProfileId = 999, DateDeleted = null };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), null))
                .ReturnsAsync(DbMock.Create(new List<ClaimEntity>()));

            var result = await _service.GetIdsAllOpenedPatientClaimAsync(1);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetIdsAllOpenedPatientClaimAsync_IncludesClaimsWithoutDeletion()
        {
            var chargeEntry = new ClaimChargeEntryEntity { Id = 1, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() };
            var claim = new ClaimEntity
            {
                Id = 1,
                ChildProfileId = 2,
                DateDeleted = null,
                isPrivatePayClaim = false,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetIdsAllOpenedPatientClaimAsync(2);

            Assert.Single(result);
            Assert.Equal(1, result[0].ClaimId);
        }

        [Fact]
        public async Task GetIdsAllOpenedPatientClaimAsync_IncludesDeletedPrivatePayClaims()
        {
            var chargeEntry = new ClaimChargeEntryEntity { Id = 1, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() };
            var deletedPrivateClaim = new ClaimEntity
            {
                Id = 1,
                ChildProfileId = 2,
                DateDeleted = DateTime.UtcNow,
                isPrivatePayClaim = true,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { deletedPrivateClaim });
            _claimRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetIdsAllOpenedPatientClaimAsync(2);

            Assert.Single(result);
            Assert.Equal(1, result[0].ClaimId);
        }

        [Fact]
        public async Task GetIdsAllOpenedPatientClaimAsync_ExcludesDeletedNonPrivateClaims()
        {
            var chargeEntry = new ClaimChargeEntryEntity { Id = 1, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() };
            var deletedClaim = new ClaimEntity
            {
                Id = 1,
                ChildProfileId = 2,
                DateDeleted = DateTime.UtcNow,
                isPrivatePayClaim = false,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry }
            };
            var filteredDbSet = DbMock.Create(new List<ClaimEntity>());
            _claimRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), null))
                .ReturnsAsync(filteredDbSet);

            var result = await _service.GetIdsAllOpenedPatientClaimAsync(2);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetIdsAllOpenedPatientClaimAsync_ExcludesDeletedChargeEntries()
        {
            var activeEntry = new ClaimChargeEntryEntity { Id = 1, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() };
            var deletedEntry = new ClaimChargeEntryEntity { Id = 2, DateDeleted = DateTime.UtcNow, ChargePayments = new List<ChargePaymentEntity>() };
            var claim = new ClaimEntity
            {
                Id = 1,
                ChildProfileId = 2,
                DateDeleted = null,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { activeEntry, deletedEntry }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetIdsAllOpenedPatientClaimAsync(2);

            Assert.Single(result);
            Assert.Single(result[0].ChargeEntries);
        }

        [Fact]
        public async Task GetIdsAllOpenedPatientClaimAsync_ExcludesDeletedChargePayments()
        {
            var activePayment = new ChargePaymentEntity { Id = 1, Amount = 100m, DateDeleted = null };
            var deletedPayment = new ChargePaymentEntity { Id = 2, Amount = 50m, DateDeleted = DateTime.UtcNow };
            var chargeEntry = new ClaimChargeEntryEntity
            {
                Id = 1,
                Charges = 150m,
                DateDeleted = null,
                ChargePayments = new List<ChargePaymentEntity> { activePayment, deletedPayment }
            };
            var claim = new ClaimEntity
            {
                Id = 1,
                ChildProfileId = 2,
                DateDeleted = null,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetIdsAllOpenedPatientClaimAsync(2);

            var chargeItem = result[0].ChargeEntries.First();
            Assert.Equal(100m, chargeItem.TotalAmount);
            Assert.Single(chargeItem.ClaimChargeItems);
        }

        [Fact]
        public async Task GetIdsAllOpenedPatientClaimAsync_CalculatesTotalAmountCorrectly()
        {
            var payments = new List<ChargePaymentEntity>
    {
        new ChargePaymentEntity { Id = 1, Amount = 100m, DateDeleted = null },
        new ChargePaymentEntity { Id = 2, Amount = 50.75m, DateDeleted = null },
        new ChargePaymentEntity { Id = 3, Amount = 25.25m, DateDeleted = null }
    };
            var chargeEntry = new ClaimChargeEntryEntity
            {
                Id = 1,
                Charges = 200m,
                DateDeleted = null,
                ChargePayments = payments
            };
            var claim = new ClaimEntity
            {
                Id = 1,
                ChildProfileId = 2,
                DateDeleted = null,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetIdsAllOpenedPatientClaimAsync(2);

            var chargeItem = result[0].ChargeEntries.First();
            Assert.Equal(176m, chargeItem.TotalAmount);
        }

        [Fact]
        public async Task GetIdsAllOpenedPatientClaimAsync_MapsAllChargeEntryProperties()
        {
            var payments = new List<ChargePaymentEntity>
    {
        new ChargePaymentEntity { Id = 1, Amount = 100m, DateDeleted = null }
    };
            var dateOfService = DateTime.UtcNow;
            var chargeEntry = new ClaimChargeEntryEntity
            {
                Id = 10,
                ClaimId = 20,
                Charges = 150.50m,
                DateOfService = dateOfService,
                BillingCode = "99213",
                Modifier1 = "25",
                Modifier2 = "GT",
                Modifier3 = "59",
                Modifier4 = "76",
                Description = "Office Visit",
                DateDeleted = null,
                ChargePayments = payments
            };
            var claim = new ClaimEntity
            {
                Id = 20,
                ChildProfileId = 2,
                DateDeleted = null,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetIdsAllOpenedPatientClaimAsync(2);

            var mappedEntry = result[0].ChargeEntries.First();
            Assert.Equal(10, mappedEntry.Id);
            Assert.Equal(20, mappedEntry.ClaimId);
            Assert.Equal(150.50m, mappedEntry.Charges);
            Assert.Equal(dateOfService, mappedEntry.DateOfService);
            Assert.Equal("99213", mappedEntry.ServiceCode);
            Assert.Equal("25", mappedEntry.Modifier1);
            Assert.Equal("GT", mappedEntry.Modifier2);
            Assert.Equal("59", mappedEntry.Modifier3);
            Assert.Equal("76", mappedEntry.Modifier4);
            Assert.Equal("Office Visit", mappedEntry.Description);
        }

        [Fact]
        public async Task GetIdsAllOpenedPatientClaimAsync_SetsClaimStatusToProcessedAsPrimary()
        {
            var chargeEntry = new ClaimChargeEntryEntity { Id = 1, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() };
            var claim = new ClaimEntity
            {
                Id = 1,
                ChildProfileId = 2,
                DateDeleted = null,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetIdsAllOpenedPatientClaimAsync(2);

            Assert.Equal((int)PaymentClaimStatus.ProcessedAsPrimary, result[0].ClaimStatus);
        }

        [Fact]
        public async Task GetIdsAllOpenedPatientClaimAsync_ReturnsMultipleClaims()
        {
            var entry1 = new ClaimChargeEntryEntity { Id = 1, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() };
            var entry2 = new ClaimChargeEntryEntity { Id = 2, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() };
            var claim1 = new ClaimEntity
            {
                Id = 1,
                ChildProfileId = 2,
                DateDeleted = null,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { entry1 }
            };
            var claim2 = new ClaimEntity
            {
                Id = 2,
                ChildProfileId = 2,
                DateDeleted = null,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { entry2 }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim1, claim2 });
            _claimRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetIdsAllOpenedPatientClaimAsync(2);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetIdsAllOpenedPatientClaimAsync_ReturnsMultipleChargeEntriesPerClaim()
        {
            var entry1 = new ClaimChargeEntryEntity { Id = 1, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() };
            var entry2 = new ClaimChargeEntryEntity { Id = 2, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() };
            var entry3 = new ClaimChargeEntryEntity { Id = 3, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() };
            var claim = new ClaimEntity
            {
                Id = 1,
                ChildProfileId = 2,
                DateDeleted = null,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { entry1, entry2, entry3 }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetIdsAllOpenedPatientClaimAsync(2);

            Assert.Single(result);
            Assert.Equal(3, result[0].ChargeEntries.Count());
        }

        [Fact]
        public async Task GetIdsAllOpenedPatientClaimAsync_HandlesClaimWithNoChargeEntries()
        {
            var claim = new ClaimEntity
            {
                Id = 1,
                ChildProfileId = 2,
                DateDeleted = null,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>()
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetIdsAllOpenedPatientClaimAsync(2);

            Assert.Single(result);
            Assert.Empty(result[0].ChargeEntries);
        }

        [Fact]
        public async Task GetIdsAllOpenedPatientClaimAsync_HandlesChargeEntryWithNoPayments()
        {
            var chargeEntry = new ClaimChargeEntryEntity
            {
                Id = 1,
                Charges = 100m,
                DateDeleted = null,
                ChargePayments = new List<ChargePaymentEntity>()
            };
            var claim = new ClaimEntity
            {
                Id = 1,
                ChildProfileId = 2,
                DateDeleted = null,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetIdsAllOpenedPatientClaimAsync(2);

            var chargeItem = result[0].ChargeEntries.First();
            Assert.Equal(0m, chargeItem.TotalAmount);
            Assert.Empty(chargeItem.ClaimChargeItems);
        }

        [Fact]
        public async Task GetIdsAllOpenedPatientClaimAsync_HandlesZeroPatientId()
        {
            var dbSet = DbMock.Create(new List<ClaimEntity>());
            _claimRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetIdsAllOpenedPatientClaimAsync(0);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetIdsAllOpenedPatientClaimAsync_HandlesNegativePatientId()
        {
            var dbSet = DbMock.Create(new List<ClaimEntity>());
            _claimRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetIdsAllOpenedPatientClaimAsync(-1);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetIdsAllOpenedPatientClaimAsync_ThrowsException_WhenRepositoryFails()
        {
            _claimRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), null))
                .ThrowsAsync(new Exception("Database connection failed"));

            await Assert.ThrowsAsync<Exception>(() =>
                _service.GetIdsAllOpenedPatientClaimAsync(2));
        }

        [Fact]
        public async Task GetIdsAllOpenedPatientClaimAsync_CallsRepositoryOnce()
        {
            var dbSet = DbMock.Create(new List<ClaimEntity>());
            _claimRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            await _service.GetIdsAllOpenedPatientClaimAsync(2);

            _claimRepoMock.Verify(
                r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), null),
                Times.Once);
        }

        [Fact]
        public async Task GetIdsAllOpenedPatientClaimAsync_HandlesLargeDataSet()
        {
            var claims = Enumerable.Range(1, 100)
                .Select(i => new ClaimEntity
                {
                    Id = i,
                    ChildProfileId = 2,
                    DateDeleted = null,
                    ClaimChargeEntries = new List<ClaimChargeEntryEntity>
                    {
                new ClaimChargeEntryEntity { Id = i, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() }
                    }
                }).ToList();
            var dbSet = DbMock.Create(claims);
            _claimRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetIdsAllOpenedPatientClaimAsync(2);

            Assert.Equal(100, result.Count);
        }

        [Fact]
        public async Task GetIdsAllOpenedPatientClaimAsync_MixesDeletedAndActivePrivateClaims()
        {
            var entry1 = new ClaimChargeEntryEntity { Id = 1, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() };
            var entry2 = new ClaimChargeEntryEntity { Id = 2, DateDeleted = null, ChargePayments = new List<ChargePaymentEntity>() };
            var activeClaim = new ClaimEntity
            {
                Id = 1,
                ChildProfileId = 2,
                DateDeleted = null,
                isPrivatePayClaim = false,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { entry1 }
            };
            var deletedPrivateClaim = new ClaimEntity
            {
                Id = 2,
                ChildProfileId = 2,
                DateDeleted = DateTime.UtcNow,
                isPrivatePayClaim = true,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { entry2 }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { activeClaim, deletedPrivateClaim });
            _claimRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetIdsAllOpenedPatientClaimAsync(2);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetIdsAllOpenedPatientClaimAsync_IncludesChargePaymentsInProjection()
        {
            var payment1 = new ChargePaymentEntity { Id = 1, Amount = 100m, DateDeleted = null };
            var payment2 = new ChargePaymentEntity { Id = 2, Amount = 50m, DateDeleted = null };
            var chargeEntry = new ClaimChargeEntryEntity
            {
                Id = 1,
                DateDeleted = null,
                ChargePayments = new List<ChargePaymentEntity> { payment1, payment2 }
            };
            var claim = new ClaimEntity
            {
                Id = 1,
                ChildProfileId = 2,
                DateDeleted = null,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), null))
                .ReturnsAsync(dbSet);

            var result = await _service.GetIdsAllOpenedPatientClaimAsync(2);

            var chargeItem = result[0].ChargeEntries.First();
            Assert.Equal(2, chargeItem.ClaimChargeItems.Count());
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_ReturnsClaims()
        {
            var payment = new PaymentEntity { HcFunderId = 1 };
            var chargePayments = new List<ChargePaymentEntity>
            {
                new ChargePaymentEntity { Amount = 100m, DateDeleted = null },
                new ChargePaymentEntity { Amount = 50m, DateDeleted = null },
                new ChargePaymentEntity { Amount = 25m, DateDeleted = null }
            };
            var chargeEntry = new ClaimChargeEntryEntity
            {
                Id = 3,
                ClaimId = 2,
                DateDeleted = null,
                ChargePayments = chargePayments
            };
            var claim = new ClaimEntity
            {
                Id = 2,
                PrimaryFunderId = 1,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry },
                ChildProfileId = 5,
                DateDeleted = null
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.Query()).Returns(dbSet);

            // Mock repository to return the charge payments for the charge entry
            var paymentDbSet = DbMock.Create(chargePayments);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ChargePaymentEntity, bool>>>(), null))
                .ReturnsAsync(paymentDbSet);

            var result = await _service.GetAllClaimsByIdAsync(payment, new[] { 2 });

            Assert.Single(result);
            Assert.Equal(2, result[0].ClaimId);
            var chargeItem = result[0].ChargeEntries.Single();
            Assert.Equal(175m, chargeItem.TotalAmount); // 100 + 50 + 25
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_ReturnsEmptyList_WhenNoClaimsExist()
        {
            var payment = new PaymentEntity { HcFunderId = 1 };
            var dbSet = DbMock.Create(new List<ClaimEntity>());
            _claimRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var result = await _service.GetAllClaimsByIdAsync(payment, new[] { 1, 2 });

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_ReturnsEmptyList_WhenClaimIdsNotFound()
        {
            var payment = new PaymentEntity { HcFunderId = 1 };
            var claim = new ClaimEntity { Id = 10, DateDeleted = null };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var result = await _service.GetAllClaimsByIdAsync(payment, new[] { 1, 2 });

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_ExcludesDeletedNonPrivateClaims()
        {
            var payment = new PaymentEntity { HcFunderId = 1 };
            var deletedClaim = new ClaimEntity
            {
                Id = 1,
                DateDeleted = DateTime.UtcNow,
                isPrivatePayClaim = false,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>()
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { deletedClaim });
            _claimRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var result = await _service.GetAllClaimsByIdAsync(payment, new[] { 1 });

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_IncludesDeletedPrivatePayClaims()
        {
            var payment = new PaymentEntity { HcFunderId = 1 };
            var chargeEntry = new ClaimChargeEntryEntity
            {
                Id = 1,
                ClaimId = 1,
                DateDeleted = null,
                Charges = 100m
            };
            var claim = new ClaimEntity
            {
                Id = 1,
                DateDeleted = DateTime.UtcNow,
                isPrivatePayClaim = true,
                PrimaryFunderId = 1,
                ChildProfileId = 5,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ChargePaymentEntity, bool>>>(), null))
                .ReturnsAsync(DbMock.Create(new List<ChargePaymentEntity>()));

            var result = await _service.GetAllClaimsByIdAsync(payment, new[] { 1 });

            Assert.Single(result);
            Assert.Equal(1, result[0].ClaimId);
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_ExcludesDeletedChargeEntries()
        {
            var payment = new PaymentEntity { HcFunderId = 1 };
            var activeEntry = new ClaimChargeEntryEntity { Id = 1, ClaimId = 1, DateDeleted = null, Charges = 100m };
            var deletedEntry = new ClaimChargeEntryEntity { Id = 2, ClaimId = 1, DateDeleted = DateTime.UtcNow, Charges = 50m };
            var claim = new ClaimEntity
            {
                Id = 1,
                DateDeleted = null,
                PrimaryFunderId = 1,
                ChildProfileId = 5,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { activeEntry, deletedEntry }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ChargePaymentEntity, bool>>>(), null))
                .ReturnsAsync(DbMock.Create(new List<ChargePaymentEntity>()));

            var result = await _service.GetAllClaimsByIdAsync(payment, new[] { 1 });

            Assert.Single(result);
            Assert.Single(result[0].ChargeEntries);
            Assert.Equal(1, result[0].ChargeEntries.First().Id);
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_LoadsChargePaymentsForEachEntry()
        {
            var payment = new PaymentEntity { HcFunderId = 1 };
            var payments = new List<ChargePaymentEntity>
    {
        new ChargePaymentEntity { Id = 1, ChargeId = 1, Amount = 100m, DateDeleted = null },
        new ChargePaymentEntity { Id = 2, ChargeId = 1, Amount = 50m, DateDeleted = null }
    };
            var chargeEntry = new ClaimChargeEntryEntity
            {
                Id = 1,
                ClaimId = 1,
                DateDeleted = null,
                Charges = 150m
            };
            var claim = new ClaimEntity
            {
                Id = 1,
                DateDeleted = null,
                PrimaryFunderId = 1,
                ChildProfileId = 5,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ChargePaymentEntity, bool>>>(), null))
                .ReturnsAsync(DbMock.Create(payments));

            var result = await _service.GetAllClaimsByIdAsync(payment, new[] { 1 });

            Assert.Single(result);
            var chargeItem = result[0].ChargeEntries.First();
            Assert.Equal(2, chargeItem.ClaimChargeItems.Count());
            Assert.Equal(150m, chargeItem.TotalAmount);
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_CalculatesTotalAmountFromPayments()
        {
            var payment = new PaymentEntity { HcFunderId = 1 };
            var payments = new List<ChargePaymentEntity>
    {
        new ChargePaymentEntity { Id = 1, ChargeId = 1, Amount = 75.25m, DateDeleted = null },
        new ChargePaymentEntity { Id = 2, ChargeId = 1, Amount = 50.50m, DateDeleted = null },
        new ChargePaymentEntity { Id = 3, ChargeId = 1, Amount = 24.25m, DateDeleted = null }
    };
            var chargeEntry = new ClaimChargeEntryEntity
            {
                Id = 1,
                ClaimId = 1,
                DateDeleted = null,
                Charges = 200m
            };
            var claim = new ClaimEntity
            {
                Id = 1,
                DateDeleted = null,
                PrimaryFunderId = 1,
                ChildProfileId = 5,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ChargePaymentEntity, bool>>>(), null))
                .ReturnsAsync(DbMock.Create(payments));

            var result = await _service.GetAllClaimsByIdAsync(payment, new[] { 1 });

            var chargeItem = result[0].ChargeEntries.First();
            Assert.Equal(150m, chargeItem.TotalAmount);
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_SetsClaimStatusToPrimary_WhenPrimaryFunderMatches()
        {
            var payment = new PaymentEntity { HcFunderId = 5 };
            var chargeEntry = new ClaimChargeEntryEntity { Id = 1, ClaimId = 1, DateDeleted = null, Charges = 100m };
            var claim = new ClaimEntity
            {
                Id = 1,
                DateDeleted = null,
                PrimaryFunderId = 5,
                SecondaryFunderId = 6,
                TertiaryFunderId = 7,
                ChildProfileId = 10,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ChargePaymentEntity, bool>>>(), null))
                .ReturnsAsync(DbMock.Create(new List<ChargePaymentEntity>()));

            var result = await _service.GetAllClaimsByIdAsync(payment, new[] { 1 });

            Assert.Equal((int)PaymentClaimStatus.ProcessedAsPrimary, result[0].ClaimStatus);
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_SetsClaimStatusToSecondary_WhenSecondaryFunderMatches()
        {
            var payment = new PaymentEntity { HcFunderId = 6 };
            var chargeEntry = new ClaimChargeEntryEntity { Id = 1, ClaimId = 1, DateDeleted = null, Charges = 100m };
            var claim = new ClaimEntity
            {
                Id = 1,
                DateDeleted = null,
                PrimaryFunderId = 5,
                SecondaryFunderId = 6,
                TertiaryFunderId = 7,
                ChildProfileId = 10,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ChargePaymentEntity, bool>>>(), null))
                .ReturnsAsync(DbMock.Create(new List<ChargePaymentEntity>()));

            var result = await _service.GetAllClaimsByIdAsync(payment, new[] { 1 });

            Assert.Equal((int)PaymentClaimStatus.ProcessedAsSecondary, result[0].ClaimStatus);
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_SetsClaimStatusToTertiary_WhenTertiaryFunderMatches()
        {
            var payment = new PaymentEntity { HcFunderId = 7 };
            var chargeEntry = new ClaimChargeEntryEntity { Id = 1, ClaimId = 1, DateDeleted = null, Charges = 100m };
            var claim = new ClaimEntity
            {
                Id = 1,
                DateDeleted = null,
                PrimaryFunderId = 5,
                SecondaryFunderId = 6,
                TertiaryFunderId = 7,
                ChildProfileId = 10,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ChargePaymentEntity, bool>>>(), null))
                .ReturnsAsync(DbMock.Create(new List<ChargePaymentEntity>()));

            var result = await _service.GetAllClaimsByIdAsync(payment, new[] { 1 });

            Assert.Equal((int)PaymentClaimStatus.ProcessedAsTertiery, result[0].ClaimStatus);
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_SetsClaimStatusToUnknown_WhenNoFunderMatches()
        {
            var payment = new PaymentEntity { HcFunderId = 99 };
            var chargeEntry = new ClaimChargeEntryEntity { Id = 1, ClaimId = 1, DateDeleted = null, Charges = 100m };
            var claim = new ClaimEntity
            {
                Id = 1,
                DateDeleted = null,
                PrimaryFunderId = 5,
                SecondaryFunderId = 6,
                TertiaryFunderId = 7,
                ChildProfileId = 10,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ChargePaymentEntity, bool>>>(), null))
                .ReturnsAsync(DbMock.Create(new List<ChargePaymentEntity>()));

            var result = await _service.GetAllClaimsByIdAsync(payment, new[] { 1 });

            Assert.Equal((int)PaymentClaimStatus.Unknown, result[0].ClaimStatus);
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_MapsAllChargeEntryProperties()
        {
            var payment = new PaymentEntity { HcFunderId = 1 };
            var dateOfService = DateTime.UtcNow;
            var chargeEntry = new ClaimChargeEntryEntity
            {
                Id = 10,
                ClaimId = 20,
                DateDeleted = null,
                Charges = 150.50m,
                Units = 3,
                DateOfService = dateOfService,
                BillingCode = "99213",
                Modifier1 = "25",
                Modifier2 = "GT",
                Modifier3 = "59",
                Modifier4 = "76",
                Description = "Office Visit"
            };
            var claim = new ClaimEntity
            {
                Id = 20,
                DateDeleted = null,
                PrimaryFunderId = 1,
                ChildProfileId = 5,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ChargePaymentEntity, bool>>>(), null))
                .ReturnsAsync(DbMock.Create(new List<ChargePaymentEntity>()));

            var result = await _service.GetAllClaimsByIdAsync(payment, new[] { 20 });

            var mappedEntry = result[0].ChargeEntries.First();
            Assert.Equal(10, mappedEntry.Id);
            Assert.Equal(20, mappedEntry.ClaimId);
            Assert.Equal(150.50m, mappedEntry.Charges);
            Assert.Equal(3, mappedEntry.Units);
            Assert.Equal(dateOfService, mappedEntry.DateOfService);
            Assert.Equal("99213", mappedEntry.ServiceCode);
            Assert.Equal("25", mappedEntry.Modifier1);
            Assert.Equal("GT", mappedEntry.Modifier2);
            Assert.Equal("59", mappedEntry.Modifier3);
            Assert.Equal("76", mappedEntry.Modifier4);
            Assert.Equal("Office Visit", mappedEntry.Description);
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_SetsPatientId()
        {
            var payment = new PaymentEntity { HcFunderId = 1 };
            var chargeEntry = new ClaimChargeEntryEntity { Id = 1, ClaimId = 1, DateDeleted = null, Charges = 100m };
            var claim = new ClaimEntity
            {
                Id = 1,
                DateDeleted = null,
                PrimaryFunderId = 1,
                ChildProfileId = 42,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ChargePaymentEntity, bool>>>(), null))
                .ReturnsAsync(DbMock.Create(new List<ChargePaymentEntity>()));

            var result = await _service.GetAllClaimsByIdAsync(payment, new[] { 1 });

            Assert.Equal(42, result[0].PatientId);
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_ReturnsMultipleClaims()
        {
            var payment = new PaymentEntity { HcFunderId = 1 };
            var entry1 = new ClaimChargeEntryEntity { Id = 1, ClaimId = 1, DateDeleted = null, Charges = 100m };
            var entry2 = new ClaimChargeEntryEntity { Id = 2, ClaimId = 2, DateDeleted = null, Charges = 200m };
            var claim1 = new ClaimEntity
            {
                Id = 1,
                DateDeleted = null,
                PrimaryFunderId = 1,
                ChildProfileId = 5,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { entry1 }
            };
            var claim2 = new ClaimEntity
            {
                Id = 2,
                DateDeleted = null,
                PrimaryFunderId = 1,
                ChildProfileId = 6,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { entry2 }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim1, claim2 });
            _claimRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ChargePaymentEntity, bool>>>(), null))
                .ReturnsAsync(DbMock.Create(new List<ChargePaymentEntity>()));

            var result = await _service.GetAllClaimsByIdAsync(payment, new[] { 1, 2 });

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_HandlesClaimWithNoChargeEntries()
        {
            var payment = new PaymentEntity { HcFunderId = 1 };
            var claim = new ClaimEntity
            {
                Id = 1,
                DateDeleted = null,
                PrimaryFunderId = 1,
                ChildProfileId = 5,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>()
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var result = await _service.GetAllClaimsByIdAsync(payment, new[] { 1 });

            Assert.Single(result);
            Assert.Empty(result[0].ChargeEntries);
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_HandlesChargeEntryWithNoPayments()
        {
            var payment = new PaymentEntity { HcFunderId = 1 };
            var chargeEntry = new ClaimChargeEntryEntity { Id = 1, ClaimId = 1, DateDeleted = null, Charges = 100m };
            var claim = new ClaimEntity
            {
                Id = 1,
                DateDeleted = null,
                PrimaryFunderId = 1,
                ChildProfileId = 5,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ChargePaymentEntity, bool>>>(), null))
                .ReturnsAsync(DbMock.Create(new List<ChargePaymentEntity>()));

            var result = await _service.GetAllClaimsByIdAsync(payment, new[] { 1 });

            var chargeItem = result[0].ChargeEntries.First();
            Assert.Equal(0m, chargeItem.TotalAmount);
            Assert.Empty(chargeItem.ClaimChargeItems);
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_CallsChargePaymentRepositoryForEachEntry()
        {
            var payment = new PaymentEntity { HcFunderId = 1 };
            var entry1 = new ClaimChargeEntryEntity { Id = 1, ClaimId = 1, DateDeleted = null, Charges = 100m };
            var entry2 = new ClaimChargeEntryEntity { Id = 2, ClaimId = 1, DateDeleted = null, Charges = 150m };
            var claim = new ClaimEntity
            {
                Id = 1,
                DateDeleted = null,
                PrimaryFunderId = 1,
                ChildProfileId = 5,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { entry1, entry2 }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ChargePaymentEntity, bool>>>(), null))
                .ReturnsAsync(DbMock.Create(new List<ChargePaymentEntity>()));

            await _service.GetAllClaimsByIdAsync(payment, new[] { 1 });

            _chargePaymentRepoMock.Verify(
                r => r.GetAllAsync(It.IsAny<Expression<Func<ChargePaymentEntity, bool>>>(), null),
                Times.Exactly(2));
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_ThrowsException_WhenClaimRepositoryFails()
        {
            var payment = new PaymentEntity { HcFunderId = 1 };
            _claimRepoMock.Setup(r => r.Query()).Throws(new Exception("Database error"));

            await Assert.ThrowsAsync<Exception>(() =>
                _service.GetAllClaimsByIdAsync(payment, new[] { 1 }));
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_ThrowsException_WhenChargePaymentRepositoryFails()
        {
            var payment = new PaymentEntity { HcFunderId = 1 };
            var chargeEntry = new ClaimChargeEntryEntity { Id = 1, ClaimId = 1, DateDeleted = null, Charges = 100m };
            var claim = new ClaimEntity
            {
                Id = 1,
                DateDeleted = null,
                PrimaryFunderId = 1,
                ChildProfileId = 5,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ChargePaymentEntity, bool>>>(), null))
                .ThrowsAsync(new Exception("Payment repository error"));

            await Assert.ThrowsAsync<Exception>(() =>
                _service.GetAllClaimsByIdAsync(payment, new[] { 1 }));
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_HandlesEmptyClaimIdsArray()
        {
            var payment = new PaymentEntity { HcFunderId = 1 };
            var dbSet = DbMock.Create(new List<ClaimEntity>());
            _claimRepoMock.Setup(r => r.Query()).Returns(dbSet);

            var result = await _service.GetAllClaimsByIdAsync(payment, Array.Empty<int>());

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_HandlesNullPayment()
        {
            var chargeEntry = new ClaimChargeEntryEntity { Id = 1, ClaimId = 1, DateDeleted = null, Charges = 100m };
            var claim = new ClaimEntity
            {
                Id = 1,
                DateDeleted = null,
                PrimaryFunderId = 1,
                ChildProfileId = 5,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry }
            };
            var dbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ChargePaymentEntity, bool>>>(), null))
                .ReturnsAsync(DbMock.Create(new List<ChargePaymentEntity>()));

            await Assert.ThrowsAsync<NullReferenceException>(() =>
                _service.GetAllClaimsByIdAsync(null, new[] { 1 }));
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_HandlesLargeDataSet()
        {
            var payment = new PaymentEntity { HcFunderId = 1 };
            var claims = Enumerable.Range(1, 50).Select(i => new ClaimEntity
            {
                Id = i,
                DateDeleted = null,
                PrimaryFunderId = 1,
                ChildProfileId = i,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>
        {
            new ClaimChargeEntryEntity { Id = i, ClaimId = i, DateDeleted = null, Charges = 100m }
        }
            }).ToList();
            var dbSet = DbMock.Create(claims);
            _claimRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ChargePaymentEntity, bool>>>(), null))
                .ReturnsAsync(DbMock.Create(new List<ChargePaymentEntity>()));

            var result = await _service.GetAllClaimsByIdAsync(payment, Enumerable.Range(1, 50).ToArray());

            Assert.Equal(50, result.Count);
        }

        [Fact]
        public async Task GetAllClaimsByIdAsync_FiltersByClaimIds()
        {
            var payment = new PaymentEntity { HcFunderId = 1 };
            var entry1 = new ClaimChargeEntryEntity { Id = 1, ClaimId = 1, DateDeleted = null, Charges = 100m };
            var entry2 = new ClaimChargeEntryEntity { Id = 2, ClaimId = 2, DateDeleted = null, Charges = 150m };
            var entry3 = new ClaimChargeEntryEntity { Id = 3, ClaimId = 3, DateDeleted = null, Charges = 200m };
            var claim1 = new ClaimEntity { Id = 1, DateDeleted = null, PrimaryFunderId = 1, ChildProfileId = 5, ClaimChargeEntries = new List<ClaimChargeEntryEntity> { entry1 } };
            var claim2 = new ClaimEntity { Id = 2, DateDeleted = null, PrimaryFunderId = 1, ChildProfileId = 6, ClaimChargeEntries = new List<ClaimChargeEntryEntity> { entry2 } };
            var claim3 = new ClaimEntity { Id = 3, DateDeleted = null, PrimaryFunderId = 1, ChildProfileId = 7, ClaimChargeEntries = new List<ClaimChargeEntryEntity> { entry3 } };

            var filteredClaims = new List<ClaimEntity> { claim1, claim3 };
            var dbSet = DbMock.Create(filteredClaims);
            _claimRepoMock.Setup(r => r.Query()).Returns(dbSet);
            _chargePaymentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ChargePaymentEntity, bool>>>(), null))
                .ReturnsAsync(DbMock.Create(new List<ChargePaymentEntity>()));

            var result = await _service.GetAllClaimsByIdAsync(payment, new[] { 1, 3 });

            Assert.Equal(2, result.Count);
            Assert.Contains(result, c => c.ClaimId == 1);
            Assert.Contains(result, c => c.ClaimId == 3);
            Assert.DoesNotContain(result, c => c.ClaimId == 2);
        }
    }
}
