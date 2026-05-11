using AutoFixture;
using AutoMapper;
using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Domain.Services.Payment;
using BillingService.Domain.Utils;
using BillingService.XUnit.Tests.Common;
using BillingService.XUnit.Tests.Common.Mocks;
using Moq;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.PaymentPosting
{
    public class PaymentNoteServiceTest : BaseTest
    {
        private Mock<IRepository<BillingDbContext, PaymentNoteEntity>> _paymentNoteRepository;
        private Mock<IRepository<BillingDbContext, PaymentEntity>> _paymentRepository;

        private IPaymentNoteService _paymentNoteService;
        private Mock<IRethinkMasterDataMicroServices> _rethinkService;

        private IMapper _mapper;

        public PaymentNoteServiceTest()
        {
            _paymentNoteRepository = new Mock<IRepository<BillingDbContext, PaymentNoteEntity>>();
            _paymentRepository = new Mock<IRepository<BillingDbContext, PaymentEntity>>();
            _rethinkService = new Mock<IRethinkMasterDataMicroServices>();

            SetupMapper();

            _paymentNoteService = new PaymentNoteService(_paymentNoteRepository.Object,
                _mapper, _paymentRepository.Object, _rethinkService.Object);
        }

        [Fact]
        public async Task GetAll_ShouldReturnAllPaymentNotes()
        {
            var paymentId = Fixture.Create<int>();
            var accountInfoId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();
            SetupMock(paymentId);

            var paymentEntity = Fixture.Build<PaymentEntity>()
                .With(x => x.Id, paymentId)
                .With(x => x.PaymentAmount, 200)
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.PaymentTypeId, (int)PaymentTypes.ERAReceived)
                .Create();

            var paymentClaimEntity = Fixture.Build<PaymentClaimEntity>()
                .With(x => x.PaymentId, paymentId)
                .With(x => x.TotalPayment, 100)
                .Create();
            paymentEntity.PaymentClaims.Add(paymentClaimEntity);

            SetupPayments(paymentEntity);
            SetupRethinkService(accountInfoId, memberId);

            var result = await _paymentNoteService.GetAll(paymentId);

            Assert.NotNull(result);
            Assert.Equal(paymentId, result.First().PaymentId);
        }

        [Fact]
        public async Task AddNote_ShouldReturnId()
        {
            var model = Fixture.Create<PaymentNoteSaveModel>();

            var result = await _paymentNoteService.AddNote(model);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task DeleteNote_ShouldReturnId()
        {
            var model = Fixture.Create<PaymentNoteDeleteModel>();

            SetupMock(model.Id, model.Id);

            var result = await _paymentNoteService.DeleteNote(model);

            Assert.NotNull(result);
            Assert.Equal(model.Id, result);
        }

        private void SetupMock(int? paymentId = null, int? id = null)
        {
            var paymentNoteEntity = Fixture.Build<PaymentNoteEntity>()
                .With(x => x.Id, id ?? Fixture.Create<int>())
                .With(x => x.PaymentId, paymentId ?? Fixture.Create<int>())
                .Create();

            _paymentNoteRepository.Setup(x => x.Query())
                .Returns(QueryMock<PaymentNoteEntity>.Create(paymentNoteEntity));
        }

        //private void SetupMemberMock()
        //{
        //    _memberRepository.Setup(x => x.Query())
        //        .Returns(QueryMock<MemberEntity>.Create());
        //}

        private void SetupPayments(PaymentEntity paymentEntity)
        {
            _paymentRepository.Setup(x => x.Query())
                .Returns(QueryMock<PaymentEntity>.Create(paymentEntity));

            _paymentRepository.Setup(x => x.GetAllAsync(
                    It.IsAny<Expression<Func<PaymentEntity, bool>>>(), null))
                .ReturnsAsync(QueryMock<PaymentEntity>.Create(paymentEntity));
        }

        private void SetupRethinkService(int accountInfoId, int memberId)
        {
            var accountMember = Fixture.Build<RethinkAccountMember>()
                .With(x => x.accountId, accountInfoId)
                .With(x => x.id, memberId)
                .Create();

            var model = Fixture.Build<RethinkAccountMembersListModel>()
                .With(x => x.data, new List<RethinkAccountMember> { accountMember })
                .Create();

            _rethinkService.Setup(x => x.GetMemberListAsync(accountInfoId)).ReturnsAsync(model);
        }

        private void SetupMapper()
        {
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MapperProfile());
            });

            _mapper = mapperConfig.CreateMapper();
        }
    }
}