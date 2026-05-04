using AutoFixture;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Services.Billing;
using BillingService.XUnit.Tests.Common;
using BillingService.XUnit.Tests.Common.Mocks;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Claim
{
    public class ClaimNoteServiceTest : BaseTest
    {
        private Mock<IRepository<BillingDbContext, ClaimNoteEntity>> _claimNoteRepository;
        private Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>> _claimChargeEntryRepository;
        private Mock<IRepository<BillingDbContext, ClaimEntity>> _claimRepository;
        private Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>> _paymentClaimServiceLineRepository;
        private Mock<IRepository<BillingDbContext, PaymentClaimEntity>> _paymentClaimRepository;
        private Mock<IRethinkMasterDataMicroServices> _rethinkServices;
        private Mock<IClaimHistoryService> _claimHistoryService;
        private IClaimNoteService _claimNoteService;

        public ClaimNoteServiceTest()
        {
            _claimNoteRepository = new Mock<IRepository<BillingDbContext, ClaimNoteEntity>>();
            _claimChargeEntryRepository = new Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>>();
            _claimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            _paymentClaimServiceLineRepository = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>>();
            _paymentClaimRepository = new Mock<IRepository<BillingDbContext, PaymentClaimEntity>>();
            _rethinkServices = new Mock<IRethinkMasterDataMicroServices>();
            _claimHistoryService = new Mock<IClaimHistoryService>();

            _claimNoteService = new ClaimNoteService(_claimNoteRepository.Object,
                _paymentClaimRepository.Object,
                _rethinkServices.Object, _claimHistoryService.Object);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnNotesForSelectedCaim()
        {
            var getAllModel = Fixture.Create<ClaimNoteGetAllModel>();
            var memberId = Fixture.Create<int>();
            var accountInfoId = Fixture.Create<int>();
            var firstName = Fixture.Create<string>();
            var middleName = Fixture.Create<string>();
            var lastName = Fixture.Create<string>();

            var claimNote = Fixture.Build<ClaimNoteEntity>()
                .With(x => x.ClaimId, getAllModel.Id)
                .With(x => x.CreatedBy, memberId)
                .With(x => x.ModifiedBy, memberId)
                .Create();

            var member = Fixture.Build<RethinkAccountMember>()
                .With(x => x.id, memberId)
                .With(x => x.firstName, firstName)
                .With(x => x.middleName, middleName)
                .With(x => x.lastName, lastName)
                .Create();

            _claimNoteRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimNoteEntity>.Create(claimNote));

            var accountMember = Fixture.Build<RethinkAccountMember>()
                .With(x => x.accountId, accountInfoId)
                .With(x => x.id, memberId)
                .With(x => x.firstName, firstName)
                .With(x => x.middleName, middleName)
                .With(x => x.lastName, lastName)
                .Create();

            var model = Fixture.Build<RethinkAccountMembersListModel>().With(x => x.data, new List<RethinkAccountMember> { accountMember }).Create();
            _rethinkServices.Setup(x => x.GetMembersAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(model);

            var result = await _claimNoteService.GetAllAsync(getAllModel);

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Single((List<ClaimNote>)result.Data);
            Assert.Collection((List<ClaimNote>)result.Data, note => Assert.Equal($"{member.firstName} {member.lastName}", note.CreatedByName));

            _claimNoteRepository.Verify(x => x.Query(), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmptyResultForSelectedClaim()
        {
            var getAllModel = Fixture.Create<ClaimNoteGetAllModel>();
            var claimNote = Fixture.Create<ClaimNoteEntity>();

            _claimNoteRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimNoteEntity>.Create(claimNote));

            var result = await _claimNoteService.GetAllAsync(getAllModel);

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Empty((List<ClaimNote>)result.Data);

            _claimNoteRepository.Verify(x => x.Query(), Times.Once);
        }

        [Fact]
        public async Task AddAsync_ShouldCreateNoteForSelectedClaim()
        {
            var saveModel = Fixture.Create<ClaimNoteSaveModel>();
            var result = await _claimNoteService.AddAsync(saveModel);

            Assert.NotNull(result);
            Assert.True(result.Success);

            _claimNoteRepository.Verify(x => x.Add(It.IsAny<ClaimNoteEntity>()), Times.Once);
            _claimNoteRepository.Verify(x => x.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task AddToClaimsAsync_ShouldCreateNoteForSelectedClaim()
        {
            var saveModel = Fixture.Create<ClaimNoteRequestModel>();
            var intId = Fixture.Create<int>();
            var claimId = saveModel.ClaimNoteModels.FirstOrDefault().ClaimId;
            var chargeEntryId = Fixture.Create<int>();

            var paymentClaimServiceLineEntity = Fixture.Build<PaymentClaimServiceLineEntity>().With(x => x.Id, claimId).With(x => x.ClaimChargeEntryId, chargeEntryId).Create();
            _paymentClaimServiceLineRepository.Setup(x => x.Query()).Returns(QueryMock<PaymentClaimServiceLineEntity>.Create(paymentClaimServiceLineEntity));

            var claimChargeEntryEntity = Fixture.Build<ClaimChargeEntryEntity>().With(x => x.Id, chargeEntryId).With(x => x.ClaimId, claimId).Create();
            _claimChargeEntryRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimChargeEntryEntity>.Create(claimChargeEntryEntity));

            var claimEntity = Fixture.Build<ClaimEntity>().With(x => x.Id, chargeEntryId).With(x => x.Id, claimId).Create();
            _claimRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimEntity>.Create(claimEntity));

            var result = await _claimNoteService.AddToClaimsAsync(saveModel);

            Assert.NotNull(result);
            Assert.True(result.Success);

            _claimNoteRepository.Verify(x => x.AddRangeAsync(It.IsAny<List<ClaimNoteEntity>>()), Times.Once);
            _claimNoteRepository.Verify(x => x.CommitAsync(), Times.Once);

        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteClaimNote_WhenNoteExists()
        {
            var deleteModel = Fixture.Create<ClaimNoteDeleteModel>();
            var clamNoteEntityToDelete = new ClaimNoteEntity { Id = deleteModel.Id };

            _claimNoteRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimNoteEntity>.Create(clamNoteEntityToDelete));
            _claimHistoryService.Setup(x => x.AddAsync(It.IsAny<ClaimHistorySaveModel>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

            var result = await _claimNoteService.DeleteAsync(deleteModel);

            Assert.NotNull(result);
            Assert.True(result.Success);

            _claimNoteRepository.Verify(x => x.Query(), Times.Once);
            _claimNoteRepository.Verify(x => x.Update(It.Is<ClaimNoteEntity>(x => x == clamNoteEntityToDelete)), Times.Once);
            _claimNoteRepository.Verify(x => x.CommitAsync(), Times.Once);
            _claimHistoryService.Verify(x => x.AddAsync(It.Is<ClaimHistorySaveModel>(h =>
                h.ClaimId == clamNoteEntityToDelete.ClaimId &&
                h.MemberId == deleteModel.MemberId &&
                h.ClaimAction == ClaimAction.Delete &&
                h.ClaimHistoryAction == ClaimHistoryAction.ClaimNoteRemoved), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFailResult_WhenClaimNoteWasNotFound()
        {
            var deleteModel = Fixture.Create<ClaimNoteDeleteModel>();

            _claimNoteRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimNoteEntity>.Create(new ClaimNoteEntity()));

            var result = await _claimNoteService.DeleteAsync(deleteModel);

            Assert.NotNull(result);
            Assert.False(result.Success);

            _claimNoteRepository.Verify(x => x.Query(), Times.Once);
            _claimNoteRepository.Verify(x => x.Update(It.IsAny<ClaimNoteEntity>()), Times.Never);
            _claimNoteRepository.Verify(x => x.CommitAsync(), Times.Never);
        }

    }
}
