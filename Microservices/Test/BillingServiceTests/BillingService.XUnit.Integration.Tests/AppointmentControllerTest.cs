using AutoFixture;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using BillingService.XUnit.Tests.Common.Mocks;
using Moq;
using Newtonsoft.Json;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Integration.Tests
{
    [Trait("Billing", "Integration")]
    [Collection("Billing")]
    public class AppointmentControllerTest : BaseControllerTest
    {
        private const string BaseUrl = "appointment";
        private const string EasternTimezoneName = "Eastern Standard Time";

        private readonly Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>> _linkRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimEntity>> _claimRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>> _claimAppointmentLinkRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimAppointmentLinkChargeEntry>> _linkChargeRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>> _claimChargeEntryRepository;
        private Mock<IRepository<BillingDbContext, PaymentClaimEntity>> _paymentClaimRepository;
        private Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>> _paymentClaimServiceLineRepository;
        private Mock<IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity>> _paymentClaimServiceLineAdjustmentRepository;
        private Mock<IRepository<BillingDbContext, ClaimSubmissionServiceLineEntity>> _claimSubmissionServiceLineRepository;

        private readonly Mock<IClaimValidationService> _claimValidationService;
        private readonly Mock<IClaimManagerService> _claimManagerService;
        private readonly Mock<IRethinkMasterDataMicroServices> _rethinkService;
        public AppointmentControllerTest(TestServerFixture fixture)
            : base(fixture)
        {
            _linkRepository = fixture.ClaimAppointmentLinkRepository;
            _claimRepository = fixture.ClaimRepository;
            _claimManagerService = fixture.ClaimManagerService;
            _claimValidationService = fixture.ClaimValidationService;
            _rethinkService = fixture.RethinkServices;
            _claimAppointmentLinkRepository = fixture.ClaimAppointmentLinkRepository;
            _linkChargeRepository = fixture.LinkChargeEntryRepository;
            _claimChargeEntryRepository = fixture.ClaimChargeEntryRepository;
            _paymentClaimRepository = fixture.PaymentClaimRepository;
            _paymentClaimServiceLineRepository = fixture.PaymentClaimServiceLineRepository;
            _paymentClaimServiceLineAdjustmentRepository = fixture.PaymentClaimServiceLineAdjustmentRepository;
            _claimSubmissionServiceLineRepository = fixture.ClaimSubmissionServiceLineRepository;
        }
        // [Trait("Category", "Integration")]
        public async Task GetFor_ShouldReturnResultModel()
        {
            var apptId = Fixture.Create<int>();
            var locationId = Fixture.Create<int>();
            var funderId = Fixture.Create<int>();
            var renderingProviderId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();
            var starDate = Fixture.Create<DateTime>();
            var yrs = Fixture.Create<int>();
            var endDate = starDate.AddYears(yrs);

            var url = $"{BaseUrl}/GetFor";
            var data = Fixture.Build<AppointmentGetRequest>().Create();
            data.EndDate = starDate.AddYears(yrs);

            SetupMocks(apptId, data.ClaimId, data.AccountInfoId, data.MemberId, data.LocationId ?? 0, data.ClientId, renderingProviderId, true);
            SetupServices(apptId, data.AccountInfoId, data.ClientId, data.LocationId ?? 0, funderId, renderingProviderId, data.MemberId, data.EndDate ?? DateTime.Now);

            var response = await PostAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<AppointmentModel>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Single(result);
        }

         [Trait("Category", "Integration")]
        [Trait("Category", "Integration")]
        public async Task GetForClaim_ShouldReturnResultModel()
        {
            var apptId = Fixture.Create<int>();
            var renderingProviderId = Fixture.Create<int>();
            var locationId = Fixture.Create<int>();
            var clientId = Fixture.Create<int>();
            var starDate = Fixture.Create<DateTime>();
            var yrs = Fixture.Create<int>();
            var endDate = starDate.AddYears(yrs);

            var url = $"{BaseUrl}/GetForClaim";
            var data = Fixture.Create<ClaimIdWithUserInfo>();

            var appointment = SetupMocks(apptId, data.Id, data.AccountInfoId, data.MemberId, locationId, clientId);
            SetupServices(apptId, appointment.StaffMember.Member.accountId, appointment.clientId ?? 0, locationId, appointment.funderId, renderingProviderId, data.MemberId, endDate);

            var response = await PostAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<AppointmentModel>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(appointment.id, result.FirstOrDefault()!.Id);
        }

         [Trait("Category", "Integration")]
        public async Task GetForClaim_ShouldReturnEmptyList()
        {
            var url = $"{BaseUrl}/GetForClaim";
            var data = Fixture.Create<ClaimIdWithUserInfo>();
            var linkEntity = Fixture.Build<ClaimAppointmentLinkEntity>()
                .With(x => x.Claim, Fixture.Create<ClaimEntity>())
                .Create();

            _linkRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimAppointmentLinkEntity>.Create(linkEntity));

            var response = await PostAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<AppointmentModel>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        // [Trait("Category", "Integration")]
        public async Task UnLinkAppointments_ShouldReturnSuccessResult()
        {
            var apptId = Fixture.Create<int>();
            var clientId = Fixture.Create<int>();
            var url = $"{BaseUrl}/UnLinkAppointments";
            var data = Fixture.Create<LinkAppointmentsRequest>();

            var app = SetupMocks(data.AppointmentIds.First(), data.ClaimId, data.AccountInfoId, data.MemberId, 0, clientId);
            SetupServicesForUnlink(data.AppointmentIds.First(), clientId);
            app.ChildProfileAuthorizationBillingCode = null;
            data.AppointmentIds = new List<int> { app.id };

            var response = await PostAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject(content);
            var success = ((dynamic)result)?.success;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.True((bool)success);
        }

        private AppointmentRethinkModel SetupMocks(int apptId, int claimId, int accountInfoId, int memberId, int locationId, int? clientId = 0, int? renderingProviderId = 0, bool markLinkDeleted = false)
        {
            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, claimId)
                .With(x => x.LocationCodeId, locationId)
                .With(x => x.RenderingStaffMemberId, renderingProviderId)
                .With(x => x.ChildProfileId, clientId == 0 ? Fixture.Create<int>() : clientId)
                .With(x => x.ClaimDiagnosisCodes, new List<ClaimDiagnosisCodeEntity>())
                .Without(x => x.ServiceLocationId)
                .Create();

            var childFunder = Fixture.Build<FunderDetails>()
                .With(x => x.id, claim.ClientFunderId)
                .Create();

            var staffMember = Fixture.Build<RethinkStaffMember>()
                .With(x => x.memberId, memberId)
                .With(x => x.Timezone, Fixture.Build<ClientTimezonesModel>()
                    .With(x => x.name, EasternTimezoneName)
                    .Create())
                .With(x => x.Member, Fixture.Build<RethinkAccountMember>()
                    .With(x => x.accountId, accountInfoId)
                    .Create())
                .Create();

            var workflowHistory = Fixture.Build<AppointmentWorkFlowHistoyModel>()
                .With(x => x.statusId, 3)
                .Create();
            var workflowHistoryCollection = new List<AppointmentWorkFlowHistoyModel> { workflowHistory };

            var appointment = Fixture.Build<AppointmentRethinkModel>()
                .With(x => x.ChildProfileAuthorizationBillingCode,
                    Fixture.Build<AppointmentClientAuthBillingCodeModel>()
                        .With(x => x.ProviderBillingCode, Fixture.Create<BillingCodeModel>())
                        .With(x => x.ChildProfileAuthorization, Fixture.Build<ClientAuthorization>()
                            .Without(x => x.ChildProfileAuthorizationDiagnosisCodes)
                            .Without(x => x.ChildProfileReferringProvider)
                            .Without(x => x.ChildProfileDiagnosis)
                            .With(x => x.authorizationNumber, claim.AuthorizationNumber)
                            .With(x => x.RenderingProvider, Fixture.Build<RethinkAccountMember>()
                                .With(x => x.id, claim.RenderingStaffMemberId)
                                .Create())
                            .Create())
                        .Create())
                .With(x => x.ProviderBillingCode, Fixture.Create<BillingCodeData>())
                .With(x => x.appointmentTypeId, 1)
                .With(x => x.occurrenceTypeId, 1)
                .With(x => x.StaffMember, staffMember)
                .With(x => x.PlaceOfService, Fixture.Build<LocationCodesModel>()
                    .With(x => x.id, claim.LocationCodeId)
                    .Create())
                .With(x => x.funderId, childFunder.funderId)
                .With(x => x.WorkFlowHistory, workflowHistory)
                .With(x => x.clientId, clientId)
                .With(x => x.locationId, locationId)
                .With(x => x.locationId, locationId)
                .With(x => x.id, apptId)
                .Create();
            appointment.DateDeleted = null;

            var linkChargeEntity = Fixture.Build<ClaimAppointmentLinkChargeEntry>().Create();

            var linkEntity = Fixture.Build<ClaimAppointmentLinkEntity>()
                .With(x => x.ClaimId, claimId)
                .With(x => x.AppointmentId, appointment.id)
                .With(x => x.Claim, Fixture.Build<ClaimEntity>()
                    .With(x => x.Id, claimId)
                    .With(x => x.AccountInfoId, accountInfoId)
                    .Without(x => x.AuthorizationId)
                    .Create())
                .With(x => x.ClaimAppointmentLinkChargeEntry, linkChargeEntity)
                .Create();
            linkEntity.DateDeleted = markLinkDeleted ? Fixture.Create<DateTime?>() : null;

            _linkRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimAppointmentLinkEntity>.Create(linkEntity));
            _claimRepository.Setup(x => x.Query())
                  .Returns(QueryMock<ClaimEntity>.Create(claim));
            _claimValidationService.Setup(x => x.ValidateClaimData(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<ClaimEntity>(), It.IsAny<ResponsibilitySequenceType>(), false, null))
                .Verifiable();

            _claimAppointmentLinkRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimAppointmentLinkEntity>.Create(linkEntity));

            _claimRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimEntity>.Create(claim));

            _linkChargeRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimAppointmentLinkChargeEntry>.Create(linkChargeEntity));

            var chargeEntry = Fixture.Build<ClaimChargeEntryEntity>().With(x => x.Id, linkChargeEntity.ClaimChargeEntryEntityId).Create();
            _claimChargeEntryRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimChargeEntryEntity>.Create(chargeEntry));

            return appointment;
        }

        private void SetupServices(int apptId, int accountInfoId, int childProfileId, int locationId, int funderId, int renderingProviderId, int currentMemberId, DateTime endDate)
        {
            _rethinkService.Setup(x => x.GetChildProfileAuthBillingCodeForAppointment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<AppointmentClientAuthBillingCodeModel>().With(x => x.ChildProfileAuthorization, new ClientAuthorization()).Create());

            _rethinkService.Setup(x => x.GetChildProfileFunderMappingByMappingId(accountInfoId, childProfileId, It.IsAny<int>())).ReturnsAsync(Fixture.Build<FunderDetails>().With(x => x.funderId, funderId).Create());

            var appointmentRethinkModel = Fixture.Build<AppointmentRethinkModel>()
                .With(x => x.clientAccountInfoId, accountInfoId)
                .With(x => x.staffAccountInfoId, accountInfoId)
                .With(x => x.appointmentTypeId, 1)
                .With(x => x.occurrenceTypeId, 1)
                .With(x => x.locationId, locationId)
                .With(x => x.funderId, funderId)
                .With(x => x.clientId, childProfileId)
                .With(x => x.ChildProfileAuthorizationBillingCode, new AppointmentClientAuthBillingCodeModel())
                .With(x => x.startDate, endDate)
                .With(x => x.id, apptId)
                .Create();


            var list = Fixture.Build<List<AppointmentRethinkModel>>().Create();
            list.Add(appointmentRethinkModel);

            _rethinkService.Setup(x => x.GetCompletedAppointmentListAsync(accountInfoId, childProfileId, It.IsAny<DateTime>())).ReturnsAsync(list);
            _rethinkService.Setup(x => x.GetAppointmentListAsync(It.IsAny<List<int>>())).ReturnsAsync(list);

            var locationCode = Fixture.Build<LocationCodesModel>().With(x => x.id, locationId).Create();
            var lcList = new List<LocationCodesModel>();
            lcList.Add(locationCode);
            _rethinkService.Setup(x => x.GetLocationCodes()).ReturnsAsync(lcList);

            _rethinkService.Setup(x => x.GetStaffMember(accountInfoId, It.IsAny<int>())).ReturnsAsync(Fixture.Build<RethinkStaffMember>().With(x => x.memberId, currentMemberId).Create());

            _rethinkService.Setup(x => x.GetMemberAsync(accountInfoId, It.IsAny<int>()))
                .ReturnsAsync(Fixture.Build<RethinkAccountMember>()
                .With(x => x.accountId, accountInfoId)
                .With(x => x.id, renderingProviderId).Create());

            _rethinkService.Setup(x => x.GetAccountReturningEntityAsync(accountInfoId, false)).ReturnsAsync(Fixture.Build<AccountInfoEntityModel>().Create());

            _rethinkService.Setup(x => x.GetChildProfileAuthorizationById(accountInfoId, It.IsAny<int>())).ReturnsAsync(Fixture.Build<ClientAuthorization>()
                .Without(x => x.ChildProfileAuthorizationDiagnosisCodes)
                .Without(x => x.ChildProfileReferringProvider)
                .Without(x => x.ChildProfileDiagnosis)
                .Create());

            _rethinkService.Setup(x => x.GetProviderBillingCode(accountInfoId, It.IsAny<int>())).ReturnsAsync(Fixture.Build<BillingCodeData>()
                .Without(x => x.funders).Create());

            _rethinkService.Setup(x => x.GetProviderService(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<ClientProviderServiceModel>().Create());

            var staff = Fixture.Build<RethinkStaffMember>().With(x => x.memberId, currentMemberId).Create();
            var staffList = new List<RethinkStaffMember>() { staff };
            _rethinkService.Setup(x => x.GetStaffMemberList(It.IsAny<int>())).ReturnsAsync(staffList);

            var timeZone = Fixture.Build<ClientTimezonesModel>().With(x => x.id, staff.timezoneId).With(x => x.name, "UTC").Create();
            var timeZonelist = new List<ClientTimezonesModel>() { timeZone };
            _rethinkService.Setup(x => x.GetTimezones()).ReturnsAsync(timeZonelist);


        }

        private void SetupServicesForUnlink(int apptId, int childProfileId)
        {
            int chargeId = Fixture.Create<int>();

            var appointmentRethinkModel = Fixture.Build<AppointmentRethinkModel>()
                .With(x => x.clientId, childProfileId)
                .With(x => x.id, apptId)
                .With(x => x.ChildProfileAuthorizationBillingCode, Fixture.Build<AppointmentClientAuthBillingCodeModel>().Without(x => x.ChildProfileAuthorization).Create())
                .Create();

            var list = Fixture.Build<List<AppointmentRethinkModel>>().Create();
            list.Add(appointmentRethinkModel);
            _rethinkService.Setup(x => x.GetAppointmentListAsync(It.IsAny<List<int>>())).ReturnsAsync(list);

            _rethinkService.Setup(x => x.GetChildProfileAuthBillingCodeForAppointment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<AppointmentClientAuthBillingCodeModel>().Without(x => x.ChildProfileAuthorization).Create());

            _rethinkService.Setup(x => x.GetProviderBillingCode(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(Fixture.Build<BillingCodeData>().Create());

            var paymentClaimServiceLineAdjustment = Fixture.Build<PaymentClaimServiceLineAdjustmentEntity>().With(x => x.PaymentClaimServiceLineId, chargeId).CreateMany();
            var paymentClaimAdjustment = Fixture.Build<PaymentClaimAdjustmentEntity>().With(x => x.PaymentClaimId, chargeId).CreateMany();
            var paymentClaimServiceLines = Fixture.Build<PaymentClaimServiceLineEntity>().With(x => x.ClaimChargeEntryId, chargeId).With(x => x.Id, chargeId).With(x => x.PaymentClaimServiceLineAdjustments, paymentClaimServiceLineAdjustment.ToList()).CreateMany();
            var paymentClaims = Fixture.Build<PaymentClaimEntity>().With(x => x.ClaimId, chargeId).With(x => x.Id, chargeId).With(x => x.PaymentClaimServiceLines, paymentClaimServiceLines.ToList()).Create();
            paymentClaims.DateDeleted = null;
            var submissionServiceLine = Fixture.Build<ClaimSubmissionServiceLineEntity>().With(x => x.ClaimChargeEntryId, chargeId).Create();

            _paymentClaimRepository.Setup(x => x.Query()).Returns(QueryMock<PaymentClaimEntity>.Create(paymentClaims));
            _paymentClaimServiceLineRepository.Setup(x => x.Query()).Returns(QueryMock<PaymentClaimServiceLineEntity>.Create(paymentClaimServiceLines));
            _paymentClaimServiceLineAdjustmentRepository.Setup(x => x.Query()).Returns(QueryMock<PaymentClaimServiceLineAdjustmentEntity>.Create(paymentClaimServiceLineAdjustment));
            _claimSubmissionServiceLineRepository.Setup(x => x.Query()).Returns(QueryMock<ClaimSubmissionServiceLineEntity>.Create(submissionServiceLine));
        }
    }
}
