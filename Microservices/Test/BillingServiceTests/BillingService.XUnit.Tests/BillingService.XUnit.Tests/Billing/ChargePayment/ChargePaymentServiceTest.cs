using AutoMapper;
using BillingService.Domain.Models;
using BillingService.Domain.Services.Billing;
using BillingService.Domain.Interfaces.Billing;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using BillingService.XUnit.Tests.Common.Mocks;
using Rethink.Services.Common.Models.ClientMicroServicesModels;

namespace BillingService.XUnit.Tests.Billing.ChargePayment
{
    public class ChargePaymentServiceTest
    {
        private readonly Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>> _chargeEntryRepoMock;
        private readonly Mock<IRepository<BillingDbContext, ClaimEntity>> _claimRepoMock;
        private readonly Mock<IRepository<BillingDbContext, PaymentMethodEntity>> _paymentMethodRepoMock;
        private readonly Mock<IRepository<BillingDbContext, ChargePaymentEntity>> _chargePaymentRepoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IRethinkMasterDataMicroServices> _rethinkServicesMock;
        private readonly IChargePaymentService _service;

        public ChargePaymentServiceTest()
        {
            _chargeEntryRepoMock = new Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>>();
            _claimRepoMock = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            _paymentMethodRepoMock = new Mock<IRepository<BillingDbContext, PaymentMethodEntity>>();
            _chargePaymentRepoMock = new Mock<IRepository<BillingDbContext, ChargePaymentEntity>>();
            _mapperMock = new Mock<IMapper>();
            _rethinkServicesMock = new Mock<IRethinkMasterDataMicroServices>();

            _service = new ChargePaymentService(
                _chargeEntryRepoMock.Object,
                _claimRepoMock.Object,
                _paymentMethodRepoMock.Object,
                _chargePaymentRepoMock.Object,
                _mapperMock.Object,
                _rethinkServicesMock.Object
            );
        }

        [Fact]
        public async Task GetPaymentOptions_ReturnsEmptyCharges_WhenClaimNotFound()
        {
            var claimId = 1;
            var accountInfoId = 2;
            var reasonCodes = new List<ClientReasonCodes> { new ClientReasonCodes { id = 1, name = "Reason1" } };
            var paymentMethods = new List<PaymentMethodEntity> { new PaymentMethodEntity { Id = 1, Name = "PM1" } };

            _rethinkServicesMock.Setup(x => x.GetReasonCodes()).Returns(Task.FromResult(reasonCodes));

            _paymentMethodRepoMock.Setup(x => x.GetAllAsync(null, null)).ReturnsAsync(paymentMethods.AsQueryable());

            var claimDbSet = DbMock.Create(new List<ClaimEntity>());
            _claimRepoMock.Setup(x => x.Query()).Returns(claimDbSet);

            var result = await _service.GetPaymentOptions(claimId, accountInfoId);

            Assert.NotNull(result);
            Assert.Empty(result.Charges);
            Assert.Single(result.Reasons);
            Assert.Single(result.PaymentMethods);
        }

        [Fact]
        public async Task GetPaymentOptions_ReturnsOptions_WhenClaimExists()
        {
            var claimId = 1;
            var accountInfoId = 2;
            var reasonCodes = new List<ClientReasonCodes> { new ClientReasonCodes { id = 1, name = "Reason1" }, new ClientReasonCodes { id = 2, name = "Reason2" } };
            var paymentMethods = new List<PaymentMethodEntity> { new PaymentMethodEntity { Id = 1, Name = "PM1" } };
            var claim = new ClaimEntity { Id = claimId, AccountInfoId = accountInfoId };
            var charges = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity { Id = 10, ClaimId = claimId, BillingCode = "A", DateOfService = DateTime.Today, DateDeleted = null }
            };

            _rethinkServicesMock.Setup(x => x.GetReasonCodes()).Returns(Task.FromResult(reasonCodes));

            // Fix for CS1929 and CS0854: Do not use ReturnsAsync with IQueryable, and do not use optional arguments in expression trees.
            _paymentMethodRepoMock.Setup(x => x.GetAllAsync(null, null)).ReturnsAsync(paymentMethods.AsQueryable());

            var claimDbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(x => x.Query()).Returns(claimDbSet);

            var chargeDbSet = DbMock.Create(charges);
            _chargeEntryRepoMock.Setup(x => x.Query()).Returns(chargeDbSet);

            var result = await _service.GetPaymentOptions(claimId, accountInfoId);

            Assert.NotNull(result);
            Assert.Single(result.Charges);
            Assert.Equal("A - " + DateTime.Today.ToString("MM/dd/yyyy"), result.Charges[0].Name);
            Assert.Equal(2, result.Reasons.Count);
            Assert.Single(result.PaymentMethods);
        }

        [Fact]
        public async Task GetRemainingAmount_ReturnsCorrectAmount_WhenChargeExists()
        {
            var chargeId = 1;
            var accountInfoId = 2;
            var charge = new ClaimChargeEntryEntity
            {
                Id = chargeId,
                Charges = 100m,
                Claim = new ClaimEntity { AccountInfoId = accountInfoId },
                DateDeleted = null
            };
            var payments = new List<ChargePaymentEntity>
            {
                new ChargePaymentEntity { ChargeId = chargeId, Amount = 30m, DateDeleted = null },
                new ChargePaymentEntity { ChargeId = chargeId, Amount = 20m, DateDeleted = null }
            };

            var chargeDbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { charge });
            _chargeEntryRepoMock.Setup(x => x.Query()).Returns(chargeDbSet);

            var paymentDbSet = DbMock.Create(payments);
            _chargePaymentRepoMock.Setup(x => x.Query()).Returns(paymentDbSet);

            var result = await _service.GetRemainingAmount(chargeId, accountInfoId);

            Assert.Equal(50m, result); // 100 - (30+20)
        }

        [Fact]
        public async Task GetRemainingAmount_ReturnsZero_WhenChargeNotFound()
        {
            var chargeId = 1;
            var accountInfoId = 2;
            var chargeDbSet = DbMock.Create(new List<ClaimChargeEntryEntity>());
            _chargeEntryRepoMock.Setup(x => x.Query()).Returns(chargeDbSet);

            var result = await _service.GetRemainingAmount(chargeId, accountInfoId);

            Assert.Equal(0m, result);
        }

        [Fact]
        public async Task GetForClaim_ReturnsMappedItems_WhenClaimExists()
        {
            var claimId = 1;
            var accountInfoId = 2;
            var claim = new ClaimEntity { Id = claimId, AccountInfoId = accountInfoId, DateDeleted = null };
            var chargePaymentEntities = new List<ChargePaymentEntity>
            {
                new ChargePaymentEntity
                {
                    Id = 10,
                    ChargeEntry = new ClaimChargeEntryEntity { Claim = claim },
                    DateDeleted = null
                }
            };

            var claimDbSet = DbMock.Create(new List<ClaimEntity> { claim });
            _claimRepoMock.Setup(x => x.Query()).Returns(claimDbSet);

            var chargePaymentDbSet = DbMock.Create(chargePaymentEntities);
            _chargePaymentRepoMock.Setup(x => x.Query()).Returns(chargePaymentDbSet);

            _mapperMock.Setup(x => x.Map<List<ChargePaymentItem>>(It.IsAny<List<ChargePaymentEntity>>()))
                .Returns(new List<ChargePaymentItem> { new ChargePaymentItem { Id = 10 } });

            var result = await _service.GetForClaim(claimId, accountInfoId);

            Assert.Single(result);
            Assert.Equal(10, result[0].Id);
        }

        [Fact]
        public async Task GetForClaim_ReturnsEmptyList_WhenClaimNotFound()
        {
            var claimId = 1;
            var accountInfoId = 2;
            var claimDbSet = DbMock.Create(new List<ClaimEntity>());
            _claimRepoMock.Setup(x => x.Query()).Returns(claimDbSet);

            _mapperMock.Setup(x => x.Map<List<ChargePaymentItem>>(It.IsAny<List<ChargePaymentEntity>>()))
                .Returns(new List<ChargePaymentItem>());

            var result = await _service.GetForClaim(claimId, accountInfoId);

            Assert.Empty(result);
        }

        [Fact]
        public async Task Save_AddsPaymentEntity_WhenChargeExists()
        {
            var item = new ChargePaymentItem { ChargeEntryId = 1 };
            var memberId = 5;
            var charge = new ClaimChargeEntryEntity { Id = 1 };
            var chargeDbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { charge });
            _chargeEntryRepoMock.Setup(x => x.Query()).Returns(chargeDbSet);

            _mapperMock.Setup(x => x.Map<ChargePaymentItem>(It.IsAny<ChargePaymentEntity>()))
                .Returns(new ChargePaymentItem { Id = 99 });

            var result = await _service.Save(item, memberId);

            _chargePaymentRepoMock.Verify(x => x.Add(It.IsAny<ChargePaymentEntity>()), Times.Once);
            _chargeEntryRepoMock.Verify(x => x.CommitAsync(), Times.Once);
            Assert.Equal(99, result.Id);
        }

        [Fact]
        public async Task Save_ReturnsDefault_WhenChargeNotFound()
        {
            var item = new ChargePaymentItem { ChargeEntryId = 1 };
            var memberId = 5;
            var chargeDbSet = DbMock.Create(new List<ClaimChargeEntryEntity>());
            _chargeEntryRepoMock.Setup(x => x.Query()).Returns(chargeDbSet);

            _mapperMock.Setup(x => x.Map<ChargePaymentItem>(It.IsAny<ChargePaymentEntity>()))
                .Returns(new ChargePaymentItem());

            var result = await _service.Save(item, memberId);

            _chargePaymentRepoMock.Verify(x => x.Add(It.IsAny<ChargePaymentEntity>()), Times.Never);
            _chargeEntryRepoMock.Verify(x => x.CommitAsync(), Times.Never);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task Delete_UpdatesEntity_WhenChargePaymentExists()
        {
            var item = new ChargePaymentItem { Id = 10, ChargeEntryId = 1 };
            var memberId = 5;
            var chargePayment = new ChargePaymentEntity { Id = 10, DateDeleted = null };
            var chargeEntry = new ClaimChargeEntryEntity
            {
                Id = 1,
                ChargePayments = new List<ChargePaymentEntity> { chargePayment }
            };
            var chargeEntryDbSet = DbMock.Create(new List<ClaimChargeEntryEntity> { chargeEntry });
            _chargeEntryRepoMock.Setup(x => x.Query()).Returns(chargeEntryDbSet);

            _mapperMock.Setup(x => x.Map<ChargePaymentItem>(It.IsAny<ChargePaymentEntity>()))
                .Returns(new ChargePaymentItem { Id = 10 });

            var result = await _service.Delete(item, memberId);

            _chargePaymentRepoMock.Verify(x => x.Update(chargePayment), Times.Once);
            _chargePaymentRepoMock.Verify(x => x.CommitAsync(), Times.Once);
            Assert.Equal(10, result.Id);
        }

        [Fact]
        public async Task Delete_ReturnsDefault_WhenChargeEntryNotFound()
        {
            var item = new ChargePaymentItem { Id = 10, ChargeEntryId = 1 };
            var memberId = 5;
            var chargeEntryDbSet = DbMock.Create(new List<ClaimChargeEntryEntity>());
            _chargeEntryRepoMock.Setup(x => x.Query()).Returns(chargeEntryDbSet);

            _mapperMock.Setup(x => x.Map<ChargePaymentItem>(It.IsAny<ChargePaymentEntity>()))
                .Returns(new ChargePaymentItem());

            var result = await _service.Delete(item, memberId);

            _chargePaymentRepoMock.Verify(x => x.Update(It.IsAny<ChargePaymentEntity>()), Times.Never);
            _chargePaymentRepoMock.Verify(x => x.CommitAsync(), Times.Never);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task AddChargePaymentEntitesAsync_CallsAddRangeAsync()
        {
            var entities = new List<ChargePaymentEntity> { new ChargePaymentEntity { Id = 1 } };
            _chargePaymentRepoMock.Setup(x => x.AddRangeAsync(entities)).Returns(Task.CompletedTask);

            await _service.AddChargePaymentEntitesAsync(entities);

            _chargePaymentRepoMock.Verify(x => x.AddRangeAsync(entities), Times.Once);
        }
    }
}
