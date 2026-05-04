using AutoFixture;
using AutoMapper;
using BillingService.Domain.DataObjects.Billing;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.BillingSettings;
using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models;
using BillingService.Domain.Models.BillingSettings;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Domain.Services.Billing;
using BillingService.Domain.Utils;
using BillingService.XUnit.Tests.Common;
using BillingService.XUnit.Tests.Common.Mocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MockQueryable;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using Rethink.Services.Common.Entities.Billing.Feature;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Messaging;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Common.Models.ClientMicroServicesModels;
using Rethink.Services.Common.Models.Clients;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Rethink.Services.Common.Entities.Billing;


namespace BillingService.XUnit.Tests.Billing.Claim
{
    public class ClaimSyncServiceTest : BaseTest
    {

        private Mock<IRepository<BillingDbContext, ClaimEntity>> _claimRepository;
        private Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>> _claimChargeEntryRepository;
        private Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>> _claimAppointmentLinkRepository;
        private Mock<IRepository<BillingDbContext, ClaimDiagnosisCodeEntity>> _claimDiagnosisCodeRepository;
        private Mock<IRepository<BillingDbContext, ClaimAppointmentLinkChargeEntry>> _linkChargeRepository;
        private Mock<IRepository<BillingDbContext, PaymentClaimEntity>> _paymentClaimRepository;
        private Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>> _paymentClaimServiceLineRepository;
        private Mock<IClaimHistoryService> _claimHistoryService;
        private Mock<IClaimManagerService> _claimManagerService;
        private Mock<IClaimValidationService> _claimValidationService;
        private Mock<IMessageBus> _messageBus;
        private IClaimSyncService _claimSyncService;
        private IMapper _mapper;
        private Mock<IRethinkMasterDataMicroServices> _rethinkService;
        private Mock<IClaimUpdateService> _claimUpdateService;
        private Mock<IChargeEntryService> _chargeEntryService;
        private Mock<IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity>> _paymentClaimServiceLineAdjustmentRepository;
        private Mock<IPaymentPostingService> _paymentPostingService;
        private Mock<IRepository<BillingDbContext, PaymentEntity>> _paymentRepositoryMock;
        private Mock<IRepository<BillingDbContext, AppointmentClaimProcessingErrorEntity>> _appointmentClaimProcessingErrorRepository;
        private Mock<IRepository<BillingDbContext, UnProcessedApointmentScheduleEntity>> _unProcessedApointmentScheduleRepository;
        private Mock<IRepository<BillingDbContext, TimezonesEntity>> _timezonesRepository;
        private Mock<IRepository<BillingDbContext, FunderSettingsEntity>> _funderSettingRepo;
        private Mock<IClaimHistoryService> _claimHistoryMock;
        private Mock<IMessageBus> _busMock;
        private Mock<IRethinkMasterDataMicroServices> _rethinkMock;
        private readonly Mock<IConfiguration> _configuration;
        private Mock<IBillingSettingsService> _billingSettingsServiceMock;
        private int appointmentId;
        private int accountInfoId;
        private int claimId;
        private int funderId;
        private int expectedServiceId;
        private int memberId;
        private int clientId;
        private int expectedPaymentClaimId;
        private AppointmentRethinkModel appointment;
        private ClaimEntity claimEntity;
        public ClaimSyncServiceTest()
        {
            _claimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            _claimChargeEntryRepository = new Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>>();
            _claimAppointmentLinkRepository = new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>>();
            _claimDiagnosisCodeRepository = new Mock<IRepository<BillingDbContext, ClaimDiagnosisCodeEntity>>();
            _linkChargeRepository = new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkChargeEntry>>();
            _claimHistoryService = new Mock<IClaimHistoryService>();
            _claimManagerService = new Mock<IClaimManagerService>();
            _claimValidationService = new Mock<IClaimValidationService>();
            _rethinkService = new Mock<IRethinkMasterDataMicroServices>();
            _messageBus = new Mock<IMessageBus>();
            _paymentClaimRepository = new Mock<IRepository<BillingDbContext, PaymentClaimEntity>>();
            _paymentClaimServiceLineRepository = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>>();
            _claimUpdateService = new Mock<IClaimUpdateService>();
            _chargeEntryService = new Mock<IChargeEntryService>(); // Initialize the mock
            _paymentClaimServiceLineAdjustmentRepository = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity>>();
            _paymentPostingService = new Mock<IPaymentPostingService>();
            _paymentRepositoryMock = new Mock<IRepository<BillingDbContext, PaymentEntity>>();
            _appointmentClaimProcessingErrorRepository = new Mock<IRepository<BillingDbContext, AppointmentClaimProcessingErrorEntity>>();
            _unProcessedApointmentScheduleRepository = new Mock<IRepository<BillingDbContext, UnProcessedApointmentScheduleEntity>>();
            _configuration = new Mock<IConfiguration>();
            _billingSettingsServiceMock = new Mock<IBillingSettingsService>();
            _timezonesRepository = new Mock<IRepository<BillingDbContext, TimezonesEntity>>();
            _funderSettingRepo = new Mock<IRepository<BillingDbContext, FunderSettingsEntity>>();
            SetupMapper();

            _claimSyncService = new ClaimSyncService(
                _claimAppointmentLinkRepository.Object,
                _claimRepository.Object,
                _claimDiagnosisCodeRepository.Object,
                _claimChargeEntryRepository.Object,
                _claimHistoryService.Object,
                _claimManagerService.Object,
                _claimValidationService.Object,
                _messageBus.Object,
                _rethinkService.Object,
                _linkChargeRepository.Object,
                _paymentClaimRepository.Object,
                _paymentClaimServiceLineRepository.Object,
                _claimUpdateService.Object,
                _chargeEntryService.Object, // Pass the mock to the constructor
                _paymentClaimServiceLineAdjustmentRepository.Object,
                _paymentPostingService.Object,
                _paymentRepositoryMock.Object,
                _appointmentClaimProcessingErrorRepository.Object,
                _unProcessedApointmentScheduleRepository.Object,
                 _configuration.Object,
                _billingSettingsServiceMock.Object,
                _timezonesRepository.Object,
                _funderSettingRepo.Object
            );
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SyncClaimDeleteAsync_ShouldDeleteApptLinkAndCreateHistoryRecord(bool hasLink)
        {
            var accountInfoId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();
            var apptId = Fixture.Create<int>();
            var claimId = Fixture.Create<int>();
            var claimsIds = new int[] { claimId };
            var appt = Fixture.Build<AppointmentRethinkModel>().Without(x => x.ChildProfileAuthorizationBillingCode).Create();
            appt.id = apptId;
            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, claimId)
                .With(x => x.IsAppointmentDeleted, !hasLink)
                .With(x => x.ClaimStatus, ClaimStatus.Paid)
                .With(x => x.IsFlagged, false)
                .Create();

            var linkEntity = hasLink ?
                new List<ClaimAppointmentLinkEntity> {
                        Fixture.Build<ClaimAppointmentLinkEntity>()
                            .With(x => x.ClaimId, claimId)
                            .With(x => x.AppointmentId, apptId)
                            .Create()
                }
                : new List<ClaimAppointmentLinkEntity> { };
            _claimAppointmentLinkRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimAppointmentLinkEntity>.Create(linkEntity));
            _claimRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(claim);

            SetupService(appt.id);

            await _claimSyncService.SyncClaimDeleteAsync(apptId);

            if (hasLink) Assert.True(linkEntity[0].DateDeleted.HasValue);
            _claimHistoryService.Verify(x => x.AddAsync(It.IsAny<ClaimHistorySaveModel>(), It.IsAny<bool>()), Times.Exactly(hasLink ? 1 : 0));
            _claimRepository.Verify(x => x.Update(It.IsAny<ClaimEntity>()), Times.Exactly(hasLink ? 1 : 0));
            Assert.True(claim.IsAppointmentDeleted);
            Assert.False(claim.IsFlagged);
        }

        private void SetupService(int apptId)
        {
            var appt = Fixture.Build<AppointmentRethinkModel>()
                .With(x => x.id, apptId).Without(x => x.ChildProfileAuthorizationBillingCode)
                .Create();

            _rethinkService.Setup(x => x.GetAppointmentAsync(apptId)).ReturnsAsync(appt);
        }

        private void SetupMapper()
        {
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MapperProfile());
            });

            _mapper = mapperConfig.CreateMapper();
        }

        private void SetupMockServices_Repo(int claimCreationFrequency = 1)
        {
            appointmentId = Fixture.Create<int>();
            accountInfoId = Fixture.Create<int>();
            memberId = Fixture.Create<int>();
            clientId = Fixture.Create<int>();
            funderId = Fixture.Create<int>();
            claimId = Fixture.Create<int>();
            expectedServiceId = Fixture.Create<int>();
            expectedPaymentClaimId = Fixture.Create<int>();

            var startHour = 9;   // 9:00 AM
            var endHour = 11;

            appointment = Fixture.Build<AppointmentRethinkModel>()
                .With(x => x.id, appointmentId)
                .With(x => x.staffAccountInfoId, accountInfoId)
                .With(x => x.staffId, memberId)
                .With(x => x.clientId, clientId)
                .With(x => x.funderId, funderId)
                .With(x => x.appointmentTypeId, 1)
                .With(x => x.occurrenceTypeId, 1)
                .With(x => x.providerBillingCodeId, Fixture.Create<int>())
                .With(x => x.procedureCodeId, Fixture.Create<int>())
                .With(x => x.startDate, DateTime.Today)
                .With(x => x.startDateTime, DateTime.Today.AddHours(startHour))
                .With(x => x.endDateTime, DateTime.Today.AddHours(endHour))
                .With(x => x.startTime, startHour * 60)      // minutes from midnight
                .With(x => x.endTime, endHour * 60)          // minutes from midnight
                .With(x => x.actualStartTime, startHour * 60)
                .With(x => x.actualEndTime, endHour * 60)
                .With(x => x.StaffMember, Fixture.Build<RethinkStaffMember>().With(s => s.accountId, accountInfoId).With(s => s.memberId, memberId).Create())
                .With(x => x.ChildProfileAuthorizationBillingCode, () => null) // Will set later
                .With(x => x.serviceId, expectedServiceId) // Set the expected serviceId
                .Create();

            var childProfileAuthorization = Fixture.Build<ClientAuthorization>()
             .With(x => x.childProfileDiagnosisId, Fixture.Create<int>()) // Set to a non-null int value
             .With(x => x.id, Fixture.Create<int>())                      // Set to a non-null int value
             .With(x => x.childProfileDiagnosisId, Fixture.Create<int>())
             .With(x => x.renderingProviderStaffId, appointment.staffId)
             .With(x => x.ChildProfileAuthorizationDiagnosisCodes, new List<ChildProfileAuthorizationDiagnosisCode>()
             {
                 new ChildProfileAuthorizationDiagnosisCode() {
                        id = Fixture.Create<int>(),
                        childProfileAuthorizationId = Fixture.Create<int>(),
                        diagnosisId = Fixture.Create<int>(),
                        order = 1,
                        includeOnClaims = true,
                        childProfileDiagnosisId = Fixture.Create<int>(),
                        Diagnosis = Fixture.Build<Diagnosis>()
                            .With(d => d.diagnosisCode, "D123")
                            .With(d => d.description, "Test Diagnosis")
                            .Create()
                 }
             })
             .Create();

            var serviceFunders = new List<ServiceFunderData>
                {
                    new ServiceFunderData
                    {
                        funderId = funderId,
                        id = Fixture.Create<int>(),
                        providerServiceId = expectedServiceId
                    }
                };

            var funders = new FunderDataModel
            {
                id = funderId,
                funderTypeId = (int)FunderType.PrivatePay,
                funderName = "Test Funder",
                accountId = accountInfoId,
                isActive = true,
                combineChargeTypeId = (int)CombineChargeTypes.DontCombine,
                ServiceFunders = serviceFunders,
                claimCreationFrequency = claimCreationFrequency,
                frequency = 2,
                selectedDays = "Monday,Tuesday",
                time = "03:30",
                TimeZoneData = new TimeZoneDataModel
                {
                    Id = 17,
                    Name = "(UTC-04:30) Caracas"
                }
            };


            var billingCodeData = new BillingCodeData
            {
                billingCode = "T1000",
                funderId = funderId,
                description = "Test Billing Code",
                providerServiceId = expectedServiceId,
                unitTypeId = 1,
                serviceId = expectedServiceId,
                funders = funders
            };

            // Create AppointmentClientAuthBillingCodeModel manually
            var childProfileAuthorizationBillingCode = new AppointmentClientAuthBillingCodeModel
            {
                id = new Random().Next(1000, 9999),        // or use Fixture.Create<int>() if you want
                providerServiceId = expectedServiceId,
                noOfUnits = 1,
                unitTypeId = 1,
                frequencyTypeId = 1,
                schedulingGoalNoOfUnits = 1,
                schedulingGoalFrequencyTypeId = 1,
                AppointmentProviderBillingCode = billingCodeData,
                providerBillingCodeId = Fixture.Create<int>(),
            };

            // AccountInforEntityMode
            var accountInfoEntityMode = new AccountInfoEntityModel
            {
                Id = accountInfoId,
                subscriptionFeatures = new Dictionary<string, object>
                {
                    { "showOSBFlag", true }
                },
                subscriptionOptions =
                [
                    new() {
                        type = "ShowBilling",
                        value = true
                    },
                    new()
                    {
                        type = "BillingOptionId",
                        value = "Rethink"
                    },
                ]
            };


            // Assign to ChildProfileAuthorizationBillingCode
            appointment.ChildProfileAuthorizationBillingCode = childProfileAuthorizationBillingCode;
            appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization = childProfileAuthorization;

            // Setup unitTypes list
            var expectedUnitTypeId = childProfileAuthorizationBillingCode.AppointmentProviderBillingCode.unitTypeId;
            var unitTypes = new List<ClientUnitTypes>
            {
                Fixture.Build<ClientUnitTypes>()
                    .With(x => x.id, expectedUnitTypeId)
                    .With(x => x.unit, 60)
                    .Create()
                // Add more if needed
            };


            var funderMappingsMicro = new ChildProfileFunderResponseModel
            {
                data = new List<FunderDetails> { new FunderDetails { funderId = funderId, id = Fixture.Create<int>() } }
            };
            var serviceLineMappings = new List<ServiceLines>
            {
                Fixture.Build<ServiceLines>()
                    .With(x => x.serviceId, expectedServiceId)
                    .Create()
            };

            var expectedFunderId = appointment.funderId;
            var expectedServiceLineId = serviceLineMappings.First()?.id ?? Fixture.Create<int>();
            var funderMappingsMicroId = funderMappingsMicro.data.FirstOrDefault(x => x.funderId == expectedFunderId);

            var clientFunderMapping = Fixture.Build<FunderDetails>()
                .With(x => x.Funder, funders)
                .With(x => x.releaseOfInformationConfirmationTypeId, 1)
                .With(x => x.authorizedPaymentConfirmationTypeId, 2)
                .With(x => x.isAutismCoveredBenefit, true)
                .Create();

            var clientFunderServiceLine = Fixture.Build<ServiceLines>()
            .With(x => x.id, expectedServiceLineId)
            .With(x => x.ChildProfileFunderMapping, Fixture.Build<FunderDetails>()
                .With(f => f.id, funderMappingsMicroId.id)
                .With(f => f.funderId, funderMappingsMicroId.funderId)
                .With(f => f.Funder, funders)
                .Create())
            .Create();



            claimEntity = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, claimId)
                .With(x => x.AccountInfoId, accountInfoId)
                .With(x => x.ChildProfileId, clientId)
                .With(x => x.CreatedBy, memberId)
                .With(x => x.RenderingStaffMemberId, appointment.staffId)
                .With(x => x.LocationCodeId, appointment.locationId)
                .With(x => x.StartDate, appointment.startDate)
                .With(x => x.ClaimChargeEntries, new List<ClaimChargeEntryEntity>())
                .Create();

            var claims = new List<ClaimEntity> { claimEntity }.AsQueryable();

            _claimRepository.Setup(r => r.Query()).Returns(claims);

            // Mock provider location
            var providerLocation = new ProviderLocations
            {
                id = 101,
                isBillingLocation = false
            };

            // Mock main location (used when provider location is not billing)
            var mainLocation = new ProviderLocations
            {
                id = 202,
                isBillingLocation = true
            };

            // Mock referring providers for a client
            var referringProviders = new List<ReferringProviderDropdownModel>
            {
                new ReferringProviderDropdownModel
                {
                    Id = 501,
                    IsDefault = true
                },
                new ReferringProviderDropdownModel
                {
                    Id = 502,
                    IsDefault = false
                }
            };

            // Mock provider location list
            var providerLocationList = new ClientProviderLocationsModel
            {
                data = new List<ProviderLocations>
                {
                    new ProviderLocations
                    {
                        id = 301,
                        isBillingLocation = true
                    },
                    new ProviderLocations
                    {
                        id = 302,
                        isBillingLocation = false
                    }
                }
            };

            // Mock rendering providers
            var renderingProviders = new List<AuthRenderingProviderType>
            {
                new AuthRenderingProviderType
                {
                    Id = 701,
                    StaffMemberId =  appointment.staffId
                }
            };

            // Mock funder mappings (child profile funder mapping)
            var childProfileFunderMappings = new ChildProfileFunderResponseModel
            {
                data = new List<FunderDetails>
                {
                    new FunderDetails
                    {
                        id = 801,
                        funderId = appointment.funderId
                    }
                }
            };

            var billingFunderIdRequest = new BillingFunderIdRequestModel
            {
                Id = 1,
                AccountInfoId = accountInfoId,
                FunderId = funderId,
                FunderName = funders.funderName,
                ScheduleType = claimCreationFrequency,
                ScheduleTime = "03:30",
                ScheduleTimeZone = 1,
                WeeklyDays = "Monday,Tuesday",
                MonthlyFrequency = 2,
            };

            var timeZoneEntity = new TimezonesEntity
            {
                Id = 1,
                DisplayName = "(UTC-04:30) Caracas",
                SimpleName = "US Eastern Standard Time",
                Name = "US Eastern Standard Time",
            };

            _rethinkMock = new Mock<IRethinkMasterDataMicroServices>();
            _rethinkMock.Setup(s => s.GetAppointmentAsync(appointmentId)).ReturnsAsync(appointment);
            _rethinkMock.Setup(s => s.GetStaffMember(accountInfoId, memberId)).ReturnsAsync(appointment.StaffMember);
            _rethinkMock.Setup(s => s.GetProviderBillingCode(appointment.StaffMember.accountId, appointment.ChildProfileAuthorizationBillingCode.providerBillingCodeId ?? 0)).ReturnsAsync(billingCodeData);
            _rethinkMock.Setup(s => s.GetChildProfileAuthBillingCodeForAppointment(accountInfoId, clientId, appointment.procedureCodeId)).ReturnsAsync(childProfileAuthorizationBillingCode);
            _rethinkMock.Setup(s => s.GetFunder(accountInfoId, funderId)).ReturnsAsync(funders);
            _rethinkMock.Setup(s => s.GetChildProfileFunderMappings(accountInfoId, clientId)).ReturnsAsync(funderMappingsMicro);
            _rethinkMock.Setup(s => s.GetServiceLineMappingsByFunderId(accountInfoId, appointment.clientId.Value, It.IsAny<int>())).ReturnsAsync(serviceLineMappings);
            _rethinkMock.Setup(s => s.GetChildProfileFunderServiceLineMappingEntity(
                    accountInfoId,
                    appointment.clientId.Value,
                    funderMappingsMicroId.id,
                    expectedServiceLineId))
            .ReturnsAsync(clientFunderServiceLine);

            _billingSettingsServiceMock = new Mock<IBillingSettingsService>();
            _billingSettingsServiceMock.Setup(s => s.GetBillingFunderIdsSettingAsync(funderId, accountInfoId)).ReturnsAsync(billingFunderIdRequest);

            _timezonesRepository.Setup(t => t.Query()).Returns(new List<TimezonesEntity> { timeZoneEntity }.AsQueryable());

            _rethinkMock.Setup(s => s.GetChildProfileAuthorizationByClientId(
                appointment.staffAccountInfoId,
                appointment.clientId.Value,
                appointment.ChildProfileAuthorizationBillingCode.childProfileAuthorizationId))
            .ReturnsAsync(childProfileAuthorization);

            _rethinkMock.Setup(s => s.GetChildProfileAuthorizationDiagnosisCodesAsync(
                 accountInfoId,
                 appointment.clientId.Value,
                 childProfileAuthorization.childProfileDiagnosisId,
                 childProfileAuthorization.id
             )).ReturnsAsync(new List<ChildProfileAuthorizationDiagnosisCode>
             {
                new ChildProfileAuthorizationDiagnosisCode
                {
                    id = Fixture.Create<int>(),
                    diagnosisId = Fixture.Create<int>(),
                    childProfileAuthorizationId = childProfileAuthorization.id,
                    childProfileDiagnosisId = childProfileAuthorization.childProfileDiagnosisId,
                    includeOnClaims = true,
                    order = 1,
                    Diagnosis= Fixture.Build<Diagnosis>()
                        .With(d => d.diagnosisCode, "D123")
                        .With(d => d.description, "Test Diagnosis")
                        .Create()
                }
             });



            // Setup mock for GetUnitTypesAsync
            _rethinkMock.Setup(s => s.GetUnitTypesAsync())
                .ReturnsAsync(unitTypes);

            var providerService = Fixture.Build<ProviderServiceModel>()
                .With(x => x.Id, expectedServiceId)
                .With(x => x.Name, "Test Service")
                .With(x => x.BaseRate, 150m)

                .Create();

            // Setup mock for GetProviderService
            _rethinkMock.Setup(s => s.GetProviderService(
                    appointment.staffAccountInfoId,
                    expectedServiceId))
                .ReturnsAsync(new ClientProviderServiceModel
                {
                    id = providerService.Id,
                    accountId = appointment.staffAccountInfoId,
                    name = providerService.Name,
                    baseRate = providerService.BaseRate
                });

            var facility = Fixture.Build<ProviderLocationModel>()
             .With(x => x.providerLocationId, 123)
             .Create();

            _rethinkMock.Setup(s => s.GetChildProfileFacility(
                    appointment.staffAccountInfoId,
                    appointment.clientId ?? 0))
                .ReturnsAsync(facility);

            _rethinkMock.Setup(s => s.GetProviderLocation(claimEntity.AccountInfoId, It.IsAny<int>()))
                .ReturnsAsync(providerLocation);

            _rethinkMock.Setup(s => s.GetMainLocation(claimEntity.AccountInfoId))
                .ReturnsAsync(mainLocation);

            _rethinkMock.Setup(s => s.GetReferringProvidersByClientId(appointment.clientId.Value, appointment.clientAccountInfoId))
                .ReturnsAsync(referringProviders);

            _rethinkMock.Setup(s => s.GetProviderLocationList(appointment.staffAccountInfoId))
                .ReturnsAsync(providerLocationList);

            _rethinkMock.Setup(s => s.GetServiceFundersEntityListByFunderId(
                   appointment.clientAccountInfoId,
                   appointment.clientId ?? 0,
                   funderMappingsMicroId.funderId))
               .ReturnsAsync(serviceFunders);

            _rethinkMock.Setup(s => s.GetRenderingProvidersAsync(claimEntity.AccountInfoId, true))
                .ReturnsAsync(renderingProviders);

            _rethinkMock.Setup(r => r.GetChildProfileFunderMappingByMappingId(appointment.clientAccountInfoId, appointment.clientId ?? 0, claimEntity.ClientFunderId ?? 0))
               .ReturnsAsync(clientFunderMapping);

            _rethinkMock.Setup(r => r.GetAccountReturningEntityAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(accountInfoEntityMode);

            var secondaryFunderResponse = new ClaimNextFundersAndControlNumberModel
            {
                funders = new List<ClaimPatientFunderModel>() // leave empty to simulate no secondary
            };

            _claimManagerService.Setup(m => m.InitializeClaim(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(() => claimEntity);

            _claimUpdateService
                .Setup(s => s.CheckAndGetSecondaryFunderDetails(claimEntity.AccountInfoId, It.IsAny<ClaimEntity>()))
                .ReturnsAsync(secondaryFunderResponse);


            _claimHistoryMock = new Mock<IClaimHistoryService>();
            _claimRepository.Setup(r => r.Update(It.IsAny<ClaimEntity>())).Verifiable();
            _claimRepository.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            _paymentClaimServiceLineAdjustmentRepository
            .Setup(r => r.AddAndGetAsync(It.IsAny<PaymentClaimServiceLineAdjustmentEntity>()))
            .Callback<PaymentClaimServiceLineAdjustmentEntity>(entity =>
            {
                entity.Id = new Random().Next(10000, 99999);
                entity.DateCreated = DateTime.UtcNow;
            });

            _paymentClaimServiceLineAdjustmentRepository
                .Setup(r => r.CommitAsync())
                .Returns(Task.CompletedTask);

            _busMock = new Mock<IMessageBus>();

            var paymentClaimServiceLineEntity = Fixture.Build<PaymentClaimServiceLineEntity>()
            .With(x => x.Id, Fixture.Create<int>()).With(x => x.PaymentClaimId, claimId).Create();

            var paymentClaimServiceLineEntities = new List<PaymentClaimServiceLineEntity> { paymentClaimServiceLineEntity }.AsQueryable();

            _paymentClaimServiceLineRepository.Setup(r => r.AddAndGetAsync(It.IsAny<PaymentClaimServiceLineEntity>()))
            .ReturnsAsync((PaymentClaimServiceLineEntity entity) =>
            {
                entity.Id = new Random().Next(1000, 9999);
                entity.DateCreated = DateTime.UtcNow;
                return entity;

            });


           // var accountInfoEntity = Fixture.Build<AccountInfoEntityModel>()
           // .With(x => x.Id, accountInfoId)
           // .Create();

           // _rethinkMock.Setup(s => s.GetAccountReturningEntityAsync(accountInfoId, It.IsAny<bool>()))
           //.ReturnsAsync(accountInfoEntity);

            var linkEntities = new List<ClaimAppointmentLinkEntity>
            {
                Fixture.Build<ClaimAppointmentLinkEntity>()
                    .With(x => x.AppointmentId, appointment.id)
                    .With(x => x.ClaimId, claimId)
                    .With(x => x.DateDeleted, (DateTime?)null)
                    .Create()
            };

            var mockLinkEntities = linkEntities.AsQueryable().BuildMock();

            _claimAppointmentLinkRepository.Setup(r => r.Query()).Returns(mockLinkEntities);

            // Setup AddAsync to add to the list
            //_claimAppointmentLinkRepository.Setup(r => r.AddAsync(It.IsAny<ClaimAppointmentLinkEntity>()))
            //    .Callback<ClaimAppointmentLinkEntity>(entity => linkEntities.Add(entity))
            //    .Returns(Task.CompletedTask);

            // Setup SaveChangesAsync as a no-op
            _claimAppointmentLinkRepository.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Setup Update to update the entity in the list
            _claimAppointmentLinkRepository.Setup(r => r.Update(It.IsAny<ClaimAppointmentLinkEntity>()))
                .Callback<ClaimAppointmentLinkEntity>(entity =>
                {
                    var idx = linkEntities.FindIndex(x => x.AppointmentId == entity.AppointmentId);
                    if (idx >= 0) linkEntities[idx] = entity;
                });

            // Setup Query to return the backing list as IQueryable
            //_claimAppointmentLinkRepository.Setup(r => r.Query()).Returns(() => linkEntities.AsQueryable());


            var paymentId = Fixture.Create<int>();
            _paymentPostingService.Setup(p => p.CreateManualPatientPaymentAsync(It.IsAny<ManualCreatePaymentModel>()))
                .ReturnsAsync(paymentId);

            var patientNameModel = new ClientUserName { firstName = "Test", middleName = "M", lastName = "User" };
            var patientProfile = Fixture.Build<ClientUserModel>()
                .With(x => x.name, patientNameModel)
                .With(x => x.accountId, claimEntity.AccountInfoId)
                .Create();
            _rethinkMock.Setup(r => r.GetChildProfile(claimEntity.AccountInfoId, claimEntity.ChildProfileId)).ReturnsAsync(patientProfile);

            var paymentEntity = Fixture.Build<PaymentEntity>()
                .With(x => x.Id, paymentId)
                .With(x => x.DateDeleted, (DateTime?)null)
                .Create();

            var paymentEntities = new List<PaymentEntity> { paymentEntity }.AsQueryable().BuildMock();

            var paymentClaimEntity = Fixture.Build<PaymentClaimEntity>()
           .With(x => x.Id, expectedPaymentClaimId)
           .With(x => x.Payment, paymentEntity) // paymentEntity should be your mock PaymentEntity
           .Create();

            var paymentClaimEntities = new List<PaymentClaimEntity> { paymentClaimEntity }.AsQueryable().BuildMock();

            _paymentClaimRepository.Setup(r => r.AddAndGetAsync(It.IsAny<PaymentClaimEntity>()))
                .ReturnsAsync((PaymentClaimEntity entity) =>
                {
                    paymentClaimEntity.Id = entity.Id;
                    paymentClaimEntity.PaymentId = entity.PaymentId;
                    paymentClaimEntity.ClaimId = entity.ClaimId;
                    return paymentClaimEntity;
                });
            _paymentClaimRepository.Setup(r => r.Query()).Returns(paymentClaimEntities);

            _paymentRepositoryMock.Setup(r => r.Query()).Returns(paymentEntities);
            _paymentRepositoryMock.Setup(r => r.Update(It.IsAny<PaymentEntity>())).Verifiable();
            _paymentRepositoryMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);


            _chargeEntryService.Setup(s => s.GetAllClaimsByIdAsync(paymentEntity, It.Is<int[]>(ids => ids.Contains(claimEntity.Id))))
                   .ReturnsAsync(new List<ClaimChargeItem> {
                    Fixture.Build<ClaimChargeItem>()
                    .With(x => x.ClaimId, claimId)
                    .With(x => x.ClaimStatus, 0)
                    .With(x => x.PatientId, claimEntity.ChildProfileId)
                    .With(x => x.ChargeEntries, new List<ManualPaymentChargeEntryItem>
                    {
                        new ManualPaymentChargeEntryItem
                        {
                            Id = 999,
                            Charges = 100m,
                            TotalAmount = 80m,
                            DateOfService = DateTime.Today,
                            ServiceCode = "A1",
                            Units = 1,
                            Description = "Therapy",
                            Modifier1 = "25",
                            Modifier2 = "",
                            Modifier3 = "",
                            Modifier4 = "",
                        }
                    })
                    .Create()
                });
        }

        [Fact]
        public async Task SyncClaimAsync_PrivatePayFunder_MarksClaimDeletedAndCreatesAdjustment_WithFixture()
        {
            SetupMockServices_Repo();

            var service = new ClaimSyncService(
                _claimAppointmentLinkRepository.Object,
                _claimRepository.Object,
                new Mock<IRepository<BillingDbContext, ClaimDiagnosisCodeEntity>>().Object,
                new Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>>().Object,
                _claimHistoryMock.Object,
               _claimManagerService.Object,
                new Mock<IClaimValidationService>().Object,
                _busMock.Object,
                _rethinkMock.Object,
                new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkChargeEntry>>().Object,
                _paymentClaimRepository.Object,
                _paymentClaimServiceLineRepository.Object,
                _claimUpdateService.Object,
                _chargeEntryService.Object,
                _paymentClaimServiceLineAdjustmentRepository.Object,
                _paymentPostingService.Object,
                _paymentRepositoryMock.Object,
                _appointmentClaimProcessingErrorRepository.Object,
                _unProcessedApointmentScheduleRepository.Object,
                _configuration.Object,
                _billingSettingsServiceMock.Object,
                _timezonesRepository.Object,
                _funderSettingRepo.Object

            );

            // Act
            await service.SyncClaimAsync(appointmentId, accountInfoId);

            // Assert
            _claimRepository.Verify(r => r.Update(It.Is<ClaimEntity>(c => c.DateDeleted != null)), Times.AtLeastOnce());
            _paymentClaimServiceLineAdjustmentRepository.Verify(r => r.AddAndGetAsync(It.IsAny<PaymentClaimServiceLineAdjustmentEntity>()), Times.AtLeastOnce());
        }


        [Fact]
        public async Task CreatePaymentClaimWithLines_CreatesPaymentClaimAndAdjustmentsSuccessfully()
        {
            // Arrange
            var paymentId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();
            var claimId = Fixture.Create<int>();
            var accountInfoId = Fixture.Create<int>();
            var childProfileId = Fixture.Create<int>();

            // Fake ClaimChargeItem
            var claim = Fixture.Build<ClaimChargeItem>()
                .With(x => x.ClaimId, claimId)
                .With(x => x.ClaimStatus, 0)
                .With(x => x.ChargeEntries, new List<ManualPaymentChargeEntryItem>
                {
            new ManualPaymentChargeEntryItem
            {
                Id = 1,
                DateOfService = DateTime.Today,
                Charges = 100m,
                TotalAmount = 80m,
                ServiceCode = "ABC123",
                Units = 1,
                Description = "Therapy"
            }
                })
                .Create();

            var patient = Fixture.Build<ChildProfileEntityModel>()
                .With(p => p.Id, childProfileId)
                .With(p => p.FirstName, "John")
                .With(p => p.MiddleName, "M")
                .With(p => p.LastName, "Doe")
                .With(p => p.AccountInfoId, accountInfoId)
                .Create();

            var claimEntity = new ClaimEntity { Id = claimId, ClaimIdentifier = "CLM123" };

            var claimQueryable = new List<ClaimEntity> { claimEntity }.AsQueryable().BuildMock();

            // Mocks
            var claimRepoMock = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            claimRepoMock.Setup(r => r.Query()).Returns(claimQueryable);

            var paymentClaimRepoMock = new Mock<IRepository<BillingDbContext, PaymentClaimEntity>>();
            paymentClaimRepoMock.Setup(r => r.AddAndGetAsync(It.IsAny<PaymentClaimEntity>()))
                .ReturnsAsync((PaymentClaimEntity entity) =>
                {
                    entity.Id = 1001;
                    entity.PaymentId = paymentId;
                    return entity;
                });

            paymentClaimRepoMock.Setup(r => r.Query())
                .Returns(new List<PaymentClaimEntity>
                {
            new PaymentClaimEntity { Id = 1001, PaymentId = paymentId, Payment = new PaymentEntity { Id = paymentId } }
                }.AsQueryable().BuildMock());

            var serviceLineRepoMock = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>>();
            serviceLineRepoMock.Setup(r => r.AddAndGetAsync(It.IsAny<PaymentClaimServiceLineEntity>()))
                .ReturnsAsync((PaymentClaimServiceLineEntity e) =>
                {
                    e.Id = 2001;
                    return e;
                });

            var adjRepoMock = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity>>();
            adjRepoMock.Setup(r => r.AddAndGetAsync(It.IsAny<PaymentClaimServiceLineAdjustmentEntity>()))
                .ReturnsAsync((PaymentClaimServiceLineAdjustmentEntity e) =>
                {
                    e.Id = 3001;
                    return e;
                });

            // Create actual service (not a mock)
            var service = new ClaimSyncService(
                new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>>().Object,
                claimRepoMock.Object,
                new Mock<IRepository<BillingDbContext, ClaimDiagnosisCodeEntity>>().Object,
                new Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>>().Object,
                new Mock<IClaimHistoryService>().Object,
                new Mock<IClaimManagerService>().Object,
                new Mock<IClaimValidationService>().Object,
                new Mock<IMessageBus>().Object,
                new Mock<IRethinkMasterDataMicroServices>().Object,
                new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkChargeEntry>>().Object,
                paymentClaimRepoMock.Object,
                serviceLineRepoMock.Object,
                _claimUpdateService.Object,
                new Mock<IChargeEntryService>().Object,
                adjRepoMock.Object,
                new Mock<IPaymentPostingService>().Object,
                new Mock<IRepository<BillingDbContext, PaymentEntity>>().Object,
                _appointmentClaimProcessingErrorRepository.Object,
                _unProcessedApointmentScheduleRepository.Object,
                 _configuration.Object,
                 _billingSettingsServiceMock.Object,
                 _timezonesRepository.Object,
                 _funderSettingRepo.Object
            );

            //
            var result = await service.CreatePaymentClaimWithLines(paymentId, claim, patient, memberId);

            // Assert
            Assert.Equal(1, result);
            paymentClaimRepoMock.Verify(r => r.AddAndGetAsync(It.IsAny<PaymentClaimEntity>()), Times.Once);
            serviceLineRepoMock.Verify(r => r.AddAndGetAsync(It.IsAny<PaymentClaimServiceLineEntity>()), Times.Once);
            adjRepoMock.Verify(r => r.AddAndGetAsync(It.IsAny<PaymentClaimServiceLineAdjustmentEntity>()), Times.Once);
        }


        [Fact]
        public async Task SyncClaimAsync_CreatesNewLink_WhenLinkIsNull()
        {
            SetupMockServices_Repo();

            var linkEntities = new List<ClaimAppointmentLinkEntity>
            {
                Fixture.Build<ClaimAppointmentLinkEntity>()
                    .With(x => x.AppointmentId, Fixture.Create<int>())
                    .With(x => x.ClaimId, claimId)
                    .With(x => x.DateDeleted, (DateTime?)null)
                    .Create()
            };

            var mockLinkEntities = linkEntities.AsQueryable().BuildMock();

            _funderSettingRepo.Setup(x => x.Query()).Returns(new List<FunderSettingsEntity>
            {
                new FunderSettingsEntity
                {
                    FunderId = 1,
                    AccountInfoId = accountInfoId,
                    CombineChargesForSameClient = true,
                     DateDeleted = null
                }
            }.AsQueryable().BuildMock());

            _claimAppointmentLinkRepository.Setup(r => r.AddAsync(It.IsAny<ClaimAppointmentLinkEntity>()))
                .Callback<ClaimAppointmentLinkEntity>(entity => linkEntities.Add(entity))
                .Returns(Task.CompletedTask);

            // Setup SaveChangesAsync as a no-op
            _claimAppointmentLinkRepository.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Setup Update to update the entity in the list
            _claimAppointmentLinkRepository.Setup(r => r.Update(It.IsAny<ClaimAppointmentLinkEntity>()))
                .Callback<ClaimAppointmentLinkEntity>(entity =>
                {
                    var idx = linkEntities.FindIndex(x => x.AppointmentId == entity.AppointmentId);
                    if (idx >= 0) linkEntities[idx] = entity;
                });

            // Setup Query to return the backing list as IQueryable
            _claimAppointmentLinkRepository.Setup(r => r.Query()).Returns(() => linkEntities.AsQueryable().BuildMock());

            var service = new ClaimSyncService(
                _claimAppointmentLinkRepository.Object,
                _claimRepository.Object,
                new Mock<IRepository<BillingDbContext, ClaimDiagnosisCodeEntity>>().Object,
                new Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>>().Object,
                _claimHistoryMock.Object,
               _claimManagerService.Object,
                new Mock<IClaimValidationService>().Object,
                _busMock.Object,
                _rethinkMock.Object,
                new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkChargeEntry>>().Object,
                _paymentClaimRepository.Object,
                _paymentClaimServiceLineRepository.Object,
                _claimUpdateService.Object,
                _chargeEntryService.Object,
                _paymentClaimServiceLineAdjustmentRepository.Object,
                _paymentPostingService.Object,
                _paymentRepositoryMock.Object,
                _appointmentClaimProcessingErrorRepository.Object,
                _unProcessedApointmentScheduleRepository.Object,
                 _configuration.Object,
                 _billingSettingsServiceMock.Object,
                 _timezonesRepository.Object,
                 _funderSettingRepo.Object
            );

            // Act
            await service.SyncClaimAsync(appointmentId, accountInfoId);

            // Assert
            _claimAppointmentLinkRepository.Verify(r => r.AddAsync(It.IsAny<ClaimAppointmentLinkEntity>()), Times.Once);
            _claimAppointmentLinkRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
            //_claimHistoryService.Verify(h => h.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true), Times.AtLeastOnce());
        }


        public async Task SyncClaimAsync_ShouldSkipIfAppointmentNotFound()
        {
            // Arrange
            SetupMockServices_Repo();
            _rethinkMock.Setup(r => r.GetAppointmentAsync(It.IsAny<int>()))
                        .ReturnsAsync((AppointmentRethinkModel)null);

            var service = new ClaimSyncService(
                _claimAppointmentLinkRepository.Object,
                _claimRepository.Object,
                _claimDiagnosisCodeRepository.Object,
                _claimChargeEntryRepository.Object,
                _claimHistoryMock.Object,
                _claimManagerService.Object,
                _claimValidationService.Object,
                _busMock.Object,
                _rethinkMock.Object,
                _linkChargeRepository.Object,
                _paymentClaimRepository.Object,
                _paymentClaimServiceLineRepository.Object,
                _claimUpdateService.Object,
                _chargeEntryService.Object,
                _paymentClaimServiceLineAdjustmentRepository.Object,
                _paymentPostingService.Object,
                _paymentRepositoryMock.Object,
                _appointmentClaimProcessingErrorRepository.Object,
                _unProcessedApointmentScheduleRepository.Object,
                 _configuration.Object,
                 _billingSettingsServiceMock.Object,
                 _timezonesRepository.Object,
                 _funderSettingRepo.Object
            );

            // Act
            await service.SyncClaimAsync(appointmentId, accountInfoId);

            // Assert
            _claimRepository.Verify(r => r.Update(It.IsAny<ClaimEntity>()), Times.Never);
            _claimAppointmentLinkRepository.Verify(r => r.AddAsync(It.IsAny<ClaimAppointmentLinkEntity>()), Times.Never);
        }

        [Fact]
        public async Task SyncClaimAsync_ShouldCreateNewClaim_WhenNoExistingClaim()
        {
            // Arrange
            SetupMockServices_Repo();

            var linkEntities = new List<ClaimAppointmentLinkEntity>
            {
                Fixture.Build<ClaimAppointmentLinkEntity>()
                    .With(x => x.AppointmentId,Fixture.Create<int>())
                    .With(x => x.ClaimId, claimId)
                    .With(x => x.DateDeleted, (DateTime?)null)
                    .Create()
            };

            _funderSettingRepo.Setup(x => x.Query()).Returns(new List<FunderSettingsEntity>
            {
                new ()
                {
                    FunderId = 1,
                    AccountInfoId = accountInfoId,
                    CombineChargesForSameClient = true,
                     DateDeleted = null
                }
            }.AsQueryable().BuildMock());

            var mockLinkEntities = linkEntities.AsQueryable().BuildMock();

            _claimAppointmentLinkRepository.Setup(r => r.AddAsync(It.IsAny<ClaimAppointmentLinkEntity>()))
                .Callback<ClaimAppointmentLinkEntity>(entity => linkEntities.Add(entity))
                .Returns(Task.CompletedTask);

            // Setup SaveChangesAsync as a no-op
            _claimAppointmentLinkRepository.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Setup Update to update the entity in the list
            _claimAppointmentLinkRepository.Setup(r => r.Update(It.IsAny<ClaimAppointmentLinkEntity>()))
                .Callback<ClaimAppointmentLinkEntity>(entity =>
                {
                    var idx = linkEntities.FindIndex(x => x.AppointmentId == entity.AppointmentId);
                    if (idx >= 0) linkEntities[idx] = entity;
                });

            // Setup Query to return the backing list as IQueryable
            _claimAppointmentLinkRepository.Setup(r => r.Query()).Returns(() => linkEntities.AsQueryable().BuildMock());

            var service = new ClaimSyncService(
                _claimAppointmentLinkRepository.Object,
                _claimRepository.Object,
                _claimDiagnosisCodeRepository.Object,
                _claimChargeEntryRepository.Object,
                _claimHistoryMock.Object,
                _claimManagerService.Object,
                _claimValidationService.Object,
                _busMock.Object,
                _rethinkMock.Object,
                _linkChargeRepository.Object,
                _paymentClaimRepository.Object,
                _paymentClaimServiceLineRepository.Object,
                _claimUpdateService.Object,
                _chargeEntryService.Object,
                _paymentClaimServiceLineAdjustmentRepository.Object,
                _paymentPostingService.Object,
                _paymentRepositoryMock.Object,
                _appointmentClaimProcessingErrorRepository.Object,
                _unProcessedApointmentScheduleRepository.Object,
                 _configuration.Object,
                 _billingSettingsServiceMock.Object,
                 _timezonesRepository.Object,
                 _funderSettingRepo.Object
            );

            // Act
            await service.SyncClaimAsync(appointmentId, accountInfoId);

            // Assert
            _claimManagerService.Verify(m => m.InitializeClaim(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task SyncClaimAsync_ShouldNotDuplicateLinks_WhenLinkAlreadyExists()
        {
            // Arrange
            SetupMockServices_Repo();

            var existingLink = Fixture.Build<ClaimAppointmentLinkEntity>()
                .With(x => x.AppointmentId, appointmentId)
                .With(x => x.ClaimId, claimId)
                .Create();

            _claimAppointmentLinkRepository.Setup(r => r.Query())
                .Returns(new List<ClaimAppointmentLinkEntity> { existingLink }.AsQueryable().BuildMock());

            var service = new ClaimSyncService(
                _claimAppointmentLinkRepository.Object,
                _claimRepository.Object,
                _claimDiagnosisCodeRepository.Object,
                _claimChargeEntryRepository.Object,
                _claimHistoryMock.Object,
                _claimManagerService.Object,
                _claimValidationService.Object,
                _busMock.Object,
                _rethinkMock.Object,
                _linkChargeRepository.Object,
                _paymentClaimRepository.Object,
                _paymentClaimServiceLineRepository.Object,
                _claimUpdateService.Object,
                _chargeEntryService.Object,
                _paymentClaimServiceLineAdjustmentRepository.Object,
                _paymentPostingService.Object,
                _paymentRepositoryMock.Object,
                _appointmentClaimProcessingErrorRepository.Object,
                _unProcessedApointmentScheduleRepository.Object,
                 _configuration.Object,
                 _billingSettingsServiceMock.Object,
                 _timezonesRepository.Object,
                 _funderSettingRepo.Object
            );

            // Act
            await service.SyncClaimAsync(appointmentId, accountInfoId);

            // Assert
            _claimAppointmentLinkRepository.Verify(r => r.AddAsync(It.IsAny<ClaimAppointmentLinkEntity>()), Times.Never);
        }

        [Fact]
        public async Task SyncClaimAsync_ShouldUpdateExistingClaim_WhenClaimAlreadyExists()
        {
            // Arrange
            SetupMockServices_Repo();

            var service = new ClaimSyncService(
                _claimAppointmentLinkRepository.Object,
                _claimRepository.Object,
                _claimDiagnosisCodeRepository.Object,
                _claimChargeEntryRepository.Object,
                _claimHistoryMock.Object,
                _claimManagerService.Object,
                _claimValidationService.Object,
                _busMock.Object,
                _rethinkMock.Object,
                _linkChargeRepository.Object,
                _paymentClaimRepository.Object,
                _paymentClaimServiceLineRepository.Object,
                _claimUpdateService.Object,
                _chargeEntryService.Object,
                _paymentClaimServiceLineAdjustmentRepository.Object,
                _paymentPostingService.Object,
                _paymentRepositoryMock.Object,
                _appointmentClaimProcessingErrorRepository.Object,
                _unProcessedApointmentScheduleRepository.Object,
                 _configuration.Object,
                 _billingSettingsServiceMock.Object,
                 _timezonesRepository.Object,
                 _funderSettingRepo.Object
            );

            // Act
            await service.SyncClaimAsync(appointmentId, accountInfoId);

            // Assert
            _claimRepository.Verify(r => r.Update(It.IsAny<ClaimEntity>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task SyncClaimAsync_ShouldHandleExceptionGracefully()
        {
            // Arrange
            SetupMockServices_Repo();

            _claimRepository.Setup(r => r.Update(It.IsAny<ClaimEntity>()))
                            .Throws(new Exception("Database error"));

            var service = new ClaimSyncService(
                _claimAppointmentLinkRepository.Object,
                _claimRepository.Object,
                _claimDiagnosisCodeRepository.Object,
                _claimChargeEntryRepository.Object,
                _claimHistoryMock.Object,
                _claimManagerService.Object,
                _claimValidationService.Object,
                _busMock.Object,
                _rethinkMock.Object,
                _linkChargeRepository.Object,
                _paymentClaimRepository.Object,
                _paymentClaimServiceLineRepository.Object,
                _claimUpdateService.Object,
                _chargeEntryService.Object,
                _paymentClaimServiceLineAdjustmentRepository.Object,
                _paymentPostingService.Object,
                _paymentRepositoryMock.Object,
                _appointmentClaimProcessingErrorRepository.Object,
                _unProcessedApointmentScheduleRepository.Object,
                 _configuration.Object,
                 _billingSettingsServiceMock.Object,
                 _timezonesRepository.Object,
                 _funderSettingRepo.Object
            );

            // Act + Assert
            var ex = await Record.ExceptionAsync(() => service.SyncClaimAsync(appointmentId, accountInfoId));

            Assert.NotNull(ex);
            Assert.IsType<Exception>(ex);
            Assert.Contains("Database error", ex.Message);
        }

        [Fact]
        public async Task SyncClaimAsync_ShouldCoverBillingCode1_BillingCode2()
        {
            // Arrange
            SetupMockServices_Repo();

            var linkEntities = new List<ClaimAppointmentLinkEntity>();

            var mockLinkEntities = linkEntities.AsQueryable().BuildMock();

            _claimAppointmentLinkRepository.Setup(r => r.Query()).Returns(mockLinkEntities);

            _funderSettingRepo.Setup(x => x.Query()).Returns(new List<FunderSettingsEntity>
            {
                new FunderSettingsEntity
                {
                    FunderId = 1,
                    AccountInfoId = accountInfoId,
                    CombineChargesForSameClient = true,
                     DateDeleted = null
                }
            }.AsQueryable().BuildMock());

            var serviceFunders = new List<ServiceFunderData>
                {
                    new ServiceFunderData
                    {
                        funderId = funderId,
                        id = Fixture.Create<int>(),
                        providerServiceId = expectedServiceId
                    }
                };

            var funders = new FunderDataModel
            {
                id = funderId,
                funderTypeId = (int)FunderType.PrivatePay,
                funderName = "Test Funder",
                accountId = accountInfoId,
                isActive = true,
                combineChargeTypeId = (int)CombineChargeTypes.DontCombine,
                ServiceFunders = serviceFunders
            };


            var billingCodeData = new BillingCodeData
            {
                billingCode = "T1000",
                billingCode2 = "T1001",
                funderId = funderId,
                description = "Test Billing Code",
                providerServiceId = expectedServiceId,
                unitTypeId = 1,
                serviceId = expectedServiceId,
                funders = funders
            };

            _claimAppointmentLinkRepository.Setup(r => r.AddAsync(It.IsAny<ClaimAppointmentLinkEntity>()))
                .Callback<ClaimAppointmentLinkEntity>(entity => linkEntities.Add(entity))
                .Returns(Task.CompletedTask);

            // Setup SaveChangesAsync as a no-op
            _claimAppointmentLinkRepository.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Setup Update to update the entity in the list
            _claimAppointmentLinkRepository.Setup(r => r.Update(It.IsAny<ClaimAppointmentLinkEntity>()))
                .Callback<ClaimAppointmentLinkEntity>(entity =>
                {
                    var idx = linkEntities.FindIndex(x => x.AppointmentId == entity.AppointmentId);
                    if (idx >= 0) linkEntities[idx] = entity;
                });

            // Setup Query to return the backing list as IQueryable
            _claimAppointmentLinkRepository.Setup(r => r.Query()).Returns(() => linkEntities.AsQueryable().BuildMock());

            // Setup mocks for dependencies
            _rethinkMock.Setup(r => r.GetAppointmentAsync(appointmentId)).ReturnsAsync(appointment);


            _claimManagerService.Setup(m => m.InitializeClaim(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(claimEntity);

            _claimRepository.Setup(r => r.Query())
                .Returns(new List<ClaimEntity> { claimEntity }.AsQueryable().BuildMock());

            _rethinkMock.Setup(s => s.GetFunder(accountInfoId, funderId)).ReturnsAsync(funders);

            _rethinkMock.Setup(s => s.GetProviderBillingCode(appointment.StaffMember.accountId, appointment.ChildProfileAuthorizationBillingCode.providerBillingCodeId ?? 0)).ReturnsAsync(() => billingCodeData);

            _billingSettingsServiceMock.Setup(s => s.GetBillingFunderIdsSettingAsync(funderId, accountInfoId)).ReturnsAsync(new BillingFunderIdRequestModel
            {
                Id = funderId,
                AccountInfoId = accountInfoId,
                FunderId = funderId,
                FunderName = funders.funderName,
                ScheduleType = 1
            });

            var service = new ClaimSyncService(
                _claimAppointmentLinkRepository.Object,
                _claimRepository.Object,
                _claimDiagnosisCodeRepository.Object,
                _claimChargeEntryRepository.Object,
                _claimHistoryMock.Object,
                _claimManagerService.Object,
                _claimValidationService.Object,
                _busMock.Object,
                _rethinkMock.Object,
                _linkChargeRepository.Object,
                _paymentClaimRepository.Object,
                _paymentClaimServiceLineRepository.Object,
                _claimUpdateService.Object,
                _chargeEntryService.Object,
                _paymentClaimServiceLineAdjustmentRepository.Object,
                _paymentPostingService.Object,
                _paymentRepositoryMock.Object,
                _appointmentClaimProcessingErrorRepository.Object,
                _unProcessedApointmentScheduleRepository.Object,
                 _configuration.Object,
                 _billingSettingsServiceMock.Object,
                 _timezonesRepository.Object,
                 _funderSettingRepo.Object
            );

            // Act
            await service.SyncClaimAsync(appointmentId, accountInfoId);


            // The UpdateClaim method should be called once
            _claimUpdateService.Verify(c => c.CheckAndGetSecondaryFunderDetails(It.IsAny<int>(), It.IsAny<ClaimEntity>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task SyncClaimAsync_ShouldDeleteAndNotCreateHistory_WhenClaimIsAlreadyDeleted()
        {
            // Arrange
            SetupMockServices_Repo();

            var accountInfoId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();
            var apptId = Fixture.Create<int>();
            var claimId = Fixture.Create<int>();
            var claimsIds = new int[] { claimId };
            var appt = Fixture.Build<AppointmentRethinkModel>().Without(x => x.ChildProfileAuthorizationBillingCode).Create();
            appt.id = apptId;
            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, claimId)
                .With(x => x.IsAppointmentDeleted, true) // Already deleted
                .With(x => x.ClaimStatus, ClaimStatus.Paid)
                .With(x => x.IsFlagged, false)
                .Create();

            var linkEntity = new List<ClaimAppointmentLinkEntity>
                {
                    Fixture.Build<ClaimAppointmentLinkEntity>()
                        .With(x => x.ClaimId, claimId)
                        .With(x => x.AppointmentId, apptId)
                        .Create()
                };
            _claimAppointmentLinkRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimAppointmentLinkEntity>.Create(linkEntity));
            _claimRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(claim);

            SetupService(appt.id);

            // Act
            await _claimSyncService.SyncClaimDeleteAsync(apptId);

            // Assert
            Assert.True(claim.IsAppointmentDeleted);
            _claimHistoryService.Verify(x => x.AddAsync(It.IsAny<ClaimHistorySaveModel>(), It.IsAny<bool>()), Times.Once);
            _claimRepository.Verify(x => x.Update(It.IsAny<ClaimEntity>()), Times.Once);
        }

        [Fact]
        public async Task SyncClaimAsync_ShouldUpdateClaimStatusToFlagged()
        {
            // Arrange
            SetupMockServices_Repo();

            // The existing claim must have ReadyToBill status for the Flagged history to be written
            var existingClaim = new ClaimEntity
            {
                Id = claimId,
                AccountInfoId = accountInfoId,
                ChildProfileId = clientId,
                StartDate = appointment.startDateTime.Date,
                DateDeleted = null,
                LocationCodeId = appointment.locationId ?? 0,
                ClaimStatus = ClaimStatus.ReadyToBill,
                AuthorizationId = appointment.ChildProfileAuthorizationBillingCode.childProfileAuthorizationId,
                RenderingStaffMemberId = appointment.staffId,
                PrimaryFunderId = funderId,
                CreatedBy = memberId,
                ClaimHistory = new List<ClaimHistoryEntity>
                {
                    new ClaimHistoryEntity
                    {
                        ClaimHistoryAction = ClaimHistoryAction.ClaimCreated,
                        Mode = ClaimActionMode.System
                    }
                },
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>()
            };

            // The link already exists (set up in SetupMockServices_Repo), so the code
            // takes the else branch and queries the claim by link.ClaimId.
            // We need the claim repository to return the existing claim with ReadyToBill status.
            _claimRepository.Setup(r => r.Query())
                .Returns(new List<ClaimEntity> { existingClaim }.AsQueryable().BuildMock());

            // Setup charge entry repository to return empty list for the claim
            _claimChargeEntryRepository.Setup(r => r.Query())
                .Returns(new List<ClaimChargeEntryEntity>().AsQueryable().BuildMock());

            var service = new ClaimSyncService(
                _claimAppointmentLinkRepository.Object,
                _claimRepository.Object,
                _claimDiagnosisCodeRepository.Object,
                _claimChargeEntryRepository.Object,
                _claimHistoryMock.Object,
                _claimManagerService.Object,
                _claimValidationService.Object,
                _busMock.Object,
                _rethinkMock.Object,
                _linkChargeRepository.Object,
                _paymentClaimRepository.Object,
                _paymentClaimServiceLineRepository.Object,
                _claimUpdateService.Object,
                _chargeEntryService.Object,
                _paymentClaimServiceLineAdjustmentRepository.Object,
                _paymentPostingService.Object,
                _paymentRepositoryMock.Object,
                _appointmentClaimProcessingErrorRepository.Object,
                _unProcessedApointmentScheduleRepository.Object,
                 _configuration.Object,
                 _billingSettingsServiceMock.Object,
                 _timezonesRepository.Object,
                 _funderSettingRepo.Object
            );

            // Act
            await service.SyncClaimAsync(appointmentId, accountInfoId);

            // Assert
            _claimHistoryMock.Verify(h => h.AddAsync(It.Is<ClaimHistorySaveModel>(m =>
                m.ClaimHistoryAction == ClaimHistoryAction.Flagged &&
                m.ClaimAction == ClaimAction.Edit), It.IsAny<bool>()), Times.AtLeastOnce);

        }

        [Fact]
        public async Task SyncClaimAsync_ShouldCreateUnBilledAppointmentScheduleIfNotExists_WhenClaimFrequamcyIsDaily()
        {
            SetupMockServices_Repo(2);
            var existingLink = Fixture.Build<ClaimAppointmentLinkEntity>()
                .With(x => x.AppointmentId, appointmentId)
                .With(x => x.ClaimId, claimId)
                .Create();
            var service = new ClaimSyncService(
                _claimAppointmentLinkRepository.Object,
                _claimRepository.Object,
                _claimDiagnosisCodeRepository.Object,
                _claimChargeEntryRepository.Object,
                _claimHistoryMock.Object,
                _claimManagerService.Object,
                _claimValidationService.Object,
                _busMock.Object,
                _rethinkMock.Object,
                _linkChargeRepository.Object,
                _paymentClaimRepository.Object,
                _paymentClaimServiceLineRepository.Object,
                _claimUpdateService.Object,
                _chargeEntryService.Object,
                _paymentClaimServiceLineAdjustmentRepository.Object,
                _paymentPostingService.Object,
                _paymentRepositoryMock.Object,
                _appointmentClaimProcessingErrorRepository.Object,
                _unProcessedApointmentScheduleRepository.Object,
                 _configuration.Object,
                 _billingSettingsServiceMock.Object,
                 _timezonesRepository.Object,
                 _funderSettingRepo.Object);


            await service.SyncClaimAsync(appointmentId, accountInfoId);

            _unProcessedApointmentScheduleRepository.Verify(r => r.AddAsync(It.IsAny<UnProcessedApointmentScheduleEntity>()), Times.Once);
            _unProcessedApointmentScheduleRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
            _timezonesRepository.Verify(t => t.Query(), Times.Once);
        }

        [Fact]
        public async Task SyncClaimAsync_ShouldCreateUnBilledAppointmentScheduleIfNotExists_WhenClaimFrequamcyIsWeekly()
        {
            SetupMockServices_Repo(3);
            var existingLink = Fixture.Build<ClaimAppointmentLinkEntity>()
                .With(x => x.AppointmentId, appointmentId)
                .With(x => x.ClaimId, claimId)
                .Create();
            var service = new ClaimSyncService(
                _claimAppointmentLinkRepository.Object,
                _claimRepository.Object,
                _claimDiagnosisCodeRepository.Object,
                _claimChargeEntryRepository.Object,
                _claimHistoryMock.Object,
                _claimManagerService.Object,
                _claimValidationService.Object,
                _busMock.Object,
                _rethinkMock.Object,
                _linkChargeRepository.Object,
                _paymentClaimRepository.Object,
                _paymentClaimServiceLineRepository.Object,
                _claimUpdateService.Object,
                _chargeEntryService.Object,
                _paymentClaimServiceLineAdjustmentRepository.Object,
                _paymentPostingService.Object,
                _paymentRepositoryMock.Object,
                _appointmentClaimProcessingErrorRepository.Object,
                _unProcessedApointmentScheduleRepository.Object,
                 _configuration.Object,
                 _billingSettingsServiceMock.Object,
                 _timezonesRepository.Object,
                 _funderSettingRepo.Object);


            await service.SyncClaimAsync(appointmentId, accountInfoId);

            _unProcessedApointmentScheduleRepository.Verify(r => r.AddAsync(It.IsAny<UnProcessedApointmentScheduleEntity>()), Times.Once);
            _unProcessedApointmentScheduleRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
            _timezonesRepository.Verify(t => t.Query(), Times.Once);
        }
    }
}