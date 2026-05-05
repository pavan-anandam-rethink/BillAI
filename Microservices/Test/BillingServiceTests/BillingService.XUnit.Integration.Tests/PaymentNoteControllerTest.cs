using AutoFixture;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.XUnit.Tests.Common.Mocks;
using Moq;
using Newtonsoft.Json;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
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
    [Trait("PaymentNoteControllerTest", "Integration")]
    [Collection("Billing")]
    public class PaymentNoteControllerTest : BaseControllerTest
    {
        private const string BaseUrl = "PaymentNote";

        private readonly Mock<IRepository<BillingDbContext, PaymentNoteEntity>> _paymentNoteRepository;
        private readonly Mock<IRepository<BillingDbContext, PaymentEntity>> _paymentRepository;
        private readonly Mock<IRethinkMasterDataMicroServices> _rethinkService;

        public PaymentNoteControllerTest(TestServerFixture fixture) : base(fixture)
        {
            _paymentNoteRepository = fixture.PaymentNoteRepository;
            _paymentRepository = fixture.PaymentRepository;
            _rethinkService = fixture.RethinkServices;
        }

         [Trait("Category", "Integration")]
        public async Task Add_ShouldReturnAddedId()
        {
            var url = $"{BaseUrl}/Add";
            var data = Fixture.Create<PaymentNote>();

            var response = await PostAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<int>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.IsType<int>(result);
        }

         [Trait("Category", "Integration")]
        public async Task GetAll_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/GetAll";
            var paymentId = Fixture.Create<int>();
            var accountInfoId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();

            SetupMock(paymentId);
            SetupRethinkService(accountInfoId, memberId);

            var response = await PostAsync(url, paymentId);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<PaymentNote>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(paymentId, result.First().PaymentId);
        }

         [Trait("Category", "Integration")]
        public async Task Delete_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/Delete";
            var model = Fixture.Create<PaymentModel>();

            SetupMock(null, model.Id);

            var response = await PostAsync(url, model);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<int>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(model.Id, result);
        }

        private void SetupMock(int? paymentId = null, int? id = null)
        {
            var paymentNoteEntity = Fixture.Build<PaymentNoteEntity>()
                .With(x => x.Id, id ?? Fixture.Create<int>())
                .With(x => x.PaymentId, paymentId ?? Fixture.Create<int>())
                .Create();

            var paymentEntity = Fixture.Build<PaymentEntity>()
            .With(x => x.Id, paymentId)
                .With(x => x.PaymentAmount, 200)
                .With(x => x.PaymentTypeId, (int)PaymentTypes.ERAReceived)
                .Create();

            var paymentClaimEntity = Fixture.Build<PaymentClaimEntity>()
                .With(x => x.PaymentId, paymentId)
                .With(x => x.TotalPayment, 100)
                .Create();
            paymentEntity.PaymentClaims.Add(paymentClaimEntity);

            _paymentNoteRepository.Setup(x => x.Query())
                .Returns(QueryMock<PaymentNoteEntity>.Create(paymentNoteEntity));

            _paymentRepository.Setup(x => x.Query()).Returns(QueryMock<PaymentEntity>.Create(paymentEntity));
        }

        private void SetupRethinkService(int accountInfoId, int memberId)
        {
            var accountMember = Fixture.Build<RethinkAccountMember>()
                .With(x => x.accountId, accountInfoId)
                .With(x => x.id, memberId)
                .Create();

            var memberlist = new List<RethinkAccountMember>();
            memberlist.Add(accountMember);

            var model = Fixture.Build<RethinkAccountMembersListModel>().With(x => x.data, memberlist).With(x => x.total, Fixture.Create<int>()).Create();

            _rethinkService.Setup(x => x.GetMemberListAsync(It.IsAny<int>())).ReturnsAsync(model);
        }
    }
}