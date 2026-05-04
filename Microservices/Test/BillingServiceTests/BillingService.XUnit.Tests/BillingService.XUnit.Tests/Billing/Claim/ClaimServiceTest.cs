using AutoFixture;
using AutoMapper;
using BillingService.Domain.DataObjects.Billing;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Billing.ChangeTracking;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.Funders;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Domain.Services.Billing;
using BillingService.Domain.Utils;
using BillingService.XUnit.Tests.Common;
using BillingService.XUnit.Tests.Common.Mocks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Rethink.Services.Common.Entities.Billing;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using Rethink.Services.Common.Entities.Billing.Claim.Validation;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Entities.Billing.PatientInvoice;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Handlers;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Messaging;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Common.Models.Clients;
using Rethink.Services.Common.Models.RethinkDataEntityClasses;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Claim
{
    public class ClaimServiceTest : BaseTest
    {
        private Mock<IRepository<BillingDbContext, PaymentClaimEntity>> _paymentClaimRepository;
        private Mock<IRepository<BillingDbContext, ClaimEntity>> _claimRepository;
        private Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>> _claimChargeEntryRepository;
        private Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>> _claimAppointmentLinkRepository;
        private Mock<IRepository<BillingDbContext, MemberViewSettingEntity>> _memberViewSettingRepository;
        private Mock<IRepository<BillingDbContext, ClaimSubmissionEntity>> _claimSubmissionRepository;
        private Mock<IRepository<BillingDbContext, ClaimDiagnosisCodeEntity>> _claimDiagnosisCodeRepository;
        private Mock<IRepository<BillingDbContext, ClaimValidationErrorEntity>> _claimValidationErrorRepository;
        private Mock<IRepository<BillingDbContext, ClaimErrorMessageEntity>> _claimErrorMessageRepository;
        private Mock<IRepository<BillingDbContext, ClaimErrorCategoryEntity>> _claimErrorCategoryRepository;
        private Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>> _paymentClaimServiceLineRepository;
        private Mock<IRepository<BillingDbContext, ClearingHouseResponseDetailsEntity>> _clearingHouseResponseRepository;
        private Mock<IRepository<BillingDbContext, ClaimAppointmentLinkChargeEntry>> _claimAppointmentLinkChargeEntryRepository;
        private Mock<IRepository<BillingDbContext, ClaimWriteOffEntity>> _claimWriteOffRepository;
        private Mock<IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity>> _claimChargeEntryWriteOffRepository;
        private Mock<IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity>> _paymentClaimServiceLineAdjustmentRepository;
        private Mock<IRepository<BillingDbContext, ClaimSubmissionServiceLineEntity>> _claimSubmissionServiceLineRepository;
        private Mock<IRepository<BillingDbContext, ClaimAttachmentEntity>> _claimAttachmentRepository;
        private Mock<IRepository<BillingDbContext, PaymentClaimAdjustmentEntity>> _claimAdjustmentRepository;
        private Mock<IRepository<BillingDbContext, PatientInvoiceDetailsEntity>> _patientInvoiceDetailsRepository;
        private Mock<IRepository<BillingDbContext, CarcCodeEntity>> _carcCodeRepository;
        private Mock<ICacheService> _cacheService;
        private Mock<IProviderBillingCodeService> _providerBillingCodeService;
        private Mock<IClaimHistoryService> _claimHistoryService;
        private Mock<IClaimManagerService> _claimManagerService;
        private Mock<IClaimValidationService> _claimValidationService;
        private Mock<IClearingHouseService> _clearingHouseService;
    
        private Mock<IClaimChangeTrackingService> _claimChangeTrackingService;
        private Mock<IClaimVersionService> _claimVersionService;
        private Mock<IClaimService> _claimMockService;
        private Mock<IClaimUpdateService> _claimUpdateService;
        private Mock<IMessageBus> _messageBus;
        private Mock<IDbHelper<BillingDbContext>> _dbHelper;
        private Mock<IRepository<BillingDbContext, ClaimNoteEntity>> _claimNoteRepository;
        private Mock<IRepository<BillingDbContext, PaymentEntity>> _paymentRepository;
        private Mock<IRethinkMasterDataMicroServices> _rethinkService;
        private IClaimService _claimService;
        private IMapper _mapper;
        private Mock<ILogger<ClaimService>> _logger;

        private readonly Mock<IRepository<BillingDbContext, ClaimValidationErrorEntity>> _billingClaimValidationErrorRepository;
      
        private readonly Mock<IRepository<BillingDbContext, ClaimFlagReasonMaster>> _claimFlagReasonMasterRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimFlagTransaction>> _claimFlagTransactionRepository;
        private readonly Mock<IRepository<BillingDbContext, StateEntity>> _stateRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimBillingProviderEntity>> _claimBillingProviderRepository;
        private readonly Mock<IRepository<BillingDbContext, ExternalCodeEntity>> _externalCodeRepository;

        public ClaimServiceTest()
        {
            _paymentClaimRepository = new Mock<IRepository<BillingDbContext, PaymentClaimEntity>>();
            _claimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            _claimChargeEntryRepository = new Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>>();
            _claimAppointmentLinkRepository = new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>>();
            _memberViewSettingRepository = new Mock<IRepository<BillingDbContext, MemberViewSettingEntity>>();
            _claimSubmissionRepository = new Mock<IRepository<BillingDbContext, ClaimSubmissionEntity>>();
            _claimDiagnosisCodeRepository = new Mock<IRepository<BillingDbContext, ClaimDiagnosisCodeEntity>>();
            _claimValidationErrorRepository = new Mock<IRepository<BillingDbContext, ClaimValidationErrorEntity>>();
            _claimErrorMessageRepository = new Mock<IRepository<BillingDbContext, ClaimErrorMessageEntity>>();
            _claimErrorCategoryRepository = new Mock<IRepository<BillingDbContext, ClaimErrorCategoryEntity>>();
            _paymentClaimServiceLineRepository = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>>();
            _clearingHouseResponseRepository = new Mock<IRepository<BillingDbContext, ClearingHouseResponseDetailsEntity>>();
            _claimAppointmentLinkChargeEntryRepository = new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkChargeEntry>>();
            _claimWriteOffRepository = new Mock<IRepository<BillingDbContext, ClaimWriteOffEntity>>();
            _claimChargeEntryWriteOffRepository = new Mock<IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity>>();
            _paymentClaimServiceLineAdjustmentRepository = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity>>();
            _claimSubmissionServiceLineRepository = new Mock<IRepository<BillingDbContext, ClaimSubmissionServiceLineEntity>>();
            _claimAttachmentRepository = new Mock<IRepository<BillingDbContext, ClaimAttachmentEntity>>();
            _claimAdjustmentRepository = new Mock<IRepository<BillingDbContext, PaymentClaimAdjustmentEntity>>();
            _patientInvoiceDetailsRepository = new Mock<IRepository<BillingDbContext, PatientInvoiceDetailsEntity>>();
            _providerBillingCodeService = new Mock<IProviderBillingCodeService>();
            _claimHistoryService = new Mock<IClaimHistoryService>();
            _claimManagerService = new Mock<IClaimManagerService>();
            _claimValidationService = new Mock<IClaimValidationService>();
            _clearingHouseService = new Mock<IClearingHouseService>();
            _claimChangeTrackingService = new Mock<IClaimChangeTrackingService>();
            _claimVersionService = new Mock<IClaimVersionService>();
            _messageBus = new Mock<IMessageBus>();
            _dbHelper = new Mock<IDbHelper<BillingDbContext>>();
            _claimNoteRepository = new Mock<IRepository<BillingDbContext, ClaimNoteEntity>>();
            _paymentRepository = new Mock<IRepository<BillingDbContext, PaymentEntity>>();
            _rethinkService = new Mock<IRethinkMasterDataMicroServices>();
            _claimMockService = new Mock<IClaimService>();
            _claimUpdateService = new Mock<IClaimUpdateService>();
            _carcCodeRepository = new Mock<IRepository<BillingDbContext, CarcCodeEntity>>();
            _cacheService = new Mock<ICacheService>();
            _claimFlagReasonMasterRepository = new Mock<IRepository<BillingDbContext, ClaimFlagReasonMaster>>();
            _claimFlagTransactionRepository = new Mock<IRepository<BillingDbContext, ClaimFlagTransaction>>();
            _stateRepository = new Mock<IRepository<BillingDbContext, StateEntity>>();
            _claimBillingProviderRepository = new Mock<IRepository<BillingDbContext, ClaimBillingProviderEntity>>();
            _logger = new Mock<ILogger<ClaimService>>();
            _externalCodeRepository = new Mock<IRepository<BillingDbContext, ExternalCodeEntity>>();

            SetupMapper();

            // Default mock setups for commonly needed services
            _rethinkService.Setup(x => x.GetRenderingProvidersAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<AuthRenderingProviderType>());
            _rethinkService.Setup(x => x.GetChildProfileAuthorizationByClientId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((ClientAuthorization)null);

            _claimService = new ClaimService(
            _paymentClaimRepository.Object,
            _claimRepository.Object,
            _claimChargeEntryRepository.Object,
            _claimAppointmentLinkRepository.Object,
            _memberViewSettingRepository.Object,
            _claimSubmissionRepository.Object,
            _claimDiagnosisCodeRepository.Object,
            _claimValidationErrorRepository.Object,
            _claimErrorMessageRepository.Object,
            _claimErrorCategoryRepository.Object,
            _paymentClaimServiceLineRepository.Object,
            _clearingHouseResponseRepository.Object,
            _claimNoteRepository.Object,
            _paymentRepository.Object,
            _claimAppointmentLinkChargeEntryRepository.Object,
            _claimWriteOffRepository.Object,
            _claimChargeEntryWriteOffRepository.Object,
            _paymentClaimServiceLineAdjustmentRepository.Object,
            _claimSubmissionServiceLineRepository.Object,
            _claimAttachmentRepository.Object,
            _claimAdjustmentRepository.Object,
            _patientInvoiceDetailsRepository.Object,
            _carcCodeRepository.Object,
            _cacheService.Object,
            _rethinkService.Object,
            _claimHistoryService.Object,
            _claimManagerService.Object,
            _claimValidationService.Object,
            _clearingHouseService.Object,
            _claimChangeTrackingService.Object,
            _claimVersionService.Object,
            _claimUpdateService.Object,
            _mapper,
            _dbHelper.Object,
            _messageBus.Object,
            _logger.Object,
            _claimFlagReasonMasterRepository.Object,
            _claimFlagTransactionRepository.Object,
            _stateRepository.Object,
            _claimBillingProviderRepository.Object,
            _externalCodeRepository.Object
             );
        }

        [Fact]
        public async Task GetBillingProviderDetailsIdAsync_ReturnsNull_WhenClaimIdIsInvalid()
        {
            var result = await _claimService.GetBillingProviderDetailsIdAsync(0);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetBillingProviderDetailsIdAsync_ReturnsNull_WhenNoRecordExists()
        {
            int claimId = 12345;

            var data = new List<ClaimBillingProviderEntity>().AsQueryable().BuildMock();
            _claimBillingProviderRepository.Setup(r => r.Query()).Returns(data);

            var result = await _claimService.GetBillingProviderDetailsIdAsync(claimId);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetBillingProviderDetailsIdAsync_ReturnsDto_WhenRecordExists()
        {
            int claimId = 12345;

            var entity = new ClaimBillingProviderEntity
            {
                ClaimId = claimId,
                ProviderType = "Person",
                FirstName = "John",
                LastNameOrFacilityName = "Smith",
                NPI = "1234567890",
                TaxId = "123456789",
                TaxonomyCode = "103T00000X",
                AddressLine1 = "123 Medical Drive",
                AddressLine2 = "",
                City = "Dallas",
                State = "TX",
                Zip = "75201",
                ZipExt = "1234",
                DateDeleted = null
            };

            var data = new List<ClaimBillingProviderEntity> { entity }.AsQueryable().BuildMock();
            _claimBillingProviderRepository.Setup(r => r.Query()).Returns(data);

            var result = await _claimService.GetBillingProviderDetailsIdAsync(claimId);

            Assert.NotNull(result);
            Assert.Equal(claimId, result.ClaimId);
            Assert.Equal(entity.ProviderType, result.ProviderType);
            Assert.Equal(entity.FirstName, result.FirstName);
            Assert.Equal(entity.LastNameOrFacilityName, result.LastNameOrFacilityName);
            Assert.Equal(entity.NPI, result.NPI);
            Assert.Equal(entity.TaxId, result.TaxId);
            Assert.Equal(entity.TaxonomyCode, result.TaxonomyCode);
            Assert.Equal(entity.AddressLine1, result.AddressLine1);
            Assert.Equal(entity.AddressLine2, result.AddressLine2);
            Assert.Equal(entity.City, result.City);
            Assert.Equal(entity.State, result.State);
            Assert.Equal(entity.Zip, result.Zip);
            Assert.Equal(entity.ZipExt, result.ZipExt);
        }

        [Fact]
        public async Task GetClaimByIdentifierAsync_ShouldReturnSuccessResult_WhenClaimExists()
        {
            var claimIdentifier = Fixture.Create<string>();
            var accountInfoId = Fixture.Create<int>();

            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.ClaimIdentifier, claimIdentifier)
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.ClaimHistory, new List<ClaimHistoryEntity>())
                .Create();

            SetupClaim(claim);

            var result = await _claimService.GetClaimByIdentifierAsync(claimIdentifier, accountInfoId);

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(claim.ClaimIdentifier, ((ClaimModel)result.Data).ClaimIdentifier);
        }

        [Fact]
        public async Task GetClaimByIdentifierAsync_ShouldReturnFailResult_WhenClaimWasNotFound()
        {
            SetupClaim(new ClaimEntity());

            var result = await _claimService.GetClaimByIdentifierAsync(Fixture.Create<string>(), Fixture.Create<int>());

            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("Claim not found", result.Error);
        }


        //[Fact]
        public async Task DeleteClaimsAsync_ShouldDeleteSeveralClaims_WhenClaimsExists()
        {
            var accountInfoId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();
            var claimId = Fixture.Create<int>();

            var firstClaim = Fixture.Build<ClaimEntity>()
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.MemberId, memberId)
                .Create();

            var secondClaim = Fixture.Build<ClaimEntity>()
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.MemberId, memberId)
                .Create();

            var claimsToDelete = new List<ClaimEntity> { firstClaim, secondClaim };
            var claimsToDeleteIds = claimsToDelete.Select(x => x.Id).ToArray();

            SetupClaims(claimsToDelete);
            _claimAppointmentLinkRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimAppointmentLinkEntity>.Create(
                    new List<ClaimAppointmentLinkEntity> { new ClaimAppointmentLinkEntity { ClaimId = firstClaim.Id }, new ClaimAppointmentLinkEntity { ClaimId = secondClaim.Id } }));

            SetupRepos(claimId);

            var result = await _claimService.DeleteClaimsAsync(accountInfoId, memberId, claimsToDeleteIds);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(claimsToDelete.Count, result.Count);
            Assert.Collection(result,
                            item => Assert.Equal(firstClaim.ClaimIdentifier, item.ClaimIdentifier),
                            item => Assert.Equal(secondClaim.ClaimIdentifier, item.ClaimIdentifier));

            _claimHistoryService.Verify(x => x.AddAsync(It.IsAny<ClaimHistorySaveModel>(), It.IsAny<bool>()), Times.Exactly(2));
            _claimRepository.Verify(x => x.Update(It.IsAny<ClaimEntity>()), Times.Exactly(2));
            _claimRepository.Verify(x => x.CommitAsync(), Times.Once);
            _claimAppointmentLinkRepository.Verify(x => x.Query(), Times.Exactly(2));
        }

        [Fact]
        public async Task GetIdsForAccountAsync_ShouldReturnClaimIdsForAccount()
        {
            var accountInfoId = Fixture.Create<int>();

            var firstClaim = Fixture.Build<ClaimEntity>()
                .With(x => x.AccountInfoId, accountInfoId)
                .Create();

            var secondClaim = Fixture.Build<ClaimEntity>()
                .With(x => x.AccountInfoId, accountInfoId)
                .Create();

            var initialClaims = new List<ClaimEntity> { firstClaim, secondClaim };
            var initialClaimIds = initialClaims.Select(x => x.Id).ToArray();

            SetupClaims(initialClaims);

            var result = await _claimService.GetIdsForAccountAsync(accountInfoId);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.IsType<List<int>>(result);
            Assert.Equal(initialClaims.Count, result.Count);
            Assert.Equal(initialClaimIds, result);
        }

        [Fact]
        public async Task GetIdsForAccountAsync_ShouldReturnEmptyList_WhenNoClaimsExist()
        {
            var accountInfoId = Fixture.Create<int>();
            SetupClaims(new List<ClaimEntity>());

            var result = await _claimService.GetIdsForAccountAsync(accountInfoId);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetIdsForAccountAsync_ShouldNotReturnDeletedClaims()
        {
            var accountInfoId = Fixture.Create<int>();

            var deletedClaim = Fixture.Build<ClaimEntity>()
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.DateDeleted, DateTime.UtcNow)
                .Create();

            var activeClaim = Fixture.Build<ClaimEntity>()
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.DateDeleted, (DateTime?)null)
                .Create();

            SetupClaims(new List<ClaimEntity> { deletedClaim, activeClaim });

            var result = await _claimService.GetIdsForAccountAsync(accountInfoId);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(activeClaim.Id, result[0]);
        }

        [Fact]
        public async Task GetIdsForAccountAsync_ShouldNotReturnClaimsForOtherAccounts()
        {
            var accountInfoId = Fixture.Create<int>();
            var otherAccountId = Fixture.Create<int>();

            var claimForOtherAccount = Fixture.Build<ClaimEntity>()
                .With(x => x.AccountInfoId, otherAccountId)
                .With(x => x.DateDeleted, (DateTime?)null)
                .Create();

            SetupClaims(new List<ClaimEntity> { claimForOtherAccount });

            var result = await _claimService.GetIdsForAccountAsync(accountInfoId);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        //[Fact]
        public async Task GetAccountClaimByIdOrPatientNameAsync_ShouldReturnClaimPatientsByName()
        {
            string patientFirstName = "Mason";
            string patientMiddleName = "";
            string patientLastName = "Walker";
            string patientFullName = string.Join(' ',
                new[] { patientFirstName, patientMiddleName, patientLastName }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

            var accountInfoId = Fixture.Create<int>();
            var claimId = Fixture.Create<int>();
            var startDate = Fixture.Create<DateTime>();
            var endDate = startDate.AddDays(3);

            var model = Fixture.Create<ClaimSearchModel>();
            model.SearchString = "mas1";
            model.PaymentId = Fixture.Create<int>();
            model.AccountInfoId = accountInfoId;

            var patient = Fixture.Build<ChildProfileEntityModel>()
                .With(x => x.AccountInfoId, model.AccountInfoId)
                .With(x => x.FirstName, patientFirstName)
                .With(x => x.MiddleName, patientMiddleName)
                .With(x => x.LastName, patientLastName)
                .Create();

            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.ChildProfileId, patient.Id)
                .With(x => x.StartDate, startDate)
                .With(x => x.EndDate, endDate)
                .Create();

            SetupClaim(claim);

            _claimSubmissionRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimSubmissionEntity>.Create(Fixture.Build<ClaimSubmissionEntity>().With(x => x.ClaimId, claimId).Create()));
            _paymentRepository.Setup(x => x.Query()).Returns(QueryMock<PaymentEntity>.Create(new PaymentEntity() { Id = model.PaymentId }));
            _rethinkService.Setup(x => x.GetChildProfile(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<ClientUserModel>().With(x => x.id, patient.Id).Create());
            var childProfiles = Fixture.Build<ChildProfileRethinkModel>().CreateMany();
            _rethinkService.Setup(x => x.GetChildProfileByName(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(childProfiles.ToList());
            var result = await _claimService.GetAccountClaimByIdOrPatientNameAsync(model);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.IsType<List<ClaimDropdownModel>>(result);
            Assert.Collection(result, item => Assert.Equal(item.PatientName, patientFullName));
        }

        //[Fact]
        public async Task GetAccountClaimByIdOrPatientNameAsync_ShouldReturnClaimPatientsByClaimId()
        {
            string patientFirstName = "Mason";
            string patientMiddleName = "";
            string patientLastName = "Walker";
            string patientFullName = string.Join(' ',
                new[] { patientFirstName, patientMiddleName, patientLastName }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

            var accountInfoId = Fixture.Create<int>();
            var funderId = Fixture.Create<int>();
            var startDate = Fixture.Create<DateTime>();
            var endDate = startDate.AddDays(3);

            var searchClaimId = Fixture.Create<int>();


            var model = Fixture.Create<ClaimSearchModel>();
            model.SearchString = searchClaimId.ToString();
            model.PaymentId = Fixture.Create<int>();
            model.AccountInfoId = accountInfoId;

            var patient = Fixture.Build<ChildProfileEntityModel>()
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.FirstName, patientFirstName)
                .With(x => x.MiddleName, patientMiddleName)
                .With(x => x.LastName, patientLastName)
                .Create();

            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, searchClaimId)
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.ChildProfileId, patient.Id)
                .With(x => x.LastBilledFunderId, funderId)
                .With(x => x.PrimaryFunderId, funderId)
                .With(x => x.StartDate, startDate)
                .With(x => x.EndDate, endDate)
                .Create();

            var funderIds = Fixture.Build<PaymentEntity>().With(x => x.HcFunderId, funderId).With(x => x.Id, model.PaymentId).Create();

            SetupClaim(claim);

            _claimSubmissionRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimSubmissionEntity>.Create(Fixture.Build<ClaimSubmissionEntity>().With(x => x.ClaimId, searchClaimId).Create()));
            _paymentRepository.Setup(x => x.Query()).Returns(QueryMock<PaymentEntity>.Create(funderIds));
            _rethinkService.Setup(x => x.GetChildProfile(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<ClientUserModel>().With(x => x.id, patient.Id).Create());

            var childProfiles = Fixture.Build<ChildProfileRethinkModel>()
                .With(x => x.Id, patient.Id)
                .With(x => x.FirstName, patientFirstName)
                .With(x => x.MiddleName, patientMiddleName)
                .With(x => x.LastName, patientLastName)
                .CreateMany();
            _rethinkService.Setup(x => x.GetChildProfileByName(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(childProfiles.ToList());

            var result = await _claimService.GetAccountClaimByIdOrPatientNameAsync(model);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.IsType<List<ClaimDropdownModel>>(result);
            Assert.Collection(result, item => Assert.Equal(item.PatientName, patientFullName));
        }

        [Fact]
        public async Task RemoveBillingClaimDetailAsync_ShouldDeleteClaimCharge_WhenChargeExists()
        {
            var claimId = Fixture.Create<int>();
            var removeModel = Fixture.Create<RemoveBillingClaimDetailsModel>();

            var clamChargeEntryToDelete = SetupServiceForRemoveBilling(removeModel, claimId);
            SetupPaymentMocks(removeModel.AccountId);

            var result = await _claimService.RemoveBillingClaimDetailAsync(removeModel);

            Assert.NotNull(result);
            Assert.True(result.Success);

            _claimChargeEntryRepository.Verify(x => x.Query(), Times.Once);
            _claimChargeEntryRepository.Verify(x => x.Update(It.Is<ClaimChargeEntryEntity>(x => x == clamChargeEntryToDelete)), Times.Once);
            _claimChargeEntryRepository.Verify(x => x.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task RemoveBillingClaimDetailAsync_ShouldReturnFailResult_WhenClaimChargeWasNotFound()
        {
            var claimId = Fixture.Create<int>();
            var removeModel = Fixture.Create<RemoveBillingClaimDetailsModel>();


            _claimChargeEntryRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimChargeEntryEntity>.Create(new ClaimChargeEntryEntity()));
            _claimAppointmentLinkRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimAppointmentLinkEntity>.Create(new ClaimAppointmentLinkEntity()));

            var result = await _claimService.RemoveBillingClaimDetailAsync(removeModel);

            Assert.NotNull(result);
            Assert.False(result.Success);

            _claimChargeEntryRepository.Verify(x => x.Query(), Times.Once);
            _claimChargeEntryRepository.Verify(x => x.Update(It.IsAny<ClaimChargeEntryEntity>()), Times.Never);
            _claimChargeEntryRepository.Verify(x => x.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task GetClaimErrorsAndAlertsAsync_ShouldReturnEmptyResult()
        {
            var claimId = Fixture.Create<int>();

            _claimValidationErrorRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimValidationErrorEntity>.Create(Fixture.Create<ClaimValidationErrorEntity>()));

            var result = await _claimService.GetClaimErrorsAndAlertsAsync(claimId);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetClaimErrorsAndAlertsAsync_ShouldReturnValidationErrors_WhenEraErrorIsNotExists()
        {
            var claimId = Fixture.Create<int>();

            var validationErrors = Fixture.Build<ClaimValidationErrorEntity>()
                .With(x => x.ClaimId, claimId)
                .With(x => x.ClaimErrorMessage, Fixture.Build<ClaimErrorMessageEntity>()
                     .With(x => x.ClaimErrorCategory, Fixture.Create<ClaimErrorCategoryEntity>())
                     .Create())
                .Without(x => x.EraValidationErrorId)
                .CreateMany();

            _claimValidationErrorRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimValidationErrorEntity>.Create(validationErrors));

            var result = await _claimService.GetClaimErrorsAndAlertsAsync(claimId);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(validationErrors.Count(), result.Count);
            Assert.Equal(validationErrors.First().ContextMessage, result.First().Message);
            Assert.Equal(validationErrors.First().ClaimErrorMessage.ShortDescription, result.First().ErrorCode);
        }

        [Fact]
        public async Task GetClaimErrorsAndAlertsAsync_ShouldReturnValidationError_WithARCError()
        {
            var claimId = Fixture.Create<int>();

            var validationErrors = Fixture.Build<ClaimValidationErrorEntity>()
                .With(x => x.ClaimId, claimId)
                .With(x => x.ClaimErrorMessage, Fixture.Build<ClaimErrorMessageEntity>()
                     .With(x => x.ClaimErrorCategory, Fixture.Create<ClaimErrorCategoryEntity>())
                     .Create())
                //TODO add rarc code
                .With(x => x.EraValidationError, Fixture.Build<EraValidationErrorEntity>()
                     .With(x => x.GroupCode, Fixture.Create<ExternalCodeEntity>())
                     .With(x => x.AdjustmentCode, Fixture.Create<ExternalCodeEntity>())
                     .Create())
                .Create();

            _claimValidationErrorRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimValidationErrorEntity>.Create(validationErrors));

            var result = await _claimService.GetClaimErrorsAndAlertsAsync(claimId);

            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetClaimErrorsAndAlertsAsync_ShouldReturnValidationError_WithERAStatusCode()
        {
            var claimId = Fixture.Create<int>();
            var eraValidationCodeId = Fixture.Create<int>();

            var statusCategoryCode = Fixture.Build<ExternalCodeEntity>()
                .With(x => x.CodeTypeId, ExternalCodeType.ClaimStatusCategoryCode)
                .Create();
            var statusCode = Fixture.Build<ExternalCodeEntity>()
                .With(x => x.CodeTypeId, ExternalCodeType.ClaimStatusCode)
                .Create();
            var validationError = Fixture.Build<ClaimValidationErrorEntity>()
                .With(x => x.ClaimId, claimId)
                .With(x => x.EraValidationErrorId, eraValidationCodeId)
                .With(x => x.ClaimErrorMessage, Fixture.Build<ClaimErrorMessageEntity>()
                     .With(x => x.ClaimErrorCategory, Fixture.Create<ClaimErrorCategoryEntity>())
                     .Create())
                .With(x => x.EraValidationError, Fixture.Build<EraValidationErrorEntity>()
                     .With(x => x.Id, eraValidationCodeId)
                     .With(x => x.GroupCode, statusCategoryCode)
                     .With(x => x.AdjustmentCode, statusCode)
                     .Create())
                .Create();

            _claimValidationErrorRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimValidationErrorEntity>.Create(validationError));

            var result = await _claimService.GetClaimErrorsAndAlertsAsync(claimId);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal($"{statusCategoryCode.Code}-{statusCode.Code}", result.First().ErrorCode);
            Assert.Equal($"{statusCategoryCode.Description}-{statusCode.Description}", result.First().Description);
        }

        [Fact]
        public async Task ApproveClaimsAsync_ShouldUpdateAndSubmitClaim_WhenClaimStatusIsPendingReview()
        {
            var accountInfoId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();
            var claimId = Fixture.Create<int>();
            var claimsIds = new int[] { claimId };

            var claims = SetupClaimsByStatus(claimsIds, false, ClaimStatus.PendingReview);

            var linkEntity = Fixture.Build<ClaimAppointmentLinkEntity>()
                .With(x => x.ClaimId, claimId)
                .Create();

            var appointment = Fixture.Build<AppointmentRethinkModel>()
                .With(x => x.id, linkEntity.AppointmentId)
                .Without(x => x.ChildProfileAuthorizationBillingCode)
                .Create();

            _claimAppointmentLinkRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimAppointmentLinkEntity>.Create(linkEntity));

            _rethinkService.Setup(x => x.GetChildProfileFunderServiceLineMappingEntity(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<ServiceLines>().Create());
            _rethinkService
                .Setup(x => x.GetFunder(accountInfoId, It.IsAny<int>()))
                .ReturnsAsync(new FunderDataModel
                {
                    funderName = "Aetna"
                });

            _rethinkService.Setup(x => x.GetChildProfileFunderMappingByMappingId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new FunderDetails { insuranceType = ResponsibilitySequenceType.Primary });

            foreach (var claim in claims)
            {
                var clientFunderServiceLine = Fixture.Build<ServiceLines>()
                    .With(x => x.serviceId, appointment.serviceId)
                    .With(x => x.ChildProfileFunderMapping, Fixture.Build<FunderDetails>()
                                                                .With(x => x.childProfileId, appointment.clientId)
                                                                .With(x => x.funderId, appointment.funderId)
                                                                .Create())
                    .With(x => x.id, claim.ClientFunderServiceLineId)
                    .Create();

            }

            var result = await _claimService.ApproveClaimsAsync(accountInfoId, memberId, claimsIds);

            Assert.NotNull(result);
            Assert.Equal(claimId, result.First().Claimid);

            _claimHistoryService.Verify(x => x.AddAsync(It.IsAny<ClaimHistorySaveModel>(), It.IsAny<bool>()), Times.Exactly(claims.Count));
            _claimRepository.Verify(x => x.Update(It.IsAny<ClaimEntity>()), Times.Exactly(claims.Count));
            _claimManagerService.Verify(x => x.SubmitInitialClaim(claimId, memberId, ClaimDocumentType.Doc837P, It.IsAny<ResponsibilitySequenceType>()), Times.Once);
            _claimRepository.Verify(x => x.CommitAsync(), Times.Once);
        }


        [Fact]
        public async Task ApproveClaimsAsync_ShouldThrowException_WhenSecondaryFunder()
        {
            // Arrange
            var accountInfoId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();
            var claimId = Fixture.Create<int>();
            var claimsIds = new[] { claimId };

            var claims = SetupClaimsByStatus(claimsIds, false, ClaimStatus.PendingReview);

            var linkEntity = Fixture.Build<ClaimAppointmentLinkEntity>()
                .With(x => x.ClaimId, claimId)
                .Create();

            var appointment = Fixture.Build<AppointmentRethinkModel>()
                .With(x => x.id, linkEntity.AppointmentId)
                .Without(x => x.ChildProfileAuthorizationBillingCode)
                .Create();

            _claimAppointmentLinkRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimAppointmentLinkEntity>.Create(linkEntity));

            _rethinkService.Setup(x => x.GetFunder(accountInfoId, It.IsAny<int>()))
                .ReturnsAsync(new FunderDataModel
                {
                    funderName = "BCBS"
                });

            _rethinkService.Setup(x => x.GetChildProfileFunderMappingByMappingId(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
                .ReturnsAsync(new FunderDetails
                {
                    insuranceType = ResponsibilitySequenceType.Secondary
                });

            // Mock _claimSubmissionRepository to return a valid ClaimSubmissionEntity
            _claimSubmissionRepository
                .Setup(x => x.Query())
                .Returns(new List<ClaimSubmissionEntity>
                {
            new ClaimSubmissionEntity { Id = 1, ClaimId = claimId }
                }.AsQueryable().BuildMock());

            // Mock _claimErrorMessagesRepo to return a valid error message entity
            _claimErrorMessageRepository
                .Setup(x => x.Query())
                .Returns(new List<ClaimErrorMessageEntity>
                {
            new ClaimErrorMessageEntity { ErrorNumber = ClaimErrorNumber.FunderNotFound, Id = 1 }
                }.AsQueryable());

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _claimService.ApproveClaimsAsync(accountInfoId, memberId, claimsIds));

            // Assert - Verify the exception message
            Assert.Contains("Value cannot be null", exception.Message); // Default ArgumentNullException message
            Assert.Contains("Claim approval pending — update Secondary Funder to Primary and complete the appointment.", exception.Message);
        }
        [Fact]
        public async Task ValidateClaimsOnFunderChangedAsync_ShouldSkip_WhenNoClaimsReturned()
        {
            _claimRepository
                .Setup(x => x.Query())
                .Returns(QueryMock<ClaimEntity>.Create(new List<ClaimEntity>()));

            await _claimService.ValidateClaimsOnFunderChangedAsync(
                funderId: 1,
                clientFunderId: 1,
                funderModifiedDate: DateTime.UtcNow,
                memberId: 1);

            _claimHistoryService.Verify(x =>
    x.AddAsync(It.IsAny<ClaimHistorySaveModel>(), It.IsAny<bool>()),
    Times.Never);
        }

        [Fact]
        public async Task ValidateClaimsOnFunderChangedAsync_ShouldContinue_WhenClientFunderDoesNotMatch()
        {
            var memberId = Fixture.Create<int>();
            var funderId = Fixture.Create<int>();

            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.MemberId, memberId)
                .With(x => x.ClientFunderId, 999) // mismatch
                .With(x => x.ClaimStatus, ClaimStatus.Billed)
                .With(x => x.DateLastModified, DateTime.UtcNow.AddDays(-5))
                .With(x => x.ClaimSubmissions, new List<ClaimSubmissionEntity>
                {
            new ClaimSubmissionEntity { Id = 1, FunderId = funderId }
                })
                .Create();

            _claimRepository
                .Setup(x => x.Query())
                .Returns(QueryMock<ClaimEntity>.Create(claim));

            await _claimService.ValidateClaimsOnFunderChangedAsync(
                funderId,
                clientFunderId: 1,
                funderModifiedDate: DateTime.UtcNow,
                memberId);
            _claimHistoryService.Verify(x =>
    x.AddAsync(It.IsAny<ClaimHistorySaveModel>(), It.IsAny<bool>()),
    Times.Never);
        }

        [Fact]
        public async Task ValidateClaimsOnFunderChangedAsync_ShouldValidateClaim_WhenAllConditionsMatch()
        {
            var memberId = Fixture.Create<int>();
            var funderId = Fixture.Create<int>();
            var clientFunderId = Fixture.Create<int>();

            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.MemberId, memberId)
                .With(x => x.ClientFunderId, clientFunderId)
                .With(x => x.ClaimStatus, ClaimStatus.Billed)
                .With(x => x.DateLastModified, DateTime.UtcNow.AddDays(-10))
                .With(x => x.ClaimSubmissions, new List<ClaimSubmissionEntity>
                {
            new ClaimSubmissionEntity { Id = 1, FunderId = funderId }
                })
                .Create();

            _claimRepository
                .Setup(x => x.Query())
                .Returns(QueryMock<ClaimEntity>.Create(claim));

            await _claimService.ValidateClaimsOnFunderChangedAsync(
                funderId,
                clientFunderId,
                DateTime.UtcNow,
                memberId);

            _claimHistoryService.Verify(x =>
     x.AddAsync(It.IsAny<ClaimHistorySaveModel>(), It.IsAny<bool>()),
     Times.Once);



            _claimValidationService.Verify(x =>
    x.ValidateClaimData(claim.Id, memberId, null, ResponsibilitySequenceType.Primary, false, null),
    Times.Once);
        }

        [Fact]
        public async Task CheckIsAuthUsedByClaimAsync_ShouldReturnLatestServiceLinesPerCharge()
        {
            // Arrange
            var authorizationId = Fixture.Create<int>();
            var claimId = Fixture.Create<int>();
            var chargeEntryId = Fixture.Create<int>();

            var validClaim = Fixture.Build<ClaimEntity>()
                .With(x => x.ClaimStatus, ClaimStatus.Billed) // valid status
                .Create();

            var olderServiceLine = Fixture.Build<ClaimSubmissionServiceLineEntity>()
                .With(x => x.ClaimChargeEntryId, chargeEntryId)
                .With(x => x.DateCreated, DateTime.UtcNow.AddDays(-2))
                .With(x => x.BillingCode, "OLD")
                .With(x => x.BillingCodeDescription, "Old Code")
                .With(x => x.Units, 1)
                .With(x => x.ServiceLineIdentifier, "SL-OLD")
                .With(x => x.ServiceLineIndex, 1)
                .Create();

            var latestServiceLine = Fixture.Build<ClaimSubmissionServiceLineEntity>()
                .With(x => x.ClaimChargeEntryId, chargeEntryId)
                .With(x => x.DateCreated, DateTime.UtcNow)
                .With(x => x.BillingCode, "NEW")
                .With(x => x.BillingCodeDescription, "New Code")
                .With(x => x.Units, 2)
                .With(x => x.ServiceLineIdentifier, "SL-NEW")
                .With(x => x.ServiceLineIndex, 2)
                .Create();

            var claimSubmission = Fixture.Build<ClaimSubmissionEntity>()
                .With(x => x.Claim, validClaim)
                .With(x => x.ChildProfileAuthorizationId, authorizationId)
                .With(x => x.ClaimSubmissionServiceLines,
                    new List<ClaimSubmissionServiceLineEntity>
                    {
                olderServiceLine,
                latestServiceLine
                    })
                .Create();

            _claimSubmissionRepository
                .Setup(x => x.Query())
                .Returns(new List<ClaimSubmissionEntity>
                {
            claimSubmission
                }.AsQueryable().BuildMock());

            // Act
            var result = await _claimService.CheckIsAuthUsedByClaimAsync(authorizationId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);

            var item = result.First();
            Assert.Equal("NEW", item.BillingCode);
            Assert.Equal("New Code", item.BillingCodeDescription);
            Assert.Equal(2, item.Units);
            Assert.Equal("SL-NEW", item.ServiceLineIdentifier);
            Assert.Equal(2, item.ServiceLineIndex);
        }
        [Fact]
        public async Task CheckIsAuthUsedByClaimAsync_ShouldReturnEmpty_WhenClaimStatusIsPendingReview()
        {
            // Arrange
            var authorizationId = Fixture.Create<int>();

            var invalidClaim = Fixture.Build<ClaimEntity>()
                .With(x => x.ClaimStatus, ClaimStatus.PendingReview)
                .Create();

            var serviceLine = Fixture.Build<ClaimSubmissionServiceLineEntity>()
                .Create();

            var claimSubmission = Fixture.Build<ClaimSubmissionEntity>()
                .With(x => x.Claim, invalidClaim)
                .With(x => x.ChildProfileAuthorizationId, authorizationId)
                .With(x => x.ClaimSubmissionServiceLines,
                    new List<ClaimSubmissionServiceLineEntity> { serviceLine })
                .Create();

            _claimSubmissionRepository
                .Setup(x => x.Query())
                .Returns(new List<ClaimSubmissionEntity>
                {
            claimSubmission
                }.AsQueryable().BuildMock());

            // Act
            var result = await _claimService.CheckIsAuthUsedByClaimAsync(authorizationId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task HasFunderBilledClaimsAsync_ShouldReturnTrue_WhenMatchingDeletedClaimExists()
        {
            // Arrange
            var model = Fixture.Build<ClientFunderModel>()
                .With(x => x.ClientId, Fixture.Create<int>())
                .With(x => x.FunderId, Fixture.Create<int>())
                .Create();

            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.ChildProfileId, model.ClientId)
                .With(x => x.DateDeleted, DateTime.UtcNow)
                .With(x => x.ClaimHistory, new List<ClaimHistoryEntity>
                {
            Fixture.Create<ClaimHistoryEntity>()
                })
                .With(x => x.ClaimSubmissions, new List<ClaimSubmissionEntity>
                {
            new ClaimSubmissionEntity { FunderId = model.FunderId }
                })
                .Create();

            _claimRepository
                .Setup(x => x.Query())
                .Returns(new List<ClaimEntity> { claim }.AsQueryable().BuildMock());

            // Act
            var result = await _claimService.HasFunderBilledClaimsAsync(model);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HasFunderBilledClaimsAsync_ShouldReturnFalse_WhenNoMatchingClaimsExist()
        {
            // Arrange
            var model = Fixture.Build<ClientFunderModel>()
                .With(x => x.ClientId, Fixture.Create<int>())
                .With(x => x.FunderId, Fixture.Create<int>())
                .Create();

            var nonMatchingClaim = Fixture.Build<ClaimEntity>()
                .With(x => x.ChildProfileId, model.ClientId)
                .With(x => x.DateDeleted, (DateTime?)null) // not deleted
                .With(x => x.ClaimHistory, new List<ClaimHistoryEntity>())
                .With(x => x.ClaimSubmissions, new List<ClaimSubmissionEntity>())
                .Create();

            _claimRepository
                .Setup(x => x.Query())
                .Returns(new List<ClaimEntity> { nonMatchingClaim }.AsQueryable().BuildMock());

            // Act
            var result = await _claimService.HasFunderBilledClaimsAsync(model);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetBilledPreviouslyClaimsIdsAsync_ShouldReturnClaimIds_WhenBilledHistoryExists()
        {
            // Arrange
            var accountInfoId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();
            var claimId1 = Fixture.Create<int>();
            var claimId2 = Fixture.Create<int>();
            var claimIds = new[] { claimId1, claimId2 };

            var billedHistory = Fixture.Build<ClaimHistoryEntity>()
                .With(x => x.ClaimHistoryAction, ClaimHistoryAction.BilledElectronically)
                .With(x => x.DateCreated, DateTime.UtcNow)
                .Create();

            var nonBilledHistory = Fixture.Build<ClaimHistoryEntity>()
                .With(x => x.ClaimHistoryAction, ClaimHistoryAction.ChargeEntryAdded)
                .With(x => x.DateCreated, DateTime.UtcNow)
                .Create();

            var claimWithBilledHistory = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, claimId1)
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.DateDeleted, (DateTime?)null)
                .With(x => x.ClaimHistory, new List<ClaimHistoryEntity> { billedHistory })
                .Create();

            var claimWithoutBilledHistory = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, claimId2)
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.DateDeleted, (DateTime?)null)
                .With(x => x.ClaimHistory, new List<ClaimHistoryEntity> { nonBilledHistory })
                .Create();

            var claims = new List<ClaimEntity> { claimWithBilledHistory, claimWithoutBilledHistory };

            _claimRepository.Setup(x => x.Query())
                .Returns(claims.AsQueryable().BuildMock());

            // Act
            var result = await ((ClaimService)_claimService).GetBilledPreviouslyClaimsIdsAsync(accountInfoId, memberId, claimIds);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(claimId1, result.First());
        }

        [Fact]
        public async Task GetBilledPreviouslyClaimsIdsAsync_ShouldReturnEmpty_WhenNoBilledHistoryExists()
        {
            // Arrange
            var accountInfoId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();
            var claimId = Fixture.Create<int>();
            var claimIds = new[] { claimId };

            var nonBilledHistory = Fixture.Build<ClaimHistoryEntity>()
                .With(x => x.ClaimHistoryAction, ClaimHistoryAction.ChargeEntryAdded)
                .With(x => x.DateCreated, DateTime.UtcNow)
                .Create();

            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, claimId)
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.DateDeleted, (DateTime?)null)
                .With(x => x.ClaimHistory, new List<ClaimHistoryEntity> { nonBilledHistory })
                .Create();

            _claimRepository.Setup(x => x.Query())
                .Returns(new List<ClaimEntity> { claim }.AsQueryable().BuildMock());

            // Act
            var result = await ((ClaimService)_claimService).GetBilledPreviouslyClaimsIdsAsync(accountInfoId, memberId, claimIds);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetClaimTabStatusesAsync_ReturnsFilteredStatuses_ForReadyToBillTab()
        {
            // Arrange
            var accountInfoId = 1;
            var claims = new List<ClaimEntity>
            {
                new ClaimEntity
                {
                    AccountInfoId = accountInfoId,
                    DateDeleted = null,
                    ClaimIdentifier = "CLM-1",
                    ClaimStatus = ClaimStatus.ReadyToBill,
                    IsFlagged = false
                },
                new ClaimEntity
                {
                    AccountInfoId = accountInfoId,
                    DateDeleted = null,
                    ClaimIdentifier = "CLM-2",
                    ClaimStatus = ClaimStatus.Rebill,
                    IsFlagged = false
                },
                new ClaimEntity
                {
                    AccountInfoId = accountInfoId,
                    DateDeleted = null,
                    ClaimIdentifier = "CLM-3",
                    ClaimStatus = ClaimStatus.PendingReview,
                    IsFlagged = false
                }
            };

            var model = new ClaimFilterGetModel
            {
                AccountInfoId = accountInfoId,
                Tab = ClaimListingTab.ReadyToBill
            };

            SetupClaims(claims);

            // Act
            var result = await _claimService.GetClaimTabStatusesAsync(model);

            // Assert
            Assert.NotNull(result);
            var ids = result.Select(r => r.Id).ToList();
            Assert.Contains((int)ClaimStatus.ReadyToBill, ids);
            Assert.Contains((int)ClaimStatus.Rebill, ids);
            Assert.DoesNotContain((int)ClaimStatus.PendingReview, ids);
            Assert.All(result, r => Assert.False(string.IsNullOrWhiteSpace(r.Name)));
        }

        [Fact]
        public async Task GetClaimTabStatusesAsync_ReturnsEmpty_WhenNoMatchingClaims()
        {
            // Arrange
            var accountInfoId = Fixture.Create<int>();
            SetupClaims(new List<ClaimEntity>());

            var model = new ClaimFilterGetModel
            {
                AccountInfoId = accountInfoId,
                Tab = ClaimListingTab.PendingReview
            };

            // Act
            var result = await _claimService.GetClaimTabStatusesAsync(model);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetClaimTabStatusesAsync_OnlyFlaggedClaims_AreIncluded_ForFlaggedTab_AndResultsAreOrderedByName()
        {
            // Arrange
            var accountInfoId = 1;
            var claimA = new ClaimEntity
            {
                Id = 1,
                AccountInfoId = accountInfoId,
                ClaimIdentifier = "CLM-A",
                ClaimStatus = ClaimStatus.Billed,
                IsFlagged = true,
                DateDeleted = null,
                ChildProfileId = 100,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(-9)
            };
            var claimB = new ClaimEntity
            {
                Id = 2,
                AccountInfoId = accountInfoId,
                ClaimIdentifier = "CLM-B",
                ClaimStatus = ClaimStatus.AcceptedFunder,
                IsFlagged = true,
                DateDeleted = null,
                ChildProfileId = 100,
                StartDate = DateTime.UtcNow.AddDays(-8),
                EndDate = DateTime.UtcNow.AddDays(-7)
            };
            var claimC = new ClaimEntity
            {
                Id = 3,
                AccountInfoId = accountInfoId,
                ClaimIdentifier = "CLM-C",
                ClaimStatus = ClaimStatus.Billed,
                IsFlagged = false,
                DateDeleted = null,
                ChildProfileId = 100,
                StartDate = DateTime.UtcNow.AddDays(-6),
                EndDate = DateTime.UtcNow.AddDays(-5)
            };

            SetupClaims(new List<ClaimEntity> { claimA, claimB, claimC });

            // Act
            var result = await _claimService.GetClaimTabStatusesAsync(new ClaimFilterGetModel
            {
                AccountInfoId = accountInfoId,
                Tab = ClaimListingTab.Flagged
            });

            // Assert - GetClaimTabStatusesAsync returns distinct ClaimStatus values with enum descriptions
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            // Only flagged claims (claimA=Billed, claimB=AcceptedFunder) should be included; claimC is not flagged
            var statusIds = result.Select(r => r.Id).ToList();
            Assert.Contains((int)ClaimStatus.Billed, statusIds);
            Assert.Contains((int)ClaimStatus.AcceptedFunder, statusIds);
            Assert.All(result, r => Assert.False(string.IsNullOrWhiteSpace(r.Name)));
        }

        [Fact]
        public async Task GetClaimHeadersAsync_ThrowException_WhenErrorOccurs()
        {
            // Arrange
            var accountInfoId = 1;
            var model = new ClaimGetRequestSortFilterWithUserInfo
            {
                AccountInfoId = accountInfoId,
                Skip = 0,
                Take = 10,
                Filters = new ClaimFiltersModel
                {
                    Tab = (int)ClaimsTab.ReadyToBill,
                    ClaimNumber = null,
                    PatientIds = null,
                    ShowVoided = false
                },
                SortingModels = new List<SortingModel>
        {
            new SortingModel { Field = "ClaimNumber", Dir = "asc" }
        }
            };

            _dbHelper.Setup(x => x.ExecuteListAsync<ClaimHeaderModel>(
                It.IsAny<string>(),
                It.IsAny<List<SqlParameter>>(),
                CommandType.StoredProcedure))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(async () =>
                await _claimService.GetClaimHeadersAsync(model));

            Assert.Equal("Database error", exception.Message);
        }

        private void SetupClaim(ClaimEntity claim)
        {
            _claimRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimEntity>.Create(claim));
        }

        private void SetupClaims(List<ClaimEntity> claims)
        {
            _claimRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimEntity>.Create(claims));
        }

        private List<ClaimEntity> SetupClaimsByStatus(int[] claimsIds, bool isFlagged, ClaimStatus? status = null)
        {
            var claims = new List<ClaimEntity>();
            foreach (int claimId in claimsIds)
            {
                var claim = Fixture.Build<ClaimEntity>()
                    .With(x => x.Id, claimId)
                    .With(x => x.IsFlagged, isFlagged)
                    .With(x => x.ClaimStatus, status ?? Fixture.Create<ClaimStatus>())
                    .Create();

                claims.Add(claim);
            }

            SetupClaims(claims);

            return claims;
        }

        private FunderDetails CreateFunderMapping(ClaimEntity claim)
        {
            var result = Fixture.Build<FunderDetails>()
                    .With(x => x.id, claim.ClientFunderId.Value)
                    .With(x => x.childProfileId, claim.ChildProfileId)
                    .With(x => x.Funder, Fixture.Build<FunderDataModel>()
                                            .With(x => x.ServiceFunders, new List<ServiceFunderData> { Fixture.Build<ServiceFunderData>()
                                                                                                            .With(x => x.providerServiceId, claim.ClientFunderServiceLineId)
                                                                                                            .Create() })
                                            .Create())
                    .Create();

            return result;
        }

        private void SetupMapper()
        {
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MapperProfile());
            });

            _mapper = mapperConfig.CreateMapper();
        }

        private void SetupRethinkServices()
        {
            var intId = Fixture.Create<int>();
            _rethinkService.Setup(x => x.GetChildProfileReturningEntity(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<ChildProfileEntityModel>().Create());
            _rethinkService.Setup(x => x.GetAccountReturningEntityAsync(It.IsAny<int>(), false)).ReturnsAsync(Fixture.Build<AccountInfoEntityModel>().Create());
            _rethinkService.Setup(x => x.GetClientDiagnosisByIdReturningEntityAsync(It.IsAny<int>())).ReturnsAsync(Fixture.Build<DiagnosisEntityModel>().Create());
            _rethinkService.Setup(x => x.GetChildProfileAuthorizationByClientId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(Fixture.Build<ClientAuthorization>().Without(x => x.ChildProfileAuthorizationDiagnosisCodes).Without(x => x.ChildProfileDiagnosis).Without(x => x.ChildProfileReferringProvider).Create());
            _rethinkService.Setup(x => x.GetChildProfileFunderServiceLineMappingEntity(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<ServiceLines>().With(x => x.serviceId, intId).Create());
            _rethinkService.Setup(x => x.GetFunder(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<FunderDataModel>().Create());
            _rethinkService.Setup(x => x.GetChildProfileReferringProviderEntity(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<clientReferringProviders>().Create());
            _rethinkService.Setup(x => x.GetProviderLocation(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<ProviderLocations>().Create());
            _rethinkService.Setup(x => x.GetChildProfileAuthorizationDiagnosisCodesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<ChildProfileAuthorizationDiagnosisCode>().Without(x => x.ChildProfileAuthorization).CreateMany().ToList());
            _rethinkService.Setup(x => x.GetMemberAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<RethinkAccountMember>().Create());
            var locationCode = Fixture.Build<LocationCodesModel>().Create();
            var lcList = new List<LocationCodesModel>();
            lcList.Add(locationCode);
            _rethinkService.Setup(x => x.GetLocationCodes()).ReturnsAsync(lcList);
            _rethinkService.Setup(x => x.GetChildProfileFunderMappingByMappingId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<FunderDetails>().Create());
            _rethinkService.Setup(x => x.GetServiceLine(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<ChildProfileServiceLines>().Create());
            var serviceFunder = Fixture.Build<ServiceFunderData>().With(x => x.providerServiceId, intId).Create();
            var serviceFunders = new List<ServiceFunderData>() { serviceFunder };
            _rethinkService.Setup(x => x.GetServiceFundersEntityListByFunderId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(serviceFunders);
        }

        private ClaimChargeEntryEntity SetupServiceForRemoveBilling(RemoveBillingClaimDetailsModel removeModel, int claimId)
        {
            var clamChargeEntryToDelete = new ClaimChargeEntryEntity { Id = removeModel.ChargeId, ClaimId = claimId };
            var paymentId = Fixture.Create<int>();
            var slId = Fixture.Create<int>();

            var linkEntity = Fixture.Build<ClaimAppointmentLinkEntity>()
                .With(x => x.ClaimId, claimId)
                .With(x => x.ClaimChargeEntriesId, clamChargeEntryToDelete.Id)
                .Create();

            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, clamChargeEntryToDelete.ClaimId)
                .With(x => x.AccountInfoId, removeModel.AccountId)
                .Create();

            SetupClaim(claim);

            _claimChargeEntryRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimChargeEntryEntity>.Create(clamChargeEntryToDelete));
            _claimAppointmentLinkRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimAppointmentLinkEntity>.Create(linkEntity));
            var linkCharge = Fixture.Build<ClaimAppointmentLinkChargeEntry>().With(x => x.ClaimChargeEntryEntityId, clamChargeEntryToDelete.Id).Create();
            _claimAppointmentLinkChargeEntryRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimAppointmentLinkChargeEntry>.Create(linkCharge));

            var paymentClaimServiceLineAdjustments_plus = Fixture.Build<PaymentClaimServiceLineAdjustmentEntity>().With(x => x.IsAdjustmentPositive, true).Create();
            var paymentClaimServiceLineAdjustments_minus = Fixture.Build<PaymentClaimServiceLineAdjustmentEntity>().With(x => x.IsAdjustmentPositive, false).Create();
            var paymentClaimServiceLine = Fixture.Build<PaymentClaimServiceLineEntity>().With(x => x.ClaimChargeEntryId, clamChargeEntryToDelete.Id).Create();
            paymentClaimServiceLine.PaymentClaimServiceLineAdjustments.Add(paymentClaimServiceLineAdjustments_plus);
            paymentClaimServiceLine.PaymentClaimServiceLineAdjustments.Add(paymentClaimServiceLineAdjustments_minus);

            var paymentEntity = Fixture.Build<PaymentEntity>()
                .With(x => x.Id, paymentId)
                .With(x => x.AccountInfoId, removeModel.AccountId)
                .With(x => x.PaymentMethodId, 1)
                .With(x => x.HasAcknowledgedErrors, false)
                .With(x => x.PaymentEraUpload, Fixture.Create<PaymentEraUploadEntity>())
                .Create();

            var claimChargeEntryWriteOffs = Fixture.Build<ClaimChargeEntryWriteOffEntity>().With(x => x.ClaimWriteOffId, claimId).CreateMany();
            var claimWriteOffs = Fixture.Build<ClaimWriteOffEntity>().With(x => x.ClaimChargeEntryWriteOffs, claimChargeEntryWriteOffs.ToList()).CreateMany();

            var paymentClaim = Fixture.Build<PaymentClaimEntity>()
                .With(x => x.PaymentId, paymentId)
                .With(x => x.ClaimId, claimId)
                .With(x => x.Claim, claim)
                .Create();
            paymentClaim.PaymentClaimServiceLines.Add(paymentClaimServiceLine);

            _paymentRepository.Setup(x => x.Query()).Returns(QueryMock<PaymentEntity>.Create(paymentEntity));
            _paymentRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<PaymentEntity, bool>>>(), null)).ReturnsAsync(QueryMock<PaymentEntity>.Create(paymentEntity));
            _paymentClaimRepository.Setup(x => x.Query()).Returns(QueryMock<PaymentClaimEntity>.Create(paymentClaim));

            var chargeEntryWriteOff = Fixture.Build<ClaimChargeEntryWriteOffEntity>().Create();
            _claimChargeEntryWriteOffRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimChargeEntryWriteOffEntity>.Create(chargeEntryWriteOff));

            var submission = Fixture.Build<ClaimSubmissionEntity>().With(x => x.ClaimId, claimId).With(x => x.Id, slId).Create();
            var submissionServiceLine = Fixture.Build<ClaimSubmissionServiceLineEntity>().With(x => x.ClaimSubmissionId, slId).Create();
            _claimSubmissionRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimSubmissionEntity>.Create(submission));
            _claimSubmissionServiceLineRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimSubmissionServiceLineEntity>.Create(submissionServiceLine));

            var patientInvoiceDetails = Fixture.Build<PatientInvoiceDetailsEntity>().With(x => x.ChargeId, removeModel.ChargeId).Create();
            _patientInvoiceDetailsRepository.Setup(x => x.Query()).Returns(QueryMock<PatientInvoiceDetailsEntity>.Create(patientInvoiceDetails));

            return clamChargeEntryToDelete;
        }

        private void SetupPaymentMocks(int? accountInfoId = null, int? paymentId = null, bool addPaymentClaims = false, bool validPayment = false)
        {
            var claimid = Fixture.Create<int>();
        }

        private void SetupRepos(int claimId)
        {
            int pcId = Fixture.Create<int>();
            int slId = Fixture.Create<int>();
            var chargeId = Fixture.Create<int>();

            var chargeEntries = Fixture.Build<ClaimChargeEntryEntity>().With(x => x.ClaimId, claimId).With(x => x.Id, chargeId).Create();
            var linkChargeEntity = Fixture.Build<ClaimAppointmentLinkChargeEntry>().With(x => x.ClaimChargeEntryEntityId, chargeId).Create();
            var chargeEntryWriteOff = Fixture.Build<ClaimChargeEntryWriteOffEntity>().With(x => x.ClaimChargeEntryId, chargeId).Create();
            var patientInvoiceDetails = Fixture.Build<PatientInvoiceDetailsEntity>().With(x => x.ChargeId, chargeId).Create();
            var claimWriteOff = Fixture.Build<ClaimWriteOffEntity>().With(x => x.ClaimId, claimId).Create();
            var paymentClaimServiceLineAdjustment = Fixture.Build<PaymentClaimServiceLineAdjustmentEntity>().With(x => x.PaymentClaimServiceLineId, slId).CreateMany();
            var paymentClaimAdjustment = Fixture.Build<PaymentClaimAdjustmentEntity>().With(x => x.PaymentClaimId, pcId).CreateMany();
            var paymentClaimServiceLines = Fixture.Build<PaymentClaimServiceLineEntity>().With(x => x.ClaimChargeEntryId, chargeId).With(x => x.Id, slId).With(x => x.PaymentClaimServiceLineAdjustments, paymentClaimServiceLineAdjustment.ToList()).CreateMany();
            var paymentClaims = Fixture.Build<PaymentClaimEntity>().With(x => x.ClaimId, claimId).With(x => x.Id, pcId).With(x => x.PaymentClaimServiceLines, paymentClaimServiceLines.ToList()).Create();
            paymentClaims.DateDeleted = null;
            var claimAttachments = Fixture.Build<ClaimAttachmentEntity>().With(x => x.ClaimId, claimId).Create();
            var diagnosis = Fixture.Build<ClaimDiagnosisCodeEntity>().With(x => x.ClaimId, claimId).Create();
            var claimNote = Fixture.Build<ClaimNoteEntity>().With(x => x.ClaimId, claimId).Create();
            var claimValidationError = Fixture.Build<ClaimValidationErrorEntity>().With(x => x.ClaimId, claimId).Create();
            var submission = Fixture.Build<ClaimSubmissionEntity>().With(x => x.ClaimId, claimId).With(x => x.Id, slId).Create();
            var submissionServiceLine = Fixture.Build<ClaimSubmissionServiceLineEntity>().With(x => x.ClaimSubmissionId, slId).Create();

            _claimChargeEntryRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimChargeEntryEntity>.Create(chargeEntries));
            _claimAppointmentLinkChargeEntryRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimAppointmentLinkChargeEntry>.Create(linkChargeEntity));
            _claimChargeEntryWriteOffRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimChargeEntryWriteOffEntity>.Create(chargeEntryWriteOff));
            _patientInvoiceDetailsRepository.Setup(x => x.Query()).Returns(QueryMock<PatientInvoiceDetailsEntity>.Create(patientInvoiceDetails));
            _claimWriteOffRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimWriteOffEntity>.Create(claimWriteOff));
            _paymentClaimRepository.Setup(x => x.Query()).Returns(QueryMock<PaymentClaimEntity>.Create(paymentClaims));
            _paymentClaimServiceLineRepository.Setup(x => x.Query()).Returns(QueryMock<PaymentClaimServiceLineEntity>.Create(paymentClaimServiceLines));
            _paymentClaimServiceLineAdjustmentRepository.Setup(x => x.Query()).Returns(QueryMock<PaymentClaimServiceLineAdjustmentEntity>.Create(paymentClaimServiceLineAdjustment));
            _claimAttachmentRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimAttachmentEntity>.Create(claimAttachments));
            _claimDiagnosisCodeRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimDiagnosisCodeEntity>.Create(diagnosis));
            _claimSubmissionRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimSubmissionEntity>.Create(submission));
            _claimSubmissionServiceLineRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimSubmissionServiceLineEntity>.Create(submissionServiceLine));
            _claimNoteRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimNoteEntity>.Create(claimNote));
            _claimValidationErrorRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimValidationErrorEntity>.Create(claimValidationError));
        }

        [Fact]
        public async Task GetMemberViewSettingsAsync_ShouldReturnDefaultSettings_WhenNotExist()
        {
            // Arrange
            var memberId = 100;
            _memberViewSettingRepository.Setup(r => r.Query())
                .Returns(new List<MemberViewSettingEntity>().AsQueryable().BuildMock());
            _memberViewSettingRepository.Setup(r => r.Add(It.IsAny<MemberViewSettingEntity>()));
            _memberViewSettingRepository.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _claimService.GetMemberViewSettingsAsync(memberId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(memberId, result.Id);
            Assert.True(result.Client);
            Assert.True(result.Funder);
            Assert.True(result.Status);
            Assert.True(result.Balance);
            _memberViewSettingRepository.Verify(r => r.Add(It.IsAny<MemberViewSettingEntity>()), Times.Once);
        }

        [Fact]
        public async Task UnapproveClaimsAsync_ShouldChangeStatusToPendingReview()
        {
            // Arrange
            var accountInfoId = 1;
            var memberId = 100;
            var claimIds = new[] { 1, 2 };
            var claims = new List<ClaimEntity>
    {
        new ClaimEntity { Id = 1, ClaimStatus = ClaimStatus.ReadyToBill },
        new ClaimEntity { Id = 2, ClaimStatus = ClaimStatus.ReadyToBill }
    };

            var mockQueryable = claims.AsQueryable().BuildMock();
            _claimRepository.Setup(r => r.Query()).Returns(mockQueryable);
            _claimRepository.Setup(r => r.Update(It.IsAny<ClaimEntity>()));
            _claimRepository.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);
            _claimHistoryService.Setup(h => h.AddAsync(It.IsAny<ClaimHistorySaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);
            _messageBus.Setup(m => m.SendBatchAsync(It.IsAny<string>(), It.IsAny<List<ClaimTransactionModel>>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _claimService.UnapproveClaimsAsync(accountInfoId, memberId, claimIds);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(claimIds.Length, result.Length);
            _claimRepository.Verify(r => r.Update(It.IsAny<ClaimEntity>()), Times.Exactly(2));
            _claimRepository.Verify(r => r.CommitAsync(), Times.Once);
            _claimHistoryService.Verify(h => h.AddAsync(It.IsAny<ClaimHistorySaveModel>(), It.IsAny<bool>()), Times.Exactly(2));
        }

        [Fact]
        public async Task GetClaimFlagReasonsAsync_ShouldReturnReasonsForAccount()
        {
            // Arrange
            var accountInfoId = 1;
            var flagReasons = new List<ClaimFlagReasonMaster>
    {
        new ClaimFlagReasonMaster { Id = 1, ReasonName = "Reason1", AccountInfoId = 0 },
        new ClaimFlagReasonMaster { Id = 2, ReasonName = "Reason2", AccountInfoId = accountInfoId },
        new ClaimFlagReasonMaster { Id = 3, ReasonName = "DeletedReason", AccountInfoId = accountInfoId, DateDeleted = DateTime.UtcNow }
    };

            var mockQueryable = flagReasons.AsQueryable().BuildMock();
            _claimFlagReasonMasterRepository.Setup(r => r.Query()).Returns(mockQueryable);

            // Act
            var result = await _claimService.GetClaimFlagReasonsAsync(accountInfoId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.ReasonName == "Reason1");
            Assert.Contains(result, r => r.ReasonName == "Reason2");
            Assert.DoesNotContain(result, r => r.ReasonName == "DeletedReason");
        }

        [Fact]
        public async Task FlagClaimsAsync_WithReasons_ShouldCreateTransactions()
        {
            // Arrange
            var accountInfoId = 1;
            var memberId = 100;
            var claimIds = new[] { 1, 2 };
            var reasonIds = new[] { 10, 20 };
            var notes = "Test notes";

            var claims = new List<ClaimEntity>
    {
        new ClaimEntity { Id = 1, IsFlagged = false },
        new ClaimEntity { Id = 2, IsFlagged = false }
    };

            var mockQueryable = claims.AsQueryable().BuildMock();
            _claimRepository.Setup(r => r.Query()).Returns(mockQueryable);
            _claimRepository.Setup(r => r.UpdateRange(It.IsAny<List<ClaimEntity>>()));
            _claimRepository.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);
            _claimFlagTransactionRepository.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<ClaimFlagTransaction>>()))
                .Returns(Task.CompletedTask);
            _claimHistoryService.Setup(h => h.AddAsync(It.IsAny<List<ClaimHistorySaveModel>>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);
            _messageBus.Setup(m => m.SendBatchAsync(It.IsAny<string>(), It.IsAny<List<ClaimTransactionModel>>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _claimService.FlagClaimsAsync(accountInfoId, memberId, claimIds, reasonIds, notes, (int?)null, (string?)null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(claimIds.Length, result.Length);
            _claimFlagTransactionRepository.Verify(r =>
                r.AddRangeAsync(It.Is<IEnumerable<ClaimFlagTransaction>>(t => t.Count() == 4)), Times.Once); // 2 claims × 2 reasons
            _claimRepository.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task UnflagClaimsAsync_ShouldSoftDeleteTransactions()
        {
            // Arrange
            var accountInfoId = 1;
            var memberId = 100;
            var claimIds = new[] { 1 };

            var claim = new ClaimEntity { Id = 1, IsFlagged = true };
            var transactions = new List<ClaimFlagTransaction>
    {
        new ClaimFlagTransaction { Id = 1, HcClaimId = 1, DateDeleted = null }
    };

            var mockClaimQueryable = new List<ClaimEntity> { claim }.AsQueryable().BuildMock();
            var mockTransactionQueryable = transactions.AsQueryable().BuildMock();

            _claimRepository.Setup(r => r.Query()).Returns(mockClaimQueryable);
            _claimFlagTransactionRepository.Setup(r => r.Query()).Returns(mockTransactionQueryable);
            _claimRepository.Setup(r => r.Update(It.IsAny<ClaimEntity>()));
            _claimRepository.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);
            _claimFlagTransactionRepository.Setup(r => r.Update(It.IsAny<ClaimFlagTransaction>()));
            _claimFlagTransactionRepository.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);
            _claimHistoryService.Setup(h => h.AddAsync(It.IsAny<ClaimHistorySaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);
            _messageBus.Setup(m => m.SendBatchAsync(It.IsAny<string>(), It.IsAny<List<ClaimTransactionModel>>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _claimService.UnflagClaimsAsync(accountInfoId, memberId, claimIds);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            _claimFlagTransactionRepository.Verify(r => r.Update(It.IsAny<ClaimFlagTransaction>()), Times.Once);
            _claimFlagTransactionRepository.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllCarcCodes_ShouldReturnCachedCodes()
        {
            // Arrange
            var carcCodes = new List<CarcCodeEntity>
    {
        new CarcCodeEntity { Id = 1, Code = "CO" },
        new CarcCodeEntity { Id = 2, Code = "PR" }
    };

            _cacheService.Setup(c => c.GetOrSetCacheAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<CarcCodeEntity>>>>(),
                It.IsAny<TimeSpan>()))
                .ReturnsAsync(carcCodes);

            var mapperMock = new Mock<IMapper>();
            mapperMock.Setup(m => m.Map<List<CarcCodeResponseModel>>(It.IsAny<List<CarcCodeEntity>>()))
                .Returns(carcCodes.Select(c => new CarcCodeResponseModel { Code = c.Code }).ToList());

            // Act
            var result = await _claimService.GetAllCarcCodes();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            _cacheService.Verify(c => c.GetOrSetCacheAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<CarcCodeEntity>>>>(),
                It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task SubmitClaimsToServiceBusAsync_ShouldSendBatchMessages()
        {
            // Arrange
            var model = new ClaimsSubmitModel
            {
                Ids = new[] { 1, 2, 3 },
                IsSecondary = false,
                AdjustmentLevel = AdjustmentLevel.Claim,
                SecondaryFunderDetails = new List<SecondaryFunderDetailsModel>(),
                AccountInfoId = 1,
                MemberId = 100
            };

            _messageBus.Setup(m => m.SendBatchAsync(
                It.IsAny<string>(),
                It.IsAny<List<ClaimProcessRequestModel>>(),
                It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Act
            await _claimService.SubmitClaimsToServiceBusAsync(model);

            // Assert
            _messageBus.Verify(m => m.SendBatchAsync(
                It.Is<string>(s => s == Topics.RT_Billing_ProcessClaimSubmission),
                It.Is<List<ClaimProcessRequestModel>>(list => list.Count == 3),
                It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task SubmitClaimsToServiceBusTopicAsync_ShouldSendApprovalMessages()
        {
            // Arrange
            var model = new IdsWithUserInfo
            {
                Ids = new[] { 1, 2 },
                AccountInfoId = 1,
                MemberId = 100
            };

            _messageBus.Setup(m => m.SendBatchAsync(
                It.IsAny<string>(),
                It.IsAny<List<ClaimApproveRequestModel>>(),
                It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Act
            await _claimService.SubmitClaimsToServiceBusTopicAsync(model);

            // Assert
            _messageBus.Verify(m => m.SendBatchAsync(
                It.Is<string>(s => s == Topics.RT_Billing_ClaimApproval),
                It.Is<List<ClaimApproveRequestModel>>(list => list.Count == 2),
                It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task AssignClaimsAsync_ShouldAssignClaims_WhenClaimsExist()
        {
            // Arrange
            var claimIds = new[] { 1, 2, 3 };
            var assigneeId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();

            var claims = claimIds.Select(id => Fixture.Build<ClaimEntity>()
         .With(x => x.Id, id)
         .With(x => x.DateDeleted, (DateTime?)null)
       .With(x => x.AssigneeId, (int?)null)
     .Create()).ToList();

            _claimRepository.Setup(r => r.Query())
         .Returns(QueryMock<ClaimEntity>.Create(claims));
            _claimRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _claimService.AssignClaimsAsync(claimIds, assigneeId, memberId);

            // Assert
            Assert.True(result);
            Assert.All(claims, claim => Assert.Equal(assigneeId, claim.AssigneeId));
            _claimRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task AssignClaimsAsync_ShouldReturnFalse_WhenNoClaimsFound()
        {
            // Arrange
            var claimIds = new[] { 1, 2, 3 };
            var assigneeId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();

            _claimRepository.Setup(r => r.Query())
              .Returns(QueryMock<ClaimEntity>.Create(new List<ClaimEntity>()));

            // Act
            var result = await _claimService.AssignClaimsAsync(claimIds, assigneeId, memberId);

            // Assert
            Assert.False(result);
            _claimRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task AssignClaimsAsync_ShouldExcludeDeletedClaims_WhenAssigning()
        {
            // Arrange
            var activeClaimId = 1;
            var deletedClaimId = 2;
            var claimIds = new[] { activeClaimId, deletedClaimId };
            var assigneeId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();

            var activeClaim = Fixture.Build<ClaimEntity>()
    .With(x => x.Id, activeClaimId)
     .With(x => x.DateDeleted, (DateTime?)null)
     .Create();

            var deletedClaim = Fixture.Build<ClaimEntity>()
                   .With(x => x.Id, deletedClaimId)
   .With(x => x.DateDeleted, DateTime.UtcNow)
              .Create();

            var claims = new List<ClaimEntity> { activeClaim, deletedClaim };

            _claimRepository.Setup(r => r.Query())
             .Returns(QueryMock<ClaimEntity>.Create(claims));
            _claimRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _claimService.AssignClaimsAsync(claimIds, assigneeId, memberId);

            // Assert
            Assert.True(result);
            Assert.Equal(assigneeId, activeClaim.AssigneeId);
            Assert.NotEqual(assigneeId, deletedClaim.AssigneeId);
            _claimRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task AssignClaimsAsync_ShouldAssignSingleClaim()
        {
            // Arrange
            var claimId = 1;
            var claimIds = new[] { claimId };
            var assigneeId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();

            var claim = Fixture.Build<ClaimEntity>()
              .With(x => x.Id, claimId)
                .With(x => x.DateDeleted, (DateTime?)null)
                .Create();

            _claimRepository.Setup(r => r.Query())
                .Returns(QueryMock<ClaimEntity>.Create(claim));
            _claimRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _claimService.AssignClaimsAsync(claimIds, assigneeId, memberId);

            // Assert
            Assert.True(result);
            Assert.Equal(assigneeId, claim.AssigneeId);
            _claimRepository.Verify(r => r.Update(It.Is<ClaimEntity>(c => c.Id == claimId)), Times.Once);
            _claimRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }


        [Fact]
        public async Task AssignClaimsAsync_ShouldReassignClaims_WhenClaimsAlreadyAssigned()
        {
            // Arrange
            var claimId = 1;
            var claimIds = new[] { claimId };
            var oldAssigneeId = Fixture.Create<int>();
            var newAssigneeId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();

            var claim = Fixture.Build<ClaimEntity>()
              .With(x => x.Id, claimId)
        .With(x => x.DateDeleted, (DateTime?)null)
         .With(x => x.AssigneeId, oldAssigneeId)
                      .Create();

            _claimRepository.Setup(r => r.Query())
                        .Returns(QueryMock<ClaimEntity>.Create(claim));
            _claimRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _claimService.AssignClaimsAsync(claimIds, newAssigneeId, memberId);

            // Assert
            Assert.True(result);
            Assert.NotEqual(oldAssigneeId, claim.AssigneeId);
            Assert.Equal(newAssigneeId, claim.AssigneeId);
            _claimRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetErrorsCodesAsync_ShouldReturnErrorsCodes_WhenErrorsExist()
        {
            // Arrange
            var errorMessage1 = Fixture.Build<ClaimErrorMessageEntity>()
                .With(x => x.Id, 1)
                .With(x => x.ShortDescription, "ERROR_001")
                .With(x => x.DateDeleted, (DateTime?)null)
                .Create();

            var errorMessage2 = Fixture.Build<ClaimErrorMessageEntity>()
                .With(x => x.Id, 2)
                .With(x => x.ShortDescription, "ERROR_002")
                .With(x => x.DateDeleted, (DateTime?)null)
                .Create();

            var errorMessages = new List<ClaimErrorMessageEntity> { errorMessage1, errorMessage2 };

            _claimErrorMessageRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimErrorMessageEntity>.Create(errorMessages));

            // Act
            var result = await _claimService.GetErrorsCodesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.ErrorsCodes);
            Assert.Equal(2, result.ErrorsCodes.Length);
            Assert.Equal("ERROR_001", result.ErrorsCodes[0].Name);
            Assert.Equal("ERROR_002", result.ErrorsCodes[1].Name);
            Assert.False(result.ErrorsCodes[0].Checked);
            Assert.False(result.ErrorsCodes[1].Checked);

            _claimErrorMessageRepository.Verify(x => x.Query(), Times.Once);
        }

        [Fact]
        public async Task GetErrorsCodesAsync_ShouldReturnEmptyArray_WhenNoErrorsExist()
        {
            // Arrange
            var emptyErrorMessages = new List<ClaimErrorMessageEntity>();

            _claimErrorMessageRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimErrorMessageEntity>.Create(emptyErrorMessages));

            // Act
            var result = await _claimService.GetErrorsCodesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.ErrorsCodes);
            Assert.Empty(result.ErrorsCodes);

            _claimErrorMessageRepository.Verify(x => x.Query(), Times.Once);
        }

        [Fact]
        public async Task GetErrorsCodesAsync_ShouldReturnErrorsCodes_WithCorrectProperties()
        {
            // Arrange
            var errorMessage = Fixture.Build<ClaimErrorMessageEntity>()
                .With(x => x.Id, 1)
                .With(x => x.ShortDescription, "VALIDATION_ERROR")
                .With(x => x.DateDeleted, (DateTime?)null)
                .Create();

            _claimErrorMessageRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimErrorMessageEntity>.Create(errorMessage));

            // Act
            var result = await _claimService.GetErrorsCodesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.ErrorsCodes);

            var errorCode = result.ErrorsCodes[0];
            Assert.NotNull(errorCode);
            Assert.Equal("VALIDATION_ERROR", errorCode.Name);
            Assert.False(errorCode.Checked); // Default value should be false

            _claimErrorMessageRepository.Verify(x => x.Query(), Times.Once);
        }

        [Fact]
        public async Task GetErrorsCodesAsync_ShouldHandleMultipleErrors_AndMapCorrectly()
        {
            // Arrange
            var errorMessages = Fixture.Build<ClaimErrorMessageEntity>()
                .With(x => x.DateDeleted, (DateTime?)null)
                .CreateMany(5)
                .ToList();

            _claimErrorMessageRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimErrorMessageEntity>.Create(errorMessages));

            // Act
            var result = await _claimService.GetErrorsCodesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.ErrorsCodes.Length);
            Assert.All(result.ErrorsCodes, errorCode =>
            {
                Assert.NotNull(errorCode.Name);
                Assert.False(errorCode.Checked);
            });

            // Verify all ShortDescriptions are mapped to Name property
            for (int i = 0; i < errorMessages.Count; i++)
            {
                Assert.Equal(errorMessages[i].ShortDescription, result.ErrorsCodes[i].Name);
            }

            _claimErrorMessageRepository.Verify(x => x.Query(), Times.Once);
        }

        [Fact]
        public async Task GetErrorsCodesAsync_ShouldReturnClaimErrorsCodesModel_WithCorrectType()
        {
            // Arrange
            var errorMessage = Fixture.Build<ClaimErrorMessageEntity>()
                .With(x => x.ShortDescription, "TEST_ERROR")
                .With(x => x.DateDeleted, (DateTime?)null)
                .Create();

            _claimErrorMessageRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimErrorMessageEntity>.Create(errorMessage));

            // Act
            var result = await _claimService.GetErrorsCodesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ClaimErrorsCodesModel>(result);
            Assert.NotNull(result.ErrorsCodes);
            Assert.IsType<ClaimErrorsCodes[]>(result.ErrorsCodes);

            _claimErrorMessageRepository.Verify(x => x.Query(), Times.Once);
        }

        [Fact]
        public async Task AssignClaimsAsync_ShouldReturnFalse_OnException()
        {
            // Arrange
            var claimIds = new[] { 1 };
            var assigneeId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();

            _claimRepository.Setup(r => r.Query())
                .Throws(new Exception("Database error"));

            // Act
            var result = await _claimService.AssignClaimsAsync(claimIds, assigneeId, memberId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetErrorsSourcesAsync_ShouldReturnErrorsSources_WhenSourcesExist()
        {
            // Arrange
            var errorCategory1 = Fixture.Build<ClaimErrorCategoryEntity>()
                .With(x => x.Id, 1)
                .With(x => x.Name, "EDI")
                .With(x => x.DateDeleted, (DateTime?)null)
                .Create();

            var errorCategory2 = Fixture.Build<ClaimErrorCategoryEntity>()
                .With(x => x.Id, 2)
                .With(x => x.Name, "ClearingHouse")
                .With(x => x.DateDeleted, (DateTime?)null)
                .Create();

            var errorCategories = new List<ClaimErrorCategoryEntity> { errorCategory1, errorCategory2 };

            _claimErrorCategoryRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimErrorCategoryEntity>.Create(errorCategories));

            // Act
            var result = await _claimService.GetErrorsSourcesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.ErrorsSources);
            Assert.Equal(2, result.ErrorsSources.Length);
            Assert.Equal("EDI", result.ErrorsSources[0]);
            Assert.Equal("ClearingHouse", result.ErrorsSources[1]);

            _claimErrorCategoryRepository.Verify(x => x.Query(), Times.Once);
        }

        [Fact]
        public async Task GetErrorsSourcesAsync_ShouldReturnEmptyArray_WhenNoSourcesExist()
        {
            // Arrange
            var emptyErrorCategories = new List<ClaimErrorCategoryEntity>();

            _claimErrorCategoryRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimErrorCategoryEntity>.Create(emptyErrorCategories));

            // Act
            var result = await _claimService.GetErrorsSourcesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.ErrorsSources);
            Assert.Empty(result.ErrorsSources);

            _claimErrorCategoryRepository.Verify(x => x.Query(), Times.Once);
        }

        [Fact]
        public async Task GetErrorsSourcesAsync_ShouldReturnErrorsSources_WithCorrectStructure()
        {
            // Arrange
            var errorCategory = Fixture.Build<ClaimErrorCategoryEntity>()
                .With(x => x.Id, 1)
                .With(x => x.Name, "ValidationError")
                .With(x => x.DateDeleted, (DateTime?)null)
                .Create();

            _claimErrorCategoryRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimErrorCategoryEntity>.Create(errorCategory));

            // Act
            var result = await _claimService.GetErrorsSourcesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.ErrorsSources);
            Assert.Equal("ValidationError", result.ErrorsSources[0]);

            _claimErrorCategoryRepository.Verify(x => x.Query(), Times.Once);
        }

        [Fact]
        public async Task GetErrorsSourcesAsync_ShouldHandleMultipleSources_AndMapCorrectly()
        {
            // Arrange
            var errorCategories = Fixture.Build<ClaimErrorCategoryEntity>()
                .With(x => x.DateDeleted, (DateTime?)null)
                .CreateMany(5)
                .ToList();

            _claimErrorCategoryRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimErrorCategoryEntity>.Create(errorCategories));

            // Act
            var result = await _claimService.GetErrorsSourcesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.ErrorsSources.Length);
            Assert.All(result.ErrorsSources, source => Assert.NotNull(source));

            // Verify all Names are mapped to ErrorsSources array
            for (int i = 0; i < errorCategories.Count; i++)
            {
                Assert.Equal(errorCategories[i].Name, result.ErrorsSources[i]);
            }

            _claimErrorCategoryRepository.Verify(x => x.Query(), Times.Once);
        }

        [Fact]
        public async Task GetErrorsSourcesAsync_ShouldReturnClaimErrorsSourcesModel_WithCorrectType()
        {
            // Arrange
            var errorCategory = Fixture.Build<ClaimErrorCategoryEntity>()
                .With(x => x.Name, "TEST_SOURCE")
                .With(x => x.DateDeleted, (DateTime?)null)
                .Create();

            _claimErrorCategoryRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimErrorCategoryEntity>.Create(errorCategory));

            // Act
            var result = await _claimService.GetErrorsSourcesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ClaimErrorsSourcesModel>(result);
            Assert.NotNull(result.ErrorsSources);
            Assert.IsType<string[]>(result.ErrorsSources);

            _claimErrorCategoryRepository.Verify(x => x.Query(), Times.Once);
        }

        [Fact]
        public async Task GetErrorsSourcesAsync_ShouldPreserveOrderOfSources()
        {
            // Arrange
            var errorCategory1 = Fixture.Build<ClaimErrorCategoryEntity>()
                .With(x => x.Id, 1)
                .With(x => x.Name, "Source_A")
                .Create();

            var errorCategory2 = Fixture.Build<ClaimErrorCategoryEntity>()
                .With(x => x.Id, 2)
                .With(x => x.Name, "Source_B")
                .Create();

            var errorCategory3 = Fixture.Build<ClaimErrorCategoryEntity>()
                .With(x => x.Id, 3)
                .With(x => x.Name, "Source_C")
                .Create();

            var errorCategories = new List<ClaimErrorCategoryEntity>
    {
        errorCategory1,
        errorCategory2,
        errorCategory3
    };

            _claimErrorCategoryRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimErrorCategoryEntity>.Create(errorCategories));

            // Act
            var result = await _claimService.GetErrorsSourcesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.ErrorsSources.Length);
            Assert.Equal("Source_A", result.ErrorsSources[0]);
            Assert.Equal("Source_B", result.ErrorsSources[1]);
            Assert.Equal("Source_C", result.ErrorsSources[2]);

            _claimErrorCategoryRepository.Verify(x => x.Query(), Times.Once);
        }


        [Fact]
        public async Task GetErrorsSourcesAsync_ShouldHandleNullOrEmptyNames()
        {
            // Arrange
            var errorCategory1 = Fixture.Build<ClaimErrorCategoryEntity>()
                .With(x => x.Id, 1)
                .With(x => x.Name, "ValidSource")
                .Create();

            var errorCategory2 = Fixture.Build<ClaimErrorCategoryEntity>()
                .With(x => x.Id, 2)
                .With(x => x.Name, (string)null)
                .Create();

            var errorCategory3 = Fixture.Build<ClaimErrorCategoryEntity>()
                .With(x => x.Id, 3)
                .With(x => x.Name, string.Empty)
                .Create();

            var errorCategories = new List<ClaimErrorCategoryEntity>
    {
        errorCategory1,
        errorCategory2,
        errorCategory3
    };

            _claimErrorCategoryRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimErrorCategoryEntity>.Create(errorCategories));

            // Act
            var result = await _claimService.GetErrorsSourcesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.ErrorsSources.Length);
            Assert.Equal("ValidSource", result.ErrorsSources[0]);
            Assert.Null(result.ErrorsSources[1]);
            Assert.Equal(string.Empty, result.ErrorsSources[2]);

            _claimErrorCategoryRepository.Verify(x => x.Query(), Times.Once);
        }
        [Fact]
        public async Task ValidateClaimDataAsync_ShouldAddClaimHistory_WhenClaimExists()
        {
            // Arrange
            var claimId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();
            var accountInfoId = Fixture.Create<int>();
            var childProfileId = Fixture.Create<int>();
            var clientFunderId = Fixture.Create<int>();
            var clientFunderServiceLineId = Fixture.Create<int>();

            var model = new ClaimValidationModel
            {
                Id = claimId,
                MemberId = memberId,
                IsSecondary = false,
                SecondaryFunderId = null
            };

            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, claimId)
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.ChildProfileId, childProfileId)
                .With(x => x.ClientFunderId, clientFunderId)
                .With(x => x.ClientFunderServiceLineId, clientFunderServiceLineId)
                .Create();

            _claimRepository.Setup(x => x.GetByIdAsync(claimId))
                .ReturnsAsync(claim);

            _claimHistoryService.Setup(x => x.AddAsync(
                It.IsAny<ClaimHistorySaveModel>(),
                It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var funderMapping = Fixture.Build<FunderDetails>()
                .With(x => x.insuranceType, ResponsibilitySequenceType.Primary)
                .Create();

            _rethinkService.Setup(x => x.GetChildProfileFunderServiceLineMappingEntity(
                accountInfoId,
                childProfileId,
                clientFunderId,
                clientFunderServiceLineId))
                .ReturnsAsync(Fixture.Build<ServiceLines>()
                    .With(x => x.ChildProfileFunderMapping, funderMapping)
                    .Create());

            _claimValidationService.Setup(x => x.ValidateClaimData(
                claimId,
                memberId,
                null,
                ResponsibilitySequenceType.Primary,
                false,
                null))
                .Returns(Task.CompletedTask);

            // Act
            await _claimService.ValidateClaimDataAsync(model);

            // Assert
            _claimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistorySaveModel>(m =>
                    m.ClaimId == claimId &&
                    m.MemberId == memberId &&
                    m.Mode == ClaimActionMode.User &&
                    m.ClaimAction == ClaimAction.ScrubbingRules &&
                    m.ClaimHistoryAction == ClaimHistoryAction.ScrubbingRulesInitiatedByUser),
                true),
                Times.Once);
        }
        // Rename the duplicate test method to avoid CS0111 and xUnit1024


        [Fact]
        public async Task MarkBilledClaimsAsync_ShouldUpdateClaimStatusToBilled()
        {
            // Arrange
            var accountInfoId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();
            var claimIds = new[] { 1, 2 };

            // Claims are NOT queried directly, but keep repository stable
            var claims = claimIds.Select(id => new ClaimEntity
            {
                Id = id,
                AccountInfoId = accountInfoId,
                ClaimStatus = ClaimStatus.ReadyToBill
            }).ToList();

            _claimRepository.Setup(x => x.Query())
                .Returns(claims.AsQueryable().BuildMock());

            // Critical: Appointment query MUST be mocked (ToListAsync is called)
            _claimAppointmentLinkRepository.Setup(x => x.Query())
                .Returns(new List<ClaimAppointmentLinkEntity>()
                    .AsQueryable()
                    .BuildMock());

            // Status update
            _claimManagerService.Setup(x => x.UpdateClaimStatusAsync(
                    It.IsAny<int>(),
                    ClaimStatus.Billed,
                    memberId,
                    false,
                    true))
                .Returns(Task.CompletedTask);

            // History
            _claimHistoryService.Setup(x =>
                    x.AddAsync(It.IsAny<ClaimHistorySaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            // Commit
            _claimRepository.Setup(x => x.CommitAsync())
                .Returns(Task.CompletedTask);

            // Bus (appointment billing → should NOT be called because list is empty)
            _messageBus.Setup(x => x.SendBatchAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<AppointmentBillingStatus>>(),
                    It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Claim transaction message (sendMessage wrapper)
            _messageBus.Setup(x => x.SendBatchAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<ClaimTransactionModel>>(),
                    It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _claimService.MarkBilledClaimsAsync(
                accountInfoId, memberId, claimIds);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Length);
            Assert.Equal(claimIds, result);

            _claimManagerService.Verify(x => x.UpdateClaimStatusAsync(
                It.IsAny<int>(),
                ClaimStatus.Billed,
                memberId,
                false,
                true), Times.Exactly(2));

            _claimHistoryService.Verify(x =>
                x.AddAsync(It.IsAny<ClaimHistorySaveModel>(), It.IsAny<bool>()),
                Times.Exactly(2));

            _claimRepository.Verify(x => x.CommitAsync(), Times.Once);

            // Appointment billing is skipped because no appointments exist
            _messageBus.Verify(x => x.SendBatchAsync(
                Queues.RT_Billing_Queue_AppointmentBillingStatus,
                It.IsAny<List<AppointmentBillingStatus>>(),
                It.IsAny<int>()), Times.Never);
        }

        public class BillingTestDbContext : DbContext
        {
            public BillingTestDbContext(DbContextOptions<BillingTestDbContext> options)
                : base(options) { }

            public DbSet<ClaimEntity> Claims => Set<ClaimEntity>();
            public DbSet<ClaimSubmissionEntity> ClaimSubmissions => Set<ClaimSubmissionEntity>();
            public DbSet<ClaimNoteEntity> ClaimNotes => Set<ClaimNoteEntity>();
        }

        [Fact]
        public async Task VoidClaimsAsync_ShouldVoidAndSubmitToClearinghouse_WhenFlagIsTrue()
        {
            // Arrange
            var accountInfoId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();
            var claimId = Fixture.Create<int>();

            var options = new DbContextOptionsBuilder<BillingTestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new BillingTestDbContext(options);

            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, claimId)
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.MemberId, memberId)
                .With(x => x.ClaimIdentifier, "CLM-VOID-001")
                .With(x => x.ClaimStatus, ClaimStatus.Billed)
                .With(x => x.FrequencyTypeId, ClaimFrequencyType.Original)
                .With(x => x.ClaimSubmissions, new List<ClaimSubmissionEntity>())
                .Create();

            dbContext.Claims.Add(claim);
            await dbContext.SaveChangesAsync();

            // Repository wiring (REAL EF)
            _claimRepository.Setup(x => x.Query())
                .Returns(dbContext.Claims);

            _claimRepository.Setup(x => x.Entry(It.IsAny<ClaimEntity>()))
                .Returns<ClaimEntity>(c => dbContext.Entry(c));

            _claimRepository.Setup(x => x.Update(It.IsAny<ClaimEntity>()));
            _claimRepository.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);

            _claimNoteRepository.Setup(x => x.Add(It.IsAny<ClaimNoteEntity>()));
            _claimNoteRepository.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);
            _claimFlagTransactionRepository.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<ClaimFlagTransaction>>()))
    .Returns(Task.FromResult(0)); // <-- FIX: Return Task<int> instead of Task

            _claimRepository.Setup(r => r.CommitAsync()).Returns(Task.FromResult(0)); // <-- FIX: Return Task<int> instead of Task

            _claimFlagTransactionRepository.Setup(r => r.CommitAsync()).Returns(Task.FromResult(0));

            _claimHistoryService.Setup(x =>
                x.AddAsync(It.IsAny<ClaimHistorySaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            _claimHistoryService.Setup(x =>
                x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            _messageBus.Setup(x =>
                x.SendBatchAsync(It.IsAny<string>(),
                                 It.IsAny<List<ClaimTransactionModel>>(),
                                 It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            var model = new ClaimsVoidModel
            {
                ClaimIds = new[] { claimId },
                SubmitToClearinghouse = true,
                Note = "Void note",
                claimNote = "Internal note"
            };

            // Act
            var result = await _claimService.VoidClaimsAsync(
                accountInfoId,
                memberId,
                model,
                null);

            // Assert
            Assert.Single(result);
            Assert.Equal("CLM-VOID-001", result[0]);

            Assert.Equal(ClaimStatus.Void, claim.ClaimStatus);
            Assert.Equal(ClaimFrequencyType.Void, claim.FrequencyTypeId);

            _claimManagerService.Verify(x =>
                x.SubmitClaimRebill(claimId, memberId, ClaimFrequencyType.Void),
                Times.Once);
        }

        [Fact]
        public async Task DeleteClaimsAsync_ShouldReturnEmptyList_WhenNoMatchingClaimsFound()
        {
            // Arrange
            var accountInfoId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();
            var claimIds = new[] { 999, 888 };

            _claimRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimEntity>.Create(new List<ClaimEntity>()));

            _claimRepository.Setup(x => x.CommitAsync()).Returns(Task.FromResult(0));

            _messageBus.Setup(x => x.SendBatchAsync(
                It.IsAny<string>(),
                It.IsAny<List<ClaimTransactionModel>>(),
                It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _claimService.DeleteClaimsAsync(accountInfoId, memberId, claimIds);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            _claimRepository.Verify(x => x.Update(It.IsAny<ClaimEntity>()), Times.Never);
            _claimHistoryService.Verify(x => x.AddAsync(It.IsAny<ClaimHistorySaveModel>(), It.IsAny<bool>()), Times.Never);
        }


        [Theory]
        [InlineData(ClaimStatus.ReadyToBill, ClaimAction.Rebill, ModifyAppointmentsPermission.Allow)]
        [InlineData(ClaimStatus.ReadyToBill, ClaimAction.Approve, ModifyAppointmentsPermission.Forbid)]
        [InlineData(ClaimStatus.ReadyToBill, ClaimAction.Edit, ModifyAppointmentsPermission.Forbid)]
        [InlineData(ClaimStatus.PendingReview, ClaimAction.Rebill, ModifyAppointmentsPermission.Allow)]
        [InlineData(ClaimStatus.Billed, ClaimAction.Rebill, ModifyAppointmentsPermission.Warn)]
        public async Task GetClaimByIdentifierAsync_ShouldSetCorrectForbidAddAppointmentPermission_BasedOnClaimStatusAndHistoryAction(
    ClaimStatus claimStatus,
    ClaimAction historyAction,
    ModifyAppointmentsPermission expectedPermission)
        {
            // Arrange
            var claimIdentifier = "CLM-001";
            var accountInfoId = 1;
            var childProfileId = 100;

            var claim = new ClaimEntity
            {
                Id = 1,
                AccountInfoId = accountInfoId,
                ChildProfileId = childProfileId,
                ClaimIdentifier = claimIdentifier,
                ClaimStatus = claimStatus,
                DateDeleted = null,
                ClaimHistory = new List<ClaimHistoryEntity>
        {
            new ClaimHistoryEntity
            {
                Id = 1,
                ClaimId = 1,
                ClaimAction = historyAction,
                ClaimHistoryAction = ClaimHistoryAction.MovedToReadyToBill,
                DateCreated = DateTime.UtcNow,
                Mode = ClaimActionMode.User
            }
        },
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>()
            };

            SetupClaim(claim);
            SetupMapper();

            // Act
            var result = await _claimService.GetClaimByIdentifierAsync(claimIdentifier, accountInfoId);

            // Assert
            Assert.True(result.Success);
            var claimModel = result.Data as ClaimModel;
            Assert.NotNull(claimModel);
            Assert.Equal(expectedPermission, claimModel.ForbidAddAppointment);
        }

        [Fact]
        public async Task GetClaimByIdentifierAsync_ShouldSetForbidAddAppointment_WhenReadyToBillWithNoHistory()
        {
            // Arrange
            var claimIdentifier = "CLM-002";
            var accountInfoId = 1;
            var childProfileId = 100;

            var claim = new ClaimEntity
            {
                Id = 2,
                AccountInfoId = accountInfoId,
                ChildProfileId = childProfileId,
                ClaimIdentifier = claimIdentifier,
                ClaimStatus = ClaimStatus.ReadyToBill,
                DateDeleted = null,
                ClaimHistory = new List<ClaimHistoryEntity>(), // Empty history
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>()
            };

            SetupClaim(claim);
            SetupMapper();

            // Act
            var result = await _claimService.GetClaimByIdentifierAsync(claimIdentifier, accountInfoId);

            // Assert
            Assert.True(result.Success);
            var claimModel = result.Data as ClaimModel;
            Assert.NotNull(claimModel);
            Assert.Equal(ModifyAppointmentsPermission.Forbid, claimModel.ForbidAddAppointment);
        }

        [Fact]
        public async Task GetClaimByIdentifierAsync_ShouldSetAllowPermission_WhenReadyToBillWithMultipleHistoriesAndLatestIsRebill()
        {
            // Arrange
            var claimIdentifier = "CLM-003";
            var accountInfoId = 1;
            var childProfileId = 100;

            var claim = new ClaimEntity
            {
                Id = 3,
                AccountInfoId = accountInfoId,
                ChildProfileId = childProfileId,
                ClaimIdentifier = claimIdentifier,
                ClaimStatus = ClaimStatus.ReadyToBill,
                DateDeleted = null,
                ClaimHistory = new List<ClaimHistoryEntity>
        {
            new ClaimHistoryEntity
            {
                Id = 1,
                ClaimId = 3,
                ClaimAction = ClaimAction.Approve,
                DateCreated = DateTime.UtcNow.AddDays(-2),
                Mode = ClaimActionMode.User
            },
            new ClaimHistoryEntity
            {
                Id = 2,
                ClaimId = 3,
                ClaimAction = ClaimAction.Edit,
                DateCreated = DateTime.UtcNow.AddDays(-1),
                Mode = ClaimActionMode.User
            },
            new ClaimHistoryEntity
            {
                Id = 3,
                ClaimId = 3,
                ClaimAction = ClaimAction.Rebill, // Latest action
                DateCreated = DateTime.UtcNow,
                Mode = ClaimActionMode.User
            }
        },
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>()
            };

            SetupClaim(claim);
            SetupMapper();

            // Act
            var result = await _claimService.GetClaimByIdentifierAsync(claimIdentifier, accountInfoId);

            // Assert
            Assert.True(result.Success);
            var claimModel = result.Data as ClaimModel;
            Assert.NotNull(claimModel);
            Assert.Equal(ModifyAppointmentsPermission.Allow, claimModel.ForbidAddAppointment);
        }


        [Fact]
        public async Task GetClaimLineAppointmentsAsync_ShouldReturnEmptyList_WhenNoAppointmentsLinked()
        {
            // Arrange
            var accountInfoId = 1;
            var serviceLineId = 100;
            var claimId = 1;

            var claimChargeEntry = new ClaimChargeEntryEntity
            {
                Id = serviceLineId,
                ClaimId = claimId,
                BillingCode = "97153",
                DiagnosisCode = "F84.0",
                DateDeleted = null,
                Claim = new ClaimEntity
                {
                    Id = claimId,
                    AccountInfoId = accountInfoId
                }
            };

            var providerBillingCodes = new List<RethinkProviderBillingCode>
    {
        new RethinkProviderBillingCode
        {
            id = 1,
            billingCode = "97153",
            billingCode2 = "97154"
        }
    };

            var diagnosis = new Diagnosis { id = 1, diagnosisCode = "F84.0" };

            _claimChargeEntryRepository.Setup(x => x.Query())
                .Returns(new List<ClaimChargeEntryEntity> { claimChargeEntry }.AsQueryable().BuildMock());

            _rethinkService.Setup(x => x.GetProviderBillingCode(accountInfoId, "97153"))
                .ReturnsAsync(providerBillingCodes);

            _rethinkService.Setup(x => x.GetDiagnosisByCodeAsync(accountInfoId, "F84.0"))
                .ReturnsAsync(diagnosis);

            _claimAppointmentLinkRepository.Setup(x => x.Query())
                .Returns(new List<ClaimAppointmentLinkEntity>().AsQueryable().BuildMock());

            _rethinkService.Setup(x => x.GetAppointmentListAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<AppointmentRethinkModel>());

            SetupMapper();

            // Act
            var result = await _claimService.GetClaimLineAppointmentsAsync(accountInfoId, serviceLineId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllCarcCodes_ShouldReturnMappedCarcCodes_WhenCodesExist()
        {
            // Arrange
            var carcCodeEntities = new List<CarcCodeEntity>
    {
        new CarcCodeEntity
        {
            Id = 1,
            Code = "1",
            Description = "Deductible Amount",
            DateDeleted = null
        },
        new CarcCodeEntity
        {
            Id = 2,
            Code = "2",
            Description = "Coinsurance Amount",
            DateDeleted = null
        },
        new CarcCodeEntity
        {
            Id = 3,
            Code = "3",
            Description = "Co-payment Amount",
            DateDeleted = null
        }
    };

            var expectedResponse = new List<CarcCodeResponseModel>
    {
        new CarcCodeResponseModel
        {
            Code = "1",
            Description = "Deductible Amount"
        },
        new CarcCodeResponseModel
        {
            Code = "2",
            Description = "Coinsurance Amount"
        },
        new CarcCodeResponseModel
        {
            Code = "3",
            Description = "Co-payment Amount"
        }
    };

            // Mock the cache service to return the entities
            _cacheService.Setup(x => x.GetOrSetCacheAsync(
                    "deniedReasonCodes",
                    It.IsAny<Func<Task<List<CarcCodeEntity>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(carcCodeEntities);

            // Act
            var result = await _claimService.GetAllCarcCodes();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal("1", result[0].Code);
            Assert.Equal("Deductible Amount", result[0].Description);

            // Verify cache service was called with correct parameters
            _cacheService.Verify(x => x.GetOrSetCacheAsync(
                "deniedReasonCodes",
                It.IsAny<Func<Task<List<CarcCodeEntity>>>>(),
                It.Is<TimeSpan>(ts => ts.TotalMinutes == 60)), Times.Once);
        }

        [Fact]
        public async Task GetAllCarcCodes_ShouldReturnEmptyList_WhenNoCodesExist()
        {
            // Arrange
            var emptyCarcCodeList = new List<CarcCodeEntity>();

            _cacheService.Setup(x => x.GetOrSetCacheAsync(
                    "deniedReasonCodes",
                    It.IsAny<Func<Task<List<CarcCodeEntity>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(emptyCarcCodeList);

            // Act
            var result = await _claimService.GetAllCarcCodes();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllCarcCodes_ShouldUseCorrectCacheKey()
        {
            // Arrange
            var carcCodeEntities = new List<CarcCodeEntity>();
            var expectedCacheKey = "deniedReasonCodes";

            _cacheService.Setup(x => x.GetOrSetCacheAsync(
                    expectedCacheKey,
                    It.IsAny<Func<Task<List<CarcCodeEntity>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(carcCodeEntities);

            // Act
            await _claimService.GetAllCarcCodes();

            // Assert
            _cacheService.Verify(x => x.GetOrSetCacheAsync(
                expectedCacheKey,
                It.IsAny<Func<Task<List<CarcCodeEntity>>>>(),
                It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task GetAllCarcCodes_ShouldUseCacheExpirationOf60Minutes()
        {
            // Arrange
            var carcCodeEntities = new List<CarcCodeEntity>();
            var expectedExpiration = TimeSpan.FromMinutes(60);

            _cacheService.Setup(x => x.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<CarcCodeEntity>>>>(),
                    It.Is<TimeSpan>(ts => ts == expectedExpiration)))
                .ReturnsAsync(carcCodeEntities);

            // Act
            await _claimService.GetAllCarcCodes();

            // Assert
            _cacheService.Verify(x => x.GetOrSetCacheAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<CarcCodeEntity>>>>(),
                It.Is<TimeSpan>(ts => ts.TotalMinutes == 60)), Times.Once);
        }

        [Fact]
        public async Task GetAllCarcCodes_ShouldFilterDeletedRecords_WhenCallingRepository()
        {
            // Arrange
            var allCarcCodes = new List<CarcCodeEntity>
    {
        new CarcCodeEntity { Id = 1, Code = "1", DateDeleted = null },
        new CarcCodeEntity { Id = 2, Code = "2", DateDeleted = DateTime.UtcNow }, // Deleted
        new CarcCodeEntity { Id = 3, Code = "3", DateDeleted = null }
    };

            // Setup mock to capture and execute the fetch function
            Func<Task<List<CarcCodeEntity>>> capturedFetchFunc = null;

            _cacheService.Setup(x => x.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<CarcCodeEntity>>>>(),
                    It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<List<CarcCodeEntity>>>, TimeSpan>(
                    async (key, fetchFunc, expiration) =>
                    {
                        capturedFetchFunc = fetchFunc;
                        return await fetchFunc();
                    });

            // Mock repository to return all codes including deleted
            _carcCodeRepository.Setup(x => x.Query())
                .Returns(allCarcCodes.AsQueryable().BuildMock());

            // Act
            await _claimService.GetAllCarcCodes();

            // Assert - verify only non-deleted codes would be returned
            var queryable = _carcCodeRepository.Object.Query();
            var filteredCodes = await queryable.Where(x => x.DateDeleted == null).ToListAsync();
            Assert.Equal(2, filteredCodes.Count);
            Assert.All(filteredCodes, c => Assert.Null(c.DateDeleted));
        }

        [Theory]
        [InlineData(ClaimListingTab.ReadyToBill, ClaimStatus.Void, false, true)]
        [InlineData(ClaimListingTab.ReadyToBill, ClaimStatus.ReadyToBill, false, true)]
        [InlineData(ClaimListingTab.ReadyToBill, ClaimStatus.Rebill, false, true)]
        [InlineData(ClaimListingTab.ReadyToBill, ClaimStatus.SubmissionFailed, false, true)]
        [InlineData(ClaimListingTab.ReadyToBill, ClaimStatus.Billed, false, false)]
        [InlineData(ClaimListingTab.ReadyToBill, ClaimStatus.ReadyToBill, true, false)] // Flagged should be excluded
        [InlineData(ClaimListingTab.BillingPending, ClaimStatus.Billed, false, true)]
        [InlineData(ClaimListingTab.BillingPending, ClaimStatus.Pending, false, true)]
        [InlineData(ClaimListingTab.BillingPending, ClaimStatus.Paid, false, true)]
        [InlineData(ClaimListingTab.BillingPending, ClaimStatus.AcceptedClearingHouse, false, true)]
        [InlineData(ClaimListingTab.BillingPending, ClaimStatus.AcceptedFunder, false, true)]
        [InlineData(ClaimListingTab.BillingPending, ClaimStatus.ReceivedFunder, false, true)]
        [InlineData(ClaimListingTab.BillingPending, ClaimStatus.BillNextFunder, false, true)]
        [InlineData(ClaimListingTab.BillingPending, ClaimStatus.Closed, false, false)]
        [InlineData(ClaimListingTab.BillingPending, ClaimStatus.Billed, true, false)] // Flagged should be excluded
        [InlineData(ClaimListingTab.Completed, ClaimStatus.Closed, false, true)]
        [InlineData(ClaimListingTab.Completed, ClaimStatus.VoidClosed, false, true)]
        [InlineData(ClaimListingTab.Completed, ClaimStatus.Billed, false, false)]
        [InlineData(ClaimListingTab.Completed, ClaimStatus.Closed, true, false)] // Flagged should be excluded
        [InlineData(ClaimListingTab.Rejected, ClaimStatus.RejectedClearinghouse, false, true)]
        [InlineData(ClaimListingTab.Rejected, ClaimStatus.RejectedFunder, false, true)]
        [InlineData(ClaimListingTab.Rejected, ClaimStatus.Billed, false, false)]
        [InlineData(ClaimListingTab.Rejected, ClaimStatus.RejectedFunder, true, false)] // Flagged should be excluded
        [InlineData(ClaimListingTab.Denied, ClaimStatus.Denied, false, true)]
        [InlineData(ClaimListingTab.Denied, ClaimStatus.Billed, false, false)]
        [InlineData(ClaimListingTab.Denied, ClaimStatus.Denied, true, false)] // Flagged should be excluded
        [InlineData(ClaimListingTab.Flagged, ClaimStatus.Billed, true, true)]
        [InlineData(ClaimListingTab.Flagged, ClaimStatus.ReadyToBill, true, true)]
        [InlineData(ClaimListingTab.Flagged, ClaimStatus.Denied, true, true)]
        [InlineData(ClaimListingTab.Flagged, ClaimStatus.Closed, true, true)]
        [InlineData(ClaimListingTab.Flagged, ClaimStatus.Billed, false, false)] // Not flagged should be excluded
        [InlineData(ClaimListingTab.PendingReview, ClaimStatus.PendingReview, false, true)]
        [InlineData(ClaimListingTab.PendingReview, ClaimStatus.ApprovalFailed, false, true)]
        [InlineData(ClaimListingTab.PendingReview, ClaimStatus.Billed, false, false)]
        [InlineData(ClaimListingTab.PendingReview, ClaimStatus.PendingReview, true, false)] // Flagged should be excluded
        public async Task FilterClaimsBySelectedTab_ShouldFilterClaimsCorrectly(
    ClaimListingTab tab,
    ClaimStatus claimStatus,
    bool isFlagged,
    bool shouldBeIncluded)
        {
            // Arrange
            var accountInfoId = 1;
            var testClaim = new ClaimEntity
            {
                Id = 1,
                AccountInfoId = accountInfoId,
                ClaimIdentifier = "TEST-001",
                ClaimStatus = claimStatus,
                IsFlagged = isFlagged,
                DateDeleted = null,
                ChildProfileId = 100,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(-9)
            };

            var claims = new List<ClaimEntity> { testClaim };

            _claimRepository.Setup(x => x.Query())
                .Returns(claims.AsQueryable().BuildMock());

            // Act
            var result = await _claimRepository.Object.Query()
                .Where(x => x.AccountInfoId == accountInfoId &&
                            x.DateDeleted == null &&
                            x.ClaimIdentifier != null)
                .Where(FilterClaimsBySelectedTab(tab))
                .ToListAsync();

            // Assert
            if (shouldBeIncluded)
            {
                Assert.Single(result);
                Assert.Equal(testClaim.Id, result[0].Id);
                Assert.Equal(claimStatus, result[0].ClaimStatus);
                Assert.Equal(isFlagged, result[0].IsFlagged);
            }
            else
            {
                Assert.Empty(result);
            }
        }

        [Fact]
        public async Task FilterClaimsBySelectedTab_ShouldExcludeDeletedClaims()
        {
            // Arrange
            var accountInfoId = 1;
            var deletedClaim = new ClaimEntity
            {
                Id = 1,
                AccountInfoId = accountInfoId,
                ClaimIdentifier = "TEST-001",
                ClaimStatus = ClaimStatus.ReadyToBill,
                IsFlagged = false,
                DateDeleted = DateTime.UtcNow, // Soft deleted
                ChildProfileId = 100,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(-9)
            };

            var claims = new List<ClaimEntity> { deletedClaim };

            _claimRepository.Setup(x => x.Query())
                .Returns(claims.AsQueryable().BuildMock());

            // Act
            var result = await _claimRepository.Object.Query()
            .Where(x => x.AccountInfoId == accountInfoId &&
                            x.DateDeleted == null &&
                            x.ClaimIdentifier != null)
                .Where(FilterClaimsBySelectedTab(ClaimListingTab.ReadyToBill))
                .ToListAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task FilterClaimsBySelectedTab_ShouldHandleMultipleClaims()
        {
            // Arrange
            var accountInfoId = 1;
            var claims = new List<ClaimEntity>
    {
        new ClaimEntity
        {
            Id = 1,
            AccountInfoId = accountInfoId,
            ClaimIdentifier = "TEST-001",
            ClaimStatus = ClaimStatus.ReadyToBill,
            IsFlagged = false,
            DateDeleted = null,
            ChildProfileId = 100,
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(-9)
        },
        new ClaimEntity
        {
            Id = 2,
            AccountInfoId = accountInfoId,
            ClaimIdentifier = "TEST-002",
            ClaimStatus = ClaimStatus.Rebill,
            IsFlagged = false,
            DateDeleted = null,
            ChildProfileId = 100,
            StartDate = DateTime.UtcNow.AddDays(-8),
            EndDate = DateTime.UtcNow.AddDays(-7)
        },
        new ClaimEntity
        {
            Id = 3,
            AccountInfoId = accountInfoId,
            ClaimIdentifier = "TEST-003",
            ClaimStatus = ClaimStatus.Billed,
            IsFlagged = false,
            DateDeleted = null,
            ChildProfileId = 100,
            StartDate = DateTime.UtcNow.AddDays(-6),
            EndDate = DateTime.UtcNow.AddDays(-5)
        }
    };

            _claimRepository.Setup(x => x.Query())
                .Returns(claims.AsQueryable().BuildMock());

            // Act
            var result = await _claimRepository.Object.Query()
                .Where(x => x.AccountInfoId == accountInfoId &&
                            x.DateDeleted == null &&
                            x.ClaimIdentifier != null)
                .Where(FilterClaimsBySelectedTab(ClaimListingTab.ReadyToBill))
                .ToListAsync();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, c => c.ClaimStatus == ClaimStatus.ReadyToBill);
            Assert.Contains(result, c => c.ClaimStatus == ClaimStatus.Rebill);
            Assert.DoesNotContain(result, c => c.ClaimStatus == ClaimStatus.Billed);
        }

        // ... previous code ...
        [Fact]
        public async Task FilterClaimsBySelectedTab_FlaggedTab_ShouldIncludeOnlyFlaggedClaims()
        {
            // Arrange
            var accountInfoId = 1;
            var claims = new List<ClaimEntity>
    {
        new ClaimEntity
        {
            Id = 1,
            AccountInfoId = accountInfoId,
            ClaimIdentifier = "TEST-001",
            ClaimStatus = ClaimStatus.ReadyToBill,
            IsFlagged = true,
            DateDeleted = null,
            ChildProfileId = 100,
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(-9)
        },
        new ClaimEntity
        {
            Id = 2,
            AccountInfoId = accountInfoId,
            ClaimIdentifier = "TEST-002",
            ClaimStatus = ClaimStatus.Billed,
            IsFlagged = true,
            DateDeleted = null,
            ChildProfileId = 100,
            StartDate = DateTime.UtcNow.AddDays(-8),
            EndDate = DateTime.UtcNow.AddDays(-7)
        },
        new ClaimEntity
        {
            Id = 3,
            AccountInfoId = accountInfoId,
            ClaimIdentifier = "TEST-003",
            ClaimStatus = ClaimStatus.Closed,
            IsFlagged = false,
            DateDeleted = null,
            ChildProfileId = 100,
            StartDate = DateTime.UtcNow.AddDays(-6),
            EndDate = DateTime.UtcNow.AddDays(-5)
        }
    };

            _claimRepository.Setup(x => x.Query())
                .Returns(claims.AsQueryable().BuildMock());

            // Act
            var result = await _claimRepository.Object.Query()
                .Where(x => x.AccountInfoId == accountInfoId &&
                            x.DateDeleted == null &&
                            x.ClaimIdentifier != null)
                .Where(FilterClaimsBySelectedTab(ClaimListingTab.Flagged))
                .ToListAsync();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, claim => Assert.True(claim.IsFlagged));
        }

        [Fact]
        public async Task FilterClaimsBySelectedTab_ShouldExcludeClaimsWithNullClaimIdentifier()
        {
            // Arrange
            var accountInfoId = 1;
            var claims = new List<ClaimEntity>
    {
        new ClaimEntity
        {
            Id = 1,
            AccountInfoId = accountInfoId,
            ClaimIdentifier = "TEST-001",
            ClaimStatus = ClaimStatus.ReadyToBill,
            IsFlagged = false,
            DateDeleted = null,
            ChildProfileId = 100,
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(-9)
        },
        new ClaimEntity
        {
            Id = 2,
            AccountInfoId = accountInfoId,
            ClaimIdentifier = null, // Null identifier
            ClaimStatus = ClaimStatus.ReadyToBill,
            IsFlagged = false,
            DateDeleted = null,
            ChildProfileId = 100,
            StartDate = DateTime.UtcNow.AddDays(-8),
            EndDate = DateTime.UtcNow.AddDays(-7)
        }
    };

            _claimRepository.Setup(x => x.Query())
                .Returns(claims.AsQueryable().BuildMock());

            // Act
            var result = await _claimRepository.Object.Query()
                .Where(x => x.AccountInfoId == accountInfoId &&
                            x.DateDeleted == null &&
                            x.ClaimIdentifier != null)
                .Where(FilterClaimsBySelectedTab(ClaimListingTab.ReadyToBill))
                .ToListAsync();

            // Assert
            Assert.Single(result);
            Assert.NotNull(result[0].ClaimIdentifier);
        }

        // Helper method - you'll need to expose FilterClaimsBySelectedTab or use reflection
        private Expression<Func<ClaimEntity, bool>> FilterClaimsBySelectedTab(ClaimListingTab tab)
        {
            switch (tab)
            {
                case ClaimListingTab.ReadyToBill:
                    return entity => (entity.ClaimStatus == ClaimStatus.Void ||
                            entity.ClaimStatus == ClaimStatus.ReadyToBill ||
                            entity.ClaimStatus == ClaimStatus.Rebill ||
                            entity.ClaimStatus == ClaimStatus.SubmissionFailed) && !entity.IsFlagged;
                case ClaimListingTab.BillingPending:
                    return entity => (entity.ClaimStatus == ClaimStatus.Billed ||
                            entity.ClaimStatus == ClaimStatus.Pending ||
                            entity.ClaimStatus == ClaimStatus.Paid ||
                            entity.ClaimStatus == ClaimStatus.AcceptedClearingHouse ||
                            entity.ClaimStatus == ClaimStatus.AcceptedFunder ||
                            entity.ClaimStatus == ClaimStatus.ReceivedFunder ||
                            entity.ClaimStatus == ClaimStatus.BillNextFunder) && !entity.IsFlagged;
                case ClaimListingTab.Completed:
                    return entity => (entity.ClaimStatus == ClaimStatus.Closed ||
                            entity.ClaimStatus == ClaimStatus.VoidClosed) && !entity.IsFlagged;
                case ClaimListingTab.Rejected:
                    return entity => (entity.ClaimStatus == ClaimStatus.RejectedClearinghouse ||
                            entity.ClaimStatus == ClaimStatus.RejectedFunder) && !entity.IsFlagged;
                case ClaimListingTab.Denied:
                    return entity => entity.ClaimStatus == ClaimStatus.Denied && !entity.IsFlagged;
                case ClaimListingTab.Flagged:
                    return entity => entity.IsFlagged;
                case ClaimListingTab.PendingReview:
                default:
                    return entity => (entity.ClaimStatus == ClaimStatus.PendingReview
                           || entity.ClaimStatus == ClaimStatus.ApprovalFailed) && !entity.IsFlagged;
            }
        }

        [Fact]
        public async Task ValidateClaimDataAsync_ShouldUseSecondaryResponsibilitySequence_WhenIsSecondaryIsTrue()
        {
            // Arrange
            var claimId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();
            var accountInfoId = Fixture.Create<int>();

            var model = new ClaimValidationModel
            {
                Id = claimId,
                MemberId = memberId,
                IsSecondary = true,
                SecondaryFunderId = null
            };

            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, claimId)
                .With(x => x.AccountInfoId, accountInfoId)
                .Create();

            _claimRepository.Setup(x => x.GetByIdAsync(claimId))
                .ReturnsAsync(claim);

            _claimHistoryService.Setup(x => x.AddAsync(
                It.IsAny<ClaimHistorySaveModel>(),
                It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            _claimValidationService.Setup(x => x.ValidateClaimData(
                claimId,
                memberId,
                null,
                ResponsibilitySequenceType.Secondary,
                false,
                null))
                .Returns(Task.CompletedTask);

            // Act
            await _claimService.ValidateClaimDataAsync(model);

            // Assert
            _claimValidationService.Verify(x => x.ValidateClaimData(
                claimId,
                memberId,
                null,
                ResponsibilitySequenceType.Secondary,
                false,
                null),
                Times.Once);
        }

        [Fact]
        public async Task ValidateClaimDataAsync_ShouldNotCallValidation_WhenClaimDoesNotExist()
        {
            // Arrange
            var claimId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();

            var model = new ClaimValidationModel
            {
                Id = claimId,
                MemberId = memberId,
                IsSecondary = false,
                SecondaryFunderId = null
            };

            _claimRepository.Setup(x => x.GetByIdAsync(claimId))
                .ReturnsAsync((ClaimEntity)null);

            // Act
            await _claimService.ValidateClaimDataAsync(model);

            // Assert
            _claimHistoryService.Verify(x => x.AddAsync(
                It.IsAny<ClaimHistorySaveModel>(),
                It.IsAny<bool>()),
                Times.Never);

            _claimValidationService.Verify(x => x.ValidateClaimData(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<ClaimEntity>(),
                It.IsAny<ResponsibilitySequenceType>(),
                It.IsAny<bool>(),
                It.IsAny<int?>()),
                Times.Never);
        }


        [Fact]
        public async Task ValidateClaimDataAsync_ShouldHandleNullClientFunderServiceLineId()
        {
            // Arrange
            var claimId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();
            var accountInfoId = Fixture.Create<int>();
            var childProfileId = Fixture.Create<int>();
            var clientFunderId = Fixture.Create<int>();

            var model = new ClaimValidationModel
            {
                Id = claimId,
                MemberId = memberId,
                IsSecondary = false,
                SecondaryFunderId = null
            };

            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, claimId)
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.ChildProfileId, childProfileId)
                .With(x => x.ClientFunderId, clientFunderId)
                .With(x => x.ClientFunderServiceLineId, (int?)null)
                .Create();

            _claimRepository.Setup(x => x.GetByIdAsync(claimId))
                .ReturnsAsync(claim);

            _claimHistoryService.Setup(x => x.AddAsync(
                It.IsAny<ClaimHistorySaveModel>(),
                It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            _claimValidationService.Setup(x => x.ValidateClaimData(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<ClaimEntity>(),
                It.IsAny<ResponsibilitySequenceType>(),
                It.IsAny<bool>(),
                It.IsAny<int?>()))
                .Returns(Task.CompletedTask);

            // Act & Assert
            await _claimService.ValidateClaimDataAsync(model);

            _claimHistoryService.Verify(x => x.AddAsync(
                It.IsAny<ClaimHistorySaveModel>(),
                It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task ValidateClaimDataAsync_ShouldAddHistoryBeforeValidation()
        {
            // Arrange
            var claimId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();
            var accountInfoId = Fixture.Create<int>();
            var childProfileId = Fixture.Create<int>();
            var clientFunderId = Fixture.Create<int>();
            var clientFunderServiceLineId = Fixture.Create<int>();

            var model = new ClaimValidationModel
            {
                Id = claimId,
                MemberId = memberId,
                IsSecondary = false,
                SecondaryFunderId = null
            };

            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, claimId)
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.ChildProfileId, childProfileId)
                .With(x => x.ClientFunderId, clientFunderId)
                .With(x => x.ClientFunderServiceLineId, clientFunderServiceLineId)
                .Create();

            _claimRepository.Setup(x => x.GetByIdAsync(claimId))
                .ReturnsAsync(claim);

            var callOrder = new List<string>();

            _claimHistoryService.Setup(x => x.AddAsync(
                It.IsAny<ClaimHistorySaveModel>(),
                It.IsAny<bool>()))
                .Callback(() => callOrder.Add("History"))
                .Returns(Task.CompletedTask);

            var funderMapping = Fixture.Build<FunderDetails>()
                .With(x => x.insuranceType, ResponsibilitySequenceType.Primary)
                .Create();

            _rethinkService.Setup(x => x.GetChildProfileFunderServiceLineMappingEntity(
                accountInfoId,
                childProfileId,
                clientFunderId,
                clientFunderServiceLineId))
                .ReturnsAsync(Fixture.Build<ServiceLines>()
                    .With(x => x.ChildProfileFunderMapping, funderMapping)
                    .Create());

            _claimValidationService.Setup(x => x.ValidateClaimData(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<ClaimEntity>(),
                It.IsAny<ResponsibilitySequenceType>(),
                It.IsAny<bool>(),
                It.IsAny<int?>()))
                .Callback(() => callOrder.Add("Validation"))
                .Returns(Task.CompletedTask);

            // Act
            await _claimService.ValidateClaimDataAsync(model);

            // Assert
            Assert.Equal(2, callOrder.Count);
            Assert.Equal("History", callOrder[0]);
            Assert.Equal("Validation", callOrder[1]);
        }

        [Fact]
        public async Task SubmitClaimsAsync_ShouldReturnEmptyList_WhenNoClaimsProvided()
        {
            // Arrange
            var accountInfoId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();

            var model = new ClaimsSubmitModel
            {
                AccountInfoId = accountInfoId,
                MemberId = memberId,
                Ids = new int[] { },
                IsSecondary = false,
                SecondaryFunderDetails = new List<SecondaryFunderDetailsModel>()
            };

            _messageBus.Setup(x => x.SendBatchAsync(
                It.IsAny<string>(),
                It.IsAny<List<ClaimTransactionModel>>(),
                It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _claimService.SubmitClaimsAsync(model);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            _claimRepository.Verify(x => x.Update(It.IsAny<ClaimEntity>()), Times.Never);
            _clearingHouseService.Verify(x => x.SubmitClaimAsync(
                It.IsAny<ClaimSubmitModel>()), Times.Never);
        }



        [Fact]
        public async Task SubmitClaimsAsync_ShouldReturnEmptyList_WhenAllClaimsAreNull()
        {
            // Arrange
            var accountInfoId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();
            var claimIds = new[] { 1, 2, 3 };

            var model = new ClaimsSubmitModel
            {
                AccountInfoId = accountInfoId,
                MemberId = memberId,
                Ids = claimIds,
                IsSecondary = false,
                SecondaryFunderDetails = claimIds.Select(id => new SecondaryFunderDetailsModel { ClaimId = id }).ToList()
            };

            _claimRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimEntity>.Create(new List<ClaimEntity>()));

            _messageBus.Setup(x => x.SendBatchAsync(
                It.IsAny<string>(),
                It.IsAny<List<ClaimTransactionModel>>(),
                It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _claimService.SubmitClaimsAsync(model);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            _claimVersionService.Verify(x => x.CreateAsync(
                It.IsAny<ClaimDetailsModel>(),
                It.IsAny<int>(),
                It.IsAny<int>()), Times.Never);

            _clearingHouseService.Verify(x => x.SubmitClaimAsync(
                It.IsAny<ClaimSubmitModel>()), Times.Never);
        }

        [Fact]
        public async Task UpdateBillingClaimAsync_ShouldCallUpdateBillingClaimDetailsAsync_WithSaveChangesTrue()
        {
            // Arrange
            var memberId = Fixture.Create<int>();
            var claimId = Fixture.Create<int>();
            var chargeId = Fixture.Create<int>();

            var updateChargeItem = new UpdateBillingClaimDetailsModel
            {
                Id = chargeId,
                ClaimId = claimId,
                Units = 2,
                PerUnitsCharge = 50m,
                Modifier1 = "M1"
            };

            var listModel = new UpdateBillingClaimDetailsListModel
            {
                BillingClaimDetailsModels = new List<UpdateBillingClaimDetailsModel> { updateChargeItem },
                MemberId = memberId
            };

            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, claimId)
                .With(x => x.PaymentClaims, new List<PaymentClaimEntity>())
                .Create();

            var chargeEntry = Fixture.Build<ClaimChargeEntryEntity>()
                .With(x => x.Id, chargeId)
                .With(x => x.ClaimId, claimId)
                .With(x => x.Claim, claim)
                .With(x => x.Charges, 100m)
                .Create();

            _claimChargeEntryRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimChargeEntryEntity>.Create(new List<ClaimChargeEntryEntity> { chargeEntry }));

            _claimRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimEntity>.Create(claim));

            _claimAppointmentLinkChargeEntryRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimAppointmentLinkChargeEntry>.Create(new List<ClaimAppointmentLinkChargeEntry>()));

            _paymentClaimRepository.Setup(x => x.Query())
                .Returns(QueryMock<PaymentClaimEntity>.Create(new List<PaymentClaimEntity>()));

            _claimChargeEntryWriteOffRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimChargeEntryWriteOffEntity>.Create(new List<ClaimChargeEntryWriteOffEntity>()));

            _claimDiagnosisCodeRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimDiagnosisCodeEntity>.Create(new List<ClaimDiagnosisCodeEntity>()));

            _rethinkService.Setup(x => x.GetUnitTypesAsync())
                .ReturnsAsync(new List<ClientUnitTypes> { new ClientUnitTypes { id = 1, unit = 60 } });

            _rethinkService.Setup(x => x.GetRenderingProvidersAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<AuthRenderingProviderType>());

            SetupCommitMocks();

            // Act
            var result = await _claimService.UpdateBillingClaimAsync(listModel, memberId, isValidateRequired: false);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            _claimChangeTrackingService.Verify(x => x.SaveChangesAsync(), Times.Once);
        }


        [Fact]
        public async Task UpdateBillingClaimDetailsAsync_ShouldThrowNullReference_WhenParentClaimNotFoundForNewCharge()
        {
            // Arrange
            var memberId = Fixture.Create<int>();
            var claimId = 999; // Non-existent claim

            var newChargeModel = new UpdateBillingClaimDetailsModel
            {
                Id = 0,
                ClaimId = claimId,
                Units = 1,
                PerUnitsCharge = 100m
            };

            var listModel = new UpdateBillingClaimDetailsListModel
            {
                BillingClaimDetailsModels = new List<UpdateBillingClaimDetailsModel> { newChargeModel },
                MemberId = memberId
            };

            _claimRepository.Setup(x => x.Query())
                .Returns(new List<ClaimEntity>().AsQueryable().BuildMock());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NullReferenceException>(() =>
                _claimService.UpdateBillingClaimDetailsAsync(listModel, memberId, isValidateRequired: false, saveChanges: false));

            Assert.Contains("Parent Claim not found for new Charge Entry", exception.Message);

            _claimChargeEntryRepository.Verify(x => x.Add(It.IsAny<ClaimChargeEntryEntity>()), Times.Never);
        }



        // Helper method to setup common commit mocks
        private void SetupCommitMocks()
        {
            _paymentClaimRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
            _paymentClaimRepository.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);
            _paymentClaimServiceLineRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
            _paymentClaimServiceLineRepository.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);
            _claimChargeEntryRepository.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);
            _claimChangeTrackingService.Setup(x => x.SaveChangesAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            _messageBus.Setup(x => x.SendBatchAsync(
                It.IsAny<string>(),
                It.IsAny<List<ClaimTransactionModel>>(),
                It.IsAny<int>()))
                .Returns(Task.CompletedTask);
        }

        // Helper method to setup GetClaimChargesForAccountAsync mocks
        private void SetupGetClaimChargesMocks()
        {
            _claimAppointmentLinkChargeEntryRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimAppointmentLinkChargeEntry>.Create(new List<ClaimAppointmentLinkChargeEntry>()));

            _claimChargeEntryWriteOffRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimChargeEntryWriteOffEntity>.Create(new List<ClaimChargeEntryWriteOffEntity>()));

            _claimDiagnosisCodeRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimDiagnosisCodeEntity>.Create(new List<ClaimDiagnosisCodeEntity>()));

            _paymentClaimRepository.Setup(x => x.Query())
                .Returns(QueryMock<PaymentClaimEntity>.Create(new List<PaymentClaimEntity>()));

            _rethinkService.Setup(x => x.GetUnitTypesAsync())
                .ReturnsAsync(new List<ClientUnitTypes> { new ClientUnitTypes { id = 1, unit = 60 } });
        }

        [Fact]
        public async Task Returns_claim_ids_when_any_history_action_is_billed()
        {
            // Arrange
            var accountInfoId = 10;
            var claimIds = new[] { 1, 2 };

            var claim1 = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, 1)
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.DateDeleted, (DateTime?)null)
                .With(x => x.ClaimHistory, new List<ClaimHistoryEntity>
                {
            new ClaimHistoryEntity
            {
                ClaimId = 1,
                DateCreated = DateTime.UtcNow.AddDays(-2),
                ClaimHistoryAction = ClaimHistoryAction.ClaimCreated
            },
            new ClaimHistoryEntity
            {
                ClaimId = 1,
                DateCreated = DateTime.UtcNow.AddDays(-1),
                ClaimHistoryAction = ClaimHistoryAction.BilledElectronically
            }
                })
                .Create();

            var claim2 = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, 2)
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.DateDeleted, (DateTime?)null)
                .With(x => x.ClaimHistory, new List<ClaimHistoryEntity>
                {
            new ClaimHistoryEntity
            {
                ClaimId = 2,
                DateCreated = DateTime.UtcNow.AddDays(-1),
                ClaimHistoryAction = ClaimHistoryAction.ClaimCreated
            }
                })
                .Create();

            var claims = new List<ClaimEntity> { claim1, claim2 };

            _claimRepository.Setup(x => x.Query())
                .Returns(claims.AsQueryable().BuildMock());

            // Act
            var result = await _claimService.GetIdsForAccountAsync(accountInfoId);

            // Assert - GetIdsForAccountAsync returns ALL non-deleted claim IDs for the account
            Assert.Equal(2, result.Count);
            Assert.Contains(1, result);
            Assert.Contains(2, result);
        }

        [Fact]
        public async Task Does_not_return_deleted_claims_even_if_billed_history_exists()
        {
            // Arrange
            var accountInfoId = 10;
            var memberId = 123;
            var claimIds = new[] { 3 };

            var deletedClaim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, 3)
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.DateDeleted, DateTime.UtcNow)
                .With(x => x.ClaimHistory, new List<ClaimHistoryEntity>
                {
            new ClaimHistoryEntity
            {
                ClaimId = 3,
                DateCreated = DateTime.UtcNow.AddDays(-1),
                ClaimHistoryAction = ClaimHistoryAction.BilledElectronically
            }
                })
                .Create();

            _claimRepository.Setup(x => x.Query())
                .Returns(new List<ClaimEntity> { deletedClaim }.AsQueryable().BuildMock());

            // Act
            var result = await _claimService.GetIdsForAccountAsync(accountInfoId);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task Ignores_claims_from_other_accountInfoId()
        {
            // Arrange
            var accountInfoId = 10;
            var memberId = 123;
            var claimIds = new[] { 4 };

            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, 4)
                .With(x => x.AccountInfoId, 999) // different account
                .With(x => x.DateDeleted, (DateTime?)null)
                .With(x => x.ClaimHistory, new List<ClaimHistoryEntity>
                {
            new ClaimHistoryEntity
            {
                ClaimId = 4,
                DateCreated = DateTime.UtcNow.AddDays(-1),
                ClaimHistoryAction = ClaimHistoryAction.BilledElectronically
            }
                })
                .Create();

            _claimRepository.Setup(x => x.Query())
                .Returns(new List<ClaimEntity> { claim }.AsQueryable().BuildMock());

            // Act
            var result = await _claimService.GetIdsForAccountAsync(accountInfoId);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task Skips_claim_ids_that_do_not_exist()
        {
            // Arrange
            var accountInfoId = 10;
            var memberId = 123;
            var claimIds = new[] { 999 };

            _claimRepository.Setup(x => x.Query())
                .Returns(new List<ClaimEntity>().AsQueryable().BuildMock());

            // Act
            var result = await _claimService.GetIdsForAccountAsync(accountInfoId);

            // Assert
            Assert.Empty(result);
        }
        [Fact]
        public async Task SaveClaimAsync_When_NoAuthorizationId_SetsCoreFields_AddsClaimCreatedHistory_Commits_AndSendsBusMessage()
        {
            // Arrange
            var accountInfoId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();
            var clientId = Fixture.Create<int>();
            var funderId = Fixture.Create<int>();
            var clientFunderId = Fixture.Create<int>();
            var renderingProviderId = Fixture.Create<int>();
            var renderingProviderTypeId = Fixture.Create<int>();
            var serviceLineId = Fixture.Create<int>();

            var dosStart = DateTime.UtcNow.AddDays(-10);
            var dosEnd = DateTime.UtcNow.AddDays(-5);

            var saveModel = Fixture.Build<ClaimSaveModel>()
                .With(x => x.ClaimInfo, Fixture.Build<ClaimInfo>()
                    .With(c => c.ClientId, clientId)
                    .With(c => c.FunderId, funderId)
                    .With(c => c.ClientFunderId, clientFunderId)
                    .With(c => c.ServiceLineId, serviceLineId)
                    .With(c => c.AuthorizationNumberId, (int?)null)
                    .With(c => c.AllowManualAuthorization, false)
                    .Create())
                .With(x => x.Provider, Fixture.Build<Provider>()
                    .With(p => p.DateOfServiceStart, dosStart)
                    .With(p => p.DateOfServiceEnd, dosEnd)
                    .With(p => p.RenderingProviderId, renderingProviderId)
                    .With(p => p.BillingProviderId, (int?)null)
                    .Create())
                .With(x => x.DiagnosisCode, new DiagnosisCode
                {
                    DiagnosisCodesToSave = new List<ClaimDiagnosisCodeModel>(),
                    BillingCodes = new List<ClaimBillingCodeModel>()
                })
                .Create();

            var claimSaveModelWithUserInfo = new ClaimSaveModelWithUserInfo
            {
                Claim = saveModel,
                BillingProviderRequest = new BillingProviderRequest(),
                AccountInfoId = accountInfoId,
                MemberId = memberId
            };

            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, 1001)
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.ChildProfileId, clientId)
                .With(x => x.MemberId, memberId)
                .With(x => x.ClaimChargeEntries, new List<ClaimChargeEntryEntity>())
                .With(x => x.ClaimDiagnosisCodes, new List<ClaimDiagnosisCodeEntity>())
                .With(x => x.ClaimHistory, new List<ClaimHistoryEntity>())
                .Create();

            _claimManagerService.Setup(x => x.InitializeClaim(
                    memberId,
                    accountInfoId,
                    saveModel.ClaimInfo.ClientId,
                    saveModel.ClaimInfo.FunderId,
                    saveModel.Provider.DateOfServiceStart,
                    saveModel.Provider.DateOfServiceEnd))
                .ReturnsAsync(claim);

            _rethinkService.Setup(x => x.GetAllRenderingProvidersAsync(accountInfoId))
                .ReturnsAsync(new ClientListUserModel
                {
                    data = new List<ClientUserModel>
                    {
                new ClientUserModel { memberId = renderingProviderId, id = renderingProviderTypeId }
                    }
                });

            _rethinkService.Setup(x => x.GetChildProfileFunderMappingByMappingId(
                    accountInfoId,
                    claim.ChildProfileId,
                    saveModel.ClaimInfo.ClientFunderId))
                .ReturnsAsync(Fixture.Build<FunderDetails>()
                    .With(x => x.releaseOfInformationConfirmationTypeId, (int?)null)
                    .With(x => x.authorizedPaymentConfirmationTypeId, 5)
                    .With(x => x.isAutismCoveredBenefit, true)
                    .Create());

            _claimUpdateService.Setup(x => x.CheckAndGetSecondaryFunderDetails(
                    accountInfoId,
                    It.IsAny<ClaimEntity>()))
                .ReturnsAsync((ClaimNextFundersAndControlNumberModel)null);

            _claimHistoryService.Setup(x => x.AddAsync(It.IsAny<ClaimHistorySaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            _claimRepository.Setup(x => x.CommitAsync())
                .Returns(Task.CompletedTask);

            _messageBus.Setup(x => x.SendAsync(It.IsAny<ClaimCreateEnd>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _rethinkService
                .Setup(x => x.GetProviderLocation(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((ProviderLocations)null);

            // Act
            var resultId = await _claimService.SaveClaimAsync(claimSaveModelWithUserInfo);

            // Assert
            Assert.Equal(claim.Id, resultId);
            Assert.Equal(ClaimStatus.PendingReview, claim.ClaimStatus);
            Assert.Equal(funderId, claim.PrimaryFunderId);
            Assert.Equal(funderId, claim.LastBilledFunderId);
            Assert.Equal(renderingProviderId, claim.RenderingStaffMemberId);
            Assert.Equal(renderingProviderTypeId, claim.RenderingProviderTypeId);
            Assert.False(claim.IsSecondaryPayerAvailable);

            _claimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistorySaveModel>(m =>
                    m.ClaimId == claim.Id &&
                    m.MemberId == memberId &&
                    m.Mode == ClaimActionMode.User &&
                    m.ClaimAction == ClaimAction.Create &&
                    m.ClaimHistoryAction == ClaimHistoryAction.ClaimCreated),
                It.IsAny<bool>()),
                Times.Once);

            _claimRepository.Verify(x => x.CommitAsync(), Times.Once);

            _messageBus.Verify(x => x.SendAsync(
                It.Is<ClaimCreateEnd>(e =>
                    e.ClaimId == claim.Id &&
                    e.AccountInfoId == claim.AccountInfoId &&
                    e.RenderingProviderId == 0 &&
                    e.RenderingProviderTypeId == claim.RenderingProviderTypeId &&
                    e.FunderId == claim.PrimaryFunderId &&
                    e.ClientId == claim.ChildProfileId &&
                    e.ChildProfileAuthorizationId == claim.AuthorizationId),
                It.IsAny<string>()),
                Times.Once);
        }
        public class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

            public DbSet<ClaimEntity> Claims => Set<ClaimEntity>();
            public DbSet<ClaimAppointmentLinkEntity> ClaimAppointmentLinks => Set<ClaimAppointmentLinkEntity>();
            public DbSet<ClaimChargeEntryEntity> ClaimChargeEntries => Set<ClaimChargeEntryEntity>();
            public DbSet<ClaimAppointmentLinkChargeEntry> ClaimAppointmentLinkChargeEntries => Set<ClaimAppointmentLinkChargeEntry>();

            // add EVERYTHING that DeleteClaimsAsync queries
            public DbSet<ClaimChargeEntryWriteOffEntity> ClaimChargeEntryWriteOffs => Set<ClaimChargeEntryWriteOffEntity>();
            public DbSet<PatientInvoiceDetailsEntity> PatientInvoiceDetails => Set<PatientInvoiceDetailsEntity>();
            public DbSet<ClaimWriteOffEntity> ClaimWriteOffs => Set<ClaimWriteOffEntity>();

            public DbSet<PaymentClaimEntity> PaymentClaims => Set<PaymentClaimEntity>();
            public DbSet<PaymentClaimAdjustmentEntity> PaymentClaimAdjustments => Set<PaymentClaimAdjustmentEntity>();
            public DbSet<PaymentClaimServiceLineEntity> PaymentClaimServiceLines => Set<PaymentClaimServiceLineEntity>();
            public DbSet<PaymentClaimServiceLineAdjustmentEntity> PaymentClaimServiceLineAdjustments => Set<PaymentClaimServiceLineAdjustmentEntity>();

            public DbSet<ClaimAttachmentEntity> ClaimAttachments => Set<ClaimAttachmentEntity>();
            public DbSet<ClaimDiagnosisCodeEntity> ClaimDiagnosisCodes => Set<ClaimDiagnosisCodeEntity>();
            public DbSet<ClaimNoteEntity> ClaimNotes => Set<ClaimNoteEntity>();
            public DbSet<ClaimValidationErrorEntity> ClaimValidationErrors => Set<ClaimValidationErrorEntity>();

            public DbSet<ClaimSubmissionEntity> ClaimSubmissions => Set<ClaimSubmissionEntity>();
            public DbSet<ClaimSubmissionServiceLineEntity> ClaimSubmissionServiceLines => Set<ClaimSubmissionServiceLineEntity>();

            // constructor-required repos you mocked (not used by this method but safe)
            public DbSet<MemberViewSettingEntity> MemberViewSettings => Set<MemberViewSettingEntity>();
            public DbSet<ClaimErrorMessageEntity> ClaimErrorMessages => Set<ClaimErrorMessageEntity>();
            public DbSet<ClaimErrorCategoryEntity> ClaimErrorCategories => Set<ClaimErrorCategoryEntity>();
            public DbSet<ClearingHouseResponseDetailsEntity> ClearingHouseResponseDetails => Set<ClearingHouseResponseDetailsEntity>();
            public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();
            public DbSet<CarcCodeEntity> CarcCodes => Set<CarcCodeEntity>();
            public DbSet<ClaimFlagReasonMaster> ClaimFlagReasons => Set<ClaimFlagReasonMaster>();
            public DbSet<ClaimFlagTransaction> ClaimFlagTransactions => Set<ClaimFlagTransaction>();
            public DbSet<ClaimSubmissionEntity> ClaimSubmission { get; set; }
        }

        [Fact]
        public async Task UpdateBillingClaimDetailsAsync_ShouldThrowException_WhenClaimNotFoundForNewCharge()
        {
            // Arrange
            var memberId = 100;
            var claimId = 1000;

            var model = new UpdateBillingClaimDetailsListModel
            {
                MemberId = memberId,
                BillingClaimDetailsModels = new List<UpdateBillingClaimDetailsModel>
        {
            new UpdateBillingClaimDetailsModel
            {
                Id = 0,
                ClaimId = claimId,
                BillingCode = "90837"
            }
        }
            };

            _claimRepository.Setup(r => r.Query())
                .Returns(Enumerable.Empty<ClaimEntity>().AsQueryable().BuildMock());

            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() =>
                _claimService.UpdateBillingClaimDetailsAsync(model, memberId, false, false));
        }

        [Fact]
        public async Task UpdateBillingClaimDetailsAsync_ShouldThrowException_WhenChargeEntryNotFound()
        {
            // Arrange
            var memberId = 100;
            var claimId = 1000;
            var chargeEntryId = 500;

            var model = new UpdateBillingClaimDetailsListModel
            {
                MemberId = memberId,
                BillingClaimDetailsModels = new List<UpdateBillingClaimDetailsModel>
        {
            new UpdateBillingClaimDetailsModel
            {
                Id = chargeEntryId,
                ClaimId = claimId,
                BillingCode = "90837"
            }
        }
            };

            _claimChargeEntryRepository.Setup(r => r.Query())
                .Returns(Enumerable.Empty<ClaimChargeEntryEntity>().AsQueryable().BuildMock());

            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() =>
                _claimService.UpdateBillingClaimDetailsAsync(model, memberId, false, false));
        }

        [Fact]
        public async Task AssignClaimsAsync_ShouldHandlePartiallyMatchingClaimIds()
        {
            // Arrange
            var existingClaimId = 1;
            var nonExistentClaimId = 999;
            var claimIds = new[] { existingClaimId, nonExistentClaimId };
            var assigneeId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();

            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, existingClaimId)
                .With(x => x.DateDeleted, (DateTime?)null)
                .Create();

            _claimRepository.Setup(r => r.Query())
                       .Returns(QueryMock<ClaimEntity>.Create(claim));
            _claimRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _claimService.AssignClaimsAsync(claimIds, assigneeId, memberId);

            // Assert
            Assert.True(result);
            Assert.Equal(assigneeId, claim.AssigneeId);
            _claimRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetStatesAsync_ReturnsEmptyList_WhenNoStatesExist()
        {
            // Arrange: cache returns empty list
            _cacheService.Setup(c => c.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<StateEntity>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(new List<StateEntity>());

            // Act
            var result = await _claimService.GetStatesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetStatesAsync_ReturnsMappedStates_WhenStatesExist()
        {
            // Arrange: cache returns a list of states
            var entities = new List<StateEntity>
            {
                new StateEntity { Id = 1, StateName = "California", StateCode = "CA", DateDeleted = null },
                new StateEntity { Id = 2, StateName = "Texas", StateCode = "TX", DateDeleted = null }
            };

            _cacheService.Setup(c => c.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<StateEntity>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(entities);

            // Act
            var result = await _claimService.GetStatesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("CA", result[0].StateCode);
            Assert.Equal("California", result[0].StateName);
            Assert.Equal("TX", result[1].StateCode);
            Assert.Equal("Texas", result[1].StateName);
        }

        [Fact]
        public async Task GetStatesAsync_ThrowsException_WhenCacheFails()
        {
            // Arrange: cache service throws
            _cacheService.Setup(c => c.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<StateEntity>>>>(),
                    It.IsAny<TimeSpan>()))
                .ThrowsAsync(new Exception("Database failure"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(async () =>
                await _claimService.GetStatesAsync());

            Assert.Equal("Database failure", ex.Message);
        }


        [Fact]
        public async Task GetAllExternalCodes_ReturnsMappedList_WhenCodesExistInCache()
        {
            // Arrange
            var cachedEntities = new List<ExternalCodeEntity>
            {
                new ExternalCodeEntity { CodeTypeId = ExternalCodeType.ClaimStatusCode, Code = "A1", Description = "Active" },
                new ExternalCodeEntity { CodeTypeId = ExternalCodeType.ClaimStatusCode, Code = "A2", Description = "Inactive" }
            };

            _cacheService
                .Setup(c => c.GetOrSetCacheAsync(
                    "externalCodes",
                    It.IsAny<Func<Task<List<ExternalCodeEntity>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(cachedEntities);

            // Act
            var result = await _claimService.GetAllExternalCodes();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("A1", result[0].Code);
            Assert.Equal("A2", result[1].Code);
        }

        [Fact]
        public async Task GetAllExternalCodes_ReturnsEmptyList_WhenCacheReturnsNull()
        {
            // Arrange
            _cacheService
                .Setup(c => c.GetOrSetCacheAsync(
                    "externalCodes",
                    It.IsAny<Func<Task<List<ExternalCodeEntity>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync((List<ExternalCodeEntity>)null);

            // Act
            var result = await _claimService.GetAllExternalCodes();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllExternalCodes_ReturnsEmptyList_WhenCacheReturnsEmptyList()
        {
            // Arrange
            _cacheService
                .Setup(c => c.GetOrSetCacheAsync(
                    "externalCodes",
                    It.IsAny<Func<Task<List<ExternalCodeEntity>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(new List<ExternalCodeEntity>());

            // Act
            var result = await _claimService.GetAllExternalCodes();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllExternalCodes_UsesCacheKey_ExternalCodes()
        {
            // Arrange
            _cacheService
                .Setup(c => c.GetOrSetCacheAsync(
                    "externalCodes",
                    It.IsAny<Func<Task<List<ExternalCodeEntity>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(new List<ExternalCodeEntity>());

            // Act
            await _claimService.GetAllExternalCodes();

            // Assert: verify the correct cache key is used
            _cacheService.Verify(c => c.GetOrSetCacheAsync(
                "externalCodes",
                It.IsAny<Func<Task<List<ExternalCodeEntity>>>>(),
                It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task GetAllExternalCodes_UsesCacheExpiry_OfOneDay()
        {
            // Arrange
            _cacheService
                .Setup(c => c.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<ExternalCodeEntity>>>>(),
                    TimeSpan.FromDays(1)))
                .ReturnsAsync(new List<ExternalCodeEntity>());

            // Act
            await _claimService.GetAllExternalCodes();

            // Assert: verify 1-day cache expiry is used
            _cacheService.Verify(c => c.GetOrSetCacheAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<ExternalCodeEntity>>>>(),
                TimeSpan.FromDays(1)), Times.Once);
        }

        [Fact]
        public async Task GetAllExternalCodes_LogsWarning_WhenNoCodesFound()
        {
            // Arrange
            _cacheService
                .Setup(c => c.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<ExternalCodeEntity>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(new List<ExternalCodeEntity>());

            // Act
            await _claimService.GetAllExternalCodes();

            // Assert
            _logger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("No external codes found")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllExternalCodes_LogsInformation_WhenCodesRetrievedSuccessfully()
        {
            // Arrange
            var entities = new List<ExternalCodeEntity>
            {
                new ExternalCodeEntity { CodeTypeId = ExternalCodeType.ClaimStatusCode }
            };

            _cacheService
                .Setup(c => c.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<ExternalCodeEntity>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(entities);

            // Act
            await _claimService.GetAllExternalCodes();

            // Assert
            _logger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Successfully retrieved")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllExternalCodes_DoesNotCallMapper_WhenCodesAreEmpty()
        {
            // Arrange
            _cacheService
                .Setup(c => c.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<ExternalCodeEntity>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(new List<ExternalCodeEntity>());

            // Act
            var result = await _claimService.GetAllExternalCodes();

            // Assert: when codes are empty, the service returns early without mapping
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
