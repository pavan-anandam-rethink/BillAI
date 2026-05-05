using AutoFixture;
using BillingService.Domain.DataObjects.Base;
using BillingService.Domain.DataObjects.Billing;
using BillingService.Domain.DataObjects.CompanyAccount;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Client;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.Funders;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.XUnit.Tests.Common.Mocks;
using BillingService.XUnit.Tests.Common.Models;
using Microsoft.Data.SqlClient;
using Moq;
using Newtonsoft.Json;
using Rethink.Services.Common.Entities.Billing;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Common.Models.Clients;
using Rethink.Services.Common.Models.RethinkDataEntityClasses;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using ClientFunderModel = BillingService.Domain.Models.Funders.ClientFunderModel;

namespace BillingService.XUnit.Integration.Tests
{
    [Trait("ClaimControllerTest", "Integration")]
    [Collection("Billing")]
    public class ClaimControllerTest : BaseControllerTest
    {
        private const string BaseUrl = "claim";

        private readonly Mock<IRepository<BillingDbContext, ClaimEntity>> _claimRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimValidationErrorEntity>> _claimValidationErrorRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimErrorCategoryEntity>> _claimErrorCategoryRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimErrorMessageEntity>> _claimErrorMessageRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimHistoryEntity>> _claimHistoryRepository;
        private readonly Mock<IRepository<BillingDbContext, PaymentClaimEntity>> _paymentClaimRepository;
        private readonly Mock<IRepository<BillingDbContext, PaymentEntity>> _paymentRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimSubmissionEntity>> _claimSubmissionRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimDiagnosisCodeEntity>> _claimDiagnosisCodeRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>> _claimAppointmentLinkRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>> _claimChargeEntryRepository;
        private readonly Mock<IRepository<BillingDbContext, MemberViewSettingEntity>> _memberViewSettingRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimHistoryActionEntity>> _claimHistoryActionRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimVersionEntity>> _claimVersionRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimAppointmentLinkChargeEntry>> _linkChargeEntryRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity>> _claimChargeEntryWriteOffRepository;

        private readonly Mock<IClaimService> _claimService;
        private readonly Mock<IClaimManagerService> _claimManagerService;
        private readonly Mock<IClientService> _clientService;
        private readonly Mock<IProviderLocationService> _providerLocationService;
        private readonly Mock<IMemberAccountService> _memberAccountService;
        private readonly Mock<ICommonService> _commonService;
        private readonly Mock<IRethinkMasterDataMicroServices> _rethinkService;

        private readonly Mock<IDbHelper<BillingDbContext>> _billingDbHelper;

        public ClaimControllerTest(TestServerFixture fixture)
            : base(fixture)
        {
            _claimRepository = fixture.ClaimRepository;
            _claimValidationErrorRepository = fixture.ClaimValidationErrorRepository;
            _claimErrorCategoryRepository = fixture.ClaimErrorCategoryRepository;
            _claimErrorMessageRepository = fixture.ClaimErrorMessageRepository;
            _claimHistoryRepository = fixture.ClaimHistoryRepository;
            _memberViewSettingRepository = fixture.MemberViewSettingsRepository;
            _claimChargeEntryRepository = fixture.ClaimChargeEntryRepository;
            _billingDbHelper = fixture.BillingDbHelper;
            _paymentClaimRepository = fixture.PaymentClaimRepository;
            _paymentRepository = fixture.PaymentRepository;
            _claimSubmissionRepository = fixture.ClaimSubmissionRepository;
            _claimDiagnosisCodeRepository = fixture.ClaimDiagnosisCodeRepository;
            _claimAppointmentLinkRepository = fixture.ClaimAppointmentLinkRepository;
            _claimHistoryActionRepository = fixture.ClaimHistoryActionRepository;
            _claimVersionRepository = fixture.ClaimVersionRepository;
            _linkChargeEntryRepository = fixture.LinkChargeEntryRepository;
            _claimChargeEntryWriteOffRepository = fixture.ClaimChargeEntryWriteOffRepository;
            _claimManagerService = fixture.ClaimManagerService;
            _clientService = fixture.ClientService;
            _providerLocationService = fixture.ProviderLocationService;
            _memberAccountService = fixture.MemberAccountService;
            _rethinkService = fixture.RethinkServices;
            _commonService = fixture.CommonService;
            _claimService = fixture.ClaimService;
        }

         [Trait("Category", "Integration")]
        public async Task Get_ShouldReturnSuccessResult()
        {
            var url = $"{BaseUrl}/get";
            var data = Fixture.Create<ClaimIdWithUserInfo>();

            SetupMocks(data);

            var response = await PostAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ActionSuccessResult<ClaimModel>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(data.ClaimIdentifier, result.Data.ClaimIdentifier);
        }

         [Trait("Category", "Integration")]
        public async Task Get_ShouldReturnFailResult_WhenClaimNotFound()
        {
            var url = $"{BaseUrl}/get";
            var data = Fixture.Create<ClaimIdWithUserInfo>();

            SetupMocks(data, Fixture.Create<string>());

            var response = await PostAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ActionErrorResult>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("Claim not found", result.Error);
        }

         [Trait("Category", "Integration")]
        public async Task GetClaimHeaders_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/GetClaimHeaders";
            var data = Fixture.Create<ClaimGetRequestSortFilterWithUserInfo>();
            var claimNo = Fixture.Create<string>();

            var claimHeader = Fixture.Build<ClaimHeaderModel>().With(x => x.TotalCount, 1).With(x => x.ClaimNumber, claimNo).Create();
            var claimsCount = Fixture.Create<ClaimsCountModel>();

            _billingDbHelper.Setup(x => x.ExecuteListAsync<ClaimHeaderModel>("GetClaimsByAccountInfoId",
                It.IsAny<List<SqlParameter>>(),
                CommandType.StoredProcedure))
                .ReturnsAsync(new List<ClaimHeaderModel> { claimHeader });

            _billingDbHelper.Setup(x => x.ExecuteListAsync<ClaimsCountModel>("GetClaimsCount",
                It.IsAny<List<SqlParameter>>(),
                CommandType.StoredProcedure))
                .ReturnsAsync(new List<ClaimsCountModel> { claimsCount });

            var claimHistory = Fixture.Build<ClaimHistoryEntity>().Create();
            var claimHistories = new List<ClaimHistoryEntity>() { claimHistory };
            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.AccountInfoId, data.AccountInfoId)
                .With(x => x.ClaimHistory, claimHistories)
                .With(x => x.ClaimIdentifier, claimNo)
                .Create();
            _claimRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimEntity>.Create(claim));

            var response = await PostAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ClaimHeaderModelResponseModel>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(claimHeader.TotalCount, result.TotalCount);
        }

        // [Trait("Category", "Integration")]
        public async Task GetClaimErrorsAndAlerts_ShouldReturnEmptyResult()
        {
            var url = $"{BaseUrl}/GetClaimErrorsAndAlerts";
            var data = Fixture.Create<ClaimIdWithUserInfo>();

            SetupMocks(data);

            _claimValidationErrorRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimValidationErrorEntity>.Create(Fixture.Create<ClaimValidationErrorEntity>()));

            var response = await PostAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ClaimErrorAlertViewModel>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        // [Trait("Category", "Integration")]
        public async Task GetClaimErrorsAndAlerts_ShouldReturnResult_WhenEraErrorIsNotExists()
        {
            var url = $"{BaseUrl}/GetClaimErrorsAndAlerts";

            var data = Fixture.Create<ClaimIdWithUserInfo>();
            var claimValidationError = Fixture.Build<ClaimValidationErrorEntity>()
                .With(x => x.ClaimId, data.Id)
                .With(x => x.ClaimErrorMessage, Fixture.Build<ClaimErrorMessageEntity>()
                    .With(x => x.ClaimErrorCategory, Fixture.Create<ClaimErrorCategoryEntity>())
                    .Create())
                .Without(x => x.EraValidationErrorId)
                .Create();

            SetupMocks(data);

            _claimValidationErrorRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimValidationErrorEntity>.Create(claimValidationError));

            var response = await PostAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ClaimErrorAlertViewModel>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Single(result);
        }

        // [Trait("Category", "Integration")]
        public async Task GetOptions_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/GetOptions";

            var data = Fixture.Create<ClaimIdWithUserInfo>();
            SetupMocks(data);

            var clientOptionModelList = new List<ClientOptionModel>() { Fixture.Build<ClientOptionModel>().Create() };
            _clientService.Setup(x => x.GetClientsListForClaimAsync(It.IsAny<int>(), It.IsAny<int>()))
                        .ReturnsAsync(clientOptionModelList);

            var providerLocationsList = new List<ProviderLocations>() { Fixture.Build<ProviderLocations>().With(x => x.isBillingLocation, true).Create() };
            _providerLocationService.Setup(x => x.GetForAccount(It.IsAny<int>()))
                .ReturnsAsync(providerLocationsList);

            var memberItemList = new List<MemberItem>() { Fixture.Build<MemberItem>().Create() };
            _memberAccountService.Setup(x => x.GetMembersByAccountInfoId(It.IsAny<int>()))
                .ReturnsAsync(memberItemList);

            var locationCodeDataList = new List<LocationCodeData>() { Fixture.Build<LocationCodeData>().Create() };
            _commonService.Setup(x => x.GetLocationCodes(It.IsAny<int>()))
                .ReturnsAsync(locationCodeDataList);

            var renderingProviderList = new List<BasicOption>() { Fixture.Create<BasicOption>() };
            _claimService.Setup(x => x.GetClaimRenderingProviders(It.IsAny<int>()))
                .ReturnsAsync(renderingProviderList);

            var referringproviderList = new List<BasicOption>() { Fixture.Build<BasicOption>().Create() };
            _claimService.Setup(x => x.GetClaimReferringProviders(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(referringproviderList);

            var ints = new List<int>() { data.Id };
            _claimService.Setup(x => x.GetIdsForAccountAsync(It.IsAny<int>()))
                .ReturnsAsync(ints);

            var claim = Fixture.Build<ClaimEntity>().With(x => x.Id, data.Id).With(x => x.AccountInfoId, data.AccountInfoId).Create();
            _claimRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimEntity>.Create(claim));

            var clientUnitTypesList = new List<ClientOptionModel>() { Fixture.Build<ClientOptionModel>().Create() };
            _rethinkService.Setup(x => x.GetUnitTypesAsync())
                .ReturnsAsync(Fixture.Build<List<ClientUnitTypes>>().Create());

            _rethinkService.Setup(x => x.GetProviderLocationList(It.IsAny<int>())).ReturnsAsync(Fixture.Build<ClientProviderLocationsModel>().Create());

            var model = Fixture.Build<RethinkAccountMembersListModel>().With(x => x.total, Fixture.Create<int>()).Create();
            _rethinkService.Setup(x => x.GetMemberListAsync(It.IsAny<int>())).ReturnsAsync(model);

            var identifier = Fixture.Build<Identifiers>().With(x => x.identifierType, "npinumber").Create();
            var identifiers = new List<Identifiers>() { identifier };
            var staff = Fixture.Build<RethinkStaffMember>().With(x => x.identifiers, identifiers).Create();
            var staffList = new List<RethinkStaffMember>() { staff };
            _rethinkService.Setup(x => x.GetStaffMemberList(It.IsAny<int>())).ReturnsAsync(staffList);

            var referringProvider = Fixture.Build<ReferringProviderDropdownModel>().Create();
            _rethinkService.Setup(x => x.GetReferringProvidersByClientId(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<ReferringProviderDropdownModel>() { referringProvider });


            var response = await PostAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<ClaimOptions>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Single(result.ClaimIds);
            Assert.Collection(result.ClaimIds, claimId => Assert.Equal(data.Id, claimId));
        }

         [Trait("Category", "Integration")]
        public async Task GetClaimDetails_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/GetClaimDetails";
            var data = Fixture.Create<ClaimIdWithUserInfo>();

            var member = Fixture.Create<RethinkAccountMember>();
            var diagnosisCodeEntities = Fixture.Build<ClaimDiagnosisCodeEntity>()
                .With(x => x.ClaimId, data.Id)
                .With(x => x.IncludeOnClaims, true)
                .Create();

            var childProfile = Fixture.Create<ChildProfileEntityModel>();
            var entries = Fixture.Create<ClaimChargeEntryEntity>();
            var locationCode = Fixture.Build<LocationCodesModel>().Create();

            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, data.Id)
                .With(x => x.RenderingStaffMemberId, member.id)
                .With(x => x.ChildProfileId, childProfile.Id)
                .With(x => x.ClaimAppointmentLinks, new List<ClaimAppointmentLinkEntity>())
                .With(x => x.ClaimDiagnosisCodes, new List<ClaimDiagnosisCodeEntity> { diagnosisCodeEntities })
                .With(x => x.ClaimChargeEntries, new List<ClaimChargeEntryEntity> { entries })
                .With(x => x.ClaimSubmissions, new List<ClaimSubmissionEntity>())
                .With(x => x.ClaimStatus, ClaimStatus.Closed)
                .With(x => x.ChildProfile, childProfile)
                .With(x => x.LocationCode, locationCode)
                .With(x => x.ClientFunder, Fixture.Create<FunderDetails>())
                .With(x => x.ChildProfileAuthorization, Fixture.Build<ClientAuthorization>().Without(x => x.ChildProfileAuthorizationDiagnosisCodes).Create())
                .Without(x => x.AuthorizationId)
                .Without(x => x.ChildProfileReferringProviderId)
                .Create();


            var paymentClaimEntity = Fixture.Create<PaymentClaimEntity>();
            var claimSubmission = Fixture.Create<ClaimSubmissionEntity>();

            _claimRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimEntity>.Create(claim));

            _claimManagerService.Setup(x => x.GetFullClaim(It.IsAny<int>()))
                .ReturnsAsync(claim);
            _paymentClaimRepository.Setup(x => x.Query())
                .Returns(QueryMock<PaymentClaimEntity>.Create(paymentClaimEntity));
            _claimSubmissionRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(claimSubmission));

            SetupRethinkServices();

            var response = await PostAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<ClaimDetailsModel>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(claim.Id, result.Id);
            Assert.Equal(claim.ClaimIdentifier, result.ClaimIdentifier);
        }

        // [Trait("Category", "Integration")]
        public async Task GetBillingClaimDetails_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/GetBillingClaimDetails";
            var claimId = Fixture.Create<int>();
            var bcdId = Fixture.Create<int>();
            var data = Fixture.Build<GetBillingClaimDetailsModel>()
                .With(x => x.ClaimId, claimId)
                .With(x => x.ChargeEntryId, claimId)
                .With(x => x.Take, 0)
                .Without(x => x.SortingModels)
                .Create();

            var diagnosisCodeEntity = Fixture.Create<ClaimDiagnosisCodeEntity>();
            var unitType = Fixture.Create<ClientUnitTypes>();
            var claimChargeEntryEntity = Fixture.Build<ClaimChargeEntryEntity>()
                .With(x => x.Id, claimId)
                .With(x => x.ClaimId, claimId)
                .With(x => x.UnitTypeId, bcdId)
                .With(x => x.UnitType, unitType)
                .Without(x => x.NoteCreatedBy)
                .Create();
            var paymentClaimEntity = Fixture.Create<PaymentClaimEntity>();

            var claimEntity = Fixture.Build<ClaimEntity>().With(x => x.Id, data.ClaimId).Create();

            var linkCharge = Fixture.Build<ClaimAppointmentLinkChargeEntry>().With(x => x.Id, claimChargeEntryEntity.Id).Create();

            _claimRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimEntity>.Create(claimEntity));

            _claimChargeEntryRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimChargeEntryEntity>.Create(claimChargeEntryEntity));
            _claimDiagnosisCodeRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimDiagnosisCodeEntity>.Create(diagnosisCodeEntity));
            _paymentClaimRepository.Setup(x => x.Query())
                .Returns(QueryMock<PaymentClaimEntity>.Create(paymentClaimEntity));

            _linkChargeEntryRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimAppointmentLinkChargeEntry>.Create(linkCharge));

            var unitTypes = Fixture.Build<ClientUnitTypes>().With(x => x.id, bcdId).CreateMany();
            _rethinkService.Setup(x => x.GetUnitTypesAsync()).ReturnsAsync(unitTypes.ToList());
            _rethinkService.Setup(x => x.GetMemberAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<RethinkAccountMember>().Create());

            var response = await PostAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<List<ClaimDetailsModel>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Collection(result, item => Assert.Equal(claimId, item.Id));
        }

         [Trait("Category", "Integration")]
        public async Task GetErrorsSources_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/GetErrorsSources";
            var data = Fixture.Create<UserInfo>();
            var errorCategory = Fixture.Create<ClaimErrorCategoryEntity>();

            _claimErrorCategoryRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimErrorCategoryEntity>.Create(errorCategory));

            var response = await PostAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ClaimErrorsSourcesModel>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Single(result.ErrorsSources);
            Assert.Collection(result.ErrorsSources, item => Assert.Equal(errorCategory.Name, item));
        }

         [Trait("Category", "Integration")]
        public async Task GetErrorsCodes_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/GetErrorsCodes";
            var data = Fixture.Create<UserInfo>();
            var errorMessage = Fixture.Create<ClaimErrorMessageEntity>();

            _claimErrorMessageRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimErrorMessageEntity>.Create(errorMessage));

            var response = await PostAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ClaimErrorsCodesModel>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Single(result.ErrorsCodes);
            Assert.Collection(result.ErrorsCodes, item => Assert.Equal(errorMessage.ShortDescription, item.Name));
        }

         [Trait("Category", "Integration")]
        public async Task GetClaimHistory_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/GetClaimHistory";
            var data = Fixture.Create<ClaimIdWithUserInfo>();

            var historyRecord = Fixture.Build<ClaimHistoryEntity>()
                .With(x => x.ClaimId, data.Id)
                .Create();

            _claimHistoryRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimHistoryEntity>.Create(historyRecord));

            var response = await PostAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ClaimHistoryModel>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Single(result);
        }

         [Trait("Category", "Integration")]
        public async Task GetClaimHistoryActions_ShouldReturnRresult()
        {
            var url = $"{BaseUrl}/GetClaimHistoryActions";
            var data = Fixture.Create<UserInfo>();

            var historyActions = Fixture.CreateMany<ClaimHistoryActionEntity>();

            _claimHistoryActionRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<ClaimHistoryActionEntity, bool>>>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(QueryMock<ClaimHistoryActionEntity>.Create(historyActions));

            var response = await PostAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ClaimHistoryActionEntity>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(historyActions.Count(), result.Count);
        }


         [Trait("Category", "Integration")]
        public async Task GetMemberViewSettings_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/GetMemberViewSettings";
            var model = Fixture.Create<UserInfo>();

            var data = Fixture.Build<MemberViewSettingEntity>()
                .With(x => x.Id, model.MemberId)
                .Create();

            _memberViewSettingRepository.Setup(x => x.Query())
                .Returns(QueryMock<MemberViewSettingEntity>.Create(data));

            var response = await PostAsync(url, model);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<MemberViewSettingEntity>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
        }

        // [Trait("Category", "Integration")]
        public async Task RemoveBillingClaimDetails_ShouldReturnSuccessResult()
        {
            var url = $"{BaseUrl}/RemoveBillingClaimDetails";
            var removeModel = Fixture.Create<RemoveBillingClaimDetailsModel>();
            var claimId = Fixture.Create<int>();
            var paymentId = Fixture.Create<int>();

            _claimChargeEntryRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimChargeEntryEntity>
                .Create(new ClaimChargeEntryEntity { Id = removeModel.ChargeId, ClaimId = claimId }));

            _claimRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimEntity>
                .Create(new ClaimEntity { Id = claimId }));

            _claimAppointmentLinkRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimAppointmentLinkEntity>.Create(new ClaimAppointmentLinkEntity { ClaimChargeEntriesId = removeModel.ChargeId }));
            _linkChargeEntryRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimAppointmentLinkChargeEntry>.Create(new ClaimAppointmentLinkChargeEntry { ClaimChargeEntryEntityId = removeModel.ChargeId }));

            var serviceLinesId = Fixture.Create<int>();
            var paymentClaimServiceLineAdjustments_plus = Fixture.Build<PaymentClaimServiceLineAdjustmentEntity>()
                .With(x => x.IsAdjustmentPositive, true)
                .With(x => x.PaymentClaimServiceLineId, serviceLinesId)
                .Create();
            paymentClaimServiceLineAdjustments_plus.DateDeleted = null;

            var paymentClaimServiceLineAdjustments_minus = Fixture.Build<PaymentClaimServiceLineAdjustmentEntity>()
                .With(x => x.IsAdjustmentPositive, false)
                .With(x => x.PaymentClaimServiceLineId, serviceLinesId)
                .Create();
            paymentClaimServiceLineAdjustments_minus.DateDeleted = null;

            var paymentClaimServiceLine = Fixture.Build<PaymentClaimServiceLineEntity>()
                .With(x => x.Id, serviceLinesId)
                .With(x => x.ClaimChargeEntryId, removeModel.ChargeId)
                .Create();
            paymentClaimServiceLine.PaymentClaimServiceLineAdjustments.Add(paymentClaimServiceLineAdjustments_plus);
            paymentClaimServiceLine.PaymentClaimServiceLineAdjustments.Add(paymentClaimServiceLineAdjustments_minus);
            paymentClaimServiceLine.DateDeleted = null;

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
                .With(x => x.Claim, new ClaimEntity { Id = claimId })
                .Create();
            paymentClaim.PaymentClaimServiceLines.Add(paymentClaimServiceLine);

            _paymentRepository.Setup(x => x.Query())
                .Returns(QueryMock<PaymentEntity>.Create(paymentEntity));

            _paymentRepository.Setup(x => x.GetAllAsync(
                    It.IsAny<Expression<Func<PaymentEntity, bool>>>(), null))
                .ReturnsAsync(QueryMock<PaymentEntity>.Create(paymentEntity));

            _paymentClaimRepository.Setup(x => x.Query())
                .Returns(QueryMock<PaymentClaimEntity>.Create(paymentClaim));

            var chargeEntryWriteOff = Fixture.Build<ClaimChargeEntryWriteOffEntity>().Create();

            _claimChargeEntryWriteOffRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimChargeEntryWriteOffEntity>.Create(chargeEntryWriteOff));

            var response = await PostAsync(url, removeModel);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ActionSuccessResult>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.True(result.Success);
        }

         [Trait("Category", "Integration")]
        public async Task GetClaimLineAppointments_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/GetClaimLineAppointments";
            var serviceLineId = Fixture.Create<int>();
            var billincCode = Fixture.Create<string>();
            var claimId = Fixture.Create<int>();
            var diagnosisCode = Fixture.Create<string>();
            var appointmentId = Fixture.Create<int>();
            var clientId = Fixture.Create<int>();
            var intId = Fixture.Create<int>();

            var data = Fixture.Create<ServiceLineIdWithUserInfo>();

            var childProfile = Fixture.Create<ChildProfileEntityModel>();

            _claimChargeEntryRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimChargeEntryEntity>
                .Create(new ClaimChargeEntryEntity
                {
                    Id = data.ServiceLineId,
                    BillingCode = billincCode,
                    ClaimId = claimId,
                    DiagnosisCode = diagnosisCode,
                    DiagnosisCode2 = string.Empty,
                    Claim = Fixture.Create<ClaimEntity>()
                }));

            _claimAppointmentLinkRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimAppointmentLinkEntity>
                .Create(new ClaimAppointmentLinkEntity { ClaimId = claimId, AppointmentId = appointmentId }));

            var bCode = Fixture.Build<RethinkProviderBillingCode>()
                .With(x => x.billingCode, billincCode)
                .With(x => x.id, intId)
                .Create();
            var bCodes = new List<RethinkProviderBillingCode>() { bCode };
            _rethinkService.Setup(x => x.GetProviderBillingCode(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(bCodes);

            _rethinkService.Setup(x => x.GetDiagnosisByCodeAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(Fixture.Build<Diagnosis>().With(x => x.id, intId).Create());
            _rethinkService.Setup(x => x.GetChildProfile(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<ClientUserModel>().Create());

            var appt = Fixture.Build<AppointmentRethinkModel>()
                .With(x => x.providerBillingCodeId, intId)
                .With(x => x.diagnosisId, intId)
                .Without(x => x.ChildProfileAuthorizationBillingCode)
                .Create();
            var list = Fixture.Build<List<AppointmentRethinkModel>>().Create();
            list.Add(appt);

            _rethinkService.Setup(x => x.GetAppointmentListAsync(It.IsAny<List<int>>())).ReturnsAsync(list);

            var response = await PostAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<List<ServiceLineAppointmentModel>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
        }

         [Trait("Category", "Integration")]
        public async Task GetAccountClaims_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/GetAccountClaims";
            var data = Fixture.Create<ClaimSearchModel>();
            var id = Fixture.Create<int>();
            var claimid = Fixture.Create<int>();

            var childProfileId = Fixture.Create<int>();

            _paymentRepository.Setup(x => x.Query()).Returns(QueryMock<PaymentEntity>.Create(new PaymentEntity { Id = data.PaymentId, HcFunderId = id }));

            _claimRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimEntity>
                .Create(new ClaimEntity { Id = claimid, AccountInfoId = data.AccountInfoId, ChildProfileId = childProfileId, PrimaryFunderId = id }));

            _claimSubmissionRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimSubmissionEntity>
                .Create(new ClaimSubmissionEntity { Id = id, ClaimId = claimid }));

            var childProfile = Fixture.Build<ChildProfileRethinkModel>()
                .With(x => x.FirstName, data.SearchString.ToLower())
                .With(x => x.MiddleName, data.SearchString.ToLower())
                .With(x => x.LastName, data.SearchString.ToLower())
                .With(x => x.Id, childProfileId)
                .Create();
            var childProfiles = new List<ChildProfileRethinkModel>();
            childProfiles.Add(childProfile);

            _rethinkService.Setup(x => x.GetChildProfileByName(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(childProfiles);

            var response = await PostAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<List<ClaimDropdownModel>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

         [Trait("Category", "Integration")]
        public async Task IsDiagnosisServiceLineHasActiveClaims_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/IsDiagnosisServiceLineHasActiveClaims";
            var data = Fixture.Create<IsDiagnosisInUseModel>();

            var authorizationId = Fixture.Create<int>();

            var authDiagnosis = Fixture.Build<ChildProfileAuthorizationDiagnosisCode>()
                .With(x => x.childProfileAuthorizationId, authorizationId)
                .With(x => x.diagnosisId, data.DiagnosisCodeId)
                .With(x => x.ChildProfileAuthorization, new ClientAuthorization())
                .Create();

            var authDiagnosisCodes = new List<ChildProfileAuthorizationDiagnosisCode> { authDiagnosis };

            var auth = Fixture.Build<ClientAuthorization>()
                .With(x => x.id, authorizationId)
                .With(x => x.ChildProfileAuthorizationDiagnosisCodes, authDiagnosisCodes)
                .Without(x => x.ChildProfileReferringProvider)
                .Without(x => x.ChildProfileDiagnosis)
                .Create();

            var claimDiagnosis = Fixture.Build<ClaimDiagnosisCodeEntity>().With(x => x.DiagnosisId, data.DiagnosisCodeId).Create();

            var claimDiagnosisList = new List<ClaimDiagnosisCodeEntity> { claimDiagnosis };

            var claim = Fixture.Build<ClaimEntity>()
               .With(x => x.ChildProfileId, data.ClientId)
               .With(x => x.ChildProfileAuthorization, auth)
               .With(x => x.ClaimDiagnosisCodes, claimDiagnosisList)
               .Create();

            _claimRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimEntity>.Create(claim));

            var claims = Fixture.Build<ClaimEntity>()
                .With(x => x.ChildProfileId, data.ClientId)
               .With(x => x.ChildProfileAuthorization, auth)
               .With(x => x.ClaimDiagnosisCodes, claimDiagnosisList)
               .CreateMany();

            _claimRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(QueryMock<ClaimEntity>.Create(claims));

            _rethinkService.Setup(x => x.GetChildProfileAuthorizationById(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(auth);

            var response = await PostAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<bool>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(result);
        }

         [Trait("Category", "Integration")]
        public async Task SaveSelectedColumns_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/SaveSelectedColumns";

            var selectedColumns = new List<string> { "client", "funder" };

            var data = Fixture.Build<MemberViewSettingWithUserInfo>()
                .With(x => x.SelectedColumns, selectedColumns)
                .Create();

            _memberViewSettingRepository.Setup(x => x.Query()).Returns(QueryMock<MemberViewSettingEntity>
                .Create(new MemberViewSettingEntity { Id = data.MemberId }));

            var response = await PostAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<MemberViewSettingEntity>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.True(result.Client);
            Assert.True(result.Funder);
        }

         [Trait("Category", "Integration")]
        public async Task GetClaimProviderLocationUsageCount_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/GetClaimProviderLocationUsageCount";
            var providerLocationId = Fixture.Create<int>();

            var claim1 = Fixture.Build<ClaimEntity>()
                .With(x => x.ProviderLocationId, providerLocationId)
                .Create();

            var claim2 = Fixture.Build<ClaimEntity>()
                .With(x => x.ToLocationId, providerLocationId)
                .Create();

            var claim3 = Fixture.Build<ClaimEntity>()
                .With(x => x.ServiceLocationId, providerLocationId)
                .Create();

            var claims = new List<ClaimEntity> { claim1, claim2, claim3 };
            _claimRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimEntity>.Create(claims));

            var response = await PostAsync(url, providerLocationId);
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<int>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(claims.Count, result);
        }

         [Trait("Category", "Integration")]
        public async Task GetClaimReferringProviderUsageCount_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/GetClaimReferringProviderUsageCount";
            var referringProviderId = Fixture.Create<int>();

            var claims = Fixture.Build<ClaimEntity>()
                .With(x => x.ChildProfileReferringProviderId, referringProviderId)
                .CreateMany(2);

            _claimRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimEntity>.Create(claims));

            var response = await PostAsync(url, referringProviderId);
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<int>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, result);
        }

         [Trait("Category", "Integration")]
        public async Task GetClaimStaffAsRendingProviderUsageCount_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/GetClaimStaffAsRendingProviderUsageCount";
            var staffId = Fixture.Create<int>();
            var claims = Fixture.Build<ClaimEntity>()
                .With(x => x.RenderingStaffMemberId, staffId)
                .CreateMany(2);

            _claimRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimEntity>.Create(claims));

            var response = await PostAsync(url, staffId);
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<int>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, result);
        }

         [Trait("Category", "Integration")]
        public async Task CheckFunderUsageByBilledClaims_ShouldReturnTrue()
        {
            var url = $"{BaseUrl}/CheckFunderUsageByBilledClaims";
            var model = Fixture.Create<ClientFunderModel>();

            var claimSubmission = Fixture.Build<ClaimSubmissionEntity>()
                .With(x => x.FunderId, model.FunderId)
                .Create();
            var claimHistory = Fixture.Create<ClaimHistoryEntity>();
            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.DateDeleted, Fixture.Create<DateTime>())
                .With(x => x.ChildProfileId, model.ClientId)
                .With(x => x.ClaimSubmissions, new List<ClaimSubmissionEntity> { claimSubmission })
                .With(x => x.ClaimHistory, new List<ClaimHistoryEntity> { claimHistory })
                .Create();

            _claimRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimEntity>.Create(claim));

            var response = await PostAsync(url, model);
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<bool>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(result);
        }

         [Trait("Category", "Integration")]
        public async Task CheckFunderUsageByBilledClaims_ShouldReturnFalse()
        {
            var url = $"{BaseUrl}/CheckFunderUsageByBilledClaims";
            var model = Fixture.Create<ClientFunderModel>();

            var claim = Fixture.Create<ClaimEntity>();

            _claimRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimEntity>.Create(claim));

            var response = await PostAsync(url, model);
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<bool>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(result);
        }

         [Trait("Category", "Integration")]
        public async Task CheckIsAuthInUseByClaim_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/CheckIsAuthInUseByClaim";
            var authorizationId = Fixture.Create<int>();

            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.ClaimStatus, ClaimStatus.None)
                .Create();
            var claimSubmissions = Fixture.Build<ClaimSubmissionEntity>()
                .With(x => x.Claim, claim)
                .With(x => x.ChildProfileAuthorizationId, authorizationId)
                .Create();

            _claimRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimEntity>.Create(claim));
            _claimSubmissionRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimSubmissionEntity>.Create(claimSubmissions));

            var response = await PostAsync(url, authorizationId);
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<List<AuthorizationBuitData>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

         [Trait("Category", "Integration")]
        public async Task GetClaimHistoryVersion_ShouldReturnSuccessResult()
        {
            var url = $"{BaseUrl}/GetClaimHistoryVersion";
            var model = Fixture.Create<IdWithUserInfo>();

            var version = Fixture.Build<ClaimVersionEntity>().Without(x => x.ClaimHistory).Create();

            _claimVersionRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(version);

            var response = await PostAsync(url, model);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

         [Trait("Category", "Integration")]
        public async Task IsFunderHasActiveClaims_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/IsFunderHasActiveClaims";
            var model = Fixture.Create<IsClientFundersInUseModel>();

            var claim = Fixture.Create<ClaimEntity>();

            _claimRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimEntity>.Create(claim));

            var response = await PostAsync(url, model);
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<List<ClientFunderWithClaimModel>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        //[Obsolete]
        //[Theory]
        //[InlineData(true)]
        //[InlineData(false)]
        //[Trait("Category", "Integration")]
        //public async Task GetClaimPatients_ShouldReturnResult(bool shouldReturnEmptyResult)
        //{
        //    var url = $"{BaseUrl}/GetClaimPatients";
        //    var model = Fixture.Create<ClaimFilterGetModel>();
        //    var optionResult = shouldReturnEmptyResult
        //        ? new List<ClaimClientFilterOptionModel>()
        //        : new List<ClaimClientFilterOptionModel> { Fixture.Build<ClaimClientFilterOptionModel>()
        //        .With(x => x.Name, model.SearchValue.ToLower())
        //        .Create() };

        //    _billingDbHelper.Setup(x => x.ExecuteListAsync<ClaimClientFilterOptionModel>("GetClaimsPatientsFilters",
        //        It.IsAny<List<SqlParameter>>(),
        //        CommandType.StoredProcedure))
        //        .ReturnsAsync(optionResult);

        //    var response = await PostAsync(url, model);
        //    var content = await response.Content.ReadAsStringAsync();
        //    var result = JsonConvert.DeserializeObject<List<ClaimClientFilterOptionModel>>(content);

        //    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        //    Assert.NotNull(result);

        //    if (shouldReturnEmptyResult)
        //    {
        //        Assert.Empty(result);
        //    }
        //    else
        //    {
        //        Assert.NotEmpty(result);
        //    }
        //}

        //[Obsolete]
        //[Theory]
        //[InlineData(true)]
        //[InlineData(false)]
        //[Trait("Category", "Integration")]
        //public async Task GetClaimFunders_ShouldReturnResult(bool shouldReturnEmptyResult)
        //{
        //    var url = $"{BaseUrl}/GetClaimFunders";
        //    var model = Fixture.Create<ClaimFilterGetModel>();
        //    var optionResult = shouldReturnEmptyResult
        //        ? new List<ClaimFilterOptionModel>()
        //        : new List<ClaimFilterOptionModel> { Fixture.Build<ClaimFilterOptionModel>().With(x => x.Name, model.SearchValue.ToLower()).Create() };

        //    _billingDbHelper.Setup(x => x.ExecuteListAsync<ClaimFilterOptionModel>("GetClaimsFundersFilters",
        //        It.IsAny<List<SqlParameter>>(),
        //        CommandType.StoredProcedure))
        //        .ReturnsAsync(optionResult);

        //    var response = await PostAsync(url, model);
        //    var content = await response.Content.ReadAsStringAsync();
        //    var result = JsonConvert.DeserializeObject<List<ClaimFilterOptionModel>>(content);

        //    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        //    Assert.NotNull(result);

        //    if (shouldReturnEmptyResult)
        //    {
        //        Assert.Empty(result);
        //    }
        //    else
        //    {
        //        Assert.NotEmpty(result);
        //    }
        //}

        //[Obsolete]
        //[Theory]
        //[InlineData(true)]
        //[InlineData(false)]
        //[Trait("Category", "Integration")]
        //public async Task GetClaimRenderingProviders_ShouldReturnResult(bool shouldReturnEmptyResult)
        //{
        //    var url = $"{BaseUrl}/GetClaimRenderingProviders";
        //    var model = Fixture.Create<ClaimFilterGetModel>();
        //    var optionResult = shouldReturnEmptyResult
        //        ? new List<ClaimFilterOptionModel>()
        //        : new List<ClaimFilterOptionModel> { Fixture.Build<ClaimFilterOptionModel>().With(x => x.Name, model.SearchValue).Create() };

        //    _billingDbHelper.Setup(x => x.ExecuteListAsync<ClaimFilterOptionModel>("GetClaimsRPFilters",
        //        It.IsAny<List<SqlParameter>>(),
        //        CommandType.StoredProcedure))
        //        .ReturnsAsync(optionResult);

        //    var response = await PostAsync(url, model);
        //    var content = await response.Content.ReadAsStringAsync();
        //    var result = JsonConvert.DeserializeObject<List<ClaimFilterOptionModel>>(content);

        //    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        //    Assert.NotNull(result);

        //    if (shouldReturnEmptyResult)
        //    {
        //        Assert.Empty(result);
        //    }
        //    else
        //    {
        //        Assert.NotEmpty(result);
        //    }
        //}

        //[Obsolete]
        //[Theory]
        //[InlineData(true)]
        //[InlineData(false)]
        //[Trait("Category", "Integration")]
        //public async Task GetClaimIdentifiers_ShouldReturnResult(bool shouldReturnEmptyResult)
        //{
        //    var url = $"{BaseUrl}/GetClaimIdentifiers";
        //    var model = Fixture.Build<ClaimFilterGetModel>()
        //        .With(x => x.Tab, ClaimListingTab.PendingReview)
        //        .Create();

        //    var claims = Fixture.Build<ClaimEntity>()
        //        .With(x => x.AccountInfoId, model.AccountInfoId)
        //        .With(x => x.ClaimStatus, ClaimStatus.PendingReview)
        //        .CreateMany();

        //    var optionResult = shouldReturnEmptyResult
        //        ? new List<ClaimEntity>()
        //        : claims;

        //    _claimRepository.Setup(x => x.Query())
        //        .Returns(QueryMock<ClaimEntity>.Create(optionResult));

        //    var response = await PostAsync(url, model);
        //    var content = await response.Content.ReadAsStringAsync();
        //    var result = JsonConvert.DeserializeObject<List<ClaimFilterOptionModel>>(content);

        //    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        //    Assert.NotNull(result);

        //    if (shouldReturnEmptyResult)
        //    {
        //        Assert.Empty(result);
        //    }
        //    else
        //    {
        //        Assert.NotEmpty(result);
        //    }
        //}

        // [Trait("Category", "Integration")]
        public async Task CompleteClaims_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/CompleteClaims";
            var claim = Fixture.Create<ClaimEntity>();
            _claimRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimEntity>.Create(claim));

            var model = Fixture.Build<IdsWithUserInfo>()
                .With(x => x.Ids, new int[] { claim.Id })
                .With(x => x.AccountInfoId, claim.AccountInfoId)
                .With(x => x.MemberId, claim.MemberId)
                .Create();



            var response = await PostAsync(url, model);
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<List<string>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(claim.ClaimIdentifier, result.First());
        }

        // [Trait("Category", "Integration")]
        public async Task ApproveClaims_ShouldReturnResult()
        {
            var url = $"{BaseUrl}/ApproveClaims";
            var claim = Fixture.Build<ClaimEntity>().With(x => x.ClaimStatus, ClaimStatus.PendingReview).Create();

            _claimRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimEntity>.Create(claim));

            //_claimManagerService.Setup(x => x.SubmitInitialClaim(claim.Id, It.IsAny<int>(), ClaimDocumentType.Doc837P, ResponsibilitySequenceType.Primary)).ReturnsAsync(Fixture.Create<int>());

            _rethinkService.Setup(x => x.GetChildProfileFunderServiceLineMappingEntity(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Create<ServiceLines>());

            var model = Fixture.Build<IdsWithUserInfo>()
                .With(x => x.Ids, new int[] { claim.Id })
                .With(x => x.AccountInfoId, claim.AccountInfoId)
                .With(x => x.MemberId, claim.MemberId)
                .Create();


            var response = await PostAsync(url, model);
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<List<string>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(claim.Id, int.Parse(result.First()));
        }

        private void SetupMocks(ClaimIdWithUserInfo requestData, string setupClaimIdentifier = null)
        {
            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, requestData.Id)
                .With(x => x.ClaimIdentifier, setupClaimIdentifier ?? requestData.ClaimIdentifier)
                .With(x => x.AccountInfoId, requestData.AccountInfoId)
                .With(x => x.ClaimHistory, new List<ClaimHistoryEntity>())
                .Create();

            _claimRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimEntity>.Create(claim));
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
    }
}
