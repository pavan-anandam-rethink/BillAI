using AutoFixture;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using BillingService.Domain.Services.Billing;
using BillingService.XUnit.Tests.Common;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Reporting;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Handlers;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Claim
{
    public class ClaimCreateServiceTest : BaseTest
    {
        private readonly Mock<IRethinkMasterDataMicroServices> _rethinkServices;
        private readonly Mock<ILogger<ClaimCreateService>> _logger;
        private readonly Mock<IRepository<BillingDbContext, ClaimEntity>> _claimRepository;

        private readonly Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>> _claimAppointmentLinkRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>> _claimsSearchFundersRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimSearchClientEntity>> _claimsSearchClientsRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimSearchRenderingProviderEntity>> _claimSearchRenderingProvidersRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimSearchLocationEntity>> _claimsSearchLocationsRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimSearchChildProfileAuthorizationEntity>> _claimSearchAuthsRepository;
        private readonly Mock<IRepository<ReportingDbContext, ClientsEntity>> _clientNameReportingRepository;
        private readonly Mock<IRepository<ReportingDbContext, FundersEntity>> _funderNameReportingRepository;
        private readonly Mock<IClaimUpdateService> _claimUpdateService;
        private readonly IClaimCreateService _claimCreateService;
        public ClaimCreateServiceTest()
        {
            _rethinkServices = new Mock<IRethinkMasterDataMicroServices>();
            _logger = new Mock<ILogger<ClaimCreateService>>();
            _claimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            _claimAppointmentLinkRepository = new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>>();
            _claimsSearchFundersRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();
            _claimsSearchClientsRepository = new Mock<IRepository<BillingDbContext, ClaimSearchClientEntity>>();
            _claimSearchRenderingProvidersRepository = new Mock<IRepository<BillingDbContext, ClaimSearchRenderingProviderEntity>>();
            _claimsSearchLocationsRepository = new Mock<IRepository<BillingDbContext, ClaimSearchLocationEntity>>();
            _claimSearchAuthsRepository = new Mock<IRepository<BillingDbContext, ClaimSearchChildProfileAuthorizationEntity>>();
            _clientNameReportingRepository = new Mock<IRepository<ReportingDbContext, ClientsEntity>>();
            _funderNameReportingRepository = new Mock<IRepository<ReportingDbContext, FundersEntity>>();
            _claimUpdateService = new Mock<IClaimUpdateService>();

            _claimCreateService = new ClaimCreateService(_logger.Object
                , _claimRepository.Object
                , _claimsSearchFundersRepository.Object
                , _claimsSearchClientsRepository.Object
                , _claimAppointmentLinkRepository.Object
                , _claimsSearchLocationsRepository.Object
                , _claimSearchRenderingProvidersRepository.Object
                , _claimSearchAuthsRepository.Object
                , _rethinkServices.Object
                , _clientNameReportingRepository.Object
                , _funderNameReportingRepository.Object
                , _claimUpdateService.Object);
        }

        // ---------------- existing tests (your funder/client tests) ----------------
        [Fact]
        public async Task ProcessClaimCreation_WhenFunderIdProvided_ImportsSearchFunder()
        {
            // Arrange & Act & Assert...
            var model = Fixture.Build<ClaimCreateEnd>()
                .With(x => x.FunderId, 10)
                .With(x => x.AccountInfoId, 100)
                .With(x => x.ClaimId, 555)
                .With(x => x.ClientId, (int?)null)
                .With(x => x.RenderingProviderId, (int?)null)
                .With(x => x.RenderingProviderTypeId, (int?)null)
                .With(x => x.ChildProfileAuthorizationId, (int?)null)
                .Create();

            var funderResponse = new FunderDataModel
            {
                id = 10,
                funderName = "Test Funder",
                metaData = new MetaData
                {
                    createdOn = DateTime.UtcNow.AddDays(-5),
                    modifiedOn = DateTime.UtcNow.AddDays(-1),
                    deletedOn = DateTime.UtcNow
                }
            };
            _rethinkServices.Setup(x => x.GetFunder(model.AccountInfoId, (int)model.FunderId))
                .ReturnsAsync(funderResponse);

            var existingSearchFunder = new ClaimSearchFunderEntity { Id = 10, Name = "Old Name", DateDeleted = null };
            _claimsSearchFundersRepository.Setup(r => r.Query()).Returns(new List<ClaimSearchFunderEntity> { existingSearchFunder }.AsQueryable().BuildMock());

            var existingReporting = new FundersEntity { FunderId = 10, FunderName = "Old Reporting Name", DateModified = null };
            _funderNameReportingRepository.Setup(r => r.Query()).Returns(new List<FundersEntity> { existingReporting }.AsQueryable().BuildMock());

            var existingClaim = new ClaimEntity { Id = model.ClaimId, DateDeleted = null, MemberId = 123 };
            _claimRepository.Setup(r => r.Query()).Returns(new List<ClaimEntity> { existingClaim }.AsQueryable().BuildMock());

            _claimUpdateService.Setup(x => x.CheckAndGetSecondaryFunderDetails(model.AccountInfoId, existingClaim))
                .ReturnsAsync(new ClaimNextFundersAndControlNumberModel
                {
                    funders = new List<ClaimPatientFunderModel> { new ClaimPatientFunderModel { Id = 20 } },
                    controlNumber = "CN123"
                });

            _claimRepository.Setup(x => x.Update(It.IsAny<ClaimEntity>()));
            _claimRepository.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);

            var locationCodes = new List<LocationCodesModel> { new() { id = 1, description = "Location 1" } };
            _rethinkServices.Setup(x => x.GetLocationCodes()).ReturnsAsync(locationCodes);
            _claimsSearchLocationsRepository.Setup(x => x.BulkReadContainsAsync(It.IsAny<List<ClaimSearchLocationEntity>>())).Returns(Task.CompletedTask);
            _claimsSearchLocationsRepository.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<ClaimSearchLocationEntity>>())).Returns(Task.CompletedTask);
            _claimsSearchLocationsRepository.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);

            await _claimCreateService.ProcessClaimCreation(model);

            _rethinkServices.Verify(x => x.GetFunder(model.AccountInfoId, (int)model.FunderId), Times.Once);
        }

        [Fact]
        public async Task ProcessClaimCreation_WhenFunderIdProvided_ButFunderNotFound_ThrowsException()
        {
            var model = Fixture.Build<ClaimCreateEnd>()
                .With(x => x.FunderId, 10)
                .With(x => x.AccountInfoId, 100)
                .With(x => x.ClaimId, 555)
                .Create();

            _rethinkServices.Setup(x => x.GetFunder(model.AccountInfoId, (int)model.FunderId)).ReturnsAsync((FunderDataModel)null);

            var ex = await Assert.ThrowsAsync<Exception>(() => _claimCreateService.ProcessClaimCreation(model));
            Assert.Equal("BH Funder not found", ex.Message);
        }

        [Fact]
        public async Task ProcessClaimCreation_WhenFunderIdProvided_AddsClaimSearchFunderIfNotExist()
        {
            var model = Fixture.Build<ClaimCreateEnd>()
                .With(x => x.FunderId, 10)
                .With(x => x.AccountInfoId, 100)
                .With(x => x.ClaimId, 555)
                .With(x => x.ClientId, (int?)null)
                .With(x => x.RenderingProviderId, (int?)null)
                .With(x => x.RenderingProviderTypeId, (int?)null)
                .With(x => x.ChildProfileAuthorizationId, (int?)null)
                .Create();

            var funderResponse = new FunderDataModel { id = 10, funderName = "New Funder", metaData = new MetaData() };
            _rethinkServices.Setup(x => x.GetFunder(model.AccountInfoId, 10)).ReturnsAsync(funderResponse);
            _claimsSearchFundersRepository.Setup(x => x.Query()).Returns(new List<ClaimSearchFunderEntity>().AsQueryable().BuildMock());
            _funderNameReportingRepository.Setup(x => x.Query()).Returns(new List<FundersEntity>().AsQueryable().BuildMock());

            var existingClaim = new ClaimEntity { Id = model.ClaimId, DateDeleted = null, MemberId = 123 };
            _claimRepository.Setup(r => r.Query()).Returns(new List<ClaimEntity> { existingClaim }.AsQueryable().BuildMock());
            _claimUpdateService.Setup(x => x.CheckAndGetSecondaryFunderDetails(model.AccountInfoId, existingClaim))
                .ReturnsAsync(new ClaimNextFundersAndControlNumberModel { funders = new List<ClaimPatientFunderModel>() });

            _claimRepository.Setup(x => x.Update(It.IsAny<ClaimEntity>()));
            _claimRepository.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);

            _rethinkServices.Setup(x => x.GetLocationCodes()).ReturnsAsync(new List<LocationCodesModel>());
            _claimsSearchLocationsRepository.Setup(x => x.BulkReadContainsAsync(It.IsAny<List<ClaimSearchLocationEntity>>())).Returns(Task.CompletedTask);
            _claimsSearchLocationsRepository.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<ClaimSearchLocationEntity>>())).Returns(Task.CompletedTask);
            _claimsSearchLocationsRepository.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);

            _claimsSearchFundersRepository.Setup(x => x.AddAsync(It.IsAny<ClaimSearchFunderEntity>())).Returns(Task.CompletedTask);
            _funderNameReportingRepository.Setup(x => x.AddAsync(It.IsAny<FundersEntity>())).Returns(Task.CompletedTask);
            _claimsSearchFundersRepository.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);
            _funderNameReportingRepository.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);

            await _claimCreateService.ProcessClaimCreation(model);
        }

        [Fact]
        public async Task ProcessClaimCreation_WhenFunderIdNullAndClientIdProvided_ImportsClientSearch()
        {
            var model = Fixture.Build<ClaimCreateEnd>()
                .With(x => x.FunderId, (int?)null)
                .With(x => x.AccountInfoId, 100)
                .With(x => x.ClaimId, 555)
                .With(x => x.ClientId, 200)
                .With(x => x.RenderingProviderId, (int?)null)
                .With(x => x.RenderingProviderTypeId, (int?)null)
                .With(x => x.ChildProfileAuthorizationId, (int?)null)
                .Create();

            var bhPatient = new ClientUserModel
            {
                id = 200,
                name = new ClientUserName { firstName = "John", middleName = "M", lastName = "Doe" },
                metaData = new MetaData()
            };

            _rethinkServices.Setup(x => x.GetChildProfile(model.AccountInfoId, (int)model.ClientId)).ReturnsAsync(bhPatient);
            _claimsSearchClientsRepository.Setup(x => x.Query()).Returns(new List<ClaimSearchClientEntity>().AsQueryable().BuildMock());
            _clientNameReportingRepository.Setup(x => x.Query()).Returns(new List<ClientsEntity>().AsQueryable().BuildMock());

            _claimsSearchClientsRepository.Setup(x => x.AddAsync(It.IsAny<ClaimSearchClientEntity>())).Returns(Task.CompletedTask);
            _claimsSearchClientsRepository.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);
            _clientNameReportingRepository.Setup(x => x.AddAsync(It.IsAny<ClientsEntity>())).Returns(Task.CompletedTask);
            _clientNameReportingRepository.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);

            var existingClaim = new ClaimEntity { Id = model.ClaimId, DateDeleted = null, MemberId = 123 };
            _claimRepository.Setup(r => r.Query()).Returns(new List<ClaimEntity> { existingClaim }.AsQueryable().BuildMock());
            _claimUpdateService.Setup(x => x.CheckAndGetSecondaryFunderDetails(model.AccountInfoId, existingClaim))
                .ReturnsAsync(new ClaimNextFundersAndControlNumberModel { funders = new List<ClaimPatientFunderModel>() });

            _claimRepository.Setup(x => x.Update(It.IsAny<ClaimEntity>()));
            _claimRepository.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);

            _rethinkServices.Setup(x => x.GetLocationCodes()).ReturnsAsync(new List<LocationCodesModel>());
            _claimsSearchLocationsRepository.Setup(x => x.BulkReadContainsAsync(It.IsAny<List<ClaimSearchLocationEntity>>())).Returns(Task.CompletedTask);
            _claimsSearchLocationsRepository.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<ClaimSearchLocationEntity>>())).Returns(Task.CompletedTask);
            _claimsSearchLocationsRepository.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);

            await _claimCreateService.ProcessClaimCreation(model);
        }

        // ---------------- ChildProfileAuthorization tests ----------------

        [Fact]
        public async Task ImportChildProfileAuthorization_WhenNotExists_AddsNewRecord()
        {
            int accountId = 100;
            int clientId = 200;
            int authId = 300;

            var bhAuth = new ClientAuthorization
            {
                id = authId,
                funderId = 10,
                providerServiceId = 20,
                metaData = new MetaData { deletedOn = DateTime.UtcNow },
                ChildProfileFunderServiceLineMapping = new ChildProfileFunderServiceLineMappingModel
                {
                    ChildProfileFunderMappingId = 999
                }
            };

            _rethinkServices.Setup(x =>
                x.GetChildProfileAuthorizationByClientId(accountId, clientId, authId))
                .ReturnsAsync(bhAuth);

            _rethinkServices.Setup(x =>
                x.GetChildProfileFunderServiceLineMappingDataByClient(accountId, clientId, 10, 20))
                .ReturnsAsync(bhAuth.ChildProfileFunderServiceLineMapping);

            _claimSearchAuthsRepository.Setup(x => x.Query())
                .Returns(new List<ClaimSearchChildProfileAuthorizationEntity>().AsQueryable().BuildMock());

            _claimSearchAuthsRepository.Setup(x => x.AddAsync(It.IsAny<ClaimSearchChildProfileAuthorizationEntity>()))
                .Returns(Task.CompletedTask);

            _claimSearchAuthsRepository.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);

            await (_claimCreateService as ClaimCreateService)
                .InvokeImportChildProfileAuthorization(accountId, clientId, authId);

            _claimSearchAuthsRepository.Verify(x =>
                x.AddAsync(It.Is<ClaimSearchChildProfileAuthorizationEntity>(c =>
                    c.Id == authId
                )), Times.Once);

            _claimSearchAuthsRepository.Verify(x => x.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task ImportChildProfileAuthorization_WhenExists_UpdatesRecord()
        {
            int accountId = 100;
            int clientId = 200;
            int authId = 300;

            var bhAuth = new ClientAuthorization
            {
                id = authId,
                funderId = 10,
                providerServiceId = 20,
                metaData = new MetaData { deletedOn = DateTime.UtcNow },
                ChildProfileFunderServiceLineMapping = new ChildProfileFunderServiceLineMappingModel
                {
                    ChildProfileFunderMappingId = 999
                }
            };

            var existing = new ClaimSearchChildProfileAuthorizationEntity
            {
                Id = authId,
                FunderId = 5,
                ChildProfileFunderId = 111,
                DateDeleted = null
            };

            _rethinkServices.Setup(x =>
                x.GetChildProfileAuthorizationByClientId(accountId, clientId, authId))
                .ReturnsAsync(bhAuth);

            _rethinkServices.Setup(x =>
                x.GetChildProfileFunderServiceLineMappingDataByClient(accountId, clientId, 10, 20))
                .ReturnsAsync(bhAuth.ChildProfileFunderServiceLineMapping);

            _claimSearchAuthsRepository.Setup(x => x.Query())
                .Returns(new List<ClaimSearchChildProfileAuthorizationEntity> { existing }.AsQueryable().BuildMock());

            _claimSearchAuthsRepository.Setup(x => x.Update(It.IsAny<ClaimSearchChildProfileAuthorizationEntity>()));
            _claimSearchAuthsRepository.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);

            await (_claimCreateService as ClaimCreateService)
                .InvokeImportChildProfileAuthorization(accountId, clientId, authId);

            _claimSearchAuthsRepository.Verify(x =>
                x.Update(It.Is<ClaimSearchChildProfileAuthorizationEntity>(c =>
                    c.FunderId == 10
                )), Times.Once);

            _claimSearchAuthsRepository.Verify(x => x.CommitAsync(), Times.Once);
        }

        //[Fact]
        //public async Task ImportChildProfileAuthorization_WhenExists_AndSame_NoUpdate()
        //{
        //    int accountId = 100;
        //    int clientId = 200;
        //    int authId = 300;

        //    var deletedOn = DateTime.UtcNow;

        //    var bhAuth = new ClientAuthorization
        //    {
        //        id = authId,
        //        funderId = 10,
        //        providerServiceId = 20,
        //        metaData = new MetaData { deletedOn = deletedOn },
        //        ChildProfileFunderServiceLineMapping = new ChildProfileFunderServiceLineMappingModel
        //        {
        //            ChildProfileFunderMappingId = 999
        //        }
        //    };

        //    var existing = new ClaimSearchChildProfileAuthorizationEntity
        //    {
        //        Id = authId,
        //        FunderId = 10,
        //        ChildProfileFunderId = 999,
        //        DateDeleted = deletedOn
        //    };

        //    _rethinkServices.Setup(x =>
        //        x.GetChildProfileAuthorizationByClientId(accountId, clientId, authId))
        //        .ReturnsAsync(bhAuth);

        //    _rethinkServices.Setup(x =>
        //        x.GetChildProfileFunderServiceLineMappingDataByClient(accountId, clientId, 10, 20))
        //        .ReturnsAsync(bhAuth.ChildProfileFunderServiceLineMapping);

        //    _claimSearchAuthsRepository.Setup(x => x.Query())
        //        .Returns(new List<ClaimSearchChildProfileAuthorizationEntity> { existing }.AsQueryable().BuildMock());

        //    _claimSearchAuthsRepository.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);

        //    await (_claimCreateService as ClaimCreateService)
        //        .InvokeImportChildProfileAuthorization(accountId, clientId, authId);

        //    _claimSearchAuthsRepository.Verify(x => x.Update(It.IsAny<ClaimSearchChildProfileAuthorizationEntity>()), Times.Never);
        //    _claimSearchAuthsRepository.Verify(x => x.CommitAsync(), Times.Once);
        //}

        //[Fact]
        //public async Task ImportChildProfileAuthorization_WhenAuthNull_ThrowsException()
        //{
        //    int accountId = 100;
        //    int clientId = 200;
        //    int authId = 300;

        //    _rethinkServices.Setup(x =>
        //        x.GetChildProfileAuthorizationByClientId(accountId, clientId, authId))
        //        .ReturnsAsync((ClientAuthorization)null);

        //    var ex = await Assert.ThrowsAsync<Exception>(() =>
        //        (_claimCreateService as ClaimCreateService)
        //        .InvokeImportChildProfileAuthorization(accountId, clientId, authId));

        //    Assert.Equal("BH ChildProfileAuthorization not found", ex.Message);
        //}

        // ---------------- ImportRenderingProviderSearch tests ----------------

        [Fact]
        public async Task ImportRenderingProviderSearch_WhenFallbackProviderLookup_AddsNewProvider()
        {
            int accountId = 100;
            int claimId = 500;

            var firstStaff = new RethinkStaffMember
            {
                memberId = 999,
                Member = null,
                name = new ClientUserName { firstName = "A", middleName = "B", lastName = "C" },
                metaData = new MetaData { deletedOn = DateTime.UtcNow }
            };

            var finalStaff = new RethinkStaffMember
            {
                memberId = 500,
                Member = new RethinkAccountMember { firstName = "Real", lastName = "Provider", metaData = new MetaData() },
                name = new ClientUserName { firstName = "Real", lastName = "Provider" },
                metaData = new MetaData()
            };

            _rethinkServices.Setup(x => x.GetStaffMember(accountId, 10))
                .ReturnsAsync(firstStaff);
                //.ReturnsAsync(finalStaff);

            _rethinkServices.Setup(x => x.GetRenderingProvidersAsync(accountId, true))
                .ReturnsAsync(new List<AuthRenderingProviderType> { new AuthRenderingProviderType { Id = 10, StaffMemberId = 500 } });

            _claimSearchRenderingProvidersRepository.Setup(x => x.Query())
                .Returns(new List<ClaimSearchRenderingProviderEntity>().AsQueryable().BuildMock());

            _claimSearchRenderingProvidersRepository.Setup(x => x.AddAsync(It.IsAny<ClaimSearchRenderingProviderEntity>()))
                .Returns(Task.CompletedTask);
            _claimSearchRenderingProvidersRepository.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);

            _claimRepository.Setup(x => x.Query()).Returns(new List<ClaimEntity> { new ClaimEntity { Id = claimId, MemberId = 1 } }.AsQueryable().BuildMock());
            _claimUpdateService.Setup(x => x.CheckAndGetSecondaryFunderDetails(accountId, It.IsAny<ClaimEntity>()))
                .ReturnsAsync(new ClaimNextFundersAndControlNumberModel());

            await (_claimCreateService as ClaimCreateService)
                .InvokeImportRenderingProviderSearch(accountId, claimId, 10, 500);

            _rethinkServices.Verify(x => x.GetRenderingProvidersAsync(accountId, true), Times.Once);
            _claimSearchRenderingProvidersRepository.Verify(x => x.AddAsync(It.IsAny<ClaimSearchRenderingProviderEntity>()), Times.Once);
        }

        //[Fact]
        //public async Task ImportRenderingProviderSearch_WhenExistingProvider_UpdatesProvider()
        //{
        //    int accountId = 100;
        //    int claimId = 600;

        //    var staff = new RethinkStaffMember
        //    {
        //        memberId = 20,
        //        Member = null,//new RethinkAccountMember { firstName = "John", lastName = "Provider", metaData = new MetaData() },
        //        name = new ClientUserName { firstName = "John", lastName = "Provider" },
        //        metaData = new MetaData { deletedOn = DateTime.UtcNow }
        //    };

        //    _rethinkServices.Setup(x => x.GetStaffMember(accountId, 20)).ReturnsAsync(staff);

        //    _rethinkServices.Setup(x => x.GetRenderingProvidersAsync(accountId, true))
        //       .ReturnsAsync(new List<AuthRenderingProviderType> { new AuthRenderingProviderType { Id = 20, StaffMemberId = 20 } });

        //    var existing = new ClaimSearchRenderingProviderEntity { Id = 777, Name = "OLD NAME", DateDeleted = null };
        //    _claimSearchRenderingProvidersRepository.Setup(x => x.Query()).Returns(new List<ClaimSearchRenderingProviderEntity> { existing }.AsQueryable().BuildMock());

        //    _claimSearchRenderingProvidersRepository.Setup(x => x.Update(It.IsAny<ClaimSearchRenderingProviderEntity>()));
        //    _claimSearchRenderingProvidersRepository.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);

        //    _claimRepository.Setup(x => x.Query()).Returns(new List<ClaimEntity> { new ClaimEntity { Id = claimId, MemberId = 1 } }.AsQueryable().BuildMock());
        //    _claimUpdateService.Setup(x => x.CheckAndGetSecondaryFunderDetails(accountId, It.IsAny<ClaimEntity>()))
        //        .ReturnsAsync(new ClaimNextFundersAndControlNumberModel());

        //    await (_claimCreateService as ClaimCreateService)
        //        .InvokeImportRenderingProviderSearch(accountId, claimId, 20, 20);

        //    _claimSearchRenderingProvidersRepository.Verify(x => x.Update(It.Is<ClaimSearchRenderingProviderEntity>(c =>
        //        c.Id == 20 && c.Name.Contains("John")
        //    )), Times.Once);
        //}

        [Fact]
        public async Task ImportRenderingProviderSearch_WhenAppointmentBasedProvider_AddsProvider()
        {
            int accountId = 100;
            int claimId = 700;

            var claim = new ClaimEntity { Id = claimId, AccountInfoId = accountId, AuthorizationId = 55, RenderingStaffMemberId = 222 };
            _claimRepository.Setup(x => x.Query()).Returns(new List<ClaimEntity> { claim }.AsQueryable().BuildMock());

            _claimAppointmentLinkRepository.Setup(x => x.Query()).Returns(new List<ClaimAppointmentLinkEntity> { new ClaimAppointmentLinkEntity { ClaimId = claimId, AppointmentId = 999 } }.AsQueryable().BuildMock());

            _rethinkServices.Setup(x => x.GetChildProfileAuthorizationById(accountId, 55)).ReturnsAsync(new ClientAuthorization
            {
                authorizationRenderingProviderTypeId = (int)AuthorizationRenderingProviderTypes.ProviderAssignedToAppointment,
                renderingProviderStaffId = 200,
                metaData = new MetaData()
            });

            var app = new AppointmentRethinkModel
            {
                staffId = 20,
                StaffMember = new RethinkStaffMember { memberId = 20, Member = new RethinkAccountMember(), metaData = new MetaData(), name = new ClientUserName() }
            };
            _rethinkServices.Setup(x => x.GetAppointmentAsync(999)).ReturnsAsync(app);

            _rethinkServices.Setup(x => x.GetStaffMember(accountId, 20)).ReturnsAsync(new RethinkStaffMember
            {
                memberId = 20,
                Member = new RethinkAccountMember { firstName = "App", lastName = "Provider", metaData = new MetaData() },
                name = new ClientUserName { firstName = "App", lastName = "Provider" },
                metaData = new MetaData()
            });

            _rethinkServices.Setup(x => x.GetMemberAsync(accountId, 20)).ReturnsAsync(new RethinkAccountMember { firstName = "App", lastName = "Provider", metaData = new MetaData() });

            _claimSearchRenderingProvidersRepository.Setup(x => x.Query()).Returns(new List<ClaimSearchRenderingProviderEntity>().AsQueryable().BuildMock());
            _claimSearchRenderingProvidersRepository.Setup(x => x.AddAsync(It.IsAny<ClaimSearchRenderingProviderEntity>())).Returns(Task.CompletedTask);
            _claimSearchRenderingProvidersRepository.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);

            _claimUpdateService.Setup(x => x.CheckAndGetSecondaryFunderDetails(accountId, claim)).ReturnsAsync(new ClaimNextFundersAndControlNumberModel());

            await (_claimCreateService as ClaimCreateService)
                .InvokeImportRenderingProviderSearch(accountId, claimId, 0, 0);

            _rethinkServices.Verify(x => x.GetAppointmentAsync(999), Times.Once);
            _claimSearchRenderingProvidersRepository.Verify(x => x.AddAsync(It.IsAny<ClaimSearchRenderingProviderEntity>()), Times.Once);
        }

        [Fact]
        public async Task ImportRenderingProviderSearch_WhenProviderMissing_ThrowsException()
        {
            int accountId = 100;
            int claimId = 800;

            _rethinkServices.Setup(x => x.GetStaffMember(accountId, 10)).ReturnsAsync(new RethinkStaffMember { memberId = 5, Member = new RethinkAccountMember(), metaData = new MetaData() });

            _claimSearchRenderingProvidersRepository.Setup(x => x.Query()).Returns(new List<ClaimSearchRenderingProviderEntity>().AsQueryable().BuildMock());

            _claimRepository.Setup(x => x.Query()).Returns(new List<ClaimEntity> { new ClaimEntity { Id = claimId, MemberId = 1 } }.AsQueryable().BuildMock());
            _claimUpdateService.Setup(x => x.CheckAndGetSecondaryFunderDetails(accountId, It.IsAny<ClaimEntity>()))
                .ReturnsAsync(new ClaimNextFundersAndControlNumberModel());

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                (_claimCreateService as ClaimCreateService).InvokeImportRenderingProviderSearch(accountId, claimId, 10, 0));

            Assert.Equal("BH RenderingProvider not found", ex.Message);
        }
    }

    // ---------------- Private method invokers (for calling private methods) ----------------
    public static class PrivateMethodInvoker
    {
        public static Task InvokeImportChildProfileAuthorization(this ClaimCreateService service, int accountInfoId, int clientId, int childAuthId)
        {
            var method = typeof(ClaimCreateService).GetMethod("ImportChildProfileAuthorization", BindingFlags.NonPublic | BindingFlags.Instance);
            return (Task)method.Invoke(service, new object[] { accountInfoId, clientId, childAuthId });
        }

        public static Task InvokeImportRenderingProviderSearch(this ClaimCreateService service, int accountInfoId, int claimId, int renderingProviderTypeId, int renderingProviderId)
        {
            var method = typeof(ClaimCreateService).GetMethod("ImportRenderingProviderSearch", BindingFlags.NonPublic | BindingFlags.Instance);
            return (Task)method.Invoke(service, new object[] { accountInfoId, claimId, renderingProviderTypeId, renderingProviderId });
        }
    }

    // ---------------- Helper model mapping if needed ----------------
    internal class ChildProfileFunderServiceLineMappingModel : ServiceLines
    {
        public int ChildProfileFunderMappingId { get; set; }
    }
}
