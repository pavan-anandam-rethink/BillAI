using BillingService.Domain.DataObjects.Billing;
using BillingService.Domain.Interfaces;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Interfaces.Files;
using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models.PatientInvoice;
using BillingService.Domain.Models.PaymentClaims;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Domain.Services.Payment;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Entities.Billing.PatientInvoice;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.ClientMicroServicesModels;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.PaymentPosting
{
    public class PaymentClaimServiceTests
    {
        private readonly Mock<IRepository<BillingDbContext, PaymentEntity>> _mockPaymentRepository;
        private readonly Mock<IRepository<BillingDbContext, PaymentClaimEntity>> _mockPaymentClaimRepository;
        private readonly Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>> _mockPaymentClaimServiceLineRepository;
        private readonly Mock<IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity>> _mockPaymentClaimServiceLineAdjustmentRepository;
        private readonly Mock<IRepository<BillingDbContext, PaymentAdjustmentReasonEntity>> _mockReasonCodesRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>> _mockClaimChargeEntryRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity>> _mockClaimChargeEntryWriteOffRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimEntity>> _mockClaimEntityRepository;
        private readonly Mock<IRepository<BillingDbContext, CarcCodeEntity>> _mockCarcCodeEntityRepository;
        private readonly Mock<IRepository<BillingDbContext, PatientInvoiceEntity>> _mockPatientInvoiceRepository;
        private readonly Mock<IRepository<BillingDbContext, PatientInvoiceDetailsEntity>> _mockPatientInvoiceDetailsRepository;
        private readonly Mock<IPaymentPostingService> _mockPaymentPostingService;
        private readonly Mock<IProviderBillingCodeService> _mockProviderBillingCodeService;
        private readonly Mock<IClaimHistoryService> _mockClaimHistoryService;
        private readonly Mock<IChargeEntryService> _mockChargeEntryService;
        private readonly Mock<IRazorViewService> _mockRazorViewService;
        private readonly Mock<IClaimManagerService> _mockClaimManagerService;
        private readonly Mock<IRethinkMasterDataMicroServices> _mockRethinkMasterDataMicroServices;
        private readonly Mock<IFileManagerService> _mockFileManagerService;
        private readonly Mock<IPdfService> _mockPdfService;
        private readonly Mock<IMessageBus> _mockMessageBus;
        private readonly Mock<IRepository<BillingDbContext, UnAllocatedPaymentEntity>> _mockUnAllocatedPaymentRepository;
        private readonly Mock<IRepository<BillingDbContext, PaymentClaimServiceLineErrorEntity>> _mockPaymentClaimServiceLineErrorEntity;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<ILogger<PaymentClaimService>> _mockLogger;

        private readonly PaymentClaimService _service;

        public PaymentClaimServiceTests()
        {
            _mockPaymentRepository = new Mock<IRepository<BillingDbContext, PaymentEntity>>();
            _mockPaymentClaimRepository = new Mock<IRepository<BillingDbContext, PaymentClaimEntity>>();
            _mockPaymentClaimServiceLineRepository = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>>();
            _mockPaymentClaimServiceLineAdjustmentRepository = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity>>();
            _mockReasonCodesRepository = new Mock<IRepository<BillingDbContext, PaymentAdjustmentReasonEntity>>();
            _mockClaimChargeEntryRepository = new Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>>();
            _mockClaimChargeEntryWriteOffRepository = new Mock<IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity>>();
            _mockClaimEntityRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            _mockCarcCodeEntityRepository = new Mock<IRepository<BillingDbContext, CarcCodeEntity>>();
            _mockPatientInvoiceRepository = new Mock<IRepository<BillingDbContext, PatientInvoiceEntity>>();
            _mockPatientInvoiceDetailsRepository = new Mock<IRepository<BillingDbContext, PatientInvoiceDetailsEntity>>();
            _mockPaymentPostingService = new Mock<IPaymentPostingService>();
            _mockProviderBillingCodeService = new Mock<IProviderBillingCodeService>();
            _mockClaimHistoryService = new Mock<IClaimHistoryService>();
            _mockChargeEntryService = new Mock<IChargeEntryService>();
            _mockRazorViewService = new Mock<IRazorViewService>();
            _mockClaimManagerService = new Mock<IClaimManagerService>();
            _mockRethinkMasterDataMicroServices = new Mock<IRethinkMasterDataMicroServices>();
            _mockFileManagerService = new Mock<IFileManagerService>();
            _mockPdfService = new Mock<IPdfService>();
            _mockMessageBus = new Mock<IMessageBus>();
            _mockUnAllocatedPaymentRepository = new Mock<IRepository<BillingDbContext, UnAllocatedPaymentEntity>>();
            _mockPaymentClaimServiceLineErrorEntity = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineErrorEntity>>();
            _mockCacheService = new Mock<ICacheService>();
            _mockLogger = new Mock<ILogger<PaymentClaimService>>();

            _service = new PaymentClaimService(
                _mockPaymentRepository.Object,
                _mockPaymentClaimRepository.Object,
                _mockPaymentClaimServiceLineRepository.Object,
                _mockPaymentClaimServiceLineAdjustmentRepository.Object,
                _mockPaymentPostingService.Object,
                _mockReasonCodesRepository.Object,
                _mockClaimChargeEntryWriteOffRepository.Object,
                _mockPatientInvoiceRepository.Object,
                _mockProviderBillingCodeService.Object,
                _mockClaimHistoryService.Object,
                _mockChargeEntryService.Object,
                _mockRazorViewService.Object,
                _mockClaimManagerService.Object,
                _mockClaimChargeEntryRepository.Object,
                _mockRethinkMasterDataMicroServices.Object,
                _mockClaimEntityRepository.Object,
                _mockFileManagerService.Object,
                _mockPdfService.Object,
                _mockMessageBus.Object,
                _mockCarcCodeEntityRepository.Object,
                _mockUnAllocatedPaymentRepository.Object,
                _mockPaymentClaimServiceLineErrorEntity.Object,
                _mockCacheService.Object,
                _mockPatientInvoiceDetailsRepository.Object,
                _mockLogger.Object
            );
        }

        #region CreatePaymentClaimsAsync Tests

        [Fact]
        public async Task CreatePaymentClaimsAsync_WithValidData_ReturnsSuccessResponse()
        {
            // Arrange
            var model = new CreatePatientClaimsModel
            {
                PaymentId = 1,
                AccountInfoId = 100,
                PatientIds = new[] { 1, 2 },
                UnAllocatedAmount = new[] { 100m, 200m },
                Notes = new[] { "Note1", "Note2" },
                MemberId = 5
            };

            var payment = new PaymentEntity { Id = 1 };
            var paymentMock = new[] { payment }.AsQueryable().BuildMock();
            _mockPaymentRepository.Setup(x => x.Query()).Returns(paymentMock);

            var clientDetails = new List<RethinkGuarantorDetails.ClientModel>
            {
                new RethinkGuarantorDetails.ClientModel { Id = 1, UserId = 10 }
            };
            _mockRethinkMasterDataMicroServices.Setup(x => x.GetClientDetailsGuarantor(It.IsAny<int>()))
                .ReturnsAsync(clientDetails);

            var patientDetails = new List<PaymentClaimEntity>();
            var patientDetailsMock = patientDetails.AsQueryable().BuildMock();
            _mockPaymentClaimRepository.Setup(x => x.Query()).Returns(patientDetailsMock);

            var invoiceQuery = new List<PatientInvoiceEntity>().AsQueryable().BuildMock();
            _mockPatientInvoiceRepository.Setup(x => x.Query()).Returns(invoiceQuery);

            var childProfile = new ClientUserModel
            {
                id = 1,
                name = new ClientUserName { firstName = "John", middleName = "M", lastName = "Doe" }
            };
            _mockRethinkMasterDataMicroServices.Setup(x => x.GetChildProfile(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(childProfile);

            _mockChargeEntryService.Setup(x => x.GetIdsAllOpenedPatientClaimAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<ClaimChargeItem>());

            // Act
            var result = await _service.CreatePaymentClaimsAsync(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            _mockPaymentPostingService.Verify(x => x.AddUnAllocatedPayments(It.IsAny<UnAllocatedPaymentsModel>()), Times.Exactly(2));
        }

        [Fact]
        public async Task CreatePaymentClaimsAsync_WithNoClaims_StillAddsUnallocatedPayments()
        {
            // Arrange
            var model = new CreatePatientClaimsModel
            {
                PaymentId = 1,
                AccountInfoId = 100,
                PatientIds = new[] { 1 },
                UnAllocatedAmount = new[] { 100m },
                MemberId = 5
            };

            var payment = new PaymentEntity { Id = 1 };
            var paymentMock = new[] { payment }.AsQueryable().BuildMock();
            _mockPaymentRepository.Setup(x => x.Query()).Returns(paymentMock);

            var clientDetails = new List<RethinkGuarantorDetails.ClientModel> { new RethinkGuarantorDetails.ClientModel { Id = 1 } };
            _mockRethinkMasterDataMicroServices.Setup(x => x.GetClientDetailsGuarantor(It.IsAny<int>()))
                .ReturnsAsync(clientDetails);

            var patientDetails = new List<PaymentClaimEntity>();
            var patientDetailsMock = patientDetails.AsQueryable().BuildMock();
            _mockPaymentClaimRepository.Setup(x => x.Query()).Returns(patientDetailsMock);

            var invoiceQuery = new List<PatientInvoiceEntity>().AsQueryable().BuildMock();
            _mockPatientInvoiceRepository.Setup(x => x.Query()).Returns(invoiceQuery);

            var childProfile = new ClientUserModel { id = 1, name = new ClientUserName { firstName = "John", lastName = "Doe" } };
            _mockRethinkMasterDataMicroServices.Setup(x => x.GetChildProfile(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(childProfile);

            // Act
            var result = await _service.CreatePaymentClaimsAsync(model);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.True(result[0].isAttached);
            _mockPaymentPostingService.Verify(x => x.AddUnAllocatedPayments(It.IsAny<UnAllocatedPaymentsModel>()), Times.Once);
        }

        #endregion

        #region CreateClaimsToEraAsync Tests

        [Fact]
        public async Task CreateClaimsToEraAsync_WithNullPayment_ReturnsZero()
        {
            // Arrange
            var model = new CreateEraClaimsModel { PaymentId = 1, ClaimsIds = new[] { 1 }, AccountInfoId = 100, MemberId = 5 };
            var emptyPaymentMock = new List<PaymentEntity>().AsQueryable().BuildMock();
            _mockPaymentRepository.Setup(x => x.Query()).Returns(emptyPaymentMock);

            // Act
            var result = await _service.CreateClaimsToEraAsync(model);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task CreateClaimsToEraAsync_WithExistingClaim_SkipsClaim()
        {
            // Arrange
            var model = new CreateEraClaimsModel
            {
                PaymentId = 1,
                ClaimsIds = new[] { 1 },
                AccountInfoId = 100,
                MemberId = 5
            };

            var payment = new PaymentEntity { Id = 1 };
            var paymentMock = new[] { payment }.AsQueryable().BuildMock();
            _mockPaymentRepository.Setup(x => x.Query()).Returns(paymentMock);

            var claims = new List<ClaimChargeItem>
            {
                new ClaimChargeItem { ClaimId = 1, PatientId = 1, ChargeEntries = new List<ManualPaymentChargeEntryItem>() }
            };
            _mockChargeEntryService.Setup(x => x.GetAllClaimsByIdAsync(It.IsAny<PaymentEntity>(), It.IsAny<int[]>()))
                .ReturnsAsync(claims);

            var existingPaymentClaim = new PaymentClaimEntity { ClaimId = 1 };
            var queryable = new[] { existingPaymentClaim }.AsQueryable().BuildMock();
            _mockPaymentClaimRepository.Setup(x => x.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<PaymentClaimEntity, bool>>>(),
                It.IsAny<IEnumerable<string>>()
            ))
            .ReturnsAsync(queryable);

            // Act
            var result = await _service.CreateClaimsToEraAsync(model);

            // Assert
            Assert.Equal(0, result);
        }

        #endregion

        #region GetPatientPaymentLinkedServiceLinesAsyncNew Tests

        [Fact]
        public async Task GetPatientPaymentLinkedServiceLinesAsyncNew_ReturnsLinkedLines()
        {
            // Arrange
            var model = new GetPatientPaymentServiceLinesModel
            {
                PatientId = 1,
                PaymentId = 1,
                ShowPaid = false,
                IsLinked = true,
                SortingModels = new List<SortingModel>(),
                FilterModels = new List<FilterModel>()
            };

            var payment = new PaymentEntity { Id = 1, PaymentTypeId = (int)PaymentTypes.ClientPayment, DepositDate = DateTime.Now };
            _mockPaymentRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(payment);

            var allCharges = new List<PaymentGroupedModel>
            {
                new PaymentGroupedModel
                {
                    PatientId = 1,
                    PaymentId = 1,
                    ChargeId = 100,
                    ServiceLineId = 1,
                    PaymentTypeId = (int)PaymentTypes.ClientPayment,
                    PaidAmount = 50,
                    ChargeAmount = 100,
                    DateDeleted = null
                }
            };
            _mockCacheService.Setup(x => x.GetOrSetCacheAsync(
              It.Is<string>(s => s.StartsWith("AllCharges")),
              It.IsAny<Func<Task<List<PaymentGroupedModel>>>>(),
              It.IsAny<TimeSpan>()))
              .ReturnsAsync(allCharges);

            var groupedDict = new Dictionary<int, PatientPaymentClaimFullModel>
            {
                { 100, new PatientPaymentClaimFullModel { ChargeId = 100, Adjustment = 10, PatientResponsibility = 20 } }
            };

            _mockCacheService.Setup(x => x.GetOrSetCacheAsync(
               It.Is<string>(s => s.StartsWith("Grouped")),
               It.IsAny<Func<Task<Dictionary<int, PatientPaymentClaimFullModel>>>>(),
               It.IsAny<TimeSpan>()))
               .ReturnsAsync(groupedDict);

            var modifiers = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity { Id = 100, Modifier1 = "25", Modifier2 = "59" }
            };
            var modifiersMock = modifiers.AsQueryable().BuildMock();
            _mockClaimChargeEntryRepository.Setup(x => x.Query()).Returns(modifiersMock);

            var writeOffs = new List<ClaimChargeEntryWriteOffEntity>();
            var writeOffsMock = writeOffs.AsQueryable().BuildMock();
            _mockClaimChargeEntryWriteOffRepository.Setup(x => x.Query()).Returns(writeOffsMock);

            // Act
            var result = await _service.GetPatientPaymentLinkedServiceLinesAsyncNew(model, false);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        #endregion

        #region GetChargeInfoByIds Tests

        [Fact]
        public async Task GetChargeInfoByIds_ReturnsChargeData()
        {
            // Arrange
            var chargeIds = new List<int> { 1, 2 };

            var serviceLines = new List<PaymentClaimServiceLineEntity>
            {
                new PaymentClaimServiceLineEntity
                {
                    Id = 1,
                    ClaimChargeEntryId = 1,
                    DateDeleted = null,
                    PaymentClaim = new PaymentClaimEntity
                    {
                        PaymentId = 1,
                        ChildProfileId = 100,
                        DateDeleted = null,
                        Payment = new PaymentEntity { PaymentTypeId = (int)PaymentTypes.InsurancePayment },
                        Claim = new ClaimEntity { DateDeleted = null }
                    },
                    PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
                    PaymentClaimServiceLineErrors = new List<PaymentClaimServiceLineErrorEntity>()
                }
            };

            var serviceLinesMock = serviceLines.AsQueryable().BuildMock();
            _mockPaymentClaimServiceLineRepository.Setup(x => x.Query()).Returns(serviceLinesMock);

            // Act
            var result = await _service.GetChargeInfoByIds(chargeIds);

            // Assert
            Assert.NotNull(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.AtLeastOnce);
        }

        #endregion

        #region GetAllPaymentChargeIds Tests

        [Fact]
        public async Task GetAllPaymentChargeIds_WithFilters_ReturnsFilteredCharges()
        {
            // Arrange
            var model = new CreateInvoiceFilters
            {
                AccountInfoId = 100,
                Filters = new CreateInvoice
                {
                    ClientIds = "1,2",
                    DateOfServiceFrom = DateTime.Now.AddDays(-30),
                    DateOfServiceTo = DateTime.Now
                }
            };

            var serviceLines = new List<PaymentClaimServiceLineEntity>
            {
                new PaymentClaimServiceLineEntity
                {
                    ClaimChargeEntryId = 1,
                    DateOfService = DateTime.Now.AddDays(-15),
                    DateDeleted = null,
                    PaymentClaim = new PaymentClaimEntity
                    {
                        ChildProfileId = 1,
                        DateDeleted = null,
                        Claim = new ClaimEntity { AccountInfoId = 100 }
                    }
                }
            };

            var serviceLinesMock = serviceLines.AsQueryable().BuildMock();
            _mockPaymentClaimServiceLineRepository.Setup(x => x.Query()).Returns(serviceLinesMock);

            // Act
            var result = await _service.GetAllPaymentChargeIds(model);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        #endregion

        #region Cache Invalidation Tests

        [Fact]
        public async Task InvalidatePaymentCacheAsync_RemovesCacheKeys()
        {
            // Arrange
            var paymentId = 1;
            var patientId = 100;

            // Act
            await _service.InvalidatePaymentCacheAsync(paymentId, patientId);

            // Assert
            _mockCacheService.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.AtLeast(3));
        }

        #endregion

        #region GetPatientPaymentUnlinkedServiceLinesAsyncNew Tests

        [Fact]
        public async Task GetPatientPaymentUnlinkedServiceLinesAsyncNew_WithUnlinkedCharges_ReturnsUnlinkedLines()
        {
            // Arrange
            var model = new GetPatientPaymentServiceLinesModel
            {
                PatientId = 1,
                PaymentId = 1,
                ShowPaid = false,
                SortingModels = new List<SortingModel>(),
                FilterModels = new List<FilterModel>()
            };

            var payment = new PaymentEntity
            {
                Id = 1,
                PaymentTypeId = (int)PaymentTypes.ClientPayment,
                DepositDate = DateTime.Now
            };
            _mockPaymentRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(payment);

            // Setup unlinked charges (PaidAmount = 0 means unlinked)
            var allCharges = new List<PaymentGroupedModel>
            {
                new PaymentGroupedModel
                {
                    PatientId = 1,
                    PaymentId = 1,
                    ChargeId = 100,
                    ServiceLineId = 1,
                    PaymentTypeId = (int)PaymentTypes.ClientPayment,
                    PaidAmount = 0, // Unlinked
                    ChargeAmount = 100,
                    DateOfService = DateTime.Now.AddDays(-5),
                    DateDeleted = null,
                    ServiceCode = "99213",
                    ProcedureModifier1 = "25",
                    ProcedureModifier2 = "GT",
                    PatientName = "John Doe"
                },
                new PaymentGroupedModel
                {
                    PatientId = 1,
                    PaymentId = 1,
                    ChargeId = 101,
                    ServiceLineId = 2,
                    PaymentTypeId = (int)PaymentTypes.ClientPayment,
                    PaidAmount = 0, // Unlinked
                    ChargeAmount = 150,
                    DateOfService = DateTime.Now.AddDays(-3),
                    DateDeleted = null,
                    ServiceCode = "99214",
                    ProcedureModifier1 = "59",
                    PatientName = "John Doe"
                }
            };

            _mockCacheService.Setup(x => x.GetOrSetCacheAsync(
                It.Is<string>(s => s.StartsWith("AllCharges")),
                It.IsAny<Func<Task<List<PaymentGroupedModel>>>>(),
                It.IsAny<TimeSpan>()))
                .ReturnsAsync(allCharges);

            var groupedDict = new Dictionary<int, PatientPaymentClaimFullModel>
            {
                { 100, new PatientPaymentClaimFullModel
                    {
                        ChargeId = 100,
                        Adjustment = 10,
                        PatientResponsibility = 20,
                        InsurancePayment = 50,
                        PatientPayment = 0,
                        PatientResponsibilityBalance = 20
                    }
                },
                { 101, new PatientPaymentClaimFullModel
                    {
                        ChargeId = 101,
                        Adjustment = 15,
                        PatientResponsibility = 30,
                        InsurancePayment = 75,
                        PatientPayment = 0,
                        PatientResponsibilityBalance = 30
                    }
                }
            };

            _mockCacheService.Setup(x => x.GetOrSetCacheAsync(
                It.Is<string>(s => s.StartsWith("Grouped")),
                It.IsAny<Func<Task<Dictionary<int, PatientPaymentClaimFullModel>>>>(),
                It.IsAny<TimeSpan>()))
                .ReturnsAsync(groupedDict);

            var modifiers = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity { Id = 100, Modifier1 = "25", Modifier2 = "GT", Modifier3 = "", Modifier4 = "" },
                new ClaimChargeEntryEntity { Id = 101, Modifier1 = "59", Modifier2 = "", Modifier3 = "", Modifier4 = "" }
            };
            var modifiersMock = modifiers.AsQueryable().BuildMock();
            _mockClaimChargeEntryRepository.Setup(x => x.Query()).Returns(modifiersMock);

            var writeOffs = new List<ClaimChargeEntryWriteOffEntity>
            {
                new ClaimChargeEntryWriteOffEntity { ClaimChargeEntryId = 100, WriteOffAmount = 5, DateDeleted = null },
                new ClaimChargeEntryWriteOffEntity { ClaimChargeEntryId = 101, WriteOffAmount = 10, DateDeleted = null }
            };
            var writeOffsMock = writeOffs.AsQueryable().BuildMock();
            _mockClaimChargeEntryWriteOffRepository.Setup(x => x.Query()).Returns(writeOffsMock);

            // Act
            var result = await _service.GetPatientPaymentUnlinkedServiceLinesAsyncNew(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.False(item.IsLinked));
            Assert.All(result, item => Assert.True(item.PatientResponsibility > 0));
        }

        [Fact]
        public async Task GetPatientPaymentUnlinkedServiceLinesAsyncNew_WithLinkedCharges_FiltersThemOut()
        {
            // Arrange
            var model = new GetPatientPaymentServiceLinesModel
            {
                PatientId = 1,
                PaymentId = 1,
                ShowPaid = false,
                SortingModels = new List<SortingModel>(),
                FilterModels = new List<FilterModel>()
            };

            var payment = new PaymentEntity
            {
                Id = 1,
                PaymentTypeId = (int)PaymentTypes.ClientPayment,
                DepositDate = DateTime.Now
            };
            _mockPaymentRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(payment);

            // All charges are linked (PaidAmount > 0)
            var allCharges = new List<PaymentGroupedModel>
            {
                new PaymentGroupedModel
                {
                    PatientId = 1,
                    PaymentId = 1,
                    ChargeId = 100,
                    ServiceLineId = 1,
                    PaymentTypeId = (int)PaymentTypes.ClientPayment,
                    PaidAmount = 50, // Linked
                    ChargeAmount = 100,
                    DateOfService = DateTime.Now.AddDays(-5),
                    DateDeleted = null,
                    ServiceCode = "99213"
                }
            };

            _mockCacheService.Setup(x => x.GetOrSetCacheAsync(
                It.Is<string>(s => s.StartsWith("AllCharges")),
                It.IsAny<Func<Task<List<PaymentGroupedModel>>>>(),
                It.IsAny<TimeSpan>()))
                .ReturnsAsync(allCharges);

            var groupedDict = new Dictionary<int, PatientPaymentClaimFullModel>();
            _mockCacheService.Setup(x => x.GetOrSetCacheAsync(
                It.Is<string>(s => s.StartsWith("Grouped")),
                It.IsAny<Func<Task<Dictionary<int, PatientPaymentClaimFullModel>>>>(),
                It.IsAny<TimeSpan>()))
                .ReturnsAsync(groupedDict);

            var modifiers = new List<ClaimChargeEntryEntity>();
            var modifiersMock = modifiers.AsQueryable().BuildMock();
            _mockClaimChargeEntryRepository.Setup(x => x.Query()).Returns(modifiersMock);

            var writeOffs = new List<ClaimChargeEntryWriteOffEntity>();
            var writeOffsMock = writeOffs.AsQueryable().BuildMock();
            _mockClaimChargeEntryWriteOffRepository.Setup(x => x.Query()).Returns(writeOffsMock);

            // Act
            var result = await _service.GetPatientPaymentUnlinkedServiceLinesAsyncNew(model);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result); // All charges were linked, so none should be returned
        }

        [Fact]
        public async Task GetPatientPaymentUnlinkedServiceLinesAsyncNew_WithShowPaidTrue_ReturnsOnlyPaidLines()
        {
            // Arrange
            var model = new GetPatientPaymentServiceLinesModel
            {
                PatientId = 1,
                PaymentId = 1,
                ShowPaid = true, // Only show paid lines
                SortingModels = new List<SortingModel>(),
                FilterModels = new List<FilterModel>()
            };

            var payment = new PaymentEntity
            {
                Id = 1,
                PaymentTypeId = (int)PaymentTypes.ClientPayment,
                DepositDate = DateTime.Now
            };
            _mockPaymentRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(payment);

            var allCharges = new List<PaymentGroupedModel>
            {
                new PaymentGroupedModel
                {
                    PatientId = 1,
                    PaymentId = 1,
                    ChargeId = 100,
                    ServiceLineId = 1,
                    PaymentTypeId = (int)PaymentTypes.ClientPayment,
                    PaidAmount = 0, // Unlinked but will have PatientPayment set
                    ChargeAmount = 100,
                    DateOfService = DateTime.Now.AddDays(-5),
                    DateDeleted = null,
                    ServiceCode = "99213",
                    PatientName = "John Doe"
                },
                new PaymentGroupedModel
                {
                    PatientId = 1,
                    PaymentId = 1,
                    ChargeId = 101,
                    ServiceLineId = 2,
                    PaymentTypeId = (int)PaymentTypes.ClientPayment,
                    PaidAmount = 0, // Unlinked with no payment
                    ChargeAmount = 150,
                    DateOfService = DateTime.Now.AddDays(-3),
                    DateDeleted = null,
                    ServiceCode = "99214",
                    PatientName = "John Doe"
                }
            };

            _mockCacheService.Setup(x => x.GetOrSetCacheAsync(
                It.Is<string>(s => s.StartsWith("AllCharges")),
                It.IsAny<Func<Task<List<PaymentGroupedModel>>>>(),
                It.IsAny<TimeSpan>()))
                .ReturnsAsync(allCharges);

            var groupedDict = new Dictionary<int, PatientPaymentClaimFullModel>
            {
                { 100, new PatientPaymentClaimFullModel
                    {
                        ChargeId = 100,
                        Adjustment = 10,
                        PatientResponsibility = 20,
                        InsurancePayment = 50,
                        PatientPayment = 15, // Has payment
                        PatientResponsibilityBalance = 5
                    }
                },
                { 101, new PatientPaymentClaimFullModel
                    {
                        ChargeId = 101,
                        Adjustment = 15,
                        PatientResponsibility = 30,
                        InsurancePayment = 75,
                        PatientPayment = 0, // No payment
                        PatientResponsibilityBalance = 30
                    }
                }
            };

            _mockCacheService.Setup(x => x.GetOrSetCacheAsync(
                It.Is<string>(s => s.StartsWith("Grouped")),
                It.IsAny<Func<Task<Dictionary<int, PatientPaymentClaimFullModel>>>>(),
                It.IsAny<TimeSpan>()))
                .ReturnsAsync(groupedDict);

            var modifiers = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity { Id = 100, Modifier1 = "25", Modifier2 = "", Modifier3 = "", Modifier4 = "" },
                new ClaimChargeEntryEntity { Id = 101, Modifier1 = "59", Modifier2 = "", Modifier3 = "", Modifier4 = "" }
            };
            var modifiersMock = modifiers.AsQueryable().BuildMock();
            _mockClaimChargeEntryRepository.Setup(x => x.Query()).Returns(modifiersMock);

            var writeOffs = new List<ClaimChargeEntryWriteOffEntity>();
            var writeOffsMock = writeOffs.AsQueryable().BuildMock();
            _mockClaimChargeEntryWriteOffRepository.Setup(x => x.Query()).Returns(writeOffsMock);

            // Act
            var result = await _service.GetPatientPaymentUnlinkedServiceLinesAsyncNew(model);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result); // Only the one with PatientPayment > 0 should be returned
            Assert.All(result, item => Assert.True(item.PatientPayment > 0));
        }

        [Fact]
        public async Task GetPatientPaymentUnlinkedServiceLinesAsyncNew_WithNoDefaultSorting_AddsPatientPaymentSort()
        {
            // Arrange
            var model = new GetPatientPaymentServiceLinesModel
            {
                PatientId = 1,
                PaymentId = 1,
                ShowPaid = false,
                SortingModels = new List<SortingModel>(), // Empty
                FilterModels = new List<FilterModel>()
            };

            var payment = new PaymentEntity
            {
                Id = 1,
                PaymentTypeId = (int)PaymentTypes.ClientPayment,
                DepositDate = DateTime.Now
            };
            _mockPaymentRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(payment);

            var allCharges = new List<PaymentGroupedModel>
            {
                new PaymentGroupedModel
                {
                    PatientId = 1,
                    PaymentId = 1,
                    ChargeId = 100,
                    ServiceLineId = 1,
                    PaymentTypeId = (int)PaymentTypes.ClientPayment,
                    PaidAmount = 0,
                    ChargeAmount = 100,
                    DateOfService = DateTime.Now.AddDays(-5),
                    DateDeleted = null,
                    ServiceCode = "99213",
                    PatientName = "John Doe"
                }
            };

            _mockCacheService.Setup(x => x.GetOrSetCacheAsync(
                It.Is<string>(s => s.StartsWith("AllCharges")),
                It.IsAny<Func<Task<List<PaymentGroupedModel>>>>(),
                It.IsAny<TimeSpan>()))
                .ReturnsAsync(allCharges);

            var groupedDict = new Dictionary<int, PatientPaymentClaimFullModel>
            {
                { 100, new PatientPaymentClaimFullModel
                    {
                        ChargeId = 100,
                        PatientResponsibility = 20,
                        PatientPayment = 0
                    }
                }
            };

            _mockCacheService.Setup(x => x.GetOrSetCacheAsync(
                It.Is<string>(s => s.StartsWith("Grouped")),
                It.IsAny<Func<Task<Dictionary<int, PatientPaymentClaimFullModel>>>>(),
                It.IsAny<TimeSpan>()))
                .ReturnsAsync(groupedDict);

            var modifiers = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity { Id = 100, Modifier1 = "", Modifier2 = "", Modifier3 = "", Modifier4 = "" }
            };
            var modifiersMock = modifiers.AsQueryable().BuildMock();
            _mockClaimChargeEntryRepository.Setup(x => x.Query()).Returns(modifiersMock);

            var writeOffs = new List<ClaimChargeEntryWriteOffEntity>();
            var writeOffsMock = writeOffs.AsQueryable().BuildMock();
            _mockClaimChargeEntryWriteOffRepository.Setup(x => x.Query()).Returns(writeOffsMock);

            // Act
            var result = await _service.GetPatientPaymentUnlinkedServiceLinesAsyncNew(model);

            // Assert
            Assert.NotNull(result);
            Assert.Single(model.SortingModels); // Should have added default sorting
            Assert.Equal("patientPayment", model.SortingModels[0].Field);
            Assert.Equal("desc", model.SortingModels[0].Dir);
        }

        [Fact]
        public async Task GetPatientPaymentUnlinkedServiceLinesAsyncNew_WithFutureDateOfService_FiltersOut()
        {
            // Arrange
            var depositDate = DateTime.Now;
            var model = new GetPatientPaymentServiceLinesModel
            {
                PatientId = 1,
                PaymentId = 1,
                ShowPaid = false,
                SortingModels = new List<SortingModel>(),
                FilterModels = new List<FilterModel>()
            };

            var payment = new PaymentEntity
            {
                Id = 1,
                PaymentTypeId = (int)PaymentTypes.ClientPayment,
                DepositDate = depositDate
            };
            _mockPaymentRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(payment);

            var allCharges = new List<PaymentGroupedModel>
            {
                new PaymentGroupedModel
                {
                    PatientId = 1,
                    PaymentId = 1,
                    ChargeId = 100,
                    ServiceLineId = 1,
                    PaymentTypeId = (int)PaymentTypes.ClientPayment,
                    PaidAmount = 0,
                    ChargeAmount = 100,
                    DateOfService = depositDate.AddDays(1), // Future date - should be filtered out
                    DateDeleted = null,
                    ServiceCode = "99213",
                    PatientName = "John Doe"
                },
                new PaymentGroupedModel
                {
                    PatientId = 1,
                    PaymentId = 1,
                    ChargeId = 101,
                    ServiceLineId = 2,
                    PaymentTypeId = (int)PaymentTypes.ClientPayment,
                    PaidAmount = 0,
                    ChargeAmount = 150,
                    DateOfService = depositDate.AddDays(-1), // Past date - should be included
                    DateDeleted = null,
                    ServiceCode = "99214",
                    PatientName = "John Doe"
                }
            };

            _mockCacheService.Setup(x => x.GetOrSetCacheAsync(
                It.Is<string>(s => s.StartsWith("AllCharges")),
                It.IsAny<Func<Task<List<PaymentGroupedModel>>>>(),
                It.IsAny<TimeSpan>()))
                .ReturnsAsync(allCharges);

            var groupedDict = new Dictionary<int, PatientPaymentClaimFullModel>
            {
                { 101, new PatientPaymentClaimFullModel
                    {
                        ChargeId = 101,
                        PatientResponsibility = 20,
                        PatientPayment = 0
                    }
                }
            };

            _mockCacheService.Setup(x => x.GetOrSetCacheAsync(
    It.Is<string>(s => s.StartsWith("Grouped")),
    It.IsAny<Func<Task<Dictionary<int, PatientPaymentClaimFullModel>>>>(),
    It.IsAny<TimeSpan>()))
    .ReturnsAsync(groupedDict);

            var modifiers = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity { Id = 101, Modifier1 = "", Modifier2 = "", Modifier3 = "", Modifier4 = "" }
            };
            var modifiersMock = modifiers.AsQueryable().BuildMock();
            _mockClaimChargeEntryRepository.Setup(x => x.Query()).Returns(modifiersMock);

            var writeOffs = new List<ClaimChargeEntryWriteOffEntity>
            {
                new ClaimChargeEntryWriteOffEntity { ClaimChargeEntryId = 100, WriteOffAmount = 5, DateDeleted = null },
                new ClaimChargeEntryWriteOffEntity { ClaimChargeEntryId = 101, WriteOffAmount = 10, DateDeleted = null }
            };
            var writeOffsMock = writeOffs.AsQueryable().BuildMock();
            _mockClaimChargeEntryWriteOffRepository.Setup(x => x.Query()).Returns(writeOffsMock);

            // Act
            var result = await _service.GetPatientPaymentUnlinkedServiceLinesAsyncNew(model);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result); // Only the past date should be included
            Assert.Equal(101, result[0].ChargeEntryId);
        }

        [Fact]
        public async Task GetPatientPaymentUnlinkedServiceLinesAsyncNew_CalculatesBalanceCorrectly()
        {
            // Arrange
            var model = new GetPatientPaymentServiceLinesModel
            {
                PatientId = 1,
                PaymentId = 1,
                ShowPaid = false,
                SortingModels = new List<SortingModel>(),
                FilterModels = new List<FilterModel>()
            };

            var payment = new PaymentEntity
            {
                Id = 1,
                PaymentTypeId = (int)PaymentTypes.ClientPayment,
                DepositDate = DateTime.Now
            };
            _mockPaymentRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(payment);

            var allCharges = new List<PaymentGroupedModel>
            {
                new PaymentGroupedModel
                {
                    PatientId = 1,
                    PaymentId = 1,
                    ChargeId = 100,
                    ServiceLineId = 1,
                    PaymentTypeId = (int)PaymentTypes.ClientPayment,
                    PaidAmount = 0,
                    ChargeAmount = 200, // Billed amount
                    DateOfService = DateTime.Now.AddDays(-5),
                    DateDeleted = null,
                    ServiceCode = "99213",
                    PatientName = "John Doe"
                }
            };

            _mockCacheService.Setup(x => x.GetOrSetCacheAsync(
                It.Is<string>(s => s.StartsWith("AllCharges")),
                It.IsAny<Func<Task<List<PaymentGroupedModel>>>>(),
                It.IsAny<TimeSpan>()))
                .ReturnsAsync(allCharges);

            // Balance = BilledAmount - InsurancePayment - Adjustment - PatientResponsibility
            // Balance = 200 - 80 - 20 - 50 = 50
            var groupedDict = new Dictionary<int, PatientPaymentClaimFullModel>
            {
                { 100, new PatientPaymentClaimFullModel
                    {
                        ChargeId = 100,
                        Adjustment = 20,
                        PatientResponsibility = 50,
                        InsurancePayment = 80,
                        PatientPayment = 0,
                        PatientResponsibilityBalance = 50
                    }
                }
            };

            _mockCacheService.Setup(x => x.GetOrSetCacheAsync(
                It.Is<string>(s => s.StartsWith("Grouped")),
                It.IsAny<Func<Task<Dictionary<int, PatientPaymentClaimFullModel>>>>(),
                It.IsAny<TimeSpan>()))
                .ReturnsAsync(groupedDict);

            var modifiers = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity { Id = 100, Modifier1 = "", Modifier2 = "", Modifier3 = "", Modifier4 = "" }
            };
            var modifiersMock = modifiers.AsQueryable().BuildMock();
            _mockClaimChargeEntryRepository.Setup(x => x.Query()).Returns(modifiersMock);

            var writeOffs = new List<ClaimChargeEntryWriteOffEntity>();
            var writeOffsMock = writeOffs.AsQueryable().BuildMock();
            _mockClaimChargeEntryWriteOffRepository.Setup(x => x.Query()).Returns(writeOffsMock);

            // Act
            var result = await _service.GetPatientPaymentUnlinkedServiceLinesAsyncNew(model);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            var line = result[0];
            Assert.Equal(200, line.BilledAmount);
            Assert.Equal(80, line.InsurancePayment);
            Assert.Equal(20, line.Adjustment);
            Assert.Equal(50, line.PatientResponsibility);
            Assert.Equal(50, line.Balance); // 200 - 80 - 20 - 50 = 50
        }

        #endregion

        #region GetAllChargesCachedAsync Tests

        [Fact]
        public async Task GetAllChargesCachedAsync_CacheHit_ReturnsDataFromCache()
        {
            // Arrange
            int paymentId = 1;
            int patientId = 100;
            int childProfileId = 50;

            var cachedData = new List<PaymentGroupedModel>
            {
                new PaymentGroupedModel { ServiceLineId = 1, ChargeId = 100, PaymentId = 1 }
            };

            _mockCacheService
                .Setup(x => x.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<PaymentGroupedModel>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(cachedData);

            // Act
            var result = await _service.GetAllChargesCachedAsync(paymentId, patientId, childProfileId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(100, result[0].ChargeId);
            _mockCacheService.Verify(x => x.GetOrSetCacheAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<PaymentGroupedModel>>>>(),
                It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task GetAllChargesCachedAsync_CacheMiss_LoadsDataAndCaches()
        {
            // Arrange
            int paymentId = 1;
            int patientId = 0;
            int childProfileId = 0;

            var paymentClaims = new List<PaymentClaimEntity>
            {
                new PaymentClaimEntity { Id = 1, PaymentId = 1, ClaimId = 10, DateDeleted = null }
            };
            var paymentClaimsMock = paymentClaims.AsQueryable().BuildMock();
            _mockPaymentClaimRepository.Setup(x => x.Query()).Returns(paymentClaimsMock);

            var serviceLines = new List<PaymentClaimServiceLineEntity>
            {
                new PaymentClaimServiceLineEntity
                {
                    Id = 1,
                    PaymentClaimId = 1,
                    ServiceCode = "99213",
                    ClaimChargeEntryId = 100,
                    DateDeleted = null,
                    PaymentClaim = new PaymentClaimEntity
                    {
                        Id = 1,
                        ClaimId = 10,
                        PaymentId = 1,
                        DateDeleted = null,
                        ClientFirstName = "John",
                        ClientLastName = "Doe",
                        Payment = new PaymentEntity { Id = 1, PaymentTypeId = (int)PaymentTypes.ClientPayment },
                        Claim = new ClaimEntity { Id = 10, ChildProfileId = 50, DateDeleted = null }
                    }
                }
            };
            var serviceLinesMock = serviceLines.AsQueryable().BuildMock();
            _mockPaymentClaimServiceLineRepository.Setup(x => x.Query()).Returns(serviceLinesMock);

            var adjustments = new List<PaymentClaimServiceLineAdjustmentEntity>();
            var adjustmentsMock = adjustments.AsQueryable().BuildMock();
            _mockPaymentClaimServiceLineAdjustmentRepository.Setup(x => x.Query()).Returns(adjustmentsMock);

            var errors = new List<PaymentClaimServiceLineErrorEntity>();
            var errorsMock = errors.AsQueryable().BuildMock();
            _mockPaymentClaimServiceLineErrorEntity.Setup(x => x.Query()).Returns(errorsMock);

            List<PaymentGroupedModel> capturedData = null;
            _mockCacheService
                .Setup(x => x.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<PaymentGroupedModel>>>>(),
                    It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<List<PaymentGroupedModel>>>, TimeSpan>(async (key, factory, expiration) =>
                {
                    capturedData = await factory();
                    return capturedData;
                });

            // Act
            var result = await _service.GetAllChargesCachedAsync(paymentId, patientId, childProfileId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(100, result[0].ChargeId);
            _mockCacheService.Verify(x => x.GetOrSetCacheAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<PaymentGroupedModel>>>>(),
                It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task GetAllChargesCachedAsync_WithCustomTimeSpan_UsesProvidedExpiration()
        {
            // Arrange
            int paymentId = 1;
            int patientId = 0;
            int childProfileId = 0;
            var customTimeSpan = TimeSpan.FromMinutes(10);

            var emptyPaymentClaims = new List<PaymentClaimEntity>();
            var paymentClaimsMock = emptyPaymentClaims.AsQueryable().BuildMock();
            _mockPaymentClaimRepository.Setup(x => x.Query()).Returns(paymentClaimsMock);

            var serviceLines = new List<PaymentClaimServiceLineEntity>();
            var serviceLinesMock = serviceLines.AsQueryable().BuildMock();
            _mockPaymentClaimServiceLineRepository.Setup(x => x.Query()).Returns(serviceLinesMock);

            var adjustments = new List<PaymentClaimServiceLineAdjustmentEntity>();
            var adjustmentsMock = adjustments.AsQueryable().BuildMock();
            _mockPaymentClaimServiceLineAdjustmentRepository.Setup(x => x.Query()).Returns(adjustmentsMock);

            var errors = new List<PaymentClaimServiceLineErrorEntity>();
            var errorsMock = errors.AsQueryable().BuildMock();
            _mockPaymentClaimServiceLineErrorEntity.Setup(x => x.Query()).Returns(errorsMock);

            TimeSpan? capturedTimeSpan = null;
            _mockCacheService
                .Setup(x => x.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<PaymentGroupedModel>>>>(),
                    It.IsAny<TimeSpan>()))
                .Callback<string, Func<Task<List<PaymentGroupedModel>>>, TimeSpan>((key, factory, expiration) =>
                {
                    capturedTimeSpan = expiration;
                })
                .ReturnsAsync(new List<PaymentGroupedModel>());

            // Act
            await _service.GetAllChargesCachedAsync(paymentId, patientId, childProfileId, customTimeSpan);

            // Assert
            Assert.NotNull(capturedTimeSpan);
            Assert.Equal(customTimeSpan, capturedTimeSpan.Value);
        }

        #endregion

        #region GetGroupedDictCachedAsync Tests

        [Fact]
        public async Task GetGroupedDictCachedAsync_WithNullPayment_ReturnsEmptyDictionary()
        {
            // Arrange
            PaymentEntity payment = null;
            int patientId = 1;
            int childProfileId = 0;
            bool isLinked = true;

            // Act
            var result = await _service.GetGroupedDictCachedAsync(payment, patientId, childProfileId, isLinked, GroupByParam.Charge);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetGroupedDictCachedAsync_CacheHit_ReturnsCachedDictionary()
        {
            // Arrange
            var payment = new PaymentEntity { Id = 1, PaymentTypeId = (int)PaymentTypes.ClientPayment };
            int patientId = 100;
            int childProfileId = 50;
            bool isLinked = true;

            var cachedDict = new Dictionary<int, PatientPaymentClaimFullModel>
            {
                { 100, new PatientPaymentClaimFullModel { ChargeId = 100, InsurancePayment = 50 } },
                { 101, new PatientPaymentClaimFullModel { ChargeId = 101, InsurancePayment = 75 } }
            };

            _mockCacheService
                .Setup(x => x.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<Dictionary<int, PatientPaymentClaimFullModel>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(cachedDict);

            // Act
            var result = await _service.GetGroupedDictCachedAsync(payment, patientId, childProfileId, isLinked, GroupByParam.Charge);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.True(result.ContainsKey(100));
            Assert.True(result.ContainsKey(101));
        }

        [Fact]
        public async Task GetGroupedDictCachedAsync_CacheMiss_LoadsAndGroupsData()
        {
            // Arrange
            var payment = new PaymentEntity { Id = 1, PaymentTypeId = (int)PaymentTypes.ClientPayment };
            int patientId = 0;
            int childProfileId = 0;
            bool isLinked = false;

            var paymentClaims = new List<PaymentClaimEntity>
            {
                new PaymentClaimEntity { Id = 1, PaymentId = 1, ClaimId = 10, DateDeleted = null }
            };
            var paymentClaimsMock = paymentClaims.AsQueryable().BuildMock();
            _mockPaymentClaimRepository.Setup(x => x.Query()).Returns(paymentClaimsMock);

            var serviceLines = new List<PaymentClaimServiceLineEntity>
            {
                new PaymentClaimServiceLineEntity
                {
                    Id = 1,
                    PaymentClaimId = 1,
                    ClaimChargeEntryId = 100,
                    ChargeAmount = 200,
                    PaymentAmount = 50,
                    DateOfService = DateTime.Now, // Added DateOfService
                    ServiceCode = "99213", // Added ServiceCode
                    DateDeleted = null,
                    PaymentClaim = new PaymentClaimEntity
                    {
                        Id = 1,
                        ClaimId = 10,
                        PaymentId = 1,
                        DateDeleted = null,
                        ClientFirstName = "John",
                        ClientMiddleName = "M", // Added middle name
                        ClientLastName = "Doe",
                        Payment = new PaymentEntity { Id = 1, PaymentTypeId = (int)PaymentTypes.ClientPayment },
                        Claim = new ClaimEntity { Id = 10, ChildProfileId = 50, DateDeleted = null }
                    }
                }
            };
            var serviceLinesMock = serviceLines.AsQueryable().BuildMock();
            _mockPaymentClaimServiceLineRepository.Setup(x => x.Query()).Returns(serviceLinesMock);

            var adjustments = new List<PaymentClaimServiceLineAdjustmentEntity>
            {
                new PaymentClaimServiceLineAdjustmentEntity
                {
                    PaymentClaimServiceLineId = 1,
                    AdjustmentAmount = 20,
                    AdjustmentGroupCode = "CO",
                    IsAdjustmentPositive = false,
                    DateDeleted = null
                }
            };
            var adjustmentsMock = adjustments.AsQueryable().BuildMock();
            _mockPaymentClaimServiceLineAdjustmentRepository.Setup(x => x.Query()).Returns(adjustmentsMock);

            var errors = new List<PaymentClaimServiceLineErrorEntity>
            {
                new PaymentClaimServiceLineErrorEntity
                {
                    PaymentClaimServiceLineId = 1,
                    ErrorType = 1
                }
            };
            var errorsMock = errors.AsQueryable().BuildMock();
            _mockPaymentClaimServiceLineErrorEntity.Setup(x => x.Query()).Returns(errorsMock);

            // Setup cache service to execute the factory function
            _mockCacheService
                .Setup(x => x.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<PaymentGroupedModel>>>>(),
                    It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<List<PaymentGroupedModel>>>, TimeSpan>(async (key, factory, expiration) =>
                {
                    return await factory();
                });

            Dictionary<int, PatientPaymentClaimFullModel> capturedDict = null;
            _mockCacheService
                .Setup(x => x.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<Dictionary<int, PatientPaymentClaimFullModel>>>>(),
                    It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<Dictionary<int, PatientPaymentClaimFullModel>>>, TimeSpan>(async (key, factory, expiration) =>
                {
                    capturedDict = await factory();
                    return capturedDict;
                });

            // Act
            var result = await _service.GetGroupedDictCachedAsync(payment, patientId, childProfileId, isLinked, GroupByParam.Charge);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ContainsKey(100));
            Assert.NotNull(capturedDict);
            Assert.Equal(result.Count, capturedDict.Count);
        }

        [Fact]
        public async Task GetGroupedDictCachedAsync_WithDuplicateChargeIds_KeepsFirstOccurrence()
        {
            // Arrange
            var payment = new PaymentEntity { Id = 1, PaymentTypeId = (int)PaymentTypes.ClientPayment };
            int patientId = 0;
            int childProfileId = 0;

            // This test verifies the duplicate handling logic
            var mockDict = new Dictionary<int, PatientPaymentClaimFullModel>
            {
                { 100, new PatientPaymentClaimFullModel { ChargeId = 100, InsurancePayment = 50 } }
            };

            _mockCacheService
                .Setup(x => x.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<Dictionary<int, PatientPaymentClaimFullModel>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(mockDict);

            // Act
            var result = await _service.GetGroupedDictCachedAsync(payment, patientId, childProfileId, true, GroupByParam.Charge);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(50, result[100].InsurancePayment);
        }

        #endregion

        #region GetAllChargesNew Integration Tests

        [Fact]
        public async Task GetAllChargesNew_WithAdjustmentsAndErrors_AggregatesCorrectly()
        {
            // Arrange
            int paymentId = 1;
            int patientId = 0;
            int childProfileId = 0;

            var paymentClaims = new List<PaymentClaimEntity>
            {
                new PaymentClaimEntity { Id = 1, PaymentId = 1, ClaimId = 10, DateDeleted = null }
            };
            var paymentClaimsMock = paymentClaims.AsQueryable().BuildMock();
            _mockPaymentClaimRepository.Setup(x => x.Query()).Returns(paymentClaimsMock);

            var serviceLines = new List<PaymentClaimServiceLineEntity>
            {
                new PaymentClaimServiceLineEntity
                {
                    Id = 1,
                    PaymentClaimId = 1,
                    ServiceCode = "99213",
                    ClaimChargeEntryId = 100,
                    ChargeAmount = 200,
                    PaymentAmount = 120,
                    DateOfService = DateTime.Now,
                    DateDeleted = null,
                    PaymentClaim = new PaymentClaimEntity
                    {
                        Id = 1,
                        ClaimId = 10,
                        PaymentId = 1,
                        DateDeleted = null,
                        ClientFirstName = "John",
                        ClientLastName = "Doe",
                        Payment = new PaymentEntity { Id = 1, PaymentTypeId = (int)PaymentTypes.ClientPayment },
                        Claim = new ClaimEntity { Id = 10, ChildProfileId = 50, DateDeleted = null }
                    }
                }
            };
            var serviceLinesMock = serviceLines.AsQueryable().BuildMock();
            _mockPaymentClaimServiceLineRepository.Setup(x => x.Query()).Returns(serviceLinesMock);

            var adjustments = new List<PaymentClaimServiceLineAdjustmentEntity>
            {
                new PaymentClaimServiceLineAdjustmentEntity
                {
                    Id = 1,
                    PaymentClaimServiceLineId = 1,
                    AdjustmentAmount = 30,
                    IsAdjustmentPositive = false,
                    AdjustmentGroupCode = "CO",
                    DateDeleted = null
                },
                new PaymentClaimServiceLineAdjustmentEntity
                {
                    Id = 2,
                    PaymentClaimServiceLineId = 1,
                    AdjustmentAmount = 20,
                    IsAdjustmentPositive = false,
                    AdjustmentGroupCode = "PR",
                    DateDeleted = null
                }
            };
            var adjustmentsMock = adjustments.AsQueryable().BuildMock();
            _mockPaymentClaimServiceLineAdjustmentRepository.Setup(x => x.Query()).Returns(adjustmentsMock);

            var errors = new List<PaymentClaimServiceLineErrorEntity>
            {
                new PaymentClaimServiceLineErrorEntity
                {
                    Id = 1,
                    PaymentClaimServiceLineId = 1,
                    ErrorType = 1
                }
            };
            var errorsMock = errors.AsQueryable().BuildMock();
            _mockPaymentClaimServiceLineErrorEntity.Setup(x => x.Query()).Returns(errorsMock);

            // Act
            await _service.GetAllChargesNew(paymentId, patientId, childProfileId);

            // Assert
            _mockPaymentClaimRepository.Verify(x => x.Query(), Times.Once);
            _mockPaymentClaimServiceLineRepository.Verify(x => x.Query(), Times.Once);
            _mockPaymentClaimServiceLineAdjustmentRepository.Verify(x => x.Query(), Times.Once);
            _mockPaymentClaimServiceLineErrorEntity.Verify(x => x.Query(), Times.Once);
        }

        //[Fact]
        //public async Task GetAllChargesNew_UsesInMemoryCache_OnSecondCall()
        //{
        //    // Arrange
        //    int paymentId = 1;
        //    int patientId = 0;
        //    int childProfileId = 0;

        //    var paymentClaims = new List<PaymentClaimEntity>
        //    {
        //        new PaymentClaimEntity { Id = 1, PaymentId = 1, ClaimId = 10, DateDeleted = null }
        //    };
        //    var paymentClaimsMock = paymentClaims.AsQueryable().BuildMock();
        //    _mockPaymentClaimRepository.Setup(x => x.Query()).Returns(paymentClaimsMock);

        //    var serviceLines = new List<PaymentClaimServiceLineEntity>
        //    {
        //        new PaymentClaimServiceLineEntity
        //        {
        //            Id = 1,
        //            PaymentClaimId = 1,
        //            DateDeleted = null,
        //            PaymentClaim = new PaymentClaimEntity
        //            {
        //                Id = 1,
        //                ClaimId = 10,
        //                PaymentId = 1,
        //                DateDeleted = null,
        //                ClientFirstName = "John",
        //                ClientLastName = "Doe",
        //                Payment = new PaymentEntity { Id = 1 },
        //                Claim = new ClaimEntity { Id = 10, DateDeleted = null }
        //            }
        //        }
        //    };
        //    var serviceLinesMock = serviceLines.AsQueryable().BuildMock();
        //    _mockPaymentClaimServiceLineRepository.Setup(x => x.Query()).Returns(serviceLinesMock);

        //    var adjustments = new List<PaymentClaimServiceLineAdjustmentEntity>();
        //    var adjustmentsMock = adjustments.AsQueryable().BuildMock();
        //    _mockPaymentClaimServiceLineAdjustmentRepository.Setup(x => x.Query()).Returns(adjustmentsMock);

        //    var errors = new List<PaymentClaimServiceLineErrorEntity>();
        //    var errorsMock = errors.AsQueryable().BuildMock();
        //    _mockPaymentClaimServiceLineErrorEntity.Setup(x => x.Query()).Returns(errorsMock);

        //    // Act - First call
        //    await _service.GetAllChargesNew(paymentId, patientId, childProfileId);

        //    // Act - Second call (should use in-memory cache)
        //    await _service.GetAllChargesNew(paymentId, patientId, childProfileId);

        //    // Assert - Repository should only be called once due to in-memory cache
        //    _mockPaymentClaimRepository.Verify(x => x.Query(), Times.Once);
        //}

        #endregion

        #region GetAllCharges Tests

        [Fact]
        public async Task GetAllCharges_WithChildProfileIdZero_LoadsClaimsByPaymentId()
        {
            // Arrange
            int paymentId = 1;
            int childProfileId = 0;

            // Setup PaymentClaim repository
            var paymentClaims = new List<PaymentClaimEntity>
    {
        new PaymentClaimEntity { Id = 1, PaymentId = 1, ClaimId = 10, DateDeleted = null },
        new PaymentClaimEntity { Id = 2, PaymentId = 1, ClaimId = 20, DateDeleted = null }
    };
            var paymentClaimsMock = paymentClaims.AsQueryable().BuildMock();
            _mockPaymentClaimRepository.Setup(x => x.Query()).Returns(paymentClaimsMock);

            // Setup ServiceLine repository
            var serviceLines = new List<PaymentClaimServiceLineEntity>
    {
        new PaymentClaimServiceLineEntity
        {
            Id = 1,
            ClaimChargeEntryId = 100,
            ServiceCode = "99213",
            DateOfService = DateTime.Now,
            DateDeleted = null,
            PaymentClaim = new PaymentClaimEntity
            {
                Id = 1,
                ClaimId = 10,
                PaymentId = 1,
                DateDeleted = null,
                ClientFirstName = "John",
                ClientLastName = "Doe",
                Payment = new PaymentEntity { Id = 1 },
                Claim = new ClaimEntity { Id = 10, DateDeleted = null }
            },
            PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
            PaymentClaimServiceLineErrors = new List<PaymentClaimServiceLineErrorEntity>()
        },
        new PaymentClaimServiceLineEntity
        {
            Id = 2,
            ClaimChargeEntryId = 200,
            ServiceCode = "99214",
            DateOfService = DateTime.Now,
            DateDeleted = null,
            PaymentClaim = new PaymentClaimEntity
            {
                Id = 2,
                ClaimId = 20,
                PaymentId = 1,
                DateDeleted = null,
                ClientFirstName = "Jane",
                ClientLastName = "Smith",
                Payment = new PaymentEntity { Id = 1 },
                Claim = new ClaimEntity { Id = 20, DateDeleted = null }
            },
            PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
            PaymentClaimServiceLineErrors = new List<PaymentClaimServiceLineErrorEntity>()
        }
    };
            var serviceLinesMock = serviceLines.AsQueryable().BuildMock();
            _mockPaymentClaimServiceLineRepository.Setup(x => x.Query()).Returns(serviceLinesMock);

            // Act
            await _service.GetAllCharges(paymentId, childProfileId);
            var result = await _service.GetAllCharges(paymentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            _mockPaymentClaimRepository.Verify(x => x.Query(), Times.Exactly(2));
        }

        [Fact]
        public async Task GetAllCharges_FiltersOutDeletedServiceLines()
        {
            // Arrange
            int paymentId = 1;
            int childProfileId = 0;

            var paymentClaims = new List<PaymentClaimEntity>
    {
        new PaymentClaimEntity { Id = 1, PaymentId = 1, ClaimId = 10, DateDeleted = null }
    };
            var paymentClaimsMock = paymentClaims.AsQueryable().BuildMock();
            _mockPaymentClaimRepository.Setup(x => x.Query()).Returns(paymentClaimsMock);

            var serviceLines = new List<PaymentClaimServiceLineEntity>
    {
        new PaymentClaimServiceLineEntity
        {
            Id = 1,
            DateDeleted = null,
            PaymentClaim = new PaymentClaimEntity
            {
                ClaimId = 10,
                DateDeleted = null,
                ClientFirstName = "John",
                ClientLastName = "Doe",
                Payment = new PaymentEntity { Id = 1 },
                Claim = new ClaimEntity { Id = 10, DateDeleted = null }
            },
            PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
            PaymentClaimServiceLineErrors = new List<PaymentClaimServiceLineErrorEntity>()
        },
        new PaymentClaimServiceLineEntity
        {
            Id = 2,
            DateDeleted = DateTime.Now,
            PaymentClaim = new PaymentClaimEntity
            {
                ClaimId = 10,
                DateDeleted = null,
                ClientFirstName = "John",
                ClientLastName = "Doe",
                Payment = new PaymentEntity { Id = 1 },
                Claim = new ClaimEntity { Id = 10, DateDeleted = null }
            },
            PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
            PaymentClaimServiceLineErrors = new List<PaymentClaimServiceLineErrorEntity>()
        }
    };
            var serviceLinesMock = serviceLines.AsQueryable().BuildMock();
            _mockPaymentClaimServiceLineRepository.Setup(x => x.Query()).Returns(serviceLinesMock);

            // Act
            await _service.GetAllCharges(paymentId, childProfileId);
            var result = await _service.GetAllCharges(paymentId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetAllCharges_WithAdjustments_IncludesAdjustmentData()
        {
            // Arrange
            int paymentId = 1;
            int childProfileId = 0;

            var paymentClaims = new List<PaymentClaimEntity>
    {
        new PaymentClaimEntity { Id = 1, PaymentId = 1, ClaimId = 10, DateDeleted = null }
    };
            var paymentClaimsMock = paymentClaims.AsQueryable().BuildMock();
            _mockPaymentClaimRepository.Setup(x => x.Query()).Returns(paymentClaimsMock);

            var adjustments = new List<PaymentClaimServiceLineAdjustmentEntity>
    {
        new PaymentClaimServiceLineAdjustmentEntity
        {
            Id = 1,
            PaymentClaimServiceLineId = 1,
            AdjustmentAmount = 25,
            AdjustmentGroupCode = "CO",
            DateDeleted = null
        }
    };

            var serviceLines = new List<PaymentClaimServiceLineEntity>
    {
        new PaymentClaimServiceLineEntity
        {
            Id = 1,
            ClaimChargeEntryId = 100,
            DateDeleted = null,
            PaymentClaim = new PaymentClaimEntity
            {
                ClaimId = 10,
                DateDeleted = null,
                ClientFirstName = "John",
                ClientLastName = "Doe",
                Payment = new PaymentEntity { Id = 1 },
                Claim = new ClaimEntity { Id = 10, DateDeleted = null }
            },
            PaymentClaimServiceLineAdjustments = adjustments,
            PaymentClaimServiceLineErrors = new List<PaymentClaimServiceLineErrorEntity>()
        }
    };
            var serviceLinesMock = serviceLines.AsQueryable().BuildMock();
            _mockPaymentClaimServiceLineRepository.Setup(x => x.Query()).Returns(serviceLinesMock);

            // Act
            await _service.GetAllCharges(paymentId, childProfileId);
            var result = await _service.GetAllCharges(paymentId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.NotEmpty(result[0].Adjustments);
        }

        [Fact]
        public async Task GetAllCharges_WithErrors_SetsHasErrorsFlag()
        {
            // Arrange
            int paymentId = 1;
            int childProfileId = 0;

            var paymentClaims = new List<PaymentClaimEntity>
    {
        new PaymentClaimEntity { Id = 1, PaymentId = 1, ClaimId = 10, DateDeleted = null }
    };
            var paymentClaimsMock = paymentClaims.AsQueryable().BuildMock();
            _mockPaymentClaimRepository.Setup(x => x.Query()).Returns(paymentClaimsMock);

            var errors = new List<PaymentClaimServiceLineErrorEntity>
    {
        new PaymentClaimServiceLineErrorEntity
        {
            Id = 1,
            PaymentClaimServiceLineId = 1,
            ErrorType = 1
        }
    };

            var serviceLines = new List<PaymentClaimServiceLineEntity>
    {
        new PaymentClaimServiceLineEntity
        {
            Id = 1,
            ClaimChargeEntryId = 100,
            DateDeleted = null,
            PaymentClaim = new PaymentClaimEntity
            {
                ClaimId = 10,
                DateDeleted = null,
                ClientFirstName = "John",
                ClientLastName = "Doe",
                Payment = new PaymentEntity { Id = 1 },
                Claim = new ClaimEntity { Id = 10, DateDeleted = null }
            },
            PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
            PaymentClaimServiceLineErrors = errors
        }
    };
            var serviceLinesMock = serviceLines.AsQueryable().BuildMock();
            _mockPaymentClaimServiceLineRepository.Setup(x => x.Query()).Returns(serviceLinesMock);

            // Act
            await _service.GetAllCharges(paymentId, childProfileId);
            var result = await _service.GetAllCharges(paymentId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.True(result[0].HasErrors);
        }

        #endregion
    }
}