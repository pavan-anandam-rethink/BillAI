using AutoFixture;
using BillingService.Domain.Models;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.XUnit.Tests.Common.Mocks;
using Moq;
using Newtonsoft.Json;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Integration.Tests
{
    [Trait("PaymentPostingControllerTest", "Integration")]
    [Collection("Billing")]
    public class PaymentPostingControllerTest : BaseControllerTest
    {
        private const string BaseUrl = "PaymentPosting";

        private readonly Mock<IRepository<BillingDbContext, PaymentEntity>> _paymentRepository;
        private readonly Mock<IRepository<BillingDbContext, PaymentClaimEntity>> _paymentClaimRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimEntity>> _claimRepository;

        public PaymentPostingControllerTest(TestServerFixture fixture) : base(fixture)
        {
            _paymentRepository = fixture.PaymentRepository;
            _paymentClaimRepository = fixture.PaymentClaimRepository;
            _claimRepository = fixture.ClaimRepository;
        }

         [Trait("Category", "Integration")]
        public async Task GetPayments_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/GetPayments";
            var data = Fixture.Create<GetPaymentsModel>();
            data.Skip = 0;
            data.Take = 10;
            data.FilterModels = new List<FilterModel>();
            data.SortingModels = new List<SortingModel>();

            data.AccountInfoId = Fixture.Create<int>();

            SetupMock(data.AccountInfoId);

            var response = await PostAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<PaymentsResponseModel>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
        }

         [Trait("Category", "Integration")]
        public async Task GetPaymentMethods_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/GetPaymentMethods";

            var response = await PostAsync(url, null);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<PaymentMethodsModel>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(Enum.GetNames(typeof(PaymentMethods)).Length, result.Count);
        }

         [Trait("Category", "Integration")]
        public async Task GetReconcileStatuses_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/GetReconcileStatuses";

            var response = await PostAsync(url, null);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<string>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(Enum.GetNames(typeof(ReconcileStatuses)).Length, result.Count);
        }

         [Trait("Category", "Integration")]
        public async Task GetProcessingPayments_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/GetProcessingPayments";
            var data = Fixture.Create<UserInfo>();
            data.AccountInfoId = Fixture.Create<int>();

            SetupMock(data.AccountInfoId);

            var response = await PostAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<PaymentProcessingModel>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
        }

         [Trait("Category", "Integration")]
        public async Task GetPaymentSummary_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/GetPaymentSummary";
            var paymentId = Fixture.Create<int>();

            SetupMock(null, paymentId);

            var response = await PostAsync(url, paymentId);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<PaymentSummary>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(paymentId, result.Id);
        }

         [Trait("Category", "Integration")]
        public async Task GetPaymentShortInfo_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/GetPaymentShortInfo";
            var paymentId = Fixture.Create<int>();

            SetupMock(null, paymentId);

            var response = await PostAsync(url, paymentId);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<PaymentShortInfo>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(paymentId, result.Id);
        }

        // [Trait("Category", "Integration")]
        //public async Task DeletePayment_ShouldReturnResult()
        //{
        //    var url = $"{BaseUrl}/DeletePayment";
        //    var model = Fixture.Create<UpdatePaymentModel>();
        //    var paymentId = model.PaymentId;

        //    SetupMock(null, paymentId[0], true, true);

        //    var response = await PostAsync(url, model);
        //    var content = await response.Content.ReadAsStringAsync();
        //    var result = JsonConvert.DeserializeObject<List<int>>(content);

        //    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        //    Assert.NotNull(result);
        //    Assert.NotEmpty(result);
        //}

         [Trait("Category", "Integration")]
        public async Task ReconcilePayment_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/ReconcilePayment";
            var model = Fixture.Create<UpdatePaymentModel>();
            var paymentId = model.PaymentId;

            SetupMock(null, paymentId[0], true, true);

            var response = await PostAsync(url, model);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<int>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }


        private void SetupMock(int? accountInfoId = null, int? paymentId = null, bool addPaymentClaims = false,
            bool validPayment = false)
        {
            var claimid = Fixture.Create<int>();

            var paymentClaimServiceLineAdjustments_plus = Fixture.Build<PaymentClaimServiceLineAdjustmentEntity>()
                .With(x => x.IsAdjustmentPositive, true)
                .Create();

            var paymentClaimServiceLineAdjustments_minus = Fixture.Build<PaymentClaimServiceLineAdjustmentEntity>()
                .With(x => x.IsAdjustmentPositive, false)
                .Create();

            var paymentClaimServiceLine = Fixture.Build<PaymentClaimServiceLineEntity>()
                .Create();
            paymentClaimServiceLine.PaymentClaimServiceLineAdjustments.Add(paymentClaimServiceLineAdjustments_plus);
            paymentClaimServiceLine.PaymentClaimServiceLineAdjustments.Add(paymentClaimServiceLineAdjustments_minus);

            var paymentEntity = Fixture.Build<PaymentEntity>()
                .With(x => x.Id, paymentId ?? Fixture.Create<int>())
                .With(x => x.AccountInfoId, accountInfoId ?? Fixture.Create<int>())
                .With(x => x.PaymentMethodId, 1)
                .With(x => x.HasAcknowledgedErrors, false)
                .With(x => x.PaymentEraUpload, Fixture.Create<PaymentEraUploadEntity>())
                .With(x => x.PaymentAmount, validPayment ? 200 : 20)
                .With(x => x.PaymentTypeId, validPayment ? (int)PaymentTypes.ClientPayment : (int)PaymentTypes.ERAReceived)
                .Create();

            var claimChargeEntryWriteOffs = Fixture.Build<ClaimChargeEntryWriteOffEntity>().With(x => x.ClaimWriteOffId, claimid).CreateMany();

            var claimWriteOffs = Fixture.Build<ClaimWriteOffEntity>().With(x => x.ClaimChargeEntryWriteOffs, claimChargeEntryWriteOffs.ToList()).CreateMany();

            var claim = Fixture.Build<ClaimEntity>().With(x => x.Id, claimid).With(x => x.ClaimWriteOffs, claimWriteOffs.ToList()).Create();

            _claimRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(claim);

            var paymentClaim = Fixture.Build<PaymentClaimEntity>()
                .With(x => x.PaymentId, paymentId ?? 0)
                .With(x => x.ClaimId, claimid)
                .With(x => x.Claim, claim)
                .With(x => x.TotalPayment, validPayment ? 100 : 500)
                .Create();
            paymentClaim.PaymentClaimServiceLines.Add(paymentClaimServiceLine);


            if (addPaymentClaims)
            {
                paymentEntity.PaymentClaims.Add(paymentClaim);
            }

            _paymentRepository.Setup(x => x.Query())
                .Returns(QueryMock<PaymentEntity>.Create(paymentEntity));

            _paymentRepository.Setup(x => x.GetAllAsync(
                    It.IsAny<Expression<Func<PaymentEntity, bool>>>(), null))
                .ReturnsAsync(QueryMock<PaymentEntity>.Create(paymentEntity));

            _paymentClaimRepository.Setup(x => x.Query())
                .Returns(QueryMock<PaymentClaimEntity>.Create(paymentClaim));
        }
    }
}