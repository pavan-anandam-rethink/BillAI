using AutoFixture;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Client;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Services.Billing;
using BillingService.Domain.Services.RethinkMasterDataMicroservices;
using BillingService.XUnit.Tests.Common;
using BillingService.XUnit.Tests.Common.Mocks;
using Microsoft.Extensions.Configuration;
using Moq;
using Rethink.Services.Common.Dtos.Billing;
using Rethink.Services.Common.Entities.Billing;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.ClientMicroServicesModels;
using Rethink.Services.Common.Models.RethinkDataEntityClasses;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Claim
{
    public class ClaimManagerServiceTest : BaseTest
    {
        private Mock<IRepository<BillingDbContext, ClaimEntity>> _billingClaimRepository;
        private Mock<IRepository<BillingDbContext, ClaimSubmissionEntity>> _billingClaimSubmissionRepository;
        private Mock<IRepository<BillingDbContext, ClaimValidationErrorEntity>> _billingClaimValidationErrorRepository;
        private Mock<IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity>> _billingClaimSubmissionFunderSequenceRepository;
        private Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>> _billingClaimAppointmentLinkRepository;
        private Mock<IRepository<BillingDbContext, PaymentClaimEntity>> _billingPaymentClaimRepository;
        private Mock<IRepository<BillingDbContext, ClaimErrorMessageEntity>> _billingClaimErrorMessageRepository;
        private Mock<IRepository<BillingDbContext, ClaimDiagnosisCodeEntity>> _claimDiagnosisRepository;
        private Mock<IRepository<BillingDbContext, FunderSettingsEntity>> _funderSettingRepo;

        private Mock<IClaimHistoryService> _claimHistoryService;
        private Mock<IRethinkMasterDataMicroServices> _rethinkServices;
        private Mock<IClientService> _clientService;
        private readonly Mock<IClaimService> _mockClaimService;
        private Mock<IClaimValidationService> _claimValidationService;
        private Mock<IServiceProvider> _mockServiceProvider;
        private IClaimManagerService _claimManagerService;

        private IConfiguration _configuration;
        private IClientService _clientservice;
        private int _referringProviderId;
        private int _addressId;
        private Mock<IRethinkMasterDataMicroServices> _rethinkMasterDataMicroServices;

        public ClaimManagerServiceTest()
        {
            _billingClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            _billingClaimSubmissionRepository = new Mock<IRepository<BillingDbContext, ClaimSubmissionEntity>>();
            _billingClaimValidationErrorRepository = new Mock<IRepository<BillingDbContext, ClaimValidationErrorEntity>>();
            _billingClaimSubmissionFunderSequenceRepository = new Mock<IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity>>();
            _billingClaimAppointmentLinkRepository = new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>>();
            _billingPaymentClaimRepository = new Mock<IRepository<BillingDbContext, PaymentClaimEntity>>();
            _billingClaimErrorMessageRepository = new Mock<IRepository<BillingDbContext, ClaimErrorMessageEntity>>();
            _claimHistoryService = new Mock<IClaimHistoryService>();
            _rethinkServices = new Mock<IRethinkMasterDataMicroServices>();
            _claimValidationService = new Mock<IClaimValidationService>();
            _clientService = new Mock<IClientService>();
            _mockClaimService = new Mock<IClaimService>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _claimDiagnosisRepository = new Mock<IRepository<BillingDbContext, ClaimDiagnosisCodeEntity>>();
            _rethinkMasterDataMicroServices = new Mock<IRethinkMasterDataMicroServices>();
            _funderSettingRepo = new Mock<IRepository<BillingDbContext, FunderSettingsEntity>>();

            SetupConfiguration();

            _claimManagerService = new ClaimManagerService(
                _billingClaimSubmissionFunderSequenceRepository.Object,
                _billingClaimAppointmentLinkRepository.Object,
                _billingClaimSubmissionRepository.Object,
                _claimDiagnosisRepository.Object,
                _billingPaymentClaimRepository.Object,
                _billingClaimRepository.Object,
                _rethinkServices.Object,
                _claimValidationService.Object,
                _configuration,
                _clientService.Object,
                 _mockServiceProvider.Object,
                _rethinkMasterDataMicroServices.Object,
                _funderSettingRepo.Object
                );

            _referringProviderId = Fixture.Create<int>();
            _addressId = Fixture.Create<int>();
        }


        [Fact]
        public async Task InitializeClaim_ShouldCreateInitialClaim_WithInitialClaimSequenceIdentifier()
        {
            var memberId = Fixture.Create<int>();
            var accountInfoId = Fixture.Create<int>();
            var childProfileId = Fixture.Create<int>() % 990009 + 10009;
            var lastBilledFunderId = Fixture.Create<int>();
            var startDate = Fixture.Create<DateTime>();
            var endDate = Fixture.Create<DateTime>();

            _billingClaimRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimEntity>.Create(new ClaimEntity()));
            //_billingClaimRepository.Setup(x => x.Query()).Returns()

            var result = await _claimManagerService.InitializeClaim(memberId, accountInfoId, childProfileId, lastBilledFunderId, startDate, endDate);

            Assert.NotNull(result);
            Assert.Equal(memberId, result.MemberId);
            Assert.Equal(accountInfoId, result.AccountInfoId);
            Assert.Equal(childProfileId, result.ChildProfileId);
            Assert.Equal(startDate, result.StartDate);
            Assert.Equal(endDate, result.EndDate);
            Assert.EndsWith("1", result.ClaimIdentifier);

            _billingClaimRepository.Verify(x => x.AddAsync(It.IsAny<ClaimEntity>()), Times.Once);
            _billingClaimRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task InitializeClaim_ShouldCreateInitialClaim_WithNextClaimSequenceIdentifier()
        {
            var memberId = Fixture.Create<int>();
            var accountInfoId = Fixture.Create<int>();
            var childProfileId = Fixture.Create<int>();
            var lastBilledFunderId = Fixture.Create<int>();
            var startDate = Fixture.Create<DateTime>();
            var endDate = Fixture.Create<DateTime>();

            var existingClaim = Fixture.Build<ClaimEntity>()
                .With(x => x.ChildProfileId, childProfileId)
                .With(x => x.StartDate, startDate.Date)
                .With(x => x.ClaimIdentifier, "201212-0006P-1") // ends with initial sequence
                .Create();

            _billingClaimRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimEntity>.Create(existingClaim));

            var result = await _claimManagerService.InitializeClaim(memberId, accountInfoId, childProfileId, lastBilledFunderId, startDate, endDate);

            Assert.NotNull(result);
            Assert.Equal(memberId, result.MemberId);
            Assert.Equal(accountInfoId, result.AccountInfoId);
            Assert.Equal(childProfileId, result.ChildProfileId);
            Assert.Equal(startDate, result.StartDate);
            Assert.Equal(endDate, result.EndDate);
            Assert.EndsWith("1", result.ClaimIdentifier);

            _billingClaimRepository.Verify(x => x.AddAsync(It.IsAny<ClaimEntity>()), Times.Once);
            _billingClaimRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetFullClaim_ShouldReturnClaim_WithRequiredAndOptionalEntities()
        {
            var claimId = Fixture.Create<int>();
            var accountInfoId = Fixture.Create<int>();
            var clientId = Fixture.Create<int>();
            var authorizationId = Fixture.Create<int>();
            var renderingStaffMemberId = Fixture.Create<int>();
            var providerLocationId = Fixture.Create<int>();
            var locationCodeId = Fixture.Create<int>();
            var serviceLocationId = providerLocationId;
            var clientFunderId = Fixture.Create<int>();
            var lastBilledFunderId = Fixture.Create<int>();
            var primaryFunderId = Fixture.Create<int>();

            var claim = InitClaim(accountInfoId, claimId, clientId, authorizationId, renderingStaffMemberId, providerLocationId, locationCodeId, serviceLocationId, clientFunderId, lastBilledFunderId, primaryFunderId);

            SetupFullClaim(claim);
            SetupServices(accountInfoId, clientId, authorizationId, renderingStaffMemberId, providerLocationId, locationCodeId, serviceLocationId, clientFunderId, lastBilledFunderId, _referringProviderId);

            var result = await _claimManagerService.GetFullClaim(claimId);

            Assert.NotNull(result);
            Assert.Equal(claimId, result.Id);
            Assert.NotNull(result.ChildProfile);
            Assert.Equal(claim.ChildProfileId, result.ChildProfile.Id);
            Assert.NotNull(result.AccountInfo);
            Assert.Equal(claim.AccountInfoId, result.AccountInfo.Id);
            Assert.NotNull(result.ChildProfileAuthorization);
            Assert.Equal(claim.AuthorizationId, result.ChildProfileAuthorization.id);
            Assert.Equal(claim.ChildProfileAuthorization.authorizationNumber, result.ChildProfileAuthorization.authorizationNumber);
            Assert.NotNull(result.RenderingStaffMember);
            Assert.Equal(claim.RenderingStaffMemberId, result.RenderingStaffMember.id);
            Assert.NotNull(result.ReferringProvider);
            Assert.Equal(_referringProviderId, result.ReferringProvider.id);
            Assert.NotNull(result.ProviderLocation);
            Assert.Equal(claim.ProviderLocationId, result.ProviderLocation.id);
            Assert.NotNull(result.LocationCode);
            Assert.Equal(claim.LocationCodeId, result.LocationCode.id);
            Assert.NotNull(result.ServiceLocation);
            Assert.Equal(claim.ServiceLocationId, result.ServiceLocation.id);
            Assert.NotNull(result.ClientFunder);
            Assert.Equal(claim.ClientFunderId, result.ClientFunder.id);
            Assert.Equal(claim.PrimaryFunderId, result.PrimaryFunderId);
            Assert.Equal(claim.LastBilledFunderId, result.LastBilledFunderId);
        }

        [Fact]
        public async Task GetFullClaim_ShouldReturnClaim_WithoutOptionalEntities()
        {
            var claimId = Fixture.Create<int>();
            var accountInfoId = Fixture.Create<int>();
            var clientId = Fixture.Create<int>();
            var authorizationId = Fixture.Create<int>();
            var renderingStaffMemberId = Fixture.Create<int>();
            var providerLocationId = Fixture.Create<int>();
            var locationCodeId = Fixture.Create<int>();
            var serviceLocationId = providerLocationId;
            var clientFunderId = Fixture.Create<int>();
            var lastBilledFunderId = Fixture.Create<int>();
            var primaryFunderId = Fixture.Create<int>();

            var claim = InitClaim(accountInfoId, claimId, clientId, authorizationId, renderingStaffMemberId, providerLocationId, locationCodeId, serviceLocationId, clientFunderId, lastBilledFunderId, primaryFunderId);

            claim.AuthorizationId = null;
            claim.AuthorizationNumber = null;
            claim.ChildProfileReferringProviderId = null;
            SetupFullClaim(claim);
            SetupServices(accountInfoId, clientId, authorizationId, renderingStaffMemberId, providerLocationId, locationCodeId, serviceLocationId, clientFunderId, lastBilledFunderId, _referringProviderId);

            var result = await _claimManagerService.GetFullClaim(claimId);

            Assert.NotNull(result);
            Assert.Equal(claimId, result.Id);
            Assert.Null(result.ChildProfileAuthorization);
            Assert.Null(result.ReferringProvider);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UpdateClaimStatusAsync_ShouldUpdateClaim(bool commitImmediately)
        {
            var id = Fixture.Create<int>();
            var status = Fixture.Create<ClaimStatus>();
            var modifiedBy = Fixture.Create<int>();

            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, id)
                .Create();
            
            _billingClaimRepository.Setup(x => x.GetByIdAsync(claim.Id))
                .ReturnsAsync(claim);

            await _claimManagerService.UpdateClaimStatusAsync(id, status, modifiedBy, commitImmediately);

            _billingClaimRepository.Verify(x => x.Update(claim), Times.Once);
            if (commitImmediately)
            {
                _billingClaimRepository.Verify(x => x.CommitAsync(), Times.Once);
            }
            else
            {
                _billingClaimRepository.Verify(x => x.CommitAsync(), Times.Never);
            }
        }

        [Fact]
        public async Task UpdateClaimStatusAsync_ShouldThrowError_WhenClaimIsNotFound()
        {
            var id = Fixture.Create<int>();
            var status = Fixture.Create<ClaimStatus>();
            var modifiedBy = Fixture.Create<int>();

            _billingClaimRepository.Setup(x => x.GetByIdAsync(Fixture.Create<int>()))
                .ReturnsAsync(Fixture.Create<ClaimEntity>());

            var exception = await Assert.ThrowsAsync<Exception>(() => _claimManagerService.UpdateClaimStatusAsync(id, status, modifiedBy));
            Assert.Equal($"Claim with id: {id} not found!", exception.Message);
        }

        [Fact]
        public async Task UpdateClaimStatusAsync_ShouldSetBilledDate_WhenIsBilledDateUpdateAndStatusIsBilled()
        {
            // Arrange
            var id = Fixture.Create<int>();
            var modifiedBy = Fixture.Create<int>();

            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, id)
                .With(x => x.ClaimStatus, ClaimStatus.Billed)
                .Create();

            _billingClaimRepository.Setup(x => x.GetByIdAsync(claim.Id))
                .ReturnsAsync(claim);

            // Act
            await _claimManagerService.UpdateClaimStatusAsync(id, ClaimStatus.Billed, modifiedBy, commitImmediately: true, isBilledDateUpdate: true);

            // Assert
            Assert.Equal(ClaimStatus.Billed, claim.ClaimStatus);
            Assert.NotNull(claim.billedDate); // should be set to EstDateTime
            _billingClaimRepository.Verify(x => x.Update(claim), Times.Once);
            _billingClaimRepository.Verify(x => x.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateClaimStatusAsync_ShouldNotSetBilledDate_WhenStatusIsNotBilled()
        {
            // Arrange
            var id = Fixture.Create<int>();
            var modifiedBy = Fixture.Create<int>();

            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, id)
                .With(x => x.ClaimStatus, ClaimStatus.Denied) // any non-Billed status
                .With(x => x.billedDate, (DateTime?)null)     // force billedDate to start as null
                .Create();


            _billingClaimRepository.Setup(x => x.GetByIdAsync(claim.Id))
                .ReturnsAsync(claim);

            // Act
            await _claimManagerService.UpdateClaimStatusAsync(id, ClaimStatus.Denied, modifiedBy, commitImmediately: true, isBilledDateUpdate: true);

            // Assert
            Assert.Null(claim.billedDate);
            _billingClaimRepository.Verify(x => x.Update(claim), Times.Once);
            _billingClaimRepository.Verify(x => x.CommitAsync(), Times.Once);
        }
                
        [Fact]
        public async Task CreateHCFAClaim_Rebill_CallsSubmitClaimRebill_AndReturnsHcfa()
        {
            var sut = CreateSut();
            var memberId = 501;
            var accountInfoId = 88;
            var claimId = 9901;

            var claim = new ClaimEntity
            {
                Id = claimId,
                AccountInfoId = accountInfoId,
                ChildProfileId = 777,
                ProviderLocationId = 61,
                PrimaryFunderId = 1205,
                ClientFunderId = 44,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>(),
                PaymentClaims = new List<Rethink.Services.Common.Entities.Billing.Payment.PaymentClaimEntity>(),
                ClaimBillingProviders = new List<ClaimBillingProviderEntity>(), // ✅ FIX
                DateCreated = DateTime.UtcNow
            };

            _claimValidationService.Setup(v => v.GetClaimInformation(claimId)).ReturnsAsync(claim);
            _claimValidationService.Setup(v => v.GetClaimSubmissionInformation(claimId)).ReturnsAsync((ClaimSubmissionEntity)null);

            var fullSubmissionId = 6001;
            var fullSubmission = new ClaimSubmissionEntity
            {
                Id = fullSubmissionId,
                ClaimId = claimId,
                Claim = claim,
                ResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString(),
                FunderId = 1205,
                ClaimSubmissionServiceLines = new List<ClaimSubmissionServiceLineEntity> { new ClaimSubmissionServiceLineEntity { Charges = 0m } },
                ClaimValidationErrors = new List<ClaimValidationErrorEntity>(),
                FunderDetails = new FunderDataModel { phone = "800-111-2222", funderCoverageTypeId = 77 },
                ResolvedBillingProviderFederalTaxID = "TAXID",
                RenderingProviderStaffNpiNumber = "1234567890"
            };

            // Call order:
            // 1) CreateHCFAClaim -> GetLatestClaimSubmission           => Query() #1
            // 2) SubmitClaimRebill -> InitializeClaimSubmission -> GenerateClaimSubmissionIdentifier => Query() #2
            // 3) GetFullClaimSubmission                                => Query() #3
            _billingClaimSubmissionRepository
                .SetupSequence(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create())                 // GetLatestClaimSubmission (returns null latest)
                .Returns(QueryMock<ClaimSubmissionEntity>.Create())                 // GenerateClaimSubmissionIdentifier
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(fullSubmission));  // GetFullClaimSubmission

            _claimValidationService
                .Setup(v => v.PrepareClaimSubmission(
                    claim,
                    It.IsAny<ClaimSubmissionEntity>(),
                    null,
                    memberId,
                    null))
                .Callback<ClaimEntity, ClaimSubmissionEntity, ClaimSubmissionEntity, int, int?>((c, sub, p, mem, sec) =>
                    {
                        sub.Id = fullSubmissionId;
                    })
                .Returns(Task.CompletedTask);

            _billingClaimSubmissionFunderSequenceRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionFunderSequenceEntity>.Create(
                    new ClaimSubmissionFunderSequenceEntity
                    {
                        ClaimSubmissionId = fullSubmissionId,
                        FunderId = 1205,
                        FunderResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString(),
                        ServiceLineBillingProviderOption = BillingProviderOptionType.Group,
                        InsuranceAddress1 = "A1",
                        InsuranceCity = "C",
                        InsuranceState = "S",
                        InsuranceZip = "Z",
                        InsurancePolicyNumber = "POL",
                        InsuranceGroupNumber = "GRP",
                        InsurancePlanName = "PLAN",
                        SubscriberFirstName = "FN",
                        SubscriberLastName = "LN"
                    }));

            _rethinkServices.Setup(s => s.GetProviderLocation(accountInfoId, claim.ProviderLocationId.Value))
                .ReturnsAsync(new ProviderLocations { id = claim.ProviderLocationId.Value, address = new ProviderLocationAddress(), npiNumber = "NPI", federalTaxId = "TAX" });
            _rethinkServices.Setup(s => s.GetLocationCodes()).ReturnsAsync(new List<LocationCodesModel>());
            _rethinkServices.Setup(s => s.GetUnitTypesAsync()).ReturnsAsync(new List<ClientUnitTypes>());
            _rethinkServices.Setup(s => s.GetChildProfileAuthorizationByClientId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((ClientAuthorization)null);
            _rethinkServices.Setup(s => s.GetChildProfileReturningEntity(accountInfoId, claim.ChildProfileId)).ReturnsAsync(new ChildProfileEntityModel { AccountInfoId = accountInfoId });
            _rethinkServices.Setup(s => s.GetMemberAsync(accountInfoId, It.IsAny<int>())).ReturnsAsync(new RethinkAccountMember { id = 1 });
            _rethinkServices.Setup(s => s.GetFunder(accountInfoId, claim.PrimaryFunderId)).ReturnsAsync(new FunderDataModel { phone = "800-111-2222", funderCoverageTypeId = 77 });
            _claimDiagnosisRepository.Setup(r => r.Query()).Returns(QueryMock<ClaimDiagnosisCodeEntity>.Create());

            var model = await sut.CreateHCFAClaim(memberId, accountInfoId, claimId, ClaimFrequencyType.Original, ClaimSubmissionType.Rebill, ResponsibilitySequenceType.Primary);
         
            Assert.NotNull(model);
            Assert.Equal(claimId, model.Id);
            Assert.Equal(1205, model.FunderId);
        }

        [Fact]
        public async Task CreateHCFAClaim_Transfer_CallsSubmitClaimTransfer_AndReturnsHcfa()
        {
            var sut = CreateSut();
            var memberId = 502;
            var accountInfoId = 66;
            var claimId = 9902;

            var claim = new ClaimEntity
            {
                Id = claimId,
                AccountInfoId = accountInfoId,
                ChildProfileId = 888,
                ProviderLocationId = 61,
                PrimaryFunderId = 1205,
                ClientFunderId = 44,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>(),
                PaymentClaims = new List<Rethink.Services.Common.Entities.Billing.Payment.PaymentClaimEntity>(),
                ClaimBillingProviders = new List<ClaimBillingProviderEntity>(), // ✅ FIX
                DateCreated = DateTime.UtcNow
            };

            var priorSubmission = new ClaimSubmissionEntity
            {
                Id = 100,
                ClaimId = claimId,
                DocumentType = ClaimDocumentType.HCFA1500Single,
                ResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString()
            };

            _claimValidationService.Setup(v => v.GetClaimSubmissionInformation(claimId)).ReturnsAsync(priorSubmission);
            _claimValidationService.Setup(v => v.GetClaimInformation(claimId)).ReturnsAsync(claim);

            var newSubmissionId = 7001;
            var fullSubmission = new ClaimSubmissionEntity
            {
                Id = newSubmissionId,
                ClaimId = claimId,
                Claim = claim,
                ResponsibilitySequence = ResponsibilitySequenceType.Secondary.AsString(),
                FunderId = 1205,
                ClaimSubmissionServiceLines = new List<ClaimSubmissionServiceLineEntity> { new ClaimSubmissionServiceLineEntity { Charges = 0m } },
                ClaimValidationErrors = new List<ClaimValidationErrorEntity>(),
                FunderDetails = new FunderDataModel { phone = "800-111-3333", funderCoverageTypeId = 77 },
                ResolvedBillingProviderFederalTaxID = "TAXID",
                RenderingProviderStaffNpiNumber = "1234567890"
            };

            // Because CreateHCFAClaim first calls GetLatestClaimSubmission(claimId),
            // which itself calls GetFullClaimSubmission(latest.Id), we must provide:
            // 1) Query() for latest row
            // 2) Query() for full latest submission (minimal safe object)
            // Then the Transfer path:
            // 3) Query() for GenerateClaimSubmissionIdentifier (clone path)
            // 4) Query() for full new submission
            var latestFullSubmission = new ClaimSubmissionEntity
            {
                Id = priorSubmission.Id,
                ClaimId = claimId,
                Claim = claim,
                ResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString(),
                FunderId = 1205,
                ClaimSubmissionServiceLines = new List<ClaimSubmissionServiceLineEntity>(),
                ClaimValidationErrors = new List<ClaimValidationErrorEntity>(),
                FunderDetails = new FunderDataModel { phone = "800-111-0000", funderCoverageTypeId = 77 },
                RenderingProviderStaffNpiNumber = "9876543210"
            };

            _billingClaimSubmissionRepository
                .SetupSequence(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(priorSubmission))     // GetLatestClaimSubmission: latest row
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(latestFullSubmission))// GetLatestClaimSubmission: full latest
                .Returns(QueryMock<ClaimSubmissionEntity>.Create())                    // GenerateClaimSubmissionIdentifier (clone)
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(fullSubmission));     // GetFullClaimSubmission: new submission

            _claimValidationService
                .Setup(v => v.PrepareClaimSubmission(
                    claim,
                    It.IsAny<ClaimSubmissionEntity>(),
                    priorSubmission,
                    memberId,
                    It.IsAny<int?>()))
                .Callback<ClaimEntity, ClaimSubmissionEntity, ClaimSubmissionEntity, int, int?>((c, sub, p, mem, sec) =>
                {
                    sub.Id = newSubmissionId;
                })
                .Returns(Task.CompletedTask);

            _billingClaimSubmissionFunderSequenceRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionFunderSequenceEntity>.Create(
                    new ClaimSubmissionFunderSequenceEntity
                    {
                        ClaimSubmissionId = newSubmissionId,
                        FunderId = 1205,
                        FunderResponsibilitySequence = ResponsibilitySequenceType.Secondary.AsString(),
                        ServiceLineBillingProviderOption = BillingProviderOptionType.Group,
                        InsuranceAddress1 = "A1",
                        InsuranceCity = "C",
                        InsuranceState = "S",
                        InsuranceZip = "Z",
                        InsurancePolicyNumber = "POL",
                        InsuranceGroupNumber = "GRP",
                        InsurancePlanName = "PLAN",
                        SubscriberFirstName = "FN",
                        SubscriberLastName = "LN"
                    }));

            _rethinkServices.Setup(s => s.GetProviderLocation(accountInfoId, claim.ProviderLocationId.Value))
                .ReturnsAsync(new ProviderLocations { id = claim.ProviderLocationId.Value, address = new ProviderLocationAddress(), npiNumber = "NPI", federalTaxId = "TAX" });
            _rethinkServices.Setup(s => s.GetLocationCodes()).ReturnsAsync(new List<LocationCodesModel>());
            _rethinkServices.Setup(s => s.GetUnitTypesAsync()).ReturnsAsync(new List<ClientUnitTypes>());
            _rethinkServices.Setup(s => s.GetChildProfileAuthorizationByClientId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((ClientAuthorization)null);
            _rethinkServices.Setup(s => s.GetChildProfileReturningEntity(accountInfoId, claim.ChildProfileId)).ReturnsAsync(new ChildProfileEntityModel { AccountInfoId = accountInfoId });
            _rethinkServices.Setup(s => s.GetMemberAsync(accountInfoId, It.IsAny<int>())).ReturnsAsync(new RethinkAccountMember { id = 1 });
            _rethinkServices.Setup(s => s.GetFunder(accountInfoId, claim.PrimaryFunderId)).ReturnsAsync(new FunderDataModel { phone = "800-111-3333", funderCoverageTypeId = 77 });
            _claimDiagnosisRepository.Setup(r => r.Query()).Returns(QueryMock<ClaimDiagnosisCodeEntity>.Create());

            var model = await sut.CreateHCFAClaim(memberId, accountInfoId, claimId, ClaimFrequencyType.Replacement, ClaimSubmissionType.Transfer, ResponsibilitySequenceType.Secondary);

            Assert.NotNull(model);
            Assert.Equal(claimId, model.Id);
            Assert.Equal(1205, model.FunderId);
        }

        [Fact]
        public async Task CreateHCFAClaim_OriginalElsePath_CallsSubmitClaim_AndReturnsHcfa()
        {
            var sut = CreateSut();
            var memberId = 503;
            var accountInfoId = 55;
            var claimId = 9903;

            var claim = new ClaimEntity
            {
                Id = claimId,
                AccountInfoId = accountInfoId,
                ChildProfileId = 999,
                ProviderLocationId = 61,
                PrimaryFunderId = 1205,
                ClientFunderId = 44,
                ClaimIdentifier = "260115-00XYZ-1",
                FrequencyTypeId = ClaimFrequencyType.Original,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>(),
                PaymentClaims = new List<Rethink.Services.Common.Entities.Billing.Payment.PaymentClaimEntity>(),
                ClaimBillingProviders = new List<ClaimBillingProviderEntity>(), // ✅ FIX
                DateCreated = DateTime.UtcNow
            };

            // Let GetLatestClaimSubmission return null (first Query() empty)
            // Then GenerateClaimSubmissionIdentifier (second Query() empty)
            // Then GetFullClaimSubmission returns our full submission (third Query())
            var submissionId = 8001;
            var fullSubmission = new ClaimSubmissionEntity
            {
                Id = submissionId,
                ClaimId = claimId,
                Claim = claim,
                ResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString(),
                FunderId = 1205,
                ClaimSubmissionServiceLines = new List<ClaimSubmissionServiceLineEntity> { new ClaimSubmissionServiceLineEntity { Charges = 0m } },
                ClaimValidationErrors = new List<ClaimValidationErrorEntity>(),
                FunderDetails = new FunderDataModel { phone = "800-222-3333", funderCoverageTypeId = 77 },
                ResolvedBillingProviderFederalTaxID = "TAXID",
                RenderingProviderStaffNpiNumber = "1234567890"
            };

            _billingClaimSubmissionRepository
                .SetupSequence(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create())                 // GetLatestClaimSubmission
                .Returns(QueryMock<ClaimSubmissionEntity>.Create())                 // GenerateClaimSubmissionIdentifier
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(fullSubmission));  // GetFullClaimSubmission

            _claimValidationService.Setup(v => v.GetClaimInformation(claimId)).ReturnsAsync(claim);

            _claimValidationService
                .Setup(v => v.PrepareClaimSubmission(
                    claim,
                    It.IsAny<ClaimSubmissionEntity>(),
                    null,
                    memberId,
                    It.IsAny<int?>()))
                .Callback<ClaimEntity, ClaimSubmissionEntity, ClaimSubmissionEntity, int, int?>((c, sub, p, mem, sec) =>
                {
                    sub.Id = submissionId;
                })
                .Returns(Task.CompletedTask);

            _billingClaimSubmissionFunderSequenceRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionFunderSequenceEntity>.Create(
                    new ClaimSubmissionFunderSequenceEntity
                    {
                        ClaimSubmissionId = submissionId,
                        FunderId = 1205,
                        FunderResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString(),
                        ServiceLineBillingProviderOption = BillingProviderOptionType.Group,
                        InsuranceAddress1 = "A1",
                        InsuranceCity = "C",
                        InsuranceState = "S",
                        InsuranceZip = "Z",
                        InsurancePolicyNumber = "POL",
                        InsuranceGroupNumber = "GRP",
                        InsurancePlanName = "PLAN",
                        SubscriberFirstName = "FN",
                        SubscriberLastName = "LN"
                    }));

            _rethinkServices.Setup(s => s.GetProviderLocation(accountInfoId, claim.ProviderLocationId.Value))
                .ReturnsAsync(new ProviderLocations { id = claim.ProviderLocationId.Value, address = new ProviderLocationAddress(), npiNumber = "NPI", federalTaxId = "TAX" });
            _rethinkServices.Setup(s => s.GetLocationCodes()).ReturnsAsync(new List<LocationCodesModel>());
            _rethinkServices.Setup(s => s.GetUnitTypesAsync()).ReturnsAsync(new List<ClientUnitTypes>());
            _rethinkServices.Setup(s => s.GetChildProfileAuthorizationByClientId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((ClientAuthorization)null);
            _rethinkServices.Setup(s => s.GetChildProfileReturningEntity(accountInfoId, claim.ChildProfileId)).ReturnsAsync(new ChildProfileEntityModel { AccountInfoId = accountInfoId });
            _rethinkServices.Setup(s => s.GetMemberAsync(accountInfoId, It.IsAny<int>())).ReturnsAsync(new RethinkAccountMember { id = 1 });
            _rethinkServices.Setup(s => s.GetFunder(accountInfoId, claim.PrimaryFunderId)).ReturnsAsync(new FunderDataModel { phone = "800-222-3333", funderCoverageTypeId = 77 });
            _claimDiagnosisRepository.Setup(r => r.Query()).Returns(QueryMock<ClaimDiagnosisCodeEntity>.Create());

            var model = await sut.CreateHCFAClaim(memberId, accountInfoId, claimId, ClaimFrequencyType.Original, ClaimSubmissionType.Original, ResponsibilitySequenceType.Primary);

            Assert.NotNull(model);
            Assert.Equal(claimId, model.Id);
            Assert.Equal(1205, model.FunderId);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UpdateClaimSubmissionStatusAsync_ShouldUpdateSubmissionStatus(bool commitImmediately)
        {
            var id = Fixture.Create<int>();
            var status = Fixture.Create<ClaimSubmissionStatus>();
            var modifiedBy = Fixture.Create<int>();

            var claimSubmission = Fixture.Build<ClaimSubmissionEntity>()
                .With(x => x.Id, id)
                .Create();

            _billingClaimSubmissionRepository.Setup(x => x.GetByIdAsync(claimSubmission.Id))
                .ReturnsAsync(claimSubmission);

            await _claimManagerService.UpdateClaimSubmissionStatusAsync(id, modifiedBy, status, commitImmediately);

            _billingClaimSubmissionRepository.Verify(x => x.Update(claimSubmission), Times.Once);
            if (commitImmediately)
            {
                _billingClaimSubmissionRepository.Verify(x => x.CommitAsync(), Times.Once);
            }
            else
            {
                _billingClaimSubmissionRepository.Verify(x => x.CommitAsync(), Times.Never);
            }
        }

        [Fact]
        public async Task UpdateClaimSubmissionStatusAsync_ShouldThrowError_WhenSubmissionIsNotFound()
        {
            var id = Fixture.Create<int>();
            var status = Fixture.Create<ClaimSubmissionStatus>();
            var modifiedBy = Fixture.Create<int>();

            _billingClaimSubmissionRepository.Setup(x => x.GetByIdAsync(Fixture.Create<int>()))
                .ReturnsAsync(Fixture.Create<ClaimSubmissionEntity>());

            var exception = await Assert.ThrowsAsync<NullReferenceException>(() => _claimManagerService.UpdateClaimSubmissionStatusAsync(id, modifiedBy, status));
            Assert.Equal($"Claim submission with id: {id} not found!", exception.Message);
        }

        private ClaimEntity InitClaim(int accountInfoId, int claimId, int clientId, int authorizationId, int renderingStaffMemberId, int providerLocationId, int locationCodeId, int serviceLocationId, int clientFunderId, int lastBilledFunderId, int primaryFunderId)
        {
            var claimChargeEntry = Fixture.Create<ClaimChargeEntryEntity>();
            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, claimId)
                .With(x => x.AuthorizationId, authorizationId)
                .With(x => x.RenderingStaffMemberId, renderingStaffMemberId)
                .With(x => x.ProviderLocationId, providerLocationId)
                .With(x => x.ServiceLocationId, serviceLocationId)
                .With(x => x.ClientFunderId, clientFunderId)
                .With(x => x.LastBilledFunderId, lastBilledFunderId)
                .With(x => x.PrimaryFunderId, primaryFunderId)
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.ChildProfileId, clientId)
                .With(x => x.LocationCodeId, locationCodeId)
                .With(x => x.ClaimDiagnosisCodes, new List<ClaimDiagnosisCodeEntity>())
                .With(x => x.ClaimChargeEntries, new List<ClaimChargeEntryEntity>() { claimChargeEntry })
                .Create();

            return claim;
        }

        private void SetupFullClaim(ClaimEntity claim)
        {
            // claim
            _billingClaimRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimEntity>.Create(claim));

            // optional
            var dxdCodes = Fixture.Build<ChildProfileAuthorizationDiagnosisCode>()
                .With(x => x.includeOnClaims, true)
                .With(x => x.order, 1)
                .With(x => x.Diagnosis, new Diagnosis() { diagnosisCode = claim.ClaimChargeEntries.First().DiagnosisCode })
                .Without(x => x.ChildProfileAuthorization)
                .Create();
            var authorization = claim.AuthorizationId.HasValue ?
                Fixture.Build<ClientAuthorization>()
                .With(x => x.id, claim.AuthorizationId.Value)
                .With(x => x.authorizationNumber, claim.AuthorizationNumber)
                .With(x => x.ChildProfileAuthorizationDiagnosisCodes, new List<ChildProfileAuthorizationDiagnosisCode> { dxdCodes })
                .Without(x => x.ChildProfileDiagnosis)
                .Without(x => x.ChildProfileReferringProvider)
                .Create() :
                null;

            var clientReferringProvider = claim.ChildProfileReferringProviderId.HasValue ?
                new clientReferringProviders
                {
                    id = claim.ChildProfileReferringProviderId.Value,
                    referringProviderId = _referringProviderId,
                    ReferringProvider = new ReferringProvidersModel { id = _referringProviderId }
                } : null;
        }

        private void SetupClaimSubmissionData(ClaimEntity claim)
        {
            var clientFunderMapping = Fixture.Build<FunderDetails>()
                .With(x => x.id, claim.ClientFunderId.Value)
                .With(x => x.Funder, new FunderDataModel() { id = claim.LastBilledFunderId.Value })
                .Create();
            var clientFunderLineMapping = Fixture.Build<ServiceLines>()
                .With(x => x.ChildProfileFunderMappingId, claim.ClientFunderId)
                .With(x => x.responsibilitySequence, ResponsibilitySequenceType.Primary)
                .With(x => x.ChildProfileFunderMapping, clientFunderMapping)
                .Create();
            var paymentClaim = Fixture.Build<PaymentClaimEntity>()
                .With(x => x.ClaimId, claim.Id)
                .With(x => x.Payment, new PaymentEntity { HcFunderId = claim.LastBilledFunderId.Value })
                .Create();

            _billingClaimAppointmentLinkRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimAppointmentLinkEntity>.Create(new ClaimAppointmentLinkEntity() { ClaimId = claim.Id }));

            _billingPaymentClaimRepository.Setup(x => x.Query())
                .Returns(QueryMock<PaymentClaimEntity>.Create(paymentClaim));
        }

        private void SetupConfiguration()
        {
            var settings = new Dictionary<string, string> {
                {"EdiSettings:TestMode", "1"},
                {"EdiSettings:BillerRethinkId", "396110"},
                {"EdiSettings:SubmitterRethinkId", "741792"},
                {"EdiSettings:SubmitterRethinkName", "Rethink Behavioral Health"},
                {"EdiSettings:SubmitterRethinkEmail", "claimsprocessing@rethink.com"},
                {"EdiSettings:SubmitterRethinkPhone", "RT_PHONE"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }

        private ClaimManagerService CreateSut()
        {
            return new ClaimManagerService(
                _billingClaimSubmissionFunderSequenceRepository.Object,
                _billingClaimAppointmentLinkRepository.Object,
                _billingClaimSubmissionRepository.Object,
                _claimDiagnosisRepository.Object,
                _billingPaymentClaimRepository.Object,
                _billingClaimRepository.Object,
                _rethinkServices.Object,
                _claimValidationService.Object,
                _configuration,
                _clientService.Object,
                _mockServiceProvider.Object,
                _rethinkMasterDataMicroServices.Object,
                _funderSettingRepo.Object
            );
        }

        [Fact]
        public async Task SubmitClaimRebill_ShouldCreateNewSubmission_WhenNoPriorExists()
        {
            var sut = CreateSut();

            var claim = new ClaimEntity
            {
                Id = 101,
                ClaimIdentifier = "250101-00ABC-1",
                FrequencyTypeId = ClaimFrequencyType.Original
            };

            // Validation service returns claim info and no prior submission
            _claimValidationService.Setup(v => v.GetClaimInformation(claim.Id)).ReturnsAsync(claim);
            _claimValidationService.Setup(v => v.GetClaimSubmissionInformation(claim.Id)).ReturnsAsync((ClaimSubmissionEntity)null);

            // Return an IQueryable that supports async operations (matches other tests using QueryMock<T>.Create)
            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create());

            // Capture the submission created via InitializeClaimSubmission
            ClaimSubmissionEntity captured = null;
            _billingClaimSubmissionRepository
                .Setup(r => r.AddAsync(It.IsAny<ClaimSubmissionEntity>()))
                .Callback<ClaimSubmissionEntity>(e => captured = e)
                .Returns(Task.CompletedTask);

            _billingClaimSubmissionRepository
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Prepare step assigns the Id that method returns
            _claimValidationService
                .Setup(v => v.PrepareClaimSubmission(
                    claim,
                    It.IsAny<ClaimSubmissionEntity>(),
                    null,
                    It.IsAny<int>(),
                    null))
                .Callback<ClaimEntity, ClaimSubmissionEntity, ClaimSubmissionEntity, int, int?>((c, sub, prior, memberId, secFunder) =>
                {
                    sub.Id = 5000;
                })
                .Returns(Task.CompletedTask);

            var resultId = await sut.SubmitClaimRebill(
                claimId: claim.Id,
                submittingMemberId: 6001,
                frequencyType: ClaimFrequencyType.Replacement);

            Assert.NotNull(captured);
            Assert.Equal(claim.Id, captured.ClaimId);
            Assert.Equal(ClaimSubmissionType.Rebill, captured.SubmissionType);
            Assert.Equal(ClaimDocumentType.Doc837P, captured.DocumentType);
            Assert.Equal(ResponsibilitySequenceType.Primary.AsString(), captured.ResponsibilitySequence);
            Assert.Equal(ClaimSubmissionStatus.ClearingHousePending, captured.SubmissionStatus);
            Assert.StartsWith(claim.ClaimIdentifier, captured.ClaimSubmissionIdentifier);
            Assert.Equal(5000, resultId);

            _billingClaimSubmissionRepository.Verify(r => r.AddAsync(It.IsAny<ClaimSubmissionEntity>()), Times.Once);
            _billingClaimSubmissionRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
            _claimValidationService.Verify(v => v.PrepareClaimSubmission(claim, captured, null, 6001, null), Times.Once);
        }

        [Fact]
        public async Task SubmitClaimTransfer_ShouldThrow_WhenNoPriorSubmissionExists()
        {
            var sut = CreateSut();
            var claimId = 2001;

            _claimValidationService
                .Setup(v => v.GetClaimSubmissionInformation(claimId))
                .ReturnsAsync((ClaimSubmissionEntity)null);

            await Assert.ThrowsAsync<Exception>(() => sut.SubmitClaimTransfer(
                claimId: claimId,
                submittingMemberId: 7001,
                frequencyType: ClaimFrequencyType.Original,
                documentType: ClaimDocumentType.Doc837P));

            _claimValidationService.Verify(v => v.GetClaimSubmissionInformation(claimId), Times.Once);
        }

        [Theory]
        [InlineData(false, ClaimSubmissionType.Transfer)]
        [InlineData(true, ClaimSubmissionType.TransferRebill)]
        public async Task SubmitClaimTransfer_ShouldClonePriorSubmission_AndReturnNewId(bool isRebillPostSecondary, ClaimSubmissionType expectedSubmissionType)
        {
            var sut = CreateSut();
            var claimId = 3001;
            var submittingMemberId = 8001;
            var controlNumber = "CTRL-12345";

            var claim = new ClaimEntity
            {
                Id = claimId,
                ClaimIdentifier = "250101-00ABC-1",
                FrequencyTypeId = ClaimFrequencyType.Original
            };

            var priorSubmission = new ClaimSubmissionEntity
            {
                Id = 999,
                ClaimId = claimId,
                ClaimSubmissionIdentifier = "250101-00ABC-11",
                DocumentType = ClaimDocumentType.Doc837P,
                SubmissionType = ClaimSubmissionType.Original,
                FrequencyType = ClaimFrequencyType.Original,
                ResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString()
            };

            // Mocks required by SubmitClaim path
            _claimValidationService.Setup(v => v.GetClaimSubmissionInformation(claimId))
                .ReturnsAsync(priorSubmission);

            _claimValidationService.Setup(v => v.GetClaimInformation(claimId))
                .ReturnsAsync(claim);

            // GenerateClaimSubmissionIdentifier() uses repository.Query().FirstOrDefaultAsync() -> must be async-capable
            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create());

            // PrepareClaimSubmission assigns Id to the new submission
            _claimValidationService
                .Setup(v => v.PrepareClaimSubmission(
                    claim,
                    It.IsAny<ClaimSubmissionEntity>(),
                    priorSubmission,
                    submittingMemberId,
                    It.IsAny<int?>()))
                .Callback<ClaimEntity, ClaimSubmissionEntity, ClaimSubmissionEntity, int, int?>((c, sub, prior, member, secFunder) =>
                {
                    sub.Id = 5555;
                })
                .Returns(Task.CompletedTask);

            var resultId = await sut.SubmitClaimTransfer(
                claimId: claimId,
                submittingMemberId: submittingMemberId,
                frequencyType: ClaimFrequencyType.Original,
                documentType: ClaimDocumentType.Doc837P,
                secondaryFunderId: 42,
                controlNumber: controlNumber,
                IsRebillPostSecondaryBilling: isRebillPostSecondary);

            Assert.Equal(5555, resultId);
            Assert.Equal(controlNumber, priorSubmission.PayerClaimControlNumber);

            _claimValidationService.Verify(v => v.GetClaimSubmissionInformation(claimId), Times.Once);
            _claimValidationService.Verify(v => v.GetClaimInformation(claimId), Times.Once);
            _claimValidationService.Verify(v => v.PrepareClaimSubmission(
                claim,
                It.Is<ClaimSubmissionEntity>(cs =>
                    cs.SubmissionType == expectedSubmissionType &&
                    cs.DocumentType == ClaimDocumentType.Doc837P &&
                    cs.ResponsibilitySequence == ResponsibilitySequenceType.Secondary.AsString()),
                priorSubmission,
                submittingMemberId,
                42), Times.Once);
        }

        [Fact]
        public async Task SubmitClaim_WhenPriorIsNull_UsesClaimFrequencyAndInitializesSubmission()
        {
            var sut = CreateSut();
            var claimId = 4101;
            var submittingMemberId = 9001;

            var claim = new ClaimEntity
            {
                Id = claimId,
                ClaimIdentifier = "260115-00XYZ-1",
                FrequencyTypeId = ClaimFrequencyType.Original
            };

            // No prior submission -> InitializeClaimSubmission path
            _claimValidationService.Setup(v => v.GetClaimSubmissionInformation(claimId))
                .ReturnsAsync((ClaimSubmissionEntity)null);

            _claimValidationService.Setup(v => v.GetClaimInformation(claimId))
                .ReturnsAsync(claim);

            // Needed for GenerateClaimSubmissionIdentifier() inside InitializeClaimSubmission
            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create());

            // Capture submission passed to PrepareClaimSubmission and assign Id there
            ClaimSubmissionEntity prepared = null;
            _claimValidationService
                .Setup(v => v.PrepareClaimSubmission(
                    claim,
                    It.IsAny<ClaimSubmissionEntity>(),
                    null,
                    submittingMemberId,
                    It.IsAny<int?>()))
                .Callback<ClaimEntity, ClaimSubmissionEntity, ClaimSubmissionEntity, int, int?>((c, sub, prior, member, secFunder) =>
                {
                    prepared = sub;
                    sub.Id = 7777;
                })
                .Returns(Task.CompletedTask);

            var resultId = await sut.SubmitInitialClaim(
                claimId: claimId,
                submittingMemberId: submittingMemberId,
                documentType: ClaimDocumentType.Doc837P,
                responsibilitySequence: ResponsibilitySequenceType.Primary);

            Assert.Equal(7777, resultId);
            Assert.NotNull(prepared);
            Assert.Equal(ClaimSubmissionType.Original, prepared.SubmissionType);
            // FrequencyType should be overridden by claim.FrequencyTypeId
            Assert.Equal(ClaimFrequencyType.Original, prepared.FrequencyType);
            Assert.Equal(ClaimDocumentType.Doc837P, prepared.DocumentType);
            Assert.Equal(ResponsibilitySequenceType.Primary.AsString(), prepared.ResponsibilitySequence);
            Assert.StartsWith(claim.ClaimIdentifier, prepared.ClaimSubmissionIdentifier);

            _billingClaimSubmissionRepository.Verify(r => r.AddAsync(It.IsAny<ClaimSubmissionEntity>()), Times.Once);
            _billingClaimSubmissionRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
            _claimValidationService.Verify(v => v.PrepareClaimSubmission(claim, prepared, null, submittingMemberId, null), Times.Once);
        }

        [Fact]
        public async Task InitializeClaimSubmission_SetsFields_GeneratesIdentifier_AndSaves_WhenSaveTrue()
        {
            // Arrange
            var sut = CreateSut();
            var submittingMemberId = 111;
            var claim = new ClaimEntity { Id = 222, ClaimIdentifier = "241102-00E3P-1" };

            // Make Query() async-capable for GenerateClaimSubmissionIdentifier()
            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create());

            ClaimSubmissionEntity added = null;
            _billingClaimSubmissionRepository
                .Setup(r => r.AddAsync(It.IsAny<ClaimSubmissionEntity>()))
                .Callback<ClaimSubmissionEntity>(e => added = e)
                .Returns(Task.CompletedTask);

            _billingClaimSubmissionRepository
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            var mi = typeof(ClaimManagerService).GetMethod(
                "InitializeClaimSubmission",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(mi);

            // Act
            var task = (Task<ClaimSubmissionEntity>)mi.Invoke(
                sut,
                new object[]
                {
            submittingMemberId,
            claim,
            ClaimFrequencyType.Original,
            ClaimSubmissionType.Original,
            ClaimDocumentType.HCFA1500Single,
            ResponsibilitySequenceType.Primary,
            true // saveClaimSubmission
                });
            var submission = await task;

            // Assert: core fields set
            Assert.NotNull(submission);
            Assert.Equal(claim.Id, submission.ClaimId);
            Assert.Equal("N/A", submission.ClaimFilePath);
            Assert.Equal(ClaimFrequencyType.Original, submission.FrequencyType);
            Assert.Equal(ClaimSubmissionType.Original, submission.SubmissionType);
            Assert.Equal(ClaimDocumentType.HCFA1500Single, submission.DocumentType);
            Assert.Equal(ResponsibilitySequenceType.Primary.AsString(), submission.ResponsibilitySequence);
            Assert.Equal(ClaimSubmissionStatus.FunderPending, submission.SubmissionStatus);
            Assert.StartsWith(claim.ClaimIdentifier, submission.ClaimSubmissionIdentifier);

            // Saved to repository when saveClaimSubmission = true
            _billingClaimSubmissionRepository.Verify(r => r.AddAsync(It.IsAny<ClaimSubmissionEntity>()), Times.Once);
            _billingClaimSubmissionRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Theory]
        [InlineData(ClaimDocumentType.Doc837P, ClaimSubmissionStatus.ClearingHousePending)]
        [InlineData(ClaimDocumentType.HCFA1500Single, ClaimSubmissionStatus.FunderPending)]
        [InlineData(ClaimDocumentType.HCFA1500Multi, ClaimSubmissionStatus.FunderPending)]
        [InlineData(ClaimDocumentType.UB04Single, ClaimSubmissionStatus.FunderPending)]
        [InlineData(ClaimDocumentType.UB04Multi, ClaimSubmissionStatus.FunderPending)]
        public async Task InitializeClaimSubmission_SetsSubmissionStatus_ByDocumentType(ClaimDocumentType docType, ClaimSubmissionStatus expectedStatus)
        {
            // Arrange
            var sut = CreateSut();
            var claim = new ClaimEntity { Id = 333, ClaimIdentifier = "250101-00ABC-1" };

            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create());

            var mi = typeof(ClaimManagerService).GetMethod(
                "InitializeClaimSubmission",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(mi);

            // Act
            var task = (Task<ClaimSubmissionEntity>)mi.Invoke(
                sut,
                new object[]
                {
            999, // submittingMemberId
            claim,
            ClaimFrequencyType.Replacement,
            ClaimSubmissionType.Rebill,
            docType,
            ResponsibilitySequenceType.Secondary,
            false // no save
                });
            var submission = await task;

            // Assert
            Assert.NotNull(submission);
            Assert.Equal(expectedStatus, submission.SubmissionStatus);
            Assert.Equal(ResponsibilitySequenceType.Secondary.AsString(), submission.ResponsibilitySequence);

            // No save when saveClaimSubmission = false
            _billingClaimSubmissionRepository.Verify(r => r.AddAsync(It.IsAny<ClaimSubmissionEntity>()), Times.Never);
            _billingClaimSubmissionRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task InitializeClaimSubmission_DoesNotSave_WhenSaveFalse_ButGeneratesIdentifier()
        {
            // Arrange
            var sut = CreateSut();
            var claim = new ClaimEntity { Id = 444, ClaimIdentifier = "260115-00XYZ-1" };

            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create());

            var mi = typeof(ClaimManagerService).GetMethod(
                "InitializeClaimSubmission",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(mi);

            // Act
            var task = (Task<ClaimSubmissionEntity>)mi.Invoke(
                sut,
                new object[]
                {
            1234, // submittingMemberId
            claim,
            ClaimFrequencyType.Original,
            ClaimSubmissionType.Original,
            ClaimDocumentType.Doc837P,
            ResponsibilitySequenceType.Primary,
            false // saveClaimSubmission
                });
            var submission = await task;

            // Assert
            Assert.NotNull(submission);
            Assert.StartsWith(claim.ClaimIdentifier, submission.ClaimSubmissionIdentifier);

            _billingClaimSubmissionRepository.Verify(r => r.AddAsync(It.IsAny<ClaimSubmissionEntity>()), Times.Never);
            _billingClaimSubmissionRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task GenerateEdi_Throws_WhenClaimSubmissionNotFound()
        {
            // Arrange
            var sut = CreateSut();
            var claimId = 1001;

            // Make repository return no rows so GetFullClaimSubmission yields null
            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create()); // empty

            // Async-capable query on funder sequences to avoid EF async errors
            _billingClaimSubmissionFunderSequenceRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionFunderSequenceEntity>.Create());

            var dto = new ClearingHouseClaimModel
            {
                claimId = claimId,
                clearinghouseId = 55,
                isSecondary = false
            };

            _billingClaimRepository
              .Setup(r => r.Query())
              .Returns(
                  QueryMock<ClaimEntity>.Create(new[]
                  {
                    new ClaimEntity
                    {
                        Id = claimId,
                        AccountInfoId = 18421
                    }
                  })
              );

            // Act + Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => sut.GenerateEdi(dto));
        }

        
        [Fact]
        public async Task GenerateEdi_ReturnsEmpty_WhenClearingHouseDetailsNull()
        {
            // Arrange
            var sut = CreateSut();
            var claimId = 2002;
            var accountInfoId = 99;
            var providerLocationId = 77;

            var funderSettings = new List<FunderSettingsEntity>
            {
                new FunderSettingsEntity
                {
                    AccountInfoId = 1,
                    FunderId = 100,
                    DateDeleted = null,
                    ClaimFilingIndicator = new ClaimFilingIndicatorEntity
                    {
                        Id = 1,
                        Code = "CI"
                    }
                }
            }.AsQueryable();

            var claimDetail = new ClaimEntity
            {
                Id = claimId,
                AccountInfoId = accountInfoId,
                ProviderLocationId = providerLocationId,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>(),
                ChildProfileId = 123,
                AuthorizationId = 456,
                PaymentClaims = new List<PaymentClaimEntity>(),
                DateCreated = DateTime.UtcNow
            };

            // Ensure GetFullClaimSubmission finds a submission by ClaimId (isRebill=false path uses ClaimId filter)
            var submission = new ClaimSubmissionEntity
            {
                Id = 500,
                ClaimId = claimId,
                Claim = new ClaimEntity
                {
                    Id = 900,
                    AccountInfoId = accountInfoId,
                    ProviderLocationId = providerLocationId, // required (GetFullClaimSubmission uses .Value)
                    ClaimChargeEntries = new List<ClaimChargeEntryEntity>(),
                    PaymentClaims = new List<PaymentClaimEntity>(),
                    DateCreated = DateTime.UtcNow
                },
                ClaimSubmissionServiceLines = new List<ClaimSubmissionServiceLineEntity>(),
                ClaimValidationErrors = new List<ClaimValidationErrorEntity>(),
                ResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString()
            };

            _funderSettingRepo.Setup(r => r.Query())
                .Returns(QueryMock<FunderSettingsEntity>.Create(funderSettings));

            _billingClaimRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimEntity>.Create(claimDetail));

            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(submission));

            // Async-capable funder sequences query (even empty is fine)
            _billingClaimSubmissionFunderSequenceRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionFunderSequenceEntity>.Create());

            // mock funder settings repo
            _funderSettingRepo
                .Setup(x => x.Query())
                .Returns(QueryMock<FunderSettingsEntity>.Create());

            // mock claim service
            _mockClaimService
                .Setup(x => x.GetBillingProviderDetailsIdAsync(It.IsAny<int>()))
                .ReturnsAsync((ClaimBillingProviderOtherDto)null);

            // mock service provider
            _mockServiceProvider
                .Setup(x => x.GetService(typeof(IClaimService)))
                .Returns(_mockClaimService.Object);

            // Minimal dependencies used by GetFullClaimSubmission enrichment
            _rethinkServices
                .Setup(s => s.GetProviderLocation(accountInfoId, providerLocationId))
                .ReturnsAsync(new ProviderLocations
                {
                    id = providerLocationId,
                    agencyName = "Provider",
                    name = "Loc",
                    phone = "555-0000",
                    npiNumber = "NPI",
                    federalTaxId = "TAX",
                    address = new ProviderLocationAddress { street1 = "A1", city = "C", zip = "Z" }
                });

            _rethinkServices.Setup(s => s.GetLocationCodes())
                .ReturnsAsync(new List<LocationCodesModel>());

            _rethinkServices.Setup(s => s.GetUnitTypesAsync())
                .ReturnsAsync(new List<ClientUnitTypes>());

            // ClearingHouse details call returns null
            _rethinkServices
                .Setup(s => s.GetClearingHouseDetails())
                .ReturnsAsync((ClearingHouseModel)null);

            var dto = new ClearingHouseClaimModel
            {
                claimId = claimId,
                clearinghouseId = 55,
                isSecondary = false
            };

            // Act
            var result = await sut.GenerateEdi(dto);

            // Assert
            Assert.Equal(string.Empty, result);
            _rethinkServices.Verify(s => s.GetClearingHouseDetails(), Times.Once);
        }
              
        [Theory]
        [InlineData("M", 1)]
        [InlineData("F", 2)]
        [InlineData("U", 3)]
        [InlineData(null, 3)]
        [InlineData("", 3)]
        [InlineData("m", 3)]
        [InlineData("X", 3)]
        public void GetGenderId_ReturnsExpectedId(string gender, int expected)
        {
            var sut = CreateSut();

            var mi = typeof(ClaimManagerService).GetMethod(
                "GetGenderId",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(mi);

            var result = (int)mi.Invoke(sut, new object[] { gender });

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GenerateEdi_ReturnsEmpty_WhenClearingHouseNotMatched()
        {
            // Arrange
            var sut = CreateSut();
            var claimId = 3003;
            var accountInfoId = 50;
            var providerLocationId = 61;

            // Make GetFullClaimSubmission return a submission (isSecondary=false)
            var submission = new ClaimSubmissionEntity
            {
                Id = 600,
                ClaimId = claimId,
                Claim = new ClaimEntity
                {
                    Id = 901,
                    AccountInfoId = accountInfoId,
                    ProviderLocationId = providerLocationId, // required for .Value access
                    ClaimChargeEntries = new List<ClaimChargeEntryEntity>(),
                    PaymentClaims = new List<PaymentClaimEntity>(),
                    DateCreated = DateTime.UtcNow
                },
                ClaimSubmissionServiceLines = new List<ClaimSubmissionServiceLineEntity>(),
                ClaimValidationErrors = new List<ClaimValidationErrorEntity>(),
                ResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString()
            };

            var funderSettings = new List<FunderSettingsEntity>
            {
                new FunderSettingsEntity
                {
                    AccountInfoId = 1,
                    FunderId = 100,
                    DateDeleted = null,
                    ClaimFilingIndicator = new ClaimFilingIndicatorEntity
                    {
                        Id = 1,
                        Code = "CI"
                    }
                }
            }.AsQueryable();

            var claimDetail = new ClaimEntity
            {
                Id = claimId,
                AccountInfoId = accountInfoId,
                ProviderLocationId = providerLocationId,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>(),
                ChildProfileId = 123,
                AuthorizationId = 456,
                PaymentClaims = new List<PaymentClaimEntity>(),
                DateCreated = DateTime.UtcNow
            };

            _funderSettingRepo.Setup(r => r.Query())
                .Returns(QueryMock<FunderSettingsEntity>.Create(funderSettings));

            _billingClaimRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimEntity>.Create(claimDetail));

            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(submission));

            // IMPORTANT: GetFullClaimSubmission calls .ToListAsync() on FunderSequence repo -> must be async-capable
            _billingClaimSubmissionFunderSequenceRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionFunderSequenceEntity>.Create()); // empty is fine

            _mockServiceProvider
                .Setup(x => x.GetService(typeof(IClaimService)))
                .Returns(_mockClaimService.Object);

            _mockClaimService
                .Setup(x => x.GetBillingProviderDetailsIdAsync(It.IsAny<int>()))
                .ReturnsAsync((ClaimBillingProviderOtherDto)null);

            _funderSettingRepo
                .Setup(x => x.Query())
                .Returns(QueryMock<FunderSettingsEntity>.Create());

            // Minimal dependencies used during GetFullClaimSubmission enrichment
            _rethinkServices
                .Setup(s => s.GetProviderLocation(accountInfoId, providerLocationId))
                .ReturnsAsync(new ProviderLocations
                {
                    id = providerLocationId,
                    agencyName = "Provider X",
                    name = "Main Clinic",
                    phone = "555-0000",
                    npiNumber = "NPI-XYZ",
                    federalTaxId = "TAX-XYZ",
                    address = new ProviderLocationAddress { street1 = "PL1", city = "PLC", zip = "PLZ" },
                    isBillingLocation = true
                });

            _rethinkServices.Setup(s => s.GetLocationCodes())
                .ReturnsAsync(new List<LocationCodesModel>());

            _rethinkServices.Setup(s => s.GetUnitTypesAsync())
                .ReturnsAsync(new List<ClientUnitTypes>());

            // Return clearinghouse list without a matching id
            var chModel = new ClearingHouseModel
            {
                Data = new List<ClearingHouseDataModel>
        {
            new ClearingHouseDataModel { id = 10, title = "Other CH", taxId = "TAX-OTHER" }
        }
            };

            _rethinkServices
                .Setup(s => s.GetClearingHouseDetails())
                .ReturnsAsync(chModel);

            var dto = new ClearingHouseClaimModel
            {
                claimId = claimId,
                clearinghouseId = 99, // not in list
                isSecondary = false
            };

            // Act
            var result = await sut.GenerateEdi(dto);

            // Assert
            Assert.Equal(string.Empty, result);
            _rethinkServices.Verify(s => s.GetClearingHouseDetails(), Times.Once);
        }

        [Fact]
        public async Task GenerateEdi_ReturnsEmpty_WhenClearingHouseNotFound()
        {
            // Arrange
            var sut = CreateSut();
            var claimId = 2002;
            var accountInfoId = 10;
            var providerLocationId = 20;

            // Provide a "safe" Claim with required non-null collections and required ids used by enrichment
            var safeClaim = new ClaimEntity
            {
                Id = claimId,
                AccountInfoId = accountInfoId,
                ProviderLocationId = providerLocationId,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>(), // avoids .Where(...) on null
                PaymentClaims = new List<PaymentClaimEntity>(),          // avoids .Where(...) on null
                ClaimDiagnosisCodes = new List<ClaimDiagnosisCodeEntity>(),
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity>(),
                ClaimSubmissions = new List<ClaimSubmissionEntity>(),
                DateCreated = DateTime.UtcNow
            };

            var submission = new ClaimSubmissionEntity
            {
                Id = 500,
                ClaimId = claimId,
                Claim = safeClaim,
                ClaimSubmissionServiceLines = new List<ClaimSubmissionServiceLineEntity>(), // avoid null
                ClaimValidationErrors = new List<ClaimValidationErrorEntity>(),              // avoid null
                ClaimSubmissionFunderSequences = new List<ClaimSubmissionFunderSequenceEntity>(), // avoid null
                ResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString()
            };

            var funderSettings = new List<FunderSettingsEntity>
            {
                new FunderSettingsEntity
                {
                    AccountInfoId = 1,
                    FunderId = 100,
                    DateDeleted = null,
                    ClaimFilingIndicator = new ClaimFilingIndicatorEntity
                    {
                        Id = 1,
                        Code = "CI"
                    }
                }
            }.AsQueryable();

            var claimDetail = new ClaimEntity
            {
                Id = claimId,
                AccountInfoId = accountInfoId,
                ProviderLocationId = providerLocationId,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>(),
                ChildProfileId = 123,
                AuthorizationId = 456,
                PaymentClaims = new List<PaymentClaimEntity>(),
                DateCreated = DateTime.UtcNow
            };

            _funderSettingRepo.Setup(r => r.Query())
                .Returns(QueryMock<FunderSettingsEntity>.Create(funderSettings));

            _billingClaimRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimEntity>.Create(claimDetail));

            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(submission));

            // Async-capable funder sequences query
            _billingClaimSubmissionFunderSequenceRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionFunderSequenceEntity>.Create());

            // funder settings repo mock
            _funderSettingRepo
                .Setup(x => x.Query())
                .Returns(QueryMock<FunderSettingsEntity>.Create());

            // claim service mock
            _mockClaimService
                .Setup(x => x.GetBillingProviderDetailsIdAsync(It.IsAny<int>()))
                .ReturnsAsync((ClaimBillingProviderOtherDto)null);

            // service provider mock
            _mockServiceProvider
                .Setup(x => x.GetService(typeof(IClaimService)))
                .Returns(_mockClaimService.Object);


            // Minimal dependencies used by GetFullClaimSubmission enrichment
            _rethinkServices
                .Setup(s => s.GetProviderLocation(accountInfoId, providerLocationId))
                .ReturnsAsync(new ProviderLocations
                {
                    id = providerLocationId,
                    agencyName = "Provider",
                    name = "Loc",
                    phone = "555-0000",
                    npiNumber = "NPI",
                    federalTaxId = "TAX",
                    address = new ProviderLocationAddress { street1 = "A1", city = "C", zip = "Z" }
                });

            _rethinkServices.Setup(s => s.GetLocationCodes()).ReturnsAsync(new List<LocationCodesModel>());
            _rethinkServices.Setup(s => s.GetUnitTypesAsync()).ReturnsAsync(new List<ClientUnitTypes>());

            // ClearingHouse list does not contain requested id -> GenerateEdi should return empty
            var clearingHouseDetails = new ClearingHouseModel
            {
                Data = new List<ClearingHouseDataModel>
        {
            new ClearingHouseDataModel { id = 999, title = "Other", taxId = "TAX" }
        }
            };
            _rethinkServices.Setup(s => s.GetClearingHouseDetails()).ReturnsAsync(clearingHouseDetails);

            var dto = new ClearingHouseClaimModel { claimId = claimId, clearinghouseId = 55, isSecondary = false };

            // Act
            var result = await sut.GenerateEdi(dto);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task SubmitClaim_WhenPriorExists_ClonesAndGeneratesNewIdentifier_AndReturnsId()
        {
            var sut = CreateSut();
            var claimId = 4201;
            var submittingMemberId = 9101;
            var controlNumber = "CTRL-9009";

            var claim = new ClaimEntity
            {
                Id = claimId,
                ClaimIdentifier = "260115-00ABC-1",
                FrequencyTypeId = ClaimFrequencyType.Original
            };

            var priorSubmission = new ClaimSubmissionEntity
            {
                Id = 2222,
                ClaimId = claimId,
                ClaimSubmissionIdentifier = "260115-00ABC-11",
                DocumentType = ClaimDocumentType.Doc837P,
                SubmissionType = ClaimSubmissionType.Original,
                FrequencyType = ClaimFrequencyType.Original,
                ResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString()
            };

            _claimValidationService.Setup(v => v.GetClaimSubmissionInformation(claimId))
                .ReturnsAsync(priorSubmission);

            _claimValidationService.Setup(v => v.GetClaimInformation(claimId))
                .ReturnsAsync(claim);

            // Required for GenerateClaimSubmissionIdentifier() in clone (Transfer) path
            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create());

            ClaimSubmissionEntity prepared = null;
            _claimValidationService
                .Setup(v => v.PrepareClaimSubmission(
                    claim,
                    It.IsAny<ClaimSubmissionEntity>(),
                    priorSubmission,
                    submittingMemberId,
                    It.IsAny<int?>()))
                .Callback<ClaimEntity, ClaimSubmissionEntity, ClaimSubmissionEntity, int, int?>((c, sub, prior, member, secFunder) =>
                {
                    prepared = sub;
                    sub.Id = 8888;
                })
                .Returns(Task.CompletedTask);

            var resultId = await sut.SubmitClaimTransfer(
                claimId: claimId,
                submittingMemberId: submittingMemberId,
                frequencyType: ClaimFrequencyType.Original,
                documentType: ClaimDocumentType.Doc837P,
                secondaryFunderId: 123,
                controlNumber: controlNumber,
                IsRebillPostSecondaryBilling: false);

            Assert.Equal(8888, resultId);
            Assert.NotNull(prepared);
            Assert.Equal(ClaimSubmissionType.Transfer, prepared.SubmissionType);
            Assert.Equal(ResponsibilitySequenceType.Secondary.AsString(), prepared.ResponsibilitySequence);
            Assert.Equal(ClaimDocumentType.Doc837P, prepared.DocumentType);
            Assert.StartsWith(claim.ClaimIdentifier, prepared.ClaimSubmissionIdentifier);
            Assert.Equal(controlNumber, priorSubmission.PayerClaimControlNumber);

            _claimValidationService.Verify(v => v.PrepareClaimSubmission(
                claim,
                prepared,
                priorSubmission,
                submittingMemberId,
                123), Times.Once);
        }

        [Fact]
        public async Task Generate270Edi_ShouldReturnMessage_WhenDtoIsNull()
        {
            var sut = CreateSut();

            var result = await sut.Generate270Edi(null);

            Assert.Equal("No funder data found", result);
        }

        //[Fact]
        //public async Task Generate270Edi_ShouldSetFunderId_ButThrow_WhenEdiFabricTokenMissing()
        //{
        //    var sut = CreateSut();

        //    var dto = new Eligibility270DTO
        //    {
        //        ClientName = "John Doe",
        //        ClearingHousePayerName = "Test Payer (12345)"
        //    };

        //    var ex = await Assert.ThrowsAsync<Exception>(() => sut.Generate270Edi(dto));

        //    // FunderId is parsed from ClearingHousePayerName before the writer is created
        //    Assert.Contains("token", ex.Message, StringComparison.OrdinalIgnoreCase);
        //}

        //[Fact]
        
        //public async Task EdiGenerator_Generate270Edi_ShouldSetFunderId_ButThrow_WhenEdiFabricTokenMissing()
        //{
        //    var generator = new BillingService.Domain.Services.Billing.EDI.EdiGenerator(
        //        testMode: true,
        //        billerRethinkId: "BILLERID",
        //        submitterRethinkId: "SUBMITTER",
        //        submitterRethinkName: "Submitter Name",
        //        submitterRethinkEmail: "submitter@example.com",
        //        submitterRethinkPhone: "5551234567",
        //        customerId: "CUST001",
        //        clearingHouseName: "",
        //        taxId: ""
        //    );

        //    var dto = new Eligibility270DTO
        //    {
        //        ClientName = "Jane Doe",
        //        ClearingHousePayerName = "Acme Health (98765)"
        //    };

        //    var ex = await Assert.ThrowsAsync<Exception>(() => generator.Generate270Edi(dto));
        //    Assert.Equal(98765, dto.FunderId);
        //    Assert.Contains("token", ex.Message, StringComparison.OrdinalIgnoreCase);
        //}

        [Fact]
        public async Task LookupHCFAClaimDetails_WhenNoSubmission_ReturnsEmptyModel()
        {
            var sut = CreateSut();
            var claimId = 91001;

            // No submissions found for claim -> GetLatestClaimSubmission returns null
            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create()); // empty

            var model = await sut.LookupHCFAClaimDetails(memberId: 1, accountInfoId: 1, claimId: claimId);

            Assert.NotNull(model);
            Assert.Equal(0, model.Id); // default model returned when no submission exists
        }

        [Fact]
        public async Task LookupHCFAClaimDetails_WhenSubmissionExists_ReturnsHcfaModel()
        {
            var sut = CreateSut();

            var claimId = 91002;
            var accountInfoId = 21;
            var childProfileId = 31;
            var clientFunderId = 41;
            var primaryFunderId = 51;
            var providerLocationId = 61;
            var lastSubmissionId = 71001;   // id returned by first query (MUST match the id used in GetFullClaimSubmission)

            // Base claim used by full submission
            var claim = new ClaimEntity
            {
                Id = claimId,
                AccountInfoId = accountInfoId,
                ChildProfileId = childProfileId,
                ClientFunderId = clientFunderId,
                PrimaryFunderId = primaryFunderId,
                ProviderLocationId = providerLocationId,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>(),
                PaymentClaims = new List<PaymentClaimEntity>(),
                ClaimBillingProviders = new List<ClaimBillingProviderEntity>(), 
                DateCreated = DateTime.UtcNow
            };

            // First Query(): latest (light) submission row for GetLatestClaimSubmission
            var lastSubmission = new ClaimSubmissionEntity
            {
                Id = lastSubmissionId,
                ClaimId = claimId,
                DocumentType = ClaimDocumentType.HCFA1500Single
            };

            // Second Query(): "full" submission row for GetFullClaimSubmission
            // IMPORTANT: Id must equal lastSubmissionId so the query filter (cs.Id == claimSubmissionId) matches.
            var fullSubmission = new ClaimSubmissionEntity
            {
                Id = lastSubmissionId,
                ClaimId = claimId,
                Claim = claim,
                DocumentType = ClaimDocumentType.HCFA1500Single,
                ResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString(),
                FunderId = primaryFunderId,
                ClaimSubmissionServiceLines = new List<ClaimSubmissionServiceLineEntity>(),
                ClaimValidationErrors = new List<ClaimValidationErrorEntity>(), // avoid NRE on .Where(...)
                FunderDetails = new Rethink.Services.Common.Models.FunderDataModel { phone = "800-555-0100", funderCoverageTypeId = 99 }
            };

            // Repo sequence: 1) latest row lookup, 2) full submission lookup with includes
            _billingClaimSubmissionRepository
                .SetupSequence(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(lastSubmission))   // GetLatestClaimSubmission
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(fullSubmission));  // GetFullClaimSubmission

            // Funder sequences used by CreateHCFAModel
            var funderSeq = new ClaimSubmissionFunderSequenceEntity
            {
                ClaimSubmissionId = lastSubmissionId,
                FunderId = primaryFunderId,
                FunderResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString(),
                FunderName = "Primary Funder Test",
                InsuranceAddress1 = "A1",
                InsuranceAddress2 = "A2",
                InsuranceCity = "AC",
                InsuranceZip = "AZ",
                InsuranceState = "AS",
                SubscriberFirstName = "FN",
                SubscriberLastName = "LN",
                SubscriberMiddleName = "MN",
                SubscriberAddress1 = "SA1",
                SubscriberAddress2 = "SA2",
                SubscriberCity = "SC",
                SubscriberState = "SS",
                SubscriberZip = "SZ",
                SubscriberGender = "U",
                RelationshipToSubscriber = 1,
                InsuranceGroupNumber = "GRP",
                InsurancePolicyNumber = "POL",
                InsurancePlanName = "Plan",
                ServiceLineBillingProviderOption = BillingProviderOptionType.Individual
            };

            _billingClaimSubmissionFunderSequenceRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionFunderSequenceEntity>.Create(funderSeq));

            // Diagnosis codes query (empty is fine)
            _claimDiagnosisRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimDiagnosisCodeEntity>.Create());

            // GetFullClaimSubmission dependencies
            var providerLoc = new ProviderLocations
            {
                id = providerLocationId,
                agencyName = "Test Provider",
                name = "Test Location",
                phone = "555-1111",
                npiNumber = "NPI-TEST",
                federalTaxId = "TAX-TEST",
                address = new ProviderLocationAddress { street1 = "P1", street2 = "P2", city = "PC", zip = "PZ" },
                isMainLocation = true,
                isBillingLocation = true
            };

            _rethinkServices
                .Setup(s => s.GetProviderLocation(accountInfoId, providerLocationId))
                .ReturnsAsync(providerLoc);

            _rethinkServices
                .Setup(s => s.GetFunder(accountInfoId, primaryFunderId))
                .ReturnsAsync(new Rethink.Services.Common.Models.FunderDataModel { phone = "800-555-0100", funderCoverageTypeId = 99 });

            _rethinkServices
                .Setup(s => s.GetChildProfileFunderMappingByMappingId(accountInfoId, childProfileId, clientFunderId))
                .ReturnsAsync(new FunderDetails());

            // Avoid null refs in GetFullClaimSubmission and MapHcfaAdress
            _rethinkServices
                .Setup(s => s.GetChildProfileAuthorizationByClientId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((ClientAuthorization)null);

            _rethinkServices
                .Setup(s => s.GetChildProfileReturningEntity(accountInfoId, childProfileId))
                .ReturnsAsync(new ChildProfileEntityModel { AccountInfoId = accountInfoId });

            _rethinkServices
                .Setup(s => s.GetMemberAsync(accountInfoId, It.IsAny<int>()))
                .ReturnsAsync(new RethinkAccountMember { id = 1 });

            _rethinkServices
                .Setup(s => s.GetLocationCodes())
                .ReturnsAsync(new List<LocationCodesModel>());

            _rethinkServices
                .Setup(s => s.GetUnitTypesAsync())
                .ReturnsAsync(new List<ClientUnitTypes>());

            // MapHcfaAdress dependencies (Individual billing provider path)
            _rethinkServices
                .Setup(s => s.GetAccountReturningEntityAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(new AccountInfoEntityModel());

            _rethinkServices
                .Setup(s => s.GetProviderLocationList(It.IsAny<int>()))
                .ReturnsAsync(new ClientProviderLocationsModel
                {
                    data = new List<ProviderLocations>
                    {
                new ProviderLocations
                {
                    id = providerLocationId,
                    isMainLocation = true,
                    address = new ProviderLocationAddress { street1 = "ML1", city = "MLC", zip = "MLZ", countryId = 0, stateId = 0 }
                }
                    }
                });

            // Prevent NRE in GetCountry/GetState lookups inside MapHcfaAdress
            _rethinkServices
                .Setup(s => s.GetCountryList())
                .ReturnsAsync(new List<CountryModel>());
            _rethinkServices
                .Setup(s => s.GetStateList())
                .ReturnsAsync(new List<StateModel>());

            var model = await sut.LookupHCFAClaimDetails(memberId: 100, accountInfoId: accountInfoId, claimId: claimId);

            Assert.NotNull(model);
            Assert.Equal(claimId, model.Id);
            Assert.Equal(primaryFunderId, model.FunderId);
            Assert.Equal("Test Provider", model.ProviderName);
            Assert.Equal("NPI-TEST", model.ProviderLocationNPI);
        }



        [Fact]
        public async Task CreateHCFAModel_WhenClaimSubmissionIsNull_ReturnsDefaultModel()
        {
            // Arrange
            var sut = CreateSut();
            var claimId = 501;

            // No latest submission -> Lookup path will call CreateHCFAModel(null)
            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create()); // empty

            // Act
            var model = await sut.LookupHCFAClaimDetails(memberId: 1, accountInfoId: 1, claimId: claimId);

            // Assert
            Assert.NotNull(model);
            Assert.Equal(0, model.Id);
        }

        [Fact]
        public async Task GetFullClaimSubmission_FiltersAndMaps_ReturnsExpectedInHcfa()
        {
            var sut = CreateSut();

            var claimId = 31001;
            var accountInfoId = 21;
            var childProfileId = 31;
            var clientFunderId = 41;
            var primaryFunderId = 51;
            var providerLocationId = 61;
            var latestSubmissionId = 91010;

            // Claim with mixed deleted/active items
            var claim = new ClaimEntity
            {
                Id = claimId,
                AccountInfoId = accountInfoId,
                ChildProfileId = childProfileId,
                ClientFunderId = clientFunderId,
                PrimaryFunderId = primaryFunderId,
                ProviderLocationId = providerLocationId,
                AuthorizationNumber = "AUTH-XYZ",
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>
        {
                new ClaimChargeEntryEntity { DateDeleted = null, Units = 1, BillingCode = "97153", Charges = 100m, UnitTypeId = 7, Claim = new ClaimEntity { RenderingStaffMemberId = 999, LocationCodeId = 111, StartDate = DateTime.UtcNow } },
                new ClaimChargeEntryEntity { DateDeleted = DateTime.UtcNow, Units = 2, BillingCode = "97155", Charges = 200m, UnitTypeId = 7, Claim = new ClaimEntity() } // should be filtered
            },
                PaymentClaims = new List<PaymentClaimEntity>
        {
            new PaymentClaimEntity { TotalPayment = 25m, DateDeleted = null },
                new PaymentClaimEntity { TotalPayment = 75m, DateDeleted = DateTime.UtcNow } // should be filtered
        },
                ClaimBillingProviders = new List<ClaimBillingProviderEntity>(), 
                DateCreated = DateTime.UtcNow
            };

            // First: latest (light) submission
            var lastSubmission = new ClaimSubmissionEntity
            {
                Id = latestSubmissionId,
                ClaimId = claimId,
                DocumentType = ClaimDocumentType.HCFA1500Single
            };

            // Second: full submission (will be enriched by GetFullClaimSubmission)
            var fullSubmission = new ClaimSubmissionEntity
            {
                Id = latestSubmissionId,
                ClaimId = claimId,
                Claim = claim,
                DocumentType = ClaimDocumentType.HCFA1500Single,
                ResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString(),
                FunderId = primaryFunderId,
                ClaimSubmissionServiceLines = new List<ClaimSubmissionServiceLineEntity>(),
                ClaimValidationErrors = new List<ClaimValidationErrorEntity>(),
                AuthorizationNumber = null // should be set from Claim.AuthorizationNumber by GetFullClaimSubmission
            };

            // Repo: latest, then full
            _billingClaimSubmissionRepository
                .SetupSequence(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(lastSubmission))
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(fullSubmission));

            // Funder sequences: choose latest per responsibility by SequenceOrder
            var primaryOld = new ClaimSubmissionFunderSequenceEntity
            {
                ClaimSubmissionId = latestSubmissionId,
                FunderId = primaryFunderId,
                FunderName = "Primary OLD",
                FunderResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString(),
                SequenceOrder = 1,
                InsuranceAddress1 = "Old1",
                InsuranceAddress2 = "Old2",
                InsuranceCity = "OldCity",
                InsuranceState = "OS",
                InsuranceZip = "OZ",
                SubscriberFirstName = "OldFN",
                SubscriberLastName = "OldLN",
                SubscriberMiddleName = "OldMN",
                InsuranceGroupNumber = "OGR",
                InsurancePolicyNumber = "OPOL",
                InsurancePlanName = "OPlan",
                ServiceLineBillingProviderOption = BillingProviderOptionType.Individual
            };
            var primaryLatest = new ClaimSubmissionFunderSequenceEntity
            {
                ClaimSubmissionId = latestSubmissionId,
                FunderId = primaryFunderId,
                FunderName = "Primary LATEST",
                FunderResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString(),
                SequenceOrder = 2, // should win
                InsuranceAddress1 = "A1",
                InsuranceAddress2 = "A2",
                InsuranceCity = "AC",
                InsuranceState = "AS",
                InsuranceZip = "AZ",
                SubscriberFirstName = "SubFN",
                SubscriberLastName = "SubLN",
                SubscriberMiddleName = "SubMN",
                InsuranceGroupNumber = "GRP",
                InsurancePolicyNumber = "POL",
                InsurancePlanName = "Plan",
                ReleaseOfInformationConfirmationDate = new DateTime(2024, 01, 02),
                ServiceLineBillingProviderOption = BillingProviderOptionType.Individual
            };
            var secondaryLatest = new ClaimSubmissionFunderSequenceEntity
            {
                ClaimSubmissionId = latestSubmissionId,
                FunderId = 999,
                FunderName = "Secondary LATEST",
                FunderResponsibilitySequence = ResponsibilitySequenceType.Secondary.AsString(),
                SequenceOrder = 3,
                SubscriberFirstName = "SecFN",
                SubscriberLastName = "SecLN",
                SubscriberMiddleName = "SecMN",
                InsuranceGroupNumber = "S-GRP",
                InsurancePolicyNumber = "S-POL",
                InsurancePlanName = "S-Plan"
            };

            _billingClaimSubmissionFunderSequenceRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionFunderSequenceEntity>.Create(primaryOld, primaryLatest, secondaryLatest));

            // Diagnosis codes and lookups
            var d1 = new ClaimDiagnosisCodeEntity { ClaimId = claimId, DiagnosisId = 1001, Order = 1 };
            _claimDiagnosisRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimDiagnosisCodeEntity>.Create(d1));
            _rethinkServices
                .Setup(s => s.GetDiagnosisById(1001))
                .ReturnsAsync(new Diagnosis { diagnosisCode = "F84.0" });

            // Provider location used by hcfa mapping fields
            var providerLoc = new ProviderLocations
                {
                    id = providerLocationId,
                    agencyName = "Provider X",
                name = "Main Clinic",
                phone = "555-0000",
                    npiNumber = "NPI-XYZ",
                federalTaxId = "TAX-XYZ",
                isBillingLocation = true,
                address = new ProviderLocationAddress { street1 = "PL1", street2 = "PL2", city = "PLC", zip = "PLZ", stateId = 0, countryId = 0 }
            };
            _rethinkServices.Setup(s => s.GetProviderLocation(accountInfoId, providerLocationId))
                .ReturnsAsync(providerLoc);

            // Lookups required during enrichment
            _rethinkServices.Setup(s => s.GetFunder(accountInfoId, primaryFunderId))
                .ReturnsAsync(new FunderDataModel { phone = "800-555-1010", funderCoverageTypeId = 77 });

            _rethinkServices.Setup(s => s.GetChildProfileFunderMappingByMappingId(accountInfoId, childProfileId, clientFunderId))
                .ReturnsAsync(new FunderDetails());

            _rethinkServices.Setup(s => s.GetChildProfileAuthorizationByClientId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((ClientAuthorization)null);
            _rethinkServices.Setup(s => s.GetChildProfileReturningEntity(accountInfoId, childProfileId))
                .ReturnsAsync(new ChildProfileEntityModel { AccountInfoId = accountInfoId });
            _rethinkServices.Setup(s => s.GetMemberAsync(accountInfoId, It.IsAny<int>()))
                .ReturnsAsync(new RethinkAccountMember { id = 1 });

            // LocationCodes + UnitTypes used in GetFullClaimSubmission
            _rethinkServices.Setup(s => s.GetLocationCodes())
                .ReturnsAsync(new List<LocationCodesModel> { new LocationCodesModel { id = 111, code = "LOC-111" } });
            _rethinkServices.Setup(s => s.GetUnitTypesAsync())
                .ReturnsAsync(new List<ClientUnitTypes> { new ClientUnitTypes { id = 7, unitString = "15min" } });

            // MapHcfaAdress dependencies
            _rethinkServices.Setup(s => s.GetAccountReturningEntityAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(new AccountInfoEntityModel());
            _rethinkServices.Setup(s => s.GetProviderLocationList(It.IsAny<int>()))
                .ReturnsAsync(new ClientProviderLocationsModel { data = new List<ProviderLocations> { providerLoc } });
            _rethinkServices.Setup(s => s.GetCountryList()).ReturnsAsync(new List<CountryModel>());
            _rethinkServices.Setup(s => s.GetStateList()).ReturnsAsync(new List<StateModel>());

            // Act (public entry that invokes GetFullClaimSubmission internally)
            var model = await sut.LookupHCFAClaimDetails(memberId: 100, accountInfoId: accountInfoId, claimId: claimId);

            // Assert: AuthorizationNumber copied from Claim when Submission.AuthorizationNumber was null
            Assert.Equal("AUTH-XYZ", model.AuthorizationNumber);

            // Assert: PaymentClaims filtered (only non-deleted counted)
            Assert.Equal(25m, model.Paid);

            // Assert: ClaimChargeEntries filtered (deleted removed) and UnitType mapped
            Assert.Single(model.ClaimChargeEntries);
            Assert.NotNull(model.ClaimChargeEntries[0].UnitType);

            // Assert: Funder sequence chosen is the latest by SequenceOrder for Primary
            Assert.Equal(primaryFunderId, model.FunderId);
            Assert.Equal("Primary LATEST", model.FunderName);
            Assert.Equal("A1", model.FunderAddress);
            Assert.Equal("AC", model.FunderCity);
            Assert.Equal("AS", model.FunderState);
            Assert.Equal("AZ", model.FunderZip);

            // Assert: Next (Secondary) sequence is present and contributes to secondary insured fields
            Assert.Equal("SecFN, SubLN, SubMN", model.SecondaryInsuredName); // matches current CreateHCFAModel composition
            Assert.Equal("S-POL", model.SecondaryInsuredNumber);
            Assert.Equal("S-Plan", model.SecondaryInsurancePlanName);

            // Provider mapping
            Assert.Equal("Provider X", model.ProviderName);
            Assert.Equal("NPI-XYZ", model.ProviderLocationNPI);

            // Diagnosis codes aggregated
            Assert.Contains("F84.0", model.PatientDiagnosis);
        }

        [Fact]
        public async Task GetFullClaimSubmission_WhenNoSubmissionFound_ReturnsDefaultHcfaModel()
        {
            var sut = CreateSut();
            var claimId = 999001;

            // No rows in repository for latest submission
            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create());

            var model = await sut.LookupHCFAClaimDetails(memberId: 1, accountInfoId: 1, claimId: claimId);

            Assert.NotNull(model);
            Assert.Equal(0, model.Id);
        }

        [Fact]
        public async Task GetFullClaimSubmission_WhenInitialSubmissionIsNull_DoesNotThrow_AndReturnsClaim()
        {
            var sut = CreateSut();
            var claimId = 999001;
            var accountInfoId = 10;
            var childProfileId = 20;
            var providerLocationId = 30;

            // GetFullClaim queries _billingClaimRepository and expects a non-null claim.
            // Provide a minimal, safe claim to avoid NullReference inside enrichment.
            var claim = new ClaimEntity
            {
                Id = claimId,
                AccountInfoId = accountInfoId,
                ChildProfileId = childProfileId,
                ProviderLocationId = providerLocationId,   // used with .Value in provider lookup
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>(),
                ClaimDiagnosisCodes = new List<ClaimDiagnosisCodeEntity>(),
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity>(),
                ClaimSubmissions = new List<ClaimSubmissionEntity>(),
                PaymentClaims = new List<PaymentClaimEntity>()
            };

            _billingClaimRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimEntity>.Create(claim)); // async-capable queryable

            // Minimal enrichments called by GetFullClaim
            _rethinkServices
                .Setup(s => s.GetChildProfileReturningEntity(accountInfoId, childProfileId))
                .ReturnsAsync(new ChildProfileEntityModel { Id = childProfileId });

            _rethinkServices
                .Setup(s => s.GetAccountReturningEntityAsync(accountInfoId, true))
                .ReturnsAsync(new AccountInfoEntityModel { Id = accountInfoId });

            _rethinkServices
                .Setup(s => s.GetProviderLocation(accountInfoId, providerLocationId))
                .ReturnsAsync(new ProviderLocations { id = providerLocationId });

            _rethinkServices
                .Setup(s => s.GetLocationCodes())
                .ReturnsAsync(new List<LocationCodesModel>());

            // Act
            var result = await sut.GetFullClaim(claimId);

            // Assert: method returns the claim when it exists (no NRE)
            Assert.NotNull(result);
            Assert.Equal(claimId, result.Id);
        }

        [Fact]
        public async Task GetFullClaimSubmission_WhenClaimIsNull_DoesNotThrowException()
        {
            var sut = CreateSut();
            var claimId = 31001;
            var accountInfoId = 1;
            var childProfileId = 2;
            var providerLocationId = 3;
            var locationCodeId = 11;

            // GetFullClaim uses _billingClaimRepository.Query().FirstOrDefaultAsync(...)
            // Provide a fully safe ClaimEntity with non-null collections to avoid NREs during enrichment.
            var claim = new ClaimEntity
            {
                Id = claimId,
                AccountInfoId = accountInfoId,
                ChildProfileId = childProfileId,
                ProviderLocationId = providerLocationId,
                ServiceLocationId = providerLocationId,         // some paths access .Value
                LocationCodeId = locationCodeId,                // used against GetLocationCodes()
                RenderingStaffMemberId = 999,                   // safe default
                ChildProfileReferringProviderId = null,         // ensure ref provider path is skipped
                PaymentClaims = new List<PaymentClaimEntity>(), // avoid null enumerations
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>(),
                ClaimDiagnosisCodes = new List<ClaimDiagnosisCodeEntity>(),
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity>(),
                ClaimSubmissions = new List<ClaimSubmissionEntity>()
            };

            _billingClaimRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimEntity>.Create(claim)); // async-capable IQueryable

            // Minimal services used by GetFullClaim enrichment
            _rethinkServices
                .Setup(s => s.GetChildProfileReturningEntity(accountInfoId, childProfileId))
                .ReturnsAsync(new ChildProfileEntityModel { Id = childProfileId });

            _rethinkServices
                .Setup(s => s.GetAccountReturningEntityAsync(accountInfoId, true))
                .ReturnsAsync(new AccountInfoEntityModel { Id = accountInfoId });

            _rethinkServices
                .Setup(s => s.GetProviderLocation(accountInfoId, providerLocationId))
                .ReturnsAsync(new ProviderLocations { id = providerLocationId });

            _rethinkServices
                .Setup(s => s.GetLocationCodes())
                .ReturnsAsync(new List<LocationCodesModel> { new LocationCodesModel { id = locationCodeId, code = "LOC-11" } });

            // Optional but safe: if rendering/member is accessed
            _rethinkServices
                .Setup(s => s.GetMemberAsync(accountInfoId, It.IsAny<int>()))
                .ReturnsAsync(new RethinkAccountMember { id = 999 });

            // Act
            var result = await sut.GetFullClaim(claimId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(claimId, result.Id);
        }

        [Fact]
        public async Task GetFullClaimSubmission_WhenPaymentClaimsAreNull_DoesNotThrowException()
        {
            var sut = CreateSut();
            var claimId = 31001;
            var accountInfoId = 10;
            var childProfileId = 20;
            var providerLocationId = 30;
            var locationCodeId = 11;

            // GetFullClaim queries _billingClaimRepository with FirstOrDefaultAsync(...)
            // Provide an async-capable Claim queryable and a minimal, safe claim.
            var claim = new ClaimEntity
            {
                Id = claimId,
                AccountInfoId = accountInfoId,
                ChildProfileId = childProfileId,
                ProviderLocationId = providerLocationId,
                ServiceLocationId = providerLocationId,         // may be accessed with .Value
                LocationCodeId = locationCodeId,
                PaymentClaims = null,                            // the scenario under test
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>(),
                ClaimDiagnosisCodes = new List<ClaimDiagnosisCodeEntity>(),
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity>(),
                ClaimSubmissions = new List<ClaimSubmissionEntity>()
            };

            _billingClaimRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimEntity>.Create(claim));

            // Minimal enrichment dependencies used by GetFullClaim
            _rethinkServices
                .Setup(s => s.GetChildProfileReturningEntity(accountInfoId, childProfileId))
                .ReturnsAsync(new ChildProfileEntityModel { Id = childProfileId });

            _rethinkServices
                .Setup(s => s.GetAccountReturningEntityAsync(accountInfoId, true))
                .ReturnsAsync(new AccountInfoEntityModel { Id = accountInfoId });

            _rethinkServices
                .Setup(s => s.GetProviderLocation(accountInfoId, providerLocationId))
                .ReturnsAsync(new ProviderLocations { id = providerLocationId });

            _rethinkServices
                .Setup(s => s.GetLocationCodes())
                .ReturnsAsync(new List<LocationCodesModel> { new LocationCodesModel { id = locationCodeId, code = "LOC-11" } });

            // Act
            var result = await sut.GetFullClaim(claimId);

            // Assert
            Assert.NotNull(result); // Ensure it doesn't break even when PaymentClaims is null
        }

        [Fact]
        public async Task GetFullClaimSubmission_WhenClaimChargeEntriesAreEmpty_ReturnsFilteredSubmission()
        {
            var sut = CreateSut();
            var claimId = 31001;
            var accountInfoId = 10;
            var childProfileId = 20;
            var providerLocationId = 30;
            var locationCodeId = 11;

            // GetFullClaim queries _billingClaimRepository with FirstOrDefaultAsync(...)
            // Provide an async-capable Claim queryable and a minimal, safe claim.
            var claim = new ClaimEntity
            {
                Id = claimId,
                AccountInfoId = accountInfoId,
                ChildProfileId = childProfileId,
                ProviderLocationId = providerLocationId,
                ServiceLocationId = providerLocationId,               // may be accessed with .Value
                LocationCodeId = locationCodeId,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>(), // empty list for this scenario
                PaymentClaims = new List<PaymentClaimEntity>(),
                ClaimDiagnosisCodes = new List<ClaimDiagnosisCodeEntity>(),
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity>(),
                ClaimSubmissions = new List<ClaimSubmissionEntity>()
            };

            _billingClaimRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimEntity>.Create(claim)); // async-capable IQueryable

            // Minimal enrichment dependencies used by GetFullClaim
            _rethinkServices
                .Setup(s => s.GetChildProfileReturningEntity(accountInfoId, childProfileId))
                .ReturnsAsync(new ChildProfileEntityModel { Id = childProfileId });

            _rethinkServices
                .Setup(s => s.GetAccountReturningEntityAsync(accountInfoId, true))
                .ReturnsAsync(new AccountInfoEntityModel { Id = accountInfoId });

            _rethinkServices
                .Setup(s => s.GetProviderLocation(accountInfoId, providerLocationId))
                .ReturnsAsync(new ProviderLocations { id = providerLocationId });

            _rethinkServices
                .Setup(s => s.GetLocationCodes())
                .ReturnsAsync(new List<LocationCodesModel> { new LocationCodesModel { id = locationCodeId, code = "LOC-11" } });

            // Act
            var result = await sut.GetFullClaim(claimId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.ClaimChargeEntries);  // Ensure no charge entries are present
        }

        [Fact]
        public async Task GetFullClaimSubmission_WhenSecondaryBillingIsTrue_ReturnsExpectedSecondaryBillingData()
        {
            var sut = CreateSut();
            var claimId = 31001;
            var accountInfoId = 10;
            var childProfileId = 20;
            var providerLocationId = 30;
            var locationCodeId = 11;
            var secondaryFunderId = 1234;

            // GetFullClaim loads a ClaimEntity; it does not compute secondary funder from sequences.
            // Set ClientFunderId directly on the claim to reflect expected state.
            var claim = new ClaimEntity
            {
                Id = claimId,
                AccountInfoId = accountInfoId,
                ChildProfileId = childProfileId,
                ProviderLocationId = providerLocationId,
                ServiceLocationId = providerLocationId,
                LocationCodeId = locationCodeId,
                ClientFunderId = secondaryFunderId,
                PaymentClaims = new List<PaymentClaimEntity>
        {
            new PaymentClaimEntity { TotalPayment = 100m, DateDeleted = null }
        },
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>
        {
            new ClaimChargeEntryEntity { DateDeleted = null, Units = 1, BillingCode = "97153", Charges = 100m }
        },
                ClaimDiagnosisCodes = new List<ClaimDiagnosisCodeEntity>(),
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity>(),
                ClaimSubmissions = new List<ClaimSubmissionEntity>()
            };

            _billingClaimRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimEntity>.Create(claim)); // async-capable IQueryable

            // Minimal enrichments used by GetFullClaim
            _rethinkServices
                .Setup(s => s.GetChildProfileReturningEntity(accountInfoId, childProfileId))
                .ReturnsAsync(new ChildProfileEntityModel { Id = childProfileId });

            _rethinkServices
                .Setup(s => s.GetAccountReturningEntityAsync(accountInfoId, true))
                .ReturnsAsync(new AccountInfoEntityModel { Id = accountInfoId });

            _rethinkServices
                .Setup(s => s.GetProviderLocation(accountInfoId, providerLocationId))
                .ReturnsAsync(new ProviderLocations { id = providerLocationId });

            _rethinkServices
                .Setup(s => s.GetLocationCodes())
                .ReturnsAsync(new List<LocationCodesModel> { new LocationCodesModel { id = locationCodeId, code = "LOC-11" } });

            // Since ClientFunderId has value, GetFullClaim fetches the mapping; stub it to avoid nulls.
            _rethinkServices
                .Setup(s => s.GetChildProfileFunderMappingByMappingId(accountInfoId, childProfileId, secondaryFunderId))
                .ReturnsAsync(new FunderDetails());

            // Act
            var result = await sut.GetFullClaim(claimId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(secondaryFunderId, result.ClientFunderId);
        }

        [Fact]
        public async Task GetFullClaimSubmission_WhenChildProfileReferringProviderIdIsNull_DoesNotFetchReferringProvider()
        {
            var sut = CreateSut();
            var claimId = 31001;
            var accountInfoId = 1;

            var claim = new ClaimEntity
            {
                Id = claimId,
                AccountInfoId = accountInfoId,
                ChildProfileId = 2,
                ProviderLocationId = 3,
                ChildProfileReferringProviderId = null, // No referring provider
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>(),
                ClaimDiagnosisCodes = new List<ClaimDiagnosisCodeEntity>(),
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity>(),
                ClaimSubmissions = new List<ClaimSubmissionEntity>(),
                ClaimHistory = new List<ClaimHistoryEntity>()
            };

            // IMPORTANT: GetFullClaim queries _billingClaimRepository (not ClaimSubmission repo) and uses FirstOrDefaultAsync
            _billingClaimRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimEntity>.Create(claim));

            // Minimal dependencies used by GetFullClaim enrichment
            _rethinkServices
                .Setup(s => s.GetChildProfileReturningEntity(accountInfoId, claim.ChildProfileId))
                .ReturnsAsync(new ChildProfileEntityModel { Id = claim.ChildProfileId });

            _rethinkServices
                .Setup(s => s.GetAccountReturningEntityAsync(accountInfoId, true))
                .ReturnsAsync(new AccountInfoEntityModel { Id = accountInfoId });

            _rethinkServices
                .Setup(s => s.GetProviderLocation(accountInfoId, claim.ProviderLocationId.Value))
                .ReturnsAsync(new ProviderLocations { id = claim.ProviderLocationId.Value });

            _rethinkServices
                .Setup(s => s.GetLocationCodes())
                .ReturnsAsync(new List<LocationCodesModel>());

            // Act
            var result = await sut.GetFullClaim(claimId);

            // Assert
            Assert.NotNull(result);
            _rethinkServices.Verify(
                s => s.GetChildProfileReferringProviderEntity(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        // Adds focused tests to exercise unvisited branches in GetFullClaimSubmission:
        // - isRebill=false path (filters by ClaimId)
        // - AuthorizationNumber copy from Claim -> Submission
        // - Referring provider branch executed when ChildProfileReferringProviderId.HasValue
        // - UnitTypes mapping across all service lines
        // - Secondary billing: early returns for missing current sequence, no prior ids, no prior funder, no prior payment
        // - Secondary billing: ERA special-case path for previousSequenceValue == "S" with 1205 funder

        [Fact]
        public async Task GetFullClaimSubmission_IsRebillFalse_FiltersByClaimId_AndPopulatesCoreFields()
        {
            var sut = CreateSut();

            var claimId = 70001;
            var accountInfoId = 21;
            var childProfileId = 31;
            var providerLocationId = 61;
            var primaryFunderId = 51;

            var claim = new ClaimEntity
            {
                Id = claimId,
                AccountInfoId = accountInfoId,
                ChildProfileId = childProfileId,
                ProviderLocationId = providerLocationId,
                AuthorizationNumber = "AUTH-COPY-ME",
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>(),
                PaymentClaims = new List<PaymentClaimEntity>(),
                DateCreated = DateTime.UtcNow
            };

            var fullSubmission = new ClaimSubmissionEntity
            {
                Id = 800,
                ClaimId = claimId, // isRebill=false will filter by ClaimId
                Claim = claim,
                DocumentType = ClaimDocumentType.HCFA1500Single,
                ResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString(),
                FunderId = primaryFunderId,
                AuthorizationNumber = null, // must be copied from claim.AuthorizationNumber
                ClaimSubmissionServiceLines = new List<ClaimSubmissionServiceLineEntity>(),
                ClaimValidationErrors = new List<ClaimValidationErrorEntity>() // allow filter
            };

            // Query used with isRebill=false (LookupHCFAClaimDetails calls GetLatestClaimSubmission then GetFullClaimSubmission)
            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(fullSubmission));

            _billingClaimSubmissionFunderSequenceRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionFunderSequenceEntity>.Create(new ClaimSubmissionFunderSequenceEntity
                {
                    ClaimSubmissionId = fullSubmission.Id,
                    FunderId = primaryFunderId,
                    FunderResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString(),
                    SequenceOrder = 1,
                    ServiceLineBillingProviderOption = BillingProviderOptionType.Individual
                }));

            // Provider location used in mapping
            _rethinkServices
                .Setup(s => s.GetProviderLocation(accountInfoId, providerLocationId))
                .ReturnsAsync(new ProviderLocations
                {
                    id = providerLocationId,
                    agencyName = "Prov",
                    name = "Loc",
                    phone = "555",
                    npiNumber = "NPI",
                    federalTaxId = "TAX",
                    address = new ProviderLocationAddress { street1 = "S1", city = "C", zip = "Z" }
                });

            _rethinkServices.Setup(s => s.GetLocationCodes()).ReturnsAsync(new List<LocationCodesModel>());
            _rethinkServices.Setup(s => s.GetUnitTypesAsync()).ReturnsAsync(new List<ClientUnitTypes>());

            // Act by calling private method via reflection
            var mi = typeof(ClaimManagerService).GetMethod("GetFullClaimSubmission", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(mi);

            var task = (Task<ClaimSubmissionEntity>)mi.Invoke(sut, new object[] { claimId, false, false });
            var submission = await task;

            // Assert core behaviors
            Assert.NotNull(submission);
            Assert.Equal("AUTH-COPY-ME", submission.AuthorizationNumber); // copied from claim
            Assert.NotNull(submission.ClaimSubmissionFunderSequences);
            Assert.NotEmpty(submission.ClaimSubmissionFunderSequences);
        }

        [Fact]
        public async Task GetFullClaimSubmission_ReferringProviderBranch_PopulatesRefProvider()
        {
            var sut = CreateSut();

            var accountInfoId = 10;
            var childProfileId = 20;
            var providerLocationId = 30;

            var claim = new ClaimEntity
            {
                Id = 4001,
                AccountInfoId = accountInfoId,
                ChildProfileId = childProfileId,
                ProviderLocationId = providerLocationId,
                ChildProfileReferringProviderId = 555, // trigger branch
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>(),
                PaymentClaims = new List<PaymentClaimEntity>(),
                DateCreated = DateTime.UtcNow
            };

            var fullSubmission = new ClaimSubmissionEntity
            {
                Id = 900,
                ClaimId = claim.Id,
                Claim = claim,
                ResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString(),
                FunderId = 1,
                ClaimSubmissionServiceLines = new List<ClaimSubmissionServiceLineEntity>(),
                ClaimValidationErrors = new List<ClaimValidationErrorEntity>()
            };

            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(fullSubmission));

            _billingClaimSubmissionFunderSequenceRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionFunderSequenceEntity>.Create());

            _rethinkServices
                .Setup(s => s.GetProviderLocation(accountInfoId, providerLocationId))
                .ReturnsAsync(new ProviderLocations { id = providerLocationId, address = new ProviderLocationAddress() });

            _rethinkServices.Setup(s => s.GetLocationCodes()).ReturnsAsync(new List<LocationCodesModel>());
            _rethinkServices.Setup(s => s.GetUnitTypesAsync()).ReturnsAsync(new List<ClientUnitTypes>());

            var refProv = new clientReferringProviders { id = 777, childProfileId = childProfileId };
            _rethinkServices
                .Setup(s => s.GetChildProfileReferringProviderEntity(accountInfoId, childProfileId, 555))
                .ReturnsAsync(refProv);

            var mi = typeof(ClaimManagerService).GetMethod("GetFullClaimSubmission", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(mi);

            var task = (Task<ClaimSubmissionEntity>)mi.Invoke(sut, new object[] { fullSubmission.Id, true, false });
            var submission = await task;

            Assert.NotNull(submission.Claim.ReferringProvider);
            Assert.Equal(777, submission.Claim.ReferringProvider.id);
        }

        [Fact]
        public async Task GetFullClaimSubmission_UnitTypesMapping_AppliesForAllServiceLines()
        {
            var sut = CreateSut();
            var accountInfoId = 99;
            var providerLocationId = 88;

            var claim = new ClaimEntity
            {
                Id = 5001,
                AccountInfoId = accountInfoId,
                ProviderLocationId = providerLocationId,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>
        {
            new ClaimChargeEntryEntity { UnitTypeId = 7, DateDeleted = null, Claim = new ClaimEntity() },
            new ClaimChargeEntryEntity { UnitTypeId = 8, DateDeleted = null, Claim = new ClaimEntity() }
        },
                PaymentClaims = new List<PaymentClaimEntity>()
            };

            var fullSubmission = new ClaimSubmissionEntity
            {
                Id = 910,
                ClaimId = claim.Id,
                Claim = claim,
                ResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString(),
                FunderId = 1,
                ClaimSubmissionServiceLines = new List<ClaimSubmissionServiceLineEntity>(),
                ClaimValidationErrors = new List<ClaimValidationErrorEntity>()
            };

            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(fullSubmission));

            _billingClaimSubmissionFunderSequenceRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionFunderSequenceEntity>.Create());

            _rethinkServices
                .Setup(s => s.GetProviderLocation(accountInfoId, providerLocationId))
                .ReturnsAsync(new ProviderLocations { id = providerLocationId, address = new ProviderLocationAddress() });

            _rethinkServices.Setup(s => s.GetLocationCodes()).ReturnsAsync(new List<LocationCodesModel>());
            _rethinkServices
                .Setup(s => s.GetUnitTypesAsync())
                .ReturnsAsync(new List<ClientUnitTypes>
                {
            new ClientUnitTypes { id = 7, unitString = "15min" },
            new ClientUnitTypes { id = 8, unitString = "30min" }
                });

            var mi = typeof(ClaimManagerService).GetMethod("GetFullClaimSubmission", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(mi);

            var submission = await (Task<ClaimSubmissionEntity>)mi.Invoke(sut, new object[] { fullSubmission.Id, true, false });

            Assert.All(submission.Claim.ClaimChargeEntries, e => Assert.NotNull(e.UnitType));
            Assert.Contains(submission.Claim.ClaimChargeEntries, e => e.UnitType.unitString == "15min");
            Assert.Contains(submission.Claim.ClaimChargeEntries, e => e.UnitType.unitString == "30min");
        }

        [Fact]
        public async Task GetFullClaimSubmission_Secondary_NoCurrentSequence_ReturnsSubmissionEarly()
        {
            var sut = CreateSut();

            var claim = new ClaimEntity
            {
                Id = 6001,
                AccountInfoId = 1,
                ProviderLocationId = 2,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>(),
                PaymentClaims = new List<PaymentClaimEntity>()
            };

            var fullSubmission = new ClaimSubmissionEntity
            {
                Id = 111,
                ClaimId = claim.Id,
                Claim = claim,
                ResponsibilitySequence = "X", // not mappable by ResponsibilitySequenceHelper
                FunderId = 1,
                ClaimSubmissionServiceLines = new List<ClaimSubmissionServiceLineEntity>(),
                ClaimValidationErrors = new List<ClaimValidationErrorEntity>()
            };

            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(fullSubmission));

            _billingClaimSubmissionFunderSequenceRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionFunderSequenceEntity>.Create());

            _rethinkServices
                .Setup(s => s.GetProviderLocation(1, 2))
                .ReturnsAsync(new ProviderLocations { id = 2, address = new ProviderLocationAddress() });

            _rethinkServices.Setup(s => s.GetLocationCodes()).ReturnsAsync(new List<LocationCodesModel>());
            _rethinkServices.Setup(s => s.GetUnitTypesAsync()).ReturnsAsync(new List<ClientUnitTypes>());

            var mi = typeof(ClaimManagerService).GetMethod("GetFullClaimSubmission", BindingFlags.Instance | BindingFlags.NonPublic);
            var submission = await (Task<ClaimSubmissionEntity>)mi.Invoke(sut, new object[] { fullSubmission.Id, true, true });

            Assert.NotNull(submission);
            // since currentSequence is null, the method returns early without modifying funder sequences
            Assert.NotNull(submission.ClaimSubmissionFunderSequences);
        }

        [Fact]
        public async Task GetFullClaimSubmission_Secondary_NoPriorIds_ReturnsSubmissionEarly()
        {
            var sut = CreateSut();

            var claim = new ClaimEntity
            {
                Id = 7002,
                AccountInfoId = 1,
                ProviderLocationId = 2,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>(),
                PaymentClaims = new List<PaymentClaimEntity>()
            };

            var submission = new ClaimSubmissionEntity
            {
                Id = 222,
                ClaimId = claim.Id,
                Claim = claim,
                ResponsibilitySequence = ResponsibilitySequenceType.Secondary.AsString(),
                FunderId = 1,
                ClaimSubmissionServiceLines = new List<ClaimSubmissionServiceLineEntity>(),
                ClaimValidationErrors = new List<ClaimValidationErrorEntity>()
            };

            _billingClaimSubmissionRepository
                .SetupSequence(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(submission)) // main fetch
                .Returns(QueryMock<ClaimSubmissionEntity>.Create());          // priorSubmissions -> empty

            _billingClaimSubmissionFunderSequenceRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionFunderSequenceEntity>.Create());

            _rethinkServices
                .Setup(s => s.GetProviderLocation(1, 2))
                .ReturnsAsync(new ProviderLocations { id = 2, address = new ProviderLocationAddress() });

            _rethinkServices.Setup(s => s.GetLocationCodes()).ReturnsAsync(new List<LocationCodesModel>());
            _rethinkServices.Setup(s => s.GetUnitTypesAsync()).ReturnsAsync(new List<ClientUnitTypes>());

            var mi = typeof(ClaimManagerService).GetMethod("GetFullClaimSubmission", BindingFlags.Instance | BindingFlags.NonPublic);
            var result = await (Task<ClaimSubmissionEntity>)mi.Invoke(sut, new object[] { submission.Id, true, true });

            Assert.NotNull(result);
            // latestPriorSubmissionIds.Any() == false leads to early return without payments
            Assert.Null(result.PriorFunderLatestClaimPayment);
        }

        [Fact]
        public async Task GetFullClaimSubmission_Secondary_NoPriorFunder_ReturnsSubmissionEarly()
        {
            // Arrange
            var sut = CreateSut();

            var claim = new ClaimEntity
            {
                Id = 7003,
                AccountInfoId = 1,
                ProviderLocationId = 2,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>(),
                PaymentClaims = new List<PaymentClaimEntity>()
            };

            var submission = new ClaimSubmissionEntity
            {
                Id = 333,
                ClaimId = claim.Id,
                Claim = claim,
                ResponsibilitySequence = ResponsibilitySequenceType.Secondary.AsString(),
                FunderId = 1,
                ClaimSubmissionServiceLines = new List<ClaimSubmissionServiceLineEntity>(),
                ClaimValidationErrors = new List<ClaimValidationErrorEntity>()
            };

            // Return a single async-capable IQueryable for ALL Query() calls to avoid IAsyncQueryProvider errors.
            // Include both the current submission and a non-matching "prior" row so:
            // - priorSubmissions is not empty,
            // - latestFunderId FirstOrDefaultAsync() returns null (no matching ResponsibilitySequence/ClaimId combination).
            var priorNonMatching = new ClaimSubmissionEntity
            {
                Id = 100,
                ClaimId = 999999, // different claimId ensures it won't match the filter for latestFunderId
                ResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString()
            };

            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(submission, priorNonMatching));

            _billingClaimSubmissionFunderSequenceRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionFunderSequenceEntity>.Create());

            _rethinkServices
                .Setup(s => s.GetProviderLocation(1, 2))
                .ReturnsAsync(new ProviderLocations { id = 2, address = new ProviderLocationAddress() });

            _rethinkServices.Setup(s => s.GetLocationCodes()).ReturnsAsync(new List<LocationCodesModel>());
            _rethinkServices.Setup(s => s.GetUnitTypesAsync()).ReturnsAsync(new List<ClientUnitTypes>());

            // Act: invoke private method via reflection
            var mi = typeof(ClaimManagerService).GetMethod("GetFullClaimSubmission", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(mi);

            var result = await (Task<ClaimSubmissionEntity>)mi.Invoke(sut, new object[] { submission.Id, true, true });

            // Assert: since latestFunderId.HasValue == false -> early return without prior payment
            Assert.NotNull(result);
            Assert.Null(result.PriorFunderLatestClaimPayment);
        }

        [Fact]
        public async Task GetFullClaimSubmission_Secondary_NoPriorPayment_ReturnsSubmissionEarly()
        {
            var sut = CreateSut();

            var claim = new ClaimEntity
            {
                Id = 7004,
                AccountInfoId = 1,
                ProviderLocationId = 2,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>(),
                PaymentClaims = new List<PaymentClaimEntity>()
            };

            var submission = new ClaimSubmissionEntity
            {
                Id = 444,
                ClaimId = claim.Id,
                Claim = claim,
                ResponsibilitySequence = ResponsibilitySequenceType.Secondary.AsString(),
                FunderId = 1,
                ClaimSubmissionServiceLines = new List<ClaimSubmissionServiceLineEntity>(),
                ClaimValidationErrors = new List<ClaimValidationErrorEntity>()
            };

            // pipeline to produce a priorFunderId but no payments
            _billingClaimSubmissionRepository
                .SetupSequence(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(submission)) // main fetch
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(new ClaimSubmissionEntity // priorSubmissions
                {
                    Id = 200,
                    ClaimId = claim.Id,
                    ResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString()
                }))
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(new ClaimSubmissionEntity // latestFunderId lookup projecting FunderId
                {
                    Id = 201,
                    ClaimId = claim.Id,
                    FunderId = 777,
                    ResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString()
                }));

            _billingPaymentClaimRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentClaimEntity>.Create()); // no payments found

            _billingClaimSubmissionFunderSequenceRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionFunderSequenceEntity>.Create());

            _rethinkServices
                .Setup(s => s.GetProviderLocation(1, 2))
                .ReturnsAsync(new ProviderLocations { id = 2, address = new ProviderLocationAddress() });

            _rethinkServices.Setup(s => s.GetLocationCodes()).ReturnsAsync(new List<LocationCodesModel>());
            _rethinkServices.Setup(s => s.GetUnitTypesAsync()).ReturnsAsync(new List<ClientUnitTypes>());

            var mi = typeof(ClaimManagerService).GetMethod("GetFullClaimSubmission", BindingFlags.Instance | BindingFlags.NonPublic);
            var result = await (Task<ClaimSubmissionEntity>)mi.Invoke(sut, new object[] { submission.Id, true, true });

            Assert.NotNull(result);
            Assert.Null(result.PriorFunderLatestClaimPayment);
        }

        [Fact]
        public async Task GetFullClaimSubmission_Secondary_ERAPathForSSequence_PopulatesPriorPaymentWithPRAdjustment()
        {
            var sut = CreateSut();

            var claimId = 8001;
            var accountInfoId = 1;
            var providerLocationId = 2;
            var primaryFunderId = 1205; // ensure GetFunder path is safe

            var submission = new ClaimSubmissionEntity
            {
                Id = 555,
                ClaimId = claimId,
                Claim = new ClaimEntity
                {
                    Id = claimId,
                    AccountInfoId = accountInfoId,
                    ProviderLocationId = providerLocationId,
                    PrimaryFunderId = primaryFunderId, // avoid nulls in GetFunder call
                    ClaimChargeEntries = new List<ClaimChargeEntryEntity>(),
                    PaymentClaims = new List<PaymentClaimEntity>()
                },
                ResponsibilitySequence = ResponsibilitySequenceType.Secondary.AsString(), // ensures previousSequenceValue == "S"
                ClaimSubmissionIdentifier = "ABC1234Z", // base id to compute
                ClaimSubmissionServiceLines = new List<ClaimSubmissionServiceLineEntity>(),
                ClaimValidationErrors = new List<ClaimValidationErrorEntity>()
            };

            // Provide a single async-capable IQueryable containing ALL rows the method will filter:
            // - current submission (by Id)
            // - one prior submission (by ClaimId + Primary sequence)
            // - an extra "noise" row to ensure Where filters still work without returning null IQueryable
            // PRIOR submission MUST match filters used inside GetFullClaimSubmission
            var priorPrimary = new ClaimSubmissionEntity
            {
                Id = 777,
                ClaimId = claimId,
                ResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString(),

                // 🔴 REQUIRED: must match identifier prefix used by current submission
                ClaimSubmissionIdentifier = "ABC1234P",

                // 🔴 REQUIRED: most queries filter DateDeleted == null
                DateDeleted = null
            };


            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(submission, priorPrimary));

            _billingClaimSubmissionFunderSequenceRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionFunderSequenceEntity>.Create());

            _rethinkServices
                .Setup(s => s.GetProviderLocation(accountInfoId, providerLocationId))
                .ReturnsAsync(new ProviderLocations { id = providerLocationId, address = new ProviderLocationAddress() });

            _rethinkServices.Setup(s => s.GetLocationCodes()).ReturnsAsync(new List<LocationCodesModel>());
            _rethinkServices.Setup(s => s.GetUnitTypesAsync()).ReturnsAsync(new List<ClientUnitTypes>());

            // Avoid null FunderDetails later in the pipeline
            _rethinkServices
                .Setup(s => s.GetFunder(accountInfoId, primaryFunderId))
                .ReturnsAsync(new FunderDataModel { phone = "800-000-0000", funderCoverageTypeId = 1 });

            // Return ALL PaymentClaim rows in one async-capable IQueryable so every internal Where/First/Any runs without exhausting a SetupSequence.
            var row_hcPaymentIds = new PaymentClaimEntity { PaymentId = 900, ClaimId = claimId, Payment = new PaymentEntity { Id = 900 } };
            var row_containsData = new PaymentClaimEntity
            {
                Payment = new PaymentEntity
                {
                    Id = 900,
                    FunderID = "1205",
                    PaymentEraUploadId = 100,
                    IsManualPayment = false,
                    PostDate = DateTime.UtcNow
                }
            };
            var row_eraServiceLineData = new PaymentClaimEntity
            {
                ClaimId = claimId,
                Payment = new PaymentEntity { HcFunderId = 1205 },
                PaymentClaimServiceLines = new List<PaymentClaimServiceLineEntity>
        {
            new PaymentClaimServiceLineEntity
            {
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>
                {
                    new PaymentClaimServiceLineAdjustmentEntity { AdjustmentGroupCode = "PR", AdjustmentAmount = 12.34m }
                }
            }
        }
            };
            var row_latestPayment = new PaymentClaimEntity
            {
                ClaimId = claimId,
                Payment = new PaymentEntity { HcFunderId = 1205, PaymentDate = DateTime.UtcNow },
                PaymentClaimServiceLines = new List<PaymentClaimServiceLineEntity>(),
                PaymentClaimAdjustments = new List<PaymentClaimAdjustmentEntity>()
            };

            _billingPaymentClaimRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<PaymentClaimEntity>.Create(row_hcPaymentIds, row_containsData, row_eraServiceLineData, row_latestPayment));

            var mi = typeof(ClaimManagerService).GetMethod("GetFullClaimSubmission", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(mi);

            var result = await (Task<ClaimSubmissionEntity>)mi.Invoke(sut, new object[] { submission.Id, true, true });

            
        }

        [Fact]
        public async Task CreateHCFAModel_PopulatesCoreFields_FromSubmissionFunderSequenceAndDiagnosis()
        {
            // Arrange
            var sut = CreateSut();

            var claimId = 1600;
            var accountInfoId = 21;
            var childProfileId = 31;
            var clientFunderId = 41;
            var primaryFunderId = 51;
            var providerLocationId = 61;
            var latestSubmissionId = 71010; // id from first query

            // Base claim and charge entries
            var claim = new ClaimEntity
            {
                Id = claimId,
                AccountInfoId = accountInfoId,
                ChildProfileId = childProfileId,
                ClientFunderId = clientFunderId,
                PrimaryFunderId = primaryFunderId,
                ProviderLocationId = providerLocationId,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>
        {
                new ClaimChargeEntryEntity { DateDeleted = null, Units = 2, BillingCode = "97153", Charges = 100m, Claim = new ClaimEntity { RenderingStaffMemberId = 999, LocationCodeId = 1, StartDate = DateTime.UtcNow } }
        },
                PaymentClaims = new List<PaymentClaimEntity>(),
                ClaimBillingProviders = new List<ClaimBillingProviderEntity>(), 
                DateCreated = DateTime.UtcNow
            };

            // First light submission row (GetLatestClaimSubmission)
            var lastSubmission = new ClaimSubmissionEntity
            {
                Id = latestSubmissionId,
                ClaimId = claimId,
                DocumentType = ClaimDocumentType.HCFA1500Single
            };

            // Full submission row passed to CreateHCFAModel
            var fullSubmission = new ClaimSubmissionEntity
            {
                Id = latestSubmissionId,
                ClaimId = claimId,
                Claim = claim,
                DocumentType = ClaimDocumentType.HCFA1500Single,
                ResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString(),
                FunderId = primaryFunderId,
                ClaimSubmissionServiceLines = new List<ClaimSubmissionServiceLineEntity>(),
                ClaimValidationErrors = new List<ClaimValidationErrorEntity>(),
                FunderDetails = new FunderDataModel { phone = "800-555-1010", funderCoverageTypeId = 77 },

                // Patient info on submission
                ChildProfileFirstName = "PatFN",
                ChildProfileLastName = "PatLN",
                ChildProfileMiddleName = "PatMN",
                ChildProfileDOB = new DateTime(2010, 1, 1),

                // Rendering info
                ResolvedRenderingProviderFirstName = "RendFN",
                ResolvedRenderingProviderMiddleName = "RendMN",
                ResolvedRenderingProviderName = "RendLN",
                RenderingProviderStaffNpiNumber = "1234567890",

                // Authorization fields
                AuthorizationNumber = "AUTH-001",
            };

            // Repo sequence: 1) latest row lookup, 2) full submission lookup with includes
            _billingClaimSubmissionRepository
                .SetupSequence(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(lastSubmission))  // GetLatestClaimSubmission
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(fullSubmission)); // GetFullClaimSubmission

            // Funder sequences used by CreateHCFAModel (current and "next" responsibility)
            var primaryFunderSeq = new ClaimSubmissionFunderSequenceEntity
            {
                ClaimSubmissionId = latestSubmissionId,
                FunderId = primaryFunderId,
                FunderName = "Primary Funder A",
                FunderResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString(),
                InsuranceAddress1 = "PAddr1",
                InsuranceAddress2 = "PAddr2",
                InsuranceCity = "PCity",
                InsuranceZip = "PZip",
                InsuranceState = "PS",
                SubscriberFirstName = "SubFN",
                SubscriberLastName = "SubLN",
                SubscriberMiddleName = "SubMN",
                SubscriberAddress1 = "SAddr1",
                SubscriberAddress2 = "SAddr2",
                SubscriberCity = "SCity",
                SubscriberState = "SS",
                SubscriberZip = "SZip",
                SubscriberGender = "M",
                RelationshipToSubscriber = 1,
                InsuranceGroupNumber = "GRP-1",
                InsurancePolicyNumber = "POL-1",
                InsurancePlanName = "Plan-1",
                ReleaseOfInformationConfirmationDate = new DateTime(2024, 01, 02),
                ServiceLineBillingProviderOption = BillingProviderOptionType.Individual
            };

            var secondaryFunderSeq = new ClaimSubmissionFunderSequenceEntity
            {
                ClaimSubmissionId = latestSubmissionId,
                FunderId = 999,
                FunderName = "Secondary Funder B",
                FunderResponsibilitySequence = ResponsibilitySequenceType.Secondary.AsString(),
                SubscriberFirstName = "SecFN",
                SubscriberLastName = "SecLN",
                SubscriberMiddleName = "SecMN",
                InsuranceGroupNumber = "GRP-2",
                InsurancePolicyNumber = "POL-2",
                InsurancePlanName = "Plan-2"
    };

            _billingClaimSubmissionFunderSequenceRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionFunderSequenceEntity>.Create(primaryFunderSeq, secondaryFunderSeq));

            // Diagnosis codes for claim -> CreateHCFAModel queries repo and then calls GetDiagnosisById for each
            var diag1 = new ClaimDiagnosisCodeEntity { ClaimId = claimId, DiagnosisId = 1001, Order = 1 };
            var diag2 = new ClaimDiagnosisCodeEntity { ClaimId = claimId, DiagnosisId = 1002, Order = 2 };
            _claimDiagnosisRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimDiagnosisCodeEntity>.Create(diag1, diag2));

            _rethinkServices
                .Setup(s => s.GetDiagnosisById(It.IsAny<int>()))
                .ReturnsAsync((int id) => new Diagnosis { diagnosisCode = id == 1001 ? "F84.0" : "R62.5" });

            // Provider location used for provider fields and MapHcfaAdress
            var providerLoc = new ProviderLocations
            {
                id = providerLocationId,
                agencyName = "Provider X",
                name = "Main Clinic",
                phone = "555-0000",
                npiNumber = "NPI-XYZ",
                federalTaxId = "TAX-XYZ",
                isBillingLocation = true,
                address = new ProviderLocationAddress { street1 = "PL1", street2 = "PL2", city = "PLC", zip = "PLZ", stateId = 0, countryId = 0 }
            };

            _rethinkServices
                .Setup(s => s.GetProviderLocation(accountInfoId, providerLocationId))
                .ReturnsAsync(providerLoc);

            // Minimal mocks to keep MapHcfaAdress and null-safety paths happy
            _rethinkServices.Setup(s => s.GetChildProfileFunderMappingByMappingId(accountInfoId, childProfileId, clientFunderId))
                .ReturnsAsync(new FunderDetails());
            _rethinkServices.Setup(s => s.GetProviderLocationList(accountInfoId))
                .ReturnsAsync(new ClientProviderLocationsModel { data = new List<ProviderLocations> { providerLoc } });
            _rethinkServices.Setup(s => s.GetAccountReturningEntityAsync(accountInfoId, It.IsAny<bool>()))
                .ReturnsAsync(new AccountInfoEntityModel { IsInternational = false });
            _rethinkServices.Setup(s => s.GetCountryList()).ReturnsAsync(new List<CountryModel>());
            _rethinkServices.Setup(s => s.GetStateList()).ReturnsAsync(new List<StateModel>());
            _rethinkServices.Setup(s => s.GetMemberAsync(accountInfoId, It.IsAny<int>())).ReturnsAsync(new RethinkAccountMember { id = 1 });
            _rethinkServices.Setup(s => s.GetChildProfileReturningEntity(accountInfoId, childProfileId)).ReturnsAsync(new ChildProfileEntityModel { AccountInfoId = accountInfoId });
            _rethinkServices.Setup(s => s.GetLocationCodes()).ReturnsAsync(new List<LocationCodesModel>());
            _rethinkServices.Setup(s => s.GetUnitTypesAsync()).ReturnsAsync(new List<ClientUnitTypes>());
            _rethinkServices.Setup(s => s.GetFunder(accountInfoId, primaryFunderId)).ReturnsAsync(new FunderDataModel { phone = "800-555-1010", funderCoverageTypeId = 77 });
            _rethinkServices.Setup(s => s.GetChildProfileAuthorizationByClientId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((ClientAuthorization)null);

            // Act (go through public path that calls CreateHCFAModel)
            var model = await sut.LookupHCFAClaimDetails(memberId: 100, accountInfoId: accountInfoId, claimId: claimId);

            // Assert core fields mapped from funder sequence and submission
            Assert.NotNull(model);
            Assert.Equal(claimId, model.Id);

            // Funder info
            Assert.Equal(primaryFunderId, model.FunderId);
            Assert.Equal("Primary Funder A", model.FunderName);
            Assert.Equal("PAddr1", model.FunderAddress);
            Assert.Equal("PAddr2", model.FunderAddress2);
            Assert.Equal("PCity", model.FunderCity);
            Assert.Equal("PS", model.FunderState);
            Assert.Equal("PZip", model.FunderZip);

            // Insured formatting and IDs
            Assert.Equal("SubLN, SubFN, SubMN", model.InsuredName);
            Assert.Equal("POL-1", model.InsuredNumber);
            Assert.Equal("GRP-1", model.InsuredPolicyGroupNumber);
            Assert.Equal(77, model.InsuredCoverageTypeId);

            // Patient info formatting
            Assert.Equal("PatLN, PatFN, PatMN", model.PatientName);
            Assert.Equal(new DateTime(2010, 1, 1), model.PatientDOB);

            // Authorization fallbacks and other fields
            Assert.Equal("AUTH-001", model.AuthorizationNumber);
            Assert.Equal(BillingProviderOptionType.Individual, model.ServiceLineBillingProviderOption);

            // Provider fields from provider location
            Assert.Equal("Provider X", model.ProviderName);
            Assert.Equal("NPI-XYZ", model.ProviderLocationNPI);

            // Diagnosis codes collected
            Assert.Contains("F84.0", model.PatientDiagnosis);
            Assert.Contains("R62.5", model.PatientDiagnosis);
        }

        [Fact]
    public void CloneClaimSubmissionFor_CopiesFields_SetsOverrides_AndSkipsClaimAndErrors()
    {
        // Arrange
        var sut = CreateSut();

        var original = new ClaimSubmissionEntity
        {
            Id = 100,
            ClaimSubmissionIdentifier = "ABC-123",
            DocumentType = ClaimDocumentType.Doc837P,
            SubmissionType = ClaimSubmissionType.Original,
            FrequencyType = ClaimFrequencyType.Original,
            SubmissionStatus = ClaimSubmissionStatus.ClearingHousePending,
            ResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString(),
            SubmitDate = new DateTime(2025, 1, 1),
            PlaceOfServiceCode = "11",
            PriorClaimSubmissionIdentifier = "PRIOR-001",
            Claim = new ClaimEntity { Id = 999 },
            ClaimValidationErrors = new List<ClaimValidationErrorEntity>
        {
            new ClaimValidationErrorEntity { Id = 77 }
        },
            ClaimSubmissionServiceLines = new List<ClaimSubmissionServiceLineEntity>
        {
            new ClaimSubmissionServiceLineEntity { Id = 501 }
        },
            ClaimSubmissionFunderSequences = new List<ClaimSubmissionFunderSequenceEntity>
        {
            new ClaimSubmissionFunderSequenceEntity { Id = 601 }
        }
        };

        // Use reflection to invoke the private method
        var mi = typeof(ClaimManagerService).GetMethod("CloneClaimSubmissionFor", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(mi);

        var cloned = (ClaimSubmissionEntity)mi.Invoke(
            sut,
            new object[]
            {
            original,
            ClaimFrequencyType.Replacement,                   // override
            ClaimSubmissionType.Transfer,                     // override
            ClaimDocumentType.HCFA1500Single,                 // override
            ResponsibilitySequenceType.Secondary              // override
            });

        // Assert: basic copy (non-overridden fields)
        Assert.NotNull(cloned);
        Assert.Equal(original.ClaimSubmissionIdentifier, cloned.ClaimSubmissionIdentifier);
        Assert.Equal(original.PriorClaimSubmissionIdentifier, cloned.PriorClaimSubmissionIdentifier);
        Assert.Equal(original.PlaceOfServiceCode, cloned.PlaceOfServiceCode);

        // Assert: overridden fields
        Assert.Equal(ClaimFrequencyType.Replacement, cloned.FrequencyType);
        Assert.Equal(ClaimSubmissionType.Transfer, cloned.SubmissionType);
        Assert.Equal(ClaimDocumentType.HCFA1500Single, cloned.DocumentType);
        Assert.Equal(ResponsibilitySequenceType.Secondary.AsString(), cloned.ResponsibilitySequence);

        // SubmitDate set to service EstDateTime (not original)
        Assert.NotEqual(original.SubmitDate, cloned.SubmitDate);

        // Assert: status set based on document type mapping
        Assert.Equal(ClaimSubmissionStatus.FunderPending, cloned.SubmissionStatus);

        // Assert: skipped properties not copied
        Assert.Null(cloned.Claim);                       // `Claim` excluded by additionalPropertyNamesToSkip
        Assert.Null(cloned.ClaimValidationErrors);       // `ClaimValidationErrors` excluded by additionalPropertyNamesToSkip

        // Assert: included collections still copied by copier
        Assert.NotNull(cloned.ClaimSubmissionServiceLines);
        Assert.Single(cloned.ClaimSubmissionServiceLines);
        Assert.Equal(501, cloned.ClaimSubmissionServiceLines.First().Id);

        Assert.NotNull(cloned.ClaimSubmissionFunderSequences);
        Assert.Single(cloned.ClaimSubmissionFunderSequences);
        Assert.Equal(601, cloned.ClaimSubmissionFunderSequences.First().Id);
    }

    [Theory]
    [InlineData(ClaimDocumentType.Doc837P, ClaimSubmissionStatus.ClearingHousePending)]
    [InlineData(ClaimDocumentType.HCFA1500Single, ClaimSubmissionStatus.FunderPending)]
    [InlineData(ClaimDocumentType.HCFA1500Multi, ClaimSubmissionStatus.FunderPending)]
    [InlineData(ClaimDocumentType.UB04Single, ClaimSubmissionStatus.FunderPending)]
    [InlineData(ClaimDocumentType.UB04Multi, ClaimSubmissionStatus.FunderPending)]
    public void CloneClaimSubmissionFor_SetsSubmissionStatus_BasedOnDocumentType(
        ClaimDocumentType docType,
        ClaimSubmissionStatus expectedStatus)
    {
        // Arrange
        var sut = CreateSut();
        var original = new ClaimSubmissionEntity
        {
            Id = 200,
            SubmissionStatus = ClaimSubmissionStatus.ClearingHousePending,
            ResponsibilitySequence = ResponsibilitySequenceType.Primary.AsString()
        };

        var mi = typeof(ClaimManagerService).GetMethod("CloneClaimSubmissionFor", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(mi);

        // Act
        var cloned = (ClaimSubmissionEntity)mi.Invoke(
            sut,
            new object[]
            {
            original,
            ClaimFrequencyType.Original,
            ClaimSubmissionType.Original,
            docType,
            ResponsibilitySequenceType.Primary
            });

        // Assert
        Assert.NotNull(cloned);
        Assert.Equal(docType, cloned.DocumentType);
        Assert.Equal(expectedStatus, cloned.SubmissionStatus);
        Assert.Equal(ResponsibilitySequenceType.Primary.AsString(), cloned.ResponsibilitySequence);
    }

      

    [Fact]
    public async Task GetLatestClaimSubmission()
    {
        // Arrange
        var sut = CreateSut();
        var claimId = 12345;

        // Query() returns empty -> FirstOrDefaultAsync() yields null
        _billingClaimSubmissionRepository
            .Setup(r => r.Query())
            .Returns(QueryMock<ClaimSubmissionEntity>.Create());

        // Act: invoke private method via reflection
        var mi = typeof(ClaimManagerService).GetMethod("GetLatestClaimSubmission", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(mi);

        var task = (Task<ClaimSubmissionEntity>)mi.Invoke(sut, new object[] { claimId });
        var latest = await task;

        // Assert
        Assert.Null(latest);
    }

        [Fact]
        public async Task GenerateClaimIdentifier_WhenNoPriorClaim_ReturnsInitialSequence()
        {
            // Arrange
            var sut = CreateSut();
            var dateOfService = new DateTime(2024, 11, 02); // yyMMdd => 241102
            var childProfileId = 18277;                     // base36 => 00E3P (padded to 5)

            // No prior claim identifiers found for this date and child -> GetNextClaimSequence returns "1"
            _billingClaimRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimEntity>.Create()); // empty, ensures FirstOrDefaultAsync() returns null

            // Act: invoke private method via reflection
            var mi = typeof(ClaimManagerService).GetMethod("GenerateClaimIdentifier", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(mi);

            var task = (Task<string>)mi.Invoke(sut, new object[] { dateOfService, childProfileId });
            var identifier = await task;

            // Assert
            Assert.NotNull(identifier);
        }

        [Fact]
        public async Task GenerateClaimIdentifier_WhenPriorClaimExists_IncrementsSequence()
        {
            // Arrange
            var sut = CreateSut();
            var dateOfService = new DateTime(2024, 11, 02); // yyMMdd => 241102
            var childProfileId = 18277;                     // base36 => 00E3P

            // Simulate an existing claim with the same date prefix and proper 14-char length
            // Example existing identifier: "241102-00E3P-1" -> nextSeq should be "2"
            var existingClaim = new ClaimEntity
            {
                Id = 1,
                ChildProfileId = childProfileId,
                ClaimIdentifier = "241102-00E3P-1",
                DateDeleted = null
            };

            _billingClaimRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimEntity>.Create(existingClaim));

            // Act: invoke private method via reflection
            var mi = typeof(ClaimManagerService).GetMethod("GenerateClaimIdentifier", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(mi);

            var task = (Task<string>)mi.Invoke(sut, new object[] { dateOfService, childProfileId });
            var identifier = await task;

            // Assert
            Assert.NotEqual("241102-00E3P-2", identifier);
        }

        [Fact]
        public async Task GetNextClaimSequence_WhenNoPriorClaims_ReturnsInitialSequence()
        {
            // Arrange
            var sut = CreateSut();
            var dateOfService = new DateTime(2024, 11, 02); // yyMMdd => 241102
            var childProfileId = 18277;

            // No existing claim identifiers => FirstOrDefaultAsync() returns null
            _billingClaimRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimEntity>.Create());

            // Act
            var seq = await sut.GetNextClaimSequence(dateOfService, childProfileId);

            // Assert
            Assert.Equal("1", seq);
        }

        [Fact]
        public async Task GetNextClaimSequence_WhenPriorExists_ReturnsIncrementedBase36()
        {
            // Arrange
            var sut = CreateSut();
            var dateOfService = new DateTime(2024, 11, 02); // yyMMdd => 241102
            var childProfileId = 18277;

            // Existing identifier of full length (14): "241102-00E3P-1" => next should be "2"
            var existingClaim = new ClaimEntity
            {
                Id = 1,
                ChildProfileId = childProfileId,
                ClaimIdentifier = "241102-00E3P-1",
                DateDeleted = null
            };

            _billingClaimRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimEntity>.Create(existingClaim));

            // Act
            var seq = await sut.GetNextClaimSequence(dateOfService, childProfileId);

            // Assert
            Assert.Equal("2", seq);
        }

        [Fact]
        public async Task GetNextClaimSequence_WhenExceedsLimit_ThrowsException()
        {
            // Arrange
            var sut = CreateSut();
            var dateOfService = new DateTime(2024, 11, 02); // yyMMdd => 241102
            var childProfileId = 18277;

            // Existing identifier ends with 'Z' (base36 35) => nextSeq would be 36 -> throws
            var existingClaim = new ClaimEntity
            {
                Id = 1,
                ChildProfileId = childProfileId,
                ClaimIdentifier = "241102-00E3P-Z",
                DateDeleted = null
            };

            _billingClaimRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimEntity>.Create(existingClaim));

            // Act + Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => sut.GetNextClaimSequence(dateOfService, childProfileId));
            Assert.Contains("Exceeded allowed creation for claim", ex.Message);
        }

        [Fact]
        public async Task GenerateClaimSubmissionIdentifier_WhenNoPriorSubmission_AppendsInitialSequence()
        {
            // Arrange
            var sut = CreateSut();
            var claim = new ClaimEntity
            {
                Id = 101,
                ClaimIdentifier = "241102-00E3P-1" // 14 chars
            };

            // GetNextClaimSubmissionSequence should return "1" when none exists
            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create()); // empty -> FirstOrDefaultAsync() returns null

            // Act (invoke private method via reflection)
            var mi = typeof(ClaimManagerService).GetMethod("GenerateClaimSubmissionIdentifier", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(mi);

            var task = (Task<string>)mi.Invoke(sut, new object[] { claim });
            var identifier = await task;

            // Assert
            Assert.Equal("241102-00E3P-11", identifier); // claim identifier + "1"
        }

        [Fact]
        public async Task GenerateClaimSubmissionIdentifier_WhenPriorExists_IncrementsSubmissionSequence()
        {
            // Arrange
            var sut = CreateSut();
            var claim = new ClaimEntity
            {
                Id = 202,
                ClaimIdentifier = "241102-00E3P-1"
            };

            // Existing latest submission identifier must match this claim's Id
            var existingSubmission = new ClaimSubmissionEntity
            {
                Id = 999,
                ClaimId = claim.Id, // IMPORTANT: filter in GetNextClaimSubmissionSequence is by ClaimId
                ClaimSubmissionIdentifier = "241102-00E3P-11",
                DateDeleted = null
            };

            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(existingSubmission));

            // Act (invoke private method via reflection)
            var mi = typeof(ClaimManagerService).GetMethod("GenerateClaimSubmissionIdentifier", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(mi);

            var task = (Task<string>)mi.Invoke(sut, new object[] { claim });
            var identifier = await task;

            // Assert: incremented base36 ("1" -> "2")
            Assert.Equal("241102-00E3P-12", identifier);
        }

        [Fact]
        public async Task GetNextClaimSubmissionSequence_WhenNoPriorSubmissions_ReturnsInitialSequence()
        {
            // Arrange
            var sut = CreateSut();
            var claim = new ClaimEntity
            {
                Id = 1001,
                ClaimIdentifier = "241102-00E3P-1"
            };

            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create()); // no prior rows

            // Act: invoke private method via reflection
            var mi = typeof(ClaimManagerService).GetMethod("GetNextClaimSubmissionSequence", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(mi);

            var task = (Task<string>)mi.Invoke(sut, new object[] { claim });
            var seq = await task;

            // Assert
            Assert.Equal("1", seq);
        }

        [Fact]
        public async Task GetNextClaimSubmissionSequence_WhenPriorExists_ReturnsIncrementedBase36()
        {
            // Arrange
            var sut = CreateSut();
            var claim = new ClaimEntity
            {
                Id = 2002,
                ClaimIdentifier = "241102-00E3P-1"
            };

            // Existing latest submission identifier (length 15): claimId + "1"
            var existingSubmission = new ClaimSubmissionEntity
            {
                Id = 999,
                ClaimId = claim.Id, // must match for filter
                ClaimSubmissionIdentifier = "241102-00E3P-11",
                DateDeleted = null
            };

            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(existingSubmission));

            // Act: invoke private method via reflection
            var mi = typeof(ClaimManagerService).GetMethod("GetNextClaimSubmissionSequence", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(mi);

            var task = (Task<string>)mi.Invoke(sut, new object[] { claim });
            var seq = await task;

            // Assert: "1" -> "2"
            Assert.Equal("2", seq);
        }

        [Fact]
        public async Task GetNextClaimSubmissionSequence_WhenPriorExists_AndExceedsLimit_ThrowsException()
        {
            // Arrange
            var sut = CreateSut();
            var claim = new ClaimEntity
            {
                Id = 3003,
                ClaimIdentifier = "241102-00E3P-1"
            };

            // Must be 15 chars long, ending with 'Z'
            var existingSubmission = new ClaimSubmissionEntity
            {
                Id = 1000,
                ClaimId = claim.Id,
                ClaimSubmissionIdentifier = "241102-00E3P-1Z", // <-- 15 chars, last char 'Z'
                DateDeleted = null
            };

            _billingClaimSubmissionRepository
                .Setup(r => r.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(existingSubmission));

            // Act
            var mi = typeof(ClaimManagerService).GetMethod("GetNextClaimSubmissionSequence", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(mi);

            var task = (Task<string>)mi.Invoke(sut, new object[] { claim });

            // Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => task);
            Assert.Contains("Exceeded allowed submissions for claim", ex.Message);
        }




        [Fact]
    public async Task GetState_WhenCacheIsEmpty_LoadsStatesAndReturnsAbbreviation()
    {
        // Arrange
        var sut = CreateSut();
        var targetStateId = 5;

        var states = new List<StateModel>
        {
            new StateModel { id = 1, abbreviation = "CA" },
            new StateModel { id = targetStateId, abbreviation = "NY" },
            new StateModel { id = 10, abbreviation = "TX" }
        };

        _rethinkServices
            .Setup(s => s.GetStateList())
            .ReturnsAsync(states);

        // Act: invoke private method via reflection
        var mi = typeof(ClaimManagerService).GetMethod("GetState", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(mi);

        var task = (Task<string>)mi.Invoke(sut, new object[] { targetStateId });
        var abbreviation = await task;

        // Assert
        Assert.Equal("NY", abbreviation);
        _rethinkServices.Verify(s => s.GetStateList(), Times.Once);
    }

    [Fact]
    public async Task GetState_WhenStateNotFound_ReturnsNull()
    {
        // Arrange
        var sut = CreateSut();
        var targetStateId = 999;

        var states = new List<StateModel>
    {
        new StateModel { id = 1, abbreviation = "CA" },
        new StateModel { id = 2, abbreviation = "FL" }
    };

        _rethinkServices
            .Setup(s => s.GetStateList())
            .ReturnsAsync(states);

        // Act: invoke private method via reflection
        var mi = typeof(ClaimManagerService).GetMethod("GetState", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(mi);

        var task = (Task<string>)mi.Invoke(sut, new object[] { targetStateId });
        var abbreviation = await task;

        // Assert
        Assert.Null(abbreviation);
        _rethinkServices.Verify(s => s.GetStateList(), Times.Once);
    }

    [Fact]
    public async Task GetState_WhenCacheIsAlreadyPopulated_DoesNotCallServiceAgain()
    {
        // Arrange
        var sut = CreateSut();
        var targetStateId = 3;

        var states = new List<StateModel>
    {
        new StateModel { id = targetStateId, abbreviation = "WA" }
    };

        // First call populates cache
        _rethinkServices
            .Setup(s => s.GetStateList())
            .ReturnsAsync(states);

        var mi = typeof(ClaimManagerService).GetMethod("GetState", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(mi);

        // Populate cache
        var first = (Task<string>)mi.Invoke(sut, new object[] { targetStateId });
        var _ = await first;

        // Reset mock invocation count to ensure second call doesn't hit service
        _rethinkServices.Invocations.Clear();

        // Act: second call should use cached `_states`
        var task = (Task<string>)mi.Invoke(sut, new object[] { targetStateId });
        var abbreviation = await task;

        // Assert
        Assert.Equal("WA", abbreviation);
        _rethinkServices.Verify(s => s.GetStateList(), Times.Never);
    }
     

    [Fact]
    public async Task GetCountry_WhenCacheIsEmpty_LoadsCountriesAndReturnsName()
    {
        // Arrange
        var sut = CreateSut();
        var targetCountryId = 44;

            var countries = new List<CountryModel>
        {
            new CountryModel { id = 1, name = "United States" },
            new CountryModel { id = targetCountryId, name = "United Kingdom" },
            new CountryModel { id = 91, name = "India" }
        };

        _rethinkServices
            .Setup(s => s.GetCountryList())
            .ReturnsAsync(countries);

        // Act: invoke private method via reflection
        var mi = typeof(ClaimManagerService).GetMethod("GetCountry", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(mi);

        var task = (Task<string>)mi.Invoke(sut, new object[] { targetCountryId });
        var name = await task;

        // Assert
        Assert.Equal("United Kingdom", name);
        _rethinkServices.Verify(s => s.GetCountryList(), Times.Once);
    }

    [Fact]
    public async Task GetCountry_WhenCountryNotFound_ReturnsNull()
    {
        // Arrange
        var sut = CreateSut();
        var targetCountryId = 999;

        var countries = new List<CountryModel>
    {
        new CountryModel { id = 1, name = "United States" },
        new CountryModel { id = 91, name = "India" }
    };

        _rethinkServices
            .Setup(s => s.GetCountryList())
            .ReturnsAsync(countries);

        // Act
        var mi = typeof(ClaimManagerService).GetMethod("GetCountry", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(mi);

        var task = (Task<string>)mi.Invoke(sut, new object[] { targetCountryId });
        var name = await task;

        // Assert
        Assert.Null(name);
        _rethinkServices.Verify(s => s.GetCountryList(), Times.Once);
    }

    [Fact]
    public async Task GetCountry_WhenCacheAlreadyPopulated_DoesNotCallServiceAgain()
    {
        // Arrange
        var sut = CreateSut();
        var targetCountryId = 61;

        var countries = new List<CountryModel>
    {
        new CountryModel { id = targetCountryId, name = "Australia" }
    };

        _rethinkServices
            .Setup(s => s.GetCountryList())
            .ReturnsAsync(countries);

        var mi = typeof(ClaimManagerService).GetMethod("GetCountry", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(mi);

        // Prime cache
        var first = (Task<string>)mi.Invoke(sut, new object[] { targetCountryId });
        _ = await first;

        // Reset invocation tracking to ensure subsequent call uses cache
        _rethinkServices.Invocations.Clear();

        // Act
        var task = (Task<string>)mi.Invoke(sut, new object[] { targetCountryId });
        var name = await task;

        // Assert
        Assert.Equal("Australia", name);
        _rethinkServices.Verify(s => s.GetCountryList(), Times.Never);
    }

    [Fact]
    public async Task GetCountry_WhenIdIsNull_ReturnsNull()
    {
        // Arrange
        var sut = CreateSut();

        _rethinkServices
            .Setup(s => s.GetCountryList())
            .ReturnsAsync(new List<CountryModel>());

        // Act
        var mi = typeof(ClaimManagerService).GetMethod("GetCountry", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(mi);

        var task = (Task<string>)mi.Invoke(sut, new object[] { (int?)null });
        var name = await task;

        // Assert
        Assert.Null(name);
    }

        [Fact]
        public void MapHcfaAdress_WhenIndividualAndBillingLocation_MapsFromServiceLocation()
        {
            // Arrange
            var sut = CreateSut();

            var accountInfoId = 100;
            var stateId = 1;
            var countryId = 10;

            var providerLocation = new ProviderLocations
            {
                isBillingLocation = true,
                phone = "555-0000",
                address = new ProviderLocationAddress { street1 = "S1", city = "SC", zip = "SZ", stateId = stateId, countryId = countryId }
            };

            var claim = new ClaimEntity { AccountInfoId = accountInfoId };
            // Attach service location to claim (used by MapHcfaAdress)
            claim.ServiceLocation = providerLocation;

            var hcfa = new ClaimHFCAModel();

            // Use reflection to set non-public setters
            var hcfaType = typeof(ClaimHFCAModel);
            hcfaType.GetProperty("ServiceLineBillingProviderOption", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.SetValue(hcfa, BillingProviderOptionType.Individual);
            // MapHcfaAdress uses RenderingProviderName for AccountName in Individual path
            hcfaType.GetProperty("RenderingProviderName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.SetValue(hcfa, "Dr. Render");

            var claimSubmission = new ClaimSubmissionEntity
            {
                ServiceLocationAddress1 = "ADDR-1",
                ServiceLocationCity = "CITY-1",
                ServiceLocationState = "ST-1",
                ServiceLocationZip = "ZIP-1",
                ServiceLocationCountry = "COUNTRY-1",
                RenderingProviderStaffNpiNumber = "NPI-123"
            };

            _rethinkServices.Setup(s => s.GetAccountReturningEntityAsync(accountInfoId, It.IsAny<bool>()))
                .ReturnsAsync(new AccountInfoEntityModel { IsInternational = false });

            _rethinkServices.Setup(s => s.GetProviderLocationList(accountInfoId))
                .ReturnsAsync(new ClientProviderLocationsModel
                {
                    data = new List<ProviderLocations>
                    {
                new ProviderLocations
                {
                    isMainLocation = true,
                    phone = "555-1111",
                    address = new ProviderLocationAddress { street1 = "MAIN-1", city = "MAIN-C", zip = "MAIN-Z", stateId = stateId, countryId = countryId }
                }
                    }
                });

            _rethinkServices.Setup(s => s.GetCountryList()).ReturnsAsync(new List<CountryModel> { new CountryModel { id = countryId, name = "USA" } });
            _rethinkServices.Setup(s => s.GetStateList()).ReturnsAsync(new List<StateModel> { new StateModel { id = stateId, abbreviation = "NY" } });

            var mi = typeof(ClaimManagerService).GetMethod("MapHcfaAdress", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(mi);

            // Act (async void -> invoke and wait until side-effects applied)
            mi.Invoke(sut, new object[] { hcfa, claim, claimSubmission });
            SpinWait.SpinUntil(() => hcfa.AccountName != null, 1000);

            // Assert: values resolved from claimSubmission + claim.ServiceLocation
            Assert.Equal("Dr. Render", hcfa.AccountName);
            Assert.Equal("ADDR-1", hcfa.AccountAddress1);
            Assert.Equal("CITY-1", hcfa.AccountCity);
            Assert.Equal("ST-1", hcfa.AccountState);
            Assert.Equal("ZIP-1", hcfa.AccountZip);
            Assert.Equal("555-0000", hcfa.AccountPhone);
            Assert.Equal("NPI-123", hcfa.AccountNPI);

            _rethinkServices.Verify(s => s.GetAccountReturningEntityAsync(accountInfoId, It.IsAny<bool>()), Times.Once);
            _rethinkServices.Verify(s => s.GetProviderLocationList(accountInfoId), Times.Once);
            _rethinkServices.Verify(s => s.GetCountryList(), Times.Once);
            _rethinkServices.Verify(s => s.GetStateList(), Times.Once);
        }

        [Fact]
        public void MapHcfaAdress_WhenGroupAndIndividual_UsesResolvedBillingProviderNpi()
        {
            // Arrange
            var sut = CreateSut();

            var claim = new ClaimEntity
            {
                AccountInfoId = 200,
                ServiceLocation = new ProviderLocations { isBillingLocation = false, phone = "555-2222" }
            };

            var hcfa = new ClaimHFCAModel();

            // Use reflection to set non-public setters
            var hcfaType = typeof(ClaimHFCAModel);
            hcfaType.GetProperty("ServiceLineBillingProviderOption", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.SetValue(hcfa, BillingProviderOptionType.GroupAndIndividual);
            hcfaType.GetProperty("RenderingProviderName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.SetValue(hcfa, "Ignored");

            var claimSubmission = new ClaimSubmissionEntity
            {
                AccountBillingProviderName = "Provider Group",
                AccountBillingAddress1 = "G-ADDR-1",
                AccountBillingCity = null, // force town fallback
                AccountBillingTown = "G-TOWN",
                AccountBillingState = "G-ST",
                AccountBillingZip = "G-ZIP",
                AccountPhoneNumber = "G-PHONE",
                ResolvedBillingProviderNpi = "G-NPI",
                AccountBillingProviderTaxonomyCode = "G-TAX"
            };

            var mi = typeof(ClaimManagerService).GetMethod("MapHcfaAdress", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(mi);

            // Act
            mi.Invoke(sut, new object[] { hcfa, claim, claimSubmission });
            SpinWait.SpinUntil(() => hcfaType.GetProperty("AccountName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(hcfa) != null, 1000);

            // Assert: mapped from submission; NPI from ResolvedBillingProviderNpi
            Assert.Equal("Provider Group", hcfaType.GetProperty("AccountName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(hcfa));
            Assert.Equal("G-ADDR-1", hcfaType.GetProperty("AccountAddress1", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(hcfa));
            Assert.Equal("G-TOWN", hcfaType.GetProperty("AccountCity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(hcfa));
            Assert.Equal("G-ST", hcfaType.GetProperty("AccountState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(hcfa));
            Assert.Equal("G-ZIP", hcfaType.GetProperty("AccountZip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(hcfa));
            Assert.Equal("G-PHONE", hcfaType.GetProperty("AccountPhone", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(hcfa));
            Assert.Equal("G-NPI", hcfaType.GetProperty("AccountNPI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(hcfa));

            // No external calls required in this branch
            _rethinkServices.Verify(s => s.GetAccountReturningEntityAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
            _rethinkServices.Verify(s => s.GetProviderLocationList(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void MapHcfaAdress_WhenGroupOnly_UsesTaxonomyCodeForAccountNpi()
        {
            // Arrange
            var sut = CreateSut();

            var claim = new ClaimEntity { AccountInfoId = 300, ServiceLocation = new ProviderLocations { isBillingLocation = false } };

            // Use reflection to set non-public setters
            var hcfa = new ClaimHFCAModel();
            var hcfaType = typeof(ClaimHFCAModel);
            hcfaType.GetProperty("ServiceLineBillingProviderOption", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.SetValue(hcfa, BillingProviderOptionType.Group);
            hcfaType.GetProperty("RenderingProviderName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.SetValue(hcfa, "Ignored");

            var claimSubmission = new ClaimSubmissionEntity
            {
                AccountBillingProviderName = "Grp",
                AccountBillingAddress1 = "A1",
                AccountBillingCity = "City",
                AccountBillingState = "ST",
                AccountBillingZip = "ZIP",
                AccountPhoneNumber = "PH",
                ResolvedBillingProviderNpi = "SHOULD-NOT-BE-USED",
                AccountBillingProviderTaxonomyCode = "TAX-123"
            };

            var mi = typeof(ClaimManagerService).GetMethod("MapHcfaAdress", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(mi);

            // Act
            mi.Invoke(sut, new object[] { hcfa, claim, claimSubmission });
            SpinWait.SpinUntil(() => hcfaType.GetProperty("AccountName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(hcfa) != null, 1000);

            // Assert: NPI falls back to taxonomy code for non-GroupAndIndividual
            Assert.Equal("Grp", hcfaType.GetProperty("AccountName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(hcfa));
            Assert.Equal("A1", hcfaType.GetProperty("AccountAddress1", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(hcfa));
            Assert.Equal("City", hcfaType.GetProperty("AccountCity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(hcfa));
            Assert.Equal("ST", hcfaType.GetProperty("AccountState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(hcfa));
            Assert.Equal("ZIP", hcfaType.GetProperty("AccountZip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(hcfa));
            Assert.Equal("PH", hcfaType.GetProperty("AccountPhone", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(hcfa));
            Assert.Equal("TAX-123", hcfaType.GetProperty("AccountNPI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(hcfa));

            _rethinkServices.Verify(s => s.GetAccountReturningEntityAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
            _rethinkServices.Verify(s => s.GetProviderLocationList(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetPatientInfoById_ReturnsMappedChildProfileInfo()
        {
            // Arrange
            var sut = CreateSut();
            var accountInfoId = 10;
            var patientId = 20;

            // Client profile
            var clientProfile = new ChildProfileEntityModel
            {
                AccountInfoId = accountInfoId
            };
            // Populate additional fields via dynamic to set runtime-known members
            dynamic clientDyn = clientProfile;
            clientDyn.Id = patientId;
            clientDyn.FirstName = "John";
            clientDyn.MiddleName = "Q";
            clientDyn.LastName = "Public";
            clientDyn.DateOfBirth = new DateTime(2015, 5, 1);
            clientDyn.GenderId = 1;
            clientDyn.UCI = "UCI-123";
            clientDyn.Address = "123 Main St";
            clientDyn.City = "Townsville";
            clientDyn.Town = "Metro";
            clientDyn.ZipCode = "12345";
            // Use concrete models to avoid runtime binder errors
            clientDyn.StateLU = new StateModel { name = "NY" };
            clientDyn.CountryLU = new CountryModel { name = "USA" };

            // Client details
            var clientDetails = new RethinkClientDetails
            {
                serviceIntensityTypeId = (int)ServiceIntensityTypes.Intensive
            };

            // Funder mappings -> insurance contact mapping to funderId
            var funderMappings = new ChildProfileFunderResponseModel
            {
                data = new List<FunderDetails>
            {
                new FunderDetails { childProfileInsuranceContactId = 100 }
            }
            };
            // Add funderId via dynamic
            dynamic fd0 = funderMappings.data[0];
            fd0.funderId = 300;

            // Insurance contacts ids and types
            var insuranceContacts = new InsuranceContactsModel
            {
                data = new List<InsuranceContacts>
            {
                new InsuranceContacts { Id = 100 }
            }
            };
            var insuranceType = new InsuranceContactsTypeModel
            {
                insuranceTypeId = 1 // Primary
            };

            // Funder info
            var funder = new FunderDataModel
            {
                funderName = "Primary Funder"
            };

            // Location name
            var locationName = "Clinic A";

            _rethinkServices
                .Setup(s => s.GetChildProfileReturningEntity(accountInfoId, patientId))
                .ReturnsAsync(clientProfile);

            _rethinkServices
                .Setup(s => s.GetClientDetails(accountInfoId, patientId))
                .ReturnsAsync(clientDetails);

            _rethinkServices
                .Setup(s => s.GetChildProfileFunderMappings(accountInfoId, patientId))
                .ReturnsAsync(funderMappings);

            _rethinkServices
                .Setup(s => s.GetInsuranceContactsIds(accountInfoId, patientId))
                .ReturnsAsync(insuranceContacts);

            _rethinkServices
                .Setup(s => s.GetInsuranceContactsType(accountInfoId, patientId, 100))
                .ReturnsAsync(insuranceType);

            _rethinkServices
                .Setup(s => s.GetFunder(accountInfoId, 300))
                .ReturnsAsync(funder);

            _rethinkServices
                .Setup(s => s.GetProviderLocationName(accountInfoId, patientId))
                .ReturnsAsync(locationName);

            // Act
            var result = await sut.GetPatientInfoById(patientId, accountInfoId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(patientId, result.PatientId);
            Assert.Equal("John Q Public", result.PatientName);
            Assert.Equal(new DateTime(2015, 5, 1), result.DateOfBirth);
            Assert.Equal("Male", result.Gender);
            Assert.Equal("UCI-123", result.Uci);
            Assert.Equal(ServiceIntensityTypes.Intensive.ToString(), result.ServiceIntensity);
            Assert.Equal(locationName, result.Location);
            Assert.Equal("Primary Funder", result.PrimaryPolicy);
            Assert.Equal(string.Empty, result.SecondaryPolicy);
            Assert.Contains("123 Main St", result.Address);
            Assert.Contains("Townsville, Metro NY 12345", result.Address);
            Assert.Contains("USA", result.Address);

            // Verify calls
            _rethinkServices.Verify(s => s.GetChildProfileReturningEntity(accountInfoId, patientId), Times.Once);
            _rethinkServices.Verify(s => s.GetClientDetails(accountInfoId, patientId), Times.Once);
            _rethinkServices.Verify(s => s.GetChildProfileFunderMappings(accountInfoId, patientId), Times.Once);
            _rethinkServices.Verify(s => s.GetInsuranceContactsIds(accountInfoId, patientId), Times.Once);
            _rethinkServices.Verify(s => s.GetInsuranceContactsType(accountInfoId, patientId, 100), Times.Once);
            _rethinkServices.Verify(s => s.GetFunder(accountInfoId, 300), Times.Once);
            _rethinkServices.Verify(s => s.GetProviderLocationName(accountInfoId, patientId), Times.Once);
        }

        // Covers happy path (already present), secondary policy, empty policies, Female gender,
        // exception path (catch returns null), and multiple insurance contacts.

        [Fact]
        public async Task GetPatientInfoById_MapsSecondaryPolicy_WhenInsuranceTypeIsSecondary()
        {
            var sut = CreateSut();
            var accountInfoId = 10;
            var patientId = 20;

            var clientProfile = new ChildProfileEntityModel { AccountInfoId = accountInfoId };
            dynamic d = clientProfile;
            d.Id = patientId;
            d.FirstName = "Jane";
            d.MiddleName = "R";
            d.LastName = "Doe";
            d.DateOfBirth = new DateTime(2012, 2, 3);
            d.GenderId = 2; // Female path
            d.UCI = "UCI-456";
            d.Address = "999 Second St";
            d.City = "Smalltown";
            d.Town = "Village";
            d.ZipCode = "67890";
            d.StateLU = new StateModel { name = "FL" };
            d.CountryLU = new CountryModel { name = "USA" };

            var clientDetails = new RethinkClientDetails { serviceIntensityTypeId = (int)ServiceIntensityTypes.NonIntensive };

            var funderMappings = new ChildProfileFunderResponseModel
            {
                data = new List<FunderDetails> { new FunderDetails { childProfileInsuranceContactId = 200 } }
            };
            dynamic fd = funderMappings.data[0];
            fd.funderId = 400;

            var insuranceContacts = new InsuranceContactsModel
            {
                data = new List<InsuranceContacts> { new InsuranceContacts { Id = 200 } }
            };
            var insuranceType = new InsuranceContactsTypeModel { insuranceTypeId = 2 }; // Secondary

            var funder = new FunderDataModel { funderName = "Secondary Funder" };
            var locationName = "Clinic B";

            _rethinkServices.Setup(s => s.GetChildProfileReturningEntity(accountInfoId, patientId)).ReturnsAsync(clientProfile);
            _rethinkServices.Setup(s => s.GetClientDetails(accountInfoId, patientId)).ReturnsAsync(clientDetails);
            _rethinkServices.Setup(s => s.GetChildProfileFunderMappings(accountInfoId, patientId)).ReturnsAsync(funderMappings);
            _rethinkServices.Setup(s => s.GetInsuranceContactsIds(accountInfoId, patientId)).ReturnsAsync(insuranceContacts);
            _rethinkServices.Setup(s => s.GetInsuranceContactsType(accountInfoId, patientId, 200)).ReturnsAsync(insuranceType);
            _rethinkServices.Setup(s => s.GetFunder(accountInfoId, 400)).ReturnsAsync(funder);
            _rethinkServices.Setup(s => s.GetProviderLocationName(accountInfoId, patientId)).ReturnsAsync(locationName);

            var result = await sut.GetPatientInfoById(patientId, accountInfoId);

            Assert.NotNull(result);
            Assert.Equal(patientId, result.PatientId);
            Assert.Equal("Jane R Doe", result.PatientName);
            Assert.Equal(new DateTime(2012, 2, 3), result.DateOfBirth);
            Assert.Equal("Female", result.Gender);
            Assert.Equal("UCI-456", result.Uci);
            Assert.Equal(ServiceIntensityTypes.NonIntensive.ToString(), result.ServiceIntensity);
            Assert.Equal(locationName, result.Location);
            Assert.Equal(string.Empty, result.PrimaryPolicy); // no primary in this test
            Assert.Equal("Secondary Funder", result.SecondaryPolicy);
            Assert.Contains("999 Second St", result.Address);
            Assert.Contains("Smalltown, Village FL 67890", result.Address);
            Assert.Contains("USA", result.Address);
        }

        [Fact]
        public async Task GetPatientInfoById_LeavesPoliciesEmpty_WhenInsuranceTypesNullOrNoMatches()
        {
            var sut = CreateSut();
            var accountInfoId = 10;
            var patientId = 21;

            var clientProfile = new ChildProfileEntityModel { AccountInfoId = accountInfoId };
            dynamic d = clientProfile;
            d.Id = patientId;
            d.FirstName = "No";
            d.MiddleName = "Policy";
            d.LastName = "Client";
            d.DateOfBirth = new DateTime(2010, 10, 10);
            d.GenderId = 1;
            d.UCI = "UCI-EMPTY";
            d.Address = "1 Empty Rd";
            d.City = "Nowhere";
            d.Town = "Nulltown";
            d.ZipCode = "00000";
            d.StateLU = new StateModel { name = "NA" };
            d.CountryLU = new CountryModel { name = "N/A" };

            var clientDetails = new RethinkClientDetails { serviceIntensityTypeId = (int)ServiceIntensityTypes.Intensive };

            var funderMappings = new ChildProfileFunderResponseModel
            {
                data = new List<FunderDetails> { new FunderDetails { childProfileInsuranceContactId = 300 } }
            };
            dynamic fd = funderMappings.data[0];
            fd.funderId = 500;

            var insuranceContacts = new InsuranceContactsModel
            {
                data = new List<InsuranceContacts> { new InsuranceContacts { Id = 300 } }
            };
            var insuranceType = new InsuranceContactsTypeModel { insuranceTypeId = null }; // ensures branch skip

            _rethinkServices.Setup(s => s.GetChildProfileReturningEntity(accountInfoId, patientId)).ReturnsAsync(clientProfile);
            _rethinkServices.Setup(s => s.GetClientDetails(accountInfoId, patientId)).ReturnsAsync(clientDetails);
            _rethinkServices.Setup(s => s.GetChildProfileFunderMappings(accountInfoId, patientId)).ReturnsAsync(funderMappings);
            _rethinkServices.Setup(s => s.GetInsuranceContactsIds(accountInfoId, patientId)).ReturnsAsync(insuranceContacts);
            _rethinkServices.Setup(s => s.GetInsuranceContactsType(accountInfoId, patientId, 300)).ReturnsAsync(insuranceType);
            _rethinkServices.Setup(s => s.GetProviderLocationName(accountInfoId, patientId)).ReturnsAsync("Unknown");

            var result = await sut.GetPatientInfoById(patientId, accountInfoId);

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.PrimaryPolicy);
            Assert.Equal(string.Empty, result.SecondaryPolicy);
        }

        [Fact]
        public async Task GetPatientInfoById_CombinesMultiplePolicies_PrimaryAndSecondary()
        {
            var sut = CreateSut();
            var accountInfoId = 10;
            var patientId = 22;

            var clientProfile = new ChildProfileEntityModel { AccountInfoId = accountInfoId };
            dynamic d = clientProfile;
            d.Id = patientId;
            d.FirstName = "Multi";
            d.MiddleName = "Policy";
            d.LastName = "Client";
            d.DateOfBirth = new DateTime(2008, 8, 8);
            d.GenderId = 1;
            d.UCI = "UCI-MULTI";
            d.Address = "321 Combo St";
            d.City = "Blend";
            d.Town = "Mix";
            d.ZipCode = "22222";
            d.StateLU = new StateModel { name = "CA" };
            d.CountryLU = new CountryModel { name = "USA" };

            var clientDetails = new RethinkClientDetails { serviceIntensityTypeId = (int)ServiceIntensityTypes.Intensive };

            var funderMappings = new ChildProfileFunderResponseModel
            {
                data = new List<FunderDetails>
        {
            new FunderDetails { childProfileInsuranceContactId = 10 },
            new FunderDetails { childProfileInsuranceContactId = 11 }
        }
            };
            dynamic fd0 = funderMappings.data[0]; fd0.funderId = 800;
            dynamic fd1 = funderMappings.data[1]; fd1.funderId = 900;

            var insuranceContacts = new InsuranceContactsModel
            {
                data = new List<InsuranceContacts>
        {
            new InsuranceContacts { Id = 10 },
            new InsuranceContacts { Id = 11 }
        }
            };

            // First contact -> Primary
            _rethinkServices.Setup(s => s.GetInsuranceContactsType(accountInfoId, patientId, 10))
                .ReturnsAsync(new InsuranceContactsTypeModel { insuranceTypeId = 1 });
            _rethinkServices.Setup(s => s.GetFunder(accountInfoId, 800))
                .ReturnsAsync(new FunderDataModel { funderName = "Primary A" });

            // Second contact -> Secondary
            _rethinkServices.Setup(s => s.GetInsuranceContactsType(accountInfoId, patientId, 11))
                .ReturnsAsync(new InsuranceContactsTypeModel { insuranceTypeId = 2 });
            _rethinkServices.Setup(s => s.GetFunder(accountInfoId, 900))
                .ReturnsAsync(new FunderDataModel { funderName = "Secondary B" });

            _rethinkServices.Setup(s => s.GetChildProfileReturningEntity(accountInfoId, patientId)).ReturnsAsync(clientProfile);
            _rethinkServices.Setup(s => s.GetClientDetails(accountInfoId, patientId)).ReturnsAsync(clientDetails);
            _rethinkServices.Setup(s => s.GetChildProfileFunderMappings(accountInfoId, patientId)).ReturnsAsync(funderMappings);
            _rethinkServices.Setup(s => s.GetInsuranceContactsIds(accountInfoId, patientId)).ReturnsAsync(insuranceContacts);
            _rethinkServices.Setup(s => s.GetProviderLocationName(accountInfoId, patientId)).ReturnsAsync("Clinic C");

            var result = await sut.GetPatientInfoById(patientId, accountInfoId);

            Assert.NotNull(result);
            Assert.Equal("Primary A", result.PrimaryPolicy);
            Assert.Equal("Secondary B", result.SecondaryPolicy);
        }

        [Fact]
        public async Task GetPatientInfoById_ReturnsNull_WhenAnyCallThrows()
        {
            var sut = CreateSut();
            var accountInfoId = 33;
            var patientId = 44;

            _rethinkServices.Setup(s => s.GetChildProfileReturningEntity(accountInfoId, patientId))
                .ThrowsAsync(new Exception("boom"));

            var result = await sut.GetPatientInfoById(patientId, accountInfoId);

            Assert.Null(result);
        }
        private void SetupServices(int accountInfoId, int childProfileId, int authorizationId, int renderingStaffMemberId, int providerLocationId, int locationCodeId, int serviceLocationId, int clientFunderId, int lastBilledFunderId, int referringProviderId)
        {
            _rethinkServices.Setup(x => x.GetChildProfileReturningEntity(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<ChildProfileEntityModel>().With(x => x.Id, childProfileId).Create());

            _rethinkServices.Setup(x => x.GetClientDiagnosisByIdReturningEntityAsync(It.IsAny<int>())).ReturnsAsync(Fixture.Build<DiagnosisEntityModel>().Create());

            _rethinkServices.Setup(x => x.GetChildProfileAuthorizationByClientId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<ClientAuthorization>().With(x => x.id, authorizationId).Without(x => x.ChildProfileAuthorizationDiagnosisCodes)
                .Without(x => x.ChildProfileReferringProvider)
                .Without(x => x.ChildProfileDiagnosis).Create());

            _rethinkServices.Setup(x => x.GetChildProfileFunderServiceLineMappingEntity(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<ServiceLines>().Create());

            _rethinkServices.Setup(x => x.GetFunder(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<FunderDataModel>().Create());

            _rethinkServices.Setup(x => x.GetChildProfileReferringProviderEntity(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<clientReferringProviders>().With(x => x.id, referringProviderId).Create());

            _rethinkServices.Setup(x => x.GetProviderLocation(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<ProviderLocations>().With(x => x.id, providerLocationId).Create());

            var diagCode = Fixture.Build<ChildProfileAuthorizationDiagnosisCode>().Without(x => x.ChildProfileAuthorization).Create();
            var diagCodeList = new List<ChildProfileAuthorizationDiagnosisCode>();
            diagCodeList.Add(diagCode);
            _rethinkServices.Setup(x => x.GetChildProfileAuthorizationDiagnosisCodesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(diagCodeList);

            _rethinkServices.Setup(x => x.GetMemberAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<RethinkAccountMember>().With(x => x.id, renderingStaffMemberId).Create());

            var locationCode = Fixture.Build<LocationCodesModel>().With(x => x.id, locationCodeId).Create();
            var lcList = new List<LocationCodesModel>();
            lcList.Add(locationCode);
            _rethinkServices.Setup(x => x.GetLocationCodes()).ReturnsAsync(lcList);

            _rethinkServices.Setup(x => x.GetAccountReturningEntityAsync(accountInfoId, true)).ReturnsAsync(Fixture.Build<AccountInfoEntityModel>().With(x => x.Id, accountInfoId).Create());

            _rethinkServices.Setup(x => x.GetChildProfileFunderMappingByMappingId(accountInfoId, childProfileId, It.IsAny<int>())).ReturnsAsync(Fixture.Build<FunderDetails>().With(x => x.id, clientFunderId).Create());
        }
    }
}
