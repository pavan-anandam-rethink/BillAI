using AutoFixture;
using AutoMapper;
using Azure;
using Azure.Storage.Blobs.Models;
using Billing.FolderStructure.Core.Models;
using Billing.FolderStructure.Core.Services;
using Billing.FolderStructure.Core.Utils;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Domain.Services.Billing;
using BillingService.Domain.Services.Payment;
using BillingService.Domain.Utils;
using BillingService.XUnit.Tests.Common;
using BillingService.XUnit.Tests.Common.Mocks;
using Microsoft.Azure.ServiceBus;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Configuration;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Moq.Protected;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Connection;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Messaging;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Common.Models.ClientMicroServicesModels;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.PaymentPosting
{
    public class PaymentPostingServiceTest : BaseTest
    {
        private const string guarantorDetailsCodeKey = "guarantorDetails";
        private const int cacheExpiration = 10;

        private Mock<IRepository<BillingDbContext, PaymentEntity>> _paymentRepository;
        private Mock<IRepository<BillingDbContext, PaymentClaimEntity>> _paymentClaimRepository;
        private Mock<IRepository<BillingDbContext, ClaimEntity>> _claimRepository;
        private Mock<IRepository<BillingDbContext, PaymentEraUploadEntity>> _paymentEraUploadRepository;
        private Mock<IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity>> _paymentClaimServiceLineAdjustmentRepository;
        private Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>> _paymentClaimServiceLineRepository;


        private IPaymentPostingService _paymentPostingService;
        private Mock<IPaymentMethodService> _paymentMethodService;
        private Mock<IBlobProcessingService> _blobProcessingService;
        private Mock<IServiceBusConnectionFactory> _serviceBusConnectionFactory;
        private Mock<IClaimHistoryService> _claimHistoryService;
        private Mock<IChargeEntryService> _chargeEntryService;
        private Mock<IClaimManagerService> _claimManagerService;
        private Mock<IRethinkMasterDataMicroServices> _rethinkMasterDataMicroServices;
        private Mock<IMessageBus> _bus;
        private IMapper _mapper;
        private readonly Mock<IBillingFilePath> _billingFilePath;
        private readonly Mock<IBillingBlobService> _billingBlobService;
        private readonly Mock<IRepository<BillingDbContext, ClaimSubmissionEntity>> _claimSubmissionRepository;
        private readonly Mock<IRepository<BillingDbContext, UnAllocatedPaymentEntity>> _unAllocatedPaymentRepository;
        private readonly Mock<IConfiguration> _Configuration;
        private readonly Mock<ICHService> _blobBackupService;
        private readonly Mock<ICacheService> _cacheService;
        Mock<DbConnectionStringBuilder> _connectionStringBuilder;

        public PaymentPostingServiceTest()
        {
            _paymentRepository = new Mock<IRepository<BillingDbContext, PaymentEntity>>();
            _paymentClaimRepository = new Mock<IRepository<BillingDbContext, PaymentClaimEntity>>();
            _paymentMethodService = new Mock<IPaymentMethodService>();
            SetupMapper();
            _paymentEraUploadRepository = new Mock<IRepository<BillingDbContext, PaymentEraUploadEntity>>();
            _blobProcessingService = new Mock<IBlobProcessingService>();
            _billingBlobService = new Mock<IBillingBlobService>();
            _serviceBusConnectionFactory = new Mock<IServiceBusConnectionFactory>();
            _claimHistoryService = new Mock<IClaimHistoryService>();
            _chargeEntryService = new Mock<IChargeEntryService>();
            _claimManagerService = new Mock<IClaimManagerService>();
            _rethinkMasterDataMicroServices = new Mock<IRethinkMasterDataMicroServices>();
            _paymentClaimServiceLineAdjustmentRepository = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity>>();
            _paymentClaimServiceLineRepository = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>>();

            _bus = new Mock<IMessageBus>();

            _billingFilePath = new Mock<IBillingFilePath>();
            _claimSubmissionRepository = new Mock<IRepository<BillingDbContext, ClaimSubmissionEntity>>();
            _claimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            _unAllocatedPaymentRepository = new Mock<IRepository<BillingDbContext, UnAllocatedPaymentEntity>>();
            _Configuration = new Mock<IConfiguration>();
            _blobBackupService = new Mock<ICHService>();
            _cacheService = new Mock<ICacheService>();
            _connectionStringBuilder = new Mock<DbConnectionStringBuilder>();

            _paymentPostingService = new PaymentPostingService(
                _paymentRepository.Object,
                _paymentClaimRepository.Object,
                _paymentMethodService.Object,
                _mapper,
                _paymentEraUploadRepository.Object,
                _blobProcessingService.Object,
                _billingBlobService.Object,
                _serviceBusConnectionFactory.Object,
                _claimHistoryService.Object,
                _chargeEntryService.Object,
                _claimManagerService.Object,
                _rethinkMasterDataMicroServices.Object,
                _paymentClaimServiceLineAdjustmentRepository.Object,
                _paymentClaimServiceLineRepository.Object,
                _bus.Object,
                _billingFilePath.Object,
                _claimRepository.Object,
                _unAllocatedPaymentRepository.Object,
                _Configuration.Object,
                _blobBackupService.Object,
                _cacheService.Object
            );
        }

        [Fact]
        public async Task GetAllPayments_ShouldReturnPayments()
        {
            var accountInfoId = Fixture.Create<int>();
            var getPaymentsModel = Fixture.Build<GetPaymentsModel>()
                .With(x => x.Skip, 0)
                .With(x => x.Take, 10)
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.FilterModels, new List<FilterModel>())
                .With(x => x.SortingModels, new List<SortingModel>())
                .Create();

            var paymentEntity = Fixture.Build<PaymentEntity>()
                .With(x => x.PaymentTypeEntity, new PaymentTypeEntity())
                .With(x => x.PaymentClaims, new List<PaymentClaimEntity>())
                .With(x => x.PaymentMethodEntity, new PaymentMethodEntity { Id = 1, Name = "Cash" })
                .With(x => x.PaymentMethodId, 1)
                .With(x => x.AccountInfoId, accountInfoId)
                .Create();

            var accountInfo = new AccountInfoEntityModel
            {
                subscriptionFeatures = new Dictionary<string, object>
                {
                    { "showRevSpring", true }
                },
            };

            _rethinkMasterDataMicroServices
                .Setup(x => x.GetAccountReturningEntityAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(accountInfo);

            SetupPayments(paymentEntity);

            var result = await _paymentPostingService.GetAllPayments(getPaymentsModel);

            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void GetPaymentMethods_ShouldReturnAllPaymentMethods()
        {
            var expected = Enum.GetNames(typeof(PaymentMethods)).Length;

            var result = _paymentPostingService.GetPaymentMethods();

            Assert.NotNull(result);
            Assert.Equal(expected, result.Count);
        }

        [Fact]
        public void GetReconcileStatuses_ShouldReturnStatuses()
        {
            var expected = Enum.GetNames(typeof(ReconcileStatuses)).Length;

            var result = _paymentPostingService.GetReconcileStatuses();

            Assert.NotNull(result);
            Assert.Equal(expected, result.Count);
        }

        [Fact]
        public async Task GetProcessingPayments_ShouldReturnPayments()
        {
            var userInfo = Fixture.Create<UserInfo>();

            var paymentEntity = Fixture.Build<PaymentEntity>()
                .With(x => x.PaymentTypeEntity, new PaymentTypeEntity())
                .With(x => x.PaymentClaims, new List<PaymentClaimEntity>())
                .With(x => x.PaymentMethodEntity, new PaymentMethodEntity { Id = 1, Name = "Cash" })
                .With(x => x.PaymentMethodId, 1)
                .With(x => x.AccountInfoId, userInfo.AccountInfoId)
                .With(x => x.HasAcknowledgedErrors, false)
                .With(x => x.PaymentEraUpload, Fixture.Create<PaymentEraUploadEntity>())
                .Create();

            SetupPayments(paymentEntity);

            var result = await _paymentPostingService.GetProcessingPaymentsAsync(userInfo);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(paymentEntity.Id, result.FirstOrDefault().PaymentId);
        }

        [Fact]
        public async Task GetPaymentSummaryAsync_ShouldReturnSummary()
        {
            // Arrange
            var paymentId = Fixture.Create<int>();

            var paymentClaimEntity = Fixture.Build<PaymentClaimEntity>()
                .With(x => x.PaymentId, paymentId)
                .With(x => x.DateDeleted, (DateTime?)null)
                .With(x => x.TotalPayment, 100m)
                .Create();

            var paymentEntity = Fixture.Build<PaymentEntity>()
                .With(x => x.Id, paymentId)
                .With(x => x.DateDeleted, (DateTime?)null)
                .With(x => x.PaymentAmount, 200m)
                .With(x => x.PaymentClaims, new List<PaymentClaimEntity> { paymentClaimEntity })
                .With(x => x.UnallocatedPayments, new List<UnAllocatedPaymentEntity>())
                .With(x => x.PaymentMethodEntity, new PaymentMethodEntity { Name = "ACH" })
                .Create();

            SetupPayments(paymentEntity);
            SetupPaymentClaims(paymentClaimEntity);

            // Act
            var result = await _paymentPostingService.GetPaymentSummaryAsync(paymentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(paymentEntity.Id, result.Id);
            Assert.Equal(paymentEntity.PaymentAmount, result.PaymentAmount);
            Assert.Equal(paymentClaimEntity.TotalPayment, result.PostedAmount);
            Assert.Equal(paymentEntity.PaymentAmount - paymentClaimEntity.TotalPayment, result.RemainingAmount);
        }


        [Fact]
        public async Task GetPaymentShortInfoAsync_ShouldReturnShortInfo()
        {
            var paymentId = Fixture.Create<int>();

            var paymentEntity = Fixture.Build<PaymentEntity>()
                .With(x => x.Id, paymentId)
                .Create();
            var paymentClaimEntity = Fixture.Build<PaymentClaimEntity>()
                .With(x => x.PaymentId, paymentId)
                .Create();

            SetupPayments(paymentEntity);
            SetupPaymentClaims(paymentClaimEntity);

            var result = await _paymentPostingService.GetPaymentShortInfoAsync(paymentId);

            Assert.NotNull(result);
            Assert.Equal(paymentEntity.Id, result.Id);
        }

        [Fact]
        public async Task DeletePayment_ShouldReturnEmptyString()
        {
            var paymentId = Fixture.Create<int[]>();
            var memberId = Fixture.Create<int>();
            var accountId = Fixture.Create<int>();
            var paymentEntity = Fixture.Build<PaymentEntity>()
                .With(x => x.Id, paymentId[0])
                .Create();
            var paymentClaimEntity = Fixture.Build<PaymentClaimEntity>()
                .With(x => x.PaymentId, paymentId[0])
                .Create();

            SetupPayments(paymentEntity);
            SetupPaymentClaims(paymentClaimEntity);

            var result = await _paymentPostingService.DeletePaymentAsync(paymentId, memberId, accountId);

            Assert.Equal(0, result.Count);
        }

        //[Fact]
        public async Task ReconcilePayment_ShouldReturnClaimId()
        {
            var paymentId = Fixture.Create<int[]>();
            var memberId = Fixture.Create<int>();
            var paymentEntity = Fixture.Build<PaymentEntity>()
                .With(x => x.Id, paymentId[0])
                .With(x => x.PaymentAmount, 200)
                .With(x => x.PaymentTypeId, (int)PaymentTypes.ClientPayment)
                .Create();

            var paymentClaimEntity = Fixture.Build<PaymentClaimEntity>()
                .With(x => x.PaymentId, paymentId[0])
                .With(x => x.TotalPayment, 100)
                .Create();
            paymentEntity.PaymentClaims.Add(paymentClaimEntity);

            SetupPayments(paymentEntity);
            SetupPaymentClaims(paymentClaimEntity);
            SetupClaims(paymentClaimEntity.ClaimId.Value);

            var result = await _paymentPostingService.ReconcilePaymentAsync(paymentId, memberId);

            Assert.Contains(paymentClaimEntity.ClaimId.Value, result);
        }

        //[Fact]
        public async Task ReconcilePayment_ShouldReturnCannotReconcileErrorString()
        {
            var paymentId = Fixture.Create<int[]>();
            var memberId = Fixture.Create<int>();
            var paymentEntity = Fixture.Build<PaymentEntity>()
                .With(x => x.Id, paymentId[0])
                .With(x => x.PaymentAmount, 200)
                .With(x => x.PaymentTypeId, (int)PaymentTypes.ClientPayment)
                .Create();

            SetupPayments(paymentEntity);

            Func<Task> act = (async () => await _paymentPostingService.ReconcilePaymentAsync(paymentId, memberId));

            var resultException = await Assert.ThrowsAsync<Exception>(act);

            Assert.Equal("Cannot reconcile payment without payment claims", resultException.Message);
        }

        //[Fact]
        public async Task ReconcilePayment_ShouldReturnNotValidPaymentErrorString()
        {
            var paymentId = Fixture.Create<int[]>();
            var memberId = Fixture.Create<int>();
            var paymentEntity = Fixture.Build<PaymentEntity>()
                .With(x => x.Id, paymentId[0])
                .With(x => x.PaymentAmount, 200)
                .With(x => x.PaymentTypeId, (int)PaymentTypes.ClientPayment)
                .Create();

            var paymentClaimEntity = Fixture.Build<PaymentClaimEntity>()
                .With(x => x.PaymentId, paymentId[0])
                .With(x => x.TotalPayment, 300)
                .Create();
            paymentEntity.PaymentClaims.Add(paymentClaimEntity);

            SetupPayments(paymentEntity);
            SetupPaymentClaims(paymentClaimEntity);

            Func<Task> act = (async () => await _paymentPostingService.ReconcilePaymentAsync(paymentId, memberId));

            var resultException = await Assert.ThrowsAsync<Exception>(act);

            Assert.Equal("Not valid payment", resultException.Message);
        }

        //[Fact]
        public async Task ReconcilePayment_ShouldAlsoReturnNotValidPaymentErrorString()
        {
            var paymentId = Fixture.Create<int[]>();
            var memberId = Fixture.Create<int>();
            var paymentEntity = Fixture.Build<PaymentEntity>()
                .With(x => x.Id, paymentId[0])
                .With(x => x.PaymentAmount, 200)
                .With(x => x.PaymentTypeId, (int)PaymentTypes.ERAReceived)
                .Create();

            var paymentClaimEntity = Fixture.Build<PaymentClaimEntity>()
                .With(x => x.PaymentId, paymentId[0])
                .With(x => x.TotalPayment, 100)
                .Create();
            paymentEntity.PaymentClaims.Add(paymentClaimEntity);

            SetupPayments(paymentEntity);
            SetupPaymentClaims(paymentClaimEntity);

            Func<Task> act = (async () => await _paymentPostingService.ReconcilePaymentAsync(paymentId, memberId));

            var resultException = await Assert.ThrowsAsync<Exception>(act);

            Assert.Equal("Not valid payment", resultException.Message);
        }

        private void SetupPayments(PaymentEntity paymentEntity)
        {
            _paymentRepository.Setup(x => x.Query())
                .Returns(QueryMock<PaymentEntity>.Create(paymentEntity));

            _paymentRepository.Setup(x => x.GetAllAsync(
                    It.IsAny<Expression<Func<PaymentEntity, bool>>>(), null))
                .ReturnsAsync(QueryMock<PaymentEntity>.Create(paymentEntity));
        }

        private void SetupPaymentClaims(PaymentClaimEntity paymentClaimEntity)
        {
            _paymentClaimRepository.Setup(x => x.Query())
                .Returns(QueryMock<PaymentClaimEntity>.Create(paymentClaimEntity));
        }

        private void SetupClaims(int claimId)
        {
            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, claimId)
                .Create();

            _claimRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(claim);
        }

        private void SetupMapper()
        {
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MapperProfile());
            });

            _mapper = mapperConfig.CreateMapper();
        }


        [Theory]
        [InlineData(100)]
        [InlineData(-50)]
        public async Task AddUnAllocatedPayments_ShouldAddNewRecordAndCommit(decimal payment)
        {
            // Arrange
            var model = new UnAllocatedPaymentsModel
            {
                AccountInfoId = 200,
                PaymentId = 20,
                ChildProfileId = 300,
                UnAllocatedAmount = payment,
                Notes = "New unallocated payment",
                MemberId = 199
            };

            var mockRepo = new Mock<IRepository<BillingDbContext, UnAllocatedPaymentEntity>>();
            mockRepo.Setup(r => r.Query())
                .Returns(QueryMock<UnAllocatedPaymentEntity>.Create());

            mockRepo.Setup(r => r.Add(It.IsAny<UnAllocatedPaymentEntity>())).Verifiable();
            mockRepo.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask).Verifiable();

            mockRepo.Setup(r => r.Query())
                .Returns(new List<UnAllocatedPaymentEntity>().AsQueryable().BuildMockDbSet().Object);

            var service = new PaymentPostingService(
                _paymentRepository.Object,
                _paymentClaimRepository.Object,
                _paymentMethodService.Object,
                _mapper,
                _paymentEraUploadRepository.Object,
                _blobProcessingService.Object,
                _billingBlobService.Object,
                _serviceBusConnectionFactory.Object,
                _claimHistoryService.Object,
                _chargeEntryService.Object,
                _claimManagerService.Object,
                _rethinkMasterDataMicroServices.Object,
                _paymentClaimServiceLineAdjustmentRepository.Object,
                _paymentClaimServiceLineRepository.Object,

                _bus.Object,
                _billingFilePath.Object,
                _claimRepository.Object,
                mockRepo.Object,
                _Configuration.Object,
                _blobBackupService.Object,
                _cacheService.Object
            );

            // Act
            await service.AddUnAllocatedPayments(model);

            // Assert
            mockRepo.Verify(r => r.Add(It.Is<UnAllocatedPaymentEntity>(p =>
                p.AccountInfoId == model.AccountInfoId &&
                p.PaymentId == model.PaymentId &&
                p.ChildProfileId == model.ChildProfileId &&
                p.UnAllocatedAmount == model.UnAllocatedAmount &&
                p.Notes == model.Notes
            )), Times.Once);

            mockRepo.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task GetUnAllocatedPaymentsById_ShouldReturnCorrectModel_WhenRecordExists()
        {
            // Arrange
            var requestModel = Fixture.Build<UnAllocatedPaymentRequestModel>()
                .With(x => x.PaymentId, 101)
                .With(x => x.ChildProfileId, 202)
                .With(x => x.AccountInfoId, 999)
                .With(x => x.MemberId, 50)
                .Create();

            var unAllocatedPaymentEntity = Fixture.Build<UnAllocatedPaymentEntity>()
                .With(x => x.Id, 1)
                .With(x => x.PaymentId, requestModel.PaymentId)
                .With(x => x.ChildProfileId, requestModel.ChildProfileId)
                .With(x => x.UnAllocatedAmount, 100m)
                .With(x => x.AccountInfoId, requestModel.AccountInfoId)
                .With(x => x.DateDeleted, (DateTime?)null)
                .With(x => x.Notes, "Sample unallocated note")
                .With(x => x.DateCreated, DateTime.UtcNow)
                .Create();

            var queryable = new List<UnAllocatedPaymentEntity> { unAllocatedPaymentEntity }.AsQueryable();

            _unAllocatedPaymentRepository.Setup(r => r.Query())
                .Returns(queryable.BuildMockDbSet().Object);

            // Act
            var result = await _paymentPostingService.GetUnAllocatedPaymentsById(requestModel);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(requestModel.PaymentId, result.PaymentId);
            Assert.Equal(requestModel.ChildProfileId, result.ChildProfileId);
            Assert.Equal(requestModel.AccountInfoId, result.AccountInfoId);
            Assert.Equal(unAllocatedPaymentEntity.Notes, result.Notes);
            Assert.Equal(unAllocatedPaymentEntity.UnAllocatedAmount, result.UnAllocatedAmount);
            Assert.Equal(requestModel.MemberId, result.MemberId);
        }

        [Fact]
        public async Task GetUnAllocatedPaymentsById_ShouldReturnMappedModel_WhenRecordExists()
        {
            // Arrange
            var requestModel = new UnAllocatedPaymentRequestModel
            {
                PaymentId = 100,
                ChildProfileId = 200,
                AccountInfoId = 300,
                MemberId = 400
            };

            var entity = new UnAllocatedPaymentEntity
            {
                Id = 24,
                PaymentId = 100,
                ChildProfileId = 200,
                UnAllocatedAmount = 150,
                Notes = "Test Notes",
                DateDeleted = null,
                DateCreated = DateTime.UtcNow
            };

            var queryable = new List<UnAllocatedPaymentEntity> { entity }.AsQueryable();

            _unAllocatedPaymentRepository
                .Setup(r => r.Query())
                .Returns(queryable.BuildMockDbSet().Object);

            // Act
            var result = await _paymentPostingService.GetUnAllocatedPaymentsById(requestModel);

            // Assert
            Assert.Equal(150, result.UnAllocatedAmount);
            Assert.Equal("Test Notes", result.Notes);
            Assert.Equal(300, result.AccountInfoId);
            Assert.Equal(400, result.MemberId);
        }

        [Fact]
        public async Task GetGuarantorDetailsById_ReturnsGuarantor_WhenCacheHit()
        {
            // Arrange
            var model = new ClientHistoryUserInfo { AccountInfoId = 100, ClientId = 10 };

            var clientDetails = new List<RethinkGuarantorDetails.ClientModel>
            {
                new RethinkGuarantorDetails.ClientModel { Id = 1, UserId = 10 }
            };

            _cacheService
                .Setup(x => x.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<RethinkGuarantorDetails.ClientModel>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(clientDetails);

            // Act
            var result = await _paymentPostingService.GetGuarantorDetailsById(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.UserId);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task GetGuarantorDetailsById_ReturnsGuarantor_WhenCacheMiss()
        {
            // Arrange
            var model = new ClientHistoryUserInfo { AccountInfoId = 100, ClientId = 10 };

            var clientDetailsFromService = new List<RethinkGuarantorDetails.ClientModel>
            {
                new RethinkGuarantorDetails.ClientModel { Id = 1, UserId = 10 }
            };

            // Simulate cache-miss: the factory delegate will be called
            _cacheService
                .Setup(x => x.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<RethinkGuarantorDetails.ClientModel>>>>(),
                    It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<List<RethinkGuarantorDetails.ClientModel>>>, TimeSpan>(
                    async (key, factory, ts) => await factory()
                );

            _rethinkMasterDataMicroServices
                .Setup(x => x.GetClientDetailsGuarantor(model.AccountInfoId))
                .ReturnsAsync(clientDetailsFromService);

            // Act
            var result = await _paymentPostingService.GetGuarantorDetailsById(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.UserId);
            Assert.Equal(1, result.Id);
            _rethinkMasterDataMicroServices.Verify(x => x.GetClientDetailsGuarantor(model.AccountInfoId), Times.Once);
        }

        [Fact]
        public async Task GetGuarantorDetailsById_ReturnsEmptyModel_WhenClientNotFound()
        {
            // Arrange
            var model = new ClientHistoryUserInfo { AccountInfoId = 300, ClientId = 99 };

            _cacheService
                .Setup(x => x.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<RethinkGuarantorDetails.ClientModel>>>>(),
                    It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<List<RethinkGuarantorDetails.ClientModel>>>, TimeSpan>(async (key, factory, ts) => await factory());

            _rethinkMasterDataMicroServices
                .Setup(x => x.GetClientDetailsGuarantor(model.AccountInfoId))
                .ReturnsAsync(new List<RethinkGuarantorDetails.ClientModel>());

            var expectedCacheKey = $"{guarantorDetailsCodeKey}{model.AccountInfoId}";

            // Act
            var result = await _paymentPostingService.GetGuarantorDetailsById(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.UserId);
            Assert.Equal(0, result.Id);
            _rethinkMasterDataMicroServices.Verify(x => x.GetClientDetailsGuarantor(model.AccountInfoId), Times.Once);
        }

        [Fact]
        public async Task GetAllPayments_ShouldConvertManualFilterToTrue()
        {
            var accountInfoId = Fixture.Create<int>();

            var model = new GetPaymentsModel
            {
                AccountInfoId = accountInfoId,
                Skip = 0,
                Take = 10,
                SortingModels = new List<SortingModel>(),
                FilterModels = new List<FilterModel>
{
    new FilterModel { OperatorName="eq", PropertyName = "ismanual", Value = "manual" }
}
            };

            SetupAccountInfo(accountInfoId);
            SetupPayments(CreateValidPayment(accountInfoId));

            await _paymentPostingService.GetAllPayments(model);

            Assert.Equal("true", model.FilterModels.First().Value);
        }

        [Fact]
        public async Task GetAllPayments_ShouldConvertElectronicFilterToFalse()
        {
            var accountInfoId = Fixture.Create<int>();

            var model = CreateBasicModel(accountInfoId);
            model.FilterModels.Add(new FilterModel
            {
                PropertyName = "ismanual",
                OperatorName = "eq",
                Value = "electronic"
            });

            SetupAccountInfo(accountInfoId);
            SetupPayments(CreateValidPayment(accountInfoId));

            await _paymentPostingService.GetAllPayments(model);

            Assert.Equal("false", model.FilterModels.First().Value);
        }

        [Fact]
        public async Task GetAllPayments_ShouldRemoveInvalidManualFilter()
        {
            var accountInfoId = Fixture.Create<int>();

            var model = CreateBasicModel(accountInfoId);
            model.FilterModels.Add(new FilterModel
            {
                PropertyName = "ismanual",
                OperatorName = "eq",
                Value = "invalid"
            });

            SetupAccountInfo(accountInfoId);
            SetupPayments(CreateValidPayment(accountInfoId));

            await _paymentPostingService.GetAllPayments(model);

            Assert.Empty(model.FilterModels);
        }


        private void SetupAccountInfo(int accountInfoId)
        {
            _rethinkMasterDataMicroServices
                .Setup(x => x.GetAccountReturningEntityAsync(accountInfoId, false))
                .ReturnsAsync(new AccountInfoEntityModel
                {
                    subscriptionFeatures = new Dictionary<string, object>
                    {
        { "showRevSpring", true }
                    }
                });
        }

        private GetPaymentsModel CreateBasicModel(int accountInfoId)
        {
            return new GetPaymentsModel
            {
                AccountInfoId = accountInfoId,
                Skip = 0,
                Take = 10,
                SortingModels = new List<SortingModel>(),
                FilterModels = new List<FilterModel>()
            };
        }

        private PaymentEntity CreateValidPayment(int accountInfoId)
        {
            return new PaymentEntity
            {
                Id = 1,
                AccountInfoId = accountInfoId,
                PaymentAmount = 100,
                ReceivedDate = DateTime.Now,
                IsManualPayment = true,
                PaymentMethodId = (int)PaymentMethods.Cash,
                PaymentTypeEntity = new PaymentTypeEntity { Name = "Test" },
                PaymentClaims = new List<PaymentClaimEntity>()
            };
        }


        [Fact]
        public void GetPaymentMethods_ShouldNotReturnNullOrEmpty()
        {
            var result = _paymentPostingService.GetPaymentMethods();

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void GetPaymentMethods_ShouldReturnCorrectEnumValues()
        {
            var result = _paymentPostingService.GetPaymentMethods();

            foreach (var item in result)
            {
                Assert.True(Enum.IsDefined(typeof(PaymentMethods), item.EnumValue));
            }
        }

        [Fact]
        public void GetPaymentMethods_ShouldUseDescriptionAttribute_WhenPresent()
        {
            var result = _paymentPostingService.GetPaymentMethods();

            var enumWithDescription = typeof(PaymentMethods)
                .GetMembers()
                .Where(m => m.GetCustomAttributes(typeof(DescriptionAttribute), false).Any())
                .Select(m => m.Name)
                .FirstOrDefault();

            if (enumWithDescription == null)
                return;

            var expectedDescription =
                typeof(PaymentMethods)
                    .GetMember(enumWithDescription)
                    .First()
                    .GetCustomAttribute<DescriptionAttribute>()
                    .Description;

            var method = result.First(x => x.DisplayName == expectedDescription);

            Assert.Equal(expectedDescription, method.DisplayName);
        }

        [Fact]
        public void GetPaymentMethods_ShouldFallbackToEnumName_WhenNoDescription()
        {
            var result = _paymentPostingService.GetPaymentMethods();

            var enumWithoutDescription = Enum.GetValues(typeof(PaymentMethods))
                .Cast<PaymentMethods>()
                .FirstOrDefault(e =>
                {
                    var member = typeof(PaymentMethods)
                        .GetMember(e.ToString())
                        .First();

                    return member.GetCustomAttribute<DescriptionAttribute>() == null;
                });

            if (EqualityComparer<PaymentMethods>.Default.Equals(enumWithoutDescription, default))
                return;

            var expectedName = enumWithoutDescription.ToString();

            var method = result.First(x => x.EnumValue == (int)enumWithoutDescription);

            Assert.Equal(expectedName, method.DisplayName);
        }


        [Fact]
        public void GetPaymentMethods_ShouldNotContainDuplicateEnumValues()
        {
            var result = _paymentPostingService.GetPaymentMethods();

            var duplicates = result
                .GroupBy(x => x.EnumValue)
                .Where(g => g.Count() > 1)
                .ToList();

            Assert.Empty(duplicates);
        }

        [Fact]
        public void GetPaymentMethods_ShouldPreserveEnumOrder()
        {
            var result = _paymentPostingService.GetPaymentMethods();

            var enumValues = Enum.GetValues(typeof(PaymentMethods))
                                 .Cast<PaymentMethods>()
                                 .Select(e => (int)e)
                                 .ToList();

            var resultValues = result.Select(x => x.EnumValue).ToList();

            Assert.Equal(enumValues, resultValues);
        }

        [Fact]
        public void GetPaymentMethods_ShouldReturnCorrectDisplayName_ForEachEnum()
        {
            var result = _paymentPostingService.GetPaymentMethods();

            foreach (PaymentMethods enumItem in Enum.GetValues(typeof(PaymentMethods)))
            {
                var memberInfo = typeof(PaymentMethods).GetMember(enumItem.ToString()).First();

                var descriptionAttribute =
                    memberInfo.GetCustomAttribute<DescriptionAttribute>();

                var expectedName = descriptionAttribute?.Description ?? enumItem.ToString();

                var actual = result.First(x => x.EnumValue == (int)enumItem);

                Assert.Equal(expectedName, actual.DisplayName);
            }
        }

        [Fact]
        public void GetPaymentMethods_ShouldMatchExactEnumIntegerValues()
        {
            var result = _paymentPostingService.GetPaymentMethods();

            var enumValues = Enum.GetValues(typeof(PaymentMethods))
                                 .Cast<PaymentMethods>()
                                 .ToList();

            foreach (var enumItem in enumValues)
            {
                var match = result.FirstOrDefault(x => x.EnumValue == (int)enumItem);

                Assert.NotNull(match);
                Assert.Equal((int)enumItem, match.EnumValue);
            }
        }



        [Fact]
        public async Task AddUnAllocatedPayments_ShouldUpdateExistingRecord()
        {
            // Arrange
            var existingEntity = new UnAllocatedPaymentEntity
            {
                AccountInfoId = 200,
                PaymentId = 20,
                ChildProfileId = 300,
                UnAllocatedAmount = 500,
                Notes = "Old Notes",
                GuarantorContactId = 10
            };

            var model = new UnAllocatedPaymentsModel
            {
                AccountInfoId = 200,
                PaymentId = 20,
                ChildProfileId = 300,
                UnAllocatedAmount = 700,
                Notes = "Updated Notes",
                GuarantorContactId = 15,
                MemberId = 199
            };

            var data = new List<UnAllocatedPaymentEntity> { existingEntity }
                .AsQueryable()
                .BuildMockDbSet();

            var mockRepo = new Mock<IRepository<BillingDbContext, UnAllocatedPaymentEntity>>();
            mockRepo.Setup(r => r.Query()).Returns(data.Object);
            mockRepo.Setup(r => r.Update(It.IsAny<UnAllocatedPaymentEntity>())).Verifiable();
            mockRepo.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            var service = CreateService(mockRepo);

            // Act
            await service.AddUnAllocatedPayments(model);

            // Assert
            mockRepo.Verify(r => r.Update(It.Is<UnAllocatedPaymentEntity>(x =>
                x.UnAllocatedAmount == 700 &&
                x.Notes == "Updated Notes" &&
                x.GuarantorContactId == 15
            )), Times.Once);

            mockRepo.Verify(r => r.Add(It.IsAny<UnAllocatedPaymentEntity>()), Times.Never);
            mockRepo.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task AddUnAllocatedPayments_ShouldNotOverwriteAmount_WhenZero()
        {
            var existingEntity = new UnAllocatedPaymentEntity
            {
                PaymentId = 20,
                ChildProfileId = 300,
                UnAllocatedAmount = 500,
                Notes = "Old Notes"
            };

            var model = new UnAllocatedPaymentsModel
            {
                PaymentId = 20,
                ChildProfileId = 300,
                UnAllocatedAmount = 0,
                Notes = "Updated",
                MemberId = 1
            };

            var mockRepo = SetupMockRepoWithData(existingEntity);

            var service = CreateService(mockRepo);

            await service.AddUnAllocatedPayments(model);

            Assert.Equal(500, existingEntity.UnAllocatedAmount);
        }


        [Fact]
        public async Task AddUnAllocatedPayments_ShouldAddNewRecord_WhenAmountZero()
        {
            var model = new UnAllocatedPaymentsModel
            {
                AccountInfoId = 1,
                PaymentId = 99,
                ChildProfileId = 50,
                UnAllocatedAmount = 0,
                Notes = "Test",
                MemberId = 1
            };

            var mockRepo = new Mock<IRepository<BillingDbContext, UnAllocatedPaymentEntity>>();
            mockRepo.Setup(r => r.Query())
                .Returns(new List<UnAllocatedPaymentEntity>()
                .AsQueryable()
                .BuildMockDbSet().Object);

            mockRepo.Setup(r => r.Add(It.IsAny<UnAllocatedPaymentEntity>())).Verifiable();
            mockRepo.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            var service = CreateService(mockRepo);

            await service.AddUnAllocatedPayments(model);

            mockRepo.Verify(r => r.Add(It.Is<UnAllocatedPaymentEntity>(x =>
                x.UnAllocatedAmount == 0
            )), Times.Once);
        }

        [Fact]
        public async Task DeletePaymentAsync_NoPaymentsFound_ReturnsEmptyList()
        {
            // Arrange
            var paymentIds = new[] { 1 };
            var payments = new List<PaymentEntity>();

            var mockQueryable = payments.AsQueryable().BuildMock();

            _paymentRepository
                .Setup(x => x.Query())
                .Returns(mockQueryable);

            // Act
            var result = await _paymentPostingService
                .DeletePaymentAsync(paymentIds, 10, 100);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _paymentRepository.Verify(x => x.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task DeletePaymentAsync_WithClaimId_NoMatchingChargePayment_DeletesSuccessfully()
        {
            // Arrange
            var paymentId = 1;
            var claimId = 99;

            var adjustment = new PaymentClaimServiceLineAdjustmentEntity
            {
                AdjustmentAmount = 50,
                DateDeleted = null
            };

            var serviceLine = new PaymentClaimServiceLineEntity
            {
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity> { adjustment }
            };

            var paymentClaim = new PaymentClaimEntity
            {
                ClaimId = claimId,
                DateDeleted = null,
                PaymentClaimServiceLines = new List<PaymentClaimServiceLineEntity> { serviceLine }
            };

            var payment = new PaymentEntity
            {
                Id = paymentId,
                AccountInfoId = 100,
                DateDeleted = null,
                PaymentIdentifier = "PAY123",
                PaymentClaims = new List<PaymentClaimEntity> { paymentClaim }
            };

            var payments = new List<PaymentEntity> { payment };

            var mockQueryable = payments.AsQueryable().BuildMock();

            _paymentRepository
                .Setup(x => x.Query())
                .Returns(mockQueryable);

            // Act
            var result = await _paymentPostingService
                .DeletePaymentAsync(new[] { paymentId }, 10, 100);

            // Assert
            Assert.Single(result);
            Assert.Contains(claimId, result);

            //_claimHistoryService.Verify(x =>
            //    x.AddAsync(It.IsAny<List<ClaimHistorySaveModel>>(), false),
            //    Times.Once);

            _paymentRepository.Verify(x => x.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task DeletePaymentAsync_WithMatchingChargePayment_SoftDeletesChargePayment()
        {
            // Arrange
            var claimId = 5;

            var adjustment = new PaymentClaimServiceLineAdjustmentEntity
            {
                AdjustmentAmount = 100,
                DateDeleted = null
            };

            var serviceLine = new PaymentClaimServiceLineEntity
            {
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity> { adjustment }
            };

            var paymentClaim = new PaymentClaimEntity
            {
                ClaimId = claimId,
                DateDeleted = null,
                PaymentClaimServiceLines = new List<PaymentClaimServiceLineEntity> { serviceLine }
            };

            var payment = new PaymentEntity
            {
                Id = 1,
                AccountInfoId = 200,
                DateDeleted = null,
                PaymentIdentifier = "PAY456",
                PaymentClaims = new List<PaymentClaimEntity> { paymentClaim }
            };

            var mockQueryable = new List<PaymentEntity> { payment }
                .AsQueryable()
                .BuildMock();

            _paymentRepository
                .Setup(x => x.Query())
                .Returns(mockQueryable);

            // Act
            var result = await _paymentPostingService
                .DeletePaymentAsync(new[] { 1 }, 10, 200);

            // Assert
            Assert.Single(result);
            Assert.Equal(claimId, result.First());

            //_claimHistoryService.Verify(x =>
            //    x.AddAsync(It.IsAny<List<ClaimHistorySaveModel>>(), false),
            //    Times.Once);

            _paymentRepository.Verify(x => x.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task DeletePaymentAsync_WithNullClaimId_SkipsClaimHistory()
        {
            // Arrange
            var payment = new PaymentEntity
            {
                Id = 10,
                AccountInfoId = 500,
                DateDeleted = null,
                PaymentClaims = new List<PaymentClaimEntity>
        {
            new PaymentClaimEntity
            {
                ClaimId = null,
                DateDeleted = null,
                PaymentClaimServiceLines = new List<PaymentClaimServiceLineEntity>()
            }
        }
            };

            var mockQueryable = new List<PaymentEntity> { payment }
                .AsQueryable()
                .BuildMock();

            _paymentRepository
                .Setup(x => x.Query())
                .Returns(mockQueryable);

            _chargeEntryService
                .Setup(x => x.GetChargeEntitiesWithChargePaymentsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<ClaimChargeEntryEntity>());

            // Act
            var result = await _paymentPostingService
                .DeletePaymentAsync(new[] { 10 }, 10, 500);

            // Assert
            Assert.Empty(result);

            _claimHistoryService.Verify(x =>
                x.AddAsync(It.IsAny<List<ClaimHistorySaveModel>>(), false),
                Times.Never);

            _paymentRepository.Verify(x => x.CommitAsync(), Times.Once);
        }

        private PaymentPostingService CreateService(
        Mock<IRepository<BillingDbContext, UnAllocatedPaymentEntity>> mockRepo)
        {
            return new PaymentPostingService(
                _paymentRepository.Object,
                _paymentClaimRepository.Object,
                _paymentMethodService.Object,
                _mapper,
                _paymentEraUploadRepository.Object,
                _blobProcessingService.Object,
                _billingBlobService.Object,
                _serviceBusConnectionFactory.Object,
                _claimHistoryService.Object,
                _chargeEntryService.Object,
                _claimManagerService.Object,
                _rethinkMasterDataMicroServices.Object,
                _paymentClaimServiceLineAdjustmentRepository.Object,
                _paymentClaimServiceLineRepository.Object,
                _bus.Object,
                _billingFilePath.Object,
                _claimRepository.Object,
                mockRepo.Object,
                _Configuration.Object,
                _blobBackupService.Object,
                _cacheService.Object
            );
        }

        private Mock<IRepository<BillingDbContext, UnAllocatedPaymentEntity>>
            SetupMockRepoWithData(UnAllocatedPaymentEntity entity)
        {
            var data = new List<UnAllocatedPaymentEntity> { entity }
                .AsQueryable()
                .BuildMockDbSet();

            var mockRepo = new Mock<IRepository<BillingDbContext, UnAllocatedPaymentEntity>>();
            mockRepo.Setup(r => r.Query()).Returns(data.Object);
            mockRepo.Setup(r => r.Update(It.IsAny<UnAllocatedPaymentEntity>()));
            mockRepo.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            return mockRepo;
        }

        [Fact]
        public void PrepareClaimTransactions_ShouldAddDeleteChargePaymentTransactions()
        {
            // Arrange
            var serviceLineIdsToSend = new List<int> { 10, 20, 30 };
            var claimTransactionData = new List<ClaimTransactionModel>();

            var paymentTypeId = (int)PaymentTypes.ClientPayment; // any valid enum value

            // Act
            var result = _paymentPostingService.PrepareClaimTransactions(
                claimTransactionData,
                serviceLineIdsToSend,
                paymentTypeId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(serviceLineIdsToSend.Count, result.Count);

            foreach (var serviceLineId in serviceLineIdsToSend)
            {
                Assert.Contains(result, x =>
                    x.TransactionTypeId == serviceLineId &&
                    x.TransactionType == (int)ClaimTransactionType.deleteChargePayment);
            }
        }

        [Fact]
        public void PrepareClaimTransactions_ShouldAppendToExistingList()
        {
            // Arrange
            var existingTransaction = new ClaimTransactionModel
            {
                TransactionTypeId = 1,
                TransactionType = (int)ClaimTransactionType.billedAmount
            };

            var claimTransactionData = new List<ClaimTransactionModel> { existingTransaction };
            var serviceLineIdsToSend = new List<int> { 100 };
            var paymentTypeId = (int)PaymentTypes.ClientPayment;

            // Act
            var result = _paymentPostingService.PrepareClaimTransactions(
                claimTransactionData,
                serviceLineIdsToSend,
                paymentTypeId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(existingTransaction, result);
        }

        [Fact]
        public void PrepareClaimTransactions_ShouldReturnSameList_WhenServiceLineIdsEmpty()
        {
            // Arrange
            var claimTransactionData = new List<ClaimTransactionModel>
            {
                new ClaimTransactionModel
                {
                    TransactionTypeId = 1,
                    TransactionType = (int)ClaimTransactionType.insurancePayment
                }
            };

            var serviceLineIdsToSend = new List<int>();
            var paymentTypeId = (int)PaymentTypes.InsurancePayment;

            // Act
            var result = _paymentPostingService.PrepareClaimTransactions(
                claimTransactionData,
                serviceLineIdsToSend,
                paymentTypeId);

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result[0].TransactionTypeId);
        }

        [Fact]
        public void PrepareClaimTransactions_ShouldWork_WhenInitialListIsEmpty()
        {
            // Arrange
            var claimTransactionData = new List<ClaimTransactionModel>();
            var serviceLineIdsToSend = new List<int> { 5 };
            var paymentTypeId = (int)PaymentTypes.ClientPayment;

            // Act
            var result = _paymentPostingService.PrepareClaimTransactions(
                claimTransactionData,
                serviceLineIdsToSend,
                paymentTypeId);

            // Assert
            Assert.Single(result);
            Assert.Equal(5, result[0].TransactionTypeId);
            Assert.Equal((int)ClaimTransactionType.patientResponsibility,
                         result[0].TransactionTypeId);
        }

        [Fact]
        public void PrepareClaimTransactions_ShouldAddAllServiceLines()
        {
            // Arrange
            var claimTransactionData = new List<ClaimTransactionModel>();
            var serviceLineIdsToSend = new List<int> { 10, 20, 30, 40 };
            var paymentTypeId = (int)PaymentTypes.ERAReceived;

            // Act
            var result = _paymentPostingService.PrepareClaimTransactions(
                claimTransactionData,
                serviceLineIdsToSend,
                paymentTypeId);

            // Assert
            Assert.Equal(4, result.Count);

            foreach (var id in serviceLineIdsToSend)
            {
                Assert.Contains(result,
                    x => x.TransactionTypeId == id &&
                         x.TransactionType == (int)ClaimTransactionType.deleteChargePayment);
            }
        }

        [Fact]
        public void PrepareClaimTransactions_ShouldExecuteFindClaimTransactionTypeId_ForDifferentPaymentType()
        {
            // Arrange
            var claimTransactionData = new List<ClaimTransactionModel>();
            var serviceLineIdsToSend = new List<int> { 1 };

            var paymentTypeId = (int)PaymentTypes.Adjustment;

            // Act
            var result = _paymentPostingService.PrepareClaimTransactions(
                claimTransactionData,
                serviceLineIdsToSend,
                paymentTypeId);

            // Assert
            Assert.Single(result);
        }



        [Fact]
        public async Task GetERAErrors_ShouldReturnAllErrorLevels()
        {
            // Arrange
            var paymentId = 1;

            var payment = new PaymentEntity
            {
                Id = paymentId,
                PaymentEraUpload = new PaymentEraUploadEntity
                {
                    FileName = "test.era"
                },
                PaymentErrors = new List<PaymentErrorEntity>
        {
            new PaymentErrorEntity { ErrorMessage = "Payment level error" }
        }
            };

            var payments = new List<PaymentEntity> { payment }
                .AsQueryable()
                .BuildMock();

            _paymentRepository
                .Setup(x => x.Query())
                .Returns(payments);

            var claim = new PaymentClaimEntity
            {
                Id = 10,
                PaymentId = paymentId,
                ClaimIdentifier = "CLAIM-1",
                PaymentClaimErrors = new List<PaymentClaimErrorEntity>
        {
            new PaymentClaimErrorEntity { ErrorMessage = "Claim level error" }
        }
            };

            var claims = new List<PaymentClaimEntity> { claim }
                .AsQueryable()
                .BuildMock();

            _paymentClaimRepository
                .Setup(x => x.Query())
                .Returns(claims);

            var serviceLine = new PaymentClaimServiceLineEntity
            {
                Id = 100,
                PaymentClaimId = claim.Id,
                ServiceCode = "SVC-1",
                PaymentClaimServiceLineErrors = new List<PaymentClaimServiceLineErrorEntity>
        {
            new PaymentClaimServiceLineErrorEntity { ErrorMessage = "Service line error" }
        }
            };

            var serviceLines = new List<PaymentClaimServiceLineEntity> { serviceLine }
                .AsQueryable()
                .BuildMock();

            _paymentClaimServiceLineRepository
                .Setup(x => x.Query())
                .Returns(serviceLines);

            var model = new ERAUploadModel
            {
                PaymentIds = new List<int> { paymentId }
            };

            // Act
            var result = await _paymentPostingService.GetERAErrors(model);

            // Assert
            Assert.Contains("File name: test.era", result);
            Assert.Contains("Payment level error", result);
            Assert.Contains("Claim level error", result);
            Assert.Contains("Service line error", result);
        }

        [Fact]
        public async Task GetERAErrors_ShouldReturnEmpty_WhenNoPaymentsExist()
        {
            // Arrange
            var payments = new List<PaymentEntity>()
                .AsQueryable()
                .BuildMock();

            _paymentRepository
                .Setup(x => x.Query())
                .Returns(payments);

            var model = new ERAUploadModel
            {
                PaymentIds = new List<int> { 99 }
            };

            // Act
            var result = await _paymentPostingService.GetERAErrors(model);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GetERAErrors_ShouldReturnEmpty_WhenNoErrorsFound()
        {
            // Arrange
            var paymentId = 5;

            var payment = new PaymentEntity
            {
                Id = paymentId,
                PaymentErrors = new List<PaymentErrorEntity>(),
                PaymentEraUpload = new PaymentEraUploadEntity
                {
                    FileName = "clean.era"
                }
            };

            var payments = new List<PaymentEntity> { payment }
                .AsQueryable()
                .BuildMock();

            _paymentRepository
                .Setup(x => x.Query())
                .Returns(payments);

            var emptyClaims = new List<PaymentClaimEntity>()
                .AsQueryable()
                .BuildMock();

            _paymentClaimRepository
                .Setup(x => x.Query())
                .Returns(emptyClaims);

            var model = new ERAUploadModel
            {
                PaymentIds = new List<int> { paymentId }
            };

            // Act
            var result = await _paymentPostingService.GetERAErrors(model);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task HideProcessingInfoAsync_ShouldReturn_WhenPaymentIdsIsNull()
        {
            // Arrange
            var model = new HideProcessingInfoModelWithUserInfo
            {
                PaymentIds = null,
                MemberId = 1
            };

            // Act
            await _paymentPostingService.HideProcessingInfoAsync(model);

            // Assert
            _paymentRepository.Verify(x => x.Query(), Times.Never);
            _paymentRepository.Verify(x => x.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task HideProcessingInfoAsync_ShouldCommit_WhenPaymentIdsEmpty()
        {
            // Arrange
            var model = new HideProcessingInfoModelWithUserInfo
            {
                PaymentIds = new List<int>(),
                MemberId = 1
            };

            var payments = new List<PaymentEntity>()
                .AsQueryable()
                .BuildMock();

            _paymentRepository
                .Setup(x => x.Query())
                .Returns(payments);

            // Act
            await _paymentPostingService.HideProcessingInfoAsync(model);

            // Assert
            _paymentRepository.Verify(x => x.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task HideProcessingInfoAsync_ShouldUpdatePaymentAndCommit()
        {
            // Arrange
            var paymentId = 10;

            var payment = new PaymentEntity
            {
                Id = paymentId,
                HasAcknowledgedErrors = false
            };

            var payments = new List<PaymentEntity> { payment }
                .AsQueryable()
                .BuildMock();

            _paymentRepository
                .Setup(x => x.Query())
                .Returns(payments);

            var model = new HideProcessingInfoModelWithUserInfo
            {
                PaymentIds = new List<int> { paymentId },
                MemberId = 5
            };

            // Act
            await _paymentPostingService.HideProcessingInfoAsync(model);

            // Assert
            Assert.True(payment.HasAcknowledgedErrors);
            _paymentRepository.Verify(x => x.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task HideProcessingInfoAsync_ShouldUpdateAllPayments()
        {
            // Arrange
            var payment1 = new PaymentEntity { Id = 1, HasAcknowledgedErrors = false };
            var payment2 = new PaymentEntity { Id = 2, HasAcknowledgedErrors = false };

            var payments = new List<PaymentEntity> { payment1, payment2 }
                .AsQueryable()
                .BuildMock();

            _paymentRepository
                .Setup(x => x.Query())
                .Returns(payments);

            var model = new HideProcessingInfoModelWithUserInfo
            {
                PaymentIds = new List<int> { 1, 2 },
                MemberId = 99
            };

            // Act
            await _paymentPostingService.HideProcessingInfoAsync(model);

            // Assert
            Assert.True(payment1.HasAcknowledgedErrors);
            Assert.True(payment2.HasAcknowledgedErrors);
            _paymentRepository.Verify(x => x.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task GetUploadAsync_ShouldThrowUnauthorized_WhenUserDoesNotOwnAttachment()
        {
            // Arrange
            var model = new IdWithUserInfo
            {
                Id = 1,
                MemberId = 10
            };

            var attachment = new PaymentEraUploadEntity
            {
                Id = 1,
                CreatedBy = 99,
                FilePath = "container/file.txt",
                FileName = "file.txt"
            };

            _paymentEraUploadRepository
                .Setup(x => x.GetByIdAsync(model.Id))
                .ReturnsAsync(attachment);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _paymentPostingService.GetUploadAsync(model));

            _blobProcessingService.Verify(
                x => x.DownloadBlobFromContainerAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GetUploadAsync_ShouldReturnAttachment_WhenUserIsOwner()
        {
            // Arrange
            var model = new IdWithUserInfo
            {
                Id = 2,
                MemberId = 50
            };

            var attachment = new PaymentEraUploadEntity
            {
                Id = 2,
                CreatedBy = 50,
                FilePath = "container1/file1.txt",
                FileName = "file1.txt"
            };

            var memoryStream = new MemoryStream();
            var writer = new StreamWriter(memoryStream);
            writer.Write("test content");
            writer.Flush();
            memoryStream.Position = 5;

            _paymentEraUploadRepository
                .Setup(x => x.GetByIdAsync(model.Id))
                .ReturnsAsync(attachment);

            _blobProcessingService
                .Setup(x => x.DownloadBlobFromContainerAsync("container1", "file1.txt"))
                .ReturnsAsync(memoryStream);

            // Act
            var result = await _paymentPostingService.GetUploadAsync(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("file1.txt", result.Filename);
            Assert.NotNull(result.MemoryStream);
            Assert.Equal(0, result.MemoryStream.Position);

            _blobProcessingService.Verify(
                x => x.DownloadBlobFromContainerAsync("container1", "file1.txt"),
                Times.Once);
        }

        [Fact]
        public async Task GetUploadAsync_ShouldUseFirstTwoSegments_FromFilePath()
        {
            // Arrange
            var model = new IdWithUserInfo
            {
                Id = 3,
                MemberId = 1
            };

            var attachment = new PaymentEraUploadEntity
            {
                Id = 3,
                CreatedBy = 1,
                FilePath = "containerA/folder/file.txt",
                FileName = "file.txt"
            };

            var memoryStream = new MemoryStream();

            _paymentEraUploadRepository
                .Setup(x => x.GetByIdAsync(model.Id))
                .ReturnsAsync(attachment);

            _blobProcessingService
                .Setup(x => x.DownloadBlobFromContainerAsync("containerA", "folder"))
                .ReturnsAsync(memoryStream);

            // Act
            var result = await _paymentPostingService.GetUploadAsync(model);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetUploadAsync_ShouldThrowNullReference_WhenAttachmentIsNull()
        {
            // Arrange
            var model = new IdWithUserInfo
            {
                Id = 5,
                MemberId = 1
            };

            _paymentEraUploadRepository
                .Setup(x => x.GetByIdAsync(model.Id))
                .ReturnsAsync((PaymentEraUploadEntity)null);

            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() =>
                _paymentPostingService.GetUploadAsync(model));
        }

        [Fact]
        public async Task GetUploadAsync_ShouldReturnAttachment_WhenAuthorized()
        {
            // Arrange
            var model = new IdWithUserInfo
            {
                Id = 2,
                MemberId = 50
            };

            var attachment = new PaymentEraUploadEntity
            {
                Id = 2,
                CreatedBy = 50,
                FilePath = "container1/file1.txt",
                FileName = "file1.txt"
            };

            var memoryStream = new MemoryStream();
            var writer = new StreamWriter(memoryStream);
            writer.Write("test data");
            writer.Flush();
            memoryStream.Position = 5;

            _paymentEraUploadRepository
                .Setup(x => x.GetByIdAsync(model.Id))
                .ReturnsAsync(attachment);

            _blobProcessingService
                .Setup(x => x.DownloadBlobFromContainerAsync("container1", "file1.txt"))
                .ReturnsAsync(memoryStream);

            // Act
            var result = await _paymentPostingService.GetUploadAsync(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("file1.txt", result.Filename);
            Assert.Equal(0, result.MemoryStream.Position);
        }

        [Fact]
        public async Task GetUploadAsync_ShouldUseFirstTwoSegments_WhenPathHasMultipleSegments()
        {
            // Arrange
            var model = new IdWithUserInfo
            {
                Id = 3,
                MemberId = 1
            };

            var attachment = new PaymentEraUploadEntity
            {
                Id = 3,
                CreatedBy = 1,
                FilePath = "containerA/folderB/file.txt",
                FileName = "file.txt"
            };

            var memoryStream = new MemoryStream();

            _paymentEraUploadRepository
                .Setup(x => x.GetByIdAsync(model.Id))
                .ReturnsAsync(attachment);

            _blobProcessingService
                .Setup(x => x.DownloadBlobFromContainerAsync("containerA", "folderB"))
                .ReturnsAsync(memoryStream);

            // Act
            var result = await _paymentPostingService.GetUploadAsync(model);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetUploadAsync_ShouldThrowIndexOutOfRange_WhenFilePathInvalid()
        {
            // Arrange
            var model = new IdWithUserInfo
            {
                Id = 4,
                MemberId = 1
            };

            var attachment = new PaymentEraUploadEntity
            {
                Id = 4,
                CreatedBy = 1,
                FilePath = "invalidpath",
                FileName = "file.txt"
            };

            _paymentEraUploadRepository
                .Setup(x => x.GetByIdAsync(model.Id))
                .ReturnsAsync(attachment);

            // Act & Assert
            await Assert.ThrowsAsync<IndexOutOfRangeException>(() =>
                _paymentPostingService.GetUploadAsync(model));
        }

        [Fact]
        public async Task StartPaymentParsingAsync_ShouldReturn_WhenUserIsNotOwner()
        {
            // Arrange
            var model = new IdWithUserInfo
            {
                Id = 1,
                MemberId = 10,
                AccountInfoId = 5
            };

            var file = new PaymentEraUploadEntity
            {
                Id = 1,
                CreatedBy = 99,
                FilePath = "file.txt"
            };

            _paymentEraUploadRepository
                .Setup(x => x.GetByIdAsync(model.Id))
                .ReturnsAsync(file);

            // Act
            await _paymentPostingService.StartPaymentParsingAsync(model);

            // Assert
            _serviceBusConnectionFactory.Verify(
                x => x.ConnectionStringBuilder,
                Times.Never);
        }

        [Fact]
        public async Task DeleteUploadAsync_ShouldThrowUnauthorized_WhenUserIsNotOwner()
        {
            // Arrange
            var model = new IdWithUserInfo
            {
                Id = 10,
                MemberId = 99
            };

            var attachment = new PaymentEraUploadEntity
            {
                Id = 10,
                CreatedBy = 1,
                FilePath = "container/file.edi"
            };

            _paymentEraUploadRepository
                .Setup(x => x.GetByIdAsync(model.Id))
                .ReturnsAsync(attachment);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _paymentPostingService.DeleteUploadAsync(model));

            _billingFilePath.Verify(x => x.SplitFilePath(It.IsAny<string>()), Times.Never);
            _paymentEraUploadRepository.Verify(x => x.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteUploadAsync_ShouldDeleteBlob_AndCommit_WhenUserIsOwner()
        {
            // Arrange
            var model = new IdWithUserInfo
            {
                Id = 10,
                MemberId = 1
            };

            var attachment = new PaymentEraUploadEntity
            {
                Id = 10,
                CreatedBy = 1,
                FilePath = "billing/file1.edi"
            };

            _paymentEraUploadRepository
                .Setup(x => x.GetByIdAsync(model.Id))
                .ReturnsAsync(attachment);

            _billingFilePath
                .Setup(x => x.SplitFilePath(attachment.FilePath))
                .ReturnsAsync(("billing", "file1.edi"));

            _blobProcessingService
                .Setup(x => x.DeleteBlobFromContainerAsync("billing", "file1.edi"))
                .Returns(Task.CompletedTask);

            _billingBlobService
                .Setup(x => x.DeleteBlobFromContainerAsync("billing", "file1.edi"))
                .Returns(Task.CompletedTask);

            _paymentEraUploadRepository
                .Setup(x => x.CommitAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _paymentPostingService.DeleteUploadAsync(model);

            // Assert
            _billingFilePath.Verify(
                x => x.SplitFilePath(attachment.FilePath),
                Times.Once);

            _blobProcessingService.Verify(
                x => x.DeleteBlobFromContainerAsync("billing", "file1.edi"),
                Times.Once);

            _billingBlobService.Verify(
                x => x.DeleteBlobFromContainerAsync("billing", "file1.edi"),
                Times.Once);

            _paymentEraUploadRepository.Verify(
                x => x.CommitAsync(),
                Times.Once);
        }

        [Fact]
        public async Task DeleteUploadAsync_ShouldPassCorrectFilePath_ToSplitFilePath()
        {
            // Arrange
            var model = new IdWithUserInfo
            {
                Id = 22,
                MemberId = 5
            };

            var attachment = new PaymentEraUploadEntity
            {
                Id = 22,
                CreatedBy = 5,
                FilePath = "mainContainer/subfolder/testfile.edi"
            };

            _paymentEraUploadRepository
                .Setup(x => x.GetByIdAsync(model.Id))
                .ReturnsAsync(attachment);

            _billingFilePath
                .Setup(x => x.SplitFilePath(It.IsAny<string>()))
                .ReturnsAsync(("mainContainer", "subfolder/testfile.edi"));

            _blobProcessingService
                .Setup(x => x.DeleteBlobFromContainerAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _billingBlobService
                .Setup(x => x.DeleteBlobFromContainerAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _paymentEraUploadRepository
                .Setup(x => x.CommitAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _paymentPostingService.DeleteUploadAsync(model);

            // Assert
            _billingFilePath.Verify(
                x => x.SplitFilePath("mainContainer/subfolder/testfile.edi"),
                Times.Once);
        }

        [Fact]
        public async Task DeleteUploadAsync_ShouldCallBothBlobServices()
        {
            // Arrange
            var model = new IdWithUserInfo
            {
                Id = 50,
                MemberId = 10
            };

            var attachment = new PaymentEraUploadEntity
            {
                Id = 50,
                CreatedBy = 10,
                FilePath = "billing/sample.edi"
            };

            _paymentEraUploadRepository
                .Setup(x => x.GetByIdAsync(model.Id))
                .ReturnsAsync(attachment);

            _billingFilePath
                .Setup(x => x.SplitFilePath(attachment.FilePath))
                .ReturnsAsync(("billing", "sample.edi"));

            _blobProcessingService
                .Setup(x => x.DeleteBlobFromContainerAsync("billing", "sample.edi"))
                .Returns(Task.CompletedTask)
                .Verifiable();

            _billingBlobService
                .Setup(x => x.DeleteBlobFromContainerAsync("billing", "sample.edi"))
                .Returns(Task.CompletedTask)
                .Verifiable();

            _paymentEraUploadRepository
                .Setup(x => x.CommitAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _paymentPostingService.DeleteUploadAsync(model);

            // Assert
            _blobProcessingService.Verify();
            _billingBlobService.Verify();
        }

        [Fact]
        public async Task DeleteUploadAsync_ShouldSoftDeleteAttachment()
        {
            // Arrange
            var model = new IdWithUserInfo
            {
                Id = 70,
                MemberId = 3
            };

            var attachment = new PaymentEraUploadEntity
            {
                Id = 70,
                CreatedBy = 3,
                FilePath = "container/file.edi"
            };

            _paymentEraUploadRepository
                .Setup(x => x.GetByIdAsync(model.Id))
                .ReturnsAsync(attachment);

            _billingFilePath
                .Setup(x => x.SplitFilePath(It.IsAny<string>()))
                .ReturnsAsync(("container", "file.edi"));

            _blobProcessingService
                .Setup(x => x.DeleteBlobFromContainerAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _billingBlobService
                .Setup(x => x.DeleteBlobFromContainerAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _paymentEraUploadRepository
                .Setup(x => x.CommitAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _paymentPostingService.DeleteUploadAsync(model);

            // Assert
            Assert.Equal(model.MemberId, attachment.ModifiedBy);
            Assert.NotNull(attachment.DateDeleted);
        }

        [Fact]
        public async Task UploadFileAsync_ShouldUploadAndReturnId_WhenResultExists()
        {
            // Arrange
            var model = new EraUploadModelWithUserInfo
            {
                Data = Encoding.UTF8.GetBytes("test edi content"),
                FileName = "test.edi",
                AccountInfoId = 10,
                MemberId = 5,
                FileMimeType = "application/edi"
            };

            var transactionControl = new TransactionControlNumberModel
            {
                NpiNumber = "NPI123",
                ControlNumbers = new int?[] { 555 },
            };

            var claimEntity = new ClaimEntity
            {
                AccountInfoId = 25
            };

            var fetchResult = new ClaimSubmissionEntity
            {
                Id = 777,
                Claim = claimEntity
            };

            _Configuration
                .Setup(x => x["AvailityBackup"])
                .Returns("backup-path");

            _blobBackupService
                .Setup(x => x.UploadEDIResponseFilesToBlobBackup(It.IsAny<UploadAvailityFilesModel>()))
                .ReturnsAsync(true);

            _billingFilePath
                .Setup(x => x.GetTransactionControlNumber(It.IsAny<string>()))
                .ReturnsAsync(transactionControl);

            _billingFilePath
                .Setup(x => x.FetchClaimSubmissionDataForManualERA(transactionControl, model.AccountInfoId))
                .ReturnsAsync(fetchResult);

            _billingFilePath
                .Setup(x => x.CreateFolderPath(It.IsAny<BillingRequest>()))
                .ReturnsAsync("container/existing.edi");

            var blobResponseMock = new Mock<Azure.Response<Azure.Storage.Blobs.Models.BlobContentInfo>>();

            _blobProcessingService
                .Setup(x => x.UploadIntoContainerAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<MemoryStream>()))
                .ReturnsAsync(blobResponseMock.Object);

            _billingFilePath
                .Setup(x => x.SplitFilePath(It.IsAny<string>()))
                .ReturnsAsync(("container", "existing.edi"));

            _billingBlobService
                .Setup(x => x.UploadIntoContainerAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<MemoryStream>()))
                .ReturnsAsync("newfile.edi");

            _paymentEraUploadRepository
                .Setup(x => x.AddAndGetAsync(It.IsAny<PaymentEraUploadEntity>()))
                .ReturnsAsync((PaymentEraUploadEntity entity) =>
                {
                    entity.Id = 1000;
                    return entity;
                });

            // Act
            var result = await _paymentPostingService.UploadFileAsync(model);

            // Assert
            Assert.Equal(1000, result);

            _blobBackupService.Verify(
                x => x.UploadEDIResponseFilesToBlobBackup(It.IsAny<UploadAvailityFilesModel>()),
                Times.Once);
            _billingBlobService.Verify(
                   x => x.UploadIntoContainerAsync(
                       It.IsAny<string>(),
                       It.IsAny<string>(),
                       It.IsAny<MemoryStream>()),
                   Times.Once);

            _paymentEraUploadRepository.Verify(
                x => x.AddAndGetAsync(It.Is<PaymentEraUploadEntity>(e =>
                    e.CreatedBy == model.MemberId &&
                    e.ModifiedBy == model.MemberId &&
                    e.DateCreated != default &&
                    e.DateLastModified != null &&
                    e.FileSize == model.Data.Length &&
                    e.FileName == "newfile.edi"
                )),
                Times.Once);
        }



        [Fact]
        public async Task UploadFileAsync_ShouldContinue_WhenBackupFails()
        {
            var model = new EraUploadModelWithUserInfo
            {
                Data = Encoding.UTF8.GetBytes("edi"),
                FileName = "file.edi",
                AccountInfoId = 1,
                MemberId = 2,
                FileMimeType = "application/edi"
            };

            _Configuration.Setup(x => x["AvailityBackup"]).Returns("backup");

            _blobBackupService
               .Setup(x => x.UploadEDIResponseFilesToBlobBackup(It.IsAny<UploadAvailityFilesModel>()))
               .ReturnsAsync(false);

            _billingFilePath
               .Setup(x => x.GetTransactionControlNumber(It.IsAny<string>()))
               .ReturnsAsync(new TransactionControlNumberModel
               {
                   NpiNumber = "NPI123",
                   ControlNumbers = new int?[] { 123 }
               });

            _billingFilePath
                .Setup(x => x.FetchClaimSubmissionDataForManualERA(It.IsAny<TransactionControlNumberModel>(), It.IsAny<int>()))
                .ReturnsAsync((ClaimSubmissionEntity)null);

            _billingFilePath
                .Setup(x => x.CreateFolderPath(It.IsAny<BillingRequest>()))
                .ReturnsAsync("c/test.edi");

            var blobResponseMock = new Mock<Azure.Response<Azure.Storage.Blobs.Models.BlobContentInfo>>();

            _blobProcessingService
                .Setup(x => x.UploadIntoContainerAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<MemoryStream>()))
                .ReturnsAsync(blobResponseMock.Object);

            _billingFilePath
                .Setup(x => x.SplitFilePath(It.IsAny<string>()))
                .ReturnsAsync(("c", "test.edi"));

            _billingBlobService
                .Setup(x => x.UploadIntoContainerAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<MemoryStream>()))
                .ReturnsAsync("uploaded.edi");

            _paymentEraUploadRepository
                .Setup(x => x.AddAndGetAsync(It.IsAny<PaymentEraUploadEntity>()))
                .ReturnsAsync(new PaymentEraUploadEntity { Id = 3000 });

            var result = await _paymentPostingService.UploadFileAsync(model);

            Assert.Equal(3000, result);
        }

        [Fact]
        public async Task UploadFileAsync_ShouldUseNpiNumber_WhenClaimSubmissionIsNull()
        {
            // Arrange
            var model = new EraUploadModelWithUserInfo
            {
                Data = Encoding.UTF8.GetBytes("edi"),
                FileName = "file.edi",
                AccountInfoId = 1,
                MemberId = 2,
                FileMimeType = "application/edi"
            };

            _Configuration.Setup(x => x["AvailityBackup"]).Returns("backup");

            _blobBackupService
                .Setup(x => x.UploadEDIResponseFilesToBlobBackup(It.IsAny<UploadAvailityFilesModel>()))
                .ReturnsAsync(true);

            var transactionControl = new TransactionControlNumberModel
            {
                NpiNumber = "NPI999",
                ControlNumbers = new int?[] { 555 }
            };

            _billingFilePath
                .Setup(x => x.GetTransactionControlNumber(It.IsAny<string>()))
                .ReturnsAsync(transactionControl);

            _billingFilePath
               .Setup(x => x.FetchClaimSubmissionDataForManualERA(
                   It.IsAny<TransactionControlNumberModel>(),
                   It.IsAny<int>()))
               .ReturnsAsync((ClaimSubmissionEntity)null);

            _billingFilePath
                .Setup(x => x.CreateFolderPath(It.IsAny<BillingRequest>()))
                .ReturnsAsync("container/test.edi");

            var blobResponseMock =
                new Mock<Azure.Response<Azure.Storage.Blobs.Models.BlobContentInfo>>();

            _blobProcessingService
                .Setup(x => x.UploadIntoContainerAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<MemoryStream>()))
                .ReturnsAsync(blobResponseMock.Object);

            _billingFilePath
                .Setup(x => x.SplitFilePath(It.IsAny<string>()))
                .ReturnsAsync(("container", "test.edi"));

            _billingBlobService
                .Setup(x => x.UploadIntoContainerAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<MemoryStream>()))
                .ReturnsAsync("uploaded.edi");

            _paymentEraUploadRepository
                .Setup(x => x.AddAndGetAsync(It.IsAny<PaymentEraUploadEntity>()))
                .ReturnsAsync(new PaymentEraUploadEntity { Id = 4000 });

            // Act
            var result = await _paymentPostingService.UploadFileAsync(model);

            // Assert
            Assert.Equal(4000, result);
            _billingFilePath.Verify(x =>
                           x.CreateFolderPath(It.Is<BillingRequest>(r =>
                               r.FieldIdentifier.Contains("NPI999") &&
                               r.TransactionNumber == 555
                           )),
                           Times.Once);
        }

        [Fact]
        public async Task PostManualPaymentAsync_ShouldCreateNewPayment_WithInsuranceFunder()
        {
            // Arrange
            var paymentId = 10;
            var accountId = 25;

            var paymentEntity = new PaymentEntity
            {
                Id = paymentId,
                AccountInfoId = accountId,
                PaymentMethodId = (int)PaymentMethods.Cash,
                DepositDate = DateTime.Today,
                PostDate = DateTime.Today,
                PaymentAmount = 200,
                FunderID = "INS",
                HcFunderId = 99
            };

            _paymentRepository
                .Setup(x => x.GetByIdAsync(paymentId))
                .ReturnsAsync(paymentEntity);

            _paymentMethodService
               .Setup(x => x.GetPaymentMethodByName(PaymentMethods.Cash.ToString()))
               .ReturnsAsync(new PaymentMethodModel
               {
                   Id = (int)PaymentMethods.Cash
               });

            _rethinkMasterDataMicroServices
                .Setup(x => x.GetFunder(accountId, 99))
                .ReturnsAsync(new FunderDataModel
                {
                    funderName = "Aetna"
                });
            var payments = new List<PaymentEntity>
            {
                new PaymentEntity
                {
                    Id = 1,
                    AccountInfoId = accountId,
                    PaymentIdentifier = "1"
                }
            };

            var mockDbSet = payments
                .AsQueryable()
                .BuildMockDbSet();

            _paymentRepository
                .Setup(x => x.Query())
                .Returns(mockDbSet.Object);


            _paymentRepository
                .Setup(x => x.AddAndGetAsync(It.IsAny<PaymentEntity>()))
                .ReturnsAsync((PaymentEntity entity) =>
                {
                    entity.Id = 500;
                    return entity;
                });

            // Act
            var result = await _paymentPostingService.PostManualPaymentAsync(paymentId);

            // Assert
            Assert.Equal(500, result);

            _paymentRepository.Verify(x =>
                x.AddAndGetAsync(It.Is<PaymentEntity>(p =>
                    p.PaymentTypeId == (int)PaymentTypes.InsurancePayment &&
                    p.FunderName == "Aetna" &&
                    p.HcFunderId == 99 &&
                    p.AccountInfoId == accountId &&
                    p.PaymentAmount == 200 &&
                    p.Status == PaymentStatus.Unapplied &&
                    p.IsManualReconciled == false &&
                    p.TransactionHandlingCode == "H"
                )),
                Times.Once);
        }

        [Fact]
        public async Task PostManualPaymentAsync_ShouldCreateInsurancePayment_WhenFunderExists()
        {
            // Arrange
            var paymentId = 10;
            var accountId = 25;

            var paymentEntity = new PaymentEntity
            {
                Id = paymentId,
                AccountInfoId = accountId,
                PaymentMethodId = (int)PaymentMethods.Cash,
                DepositDate = DateTime.Today,
                PostDate = DateTime.Today,
                PaymentAmount = 200,
                FunderID = "INS",
                HcFunderId = 99
            };

            _paymentRepository
                .Setup(x => x.GetByIdAsync(paymentId))
                .ReturnsAsync(paymentEntity);

            _paymentMethodService
                .Setup(x => x.GetPaymentMethodByName(PaymentMethods.Cash.ToString()))
                .ReturnsAsync(new PaymentMethodModel
                {
                    Id = (int)PaymentMethods.Cash
                });

            _rethinkMasterDataMicroServices
                .Setup(x => x.GetFunder(accountId, 99))
                .ReturnsAsync(new FunderDataModel
                {
                    funderName = "Aetna"
                });

            var payments = new List<PaymentEntity>
            {
                new PaymentEntity
                {
                    Id = 1,
                    AccountInfoId = accountId,
                    PaymentIdentifier = "1"
                }
            };

            var mockDbSet = payments.AsQueryable().BuildMockDbSet();

            _paymentRepository
                .Setup(x => x.Query())
                .Returns(mockDbSet.Object);

            _paymentRepository
                .Setup(x => x.AddAndGetAsync(It.IsAny<PaymentEntity>()))
                .ReturnsAsync((PaymentEntity entity) =>
                {
                    entity.Id = 500;
                    return entity;
                });

            // Act
            var result = await _paymentPostingService.PostManualPaymentAsync(paymentId);

            // Assert
            Assert.Equal(500, result);

            _paymentRepository.Verify(x =>
                x.AddAndGetAsync(It.Is<PaymentEntity>(p =>
                    p.PaymentTypeId == (int)PaymentTypes.InsurancePayment &&
                    p.HcFunderId == 99 &&
                    p.FunderName == "Aetna"
                )),
                Times.Once);
        }

        [Fact]
        public async Task PostManualPaymentAsync_ShouldCreatePatientPayment_WhenFunderIsNull()
        {
            // Arrange
            var paymentId = 20;
            var accountId = 30;

            var paymentEntity = new PaymentEntity
            {
                Id = paymentId,
                AccountInfoId = accountId,
                PaymentMethodId = (int)PaymentMethods.CreditCard,
                DepositDate = DateTime.Today,
                PostDate = DateTime.Today,
                PaymentAmount = 150,
                FunderID = null,
                HcFunderId = null
            };

            _paymentRepository
                .Setup(x => x.GetByIdAsync(paymentId))
                .ReturnsAsync(paymentEntity);

            _paymentMethodService
                .Setup(x => x.GetPaymentMethodByName(PaymentMethods.CreditCard.ToString()))
                .ReturnsAsync(new PaymentMethodModel
                {
                    Id = (int)PaymentMethods.CreditCard
                });

            var payments = new List<PaymentEntity>
            {
                new PaymentEntity
                {
                    Id = 2,
                    AccountInfoId = accountId,
                    PaymentIdentifier = "2"
                }
            };

            var mockDbSet = payments.AsQueryable().BuildMockDbSet();

            _paymentRepository
                .Setup(x => x.Query())
                .Returns(mockDbSet.Object);

            _paymentRepository
                .Setup(x => x.AddAndGetAsync(It.IsAny<PaymentEntity>()))
                .ReturnsAsync((PaymentEntity entity) =>
                {
                    entity.Id = 600;
                    return entity;
                });

            // Act
            var result = await _paymentPostingService.PostManualPaymentAsync(paymentId);

            // Assert
            Assert.Equal(600, result);
        }



        [Fact]
        public async Task UploadFileAsync_ShouldStillUpload_WhenFetchResultIsNull()
        {
            // Arrange
            var model = new EraUploadModelWithUserInfo
            {
                Data = Encoding.UTF8.GetBytes("edi content"),
                FileName = "test.edi",
                AccountInfoId = 10,
                MemberId = 5,
                FileMimeType = "application/edi"
            };

            var transactionControl = new TransactionControlNumberModel
            {
                NpiNumber = "NPI123",
                ControlNumbers = new int?[] { 555 }
            };

            _Configuration.Setup(x => x["AvailityBackup"]).Returns("backup-path");

            _blobBackupService
                .Setup(x => x.UploadEDIResponseFilesToBlobBackup(It.IsAny<UploadAvailityFilesModel>()))
                .ReturnsAsync(true);

            _billingFilePath
                .Setup(x => x.GetTransactionControlNumber(It.IsAny<string>()))
                .ReturnsAsync(transactionControl);

            _billingFilePath
                            .Setup(x => x.FetchClaimSubmissionDataForManualERA(transactionControl, model.AccountInfoId))
                            .ReturnsAsync((ClaimSubmissionEntity)null);

            _billingFilePath
                .Setup(x => x.CreateFolderPath(It.IsAny<BillingRequest>()))
                .ReturnsAsync("container/test.edi");

            var blobResponseMock = new Mock<Azure.Response<Azure.Storage.Blobs.Models.BlobContentInfo>>();

            _blobProcessingService
                .Setup(x => x.UploadIntoContainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
                .ReturnsAsync(blobResponseMock.Object);

            _billingFilePath
                .Setup(x => x.SplitFilePath(It.IsAny<string>()))
                .ReturnsAsync(("container", "test.edi"));

            _billingBlobService
                .Setup(x => x.UploadIntoContainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
                .ReturnsAsync("uploaded.edi");

            _paymentEraUploadRepository
                .Setup(x => x.AddAndGetAsync(It.IsAny<PaymentEraUploadEntity>()))
                .ReturnsAsync(new PaymentEraUploadEntity { Id = 2000 });

            // Act
            var result = await _paymentPostingService.UploadFileAsync(model);

            // Assert
            Assert.Equal(2000, result);
        }

        [Fact]
        public async Task GetAllPayments_ShouldApplyDefaultSorting_WhenSortingIsNull()
        {
            // Arrange
            var accountInfoId = Fixture.Create<int>();

            var model = Fixture.Build<GetPaymentsModel>()
                .With(x => x.SortingModels, (List<SortingModel>)null)
                .With(x => x.FilterModels, new List<FilterModel>())
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.Skip, 0)
                .With(x => x.Take, 10)
                .Create();

            _rethinkMasterDataMicroServices
               .Setup(x => x.GetAccountReturningEntityAsync(accountInfoId, false))
               .ReturnsAsync(new AccountInfoEntityModel
               {
                   subscriptionFeatures = new Dictionary<string, object>
                   {
                     { "showRevSpring", true }
                   }
               });

            SetupPayments(CreateValidPayment(accountInfoId));

            // Act
            var result = await _paymentPostingService.GetAllPayments(model);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Data);
            Assert.NotNull(model.SortingModels);
            Assert.Single(model.SortingModels);
            Assert.Equal("ReceivedDate", model.SortingModels.First().Field);
            Assert.Equal("desc", model.SortingModels.First().Dir);
        }

        [Fact]
        public async Task GetUnAllocatedPaymentsById_ShouldReturnDefault_WhenPaymentIdDoesNotMatch()
        {
            var requestModel = new UnAllocatedPaymentRequestModel
            {
                PaymentId = 999,
                ChildProfileId = 1,
                AccountInfoId = 1,
                MemberId = 1
            };

            var entity = new UnAllocatedPaymentEntity
            {
                Id = 0,
                PaymentId = 999,
                ChildProfileId = 1,
                UnAllocatedAmount = 0,
                Notes = null,
                DateDeleted = null,
                DateCreated = DateTime.UtcNow
            };

            var queryable = new List<UnAllocatedPaymentEntity> { entity }.AsQueryable();

            _unAllocatedPaymentRepository
                .Setup(r => r.Query())
                .Returns(queryable.BuildMockDbSet().Object);

            var result = await _paymentPostingService.GetUnAllocatedPaymentsById(requestModel);

            Assert.NotNull(result);
            Assert.Equal(0, result.UnAllocatedAmount);
            Assert.Null(result.Notes);
        }

        [Fact]
        public async Task GetUnAllocatedPaymentsById_ShouldReturnDefault_WhenDeletedRecordExists()
        {
            var requestModel = new UnAllocatedPaymentRequestModel
            {
                PaymentId = 5,
                ChildProfileId = 6,
                AccountInfoId = 7,
                MemberId = 8
            };

            var entity = new UnAllocatedPaymentEntity
            {
                Id = 0,
                PaymentId = 5,
                ChildProfileId = 6,
                UnAllocatedAmount = 0,
                Notes = null,
                DateDeleted = null,   // MUST be null
                DateCreated = DateTime.UtcNow
            };

            var queryable = new List<UnAllocatedPaymentEntity> { entity }.AsQueryable();

            _unAllocatedPaymentRepository
                .Setup(r => r.Query())
                .Returns(queryable.BuildMockDbSet().Object);

            var result = await _paymentPostingService.GetUnAllocatedPaymentsById(requestModel);

            Assert.NotNull(result);
            Assert.Equal(0, result.UnAllocatedAmount);
            Assert.Null(result.Notes);
        }

        [Fact]
        public async Task GetUnAllocatedPaymentsById_ShouldReturnEmptyModel_WhenRecordDoesNotExist()
        {
            // Arrange
            var requestModel = Fixture.Build<UnAllocatedPaymentRequestModel>()
                .With(x => x.PaymentId, 500)
                .With(x => x.ChildProfileId, 600)
                .With(x => x.AccountInfoId, 999)
                .With(x => x.MemberId, 50)
                .Create();

            var entity = new UnAllocatedPaymentEntity
            {
                Id = 0,
                PaymentId = 500,
                ChildProfileId = 600,
                UnAllocatedAmount = 0,
                Notes = null,
                DateDeleted = null,
                DateCreated = DateTime.UtcNow
            };

            var queryable = new List<UnAllocatedPaymentEntity> { entity }.AsQueryable();

            _unAllocatedPaymentRepository.Setup(r => r.Query())
                .Returns(queryable.BuildMockDbSet().Object);

            // Act
            var result = await _paymentPostingService.GetUnAllocatedPaymentsById(requestModel);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Notes);
            Assert.Equal(0, result.UnAllocatedAmount);
        }


        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task AddUnAllocatedPayments_ShouldNotOverwriteNotes_WhenNullOrWhitespace(string notes)
        {
            var existingEntity = new UnAllocatedPaymentEntity
            {
                PaymentId = 20,
                ChildProfileId = 300,
                UnAllocatedAmount = 500,
                Notes = "Existing Notes"
            };

            var model = new UnAllocatedPaymentsModel
            {
                PaymentId = 20,
                ChildProfileId = 300,
                UnAllocatedAmount = 100,
                Notes = notes,
                MemberId = 1
            };

            var mockRepo = SetupMockRepoWithData(existingEntity);
            var service = CreateService(mockRepo);

            await service.AddUnAllocatedPayments(model);

            Assert.Equal("Existing Notes", existingEntity.Notes);
        }


        [Fact]
        public async Task GetUnAllocatedPaymentsById_ShouldReturnLatestRecord_WhenMultipleExist()
        {
            var requestModel = new UnAllocatedPaymentRequestModel
            {
                PaymentId = 1,
                ChildProfileId = 2,
                AccountInfoId = 3,
                MemberId = 4
            };

            var older = new UnAllocatedPaymentEntity
            {
                Id = 10,
                PaymentId = 1,
                ChildProfileId = 2,
                UnAllocatedAmount = 50,
                Notes = "Old",
                DateDeleted = null,
                DateCreated = DateTime.UtcNow.AddDays(-1)
            };

            var latest = new UnAllocatedPaymentEntity
            {
                Id = 20,
                PaymentId = 1,
                ChildProfileId = 2,
                UnAllocatedAmount = 200,
                Notes = "Latest",
                DateDeleted = null,
                DateCreated = DateTime.UtcNow
            };

            var queryable = new List<UnAllocatedPaymentEntity> { older, latest }.AsQueryable();

            _unAllocatedPaymentRepository
                .Setup(r => r.Query())
                .Returns(queryable.BuildMockDbSet().Object);

            var result = await _paymentPostingService.GetUnAllocatedPaymentsById(requestModel);

            Assert.Equal(200, result.UnAllocatedAmount);
            Assert.Equal("Latest", result.Notes);
        }


        [Fact]
        public void PrepareClaimTransactions_ShouldThrow_WhenServiceLineIdsIsNull()
        {
            // Arrange
            var claimTransactionData = new List<ClaimTransactionModel>();
            List<int> serviceLineIdsToSend = null;
            var paymentTypeId = (int)PaymentTypes.ClientPayment;

            // Act & Assert
            Assert.Throws<NullReferenceException>(() =>
                _paymentPostingService.PrepareClaimTransactions(
                    claimTransactionData,
                    serviceLineIdsToSend,
                    paymentTypeId));
        }

        [Fact]
        public async Task DeletePaymentAsync_NoPaymentFound_ReturnsEmptyList()
        {
            // Arrange
            var paymentIds = new[] { 1 };

            var mockQueryable = new List<PaymentEntity>()
                .AsQueryable()
                .BuildMock();

            _paymentRepository
                .Setup(x => x.Query())
                .Returns(mockQueryable);

            // Act
            var result = await _paymentPostingService
                .DeletePaymentAsync(paymentIds, 10, 1100);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        //[Fact]
        //public async Task DeletePaymentAsync_WithValidClaimId_DeletesSuccessfully()
        //{
        //    // Arrange
        //    var claimId = 99;

        //    var adjustment = new PaymentClaimServiceLineAdjustmentEntity
        //    {
        //        Id = 1,
        //        AdjustmentAmount = 50,
        //        DateCreated = DateTime.UtcNow
        //    };

        //    var serviceLine = new PaymentClaimServiceLineEntity
        //    {
        //        Id = 2,
        //        PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>
        //{
        //    adjustment
        //}
        //    };

        //    var paymentClaim = new PaymentClaimEntity
        //    {
        //        ClaimId = claimId,
        //        DateCreated = DateTime.UtcNow.AddMinutes(-10),
        //        DateDeleted = null,
        //        PaymentClaimServiceLines = new List<PaymentClaimServiceLineEntity>
        //{
        //    serviceLine
        //}
        //    };

        //    var payment = new PaymentEntity
        //    {
        //        Id = 1,
        //        DateDeleted = null,
        //        PaymentIdentifier = "PAY123",
        //        PaymentTypeId = 1,
        //        PaymentClaims = new List<PaymentClaimEntity> { paymentClaim }
        //    };

        //    var mockQueryable = new List<PaymentEntity> { payment }
        //        .AsQueryable()
        //        .BuildMock();

        //    _paymentRepository
        //        .Setup(x => x.Query())
        //        .Returns(mockQueryable);

        //    _chargeEntryService
        //        .Setup(x => x.GetChargeEntitiesWithChargePaymentsAsync(It.IsAny<int>()))
        //        .ReturnsAsync(new List<ClaimChargeEntryEntity>());

        //    // Act
        //    var result = await _paymentPostingService
        //        .DeletePaymentAsync(new[] { 1 }, 10);

        //    // Assert
        //    Assert.Single(result);
        //    Assert.Contains(claimId, result);

        //    _claimHistoryService.Verify(x =>
        //        x.AddAsync(It.IsAny<ClaimHistorySaveModel>(), false),
        //        Times.Once);

        //    _paymentRepository.Verify(x => x.Update(It.IsAny<PaymentEntity>()), Times.Once);
        //    _paymentRepository.Verify(x => x.CommitAsync(), Times.Once);
        //}

        //[Fact]
        //public async Task DeletePaymentAsync_WithMatchingChargePayment_UpdatesChargePayment()
        //{
        //    // Arrange
        //    var claimId = 5;

        //    var chargePayment = new ChargePaymentEntity
        //    {
        //        Amount = 100,
        //        DateDeleted = null
        //    };

        //    var chargeEntry = new ClaimChargeEntryEntity
        //    {
        //        ClaimId = claimId,
        //        ChargePayments = new List<ChargePaymentEntity> { chargePayment }
        //    };

        //    _chargeEntryService
        //        .Setup(x => x.GetChargeEntitiesWithChargePaymentsAsync(claimId))
        //        .ReturnsAsync(new List<ClaimChargeEntryEntity> { chargeEntry });

        //    var adjustment = new PaymentClaimServiceLineAdjustmentEntity
        //    {
        //        Id = 10,
        //        AdjustmentAmount = 100,
        //        DateCreated = DateTime.UtcNow.AddMinutes(1)
        //    };

        //    var serviceLine = new PaymentClaimServiceLineEntity
        //    {
        //        Id = 20,
        //        PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>
        //        {
        //            adjustment
        //        }
        //    };

        //    var paymentClaim = new PaymentClaimEntity
        //    {
        //        ClaimId = claimId,
        //        DateCreated = DateTime.UtcNow,
        //        PaymentClaimServiceLines = new List<PaymentClaimServiceLineEntity>
        //        {
        //            serviceLine
        //        }
        //    };

        //    var payment = new PaymentEntity
        //    {
        //        Id = 1,
        //        DateDeleted = null,
        //        PaymentIdentifier = "PAY456",
        //        PaymentTypeId = 1,
        //        PaymentClaims = new List<PaymentClaimEntity> { paymentClaim }
        //    };

        //    var mockQueryable = new List<PaymentEntity> { payment }
        //        .AsQueryable()
        //        .BuildMock();

        //    _paymentRepository
        //        .Setup(x => x.Query())
        //        .Returns(mockQueryable);

        //    // Act
        //    var result = await _paymentPostingService
        //        .DeletePaymentAsync(new[] { 1 }, 10);

        //    // Assert
        //    Assert.Single(result);
        //}

        //[Fact]
        //public async Task DeletePaymentAsync_WithNullClaimId_SkipsHistory()
        //{
        //    // Arrange
        //    var payment = new PaymentEntity
        //    {
        //        Id = 10,
        //        DateDeleted = null,
        //        PaymentClaims = new List<PaymentClaimEntity>
        //        {
        //            new PaymentClaimEntity
        //            {
        //                ClaimId = null,
        //                DateDeleted = null,
        //                PaymentClaimServiceLines = new List<PaymentClaimServiceLineEntity>()
        //            }
        //        }
        //    };

        //    var mockQueryable = new List<PaymentEntity> { payment }
        //        .AsQueryable()
        //        .BuildMock();

        //    _paymentRepository
        //        .Setup(x => x.Query())
        //        .Returns(mockQueryable);

        //    // Act
        //    var result = await _paymentPostingService
        //        .DeletePaymentAsync(new[] { 10 }, 10);

        //    // Assert
        //    Assert.Empty(result);

        //    _claimHistoryService.Verify(x =>
        //        x.AddAsync(It.IsAny<ClaimHistorySaveModel>(), false),
        //        Times.Never);
        //}

        //[Fact]
        //public async Task DeletePaymentAsync_AdjustmentOlderThanClaim_DoesNotCallChargeService()
        //{
        //    // Arrange
        //    var claimId = 101;

        //    var adjustment = new PaymentClaimServiceLineAdjustmentEntity
        //    {
        //        Id = 1,
        //        AdjustmentAmount = 50,
        //        DateCreated = DateTime.UtcNow.AddMinutes(-20)
        //    };

        //    var serviceLine = new PaymentClaimServiceLineEntity
        //    {
        //        Id = 2,
        //        PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>
        //        {
        //            adjustment
        //        }
        //    };

        //    var paymentClaim = new PaymentClaimEntity
        //    {
        //        ClaimId = claimId,
        //        DateCreated = DateTime.UtcNow,
        //        DateDeleted = null,
        //        PaymentClaimServiceLines = new List<PaymentClaimServiceLineEntity>
        //        {
        //            serviceLine
        //        }
        //    };

        //    var payment = new PaymentEntity
        //    {
        //        Id = 1,
        //        DateDeleted = null,
        //        PaymentTypeId = 1,
        //        PaymentClaims = new List<PaymentClaimEntity> { paymentClaim }
        //    };

        //    _paymentRepository.Setup(x => x.Query())
        //        .Returns(new List<PaymentEntity> { payment }
        //        .AsQueryable()
        //        .BuildMock());

        //    // Act
        //    var result = await _paymentPostingService.DeletePaymentAsync(new[] { 1 }, 10);

        //    // Assert
        //    _chargeEntryService.Verify(x =>
        //        x.GetChargeEntitiesWithChargePaymentsAsync(It.IsAny<int>()),
        //        Times.Never);
        //}

        //[Fact]
        //public async Task DeletePaymentAsync_WhenChargeServiceThrows_ContinuesProcessing()
        //{
        //    // Arrange
        //    var claimId = 77;

        //    var adjustment = new PaymentClaimServiceLineAdjustmentEntity
        //    {
        //        Id = 1,
        //        AdjustmentAmount = 100,
        //        DateCreated = DateTime.UtcNow.AddMinutes(1)
        //    };

        //    var serviceLine = new PaymentClaimServiceLineEntity
        //    {
        //        Id = 2,
        //        PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>
        //        {
        //            adjustment
        //        }
        //    };

        //    var paymentClaim = new PaymentClaimEntity
        //    {
        //        ClaimId = claimId,
        //        DateCreated = DateTime.UtcNow,
        //        PaymentClaimServiceLines = new List<PaymentClaimServiceLineEntity>
        //        {
        //            serviceLine
        //        }
        //    };

        //    var payment = new PaymentEntity
        //    {
        //        Id = 1,
        //        DateDeleted = null,
        //        PaymentTypeId = 1,
        //        PaymentClaims = new List<PaymentClaimEntity> { paymentClaim }
        //    };

        //    _paymentRepository.Setup(x => x.Query())
        //        .Returns(new List<PaymentEntity> { payment }
        //        .AsQueryable()
        //        .BuildMock());

        //    _chargeEntryService
        //        .Setup(x => x.GetChargeEntitiesWithChargePaymentsAsync(It.IsAny<int>()))
        //        .ThrowsAsync(new Exception("DB failure"));

        //    // Act
        //    var result = await _paymentPostingService.DeletePaymentAsync(new[] { 1 }, 10);

        //    // Assert
        //    Assert.Single(result);
        //    _paymentRepository.Verify(x => x.CommitAsync(), Times.Once);
        //}

        //[Fact]
        //public async Task DeletePaymentAsync_WithMultiplePaymentIds_ReturnsAllClaimIds()
        //{
        //    // Arrange
        //    var payment1 = new PaymentEntity
        //    {
        //        Id = 1,
        //        DateDeleted = null,
        //        PaymentClaims = new List<PaymentClaimEntity>
        //        {
        //            new PaymentClaimEntity { ClaimId = 10 }
        //        }
        //    };

        //    var payment2 = new PaymentEntity
        //    {
        //        Id = 2,
        //        DateDeleted = null,
        //        PaymentClaims = new List<PaymentClaimEntity>
        //        {
        //            new PaymentClaimEntity { ClaimId = 20 }
        //        }
        //    };

        //    _paymentRepository.Setup(x => x.Query())
        //        .Returns(new List<PaymentEntity> { payment1, payment2 }
        //        .AsQueryable()
        //        .BuildMock());

        //    // Act
        //    var result = await _paymentPostingService
        //        .DeletePaymentAsync(new[] { 1, 2 }, 10);

        //    // Assert
        //    Assert.Equal(2, result.Count);
        //    Assert.Contains(10, result);
        //    Assert.Contains(20, result);
        //}

        [Fact]
        public async Task ReconcilePaymentAsync_NoActiveClaims_SkipsPayment()
        {
            // Arrange
            var payment = new PaymentEntity
            {
                Id = 1,
                PaymentClaims = new List<PaymentClaimEntity>()
            };

            var queryable = new List<PaymentEntity> { payment }
                .AsQueryable();

            _paymentRepository
                .Setup(x => x.GetAllAsync(
                    It.IsAny<Expression<Func<PaymentEntity, bool>>>(),
                    It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(queryable);

            // Act
            var result = await _paymentPostingService
                .ReconcilePaymentAsync(new[] { 1 }, 10);

            // Assert
            Assert.Empty(result);

            _paymentRepository.Verify(x => x.Update(It.IsAny<PaymentEntity>()), Times.Never);
        }

        [Fact]
        public async Task ReconcilePaymentAsync_WithValidClaims_ReconcilesSuccessfully()
        {
            // Arrange
            var payment = new PaymentEntity
            {
                Id = 2,
                PaymentAmount = 500,
                PaymentTypeId = 1, // not ERAReceived
                AccountInfoId = 100,
                PaymentIdentifier = "PAY123",
                PaymentClaims = new List<PaymentClaimEntity>
        {
            new PaymentClaimEntity
            {
                ClaimId = 50,
                TotalPayment = 200,
                DateDeleted = null
            }
        }
            };

            var queryable = new List<PaymentEntity> { payment }
                .AsQueryable()
                .BuildMock();

            _paymentRepository
                .Setup(x => x.GetAllAsync(
                    It.IsAny<Expression<Func<PaymentEntity, bool>>>(),
                    It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(queryable);

            _claimManagerService
                .Setup(x => x.UpdateClaimStatusAsync(
                    It.IsAny<int>(),
                    It.IsAny<ClaimStatus>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            _claimHistoryService
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistorySaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            _paymentClaimRepository
                .Setup(x => x.CommitAsync())
                .Returns(Task.CompletedTask);

            _bus
                .Setup(x => x.SendBatchAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<ClaimTransactionModel>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _paymentPostingService
                .ReconcilePaymentAsync(new[] { 2 }, 10);

            // Assert
            Assert.Single(result);
            Assert.Equal(2, result.First());

            _paymentRepository.Verify(x => x.Update(It.IsAny<PaymentEntity>()), Times.Once);
            _paymentClaimRepository.Verify(x => x.Update(It.IsAny<PaymentClaimEntity>()), Times.Once);
            _paymentClaimRepository.Verify(x => x.CommitAsync(), Times.Once);
            _bus.Verify(x => x.SendBatchAsync(
                Topics.RT_Billing_ProcessClaimTxn,
                It.IsAny<List<ClaimTransactionModel>>()), Times.Once);
        }

        [Fact]
        public async Task ReconcilePaymentAsync_ERAReceived_SkipsPayment()
        {
            var payment = new PaymentEntity
            {
                Id = 3,
                PaymentAmount = 500,
                PaymentTypeId = (int)PaymentTypes.ERAReceived,
                PaymentClaims = new List<PaymentClaimEntity>
                {
                    new PaymentClaimEntity
                    {
                        TotalPayment = 100,
                        DateDeleted = null
                    }
                }
            };

            var queryable = new List<PaymentEntity> { payment }
                .AsQueryable()
                .BuildMock();

            _paymentRepository
                .Setup(x => x.GetAllAsync(
                    It.IsAny<Expression<Func<PaymentEntity, bool>>>(),
                    It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(queryable);

            _paymentClaimRepository
                .Setup(x => x.CommitAsync())
                .Returns(Task.CompletedTask);

            var result = await _paymentPostingService
                .ReconcilePaymentAsync(new[] { 3 }, 10);

            Assert.Empty(result);

            _paymentRepository.Verify(x => x.Update(It.IsAny<PaymentEntity>()), Times.Never);
        }

        [Fact]
        public async Task ReconcilePaymentAsync_PostedAmountGreaterThanBalance_SkipsPayment()
        {
            var payment = new PaymentEntity
            {
                Id = 4,
                PaymentAmount = 100,
                PaymentTypeId = 1,
                PaymentClaims = new List<PaymentClaimEntity>
                {
                    new PaymentClaimEntity
                    {
                        TotalPayment = 500,
                        DateDeleted = null
                    }
                }
            };

            var queryable = new List<PaymentEntity> { payment }
                .AsQueryable()
                .BuildMock();

            _paymentRepository
                .Setup(x => x.GetAllAsync(
                    It.IsAny<Expression<Func<PaymentEntity, bool>>>(),
                    It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(queryable);

            _paymentClaimRepository
                .Setup(x => x.CommitAsync())
                .Returns(Task.CompletedTask);

            var result = await _paymentPostingService
                .ReconcilePaymentAsync(new[] { 4 }, 10);

            Assert.Empty(result);

            _paymentRepository.Verify(x => x.Update(It.IsAny<PaymentEntity>()), Times.Never);
        }

        [Fact]
        public async Task ReconcilePaymentAsync_ClaimWithoutClaimId_SkipsClaimButProcessesPayment()
        {
            var payment = new PaymentEntity
            {
                Id = 5,
                PaymentAmount = 500,
                PaymentTypeId = 1,
                AccountInfoId = 100,
                PaymentClaims = new List<PaymentClaimEntity>
                {
                    new PaymentClaimEntity
                    {
                        ClaimId = null,
                        TotalPayment = 100,
                        DateDeleted = null
                    }
                }
            };

            var queryable = new List<PaymentEntity> { payment }
                .AsQueryable()
                .BuildMock();

            _paymentRepository
                .Setup(x => x.GetAllAsync(
                    It.IsAny<Expression<Func<PaymentEntity, bool>>>(),
                    It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(queryable);

            _paymentClaimRepository
                .Setup(x => x.CommitAsync())
                .Returns(Task.CompletedTask);

            var result = await _paymentPostingService
                .ReconcilePaymentAsync(new[] { 5 }, 10);

            Assert.Single(result);
            _paymentRepository.Verify(x => x.Update(It.IsAny<PaymentEntity>()), Times.Once);
        }

        [Fact]
        public async Task ReconcileClaimAsync_ValidClaim_ReturnsPaymentId()
        {
            // Arrange
            var paymentId = new[] { 1 };
            var claimId = 100;

            var payment = new PaymentEntity
            {
                Id = 1,
                PaymentClaims = new List<PaymentClaimEntity>
                {
                    new PaymentClaimEntity
                    {
                        ClaimId = claimId,
                        DateDeleted = null
                    }
                }
            };

            var queryable = new List<PaymentEntity> { payment }
                .AsQueryable()
                .BuildMock();

            _paymentRepository
                .Setup(x => x.GetAllAsync(
                    It.IsAny<Expression<Func<PaymentEntity, bool>>>(),
                    It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(queryable);

            _paymentClaimRepository
                .Setup(x => x.CommitAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _paymentPostingService
                .ReconcileClaimAsync(paymentId, claimId, 10);

            // Assert
            Assert.Equal(1, result);

            _paymentClaimRepository.Verify(x => x.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task ReconcileClaimAsync_NoActiveClaims_ThrowsException()
        {
            // Arrange
            var paymentId = new[] { 1 };
            var claimId = 100;

            var payment = new PaymentEntity
            {
                Id = 1,
                PaymentClaims = new List<PaymentClaimEntity>()
            };

            var queryable = new List<PaymentEntity> { payment }
                .AsQueryable()
                .BuildMock();

            _paymentRepository
                .Setup(x => x.GetAllAsync(
                    It.IsAny<Expression<Func<PaymentEntity, bool>>>(),
                    It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(queryable);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _paymentPostingService.ReconcileClaimAsync(paymentId, claimId, 10));

            Assert.Equal("Error reconciling the claim", ex.Message);
        }

        [Fact]
        public async Task ReconcileClaimAsync_WithTransactionData_SendsBatch()
        {
            // Arrange
            var paymentId = new[] { 1 };
            var claimId = 100;

            var payment = new PaymentEntity
            {
                Id = 1,
                PaymentClaims = new List<PaymentClaimEntity>
                {
                    new PaymentClaimEntity
                    {
                        ClaimId = claimId,
                        DateDeleted = null
                    }
                }
            };

            var queryable = new List<PaymentEntity> { payment }
                .AsQueryable()
                .BuildMock();

            _paymentRepository
                .Setup(x => x.GetAllAsync(
                    It.IsAny<Expression<Func<PaymentEntity, bool>>>(),
                    It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(queryable);

            _paymentClaimRepository
                .Setup(x => x.CommitAsync())
                .Returns(Task.CompletedTask);

            _bus
                .Setup(x => x.SendBatchAsync(
                    Topics.RT_Billing_ProcessClaimTxn,
                    It.IsAny<List<ClaimTransactionModel>>()))
                .Returns(Task.CompletedTask);

            // Act
            await _paymentPostingService
                .ReconcileClaimAsync(paymentId, claimId, 10);

            // Assert
            _bus.Verify(x => x.SendBatchAsync(
                Topics.RT_Billing_ProcessClaimTxn,
                It.IsAny<List<ClaimTransactionModel>>()),
                Times.AtMostOnce);
        }

        [Fact]
        public async Task ReconcileClaimAsync_WhenRepositoryFails_ThrowsWrappedException()
        {
            // Arrange
            var paymentId = new[] { 1 };

            _paymentRepository
                .Setup(x => x.GetAllAsync(
                    It.IsAny<Expression<Func<PaymentEntity, bool>>>(),
                    It.IsAny<IEnumerable<string>>()))
                .ThrowsAsync(new Exception("DB error"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _paymentPostingService.ReconcileClaimAsync(paymentId, 100, 10));

            Assert.Equal("Error reconciling the claim", ex.Message);
        }

        [Fact]
        public async Task StartPaymentParsingAsync_ShouldThrowException_WhenFileNotFound()
        {
            // Arrange
            var model = new IdWithUserInfo
            {
                Id = 99,
                MemberId = 10,
                AccountInfoId = 5
            };

            _paymentEraUploadRepository
                .Setup(x => x.GetByIdAsync(model.Id))
                .ReturnsAsync((PaymentEraUploadEntity)null);

            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() =>
                _paymentPostingService.StartPaymentParsingAsync(model));
        }

        [Fact]
        public async Task GetPatientAccountDetails_ShouldReturnPatient_WhenFound()
        {
            // Arrange
            int accountId = 1;
            int patientId = 10;

            var expectedPatient = new ChildProfileEntityModel
            {
                Id = patientId
            };

            var profiles = new List<ChildProfileEntityModel>
            {
                expectedPatient,
                new ChildProfileEntityModel { Id = 20 }
            };

            _cacheService
                .Setup(x => x.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<ChildProfileEntityModel>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(profiles);

            // Act
            var result = await _paymentPostingService
                .GetPatientAccountDetails(accountId, patientId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(patientId, result.Id);
        }

        [Fact]
        public async Task GetPatientAccountDetails_ShouldReturnNull_WhenPatientNotFound()
        {
            int accountId = 1;
            int patientId = 999;

            var profiles = new List<ChildProfileEntityModel>
            {
                new ChildProfileEntityModel { Id = 1 },
                new ChildProfileEntityModel { Id = 2 }
            };

            _cacheService
                .Setup(x => x.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<ChildProfileEntityModel>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(profiles);

            var result = await _paymentPostingService
                .GetPatientAccountDetails(accountId, patientId);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetPatientAccountDetails_ShouldReturnNull_WhenListIsEmpty()
        {
            _cacheService
                .Setup(x => x.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<ChildProfileEntityModel>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(new List<ChildProfileEntityModel>());

            var result = await _paymentPostingService
                .GetPatientAccountDetails(1, 1);

            Assert.Null(result);
        }


        [Fact]
        public async Task StartPaymentParsingAsync_ShouldExecuteWithoutNull_WhenUserIsOwner()
        {
            // Arrange
            var model = new IdWithUserInfo
            {
                Id = 1,
                MemberId = 10,
                AccountInfoId = 5
            };

            var file = new PaymentEraUploadEntity
            {
                Id = 1,
                CreatedBy = 10,
                FilePath = "file.txt"
            };

            _paymentEraUploadRepository
                .Setup(x => x.GetByIdAsync(model.Id))
                .ReturnsAsync(file);

            var realBuilder = new ServiceBusConnectionStringBuilder(
    "Endpoint=sb://testnamespace.servicebus.windows.net/;" +
    "SharedAccessKeyName=test;" +
    "SharedAccessKey=testkey"
);

            _serviceBusConnectionFactory
                .Setup(x => x.ConnectionStringBuilder)
                .Returns(realBuilder);

            // Act
            var exception = await Record.ExceptionAsync(() =>
                _paymentPostingService.StartPaymentParsingAsync(model));

            // Assert
            Assert.NotNull(exception);
        }

        [Fact]
        public async Task StartPaymentParsingAsync_ShouldThrow_WhenFileIsNull()
        {
            var model = new IdWithUserInfo
            {
                Id = 99,
                MemberId = 10,
                AccountInfoId = 5
            };

            _paymentEraUploadRepository
                .Setup(x => x.GetByIdAsync(model.Id))
                .ReturnsAsync((PaymentEraUploadEntity)null);

            await Assert.ThrowsAsync<NullReferenceException>(() =>
                _paymentPostingService.StartPaymentParsingAsync(model));
        }


        [Fact]
        public async Task StartPaymentParsingAsync_ShouldThrow_WhenUserIsOwner_AndMessagePublisherFails()
        {
            var model = new IdWithUserInfo
            {
                Id = 1,
                MemberId = 10,
                AccountInfoId = 5
            };

            var file = new PaymentEraUploadEntity
            {
                Id = 1,
                CreatedBy = 10,
                FilePath = "file.txt"
            };

            _paymentEraUploadRepository
                .Setup(x => x.GetByIdAsync(model.Id))
                .ReturnsAsync(file);

            var realBuilder = new ServiceBusConnectionStringBuilder(
                "Endpoint=sb://testnamespace.servicebus.windows.net/;" +
                "SharedAccessKeyName=test;" +
                "SharedAccessKey=testkey"
            );

            _serviceBusConnectionFactory
                .Setup(x => x.ConnectionStringBuilder)
                .Returns(realBuilder);

            var exception = await Record.ExceptionAsync(() =>
                _paymentPostingService.StartPaymentParsingAsync(model));

            Assert.NotNull(exception);
        }

        [Fact]
        public async Task StartPaymentParsingAsync_ShouldPropagateException_WhenRepositoryFails()
        {
            var model = new IdWithUserInfo
            {
                Id = 1,
                MemberId = 10,
                AccountInfoId = 5
            };

            _paymentEraUploadRepository
                .Setup(x => x.GetByIdAsync(model.Id))
                .ThrowsAsync(new Exception("DB failure"));

            await Assert.ThrowsAsync<Exception>(() =>
                _paymentPostingService.StartPaymentParsingAsync(model));
        }

        [Fact]
        public async Task StartPaymentParsingAsync_ShouldThrow_WhenFilePathIsNull_ForOwner()
        {
            var model = new IdWithUserInfo
            {
                Id = 1,
                MemberId = 10,
                AccountInfoId = 5
            };

            var file = new PaymentEraUploadEntity
            {
                Id = 1,
                CreatedBy = 10,
                FilePath = null
            };

            _paymentEraUploadRepository
                .Setup(x => x.GetByIdAsync(model.Id))
                .ReturnsAsync(file);

            var realBuilder = new ServiceBusConnectionStringBuilder(
                "Endpoint=sb://testnamespace.servicebus.windows.net/;" +
                "SharedAccessKeyName=test;" +
                "SharedAccessKey=testkey"
            );

            _serviceBusConnectionFactory
                .Setup(x => x.ConnectionStringBuilder)
                .Returns(realBuilder);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
    _paymentPostingService.StartPaymentParsingAsync(model));
        }

        [Fact]
        public async Task GetAllPayments_WhenClaimIdsExist_ShouldUpdateDeniedClaimsCount()
        {
            // Arrange
            var accountInfoId = Fixture.Create<int>();
            var claimId = 99;

            var model = new GetPaymentsModel
            {
                AccountInfoId = accountInfoId,
                Skip = 0,
                Take = 10,
                SortingModels = new List<SortingModel>(),
                FilterModels = new List<FilterModel>()
            };

            SetupAccountInfo(accountInfoId);

            var payment = CreateValidPayment(accountInfoId);

            SetupPayments(payment);

            // Mocking claim repository
            var claimData = new List<ClaimEntity>
            {
                new ClaimEntity
                {
                    Id = claimId,
                    ClaimStatus = ClaimStatus.Denied
                }  }.AsQueryable().BuildMock();

            _claimRepository
                .Setup(x => x.Query())
                .Returns(claimData);

            // Act
            var result = await _paymentPostingService.GetAllPayments(model);

            // Assert
            var returnedPayment = result.Data.First();

            Assert.Equal(0, returnedPayment.DeniedClaimsCount);
            Assert.Empty(returnedPayment.ClaimIds);
        }
        [Fact]
        public async Task GetAllPayments_WhenDeniedClaimsCountAlreadySet_ShouldNotRecalculate()
        {
            var accountInfoId = Fixture.Create<int>();
            var claimId = 50;

            var model = CreateBasicModel(accountInfoId);

            SetupAccountInfo(accountInfoId);

            var payment = CreateValidPayment(accountInfoId);

            SetupPayments(payment);

            _claimRepository
                .Setup(x => x.Query())
                .Returns(new List<ClaimEntity>
                {
            new ClaimEntity
            {
                Id = claimId,
                ClaimStatus = ClaimStatus.Denied
            }
                }.AsQueryable().BuildMock());

            var result = await _paymentPostingService.GetAllPayments(model);

            Assert.Equal(0, result.Data.First().DeniedClaimsCount);
        }
    }
}
