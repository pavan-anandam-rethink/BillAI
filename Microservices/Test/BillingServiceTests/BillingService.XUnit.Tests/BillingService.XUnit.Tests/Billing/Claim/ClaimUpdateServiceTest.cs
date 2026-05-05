using AutoFixture;
using Azure.Storage.Blobs.Models;
using BillingService.Domain.DataObjects.Billing;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using BillingService.Domain.Services.Billing;
using BillingService.XUnit.Tests.Common;
using Microsoft.EntityFrameworkCore;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Common.Models.Clients;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Claim
{
    public class ClaimUpdateServiceTests : BaseTest
    {
        private readonly Mock<IRepository<BillingDbContext, ClaimEntity>> _claimRepoMock;
        private Mock<IRethinkMasterDataMicroServices> _rethinkServicesMock;
        private readonly Mock<IRepository<BillingDbContext, ClaimDiagnosisCodeEntity>> _billingClaimDiagnosisCodeEntityRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>> _chargeEntryRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>> _linkRepository;
        private readonly Mock<IRepository<BillingDbContext, AppointmentClaimProcessingErrorEntity>> _appointmentClaimProcessingErrorRepository;
        private ClaimUpdateService _service;
        private IServiceProvider _serviceProvider;
        private readonly Mock<IRepository<BillingDbContext, ClaimAppointmentLinkChargeEntry>> _linkChargeRepository;
        private readonly Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>> _paymentClaimServiceLineRepository;
        private readonly Mock<IRepository<BillingDbContext, PaymentClaimEntity>> _paymentClaimRepository;
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

        public ClaimUpdateServiceTests()
        {
            _claimRepoMock = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            _rethinkServicesMock = new Mock<IRethinkMasterDataMicroServices>();
            _billingClaimDiagnosisCodeEntityRepository = new Mock<IRepository<BillingDbContext, ClaimDiagnosisCodeEntity>>();
            _chargeEntryRepository = new Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>>();
            _linkRepository = new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>>();
            _appointmentClaimProcessingErrorRepository = new Mock<IRepository<BillingDbContext, AppointmentClaimProcessingErrorEntity>>();
            _serviceProvider = new Mock<IServiceProvider>().Object;
            _linkChargeRepository = new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkChargeEntry>>();
            _paymentClaimServiceLineRepository = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>>();
            _paymentClaimRepository = new Mock<IRepository<BillingDbContext, PaymentClaimEntity>>();
            _service = new ClaimUpdateService(_claimRepoMock.Object, _rethinkServicesMock.Object, _billingClaimDiagnosisCodeEntityRepository.Object, _chargeEntryRepository.Object, _linkRepository.Object, _appointmentClaimProcessingErrorRepository.Object, _serviceProvider, _linkChargeRepository.Object, _paymentClaimServiceLineRepository.Object, _paymentClaimRepository.Object);
        }

        [Fact]
        public async Task UpdateClaimSecondaryFunderOnRefresh_ReturnsTrue_WhenSecondaryFunderFound()
        {
            // Arrange
            int accountInfoId = 1, memberId = 2, claimId = 3;

            var claim = new ClaimEntity
            {
                Id = claimId,
                ChildProfileId = 2,
                ClientFunderServiceLineId = 1,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today
            };

            // Mock claim repository query
            var claims = new List<ClaimEntity> { claim }.AsQueryable();
            var dbSetMock = claims.BuildMockDbSet(); // using MockQueryable.Moq
            _claimRepoMock.Setup(x => x.Query()).Returns(dbSetMock.Object);
            _claimRepoMock.Setup(x => x.Update(It.IsAny<ClaimEntity>()));
            _claimRepoMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Mock rethink service for secondary funder detection
            _rethinkServicesMock.Setup(x => x.GetChildProfileFunderServiceLineMapping(accountInfoId, It.IsAny<int>()))
                .ReturnsAsync(new List<ServiceLines>
                {
                    new ServiceLines
                    {
                        id = 1,
                        serviceId = 1,
                        responsibilitySequence = Rethink.Services.Common.Enums.BH.ResponsibilitySequenceType.Secondary,
                        ChildProfileFunderMapping = new FunderDetails
                        {
                            startDate = null,
                            endDate = null,
                            metaData = new MetaData { deletedOn = null },
                            Funder = new FunderDataModel { funderName = "Funder" }
                        },
                        ChildProfileFunderMappingId = 1
                    }
                });

            // ---- Mock dependencies used by SyncClaimDiagnosisCode ----
            // AppointmentId lookup
            var claimAppointmentLinks = new List<ClaimAppointmentLinkEntity>
            {
                new ClaimAppointmentLinkEntity { ClaimId = claimId, AppointmentId = 10, DateDeleted = null }
            };

            var claimWithLinks = new ClaimEntity
            {
                Id = claimId,
                ClaimAppointmentLinks = claimAppointmentLinks
            };

            var claimQuery = new List<ClaimEntity> { claimWithLinks }.AsQueryable();
            var claimDbSet = claimQuery.BuildMockDbSet();
            _claimRepoMock.Setup(x => x.Query()).Returns(claimDbSet.Object);

            // Appointment mock
            var appointment = new AppointmentRethinkModel
            {
                staffAccountInfoId = 1,
                clientId = 1,
                procedureCodeId = 101
            };

            _rethinkServicesMock.Setup(x => x.GetAppointmentAsync(10)).ReturnsAsync(appointment);

            _rethinkServicesMock.Setup(x => x.GetChildProfileAuthBillingCodeForAppointment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new AppointmentClientAuthBillingCodeModel
                {
                    providerBillingCodeId = 1,
                    childProfileAuthorizationId = 2
                });

            _rethinkServicesMock.Setup(x => x.GetProviderBillingCode(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new BillingCodeData());

            _rethinkServicesMock.Setup(x => x.GetChildProfileAuthorizationByClientId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new ClientAuthorization
                {
                    id = 2,
                    childProfileDiagnosisId = 3
                });

            _rethinkServicesMock.Setup(x => x.GetChildProfileAuthorizationDiagnosisCodesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<ChildProfileAuthorizationDiagnosisCode>
                {
                    new ChildProfileAuthorizationDiagnosisCode
                    {
                        diagnosisId = 5,
                        order = 1,
                        includeOnClaims = true
                    }
                });

            // Act
            var result = await _service.UpdateClaimSecondaryFunderOnRefresh(accountInfoId, memberId, claimId);

            // Assert
            Assert.False(result.Success);
            _claimRepoMock.Verify(x => x.Update(It.IsAny<ClaimEntity>()), Times.AtLeastOnce);
            _claimRepoMock.Verify(x => x.SaveChangesAsync(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task UpdateClaimSecondaryFunderOnRefresh_ReturnsTrue_WhenNoSecondaryFunderFound()
        {
            // Arrange
            int accountInfoId = 1, memberId = 2, claimId = 3;
            var claim = new ClaimEntity { Id = claimId, DateDeleted = null };
            var claims = new List<ClaimEntity> { claim }.AsQueryable();
            var dbSetMock = claims.BuildMockDbSet();

            _claimRepoMock.Setup(x => x.Query()).Returns(dbSetMock.Object);
            _claimRepoMock.Setup(x => x.Update(It.IsAny<ClaimEntity>()));
            _claimRepoMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            _rethinkServicesMock.Setup(x => x.GetChildProfileFunderServiceLineMapping(accountInfoId, It.IsAny<int>()))
                .ReturnsAsync(new List<ServiceLines>()); // No secondary funder

            var appointment = new AppointmentRethinkModel
            {
                staffAccountInfoId = 1,
                clientId = 1,
                procedureCodeId = 101
            };

            _rethinkServicesMock.Setup(x => x.GetAppointmentAsync(It.IsAny<int>())).ReturnsAsync(appointment);

            _rethinkServicesMock.Setup(x => x.GetChildProfileAuthBillingCodeForAppointment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((AppointmentClientAuthBillingCodeModel)null);

            // Act
            var result = await _service.UpdateClaimSecondaryFunderOnRefresh(accountInfoId, memberId, claimId);

            // Assert
            Assert.True(result.Success);
            Assert.False(claim.IsSecondaryPayerAvailable);
        }

        [Fact]
        public async Task UpdateClaimSecondaryFunderOnRefresh_ReturnsFalse_WhenClaimNotFound()
        {
            // Arrange
            int accountInfoId = 1, memberId = 2, claimId = 3;
            var claims = new List<ClaimEntity>().AsQueryable();
            var dbSetMock = claims.BuildMockDbSet();

            _claimRepoMock.Setup(x => x.Query()).Returns(dbSetMock.Object);

            // Act
            var result = await _service.UpdateClaimSecondaryFunderOnRefresh(accountInfoId, memberId, claimId);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task UpdateClaimSecondaryFunderOnRefresh_ReturnsFalse_OnException()
        {
            // Arrange
            int accountInfoId = 1, memberId = 2, claimId = 3;
            _claimRepoMock.Setup(x => x.Query()).Throws(new Exception("DB error"));

            // Act
            var result = await _service.UpdateClaimSecondaryFunderOnRefresh(accountInfoId, memberId, claimId);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task CheckAndGetSecondaryFunderDetails_ReturnsModel_WhenSecondaryFunderExists()
        {
            // Arrange
            int accountInfoId = 1;
            var claim = new ClaimEntity
            {
                Id = 1,
                ChildProfileId = 2,
                ClientFunderServiceLineId = 1,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today
            };
            var funderMappings = new List<ServiceLines>
            {
                new ServiceLines
                {
                    id = 1,
                    serviceId = 1,
                    responsibilitySequence = Rethink.Services.Common.Enums.BH.ResponsibilitySequenceType.Secondary,
                    ChildProfileFunderMapping = new FunderDetails
                    {
                        startDate = DateTime.Today.AddDays(-1),
                        endDate = DateTime.Today.AddDays(1),
                        metaData = new MetaData { deletedOn = null },
                        Funder = new FunderDataModel { funderName = "Funder" }
                    },
                    ChildProfileFunderMappingId = 1
                }
            };
            _rethinkServicesMock.Setup(x => x.GetChildProfileFunderServiceLineMapping(accountInfoId, claim.ChildProfileId))
                .ReturnsAsync(funderMappings);

            // Act
            var result = await _service.CheckAndGetSecondaryFunderDetails(accountInfoId, claim);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.funders);
            Assert.Equal("Funder", result.funders[0].Name);
        }

        [Fact]
        public async Task CheckAndGetSecondaryFunderDetails_ReturnsNull_WhenNoSecondaryFunder()
        {
            // Arrange
            int accountInfoId = 1;
            var claim = new ClaimEntity
            {
                Id = 1,
                ChildProfileId = 2,
                ClientFunderServiceLineId = 1,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today
            };
            var funderMappings = new List<ServiceLines>();
            _rethinkServicesMock.Setup(x => x.GetChildProfileFunderServiceLineMapping(accountInfoId, claim.ChildProfileId))
                .ReturnsAsync(funderMappings);

            // Act
            var result = await _service.CheckAndGetSecondaryFunderDetails(accountInfoId, claim);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CheckAndGetSecondaryFunderDetails_ReturnsNull_OnException()
        {
            // Arrange
            int accountInfoId = 1;
            var claim = new ClaimEntity { Id = 1, ChildProfileId = 2, ClientFunderServiceLineId = 1 };
            _rethinkServicesMock.Setup(x => x.GetChildProfileFunderServiceLineMapping(accountInfoId, claim.ChildProfileId))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _service.CheckAndGetSecondaryFunderDetails(accountInfoId, claim);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void RoundCacluation_ShouldReturnCorrectValue_ForAllRoundingTypes()
        {
            // Arrange
            var method = typeof(ClaimUpdateService)
                .GetMethod("RoundCacluation",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            Assert.NotNull(method);

            // Act
            var roundNearest = (double)method.Invoke(_service,
                new object[] { 2.5, RoundingTypes.RoundToNearestUnit });

            var roundUp = (double)method.Invoke(_service,
                new object[] { 2.1, RoundingTypes.RoundUp });

            var roundDown = (double)method.Invoke(_service,
                new object[] { 2.9, RoundingTypes.RoundDown });

            var noRounding = (double)method.Invoke(_service,
                new object[] { 2.4, RoundingTypes.NoRounding });

            // Assert
            Assert.Equal(2, roundNearest);
            Assert.Equal(3, roundUp);
            Assert.Equal(2, roundDown);
            Assert.Equal(2.4, noRounding);
        }

        [Fact]
        public async Task GetProviderBillingCode_ShouldReturnAuthorizationBillingCode_WhenAuthorizationExists()
        {
            // Arrange
            var appointment = new AppointmentRethinkModel
            {
                id = 1,
                staffAccountInfoId = 10
            };

            var billingCode = new BillingCodeData
            {
                funderId = 100,
                unitTypeId = 200,
                serviceId = 300
            };

            var authBillingCode = new AppointmentClientAuthBillingCodeModel
            {
                AppointmentProviderBillingCode = billingCode
            };

            var funder = new FunderDataModel();
            var unitTypes = new List<ClientUnitTypes>
            {
                new ClientUnitTypes { id = 200 }
            };
            var providerService = new ClientProviderServiceModel();
            _rethinkServicesMock
                .Setup(x => x.GetFunder(10, 100))
                .ReturnsAsync(funder);

            _rethinkServicesMock
                .Setup(x => x.GetUnitTypesAsync())
                .ReturnsAsync(unitTypes);

            _rethinkServicesMock
                .Setup(x => x.GetProviderService(10, 300))
                .ReturnsAsync(providerService);

            // Act
            var result = await _service.GetProviderBillingCode(appointment, authBillingCode);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(funder, result.funders);
            Assert.Equal(unitTypes.First(), result.unitTypes);
            Assert.Equal(providerService, result.providerSerivces);
        }

        [Fact]
        public async Task GetProviderBillingCode_ShouldReturnProviderBillingCode_WhenAuthorizationNull()
        {
            // Arrange
            var appointment = new AppointmentRethinkModel
            {
                id = 1,
                staffAccountInfoId = 10,
                providerBillingCodeId = 500
            };

            var billingCode = new BillingCodeData
            {
                funderId = 100,
                unitTypeId = 200,
                serviceId = 300
            };

            var funder = new FunderDataModel();
            var unitTypes = new List<ClientUnitTypes>
            {
                new ClientUnitTypes { id = 200 }
            };
            var providerService = new ClientProviderServiceModel();
            _rethinkServicesMock
                .Setup(x => x.GetProviderBillingCode(10, 500))
                .ReturnsAsync(billingCode);

            _rethinkServicesMock
                .Setup(x => x.GetFunder(10, 100))
                .ReturnsAsync(funder);

            _rethinkServicesMock
                .Setup(x => x.GetUnitTypesAsync())
                .ReturnsAsync(unitTypes);

            _rethinkServicesMock
                .Setup(x => x.GetProviderService(10, 300))
                .ReturnsAsync(providerService);

            // Act
            var result = await _service.GetProviderBillingCode(appointment, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(funder, result.funders);
        }

        [Fact]
        public async Task GetProviderBillingCode_ShouldReturnProviderBillingCode_WhenAuthorizationMissing()
        {
            // Arrange
            var appointment = new AppointmentRethinkModel
            {
                id = 1,
                staffAccountInfoId = 10,
                providerBillingCodeId = 500,
                modifiedBy = 99
            };

            var billingCode = new BillingCodeData
            {
                funderId = 100,
                unitTypeId = 200,
                serviceId = 300
            };

            var funder = new FunderDataModel();

            var unitTypes = new List<ClientUnitTypes>
            {
                new ClientUnitTypes { id = 200 }
            };

            var providerService = new ClientProviderServiceModel();

            _rethinkServicesMock
                .Setup(x => x.GetProviderBillingCode(10, 500))
                .ReturnsAsync(billingCode);

            _rethinkServicesMock
                .Setup(x => x.GetFunder(10, 100))
                .ReturnsAsync(funder);

            _rethinkServicesMock
                .Setup(x => x.GetUnitTypesAsync())
                .ReturnsAsync(unitTypes);

            _rethinkServicesMock
                .Setup(x => x.GetProviderService(10, 300))
                .ReturnsAsync(providerService);

            // Act
            var result = await _service.GetProviderBillingCode(appointment, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(billingCode, result);
        }

        [Fact]
        public async Task LogAppointmentProcessionError_ShouldSoftDeleteExistingErrors_AndAddNewError()
        {
            // Arrange
            int appointmentId = 10;
            int memberId = 20;
            string errorMessage = "Test Error";

            var link = new ClaimAppointmentLinkEntity
            {
                Id = 100,
                AppointmentId = appointmentId,
                DateDeleted = null
            };

            var existingErrors = new List<AppointmentClaimProcessingErrorEntity>
            {
                new AppointmentClaimProcessingErrorEntity
                {
                    Id = 1,
                    ClaimAppointmentLinkId = 100,
                    DateDeleted = null
                }
            };

            var linkDbSet = new List<ClaimAppointmentLinkEntity> { link }
                .AsQueryable()
                .BuildMockDbSet();

            var errorDbSet = existingErrors
                .AsQueryable()
                .BuildMockDbSet();

            _linkRepository.Setup(x => x.Query())
                .Returns(linkDbSet.Object);

            _appointmentClaimProcessingErrorRepository.Setup(x => x.Query())
                .Returns(errorDbSet.Object);

            _appointmentClaimProcessingErrorRepository
                .Setup(x => x.AddAsync(It.IsAny<AppointmentClaimProcessingErrorEntity>()))
                .Returns(Task.CompletedTask);

            _appointmentClaimProcessingErrorRepository
                .Setup(x => x.CommitAsync())
                .Returns(Task.CompletedTask);

            _appointmentClaimProcessingErrorRepository
                .Setup(x => x.UpdateRange(It.IsAny<List<AppointmentClaimProcessingErrorEntity>>()));

            var method = typeof(ClaimUpdateService)
                .GetMethod("LogAppointmentProcessionError",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            // Act
            await (Task)method.Invoke(_service,
                new object[] { appointmentId, memberId, errorMessage });

            // Assert
            _appointmentClaimProcessingErrorRepository.Verify(
                x => x.UpdateRange(It.IsAny<List<AppointmentClaimProcessingErrorEntity>>()),
                Times.Once);

            _appointmentClaimProcessingErrorRepository.Verify(
                x => x.AddAsync(It.IsAny<AppointmentClaimProcessingErrorEntity>()),
                Times.Once);

            _appointmentClaimProcessingErrorRepository.Verify(
                x => x.CommitAsync(),
                Times.Once);
        }

        [Fact]
        public async Task LogAppointmentProcessionError_ShouldAddNewError_WhenNoExistingErrors()
        {
            // Arrange
            int appointmentId = 10;
            int memberId = 20;
            string errorMessage = "Test Error";

            var link = new ClaimAppointmentLinkEntity
            {
                Id = 100,
                AppointmentId = appointmentId,
                DateDeleted = null
            };

            var linkDbSet = new List<ClaimAppointmentLinkEntity> { link }
                .AsQueryable()
                .BuildMockDbSet();

            var errorDbSet = new List<AppointmentClaimProcessingErrorEntity>()
                .AsQueryable()
                .BuildMockDbSet();

            _linkRepository.Setup(x => x.Query())
                .Returns(linkDbSet.Object);

            _appointmentClaimProcessingErrorRepository.Setup(x => x.Query())
                .Returns(errorDbSet.Object);

            _appointmentClaimProcessingErrorRepository
                .Setup(x => x.AddAsync(It.IsAny<AppointmentClaimProcessingErrorEntity>()))
                .Returns(Task.CompletedTask);

            _appointmentClaimProcessingErrorRepository
                .Setup(x => x.CommitAsync())
                .Returns(Task.CompletedTask);

            var method = typeof(ClaimUpdateService)
                .GetMethod("LogAppointmentProcessionError",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            // Act
            await (Task)method.Invoke(_service,
                new object[] { appointmentId, memberId, errorMessage });

            // Assert
            _appointmentClaimProcessingErrorRepository.Verify(
                x => x.UpdateRange(It.IsAny<List<AppointmentClaimProcessingErrorEntity>>()),
                Times.Never); // ✔ Correct

            _appointmentClaimProcessingErrorRepository.Verify(
                x => x.AddAsync(It.IsAny<AppointmentClaimProcessingErrorEntity>()),
                Times.Once);

            _appointmentClaimProcessingErrorRepository.Verify(
                x => x.CommitAsync(),
                Times.Once);
        }

        [Fact]
        public async Task SyncClaimDiagnosisCode_ShouldUpdateChargeEntry_WhenBillingRateChanged()
        {
            // Arrange
            int accountInfoId = 1;
            int memberId = 2;
            int claimId = 3;
            int appointmentId = 10;

            // Claim with appointment link
            var claim = new ClaimEntity
            {
                Id = claimId,
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity>
                {
                    new ClaimAppointmentLinkEntity
                    {
                        ClaimId = claimId,
                        AppointmentId = appointmentId,
                        DateDeleted = null
                    }
                }
            };

            var claimDbSet = new List<ClaimEntity> { claim }
                .AsQueryable()
                .BuildMockDbSet();

            _claimRepoMock.Setup(x => x.Query())
                .Returns(claimDbSet.Object);

            // Diagnosis repo mock
            var diagnosisDbSet = new List<ClaimDiagnosisCodeEntity>()
                .AsQueryable()
                .BuildMockDbSet();

            _billingClaimDiagnosisCodeEntityRepository.Setup(x => x.Query())
                .Returns(diagnosisDbSet.Object);

            _billingClaimDiagnosisCodeEntityRepository.Setup(x => x.AddAsync(It.IsAny<ClaimDiagnosisCodeEntity>()))
                .Returns(Task.CompletedTask);

            _billingClaimDiagnosisCodeEntityRepository.Setup(x => x.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Charge entry exists
            var chargeEntry = new ClaimChargeEntryEntity
            {
                ClaimId = claimId,
                BillingCodeId = 999,
                DateDeleted = null,
                Charges = 0,
                UnitRate = 0
            };

            var chargeDbSet = new List<ClaimChargeEntryEntity> { chargeEntry }
                .AsQueryable()
                .BuildMockDbSet();

            _chargeEntryRepository.Setup(x => x.Query())
                .Returns(chargeDbSet.Object);

            _chargeEntryRepository.Setup(x => x.Update(It.IsAny<ClaimChargeEntryEntity>()));

            _chargeEntryRepository.Setup(x => x.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Appointment
            var appointment = new AppointmentRethinkModel
            {
                id = appointmentId,
                staffAccountInfoId = accountInfoId,
                clientId = 1,
                procedureCodeId = 1,
                providerBillingCodeId = 999,
                providerBillingCodeCredentialId = 1,
                startDateTime = DateTime.Today.AddHours(10),
                endDateTime = DateTime.Today.AddHours(11),
                ProviderBillingCodeCredential = new ProviderBillingCodeCredentialModel
                {
                    contractRate = 100
                }
            };

            _rethinkServicesMock.Setup(x => x.GetAppointmentAsync(appointmentId))
                .ReturnsAsync(appointment);

            // Billing code
            var billingCode = new BillingCodeData
            {
                id = 999,
                funderId = 1,
                unitTypeId = 1,
                serviceId = 1,
                rate = 200,
                providerBillingCodeRateTypeId = 1,
                providerBillingCodeRoundingTypeId = 1,
                restrictStaffProviderToService = false
            };

            var auth = new AppointmentClientAuthBillingCodeModel
            {
                providerBillingCodeId = 999,
                childProfileAuthorizationId = 1,
                AppointmentProviderBillingCode = billingCode,
                ChildProfileAuthorization = new ClientAuthorization
                {
                    id = 1,
                    childProfileDiagnosisId = 1,
                    ChildProfileAuthorizationDiagnosisCodes =
                        new List<ChildProfileAuthorizationDiagnosisCode>
                        {
                            new ChildProfileAuthorizationDiagnosisCode
                            {
                                diagnosisId = 1,
                                order = 1,
                                includeOnClaims = true
                            }
                        }
                }
            };

            _rethinkServicesMock.Setup(x =>
                x.GetChildProfileAuthBillingCodeForAppointment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(auth);

            _rethinkServicesMock.Setup(x =>
                x.GetProviderBillingCode(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(billingCode);

            _rethinkServicesMock.Setup(x =>
                x.GetChildProfileAuthorizationByClientId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(auth.ChildProfileAuthorization);

            _rethinkServicesMock.Setup(x =>
                x.GetChildProfileAuthorizationDiagnosisCodesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(auth.ChildProfileAuthorization.ChildProfileAuthorizationDiagnosisCodes);

            _rethinkServicesMock.Setup(x =>
                x.GetProviderBillingCodeCredential(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new ProviderBillingCodeCredentialModel
                {
                    contractRate = 100
                });

            // UnitTypes mock
            _rethinkServicesMock.Setup(x => x.GetUnitTypesAsync())
                .ReturnsAsync(new List<ClientUnitTypes>
                {
            new ClientUnitTypes { id = 1, unit = 60 }
                });

            // ProviderService mock
            _rethinkServicesMock.Setup(x => x.GetProviderService(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new ClientProviderServiceModel
                {
                    baseRate = 150
                });

            // Call private method
            var method = typeof(ClaimUpdateService)
                .GetMethod("SyncClaimDiagnosisCode",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            await (Task)method.Invoke(_service,
                new object[] { accountInfoId, memberId, claimId });

            // Assert
            _chargeEntryRepository.Verify(x => x.Update(It.IsAny<ClaimChargeEntryEntity>()), Times.Once);

            _chargeEntryRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SyncClaimUpdatePrimaryFunder_NoAppointmentLinks_Returns()
        {
            // Arrange
            var emptyLinks = new List<ClaimAppointmentLinkEntity>();
            var mockQuery = emptyLinks.AsQueryable().BuildMock();

            _linkRepository
                .Setup(x => x.Query())
                .Returns(mockQuery);

            var claimHistoryMock = new Mock<IClaimHistoryService>();
            claimHistoryMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistorySaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var claimSyncServiceMock = new Mock<IClaimSyncService>();
            claimSyncServiceMock
            .Setup(x => x.AddDiagnosisCodes(
                It.IsAny<ClaimEntity>(),
                It.IsAny<AppointmentClientAuthBillingCodeModel>(),
                It.IsAny<int>()));


            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IClaimHistoryService)))
                .Returns(claimHistoryMock.Object);

            serviceProviderMock
                .Setup(x => x.GetService(typeof(IClaimSyncService)))
                .Returns(claimSyncServiceMock.Object);

            _service = new ClaimUpdateService(
                _claimRepoMock.Object,
                _rethinkServicesMock.Object,
                _billingClaimDiagnosisCodeEntityRepository.Object,
                _chargeEntryRepository.Object,
                _linkRepository.Object,
                _appointmentClaimProcessingErrorRepository.Object,
                serviceProviderMock.Object,
                _linkChargeRepository.Object,
                _paymentClaimServiceLineRepository.Object,
                _paymentClaimRepository.Object
            );

            // Act
            await _service.SyncClaimUpdatePrimaryFunder(1, 1, 1);

            // Assert
            _rethinkServicesMock.Verify(
                x => x.GetAppointmentAsync(It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public async Task SyncClaimUpdatePrimaryFunder_DifferentFunders_ThrowsException()
        {
            // Arrange
            var claim = new ClaimEntity { Id = 1 };
            var links = new List<ClaimAppointmentLinkEntity>
            {
                new ClaimAppointmentLinkEntity { ClaimId = 1, AppointmentId = 10, Claim = claim },
                new ClaimAppointmentLinkEntity { ClaimId = 1, AppointmentId = 20, Claim = claim }
            };

            _linkRepository
                .Setup(x => x.Query())
                .Returns(links.AsQueryable().BuildMock());

            _rethinkServicesMock
                .Setup(x => x.GetAppointmentAsync(It.IsAny<int>()))
                .ReturnsAsync((int appointmentId) =>
                    appointmentId == 10
                        ? CreateAppointment(10, 1, 100)   // funder 1
                        : CreateAppointment(20, 2, 100)); // funder 2

            // Optional safety mocks (avoid NullReference from deeper calls)
            _rethinkServicesMock
                .Setup(x => x.GetStaffMember(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new RethinkStaffMember
                {
                    accountId = 1,
                    identifiers = new List<Identifiers>()
                });

            _rethinkServicesMock
                .Setup(x => x.GetChildProfileAuthBillingCodeForAppointment(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
                .ReturnsAsync(new AppointmentClientAuthBillingCodeModel
                {
                    childProfileAuthorizationId = 100,
                    providerBillingCodeId = 1,
                    ChildProfileAuthorization = new ClientAuthorization
                    {
                        id = 100,
                        authorizationNumber = "AUTH123"
                    }
                });

            var claimHistoryMock = new Mock<IClaimHistoryService>();
            claimHistoryMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistorySaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var claimSyncServiceMock = new Mock<IClaimSyncService>();
            claimSyncServiceMock
            .Setup(x => x.AddDiagnosisCodes(
                It.IsAny<ClaimEntity>(),
                It.IsAny<AppointmentClientAuthBillingCodeModel>(),
                It.IsAny<int>()));


            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IClaimHistoryService)))
                .Returns(claimHistoryMock.Object);

            serviceProviderMock
                .Setup(x => x.GetService(typeof(IClaimSyncService)))
                .Returns(claimSyncServiceMock.Object);

            _service = new ClaimUpdateService(
                _claimRepoMock.Object,
                _rethinkServicesMock.Object,
                _billingClaimDiagnosisCodeEntityRepository.Object,
                _chargeEntryRepository.Object,
                _linkRepository.Object,
                _appointmentClaimProcessingErrorRepository.Object,
                serviceProviderMock.Object,
                _linkChargeRepository.Object,
                _paymentClaimServiceLineRepository.Object,
                _paymentClaimRepository.Object
            );

            // Act
            var act = async () =>
                await _service.SyncClaimUpdatePrimaryFunder(10, 1, 1);

            // Assert
            var ex = await Assert.ThrowsAsync<ClaimPrimaryFunderUpdateException>(act);
            Assert.Contains("multiple funders", ex.Message);
        }

        [Fact]
        public async Task SyncClaimUpdatePrimaryFunder_ValidAppointments_DeletesExistingCharges()
        {
            // Arrange
            SetupMockServices_Repo();

            // --- Mock IClaimHistoryService ---
            var claimHistoryMock = new Mock<IClaimHistoryService>();
            claimHistoryMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistorySaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var claimSyncServiceMock = new Mock<IClaimSyncService>();
            claimSyncServiceMock
            .Setup(x => x.AddDiagnosisCodes(
                It.IsAny<ClaimEntity>(),
                It.IsAny<AppointmentClientAuthBillingCodeModel>(),
                It.IsAny<int>()));


            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IClaimHistoryService)))
                .Returns(claimHistoryMock.Object);

            serviceProviderMock
                .Setup(x => x.GetService(typeof(IClaimSyncService)))
                .Returns(claimSyncServiceMock.Object);

            var chargeEntry = new ClaimChargeEntryEntity
            {
                Id = 100,
                ClaimId = claimId,
                BillingCodeId = 999,
                DateDeleted = null,
                Charges = 0,
                UnitRate = 0
            };

            var chargeDbSet = new List<ClaimChargeEntryEntity> { chargeEntry }
                .AsQueryable()
                .BuildMockDbSet();

            _chargeEntryRepository.Setup(x => x.Query())
                .Returns(chargeDbSet.Object);

            _chargeEntryRepository.Setup(x => x.Update(It.IsAny<ClaimChargeEntryEntity>()));

            _chargeEntryRepository.Setup(x => x.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            var linkChargeEntries = new List<ClaimAppointmentLinkChargeEntry>
                {
                         Fixture.Build<ClaimAppointmentLinkChargeEntry>()
                        .With(x => x.Id, 1)
                        .With(x => x.ClaimChargeEntryEntityId, 100)
                        .With(x => x.DateDeleted, (DateTime?)null)
                        .Create()
                };

            // Build async mock
            var mockLinkChargeEntries = linkChargeEntries.AsQueryable().BuildMockDbSet();
            _linkChargeRepository.Setup(r => r.Query()).Returns(mockLinkChargeEntries.Object);

            _service = new ClaimUpdateService(
                _claimRepoMock.Object,
                _rethinkServicesMock.Object,
                _billingClaimDiagnosisCodeEntityRepository.Object,
                _chargeEntryRepository.Object,
                _linkRepository.Object,
                _appointmentClaimProcessingErrorRepository.Object,
                serviceProviderMock.Object,
                _linkChargeRepository.Object,
                _paymentClaimServiceLineRepository.Object,
                _paymentClaimRepository.Object
            );

            // Act
            await _service.SyncClaimUpdatePrimaryFunder(accountInfoId, memberId, claimId);

            // Assert
            _chargeEntryRepository.Verify(
                x => x.Update(It.IsAny<ClaimChargeEntryEntity>()),
                Times.AtLeastOnce);

            _claimRepoMock.Verify(x => x.Update(It.IsAny<ClaimEntity>()), Times.AtLeastOnce);
            claimHistoryMock.Verify(x => x.AddAsync(It.IsAny<ClaimHistorySaveModel>(), true), Times.AtLeastOnce);
        }

        private AppointmentRethinkModel CreateAppointment(int appointmentId = 10, int funderId = 1, int authorizationId = 100)
        {
            return new AppointmentRethinkModel
            {
                id = appointmentId,
                funderId = funderId,
                staffAccountInfoId = 1,
                clientAccountInfoId = 1,
                clientId = 1,
                procedureCodeId = 101,
                serviceId = 1,
                staffId = 1,
                providerBillingCodeId = 1,
                startDateTime = DateTime.Today.AddHours(10),
                endDateTime = DateTime.Today.AddHours(11),
                locationId = 1,
                StaffMember = new RethinkStaffMember
                {
                    accountId = 1,
                    identifiers = new List<Identifiers>()
                },
                ChildProfileAuthorizationBillingCode =
                            new AppointmentClientAuthBillingCodeModel
                            {
                                childProfileAuthorizationId = 100,
                                providerBillingCodeId = 1,
                                ChildProfileAuthorization =
                                    new ClientAuthorization
                                    {
                                        id = authorizationId,
                                        authorizationNumber = "AUTH123"
                                    }
                            }
            };
        }

        private void SetupMockServices_Repo()
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
                        id =  Fixture.Create<int>(),
                        childProfileAuthorizationId =  Fixture.Create<int>(),
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
                ServiceFunders = serviceFunders
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
            _claimRepoMock.Setup(r => r.Query()).Returns(claims);

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


            _rethinkServicesMock = new Mock<IRethinkMasterDataMicroServices>();
            _rethinkServicesMock.Setup(s => s.GetAppointmentAsync(appointmentId)).ReturnsAsync(appointment);
            _rethinkServicesMock.Setup(s => s.GetStaffMember(accountInfoId, memberId)).ReturnsAsync(appointment.StaffMember);
            _rethinkServicesMock.Setup(s => s.GetProviderBillingCode(appointment.StaffMember.accountId, appointment.ChildProfileAuthorizationBillingCode.providerBillingCodeId ?? 0)).ReturnsAsync(billingCodeData);
            _rethinkServicesMock.Setup(s => s.GetChildProfileAuthBillingCodeForAppointment(accountInfoId, clientId, appointment.procedureCodeId)).ReturnsAsync(childProfileAuthorizationBillingCode);
            _rethinkServicesMock.Setup(s => s.GetFunder(accountInfoId, funderId)).ReturnsAsync(funders);
            _rethinkServicesMock.Setup(s => s.GetChildProfileFunderMappings(accountInfoId, clientId)).ReturnsAsync(funderMappingsMicro);
            _rethinkServicesMock.Setup(s => s.GetServiceLineMappingsByFunderId(accountInfoId, appointment.clientId.Value, It.IsAny<int>())).ReturnsAsync(serviceLineMappings);
            _rethinkServicesMock.Setup(s => s.GetChildProfileFunderServiceLineMappingEntity(
                    accountInfoId,
                    appointment.clientId.Value,
                    funderMappingsMicroId.id,
                    expectedServiceLineId))
            .ReturnsAsync(clientFunderServiceLine);

            _rethinkServicesMock.Setup(s => s.GetChildProfileAuthorizationByClientId(
                appointment.staffAccountInfoId,
                appointment.clientId.Value,
                appointment.ChildProfileAuthorizationBillingCode.childProfileAuthorizationId))
            .ReturnsAsync(childProfileAuthorization);

            _rethinkServicesMock.Setup(s => s.GetChildProfileAuthorizationDiagnosisCodesAsync(
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
            _rethinkServicesMock.Setup(s => s.GetUnitTypesAsync())
                .ReturnsAsync(unitTypes);

            var providerService = Fixture.Build<ProviderServiceModel>()
                .With(x => x.Id, expectedServiceId)
                .With(x => x.Name, "Test Service")
                .With(x => x.BaseRate, 150m)

                .Create();

            // Setup mock for GetProviderService
            _rethinkServicesMock.Setup(s => s.GetProviderService(
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

            _rethinkServicesMock.Setup(s => s.GetChildProfileFacility(
                    appointment.staffAccountInfoId,
                    appointment.clientId ?? 0))
                .ReturnsAsync(facility);

            _rethinkServicesMock.Setup(s => s.GetProviderLocation(claimEntity.AccountInfoId, It.IsAny<int>()))
                .ReturnsAsync(providerLocation);

            _rethinkServicesMock.Setup(s => s.GetMainLocation(claimEntity.AccountInfoId))
                .ReturnsAsync(mainLocation);

            _rethinkServicesMock.Setup(s => s.GetReferringProvidersByClientId(appointment.clientId.Value, appointment.clientAccountInfoId))
                .ReturnsAsync(referringProviders);

            _rethinkServicesMock.Setup(s => s.GetProviderLocationList(appointment.staffAccountInfoId))
                .ReturnsAsync(providerLocationList);

            _rethinkServicesMock.Setup(s => s.GetServiceFundersEntityListByFunderId(
                   appointment.clientAccountInfoId,
                   appointment.clientId ?? 0,
                   funderMappingsMicroId.funderId))
               .ReturnsAsync(serviceFunders);

            _rethinkServicesMock.Setup(s => s.GetRenderingProvidersAsync(claimEntity.AccountInfoId, true))
                .ReturnsAsync(renderingProviders);

            _rethinkServicesMock.Setup(r => r.GetChildProfileFunderMappingByMappingId(appointment.clientAccountInfoId, appointment.clientId ?? 0, claimEntity.ClientFunderId ?? 0))
               .ReturnsAsync(clientFunderMapping);

            var secondaryFunderResponse = new ClaimNextFundersAndControlNumberModel
            {
                funders = new List<ClaimPatientFunderModel>() // leave empty to simulate no secondary
            };

            //_claimManagerService.Setup(m => m.InitializeClaim(
            //    It.IsAny<int>(),
            //    It.IsAny<int>(),
            //    It.IsAny<int>(),
            //    It.IsAny<int>(),
            //    It.IsAny<DateTime>(),
            //    It.IsAny<DateTime>()))
            //.ReturnsAsync(() => claimEntity);

            //_claimHistoryMock = new Mock<IClaimHistoryService>();
            _claimRepoMock.Setup(r => r.Update(It.IsAny<ClaimEntity>())).Verifiable();
            _claimRepoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            //_paymentClaimServiceLineAdjustmentRepository
            //.Setup(r => r.AddAndGetAsync(It.IsAny<PaymentClaimServiceLineAdjustmentEntity>()))
            //.Callback<PaymentClaimServiceLineAdjustmentEntity>(entity =>
            //{
            //    entity.Id = new Random().Next(10000, 99999);
            //    entity.DateCreated = DateTime.UtcNow;
            //});

            //_paymentClaimServiceLineAdjustmentRepository
            //    .Setup(r => r.CommitAsync())
            //    .Returns(Task.CompletedTask);



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


            var accountInfoEntity = Fixture.Build<AccountInfoEntityModel>()
            .With(x => x.Id, accountInfoId)
            .Create();

            _rethinkServicesMock.Setup(s => s.GetAccountReturningEntityAsync(accountInfoId, It.IsAny<bool>()))
           .ReturnsAsync(accountInfoEntity);

            var linkEntities = new List<ClaimAppointmentLinkEntity>
            {
                Fixture.Build<ClaimAppointmentLinkEntity>()
                    .With(x => x.AppointmentId, appointment.id)
                    .With(x => x.ClaimId, claimId)
                    .With(x => x.DateDeleted, (DateTime?)null)
                    .With(x => x.Claim, claimEntity)
                    .Create()
            };

            var mockLinkEntities = linkEntities.AsQueryable().BuildMock();

            _linkRepository.Setup(r => r.Query()).Returns(mockLinkEntities);

            // Setup AddAsync to add to the list
            //_claimAppointmentLinkRepository.Setup(r => r.AddAsync(It.IsAny<ClaimAppointmentLinkEntity>()))
            //    .Callback<ClaimAppointmentLinkEntity>(entity => linkEntities.Add(entity))
            //    .Returns(Task.CompletedTask);

            // Setup SaveChangesAsync as a no-op
            _linkRepository.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Setup Update to update the entity in the list
            _linkRepository.Setup(r => r.Update(It.IsAny<ClaimAppointmentLinkEntity>()))
                .Callback<ClaimAppointmentLinkEntity>(entity =>
                {
                    var idx = linkEntities.FindIndex(x => x.AppointmentId == entity.AppointmentId);
                    if (idx >= 0) linkEntities[idx] = entity;
                });


            //_chargeEntryService.Setup(s => s.GetAllClaimsByIdAsync(paymentEntity, It.Is<int[]>(ids => ids.Contains(claimEntity.Id))))
            //       .ReturnsAsync(new List<ClaimChargeItem> {
            //        fixture.Build<ClaimChargeItem>()
            //        .With(x => x.ClaimId, claimId)
            //        .With(x => x.ClaimStatus, 0)
            //        .With(x => x.PatientId, claimEntity.ChildProfileId)
            //        .With(x => x.ChargeEntries, new List<ManualPaymentChargeEntryItem>
            //        {
            //            new ManualPaymentChargeEntryItem
            //            {
            //                Id = 999,
            //                Charges = 100m,
            //                TotalAmount = 80m,
            //                DateOfService = DateTime.Today,
            //                ServiceCode = "A1",
            //                Units = 1,
            //                Description = "Therapy",
            //                Modifier1 = "25",
            //                Modifier2 = "",
            //                Modifier3 = "",
            //                Modifier4 = "",
            //            }
            //        })
            //        .Create()
            //    });
        }
    }
}
