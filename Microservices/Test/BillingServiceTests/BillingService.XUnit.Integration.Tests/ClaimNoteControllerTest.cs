using AutoFixture;
using BillingService.Domain.Models.Claims;
using BillingService.XUnit.Tests.Common.Mocks;
using BillingService.XUnit.Tests.Common.Models;
using Moq;
using Newtonsoft.Json;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Integration.Tests
{
    [Trait("ClaimNoteControllerTest", "Integration")]
    [Collection("Billing")]
    public class ClaimNoteControllerTest : BaseControllerTest
    {
        private const string BaseUrl = "ClaimNote";

        private readonly Mock<IRepository<BillingDbContext, ClaimNoteEntity>> _claimNoteRepository;
        private readonly Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>> _paymentClaimServiceLineRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>> _claimChargeEntryRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimEntity>> _claimRepository;
        private readonly Mock<IRethinkMasterDataMicroServices> _rethinkServices;

        public ClaimNoteControllerTest(TestServerFixture fixture)
            : base(fixture)
        {
            _claimNoteRepository = fixture.ClaimNoteRepository;
            _paymentClaimServiceLineRepository = fixture.PaymentClaimServiceLineRepository;
            _claimChargeEntryRepository = fixture.ClaimChargeEntryRepository;
            _claimRepository = fixture.ClaimRepository;
            _rethinkServices = fixture.RethinkServices;
        }

         [Trait("Category", "Integration")]
        public async Task GetAll_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/GetAll";
            var getAllModel = Fixture.Create<ClaimNoteGetAllModel>();
            var claimId = Fixture.Create<int>();
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

            var response = await PostAsync(url, getAllModel);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ActionSuccessResult<List<ClaimNote>>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Single(result.Data);
            Assert.Collection(result.Data, note => Assert.Equal($"{member.firstName} {member.lastName}", note.CreatedByName));
        }

         [Trait("Category", "Integration")]
        public async Task GetAll_ShouldReturnEmptyResult()
        {
            var url = $"{BaseUrl}/GetAll";
            var getAllModel = Fixture.Create<ClaimNoteGetAllModel>();
            var claimNote = Fixture.Create<ClaimNoteEntity>();

            _claimNoteRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimNoteEntity>.Create(claimNote));

            var response = await PostAsync(url, getAllModel);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ActionSuccessResult<List<ClaimNote>>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Empty(result.Data);
        }

         [Trait("Category", "Integration")]
        public async Task Add_ShouldReturnSeccessResult()
        {
            var url = $"{BaseUrl}/Add";
            var saveModel = Fixture.Create<ClaimNoteSaveModel>();

            var response = await PostAsync(url, saveModel);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ActionSuccessResult>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.True(result.Success);
        }

         [Trait("Category", "Integration")]
        public async Task AddToSeveral_ShouldCreateNoteOnClaims()
        {
            var url = $"{BaseUrl}/AddToSeveral";
            var saveModel = Fixture.Create<ClaimNoteRequestModel>();
            var claimId = saveModel.ClaimNoteModels.FirstOrDefault().ClaimId;
            var chargeEntryId = Fixture.Create<int>();

            var paymentClaimServiceLineEntity = Fixture.Build<PaymentClaimServiceLineEntity>().With(x => x.Id, claimId).With(x => x.ClaimChargeEntryId, chargeEntryId).Create();
            _paymentClaimServiceLineRepository.Setup(x => x.Query()).Returns(QueryMock<PaymentClaimServiceLineEntity>.Create(paymentClaimServiceLineEntity));

            var claimChargeEntryEntity = Fixture.Build<ClaimChargeEntryEntity>().With(x => x.Id, chargeEntryId).With(x => x.ClaimId, claimId).Create();
            _claimChargeEntryRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimChargeEntryEntity>.Create(claimChargeEntryEntity));

            var claimEntity = Fixture.Build<ClaimEntity>().With(x => x.Id, chargeEntryId).With(x => x.Id, claimId).Create();
            _claimRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimEntity>.Create(claimEntity));

            var response = await PostAsync(url, saveModel);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ActionSuccessResult>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.True(result.Success);
        }

         [Trait("Category", "Integration")]
        public async Task Delete_ShouldReturnSuccessResult()
        {
            var url = $"{BaseUrl}/Delete";
            var deleteModel = Fixture.Create<ClaimNoteDeleteModel>();

            _claimNoteRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimNoteEntity>.Create(new ClaimNoteEntity { Id = deleteModel.Id }));

            var response = await PostAsync(url, deleteModel);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ActionSuccessResult>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.True(result.Success);
        }

         [Trait("Category", "Integration")]
        public async Task Delete_ShouldReturnErrorResult_IfNoteWasNotFound()
        {
            var url = $"{BaseUrl}/Delete";
            var deleteModel = Fixture.Create<ClaimNoteDeleteModel>();

            _claimNoteRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimNoteEntity>.Create(new ClaimNoteEntity()));

            var response = await PostAsync(url, deleteModel);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ActionErrorResult>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("Note not found", result.Error);
        }
    }
}
