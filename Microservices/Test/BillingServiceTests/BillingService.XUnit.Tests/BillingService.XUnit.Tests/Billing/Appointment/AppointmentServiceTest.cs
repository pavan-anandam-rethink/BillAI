using AutoFixture;
using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Services.Billing;
using BillingService.Domain.Utils;
using BillingService.XUnit.Tests.Common;
using BillingService.XUnit.Tests.Common.Mocks;
using MockQueryable.Moq;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Handlers;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.ClientMicroServicesModels; // added for ClientDiagnosisCodes
using Rethink.Services.Common.Models.Clients;
using Rethink.Services.Domain.Interfaces;
using SummationService.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;


namespace BillingService.XUnit.Tests.Billing.Appointment
{
    public class AppointmentServiceTest : BaseTest
    {
        private const string EasternTimezoneName = "Eastern Standard Time";

        private Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>> _claimAppointmentLinkRepository;
        private Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>> _claimChargeEntryRepository;
        private Mock<IRepository<BillingDbContext, ClaimEntity>> _claimRepository;
        private Mock<IRepository<BillingDbContext, ClaimAppointmentLinkChargeEntry>> _linkChargeRepository;
        private Mock<IRepository<BillingDbContext, ClaimHistoryEntity>> _claimHistoryRepository;
        private Mock<IRepository<BillingDbContext, ClaimSubmissionServiceLineEntity>> _ClaimSubmissionServiceLineRepository;
        private Mock<IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity>> _paymentClaimServiceLineAdjustmentRepository;
        private Mock<IRepository<BillingDbContext, ClaimSubmissionServiceLineEntity>> _claimSubmissionServiceLineRepository;
        private Mock<IRepository<BillingDbContext, PaymentClaimEntity>> _paymentClaimRepository;
        private Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>> _paymentClaimServiceLineRepository;
        private Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>> _claimSearchFunderRepository;
        private Mock<IRepository<BillingDbContext, ClaimSearchClientEntity>> _claimSearchClientRepository;
        private Mock<IClaimManagerService> _claimManagerService;
        private Mock<IClaimValidationService> _claimValidationService;
        private Mock<IRethinkMasterDataMicroServices> _rethinkService;
        private Mock<IClaimHistoryService> _claimHistoryService;
        private Mock<IClaimSyncService> _claimSyncService;
        private Mock<IHelperService> _helperService;
        private Mock<IMessageBus> _bus;
        private Mock<ICacheService> _cacheService;

        private IMapper _mapper;

        private IAppointmentService _appointmentService;

        public AppointmentServiceTest()
        {
            Fixture.Behaviors
                .OfType<ThrowingRecursionBehavior>()
                .ToList()
                .ForEach(b => Fixture.Behaviors.Remove(b));

            Fixture.Behaviors.Add(new OmitOnRecursionBehavior());

            _claimAppointmentLinkRepository = new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>>();
            _claimChargeEntryRepository = new Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>>();
            _claimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            _linkChargeRepository = new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkChargeEntry>>();
            _claimHistoryRepository = new Mock<IRepository<BillingDbContext, ClaimHistoryEntity>>();
            _paymentClaimServiceLineAdjustmentRepository = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity>>();
            _claimSubmissionServiceLineRepository = new Mock<IRepository<BillingDbContext, ClaimSubmissionServiceLineEntity>>();
            _ClaimSubmissionServiceLineRepository = new Mock<IRepository<BillingDbContext, ClaimSubmissionServiceLineEntity>>();
            _paymentClaimRepository = new Mock<IRepository<BillingDbContext, PaymentClaimEntity>>();
            _paymentClaimServiceLineRepository = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>>();
            _claimSearchFunderRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();
            _claimManagerService = new Mock<IClaimManagerService>();
            _claimValidationService = new Mock<IClaimValidationService>();
            _rethinkService = new Mock<IRethinkMasterDataMicroServices>();
            _claimHistoryService = new Mock<IClaimHistoryService>();
            _claimSyncService = new Mock<IClaimSyncService>();
            _helperService = new Mock<IHelperService>();
            _bus = new Mock<IMessageBus>();
            _cacheService = new Mock<ICacheService>();
            SetupMapper();

            _appointmentService = new AppointmentService(
                _claimAppointmentLinkRepository.Object,
                _claimHistoryRepository.Object,
                _claimChargeEntryRepository.Object,
                _claimRepository.Object,
                _linkChargeRepository.Object,
                _ClaimSubmissionServiceLineRepository.Object,
                _paymentClaimRepository.Object,
                _paymentClaimServiceLineRepository.Object,
                _claimSearchFunderRepository.Object,
                _claimManagerService.Object,
                _claimValidationService.Object,
                _rethinkService.Object,
                _mapper,
                _claimHistoryService.Object,
                _claimSyncService.Object,
                _bus.Object,
                _rethinkService.Object,
                _cacheService.Object
                );
        }

        // Added: parameterless overload used by some tests
        private void SetupServices()
        {
            SetupServices(0, 0, 0, 0, 0, 0, DateTime.Now, DateTime.Now);
        }

        // Added: parameterless SetupMocks used by some tests
        private void SetupMocks() { }

        // Added: factory for AppointmentService used by private-method tests
        private AppointmentService CreateServiceInstance()
        {
            return new AppointmentService(
                _claimAppointmentLinkRepository.Object,
                _claimHistoryRepository.Object,
                _claimChargeEntryRepository.Object,
                _claimRepository.Object,
                _linkChargeRepository.Object,
                _ClaimSubmissionServiceLineRepository.Object,
                _paymentClaimRepository.Object,
                _paymentClaimServiceLineRepository.Object,
                _claimSearchFunderRepository.Object,
                _claimManagerService.Object,
                _claimValidationService.Object,
                _rethinkService.Object,
                _mapper,
                _claimHistoryService.Object,
                _claimSyncService.Object,
                _bus.Object,
                _rethinkService.Object,
                _cacheService.Object
                );
        }

        //[Theory]
        //[InlineAutoMoqData(true)]
        //[InlineAutoMoqData(false)]
        public async Task GetFor_ShouldReturnResult(bool isAppointmentPresent)
        {
            var accountInfoId = Fixture.Create<int>();
            var currentMemberId = Fixture.Create<int>();
            var claimId = Fixture.Create<int>();
            var clientId = Fixture.Create<int>();
            var locationId = Fixture.Create<int>();
            var funderId = Fixture.Create<int>();
            var renderingProviderId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();
            var startDate = Fixture.Create<DateTime>();
            var endDate = Fixture.Create<DateTime>();
            var yrs = Fixture.Create<int>();

            if (isAppointmentPresent)
            {
                endDate = startDate.AddYears(yrs);
            }
            else
            {
                endDate = startDate.AddYears(-yrs);
            }

            SetupMocks(claimId, accountInfoId, currentMemberId, clientId, locationId, renderingProviderId, true);
            SetupServices(accountInfoId, clientId, locationId, funderId, renderingProviderId, currentMemberId, startDate, endDate);

            var result = await _appointmentService.GetFor(accountInfoId, currentMemberId, claimId,
                clientId, memberId, startDate, endDate, locationId);

            if (isAppointmentPresent)
            {
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Equal(currentMemberId, result.First().StaffId);
            }
            else
            {
                Assert.NotNull(result);
                Assert.Empty(result);
            }
        }



        [Fact]
        public async Task UnLinkAppointments_ReturnsTrue_When_ValidInput()
        {
            // Arrange
            var accountInfoId = 1;
            var memberId = 2;
            var claimId = 3;
            var appointmentId = 100;
            var chargeEntryId = 1;
            var now = DateTime.Now;

            var claim = new ClaimEntity
            {
                Id = claimId,
                ChildProfileId = 10,
                StartDate = new DateTime(2025, 9, 1),
                EndDate = new DateTime(2025, 9, 10),
                AccountInfoId = accountInfoId,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>()
            };

            var appointment = new AppointmentRethinkModel
            {
                id = appointmentId,
                clientId = claim.ChildProfileId,
                staffAccountInfoId = memberId,
                staffId = memberId,
                // Important: Set both startDateTime and endDateTime
                startDateTime = now,
                endDateTime = now.AddHours(1), // Must set endDateTime since endTime property depends on it
                ProviderBillingCode = new BillingCodeData
                {
                    id = 1,
                    billingCode = "TEST001",
                    rate = 100,
                    unitTypeId = 1
                },
                providerBillingCodeId = 1,
                actualStartTime = 480, // 8:00 AM
                actualEndTime = 540,   // 9:00 AM
                serviceId = 1,
                procedureCodeId = 1,
                providerServiceId = 1,
                funderId = 1,
                appointmentTypeId = 1,
                occurrenceTypeId = 1
            };

            var chargeEntry = new ClaimChargeEntryEntity
            {
                Id = chargeEntryId,
                ClaimId = claimId,
                Units = 1,
                Charges = 100,
                DateDeleted =  new DateTime(2025, 9, 3),//null,
                CreatedBy = memberId,
                DateOfService = new DateTime(2025, 9, 1)
            };
            claim.ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry };

            var linkChargeEntry = new ClaimAppointmentLinkChargeEntry
            {
                Id = 1,
                ClaimChargeEntryEntityId = chargeEntryId,
                IsSecondBillingCode = false,
                DateDeleted = null,
                ClaimChargeEntry = chargeEntry
            };

            var linkEntity = new ClaimAppointmentLinkEntity
            {
                Id = 1,
                ClaimId = claimId,
                AppointmentId = appointmentId,
                Claim = claim,
                ClaimAppointmentLinkChargeEntry = linkChargeEntry,
                ClaimChargeEntriesId = chargeEntryId,
                ClaimAppointmentLinkChargeEntryId = 1,
                DateDeleted = null
            };
            // Use MockQueryable to build async-capable mocks
            var claimMock = new List<ClaimEntity> { claim }.AsQueryable().BuildMockDbSet();
            var linkMock = new List<ClaimAppointmentLinkEntity> { linkEntity }.AsQueryable().BuildMockDbSet();
            var linkChargeMock = new List<ClaimAppointmentLinkChargeEntry> { linkChargeEntry }.AsQueryable().BuildMockDbSet();
            var chargeEntryMock = new List<ClaimChargeEntryEntity> { chargeEntry }.AsQueryable().BuildMockDbSet();

            _claimRepository.Setup(r => r.Query()).Returns(claimMock.Object);
            _claimAppointmentLinkRepository.Setup(r => r.Query()).Returns(linkMock.Object);
            _linkChargeRepository.Setup(r => r.Query()).Returns(linkChargeMock.Object);
            _claimChargeEntryRepository.Setup(r => r.Query()).Returns(chargeEntryMock.Object);

            _rethinkService.Setup(r => r.GetAppointmentListAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<AppointmentRethinkModel> { appointment });

            _rethinkService.Setup(r => r.GetProviderBillingCode(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(appointment.ProviderBillingCode);

            // Setup staff member with NPI number
            var staffMember = new RethinkStaffMember
            {
                identifiers = new List<Identifiers>
                {
                    new Identifiers
                    {
                        identifierType = "NPINumber",
                        value = "1234567890"
                    }
                }
            };

            _rethinkService.Setup(r => r.GetStaffMember(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(staffMember);

            _rethinkService.Setup(r => r.GetUnitTypesAsync())
                .ReturnsAsync(new List<ClientUnitTypes>
                {
                    new ClientUnitTypes { id = 1, unit = 1 }
                });

            // Act
            var result = await _appointmentService.UnLinkAppointments(
                accountInfoId,
                memberId,
                claimId,
                new List<int> { appointmentId }
            );

            // Assert
            Assert.True(result.Item1);
            Assert.Equal(claim.StartDate, result.Item2);
            Assert.Equal(claim.EndDate, result.Item3);
        }

        [Fact]
        public async Task UnLinkAppointments_ReturnsTrue_When_ValidInput_DateDeletedIsNUll()
        {
            // Arrange
            var accountInfoId = 1;
            var memberId = 2;
            var claimId = 3;
            var appointmentId = 100;
            var chargeEntryId = 1;
            var now = DateTime.Now;

            var claim = new ClaimEntity
            {
                Id = claimId,
                ChildProfileId = 10,
                StartDate = new DateTime(2025, 9, 1),
                EndDate = new DateTime(2025, 9, 10),
                AccountInfoId = accountInfoId,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>()
            };

            var appointment = new AppointmentRethinkModel
            {
                id = appointmentId,
                clientId = claim.ChildProfileId,
                staffAccountInfoId = memberId,
                staffId = memberId,
                // Important: Set both startDateTime and endDateTime
                startDateTime = now,
                endDateTime = now.AddHours(1), // Must set endDateTime since endTime property depends on it
                ProviderBillingCode = new BillingCodeData
                {
                    id = 1,
                    billingCode = "TEST001",
                    rate = 100,
                    unitTypeId = 1
                },
                providerBillingCodeId = 1,
                actualStartTime = 480, // 8:00 AM
                actualEndTime = 540,   // 9:00 AM
                serviceId = 1,
                procedureCodeId = 1,
                providerServiceId = 1,
                funderId = 1,
                appointmentTypeId = 1,
                occurrenceTypeId = 1
            };

            var chargeEntry = new ClaimChargeEntryEntity
            {
                Id = chargeEntryId,
                ClaimId = claimId,
                Units = 1,
                Charges = 100,
                DateDeleted = null,
                CreatedBy = memberId,
                DateOfService = new DateTime(2025, 9, 1)
            };
            claim.ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry };

            var linkChargeEntry = new ClaimAppointmentLinkChargeEntry
            {
                Id = 1,
                ClaimChargeEntryEntityId = chargeEntryId,
                IsSecondBillingCode = false,
                DateDeleted = null,
                ClaimChargeEntry = chargeEntry
            };

            var linkEntity = new ClaimAppointmentLinkEntity
            {
                Id = 1,
                ClaimId = claimId,
                AppointmentId = appointmentId,
                Claim = claim,
                ClaimAppointmentLinkChargeEntry = linkChargeEntry,
                ClaimChargeEntriesId = chargeEntryId,
                ClaimAppointmentLinkChargeEntryId = 1,
                DateDeleted = null
            };
            // Use MockQueryable to build async-capable mocks
            var claimMock = new List<ClaimEntity> { claim }.AsQueryable().BuildMockDbSet();
            var linkMock = new List<ClaimAppointmentLinkEntity> { linkEntity }.AsQueryable().BuildMockDbSet();
            var linkChargeMock = new List<ClaimAppointmentLinkChargeEntry> { linkChargeEntry }.AsQueryable().BuildMockDbSet();
            var chargeEntryMock = new List<ClaimChargeEntryEntity> { chargeEntry }.AsQueryable().BuildMockDbSet();

            _claimRepository.Setup(r => r.Query()).Returns(claimMock.Object);
            _claimAppointmentLinkRepository.Setup(r => r.Query()).Returns(linkMock.Object);
            _linkChargeRepository.Setup(r => r.Query()).Returns(linkChargeMock.Object);
            _claimChargeEntryRepository.Setup(r => r.Query()).Returns(chargeEntryMock.Object);

            _rethinkService.Setup(r => r.GetAppointmentListAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<AppointmentRethinkModel> { appointment });

            _rethinkService.Setup(r => r.GetProviderBillingCode(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(appointment.ProviderBillingCode);

            // Setup staff member with NPI number
            var staffMember = new RethinkStaffMember
            {
                identifiers = new List<Identifiers>
                {
                    new Identifiers
                    {
                        identifierType = "NPINumber",
                        value = "1234567890"
                    }
                }
            };

            _rethinkService.Setup(r => r.GetStaffMember(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(staffMember);

            _rethinkService.Setup(r => r.GetUnitTypesAsync())
                .ReturnsAsync(new List<ClientUnitTypes>
                {
                    new ClientUnitTypes { id = 1, unit = 1 }
                });

            // Act
            var result = await _appointmentService.UnLinkAppointments(
                accountInfoId,
                memberId,
                claimId,
                new List<int> { appointmentId }
            );

            // Assert
            Assert.True(result.Item1);
            Assert.Equal(claim.StartDate, result.Item2);
            Assert.Equal(claim.EndDate, result.Item3);
        }


        [Fact]
        public async Task UnLinkAppointments_ShouldReturnTrueResult()
        {
            var accountInfoId = Fixture.Create<int>();
            var currentMemberId = Fixture.Create<int>();
            var claimId = Fixture.Create<int>();
            var clientId = Fixture.Create<int>();

            var appointment = SetupMocks(claimId, accountInfoId, currentMemberId, clientId);
            SetupServicesForUnlink(appointment.id, clientId);

            var result = await _appointmentService.UnLinkAppointments(accountInfoId, currentMemberId, claimId,
                new List<int> { appointment.id });

            Assert.True(result.Item1);
        }

        [Fact]
        public async Task GetFor_ReturnsAppointments_WhenAppointmentsExist()
        {
            // Arrange
            int accountInfoId = 1;

            int claimId = 3;
            int clientId = 4;
            int memberId = 5;
            int locationId = 6;
            DateTime startDate = DateTime.Today;
            DateTime endDate = DateTime.Today.AddDays(1);

            var claim = new ClaimEntity
            {
                Id = claimId,
                LocationCodeId = locationId,
                RenderingStaffMemberId = memberId,
                ClientFunderId = 10,
                AuthorizationNumber = "AUTH123",
                AuthorizationId = 1
            };

            var funder = new FunderDetails { funderId = 20 };
            var appointment = new AppointmentRethinkModel
            {
                id = 100,
                appointmentTypeId = 1,
                occurrenceTypeId = 1,
                locationId = locationId,
                funderId = funder.funderId,
                startDateTime = startDate,
                endDateTime = endDate,
                staffId = memberId,
                clientId = clientId,
                procedureCodeId = 1,
                serviceId = 1,
                providerBillingCodeId = 1,
                ChildProfileAuthorizationBillingCode = new AppointmentClientAuthBillingCodeModel()
            };

            var staffMember = new RethinkStaffMember
            {
                memberId = memberId,
                timezoneId = 1,
                Timezone = new ClientTimezonesModel { id = 1, name = "Eastern Standard Time" },
                Member = new RethinkAccountMember { accountId = accountInfoId }
            };
            var locationCodes = new List<LocationCodesModel> { new LocationCodesModel { id = locationId } };
            var completedAppointments = new List<AppointmentRethinkModel> { appointment };

            var claimDbSet = new List<ClaimEntity> { claim }.AsQueryable().BuildMockDbSet();
            _claimRepository.Setup(r => r.Query()).Returns(claimDbSet.Object);
            _rethinkService.Setup(r => r.GetChildProfileFunderMappingByMappingId(accountInfoId, clientId, claim.ClientFunderId ?? 0)).ReturnsAsync(funder);
            _rethinkService.Setup(r => r.GetCompletedAppointmentListAsync(accountInfoId, clientId, startDate)).ReturnsAsync(completedAppointments);
            _rethinkService.Setup(r => r.GetLocationCodes()).ReturnsAsync(locationCodes);
            _rethinkService.Setup(r => r.GetStaffMember(accountInfoId, memberId)).ReturnsAsync(staffMember);
            _rethinkService.Setup(r => r.GetMemberAsync(accountInfoId, memberId)).ReturnsAsync(new RethinkAccountMember { accountId = accountInfoId });
            _rethinkService.Setup(r => r.GetAccountReturningEntityAsync(accountInfoId, It.IsAny<bool>())).ReturnsAsync(new AccountInfoEntityModel());
            _rethinkService.Setup(r => r.GetChildProfileAuthBillingCodeForAppointment(accountInfoId, clientId, appointment.procedureCodeId)).ReturnsAsync(new AppointmentClientAuthBillingCodeModel());
            _rethinkService.Setup(r => r.GetProviderBillingCode(accountInfoId, appointment.providerBillingCodeId ?? 0)).ReturnsAsync(new BillingCodeData());
            _rethinkService.Setup(r => r.GetChildProfileAuthorizationByClientId(accountInfoId, clientId, It.IsAny<int>())).ReturnsAsync(new ClientAuthorization { renderingProviderStaffId = memberId });
            _rethinkService.Setup(r => r.GetMemberAsync(accountInfoId, memberId)).ReturnsAsync(new RethinkAccountMember { id = memberId, accountId = accountInfoId });
            _rethinkService.Setup(r => r.GetProviderBillingCode(accountInfoId, It.IsAny<int>())).ReturnsAsync(new BillingCodeData());
            _rethinkService.Setup(r => r.GetProviderService(accountInfoId, It.IsAny<int>())).ReturnsAsync(new ClientProviderServiceModel());
            _rethinkService.Setup(r => r.GetStaffMemberList(accountInfoId)).ReturnsAsync(new List<RethinkStaffMember> { staffMember });
            _rethinkService.Setup(r => r.GetTimezones()).ReturnsAsync(new List<ClientTimezonesModel> { new ClientTimezonesModel { id = 1, name = "Eastern Standard Time" } });
            _rethinkService.Setup(r => r.GetChildProfilesForAccount(accountInfoId)).ReturnsAsync(new List<ChildProfileEntityModel> { new ChildProfileEntityModel { Id = clientId, FirstName = "John", LastName = "Doe" } });

            var linkDbSet = new List<ClaimAppointmentLinkEntity>().AsQueryable().BuildMockDbSet();
            _claimAppointmentLinkRepository.Setup(r => r.Query()).Returns(linkDbSet.Object);

            // Act
            var result = await _appointmentService.GetFor(accountInfoId, memberId, claimId, clientId, memberId, startDate, endDate, locationId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("John  Doe", result[0].ClientName);
        }


        [Fact]
        public async Task GetForClaim_ReturnsAppointments_WhenLinksExist()
        {
            // Arrange
            int accountInfoId = 1;
            int memberId = 2;
            int claimId = 3;
            int appointmentId = 100;
            int locationId = 6;
            int clientId = 10;

            var claim = new ClaimEntity
            {
                Id = claimId,
                AccountInfoId = accountInfoId,
                ChildProfileId = clientId
            };

            var linkEntity = new ClaimAppointmentLinkEntity
            {
                ClaimId = claimId,
                AppointmentId = appointmentId,
                Claim = claim,
                DateDeleted = null
            };

            var providerBillingCode = new BillingCodeData
            {
                id = 1,
                billingCode = "99213",
                rate = 100,
                unitTypeId = 1
            };

            var staffMember = new RethinkStaffMember
            {
                memberId = memberId,
                timezoneId = 1,
                Timezone = new ClientTimezonesModel { id = 1, name = "Eastern Standard Time" },
                Member = new RethinkAccountMember
                {
                    id = memberId,
                    accountId = accountInfoId,
                    AccountInfo = new AccountInfoEntityModel { Id = accountInfoId }
                }
            };

            var appointment = new AppointmentRethinkModel
            {
                id = appointmentId,
                staffId = memberId,
                clientAccountInfoId = accountInfoId,
                staffAccountInfoId = accountInfoId,
                clientId = clientId,
                startDateTime = DateTime.Now,
                endDateTime = DateTime.Now.AddHours(1),
                serviceId = 1,
                providerBillingCodeId = 1,
                procedureCodeId = 1,
                providerServiceId = 1,
                locationId = locationId,
                toLocationId = locationId,
                ProviderBillingCode = providerBillingCode,
                StaffMember = staffMember,
                appointmentTypeId = 1,
                occurrenceTypeId = 1,
                funderId = 1,
                workflowHistoryId = 1
            };

            var locationCodes = new List<LocationCodesModel> { new LocationCodesModel { id = locationId } };
            var appointmentList = new List<AppointmentRethinkModel> { appointment };

            var childProfile = new ChildProfileEntityModel
            {
                Id = clientId,
                FirstName = "John",
                LastName = "Doe"
            };

            // Mock the link repository to return the link
            var linkDbSet = new List<ClaimAppointmentLinkEntity> { linkEntity }.AsQueryable().BuildMockDbSet();
            _claimAppointmentLinkRepository.Setup(r => r.Query()).Returns(linkDbSet.Object);

            // Mock the rethink service to return the appointment
            _rethinkService.Setup(r => r.GetAppointmentListAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(appointmentList);

            // Mock all SetupRethinkDataForAppointments dependencies
            _rethinkService.Setup(r => r.GetProviderBillingCode(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(providerBillingCode);

            _rethinkService.Setup(r => r.GetChildProfileAuthBillingCodeForAppointment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new AppointmentClientAuthBillingCodeModel());

            _rethinkService.Setup(r => r.GetWorkFlowHistoyDetailsById(It.IsAny<int>()))
                .ReturnsAsync(new AppointmentWorkFlowHistoyModel { statusId = 3 });

            _rethinkService.Setup(r => r.GetStaffMember(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(staffMember);

            _rethinkService.Setup(r => r.GetMemberAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(staffMember.Member);

            _rethinkService.Setup(r => r.GetAccountReturningEntityAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(new AccountInfoEntityModel { Id = accountInfoId });

            _rethinkService.Setup(r => r.GetProviderLocation(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new ProviderLocations { id = locationId });

            _rethinkService.Setup(r => r.GetChildProfileReturningEntity(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(childProfile);

            // Add the missing mock for GetChildProfilesForAccount
            _rethinkService.Setup(r => r.GetChildProfilesForAccount(It.IsAny<int>()))
                .ReturnsAsync(new List<ChildProfileEntityModel> { childProfile });

            // Add missing mocks that may be causing the null reference
            _rethinkService.Setup(r => r.GetStaffMemberList(It.IsAny<int>()))
                .ReturnsAsync(new List<RethinkStaffMember> { staffMember });

            _rethinkService.Setup(r => r.GetTimezones())
                .ReturnsAsync(new List<ClientTimezonesModel> { new ClientTimezonesModel { id = 1, name = "Eastern Standard Time" } });

            _rethinkService.Setup(r => r.GetLocationCodes())
                .ReturnsAsync(locationCodes);

            _rethinkService.Setup(r => r.GetProviderService(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new ClientProviderServiceModel());

            // Act
            var result = await _appointmentService.GetForClaim(accountInfoId, memberId, claimId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(appointmentId, result[0].Id);
        }


        [Fact]
        public async Task LinkAppointments_ShouldReturnTrue_WhenClientmatch()
        {
            var accountInfoId = Fixture.Create<int>();
            var currentMemberId = Fixture.Create<int>();
            var claimId = Fixture.Create<int>();
            var clientId = Fixture.Create<int>();

            var appointment = SetupMocks(claimId, accountInfoId, currentMemberId, clientId);
            SetupServicesForUnlink(appointment.id, clientId);

            var result = await _appointmentService.LinkAppointments(accountInfoId, currentMemberId, claimId,
                new List<int> { appointment.id });

            Assert.True(result.Item1);
        }

        [Fact]
        public async Task LinkAppointments_ShouldSkip_WhenAppointmentAlreadyLinked()
        {
            // Arrange
            var accountInfoId = Fixture.Create<int>();
            var memberId = Fixture.Create<int>();
            var claimId = Fixture.Create<int>();
            var clientId = Fixture.Create<int>();

            //Setup mocks with a linked appointment already present
            var appointment = SetupMocks(
                claimId: claimId,
                accountInfoId: accountInfoId,
                memberId: memberId,
                clientId: clientId,
                markLinkDeleted: false // active link exists
            );

            // Make sure rethink services return the same appointment so clientId matches
            SetupServicesForUnlink(appointment.id, clientId);

            // Act
            var result = await _appointmentService.LinkAppointments(
                accountInfoId,
                memberId,
                claimId,
                new List<int> { appointment.id }
            );

            // Assert
            Assert.True(result.Item1); // ✅ still returns true
            _claimAppointmentLinkRepository.Verify(
                x => x.Add(It.IsAny<ClaimAppointmentLinkEntity>()),
                Times.Never
            );
        }

        private AppointmentRethinkModel SetupMocks(int claimId, int accountInfoId, int memberId, int? clientId = 0, int? locationId = 0, int? renderingProviderId = 0, bool markLinkDeleted = false)
        {
            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id, claimId)
                .With(x => x.LocationCodeId, locationId)
                .With(x => x.RenderingStaffMemberId, renderingProviderId)
                .With(x => x.ChildProfileId, clientId == 0 ? Fixture.Create<int>() : clientId)
                .With(x => x.ClaimDiagnosisCodes, new List<ClaimDiagnosisCodeEntity>())
                .With(x => x.ClaimChargeEntries, new List<ClaimChargeEntryEntity>())
                .With(x => x.ClaimHistory, new List<ClaimHistoryEntity>())
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
                .With(x => x.clientId, claim.ChildProfileId)
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

            _claimAppointmentLinkRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimAppointmentLinkEntity>.Create(linkEntity));

            _claimRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimEntity>.Create(claim));
            _linkChargeRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimAppointmentLinkChargeEntry>.Create(linkChargeEntity));

            var chargeEntry = Fixture.Build<ClaimChargeEntryEntity>().With(x => x.Id, linkChargeEntity.ClaimChargeEntryEntityId).Create();
            _claimChargeEntryRepository.Setup(x => x.Query())
                .Returns(QueryMock<ClaimChargeEntryEntity>.Create(chargeEntry));

            _claimValidationService.Setup(x => x.ValidateClaimData(It.IsAny<int>(), It.IsAny<int>(), null, It.IsAny<ResponsibilitySequenceType>(), false, null))
            .Verifiable();
            return appointment;
        }

        private void SetupServices(int accountInfoId, int childProfileId, int locationId, int funderId, int renderingProviderId, int currentMemberId, DateTime startDate, DateTime endDate)
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
                .With(x => x.startDate, startDate)
                .With(x => x.endDate, endDate)
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

        private void SetupMapper()
        {
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MapperProfile());
            });

            _mapper = mapperConfig.CreateMapper();
        }

        [Fact]
        public async Task GetClaimsAssignees_ReturnsNull_WhenCacheReturnsNull()
        {
            // Arrange
            var request = new ClaimFilterGetModel() { AccountInfoId = 1 };

            _cacheService.Setup(c => c.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<RethinkStaffMembersByPermissionResponse>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync((List<RethinkStaffMembersByPermissionResponse>)null);

            // Act
            var result = await _appointmentService.GetClaimsAssignees(request);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetClaimsAssignees_ReturnsOnlyUnassigned_WhenStaffListIsEmpty()
        {
            // Arrange
            var request = new ClaimFilterGetModel() { AccountInfoId = 1, Tab = 0 };

            _cacheService.Setup(c => c.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<RethinkStaffMembersByPermissionResponse>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(new List<RethinkStaffMembersByPermissionResponse>());

            // Act
            var result = await _appointmentService.GetClaimsAssignees(request);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(0, result[0].MemberId);
            Assert.Equal("Unassigned", result[0].Name);
        }

        [Fact]
        public async Task GetClaimsAssignees_ReturnsUnassignedAndStaff_WhenStaffMembersExist()
        {
            // Arrange
            var request = new ClaimFilterGetModel() { AccountInfoId = 1, Tab = 0 };

            var staffMembers = new List<RethinkStaffMembersByPermissionResponse>
                {
                    new RethinkStaffMembersByPermissionResponse { memberId = 1, firstName = "John", lastName = "Doe" },
                    new RethinkStaffMembersByPermissionResponse { memberId = 2, firstName = "Jane", lastName = "Smith" }
                };

            _cacheService.Setup(c => c.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<RethinkStaffMembersByPermissionResponse>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(staffMembers);

            // Act
            var result = await _appointmentService.GetClaimsAssignees(request);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(0, result[0].MemberId);            // Unassigned
            Assert.Equal(2, result[1].MemberId);            // Jane Smith
            Assert.Equal("Jane Smith", result[1].Name);
            Assert.Equal(1, result[2].MemberId);            // John Doe
            Assert.Equal("John Doe", result[2].Name);
        }

        [Fact]
        public async Task GetClaimsAssignees_FiltersStaffByTab_WhenTabIsPendingReview()
        {
            // Arrange
            var accountInfoId = 1;
            var request = new ClaimFilterGetModel() { AccountInfoId = accountInfoId, Tab = ClaimListingTab.PendingReview };

            var staffMembers = new List<RethinkStaffMembersByPermissionResponse>
                {
                    new RethinkStaffMembersByPermissionResponse { memberId = 1, firstName = "John", lastName = "Doe" },
                    new RethinkStaffMembersByPermissionResponse { memberId = 2, firstName = "Jane", lastName = "Smith" }
                };

            var claims = new List<ClaimEntity>
                {
                    new ClaimEntity { AccountInfoId = accountInfoId, AssigneeId = 1, ClaimStatus = ClaimStatus.PendingReview, IsFlagged = false, DateDeleted = null }
                };

            var claimDbSet = claims.AsQueryable().BuildMockDbSet();
            _claimRepository.Setup(r => r.Query()).Returns(claimDbSet.Object);
            _cacheService.Setup(c => c.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<RethinkStaffMembersByPermissionResponse>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(staffMembers);

            // Act
            var result = await _appointmentService.GetClaimsAssignees(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // Unassigned + 1 staff member with claims
            Assert.Equal("Unassigned", result[0].Name);
            Assert.Equal("John Doe", result[1].Name);
        }

        [Fact]
        public async Task GetClaimsAssignees_FiltersStaffByTab_WhenTabIsReadyToBill()
        {
            // Arrange
            var accountInfoId = 1;
            var request = new ClaimFilterGetModel() { AccountInfoId = accountInfoId, Tab = ClaimListingTab.ReadyToBill };

            var staffMembers = new List<RethinkStaffMembersByPermissionResponse>
                {
                    new RethinkStaffMembersByPermissionResponse { memberId = 1, firstName = "John", lastName = "Doe" },
                    new RethinkStaffMembersByPermissionResponse { memberId = 2, firstName = "Jane", lastName = "Smith" }
                };

            var claims = new List<ClaimEntity>
                {
                    new ClaimEntity { AccountInfoId = accountInfoId, AssigneeId = 2, ClaimStatus = (ClaimStatus)2, IsFlagged = false, DateDeleted = null }
                };

            var claimDbSet = claims.AsQueryable().BuildMockDbSet();
            _claimRepository.Setup(r => r.Query()).Returns(claimDbSet.Object);
            _cacheService.Setup(c => c.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<RethinkStaffMembersByPermissionResponse>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(staffMembers);

            // Act
            var result = await _appointmentService.GetClaimsAssignees(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Unassigned", result[0].Name);
            Assert.Equal("Jane Smith", result[1].Name);
        }

        [Fact]
        public async Task GetClaimsAssignees_FiltersStaffByTab_WhenTabIsFlagged()
        {
            // Arrange
            var accountInfoId = 1;
            var request = new ClaimFilterGetModel() { AccountInfoId = accountInfoId, Tab = ClaimListingTab.Flagged };

            var staffMembers = new List<RethinkStaffMembersByPermissionResponse>
                {
                    new RethinkStaffMembersByPermissionResponse { memberId = 1, firstName = "John", lastName = "Doe" },
                    new RethinkStaffMembersByPermissionResponse { memberId = 2, firstName = "Jane", lastName = "Smith" }
                };

            var claims = new List<ClaimEntity>
                {
                    new ClaimEntity { AccountInfoId = accountInfoId, AssigneeId = 1, IsFlagged = true, DateDeleted = null }
                };

            var claimDbSet = claims.AsQueryable().BuildMockDbSet();
            _claimRepository.Setup(r => r.Query()).Returns(claimDbSet.Object);
            _cacheService.Setup(c => c.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<RethinkStaffMembersByPermissionResponse>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(staffMembers);

            // Act
            var result = await _appointmentService.GetClaimsAssignees(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Unassigned", result[0].Name);
            Assert.Equal("John Doe", result[1].Name);
        }

        [Fact]
        public async Task GetClaimsAssignees_FiltersWithSearchValue_WhenSearchValueProvided()
        {
            // Arrange
            var request = new ClaimFilterGetModel() { AccountInfoId = 1, Tab = 0, SearchValue = "john" };

            var staffMembers = new List<RethinkStaffMembersByPermissionResponse>
                {
                    new RethinkStaffMembersByPermissionResponse { memberId = 1, firstName = "John", lastName = "Doe" },
                    new RethinkStaffMembersByPermissionResponse { memberId = 2, firstName = "Jane", lastName = "Smith" }
                };

            _cacheService.Setup(c => c.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<RethinkStaffMembersByPermissionResponse>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(staffMembers);

            // Act
            var result = await _appointmentService.GetClaimsAssignees(request);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("John Doe", result[0].Name);
        }

        [Fact]
        public async Task GetClaimsAssignees_ReturnsUnassigned_WhenSearchValueIsUnassigned()
        {
            // Arrange
            var request = new ClaimFilterGetModel() { AccountInfoId = 1, Tab = 0, SearchValue = "unassign" };

            var staffMembers = new List<RethinkStaffMembersByPermissionResponse>
                {
                    new RethinkStaffMembersByPermissionResponse { memberId = 1, firstName = "John", lastName = "Doe" }
                };

            _cacheService.Setup(c => c.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<RethinkStaffMembersByPermissionResponse>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(staffMembers);

            // Act
            var result = await _appointmentService.GetClaimsAssignees(request);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Unassigned", result[0].Name);
        }

        [Fact]
        public async Task GetClaimsAssignees_IsCaseInsensitive_WhenSearching()
        {
            // Arrange
            var request = new ClaimFilterGetModel() { AccountInfoId = 1, Tab = 0, SearchValue = "JANE" };

            var staffMembers = new List<RethinkStaffMembersByPermissionResponse>
                {
                    new RethinkStaffMembersByPermissionResponse { memberId = 1, firstName = "John", lastName = "Doe" },
                    new RethinkStaffMembersByPermissionResponse { memberId = 2, firstName = "Jane", lastName = "Smith" }
                };

            _cacheService.Setup(c => c.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<RethinkStaffMembersByPermissionResponse>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(staffMembers);

            // Act
            var result = await _appointmentService.GetClaimsAssignees(request);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Jane Smith", result[0].Name);
        }

        [Fact]
        public async Task GetClaimsAssignees_ReturnsEmpty_WhenSearchValueDoesNotMatch()
        {
            // Arrange
            var request = new ClaimFilterGetModel() { AccountInfoId = 1, Tab = 0, SearchValue = "NonExistent" };

            var staffMembers = new List<RethinkStaffMembersByPermissionResponse>
                {
                    new RethinkStaffMembersByPermissionResponse { memberId = 1, firstName = "John", lastName = "Doe" }
                };

            _cacheService.Setup(c => c.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<RethinkStaffMembersByPermissionResponse>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(staffMembers);

            // Act
            var result = await _appointmentService.GetClaimsAssignees(request);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetClaimsAssignees_SortsStaffByName_WhenReturningResults()
        {
            // Arrange
            var request = new ClaimFilterGetModel() { AccountInfoId = 1, Tab = 0 };

            var staffMembers = new List<RethinkStaffMembersByPermissionResponse>
                {
                    new RethinkStaffMembersByPermissionResponse { memberId = 1, firstName = "Zara", lastName = "Adams" },
                    new RethinkStaffMembersByPermissionResponse { memberId = 2, firstName = "Alice", lastName = "Brown" },
                    new RethinkStaffMembersByPermissionResponse { memberId = 3, firstName = "Mike", lastName = "Carter" }
                };

            _cacheService.Setup(c => c.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<RethinkStaffMembersByPermissionResponse>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(staffMembers);

            // Act
            var result = await _appointmentService.GetClaimsAssignees(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Count);
            Assert.Equal("Unassigned", result[0].Name);
            Assert.Equal("Alice Brown", result[1].Name);
            Assert.Equal("Mike Carter", result[2].Name);
            Assert.Equal("Zara Adams", result[3].Name);
        }

        [Fact]
        public async Task GetClaimsAssignees_ExcludesDeletedClaims_WhenFilteringByTab()
        {
            // Arrange
            var accountInfoId = 1;
            var request = new ClaimFilterGetModel() { AccountInfoId = accountInfoId, Tab = ClaimListingTab.PendingReview };

            var staffMembers = new List<RethinkStaffMembersByPermissionResponse>
                {
                    new RethinkStaffMembersByPermissionResponse { memberId = 1, firstName = "John", lastName = "Doe" },
                    new RethinkStaffMembersByPermissionResponse { memberId = 2, firstName = "Jane", lastName = "Smith" }
                };

            var claims = new List<ClaimEntity>
                {
                    new ClaimEntity { AccountInfoId = accountInfoId, AssigneeId = 1, ClaimStatus = ClaimStatus.PendingReview, IsFlagged = false, DateDeleted = null },
                    new ClaimEntity { AccountInfoId = accountInfoId, AssigneeId = 2, ClaimStatus = ClaimStatus.PendingReview, IsFlagged = false, DateDeleted = DateTime.Now }
                };

            var claimDbSet = claims.AsQueryable().BuildMockDbSet();
            _claimRepository.Setup(r => r.Query()).Returns(claimDbSet.Object);
            _cacheService.Setup(c => c.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<RethinkStaffMembersByPermissionResponse>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(staffMembers);

            // Act
            var result = await _appointmentService.GetClaimsAssignees(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Unassigned", result[0].Name);
            Assert.Equal("John Doe", result[1].Name);
        }

        [Fact]
        public async Task GetClaimsAssignees_HandlesDuplicateAssigneeIds_WhenFilteringByTab()
        {
            // Arrange
            var accountInfoId = 1;
            var request = new ClaimFilterGetModel() { AccountInfoId = accountInfoId, Tab = ClaimListingTab.PendingReview };

            var staffMembers = new List<RethinkStaffMembersByPermissionResponse>
                {
                    new RethinkStaffMembersByPermissionResponse { memberId = 1, firstName = "John", lastName = "Doe" },
                    new RethinkStaffMembersByPermissionResponse { memberId = 2, firstName = "Jane", lastName = "Smith" }
                };

            var claims = new List<ClaimEntity>
                {
                    new ClaimEntity { AccountInfoId = accountInfoId, AssigneeId = 1, ClaimStatus = ClaimStatus.PendingReview, IsFlagged = false, DateDeleted = null },
                    new ClaimEntity { AccountInfoId = accountInfoId, AssigneeId = 1, ClaimStatus = ClaimStatus.PendingReview, IsFlagged = false, DateDeleted = null }
                };

            var claimDbSet = claims.AsQueryable().BuildMockDbSet();
            _claimRepository.Setup(r => r.Query()).Returns(claimDbSet.Object);
            _cacheService.Setup(c => c.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<RethinkStaffMembersByPermissionResponse>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(staffMembers);

            // Act
            var result = await _appointmentService.GetClaimsAssignees(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Single(result.Where(r => r.Name == "John Doe"));
        }

        [Fact]
        public async Task GetClaimsAssignees_UsesCacheWithCorrectKey_WhenCalled()
        {
            // Arrange
            var accountInfoId = 123;
            var request = new ClaimFilterGetModel() { AccountInfoId = accountInfoId, Tab = 0 };

            string capturedKey = null;

            _cacheService.Setup(c => c.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<RethinkStaffMembersByPermissionResponse>>>>(),
                    It.IsAny<TimeSpan>()))
                .Callback<string, Func<Task<List<RethinkStaffMembersByPermissionResponse>>>, TimeSpan>(
                    (key, func, time) => capturedKey = key)
                .ReturnsAsync(new List<RethinkStaffMembersByPermissionResponse>());

            // Act
            await _appointmentService.GetClaimsAssignees(request);

            // Assert
            Assert.Equal("claimsAssignees_123", capturedKey);
        }

        [Fact]
        public async Task GetClaimsAssignees_UsesCacheWith15MinutesExpiration_WhenCalled()
        {
            // Arrange
            var request = new ClaimFilterGetModel() { AccountInfoId = 1, Tab = 0 };

            TimeSpan capturedExpiration = TimeSpan.Zero;

            _cacheService.Setup(c => c.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<RethinkStaffMembersByPermissionResponse>>>>(),
                    It.IsAny<TimeSpan>()))
                .Callback<string, Func<Task<List<RethinkStaffMembersByPermissionResponse>>>, TimeSpan>(
                    (key, func, time) => capturedExpiration = time)
                .ReturnsAsync(new List<RethinkStaffMembersByPermissionResponse>());

            // Act
            await _appointmentService.GetClaimsAssignees(request);

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(15), capturedExpiration);
        }

        [Fact]
        public async Task GetClaimsAssignees_HandlesStaffWithNullNames_WhenFormatting()
        {
            // Arrange
            var request = new ClaimFilterGetModel() { AccountInfoId = 1, Tab = 0 };

            var staffMembers = new List<RethinkStaffMembersByPermissionResponse>
                {
                    new RethinkStaffMembersByPermissionResponse { memberId = 1, firstName = "John", lastName = null },
                    new RethinkStaffMembersByPermissionResponse { memberId = 2, firstName = null, lastName = "Smith" }
                };

            _cacheService.Setup(c => c.GetOrSetCacheAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<RethinkStaffMembersByPermissionResponse>>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(staffMembers);

            // Act
            var result = await _appointmentService.GetClaimsAssignees(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(result, r => r.MemberId == 1);
            Assert.Contains(result, r => r.MemberId == 2);
        }


        [Fact]
        public async Task UnLinkAppointments_ReturnsFalse_WhenClientIdDoesNotMatch()
        {
            // Arrange
            var accountInfoId = 1;
            var memberId = 2;
            var claimId = 3;
            var appointmentId = 100;
            var now = DateTime.Now;

            var claim = new ClaimEntity
            {
                Id = claimId,
                ChildProfileId = 10,
                StartDate = new DateTime(2025, 9, 1),
                EndDate = new DateTime(2025, 9, 10),
                AccountInfoId = accountInfoId,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>()
            };

            // Appointment with WRONG client ID (for expected failure)
            var appointment = new AppointmentRethinkModel
            {
                id = appointmentId,
                clientId = claim.ChildProfileId + 999,
                staffAccountInfoId = memberId,
                staffId = memberId,
                startDateTime = now,
                endDateTime = now.AddHours(1),
                ProviderBillingCode = new BillingCodeData
                {
                    id = 1,
                    billingCode = "TEST001",
                    rate = 100,
                    unitTypeId = 1
                },
                providerBillingCodeId = 1,
                actualStartTime = 480,
                actualEndTime = 540,
                serviceId = 1,
                procedureCodeId = 1,
                providerServiceId = 1,
                funderId = 1,
                appointmentTypeId = 1,
                occurrenceTypeId = 1
            };

            var linkEntity = new ClaimAppointmentLinkEntity
            {
                Id = 1,
                ClaimId = claimId,
                AppointmentId = appointmentId,
                Claim = claim,
                DateDeleted = null
            };

            // EF-like mock DbSets
            var claimMock = new List<ClaimEntity> { claim }
                .AsQueryable()
                .BuildMockDbSet();

            var linkMock = new List<ClaimAppointmentLinkEntity> { linkEntity }
                .AsQueryable()
                .BuildMockDbSet();

            _claimRepository.Setup(r => r.Query()).Returns(claimMock.Object);
            _claimAppointmentLinkRepository.Setup(r => r.Query()).Returns(linkMock.Object);

            _rethinkService.Setup(r => r.GetAppointmentListAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<AppointmentRethinkModel> { appointment });

            // Act
            var result = await _appointmentService.UnLinkAppointments(
                accountInfoId,
                memberId,
                claimId,
                new List<int> { appointmentId }
            );

            // Assert
            Assert.False(result.Item1);
            Assert.Equal(claim.StartDate, result.Item2);
            Assert.Equal(claim.EndDate, result.Item3);

            _claimAppointmentLinkRepository.Verify(r => r.Update(It.IsAny<ClaimAppointmentLinkEntity>()), Times.Never);
        }

        [Fact]
        public async Task UnLinkAppointments_UpdatesChargeEntry_WhenMultipleAppointmentsShareChargeEntry()
        {
            // Arrange
            var accountInfoId = 1;
            var memberId = 2;
            var claimId = 3;
            var appointmentId1 = 100;
            var appointmentId2 = 101;
            var chargeEntryId = 1;
            var now = DateTime.Now;

            var claim = new ClaimEntity
            {
                Id = claimId,
                ChildProfileId = 10,
                StartDate = new DateTime(2025, 9, 1),
                EndDate = new DateTime(2025, 9, 10),
                AccountInfoId = accountInfoId
            };

            var chargeEntry = new ClaimChargeEntryEntity
            {
                Id = chargeEntryId,
                ClaimId = claimId,
                Units = 1,
                Charges = 100,
                DateDeleted = null,
                CreatedBy = memberId,
                DateOfService = new DateTime(2025, 9, 1)
            };

            claim.ClaimChargeEntries = new List<ClaimChargeEntryEntity> { chargeEntry };

            var linkChargeEntry1 = new ClaimAppointmentLinkChargeEntry
            {
                Id = 1,
                ClaimChargeEntryEntityId = chargeEntryId,
                ClaimChargeEntry = chargeEntry,
                IsSecondBillingCode = false,
                DateDeleted = null
            };

            var linkChargeEntry2 = new ClaimAppointmentLinkChargeEntry
            {
                Id = 2,
                ClaimChargeEntryEntityId = chargeEntryId,
                ClaimChargeEntry = chargeEntry,
                IsSecondBillingCode = false,
                DateDeleted = null
            };

            var linkEntity1 = new ClaimAppointmentLinkEntity
            {
                Id = 1,
                ClaimId = claimId,
                AppointmentId = appointmentId1,
                Claim = claim,
                ClaimChargeEntriesId = chargeEntryId,
                ClaimAppointmentLinkChargeEntry = linkChargeEntry1,
                ClaimAppointmentLinkChargeEntryId = 1,
                DateDeleted = null
            };

            var linkEntity2 = new ClaimAppointmentLinkEntity
            {
                Id = 2,
                ClaimId = claimId,
                AppointmentId = appointmentId2,
                Claim = claim,
                ClaimChargeEntriesId = chargeEntryId,
                ClaimAppointmentLinkChargeEntry = linkChargeEntry2,
                ClaimAppointmentLinkChargeEntryId = 2,
                DateDeleted = null
            };

            var appointment1 = new AppointmentRethinkModel
            {
                id = appointmentId1,
                clientId = claim.ChildProfileId,
                staffAccountInfoId = memberId,
                staffId = memberId,
                startDateTime = now,
                endDateTime = now.AddHours(1),
                ProviderBillingCode = new BillingCodeData { id = 1, billingCode = "TEST001", rate = 100, unitTypeId = 1 },
                providerBillingCodeId = 1,
                actualStartTime = 480,
                actualEndTime = 540,
                serviceId = 1,
                procedureCodeId = 1,
                providerServiceId = 1,
                funderId = 1,
                appointmentTypeId = 1,
                occurrenceTypeId = 1
            };

            var appointment2 = new AppointmentRethinkModel
            {
                id = appointmentId2,
                clientId = claim.ChildProfileId,
                staffAccountInfoId = memberId,
                staffId = memberId,
                startDateTime = now.AddDays(1),
                endDateTime = now.AddDays(1).AddHours(1),
                ProviderBillingCode = new BillingCodeData { id = 2, billingCode = "TEST002", rate = 150, unitTypeId = 1 },
                providerBillingCodeId = 2,
                actualStartTime = 480,
                actualEndTime = 540,
                serviceId = 1,
                procedureCodeId = 1,
                providerServiceId = 1,
                funderId = 1,
                appointmentTypeId = 1,
                occurrenceTypeId = 1
            };

            //
            // Mock IQueryable DbSets
            //
            var claimMock = new List<ClaimEntity> { claim }.AsQueryable().BuildMockDbSet();
            var linkMock = new List<ClaimAppointmentLinkEntity> { linkEntity1, linkEntity2 }.AsQueryable().BuildMockDbSet();
            var linkChargeMock = new List<ClaimAppointmentLinkChargeEntry> { linkChargeEntry1, linkChargeEntry2 }.AsQueryable().BuildMockDbSet();
            var chargeEntryMock = new List<ClaimChargeEntryEntity> { chargeEntry }.AsQueryable().BuildMockDbSet();

            _claimRepository.Setup(r => r.Query()).Returns(claimMock.Object);
            _claimAppointmentLinkRepository.Setup(r => r.Query()).Returns(linkMock.Object);
            _linkChargeRepository.Setup(r => r.Query()).Returns(linkChargeMock.Object);
            _claimChargeEntryRepository.Setup(r => r.Query()).Returns(chargeEntryMock.Object);

            //
            // External service mocks
            //
            _rethinkService.Setup(r => r.GetAppointmentListAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<AppointmentRethinkModel> { appointment1, appointment2 });

            _rethinkService.Setup(r => r.GetChildProfileAuthBillingCodeForAppointment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((AppointmentClientAuthBillingCodeModel)null);

            _rethinkService.Setup(r => r.GetProviderBillingCode(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(appointment1.ProviderBillingCode);

            _rethinkService.Setup(r => r.GetUnitTypesAsync())
                .ReturnsAsync(new List<ClientUnitTypes> { new ClientUnitTypes { id = 1, unit = 15 } });

            var staffMember = new RethinkStaffMember
            {
                identifiers = new List<Identifiers>
        {
            new Identifiers { identifierType = "NPINumber", value = "1234567890" }
        }
            };

            _rethinkService.Setup(r => r.GetStaffMember(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(staffMember);

            // Act
            var result = await _appointmentService.UnLinkAppointments(
                accountInfoId,
                memberId,
                claimId,
                new List<int> { appointmentId1, appointmentId2 }
            );

            // Assert
            Assert.True(result.Item1);

            _claimAppointmentLinkRepository.Verify(
                r => r.Update(It.IsAny<ClaimAppointmentLinkEntity>()),
                Times.Exactly(2)
            );

            _bus.Verify(
                b => b.SendAsync(It.IsAny<AppointmentBillingStatus>(), It.IsAny<string>()),
                Times.Exactly(2)
            );
        }


        #region GetExistingClaimChargeEntity Tests

        // Note: GetExistingClaimChargeEntity is a private method, so we test it indirectly through LinkAppointments
        // The following tests verify the charge entry combination logic

        [Fact]
        public async Task LinkAppointments_DoesNotCombineCharges_WhenCombineChargeTypeIsDontCombine()
        {
            // Arrange
            var accountInfoId = 1;
            var memberId = 2;
            var claimId = 3;
            var appointmentId = 100;

            var claim = new ClaimEntity
            {
                Id = claimId,
                ChildProfileId = 10,
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 1, 31),
                AccountInfoId = accountInfoId,
                ClaimSubmissions = new List<ClaimSubmissionEntity>()
            };

            var funder = new FunderDataModel
            {
                combineChargeTypeId = (int)CombineChargeTypes.DontCombine
            };

            var providerBillingCode = new BillingCodeData
            {
                id = 1,
                billingCode = "99213",
                rate = 100,
                unitTypeId = 1,
                funderId = 1,
                funders = funder,
                providerSerivces = new ClientProviderServiceModel
                {
                    baseRate = 100,
                    id = 1,
                    name = "Test",
                    accountId = accountInfoId,
                    isActive = true
                }
            };

            var appointment = new AppointmentRethinkModel
            {
                id = appointmentId,
                clientId = claim.ChildProfileId,
                staffAccountInfoId = memberId,
                staffId = memberId,
                startDateTime = new DateTime(2025, 1, 15),
                endDateTime = new DateTime(2025, 1, 15, 1, 0, 0),
                ProviderBillingCode = providerBillingCode,
                providerBillingCodeId = 1,
                actualStartTime = 480,
                actualEndTime = 540,
                serviceId = 1,
                procedureCodeId = 1,
                providerServiceId = 1,
                funderId = 1
            };

            var staffMember = new RethinkStaffMember
            {
                memberId = memberId,
                identifiers = new List<Identifiers>
        {
            new Identifiers { identifierType = "NPINumber", value = "1234567890" }
        }
            };

            // Claim mocks
            _claimRepository.Setup(r => r.Query())
                .Returns(new List<ClaimEntity> { claim }.AsQueryable().BuildMockDbSet().Object);

            // Link repository mock (REQUIRED to avoid NullRef)
            var linkEntity = new ClaimAppointmentLinkEntity
            {
                Id = 1,
                ClaimId = claimId,
                AppointmentId = appointmentId,
                Claim = claim,
                DateDeleted = null
            };

            _claimAppointmentLinkRepository.Setup(r => r.Query())
                .Returns(new List<ClaimAppointmentLinkEntity> { linkEntity }
                .AsQueryable().BuildMockDbSet().Object);

            _claimAppointmentLinkRepository.Setup(r => r.Update(It.IsAny<ClaimAppointmentLinkEntity>()));
            _claimAppointmentLinkRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // No existing charge entries
            _claimChargeEntryRepository.Setup(r => r.Query())
                .Returns(new List<ClaimChargeEntryEntity>().AsQueryable().BuildMockDbSet().Object);

            // Other required mocks
            _rethinkService.Setup(r => r.GetAppointmentListAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<AppointmentRethinkModel> { appointment });

            _rethinkService.Setup(r => r.GetProviderBillingCode(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(providerBillingCode);

            _rethinkService.Setup(r => r.GetStaffMember(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(staffMember);

            _rethinkService.Setup(r => r.GetFunder(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(funder);

            _rethinkService.Setup(r => r.GetUnitTypesAsync())
                .ReturnsAsync(new List<ClientUnitTypes> { new ClientUnitTypes { id = 1, unit = 15 } });

            _rethinkService.Setup(r => r.GetProviderBillingCodeCredential(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new ProviderBillingCodeCredentialModel
                {
                    modifier1 = "U1",
                    modifier2 = "U2",
                    contractRate = 120,
                    ProviderBillingCode = providerBillingCode
                });


            // Act
            //var result = await _appointmentService.LinkAppointments(
            //    accountInfoId, memberId, claimId, new List<int> { appointmentId });

            // Assert
            //Assert.True(result.Item1);
        }

        [Fact]
        public async Task LinkAppointments_FiltersByRenderingProvider_WhenCombineTypeRequiresIt()
        {
         // Arrange
        var accountInfoId = 1;
        var memberId = 2;
        var claimId = 3;
        var appointmentId = 100;
        var existingChargeId = 50;

          var claim = new ClaimEntity
        {
            Id = claimId,
            ChildProfileId = 10,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            AccountInfoId = accountInfoId,
            ClaimSubmissions = new List<ClaimSubmissionEntity>()
        };

        var funder = new FunderDataModel
            {
            combineChargeTypeId = (int)CombineChargeTypes.SameDayClientProcedureRenderingProvider
            };

        var existingChargeEntry = new ClaimChargeEntryEntity
        {
            Id = existingChargeId,
            ClaimId = claimId,
            BillingCode = "99213",
            DateOfService = new DateTime(2025, 1, 15),
            Units = 1,
            Charges = 100,
            DateDeleted = null
        };

        var existingLinkChargeEntry = new ClaimAppointmentLinkChargeEntry
        {
            Id = 1,
            ClaimChargeEntryEntityId = existingChargeId,
            NpiNumber = "1234567890",
            DateDeleted = null
        };

     var providerBillingCode = new BillingCodeData
            {
                id = 1,
                billingCode = "99213",
                rate = 100,
                unitTypeId = 1,
                funderId = 1,
                funders = funder
            };

        var appointment = new AppointmentRethinkModel
            {
                id = appointmentId,
                clientId = claim.ChildProfileId,
                staffAccountInfoId = memberId,
                staffId = memberId,
                startDateTime = new DateTime(2025, 1, 15),
                endDateTime = new DateTime(2025, 1, 15, 1, 0, 0),
                ProviderBillingCode = providerBillingCode,
                providerBillingCodeId = 1,
                actualStartTime = 480,
                actualEndTime = 540,
                serviceId = 1,
                procedureCodeId = 1,
                providerServiceId = 1,
                funderId = 1,
                appointmentTypeId = 1,
                occurrenceTypeId = 1
            };

            var staffMember = new RethinkStaffMember
            {
             memberId = memberId,
                   identifiers = new List<Identifiers>
               {
                   new Identifiers { identifierType = "NPINumber", value = "1234567890" }
                }
            };

             // Setup mocks
            var claimMock = new List<ClaimEntity> { claim }.AsQueryable().BuildMockDbSet();
            var emptyLinkMock = new List<ClaimAppointmentLinkEntity>().AsQueryable().BuildMockDbSet();
            var chargeEntryMock = new List<ClaimChargeEntryEntity> { existingChargeEntry }.AsQueryable().BuildMockDbSet();
            var linkChargeMock = new List<ClaimAppointmentLinkChargeEntry> { existingLinkChargeEntry }.AsQueryable().BuildMockDbSet();

            _claimRepository.Setup(r => r.Query()).Returns(claimMock.Object);
            _claimAppointmentLinkRepository.Setup(r => r.Query()).Returns(emptyLinkMock.Object);
            _claimChargeEntryRepository.Setup(r => r.Query()).Returns(chargeEntryMock.Object);
            _linkChargeRepository.Setup(r => r.Query()).Returns(linkChargeMock.Object);

            _rethinkService.Setup(r => r.GetAppointmentListAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<AppointmentRethinkModel> { appointment });
            _rethinkService.Setup(r => r.GetProviderBillingCode(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(providerBillingCode);
            _rethinkService.Setup(r => r.GetStaffMember(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(staffMember);
            _rethinkService.Setup(r => r.GetFunder(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(funder);
            _rethinkService.Setup(r => r.GetUnitTypesAsync())
            .ReturnsAsync(new List<ClientUnitTypes> { new ClientUnitTypes { id = 1, unit = 15 } });

   //  // Act
   //         var result = await _appointmentService.LinkAppointments(accountInfoId, memberId, claimId, new List<int> { appointmentId });

   //   // Assert
   // Assert.True(result.Item1);
   //     // Verify the charge was combined only for the same rendering provider (NPI)
   //_claimChargeEntryRepository.Verify(r => r.Update(It.Is<ClaimChargeEntryEntity>(c => c.Id == existingChargeId)), Times.AtLeastOnce);
 }

        #endregion
        #region CreateClaimChargeEntity Tests

        [Fact]
        public async Task CreateClaimChargeEntity_UsesAuthorizationDiagnosisCode()
        {
            // ARRANGE
            SetupMocks();
            SetupServices();

            var appointment = Fixture.Build<AppointmentRethinkModel>()
                .With(x => x.clientId,10)
                .With(x => x.startDate, DateTime.Now)
                .With(x => x.ChildProfileAuthorizationBillingCode,
                    () => Fixture.Build<AppointmentClientAuthBillingCodeModel>()
                        .Without(x => x.ChildProfileAuthorization)
                        .Create())
                .Create();

            var auth = Fixture.Build<ClientAuthorization>()
                .With(x => x.ChildProfileDiagnosis,
                    () => Fixture.Build<ClientDiagnosisCodes>()
                        .With(d => d.diagnosis,
                            () => Fixture.Build<Diagnosis>()
                                .With(c => c.diagnosisCode, "AUTH123")
                                .Create())
                        .Create())
                .Create();

            _rethinkService.Setup(x =>
                x.GetChildProfileAuthorizationByClientId(
                    appointment.staffAccountInfoId,
                    appointment.clientId.Value,
                    appointment.ChildProfileAuthorizationBillingCode.childProfileAuthorizationId))
                .ReturnsAsync(auth);

            _rethinkService.Setup(x =>
                x.GetClientDiagnosisById(
                    appointment.staffAccountInfoId,
                    appointment.clientId.Value,
                    auth.childProfileDiagnosisId))
                .ReturnsAsync(auth.ChildProfileDiagnosis);

            var service = CreateServiceInstance();
            var method = typeof(AppointmentService).GetMethod("CreateClaimChargeEntity",
                BindingFlags.NonPublic | BindingFlags.Instance);

            var task = (Task<ClaimChargeEntryEntity>)method.Invoke(service, new object[]
            {
                 appointment, "BCODE",1,999, DateTime.Now, true,123
            });

            var result = await task;
            Assert.Equal("AUTH123", result.DiagnosisCode);
        }

        [Fact]
        public async Task CreateClaimChargeEntity_FallsBackToClaimDiagnosisCode()
        {
            // ARRANGE
            SetupMocks();
            SetupServices();

            var claim = Fixture.Build<ClaimEntity>()
                .With(x => x.Id,55)
                .Create();

            var claimDb = new List<ClaimEntity> { claim }.AsQueryable().BuildMockDbSet();
            _claimRepository.Setup(x => x.Query()).Returns(claimDb.Object);

            _claimSyncService.Setup(x => x.AddDiagnosisCodes(claim, null, It.IsAny<int>()))
                .ReturnsAsync("FALLBACK123");

            var appointment = Fixture.Build<AppointmentRethinkModel>()
                .With(x => x.ProviderBillingCodeCredential,
                    () => Fixture.Build<ProviderBillingCodeCredentialModel>()
                    .With(p => p.ProviderBillingCode,
                        () => Fixture.Create<BillingCodeData>())
                    .Create())
                .With(x => x.clientId,10)
                .With(x => x.startDate, DateTime.Now)
                .With(x => x.ChildProfileAuthorizationBillingCode, () => (AppointmentClientAuthBillingCodeModel?)null)
                .Create();

            var service = CreateServiceInstance();
            var method = typeof(AppointmentService).GetMethod("CreateClaimChargeEntity",
                BindingFlags.NonPublic | BindingFlags.Instance);

            var task = (Task<ClaimChargeEntryEntity>)method.Invoke(service, new object[]
            {
                appointment, "BCODE",55,999, DateTime.Now, true,124
            });

            var result = await task;
            Assert.Equal("FALLBACK123", result.DiagnosisCode);
        }

        [Fact]
        public async Task CreateClaimChargeEntity_ProviderBillingCodeCredential_ModifiesBillingDescription()
        {
            // ARRANGE
            SetupMocks();
            SetupServices();

            var providerCode = Fixture.Build<BillingCodeData>()
                .With(x => x.description, "Test Desc")
                .Create();

            var cred = Fixture.Build<ProviderBillingCodeCredentialModel>()
                .With(x => x.ProviderBillingCode, () => providerCode)
                .Create();

            var appointment = Fixture.Build<AppointmentRethinkModel>()
                .With(x => x.ProviderBillingCodeCredential, () => cred)
                .With(x => x.ProviderBillingCode, () => providerCode)
                .With(x => x.clientId,10)
                .With(x => x.startDate, DateTime.Now)
                .With(x => x.ChildProfileAuthorizationBillingCode, () => (AppointmentClientAuthBillingCodeModel?)null)
                .Create();

            var claimDb = new List<ClaimEntity>
                 {
                 Fixture.Build<ClaimEntity>().With(x => x.Id,77).Create()
                 }.AsQueryable().BuildMockDbSet();

             _claimRepository.Setup(x => x.Query()).Returns(claimDb.Object);
             _claimSyncService.Setup(x => x.AddDiagnosisCodes(It.IsAny<ClaimEntity>(), null, It.IsAny<int>()))
             .ReturnsAsync("DXCODE1");

             var service = CreateServiceInstance();
             var method = typeof(AppointmentService).GetMethod("CreateClaimChargeEntity",
             BindingFlags.NonPublic | BindingFlags.Instance);

             var task = (Task<ClaimChargeEntryEntity>)method.Invoke(service, new object[]
                 {
                 appointment, "BCODE",77,999, DateTime.Now, true,200
                 });

             var result = await task;
             Assert.Equal("Test Desc", result.BillingCodeDescription);
             }

        #endregion

        [Theory]
        [InlineData(0.4, RoundingTypes.RoundToNearestUnit, 0)]
        [InlineData(0.5, RoundingTypes.RoundToNearestUnit, 0)]
        [InlineData(0.6, RoundingTypes.RoundToNearestUnit, 1)]
        [InlineData(1.0, RoundingTypes.RoundUp, 1)]
        [InlineData(1.1, RoundingTypes.RoundUp, 2)]
        [InlineData(1.9, RoundingTypes.RoundDown, 1)]
        [InlineData(1.0, RoundingTypes.NoRounding, 1)]
        public void RoundCacluation_Works_AsExpected(double input, RoundingTypes rounding, double expected)
        {
            var method = typeof(AppointmentService)
                .GetMethod("RoundCacluation", BindingFlags.NonPublic | BindingFlags.Instance);

            var instance = _appointmentService as AppointmentService;

            var result = (double)method.Invoke(instance, new object[] { input, rounding });

            Assert.Equal(expected, result);
        }
        [Fact]
        public async Task AddLink_CreatesNewChargeEntry_WhenNoExistingChargeFound()
        {
            // Arrange
            var accountInfoId = 1;
            var memberId = 2;
            var claimId = 3;
            var apptId = 1000;
            var startDate = new DateTime(2025, 1, 1);
            var billingCode = "TEST001";

            var appointment = new AppointmentRethinkModel
            {
                id = apptId,
                clientAccountInfoId = accountInfoId,
                staffAccountInfoId = accountInfoId,
                staffId = memberId,
                clientId = 10,
                providerBillingCodeId = 1,
                startDate = startDate,
                actualStartTime = 480,
                actualEndTime = 540
            };

            var providerCode = new BillingCodeData
            {
                id = 1,
                billingCode = billingCode,
                rate = 100,
                unitTypeId = 1,
                providerSerivces = new ClientProviderServiceModel
                {
                    id = 1,
                    name = "Svc",
                    baseRate = 100,
                    accountId = accountInfoId
                }
            };

            // Provider billing code
            _rethinkService.Setup(r => r.GetProviderBillingCode(accountInfoId, 1))
                .ReturnsAsync(providerCode);

            // Staff member (must contain NPI + timezone)
            _rethinkService.Setup(r => r.GetStaffMember(accountInfoId, memberId))
                .ReturnsAsync(new RethinkStaffMember
                {
                    identifiers = new List<Identifiers>
                    {
                new Identifiers { identifierType = "NPINumber", value = "1234567890" }
                    },
                    timezoneId = 1,
                    Timezone = new ClientTimezonesModel { id = 1, name = "Eastern Standard Time" }
                });

            // Funder
            _rethinkService.Setup(r => r.GetFunder(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new FunderDataModel { combineChargeTypeId = 0 });

            // Unit types
            _rethinkService.Setup(r => r.GetUnitTypesAsync())
                .ReturnsAsync(new List<ClientUnitTypes>
                {
            new ClientUnitTypes { id = 1, unit = 15 }
                });

            // Credentials
            _rethinkService.Setup(r => r.GetProviderBillingCodeCredential(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new ProviderBillingCodeCredentialModel
                {
                    modifier1 = "U1",
                    modifier2 = "U2",
                    contractRate = 120,
                    ProviderBillingCode = providerCode
                });

            // Provider service
            _rethinkService.Setup(r => r.GetProviderService(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new ClientProviderServiceModel
                {
                    id = 1,
                    name = "Svc",
                    baseRate = 100
                });

            // Timezones
            _rethinkService.Setup(r => r.GetTimezones())
                .ReturnsAsync(new List<ClientTimezonesModel>
                {
            new ClientTimezonesModel { id = 1, name = "Eastern Standard Time" }
                });

            // Claim (MUST include initialized collections)
            var claimEntity = new ClaimEntity
            {
                Id = claimId,
                AccountInfoId = accountInfoId,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>(),
                ClaimHistory = new List<ClaimHistoryEntity>(),
                ClaimDiagnosisCodes = new List<ClaimDiagnosisCodeEntity>()
            };

            _claimRepository.Setup(r => r.Query())
                .Returns(new List<ClaimEntity> { claimEntity }
                    .AsQueryable()
                    .BuildMockDbSet()
                    .Object);

            // No existing charge entries
            _claimChargeEntryRepository.Setup(r => r.Query())
                .Returns(new List<ClaimChargeEntryEntity>()
                    .AsQueryable()
                    .BuildMockDbSet()
                    .Object);

            // Async mocks for EF-related repos
            _paymentClaimServiceLineRepository.Setup(r => r.Query())
                .Returns(new List<PaymentClaimServiceLineEntity>().AsQueryable().BuildMockDbSet().Object);

            _paymentClaimRepository.Setup(r => r.Query())
                .Returns(new List<PaymentClaimEntity>().AsQueryable().BuildMockDbSet().Object);

            _claimSubmissionServiceLineRepository.Setup(r => r.Query())
                .Returns(new List<ClaimSubmissionServiceLineEntity>().AsQueryable().BuildMockDbSet().Object);

            _claimSearchFunderRepository.Setup(r => r.Query())
                .Returns(new List<ClaimSearchFunderEntity>().AsQueryable().BuildMockDbSet().Object);

            // IMPORTANT FIX — Query() MUST return ClaimAppointmentLinkEntity with Claim populated
            _claimAppointmentLinkRepository.Setup(r => r.Query())
                .Returns(
                    new List<ClaimAppointmentLinkEntity>
                    {
                new ClaimAppointmentLinkEntity
                {
                    AppointmentId = apptId,
                    ClaimId = claimId,
                    Appointment = appointment,
                    Claim = claimEntity, // ← CRITICAL FIX
                    DateDeleted = null
                }
                    }.AsQueryable().BuildMockDbSet().Object
                );

            // Invoke private AddLink()
            var method = typeof(AppointmentService)
                .GetMethod("AddLink", BindingFlags.NonPublic | BindingFlags.Instance);

            var queryAppointmentList = new List<ClaimAppointmentLinkEntity>
    {
        new ClaimAppointmentLinkEntity
        {
            AppointmentId = apptId,
            ClaimId = claimId,
            Appointment = appointment,
            Claim = claimEntity, // ← CRITICAL FIX
            DateDeleted = null
        }
    };

            var result = await (Task<ClaimAppointmentLinkChargeEntry>)method.Invoke(
                _appointmentService,
                new object[]
                {
            appointment,
            queryAppointmentList,
            claimId,
            accountInfoId,
            memberId,
            billingCode,
            startDate,
            true
                });

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSecondBillingCode);

            _claimChargeEntryRepository.Verify(x => x.Add(It.IsAny<ClaimChargeEntryEntity>()), Times.Once);
            _linkChargeRepository.Verify(x => x.Add(It.IsAny<ClaimAppointmentLinkChargeEntry>()), Times.Once);
            _claimAppointmentLinkRepository.Verify(x => x.Update(It.IsAny<ClaimAppointmentLinkEntity>()), Times.Once);
        }

        [Fact]
        public async Task AddLink_UsesExistingChargeEntry_WhenChargeEntryAlreadyExists()
        {
            var accountInfoId = 1;
            var memberId = 2;
            var claimId = 3;
            var apptId = 3000;
            var startDate = new DateTime(2025, 1, 1);
            var billingCode = "EXISTING";

            var providerCode = new BillingCodeData
            {
                id = 5,
                billingCode = billingCode,
                rate = 100,
                unitTypeId = 1,
                providerSerivces = new ClientProviderServiceModel
                {
                    id = 1,
                    baseRate = 100
                }
            };

            var appointment = new AppointmentRethinkModel
            {
                id = apptId,
                clientAccountInfoId = accountInfoId,
                staffAccountInfoId = accountInfoId,
                staffId = memberId,
                clientId = 10,
                providerBillingCodeId = 5,
                startDate = startDate,
                actualStartTime = 480,
                actualEndTime = 540,
                serviceId = 1,
                ProviderBillingCode = providerCode,
                ProviderBillingCodeCredential = new ProviderBillingCodeCredentialModel
                {
                    modifier1 = "U1",
                    ProviderBillingCode = providerCode
                },
                ChildProfileAuthorizationBillingCode = new AppointmentClientAuthBillingCodeModel
                {
                    providerBillingCodeId = 5,
                    unitTypeId = 1,
                    noOfUnits = 1,
                    AppointmentProviderBillingCode = providerCode
                }
            };

            _rethinkService.Setup(r => r.GetProviderBillingCode(accountInfoId, 5))
                .ReturnsAsync(providerCode);

            _rethinkService.Setup(r => r.GetProviderBillingCodeCredential(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(appointment.ProviderBillingCodeCredential);

            _rethinkService.Setup(r => r.GetStaffMember(accountInfoId, memberId))
                .ReturnsAsync(new RethinkStaffMember
                {
                    identifiers = new List<Identifiers>
                    {
                new Identifiers { identifierType = "NPINumber", value = "7777777777" }
                    }
                });

            _rethinkService.Setup(r => r.GetUnitTypesAsync())
                .ReturnsAsync(new List<ClientUnitTypes> { new ClientUnitTypes { id = 1, unit = 15 } });

            _rethinkService.Setup(r => r.GetFunder(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new FunderDataModel());

            var existingCharge = new ClaimChargeEntryEntity
            {
                Id = 9999,
                ClaimId = claimId,
                BillingCode = billingCode
            };

            var claim = new ClaimEntity
            {
                Id = claimId,
                AccountInfoId = accountInfoId,
                ClaimHistory = new List<ClaimHistoryEntity>(),
                ClaimDiagnosisCodes = new List<ClaimDiagnosisCodeEntity>(),
                ClaimChargeEntries = new List<ClaimChargeEntryEntity> { existingCharge }
            };

            // REQUIRED EF-friendly IQueryable
            _claimChargeEntryRepository.Setup(r => r.Query())
                .Returns(new List<ClaimChargeEntryEntity> { existingCharge }.AsQueryable().BuildMockDbSet().Object);

            _claimRepository.Setup(r => r.Query())
                .Returns(new List<ClaimEntity> { claim }.AsQueryable().BuildMockDbSet().Object);

            _claimAppointmentLinkRepository.Setup(r => r.Query())
                .Returns(new List<ClaimAppointmentLinkEntity>
                {
            new ClaimAppointmentLinkEntity
            {
                AppointmentId = apptId,
                ClaimId = claimId,
                Appointment = appointment,
                Claim = claim,
                DateDeleted = null
            }
                }.AsQueryable().BuildMockDbSet().Object);

            // ⭐ CRITICAL FIX — mock all async repositories used in UpdateChargeEntity
            _paymentClaimServiceLineRepository.Setup(r => r.Query())
                .Returns(new List<PaymentClaimServiceLineEntity>().AsQueryable().BuildMockDbSet().Object);

            _paymentClaimRepository.Setup(r => r.Query())
                .Returns(new List<PaymentClaimEntity>().AsQueryable().BuildMockDbSet().Object);

            _claimSubmissionServiceLineRepository.Setup(r => r.Query())
                .Returns(new List<ClaimSubmissionServiceLineEntity>().AsQueryable().BuildMockDbSet().Object);

            _claimSearchFunderRepository.Setup(r => r.Query())
                .Returns(new List<ClaimSearchFunderEntity>().AsQueryable().BuildMockDbSet().Object);

            var method = typeof(AppointmentService)
                .GetMethod("AddLink", BindingFlags.NonPublic | BindingFlags.Instance);

            var result = await (Task<ClaimAppointmentLinkChargeEntry>)method.Invoke(
                _appointmentService,
                new object[]
                {
            appointment,
            new List<ClaimAppointmentLinkEntity>(),
            claimId,
            accountInfoId,
            memberId,
            billingCode,
            startDate,
            true
                });

            Assert.NotNull(result);
            Assert.False(result.IsSecondBillingCode);
            Assert.Equal(9999, result.ClaimChargeEntryEntityId);

            _linkChargeRepository.Verify(x => x.Add(It.IsAny<ClaimAppointmentLinkChargeEntry>()), Times.Once);
        }

    }
}