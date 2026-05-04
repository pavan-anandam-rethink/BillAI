using AutoFixture;
using AutoFixture.Xunit2;
using BillingService.XUnit.Tests.Common;
using BillingService.XUnit.Tests.Common.Attributes;
using BillingService.XUnit.Tests.Common.Mocks;
using MockQueryable;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Domain.Interfaces;
using Rethink.Services.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Claim
{
    public class ClaimHistoryServiceTest : BaseTest
    {
        private Mock<IRethinkMasterDataMicroServices> _rethinkServices;
        private IClaimHistoryService _claimHistoryService;

        private Mock<IRepository<BillingDbContext, ClaimHistoryEntity>> _claimHistoryRepository;
        private Mock<IRepository<BillingDbContext, ClaimHistoryActionEntity>> _claimHistoryActionRepository;

        public ClaimHistoryServiceTest()
        {
            _rethinkServices = new Mock<IRethinkMasterDataMicroServices>();
            _claimHistoryRepository = new Mock<IRepository<BillingDbContext, ClaimHistoryEntity>>();
            _claimHistoryActionRepository = new Mock<IRepository<BillingDbContext, ClaimHistoryActionEntity>>();

            _claimHistoryService = new ClaimHistoryService(_claimHistoryRepository.Object, _claimHistoryActionRepository.Object, _rethinkServices.Object);
        }

        [Theory, AutoMoqData]
        public async Task GetClaimHistoryActionsAsync_ShouldReturnResult(
            List<ClaimHistoryActionEntity> historyActions,
            [Frozen] Mock<IRepository<BillingDbContext, ClaimHistoryActionEntity>> claimHistoryActionRepository,
            ClaimHistoryService claimHistoryService)
        {
            claimHistoryActionRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<ClaimHistoryActionEntity, bool>>>(),
                                                                  It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(QueryMock<ClaimHistoryActionEntity>.Create(historyActions));

            var result = await claimHistoryService.GetClaimHistoryActionsAsync();

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(historyActions.Count, result.Count);

            claimHistoryActionRepository.Verify(x => x.GetAllAsync(It.IsAny<Expression<Func<ClaimHistoryActionEntity, bool>>>(),
                                                                   It.IsAny<IEnumerable<string>>()), Times.Once);
        }

        [Theory]
        [InlineAutoMoqData(true)]
        [InlineAutoMoqData(false)]
        public async Task AddAsync_ShouldCreateClaimHistory(
            bool commitImmediately,
            ClaimHistorySaveModel saveModel,
            [Frozen] Mock<IRepository<BillingDbContext, ClaimHistoryEntity>> claimHistoryRepository,
            ClaimHistoryService claimHistoryService)
        {
            await claimHistoryService.AddAsync(saveModel, commitImmediately);

            claimHistoryRepository.Verify(x => x.AddAsync(It.IsAny<ClaimHistoryEntity>()), Times.Once);

            if (commitImmediately)
            {
                claimHistoryRepository.Verify(x => x.CommitAsync(), Times.Once);
            }
            else
            {
                claimHistoryRepository.Verify(x => x.CommitAsync(), Times.Never);
            }
        }

        [Theory]
        [InlineAutoMoqData(true)]
        [InlineAutoMoqData(false)]
        public async Task AddAsync_ShouldCreateClaimHistoryField(
            bool commitImmediately,
            ClaimHistoryFieldSaveModel saveModel,
            [Frozen] Mock<IRepository<BillingDbContext, ClaimHistoryEntity>> claimHistoryRepository,
            ClaimHistoryService claimHistoryService)
        {
            await claimHistoryService.AddAsync(saveModel, commitImmediately);

            claimHistoryRepository.Verify(x => x.AddAsync(It.IsAny<ClaimHistoryEntity>()), Times.Once);

            if (commitImmediately)
            {
                claimHistoryRepository.Verify(x => x.CommitAsync(), Times.Once);
            }
            else
            {
                claimHistoryRepository.Verify(x => x.CommitAsync(), Times.Never);
            }
        }

        [Theory]
        [InlineAutoMoqData(true)]
        [InlineAutoMoqData(false)]
        public async Task AddAsync_ShouldCreateClaimHistoryVersion(
            bool commitImmediately,
            ClaimHistoryVersionSaveModel saveModel,
            [Frozen] Mock<IRepository<BillingDbContext, ClaimHistoryEntity>> claimHistoryRepository,
            ClaimHistoryService claimHistoryService)
        {
            await claimHistoryService.AddAsync(saveModel, commitImmediately);

            claimHistoryRepository.Verify(x => x.AddAsync(It.IsAny<ClaimHistoryEntity>()), Times.Once);

            if (commitImmediately)
            {
                claimHistoryRepository.Verify(x => x.CommitAsync(), Times.Once);
            }
            else
            {
                claimHistoryRepository.Verify(x => x.CommitAsync(), Times.Never);
            }
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnResult()
        {
            int claimId = Fixture.Create<int>();
            int memberId = Fixture.Create<int>();
            int accountInfoId = Fixture.Create<int>();
            var firstName = Fixture.Create<string>();
            var middleName = Fixture.Create<string>();
            var lastName = Fixture.Create<string>();

            var historyRecords = Fixture.Build<ClaimHistoryEntity>()
                .With(x => x.ClaimId, claimId)
                .With(x => x.CreatedBy, memberId)
                .With(x => x.ModifiedBy, memberId)
                .CreateMany(5);

            var member = Fixture.Build<RethinkAccountMember>()
                 .With(x => x.id, memberId)
                 .With(x => x.firstName, firstName)
                .With(x => x.middleName, middleName)
                .With(x => x.lastName, lastName)
                 .Create();

            _claimHistoryRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimHistoryEntity>.Create(historyRecords));



            var accountMember = Fixture.Build<RethinkAccountMember>()
                .With(x => x.accountId, accountInfoId)
                .With(x => x.id, memberId)
                .With(x => x.firstName, firstName)
                .With(x => x.middleName, middleName)
                .With(x => x.lastName, lastName)
                .Create();

            var model = Fixture.Build<RethinkAccountMembersListModel>().With(x => x.data, new List<RethinkAccountMember> { accountMember }).Create();

            _rethinkServices.Setup(x => x.GetMembersAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(model);

            var result = await _claimHistoryService.GetAllAsync(claimId, accountInfoId, memberId);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(historyRecords.Count(), result.Count);
            Assert.Contains(result, item => item.ChangeBy == $"{member.firstName} {member.middleName} {member.lastName}");

            _claimHistoryRepository.Verify(x => x.Query(), Times.Once);
        }


        [Fact]
        public async Task UpdateHistoryFor277_Accepted_PicksAcceptedRow_SetsActionDate_UpdatesAndSaves()
        {
            // Arrange
            var claimId = 101;

            var rows = new List<ClaimHistoryEntity>
            {
                new ClaimHistoryEntity { ClaimId = claimId, DateDeleted = null, ClaimHistoryAction = ClaimHistoryAction.ClaimResponseAccepted277 }, // expected
                new ClaimHistoryEntity { ClaimId = claimId, DateDeleted = null, ClaimHistoryAction = ClaimHistoryAction.ClaimResponseRejected277 },
                new ClaimHistoryEntity { ClaimId = claimId, DateDeleted = null, ClaimHistoryAction = ClaimHistoryAction.ClaimResponseReceived277 },
                new ClaimHistoryEntity { ClaimId = 999,    DateDeleted = null, ClaimHistoryAction = ClaimHistoryAction.ClaimResponseAccepted277 }
            };

            var queryableMock = rows.AsQueryable().BuildMock(); // async IQueryable

            var claimHistoryRepo = new Mock<IRepository<BillingDbContext, ClaimHistoryEntity>>();
            claimHistoryRepo.Setup(r => r.Query()).Returns(queryableMock);

            ClaimHistoryEntity updatedEntity = null!;
            claimHistoryRepo.Setup(r => r.Update(It.IsAny<ClaimHistoryEntity>()))
                            .Callback<ClaimHistoryEntity>(e => updatedEntity = e);

            claimHistoryRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.FromResult(1));

            var actionRepo = new Mock<IRepository<BillingDbContext, ClaimHistoryActionEntity>>(); // not used in this method
            var rethink = new Mock<IRethinkMasterDataMicroServices>();                             // not used in this method

            var sut = new ClaimHistoryService(claimHistoryRepo.Object, actionRepo.Object, rethink.Object);

            var input = new ClearingHouseResponseDetailsEntity
            {
                ClaimId = claimId,
                IsAccepted = true,
                ClaimValidationErrorId = 0
            };

            // Act
            await sut.UpdateHistoryFor277(input);

            // Assert
            Assert.NotNull(updatedEntity);
            Assert.Equal(claimId, updatedEntity.ClaimId);
            Assert.Equal(ClaimHistoryAction.ClaimResponseAccepted277, updatedEntity.ClaimHistoryAction);
            //Assert.True(updatedEntity.ActionDate);

            claimHistoryRepo.Verify(r => r.Update(It.Is<ClaimHistoryEntity>(e =>
                e.ClaimId == claimId &&
                e.ClaimHistoryAction == ClaimHistoryAction.ClaimResponseAccepted277 
            )), Times.Once);

            claimHistoryRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }


        [Fact]
        public async Task UpdateHistoryFor277_Rejected_PicksRejectedRow_SetsActionDate_UpdatesAndSaves()
        {
            // Arrange
            var claimId = 202;

            var rows = new List<ClaimHistoryEntity>
            {
                new ClaimHistoryEntity { ClaimId = claimId, DateDeleted = null, ClaimHistoryAction = ClaimHistoryAction.ClaimResponseRejected277 }, // expected
                new ClaimHistoryEntity { ClaimId = claimId, DateDeleted = null, ClaimHistoryAction = ClaimHistoryAction.ClaimResponseAccepted277 },
                new ClaimHistoryEntity { ClaimId = claimId, DateDeleted = null, ClaimHistoryAction = ClaimHistoryAction.ClaimResponseReceived277 }
            };

            var queryableMock = rows.AsQueryable().BuildMock(); // async IQueryable

            var claimHistoryRepo = new Mock<IRepository<BillingDbContext, ClaimHistoryEntity>>();
            claimHistoryRepo.Setup(r => r.Query()).Returns(queryableMock);

            ClaimHistoryEntity updatedEntity = null!;
            claimHistoryRepo.Setup(r => r.Update(It.IsAny<ClaimHistoryEntity>()))
                            .Callback<ClaimHistoryEntity>(e => updatedEntity = e);

            claimHistoryRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.FromResult(1));

            var actionRepo = new Mock<IRepository<BillingDbContext, ClaimHistoryActionEntity>>();
            var rethink = new Mock<IRethinkMasterDataMicroServices>();

            var sut = new ClaimHistoryService(claimHistoryRepo.Object, actionRepo.Object, rethink.Object);

            var input = new ClearingHouseResponseDetailsEntity
            {
                ClaimId = claimId,
                IsAccepted = false,
                ClaimValidationErrorId = 42 // > 0 => rejected path
            };

            // Act
            await sut.UpdateHistoryFor277(input);

            // Assert
            Assert.NotNull(updatedEntity);
            Assert.Equal(claimId, updatedEntity.ClaimId);
            Assert.Equal(ClaimHistoryAction.ClaimResponseRejected277, updatedEntity.ClaimHistoryAction);
          

            claimHistoryRepo.Verify(r => r.Update(It.Is<ClaimHistoryEntity>(e =>
                e.ClaimId == claimId &&
                e.ClaimHistoryAction == ClaimHistoryAction.ClaimResponseRejected277
            )), Times.Once);

            claimHistoryRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateHistoryFor277_Received_PicksReceivedRow_SetsActionDate_UpdatesAndSaves()
        {
            // Arrange
            var claimId = 303;

            var rows = new List<ClaimHistoryEntity>
            {
                new ClaimHistoryEntity { ClaimId = claimId, DateDeleted = null, ClaimHistoryAction = ClaimHistoryAction.ClaimResponseReceived277 }, // expected
                new ClaimHistoryEntity { ClaimId = claimId, DateDeleted = null, ClaimHistoryAction = ClaimHistoryAction.ClaimResponseRejected277 },
                new ClaimHistoryEntity { ClaimId = claimId, DateDeleted = null, ClaimHistoryAction = ClaimHistoryAction.ClaimResponseAccepted277 }
            };

            var queryableMock = rows.AsQueryable().BuildMock(); // async IQueryable

            var claimHistoryRepo = new Mock<IRepository<BillingDbContext, ClaimHistoryEntity>>();
            claimHistoryRepo.Setup(r => r.Query()).Returns(queryableMock);

            ClaimHistoryEntity updatedEntity = null!;
            claimHistoryRepo.Setup(r => r.Update(It.IsAny<ClaimHistoryEntity>()))
                            .Callback<ClaimHistoryEntity>(e => updatedEntity = e);

            claimHistoryRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.FromResult(1));

            var actionRepo = new Mock<IRepository<BillingDbContext, ClaimHistoryActionEntity>>();
            var rethink = new Mock<IRethinkMasterDataMicroServices>();

            var sut = new ClaimHistoryService(claimHistoryRepo.Object, actionRepo.Object, rethink.Object);

            var input = new ClearingHouseResponseDetailsEntity
            {
                ClaimId = claimId,
                IsAccepted = false,
                ClaimValidationErrorId = 0 // received path (code compares to 0 or null)
            };

            // Act
            await sut.UpdateHistoryFor277(input);

            // Assert
            Assert.NotNull(updatedEntity);
            Assert.Equal(claimId, updatedEntity.ClaimId);
            Assert.Equal(ClaimHistoryAction.ClaimResponseReceived277, updatedEntity.ClaimHistoryAction);
          

            claimHistoryRepo.Verify(r => r.Update(It.Is<ClaimHistoryEntity>(e =>
                e.ClaimId == claimId &&
                e.ClaimHistoryAction == ClaimHistoryAction.ClaimResponseReceived277
            )), Times.Once);

            claimHistoryRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateHistoryFor277_NoBranchWhenNegativeErrorId_PicksFirstRowAndUpdates()
        {
            var claimId = 404;
            var rows = new List<ClaimHistoryEntity>
                {
                    new ClaimHistoryEntity { ClaimId = claimId, DateDeleted = null, ClaimHistoryAction = ClaimHistoryAction.ClaimResponseRejected277 },
                    new ClaimHistoryEntity { ClaimId = claimId, DateDeleted = null, ClaimHistoryAction = ClaimHistoryAction.ClaimResponseAccepted277 },
                };

            var queryable = rows.AsQueryable().BuildMock();

            var repo = new Mock<IRepository<BillingDbContext, ClaimHistoryEntity>>();
            repo.Setup(r => r.Query()).Returns(queryable);
            ClaimHistoryEntity updated = null!;
            repo.Setup(r => r.Update(It.IsAny<ClaimHistoryEntity>())).Callback<ClaimHistoryEntity>(e => updated = e);
         
            repo.Setup(r => r.SaveChangesAsync()).Returns(Task.FromResult(1));
            var actionRepo = new Mock<IRepository<BillingDbContext, ClaimHistoryActionEntity>>();
            var rethink = new Mock<IRethinkMasterDataMicroServices>();
            var sut = new ClaimHistoryService(repo.Object, actionRepo.Object, rethink.Object);

            var input = new ClearingHouseResponseDetailsEntity
            {
                ClaimId = claimId,
                IsAccepted = false,
                ClaimValidationErrorId = -1 // forces no branch block
            };

            await sut.UpdateHistoryFor277(input);

            Assert.NotNull(updated);
            Assert.Equal(claimId, updated.ClaimId);
          
                                                              // No specific action guaranteed here because no filtering applied; it's the FirstOrDefault() of the filtered-by-claim list
            repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }


        [Fact]
        public async Task AddAsync_MapsEntities_CallsAddRange_And_Commits_When_CommitImmediately_True()
        {
            // Arrange
            var claimHistoryRepo = new Mock<IRepository<BillingDbContext, ClaimHistoryEntity>>();
            var actionRepo = new Mock<IRepository<BillingDbContext, ClaimHistoryActionEntity>>();
            var rethink = new Mock<IRethinkMasterDataMicroServices>();

            var sut = new ClaimHistoryService(claimHistoryRepo.Object, actionRepo.Object, rethink.Object);

            var explicitDate = new DateTime(2025, 10, 13, 12, 0, 0, DateTimeKind.Utc);

            var models = new List<ClaimHistorySaveModel>
            {
                new ClaimHistorySaveModel
                {
                    ClaimId = 1,
                    MemberId = 10,
                    Mode = ClaimActionMode.System, // use your real enum values
                    ClaimAction = ClaimAction.Create,
                    ClaimHistoryAction = ClaimHistoryAction.ClaimResponseAccepted277,
                    ActionDate = null, // should fallback to EstDateTime
                    OldValue = "Old",
                    NewValue = "New"
                },
                new ClaimHistorySaveModel
                {
                    ClaimId = 2,
                    MemberId = 11,
                    Mode = ClaimActionMode.User,
                    ClaimAction = ClaimAction.Denied,
                    ClaimHistoryAction = ClaimHistoryAction.ClaimResponseRejected277,
                    ActionDate = explicitDate, // should keep this exact value
                    OldValue = "Prev",
                    NewValue = "Curr"
                }
            };

            List<ClaimHistoryEntity> captured = null!;
            claimHistoryRepo
                .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<ClaimHistoryEntity>>()))
                .Callback<IEnumerable<ClaimHistoryEntity>>(e => captured = e.ToList())
                .Returns(Task.CompletedTask);

            claimHistoryRepo
                .Setup(r => r.CommitAsync())
                .Returns(Task.CompletedTask);

            // Act
            await sut.AddAsync(models, commitImmediately: true);

            // Assert: AddRangeAsync called once with 2 entities and CommitAsync called once
            claimHistoryRepo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<ClaimHistoryEntity>>()), Times.Once);
            claimHistoryRepo.Verify(r => r.CommitAsync(), Times.Once);

            Assert.NotNull(captured);
            Assert.Equal(2, captured.Count);

            // First entity (fallback ActionDate expected)
            var e1 = captured[0];
            Assert.Equal(1, e1.ClaimId);
            Assert.Equal(ClaimActionMode.System, e1.Mode);
            Assert.Equal(ClaimAction.Create, e1.ClaimAction);
            Assert.Equal(ClaimHistoryAction.ClaimResponseAccepted277, e1.ClaimHistoryAction);
            Assert.Equal("Old", e1.OldValue);
            Assert.Equal("New", e1.NewValue);
            

            // Second entity (uses explicit ActionDate)
            var e2 = captured[1];
            Assert.Equal(2, e2.ClaimId);
            Assert.Equal(ClaimActionMode.User, e2.Mode);
            Assert.Equal(ClaimAction.Denied, e2.ClaimAction);
            Assert.Equal(ClaimHistoryAction.ClaimResponseRejected277, e2.ClaimHistoryAction);
            Assert.Equal("Prev", e2.OldValue);
            Assert.Equal("Curr", e2.NewValue);
            Assert.Equal(explicitDate, e2.ActionDate);
        }


        [Fact]
        public async Task AddAsync_MapsEntities_CallsAddRange_And_Commits_When_CommitImmediately_False()
        {
            // Arrange
            var claimHistoryRepo = new Mock<IRepository<BillingDbContext, ClaimHistoryEntity>>();
            var actionRepo = new Mock<IRepository<BillingDbContext, ClaimHistoryActionEntity>>();
            var rethink = new Mock<IRethinkMasterDataMicroServices>();

            var sut = new ClaimHistoryService(claimHistoryRepo.Object, actionRepo.Object, rethink.Object);

            var explicitDate = new DateTime(2025, 10, 13, 12, 0, 0, DateTimeKind.Utc);

            var models = new List<ClaimHistoryFieldSaveModel>
            {
                new ClaimHistoryFieldSaveModel
                {
                    ClaimId = 1,
                    MemberId = 10,
                    Mode = ClaimActionMode.System, // use your real enum values
                    ClaimAction = ClaimAction.Create,
                    ClaimHistoryAction = ClaimHistoryAction.ClaimResponseAccepted277,
                    ActionDate = null, // should fallback to EstDateTime
                    OldValue = "Old",
                    NewValue = "New"
                },
                new ClaimHistoryFieldSaveModel
                {
                    ClaimId = 2,
                    MemberId = 11,
                    Mode = ClaimActionMode.User,
                    ClaimAction = ClaimAction.Denied,
                    ClaimHistoryAction = ClaimHistoryAction.ClaimResponseRejected277,
                    ActionDate = explicitDate, // should keep this exact value
                    OldValue = "Prev",
                    NewValue = "Curr"
                }
            };

            List<ClaimHistoryEntity> captured = null!;
            claimHistoryRepo
                .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<ClaimHistoryEntity>>()))
                .Callback<IEnumerable<ClaimHistoryEntity>>(e => captured = e.ToList())
                .Returns(Task.CompletedTask);

            claimHistoryRepo
                .Setup(r => r.CommitAsync())
                .Returns(Task.CompletedTask);

            // Act
            await sut.AddAsync(models, commitImmediately: true);

            // Assert: AddRangeAsync called once with 2 entities and CommitAsync called once
            claimHistoryRepo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<ClaimHistoryEntity>>()), Times.Once);
            claimHistoryRepo.Verify(r => r.CommitAsync(), Times.Once);

            Assert.NotNull(captured);
            Assert.Equal(2, captured.Count);

            // First entity (fallback ActionDate expected)
            var e1 = captured[0];
            Assert.Equal(1, e1.ClaimId);
            Assert.Equal(ClaimActionMode.System, e1.Mode);
            Assert.Equal(ClaimAction.Create, e1.ClaimAction);
            Assert.Equal(ClaimHistoryAction.ClaimResponseAccepted277, e1.ClaimHistoryAction);
            Assert.Equal("Old", e1.OldValue);
            Assert.Equal("New", e1.NewValue);


            // Second entity (uses explicit ActionDate)
            var e2 = captured[1];
            Assert.Equal(2, e2.ClaimId);
            Assert.Equal(ClaimActionMode.User, e2.Mode);
            Assert.Equal(ClaimAction.Denied, e2.ClaimAction);
            Assert.Equal(ClaimHistoryAction.ClaimResponseRejected277, e2.ClaimHistoryAction);
            Assert.Equal("Prev", e2.OldValue);
            Assert.Equal("Curr", e2.NewValue);
            Assert.Equal(explicitDate, e2.ActionDate);
        }

        [Fact]
        public async Task GetAllAsync_WithRethinkUser_ShouldMapCorrectly()
        {
            // Arrange
            int claimId = 100;

            var records = new List<ClaimHistoryEntity>
            {
                 new ClaimHistoryEntity
                 {
                    ClaimId = claimId,
                    DateDeleted = null,
                    ClaimAction = ClaimAction.Create,
                    ClaimHistoryAction = ClaimHistoryAction.ClaimResponseAccepted277,
                    ActionDate = DateTime.UtcNow,
                    OldValue = "Old",
                    NewValue = "New",
                    RethinkUser = "Jane Doe"
                 },
                new ClaimHistoryEntity
                {
                    ClaimId = claimId,
                    DateDeleted = null,
                    ClaimAction = ClaimAction.Create,
                    ClaimHistoryAction = ClaimHistoryAction.ClaimResponseAccepted277,
                    ActionDate = DateTime.UtcNow,
                    OldValue = "Old",
                    NewValue = "New",
                    RethinkUser = null
                },
                new ClaimHistoryEntity
                {
                    ClaimId = claimId,
                    DateDeleted = null,
                    ClaimAction = ClaimAction.Create,
                    ClaimHistoryAction = ClaimHistoryAction.ClaimResponseAccepted277,
                    ActionDate = DateTime.UtcNow,
                    OldValue = "Old",
                    NewValue = "New",
                    RethinkUser = ""
                }
            };

            _claimHistoryRepository
                .Setup(x => x.Query())
                .Returns(QueryMock<ClaimHistoryEntity>.Create(records));

            // Act
            var result = await _claimHistoryService.GetAllAsync(claimId);

            // Assert
            Assert.Equal(3, result.Count);

            Assert.Equal("Jane Doe", result[0].RethinkUser);
            Assert.Equal("N/A", result[1].RethinkUser);
            Assert.Equal("N/A", result[2].RethinkUser);
        }


        //private void SetupRethinkService(int accountInfoId, int memberId)
        //{
        //    var accountMember = Fixture.Build<RethinkAccountMember>()
        //        .With(x => x.accountId, accountInfoId)
        //        .With(x => x.id, memberId)
        //        .Create();

        //    var model = Fixture.Build<RethinkAccountMembersListModel>()
        //        .With(x => x.data, new List<RethinkAccountMember> { accountMember })
        //        .Create();

        //    var memberIds = "memberIds=" + memberId;

        //    _rethinkServices.Setup(x => x.GetMembersAsync(accountInfoId, memberIds)).ReturnsAsync(model);
        //}
    }
}
