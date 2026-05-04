using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models.BulkPaymentPosting;
using BillingService.Domain.Models.PaymentClaims;
using BillingService.Domain.Models.PaymentClaimServiceLineAdjustment;
using BillingService.Domain.Services.Payment;
using BillingService.XUnit.Tests.Common.Mocks;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.BulkPaymentPosting
{
    public class BulkPaymentPostingServiceTest
    {
        private readonly Mock<IRepository<BillingDbContext, PaymentClaimEntity>> _paymentClaimRepositoryMock;
        private readonly Mock<IPaymentClaimService> _paymentClaimServiceMock;
        private readonly Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>> _claimChargeEntryRepositoryMock;
        private readonly IMapper _mapper;
        private readonly Mock<IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity>> _claimChargeEntryWriteOffRepositoryMock;

        private readonly BulkPaymentPostingService _service;

        public BulkPaymentPostingServiceTest()
        {
            _paymentClaimRepositoryMock = new Mock<IRepository<BillingDbContext, PaymentClaimEntity>>();
            _paymentClaimServiceMock = new Mock<IPaymentClaimService>();
            _claimChargeEntryRepositoryMock = new Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>>();
            _claimChargeEntryWriteOffRepositoryMock = new Mock<IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity>>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<PaymentClaimServiceLineAdjustmentEntity, PaymentClaimServiceLineAdjustmentModel>();
            });
            _mapper = mapperConfig.CreateMapper();

            _service = new BulkPaymentPostingService(
                _paymentClaimRepositoryMock.Object,
                _mapper,
                _paymentClaimServiceMock.Object,
                _claimChargeEntryRepositoryMock.Object,
                _claimChargeEntryWriteOffRepositoryMock.Object
            );
        }

        [Fact]
        public async Task GetAllPayments_ReturnsEmptyList_WhenNoPaymentClaimsFound()
        {
            //Arrange
            var request = new BulkPaymentPostingRequestModel { Ids = new int[] { 14276, 14279, 14280 }, Skip = 0, Take = 1 };
            var emptyList = new List<PaymentClaimEntity>().AsQueryable();
            var mockSet = QueryMock<PaymentClaimEntity>.Create(emptyList);

            _paymentClaimRepositoryMock.Setup(r => r.Query())
                .Returns(mockSet);

            // Act
            var result = await _service.GetAllPayments(request);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllPayments_ReturnsPaymentResponses_WhenPaymentClaimsFound()
        {
            // Arrange
            var paymentClaimId = 14290;
            var paymentId = 6094;
            var serviceLineId = 14290;
            var chargeEntryId = 3978;

            var paymentEntity = new PaymentEntity { Id = paymentId };

            var paymentClaimEntity = new PaymentClaimEntity
            {
                Id = paymentClaimId,
                PaymentId = paymentId,
                ClaimId = 2922,
                ClaimIdentifier = "250625-09QBA-21",
                ClientFirstName = "pj_client",
                ClientLastName = "pj_client",
                ClaimStatus = "Processed",
                Payment = paymentEntity
            };

            var paymentClaims = new List<PaymentClaimEntity> { paymentClaimEntity };
            var paymentClaimsMockSet = QueryMock<PaymentClaimEntity>.Create(paymentClaims.AsQueryable());
            _paymentClaimRepositoryMock
                .Setup(r => r.Query())
                .Returns(paymentClaimsMockSet);

            var adjustments = new List<PaymentClaimServiceLineAdjustmentEntity>
            {
                new PaymentClaimServiceLineAdjustmentEntity
                {
                    Id = 1,
                    PaymentClaimServiceLineId = serviceLineId,
                    AdjustmentGroupCode = "CO",
                    AdjustmentReasonCode = "45",
                    IsAdjustmentPositive = true
                }
            };

            var charge = new PaymentGroupedModel
            {
                ServiceLineId = serviceLineId,
                PaymentClaimId = paymentClaimId,
                PaymentId = paymentId,
                PatientId = 454006,
                ChargeId = chargeEntryId,
                DateOfService = new DateTime(2025, 6, 25),
                AllowedAmount = 15,
                PaidAmount = 34,
                ChargeAmount = 100,
                Adjustments = adjustments,
                HasErrors = false,
                DateDeleted = null
            };

            _paymentClaimServiceMock
                .Setup(s => s.GetAllCharges(paymentId))
                .ReturnsAsync(new List<PaymentGroupedModel> { charge });

            _paymentClaimServiceMock
                .Setup(s => s.GetGroupedByPayments(
                    It.IsAny<List<PaymentGroupedModel>>(),
                    paymentEntity,
                    GroupByParam.Charge,
                    true))
                .ReturnsAsync(new List<PatientPaymentClaimFullModel>
                {
            new PatientPaymentClaimFullModel
            {
                ChargeId = chargeEntryId,
                PatientResponsibility = 20,
                PatientResponsibilityBalance = 20,
                Adjustment = 10,
                InsurancePayment = 34,
                PatientPayment = 100
            }
                });

            var claimChargeEntries = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity
                {
                    Id = chargeEntryId,
                    Modifier1 = "M1",
                    Modifier2 = "M2"
                }
            };

            var claimChargeEntriesMockSet =
                QueryMock<ClaimChargeEntryEntity>.Create(claimChargeEntries.AsQueryable());

            _claimChargeEntryRepositoryMock
                .Setup(r => r.Query())
                .Returns(claimChargeEntriesMockSet);

            _claimChargeEntryWriteOffRepositoryMock
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimChargeEntryWriteOffEntity>
                    .Create(new List<ClaimChargeEntryWriteOffEntity>().AsQueryable()));

            var requestModel = new BulkPaymentPostingRequestModel
            {
                Ids = new[] { paymentClaimId },
                MemberId = 105815,
                AccountInfoId = 18421,
                Skip = 0,
                Take = 1
            };

            // Act
            var result = await _service.GetAllPayments(requestModel);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);

            var paymentResponse = result.First();

            Assert.Equal(paymentClaimEntity.ClaimId, paymentResponse.ClaimId);
            Assert.Equal("pj_client pj_client", paymentResponse.PatientName);
            Assert.Equal(34, paymentResponse.PaidAmount);
            Assert.False(paymentResponse.HasErrors);
        }

    }
}