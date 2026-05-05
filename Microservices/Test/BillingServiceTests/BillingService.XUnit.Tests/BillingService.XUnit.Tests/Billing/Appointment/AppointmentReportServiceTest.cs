using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using BillingService.Domain.Services.Billing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Messaging;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.ReportingModels;
using Rethink.Services.Domain.Interfaces;
using SummationService.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Appointment
{
    public class AppointmentReportServiceTest
    {
        private Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>> _linkRepository;
        private Mock<IAppointmentService> _appointmentServiceMock;
        private Mock<IRethinkMasterDataMicroServices> _rethinkServiceMock;
        private Mock<IHelperService> _helperServiceMock;
        private Mock<IRepository<BillingDbContext, ClaimEntity>> _claimRepository;
        private Mock<IRepository<BillingDbContext, AppointmentClaimProcessingErrorEntity>> _appointmentClaimProcessingErrorRepository;
        private Mock<IMapper> _mapper;
        private Mock<IClaimSyncService> _claimSyncService;
        private AppointmentReportService _service;
        private Mock<IRepository<BillingDbContext, UnProcessedApointmentScheduleEntity>> _unProcessedApointmentScheduleRepository;

        private void SetupService(List<ClaimAppointmentLinkEntity> links, List<AppointmentRethinkModel> rethinkAppointments)
        {
            _linkRepository = new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>>();
            _appointmentServiceMock = new Mock<IAppointmentService>();
            _rethinkServiceMock = new Mock<IRethinkMasterDataMicroServices>();
            _helperServiceMock = new Mock<IHelperService>();
            _claimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            _appointmentClaimProcessingErrorRepository = new Mock<IRepository<BillingDbContext, AppointmentClaimProcessingErrorEntity>>();
            _claimSyncService = new Mock<IClaimSyncService>();
            _unProcessedApointmentScheduleRepository = new Mock<IRepository<BillingDbContext, UnProcessedApointmentScheduleEntity>>();
            _mapper = new Mock<IMapper>();

            //var linkDbSet = links.AsQueryable().BuildMockDbSet();
            //_linkRepository.Setup(r => r.Query()).Returns(linkEntities);

            var mockQueryable = links.BuildMock();
            _linkRepository
                .Setup(r => r.Query())
                .Returns(mockQueryable);

            _rethinkServiceMock.Setup(x => x.GetAllCompletedAppointmentsForAnAccountAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>()))
                .ReturnsAsync([500, 700, 400]);

            var schedultedDataList = new List<UnProcessedApointmentScheduleEntity>();

            var scheduledDbSet = schedultedDataList.AsQueryable().BuildMockDbSet();
            _unProcessedApointmentScheduleRepository.Setup(r => r.Query()).Returns(scheduledDbSet.Object);

            var claimEntity = new List<ClaimEntity>()
            {
                new() {AccountInfoId = It.IsAny<int>(), DateDeleted= It.IsAny<DateTime>(), StartDate = It.IsAny<DateTime>() },
                new() {AccountInfoId = It.IsAny<int>(), DateDeleted= It.IsAny<DateTime>(), StartDate = It.IsAny<DateTime>() },
                new() {AccountInfoId = It.IsAny<int>(), DateDeleted= It.IsAny<DateTime>(), StartDate = It.IsAny<DateTime>() }
            }.AsQueryable();
            _claimRepository.Setup(x => x.Query()).Returns(claimEntity);

            _rethinkServiceMock
                .Setup(r => r.GetAppointmentListAsync(It.IsAny<List<int>>()))
                .ReturnsAsync((List<int> ids) => rethinkAppointments.Where(a => ids.Contains(a.id)).ToList());

            _appointmentServiceMock
                .Setup(a => a.SetupRethinkDataForAppointments(It.IsAny<List<AppointmentRethinkModel>>()))
                .ReturnsAsync((List<AppointmentRethinkModel> ap) => ap);

            _appointmentServiceMock
                .Setup(a => a.ToAppointmentItems(It.IsAny<int>(), It.IsAny<List<AppointmentRethinkModel>>(), It.IsAny<int>()))
                .ReturnsAsync((int acct, List<AppointmentRethinkModel> ap, int member) =>
                    ap.Select(x => new AppointmentModel
                    {
                        Id = x.id,
                        StartDate = x.startDateTime,
                        EndDate = x.endDateTime ?? x.startDateTime.AddHours(1),
                        ClientName = "Client-" + (x.clientId ?? 0),
                        FunderName = "Funder-" + x.funderId,
                        StaffName = "Staff-" + x.staffId,
                        ServiceName = "Service-" + x.serviceId,
                        BillingCode = "BC-" + x.providerBillingCodeId,
                        ServiceLocation = "SvcLoc-" + x.toLocationId,
                        Location = "Loc-" + (x.locationId ?? 0)
                    }).ToList());

            _helperServiceMock.Setup(h => h.DefineStyles(It.IsAny<WorkbookPart>()));
            _helperServiceMock
                .Setup(h => h.AddCell(It.IsAny<ExcelCellType>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns((ExcelCellType t, dynamic v, bool alt) => new Cell
                {
                    DataType = CellValues.String,
                    CellValue = new CellValue(Convert.ToString(v))
                });

            _claimSyncService
                .Setup(b => b.PublishUnbilledAppointmentForClaimProcessingAsync(It.IsAny<int>(), It.IsAny<int>(),It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            _service = new AppointmentReportService(
                _linkRepository.Object,
                _claimRepository.Object,
                _appointmentClaimProcessingErrorRepository.Object,
                _appointmentServiceMock.Object,
                _rethinkServiceMock.Object,
                _helperServiceMock.Object,
                _claimSyncService.Object,
                _mapper.Object,
                _unProcessedApointmentScheduleRepository.Object);
        }

        private ClaimAppointmentLinkEntity Link(int claimId, int apptId, int accountInfoId, int clientId, int funderId, ClaimStatus status, bool deleted)
        {
            return new ClaimAppointmentLinkEntity
            {
                ClaimId = claimId,
                AppointmentId = apptId,
                AccountInfoId = accountInfoId,
                DateDeleted = deleted ? DateTime.UtcNow : null,
                Claim = new ClaimEntity
                {
                    Id = claimId,
                    AccountInfoId = accountInfoId,
                    ChildProfileId = clientId,
                    PrimaryFunderId = funderId,
                    ClaimStatus = status,
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = DateTime.UtcNow.Date
                }
            };
        }

        private AppointmentRethinkModel Appt(int id, int clientId, int staffId, int funderId, int serviceId, int toLocationId, DateTime? start = null, DateTime? end = null)
        {
            return new AppointmentRethinkModel
            {
                id = id,
                clientId = clientId,
                staffId = staffId,
                funderId = funderId,
                serviceId = serviceId,
                providerBillingCodeId = 500 + id,
                toLocationId = toLocationId,
                locationId = toLocationId,
                startDateTime = (start ?? DateTime.UtcNow.Date.AddHours(9)),
                endDateTime = (end ?? DateTime.UtcNow.Date.AddHours(10))
            };
        }

        // Base tests
        [Fact]
        public async Task GetUnbilledAppointmentDetails_NoData_ReturnsEmpty()
        {
            // Arrange
            SetupService(
                new List<ClaimAppointmentLinkEntity> { Link(1, 100, 10, 200, 300, ClaimStatus.PendingReview, deleted: false) },
                new List<AppointmentRethinkModel>());
            var req = new UnbilledAppointmentsRequestModel
            {
                AccountInfoId = 10,
                MemberId = 99,
                Clients = new List<int>(),
                PayerOrFunder = new List<int>(),
                Staff = new List<int>(),
                Location = new List<int>(),
                PlaceOfService = new List<int>(),
                Skip = 0,
                Take = 0,
                SortingModels = new List<SortingModel>()
            };
            // Act
            var result = await _service.GetUnbilledAppointmentDetails(req);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.totalCount);
            Assert.Empty(result.appointmentModels);
        }
        [Fact]
        public async Task GetUnbilledAppointmentDetails_FunderFilter_SecondStageEarlyReturn()
        {
            // Arrange
            SetupService(
                new List<ClaimAppointmentLinkEntity>
                {
                    Link(1, 9101, 10, 2001, 777, ClaimStatus.PendingReview, deleted:true),
                    Link(2, 9102, 10, 2002, 777, ClaimStatus.PendingReview, deleted:true)
                },
                new List<AppointmentRethinkModel>
                {
                    Appt(9101, 2001, 4001, 888, 5001, 6001),
                    Appt(9102, 2002, 4002, 889, 5002, 6002)
                });

            var req = new UnbilledAppointmentsRequestModel
            {
                AccountInfoId = 10,
                MemberId = 123,
                PayerOrFunder = new List<int> { 777 }, // matches claim funder ONLY
                Clients = new List<int>(),
                Staff = new List<int>(),
                Location = new List<int>(),
                PlaceOfService = new List<int>(),
                Skip = 0,
                Take = 0,
                SortingModels = new List<SortingModel>()
            };

            // Act
            var result = await _service.GetUnbilledAppointmentDetails(req);

            // Assert
            Assert.Equal(0, result.totalCount);
            Assert.Empty(result.appointmentModels);
            _appointmentServiceMock.Verify(a => a.SetupRethinkDataForAppointments(It.IsAny<List<AppointmentRethinkModel>>()), Times.Never);
            _appointmentServiceMock.Verify(a => a.ToAppointmentItems(It.IsAny<int>(), It.IsAny<List<AppointmentRethinkModel>>(), It.IsAny<int>()), Times.Never);
        }
        [Fact]
        public async Task GetUnbilledAppointmentDetails_ClientFilter_SecondStageEarlyReturn()
        {
            // Arrange
               SetupService(
                new List<ClaimAppointmentLinkEntity>
                {
                    Link(1, 25001, 10, 5001, 7001, ClaimStatus.PendingReview, deleted:true),
                    Link(2, 25002, 10, 5001, 7002, ClaimStatus.PendingReview, deleted:true)
                },
                new List<AppointmentRethinkModel>
                {
                    Appt(25001, 6001, 8001, 7001, 9001, 1001),
                    Appt(25002, 6002, 8002, 7002, 9002, 1002)
                });

            var req = new UnbilledAppointmentsRequestModel
            {
                AccountInfoId = 10,
                MemberId = 321,
                Clients = new List<int> { 5001 },   // Matches claim.ChildProfileId only
                PayerOrFunder = new List<int>(),
                Staff = new List<int>(),
                Location = new List<int>(),
                PlaceOfService = new List<int>(),
                Skip = 0,
                Take = 0,
                SortingModels = new List<SortingModel>()
            };

            // Act
            var result = await _service.GetUnbilledAppointmentDetails(req);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.totalCount);
            Assert.Empty(result.appointmentModels);
            _appointmentServiceMock.Verify(a => a.SetupRethinkDataForAppointments(It.IsAny<List<AppointmentRethinkModel>>()), Times.Never);
            _appointmentServiceMock.Verify(a => a.ToAppointmentItems(It.IsAny<int>(), It.IsAny<List<AppointmentRethinkModel>>(), It.IsAny<int>()), Times.Never);
        }
        [Fact]
        public async Task GetUnbilledAppointmentDetails_FilterByFunder_AppointmentLevelMatch()
        {
            // Arrange
            // Two deleted links (unbilled) with different funders; request filters to one funder.
            SetupService(
                new List<ClaimAppointmentLinkEntity>
                {
                    Link(1, 501, 10, 2001, 3001, ClaimStatus.PendingReview, deleted:true),
                    Link(2, 502, 10, 2002, 3002, ClaimStatus.PendingReview, deleted:true)
                },
                new List<AppointmentRethinkModel>
                {
                    // funderId matches request (3001)
                    Appt(500, 2001, 4001, 3001, 5001, 6001),
                    // funderId does not match request
                    Appt(502, 2002, 4002, 3002, 5002, 6002)
                });

            var req = new UnbilledAppointmentsRequestModel
            {
                AccountInfoId = 10,
                MemberId = 99,
                PayerOrFunder = new List<int> { 3001 }, // triggers second-stage funder filter
                Clients = new List<int>(),
                Staff = new List<int>(),
                Location = new List<int>(),
                PlaceOfService = new List<int>(),
                Skip = 0,
                Take = 0,
                SortingModels = new List<SortingModel>()
            };

            // Act
            var result = await _service.GetUnbilledAppointmentDetails(req);

            // Assert
            Assert.Equal(1, result.totalCount);
            Assert.Single(result.appointmentModels);
            Assert.Equal(500, result.appointmentModels[0].Id);
        }

        [Fact]
        public async Task GetUnbilledAppointmentDetails_FilterByFunder_AppointmentLevelEarlyEmpty()
        {
            // Arrange
            // Claim funders match filter so second-stage executes, but appointment funders all different -> early empty return in filter block.
            SetupService(
                new List<ClaimAppointmentLinkEntity>
                {
                    Link(1, 601, 10, 2101, 9999, ClaimStatus.PendingReview, deleted:true),
                    Link(2, 602, 10, 2102, 9999, ClaimStatus.PendingReview, deleted:true)
                },
                new List<AppointmentRethinkModel>
                {
                    Appt(601, 2101, 4101, 3001, 5001, 6001), // appointment funderId 3001 (not 9999)
                    Appt(602, 2102, 4102, 3002, 5002, 6002)  // appointment funderId 3002 (not 9999)
                });

            var req = new UnbilledAppointmentsRequestModel
            {
                AccountInfoId = 10,
                MemberId = 88,
                PayerOrFunder = new List<int> { 9999 }, // passes claim-level but fails appointment-level
                Clients = new List<int>(),
                Staff = new List<int>(),
                Location = new List<int>(),
                PlaceOfService = new List<int>(),
                Skip = 0,
                Take = 0,
                SortingModels = new List<SortingModel>()
            };

            // Act
            var result = await _service.GetUnbilledAppointmentDetails(req);

            // Assert
            Assert.Equal(0, result.totalCount);
            Assert.Empty(result.appointmentModels);
        }

        [Fact]
        public async Task GetUnbilledAppointmentDetails_SortingModels_SortsByStartDateDescending()
        {
            // Arrange
            var earlier = DateTime.UtcNow.Date.AddHours(8);
            var later = DateTime.UtcNow.Date.AddHours(10);

            SetupService(
                new List<ClaimAppointmentLinkEntity>
                {
                    Link(1, 701, 10, 2201, 3301, ClaimStatus.PendingReview, deleted:true),
                    Link(2, 702, 10, 2202, 3301, ClaimStatus.PendingReview, deleted:true)
                },
                new List<AppointmentRethinkModel>
                {
                    Appt(700, 2201, 4201, 3301, 5201, 6301, earlier),
                    Appt(500, 2202, 4202, 3301, 5202, 6302, later)
                });

            var req = new UnbilledAppointmentsRequestModel
            {
                AccountInfoId = 10,
                MemberId = 44,
                Clients = new List<int>(),
                PayerOrFunder = new List<int>(),
                Staff = new List<int>(),
                Location = new List<int>(),
                PlaceOfService = new List<int>(),
                Skip = 0,
                Take = 0,
                SortingModels = new List<SortingModel>
                {
                    new SortingModel { Field = "StartDate", Dir = "desc" }
                }
            };

            // Act
            var result = await _service.GetUnbilledAppointmentDetails(req);

            // Assert
            Assert.Equal(2, result.totalCount);
            Assert.Equal(500, result.appointmentModels[0].Id); // later first (desc)
            Assert.Equal(700, result.appointmentModels[1].Id);
        }

        [Fact]
        public async Task GetUnbilledAppointmentDetails_Paging_ReturnsSubset()
        {
            // Arrange
            SetupService(
                new List<ClaimAppointmentLinkEntity>
                {
                    Link(1, 101, 10, 201, 301, ClaimStatus.PendingReview, deleted:true),
                    Link(2, 102, 10, 202, 302, ClaimStatus.ReadyToBill, deleted:true)
                },
                new List<AppointmentRethinkModel>
                {
                    Appt(500, 201, 401, 301, 501, 601),
                    Appt(400, 202, 402, 302, 502, 602)
                });
            var req = new UnbilledAppointmentsRequestModel
            {
                AccountInfoId = 10,
                MemberId = 99,
                Clients = new List<int>(),
                PayerOrFunder = new List<int>(),
                Staff = new List<int>(),
                Location = new List<int>(),
                PlaceOfService = new List<int>(),
                Skip = 0,
                Take = 1,
                SortingModels = new List<SortingModel>()
            };
            // Act
            var result = await _service.GetUnbilledAppointmentDetails(req);
            // Assert
            Assert.Equal(2, result.totalCount);
            Assert.Single(result.appointmentModels);
            _appointmentServiceMock.Verify(a =>
                a.ToAppointmentItems(10, It.Is<List<AppointmentRethinkModel>>(l => l.Count == 1), 99),
                Times.Once);
        }

        [Fact]
        public async Task CreateClaimsForUnbilledAppointmentsAsync_SendsBusMessages()
        {
            // Arrange
            SetupService(new List<ClaimAppointmentLinkEntity>(), new List<AppointmentRethinkModel>());
            // Act
            var ok = await _service.CreateClaimsForUnbilledAppointmentsAsync(10, 20, new[] { 1, 2, 3 });
            // Assert
            Assert.True(ok);
            _claimSyncService
                .Verify(b => b.PublishUnbilledAppointmentForClaimProcessingAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
        }

        [Fact]
        public async Task ExportUnbilledAppointmentDataAsync_CreatesExcel()
        {
            // Arrange
            SetupService(
                new List<ClaimAppointmentLinkEntity> { Link(1, 500, 10, 700, 800, ClaimStatus.PendingReview, deleted: true) },
                new List<AppointmentRethinkModel> { Appt(500, 700, 400, 800, 900, 1000) });
            var exportModel = BuildExportModel(10, 99);
            // Act
            var bytes = await _service.ExportUnbilledAppointmentDataAsync(exportModel);
            // Assert
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);
            using var ms = new MemoryStream(bytes);
            using var doc = SpreadsheetDocument.Open(ms, false);
            var sheet = doc.WorkbookPart.Workbook.Sheets.OfType<Sheet>().FirstOrDefault();
            Assert.NotNull(sheet);
            Assert.Equal("Unbilled Appointment Reports", sheet.Name);
        }

        [Fact]
        public async Task ExportUnbilledAppointmentDataAsync_WhenMappingThrows_WrapsException()
        {
            // Arrange
            SetupService(
                new List<ClaimAppointmentLinkEntity> { Link(1, 900, 10, 700, 800, ClaimStatus.PendingReview, deleted: true) },
                new List<AppointmentRethinkModel> { Appt(900, 700, 400, 800, 900, 1000) });
            _appointmentServiceMock
                .Setup(a => a.SetupRethinkDataForAppointments(It.IsAny<List<AppointmentRethinkModel>>()))
                .ThrowsAsync(new InvalidOperationException("Boom"));
            var exportModel = BuildExportModel(10, 99);
            // Act / Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _service.ExportUnbilledAppointmentDataAsync(exportModel));
            Assert.Equal("An error occurred while generating the excel file.", ex.Message);
            Assert.IsType<InvalidOperationException>(ex.InnerException);
        }

        // Additional coverage
        [Fact]
        public async Task GetUnbilledAppointmentDetails_TakeZero_ReturnsAll()
        {
            // Arrange
            SetupService(
                new List<ClaimAppointmentLinkEntity>
                {
                    Link(1, 101, 10, 200, 300, ClaimStatus.PendingReview, true),
                    Link(2, 102, 10, 201, 301, ClaimStatus.ReadyToBill, true)
                },
                new List<AppointmentRethinkModel>
                {
                    Appt(500,200,400,300,500,600),
                    Appt(700,201,401,301,501,601)
                });
            var req = new UnbilledAppointmentsRequestModel { AccountInfoId = 10, MemberId = 50, Skip = 0, Take = 0, Clients = new(), PayerOrFunder = new(), Staff = new(), Location = new(), PlaceOfService = new(), SortingModels = new() };
            // Act
            var result = await _service.GetUnbilledAppointmentDetails(req);
            // Assert
            Assert.Equal(2, result.totalCount);
            Assert.Equal(2, result.appointmentModels.Count);
            _appointmentServiceMock.Verify(a =>
                a.ToAppointmentItems(10, It.Is<List<AppointmentRethinkModel>>(l => l.Count == 2), 50),
                Times.Once);
        }

        [Fact]
        public async Task GetUnbilledAppointmentDetails_SkipBeyondCount_ReturnsEmptyPaged()
        {
            // Arrange
            SetupService(
                new List<ClaimAppointmentLinkEntity> { Link(1, 200, 10, 300, 400, ClaimStatus.PendingReview, true) },
                new List<AppointmentRethinkModel> { Appt(400, 300, 500, 400, 600, 700) });
            var req = new UnbilledAppointmentsRequestModel { AccountInfoId = 10, MemberId = 88, Skip = 5, Take = 10, Clients = new(), PayerOrFunder = new(), Staff = new(), Location = new(), PlaceOfService = new(), SortingModels = new() };
            // Act
            var result = await _service.GetUnbilledAppointmentDetails(req);
            // Assert
            Assert.Equal(1, result.totalCount);
            Assert.Empty(result.appointmentModels);
        }

        [Fact]
        public async Task GetUnbilledAppointmentDetails_FilterByClient_ReturnsOnlyMatching()
        {
            // Arrange
            SetupService(
                new List<ClaimAppointmentLinkEntity>
                {
                    Link(1, 301, 10, 501, 601, ClaimStatus.PendingReview, true),
                    Link(2, 302, 10, 999, 602, ClaimStatus.ReadyToBill, true)
                },
                new List<AppointmentRethinkModel>
                {
                    Appt(500,501,700,601,800,900),
                    Appt(302,999,701,602,801,901)
                });
            var req = new UnbilledAppointmentsRequestModel { AccountInfoId = 10, MemberId = 12, Clients = new() { 501 }, PayerOrFunder = new(), Staff = new(), Location = new(), PlaceOfService = new(), SortingModels = new() };
            // Act
            var result = await _service.GetUnbilledAppointmentDetails(req);
            // Assert
            Assert.Equal(1, result.totalCount);
            Assert.Single(result.appointmentModels);
            Assert.Equal(500, result.appointmentModels[0].Id);
        }

        [Fact]
        public async Task GetUnbilledAppointmentDetails_MultipleFiltersResultEmpty()
        {
            // Arrange
            SetupService(
                new List<ClaimAppointmentLinkEntity>
                {
                    Link(1, 401, 10, 800, 900, ClaimStatus.PendingReview, true),
                    Link(2, 402, 10, 801, 901, ClaimStatus.ReadyToBill, true)
                },
                new List<AppointmentRethinkModel>
                {
                    Appt(401,800,1000,900,1100,1200),
                    Appt(402,801,1001,901,1101,1201)
                });
            var req = new UnbilledAppointmentsRequestModel
            {
                AccountInfoId = 10,
                MemberId = 15,
                Clients = new() { 999 },
                PayerOrFunder = new() { 1234 },
                Staff = new() { 7777 },
                Location = new() { 5555 },
                PlaceOfService = new() { 4444 },
                Skip = 0,
                Take = 0,
                SortingModels = new()
            };
            // Act
            var result = await _service.GetUnbilledAppointmentDetails(req);
            // Assert
            Assert.Equal(0, result.totalCount);
            Assert.Empty(result.appointmentModels);
        }

        [Fact]
        public async Task GetUnbilledAppointmentDetails_DuplicateLinksOnlyDeletedWithoutActiveIncluded()
        {
            // Arrange
            var links = new List<ClaimAppointmentLinkEntity>
            {
                Link(1, 501, 10, 600, 700, ClaimStatus.PendingReview, deleted:true),
                Link(2, 501, 10, 600, 700, ClaimStatus.PendingReview, deleted:false),
                Link(3, 502, 10, 601, 701, ClaimStatus.PendingReview, deleted:true)
            };
            var appts = new List<AppointmentRethinkModel> { Appt(500, 600, 800, 700, 900, 1000) };
            SetupService(links, appts);
            var req = new UnbilledAppointmentsRequestModel { AccountInfoId = 10, MemberId = 77, Clients = new(), PayerOrFunder = new(), Staff = new(), Location = new(), PlaceOfService = new(), Skip = 0, Take = 0, SortingModels = new() };
            // Act
            var result = await _service.GetUnbilledAppointmentDetails(req);
            // Assert
            Assert.Equal(1, result.totalCount);
            Assert.Single(result.appointmentModels);
            Assert.Equal(500, result.appointmentModels[0].Id);
        }

        [Fact]
        public async Task GetUnbilledAppointmentDetails_DateRangeFilter_Applies()
        {
            // Arrange
            var day = DateTime.UtcNow.Date;
            var apptInside = Appt(601, 1001, 2001,  500, 700, 400, day.AddHours(9), day.AddHours(10));
            var apptOutside = Appt(602, 1002, 2002, 502, 701, 400, day.AddDays(2).AddHours(9), day.AddDays(2).AddHours(10));
            SetupService(
                new List<ClaimAppointmentLinkEntity>
                {
                    Link(1, 601, 10, 1001, 3001, ClaimStatus.PendingReview, true),
                    Link(2, 603, 10, 1002, 3002, ClaimStatus.PendingReview, true)
                },
                new List<AppointmentRethinkModel> { apptInside, apptOutside });
            var req = new UnbilledAppointmentsRequestModel { AccountInfoId = 10, MemberId = 55, StartDate = day, EndDate = day, Clients = new(), PayerOrFunder = new(), Staff = new(), Location = new(), PlaceOfService = new(), Skip = 0, Take = 0, SortingModels = new() };
            // Act
            var result = await _service.GetUnbilledAppointmentDetails(req);
            // Assert
            Assert.Equal(1, result.totalCount);
            Assert.Single(result.appointmentModels);
            Assert.Equal(601, result.appointmentModels[0].Id);
        }

        [Fact]
        public async Task GetUnbilledAppointmentDetails_SetupRethinkThrows_Propagates()
        {
            // Arrange
            SetupService(
                new List<ClaimAppointmentLinkEntity> { Link(1, 700, 10, 800, 900, ClaimStatus.PendingReview, true) },
                new List<AppointmentRethinkModel> { Appt(700, 800, 1000, 900, 1100, 1200) });
            _appointmentServiceMock
                .Setup(a => a.SetupRethinkDataForAppointments(It.IsAny<List<AppointmentRethinkModel>>()))
                .ThrowsAsync(new InvalidOperationException("Enrichment failed"));
            var req = new UnbilledAppointmentsRequestModel { AccountInfoId = 10, MemberId = 5, Clients = new(), PayerOrFunder = new(), Staff = new(), Location = new(), PlaceOfService = new(), Skip = 0, Take = 0, SortingModels = new() };
            // Act / Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetUnbilledAppointmentDetails(req));
            Assert.Equal("Enrichment failed", ex.Message);
        }

        [Fact]
        public async Task CreateClaimsForUnbilledAppointmentsAsync_BusThrows_Propagates()
        {
            // Arrange
            SetupService(new List<ClaimAppointmentLinkEntity>(), new List<AppointmentRethinkModel>());

            _claimSyncService
                .Setup(b => b.PublishUnbilledAppointmentForClaimProcessingAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new ApplicationException("Bus failure"));

            // Act / Assert
            await Assert.ThrowsAsync<ApplicationException>(() =>
                _service.CreateClaimsForUnbilledAppointmentsAsync(10, 20, new[] { 99 }));
        }

        [Fact]
        public async Task CreateClaimsForUnbilledAppointmentsAsync_EmptyArray_NoMessages()
        {
            // Arrange
            SetupService(new List<ClaimAppointmentLinkEntity>(), new List<AppointmentRethinkModel>());
            // Act
            var ok = await _service.CreateClaimsForUnbilledAppointmentsAsync(10, 20, Array.Empty<int>());
            // Assert
            Assert.True(ok);

            _claimSyncService.Verify(b => b.PublishUnbilledAppointmentForClaimProcessingAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ExportUnbilledAppointmentDataAsync_NoAppointments_HeaderOnly()
        {
            // Arrange
            SetupService(
                new List<ClaimAppointmentLinkEntity>
                {
                    Link(1, 800, 10, 900, 1000, ClaimStatus.PendingReview, deleted:true),
                    Link(2, 800, 10, 900, 1000, ClaimStatus.PendingReview, deleted:false)
                },
                new List<AppointmentRethinkModel>());
            var exportModel = BuildExportModel(10, 99);
            // Act
            var bytes = await _service.ExportUnbilledAppointmentDataAsync(exportModel);
            // Assert
            using var ms = new MemoryStream(bytes);
            using var doc = SpreadsheetDocument.Open(ms, false);
            var sheetPart = doc.WorkbookPart.WorksheetParts.First();
            var sheetData = sheetPart.Worksheet.GetFirstChild<SheetData>();
            Assert.True(sheetData.Elements<Row>().Count() >= 1);
        }

        [Fact]
        public async Task ExportUnbilledAppointmentDataAsync_DefineStylesCalledOnce()
        {
            // Arrange
            SetupService(
                new List<ClaimAppointmentLinkEntity> { Link(1, 900, 10, 1000, 1100, ClaimStatus.PendingReview, true) },
                new List<AppointmentRethinkModel> { Appt(900, 1000, 1200, 1100, 1300, 1400) });
            var exportModel = BuildExportModel(10, 99);
            // Act
            await _service.ExportUnbilledAppointmentDataAsync(exportModel);
            // Assert
            _helperServiceMock.Verify(h => h.DefineStyles(It.IsAny<WorkbookPart>()), Times.Once);
        }

        [Fact]
        public async Task ExportUnbilledAppointmentDataAsync_HeaderHasExpectedColumns()
        {
            // Arrange
            SetupService(
                new List<ClaimAppointmentLinkEntity> { Link(1, 910, 10, 1000, 1100, ClaimStatus.PendingReview, true) },
                new List<AppointmentRethinkModel> { Appt(910, 1000, 1200, 1100, 1300, 1400) });
            var exportModel = BuildExportModel(10, 99);
            // Act
            var bytes = await _service.ExportUnbilledAppointmentDataAsync(exportModel);
            // Assert
            using var ms = new MemoryStream(bytes);
            using var doc = SpreadsheetDocument.Open(ms, false);
            var sheetPart = doc.WorkbookPart.WorksheetParts.First();
            var sheetData = sheetPart.Worksheet.GetFirstChild<SheetData>();
            var headerRow = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex == 10);
            Assert.NotNull(headerRow);
            Assert.Equal(11, headerRow.Elements<Cell>().Count());
        }

        [Fact]
        public async Task ExportUnbilledAppointmentDataAsync_FilterRowsContainValues()
        {
            // Arrange
            SetupService(new List<ClaimAppointmentLinkEntity>(), new List<AppointmentRethinkModel>());
            var exportModel = BuildExportModel(10, 42);
            exportModel.Filter.Clients.Add("123");
            exportModel.Filter.Staff.Add("456");
            exportModel.Filter.PayerOrFunder.Add("789");
            // Act
            var bytes = await _service.ExportUnbilledAppointmentDataAsync(exportModel);
            // Assert
            using var ms = new MemoryStream(bytes);
            using var doc = SpreadsheetDocument.Open(ms, false);
            var sheetPart = doc.WorkbookPart.WorksheetParts.First();
            var sheetData = sheetPart.Worksheet.GetFirstChild<SheetData>();
            var clientFilterRow = sheetData.Elements<Row>()
                .Select(r => r.Elements<Cell>().Select(c => c.CellValue?.Text).ToList())
                .FirstOrDefault(vals => vals.Any(v => v == "Client IDs"));
            Assert.NotNull(clientFilterRow);
            Assert.Contains("123", clientFilterRow);
        }

        // Helpers
        private ExportModelForUnbilledAppointments BuildExportModel(int accountInfoId, int memberId)
        {
            return new ExportModelForUnbilledAppointments
            {
                Filter = new UnbilledAppointmentsRequestModelForExport
                {
                    AccountInfoId = accountInfoId,
                    MemberId = memberId,
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = DateTime.UtcNow.Date.AddDays(1),
                    Clients = new List<string>(),
                    Staff = new List<string>(),
                    PayerOrFunder = new List<string>(),
                    PlaceOfService = new List<string>(),
                    Location = new List<string>()
                },
                Model = new UnbilledAppointmentsRequestModel
                {
                    AccountInfoId = accountInfoId,
                    MemberId = memberId,
                    Clients = new List<int>(),
                    PayerOrFunder = new List<int>(),
                    Staff = new List<int>(),
                    Location = new List<int>(),
                    PlaceOfService = new List<int>(),
                    Skip = 0,
                    Take = 0,
                    SortingModels = new List<SortingModel>()
                }
            };
        }

        [Fact]
        public async Task GetUnbilledAppointmentDetails_NoUnbilledIds_ReturnsEmpty()
        {
            // Arrange
            SetupService(
                new List<ClaimAppointmentLinkEntity>
                {
                    Link(1, 1001, 10, 2001, 3001, ClaimStatus.PendingReview, deleted:false),
                    Link(2, 1002, 10, 2002, 3002, ClaimStatus.ReadyToBill, deleted:false)
                },
                new List<AppointmentRethinkModel>());
            var req = new UnbilledAppointmentsRequestModel { AccountInfoId = 10, MemberId = 99, Clients = new(), PayerOrFunder = new(), Staff = new(), Location = new(), PlaceOfService = new(), Skip = 0, Take = 0, SortingModels = new() };
            // Act
            var result = await _service.GetUnbilledAppointmentDetails(req);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.totalCount);
            Assert.Empty(result.appointmentModels);
            _appointmentServiceMock.Verify(a => a.SetupRethinkDataForAppointments(It.IsAny<List<AppointmentRethinkModel>>()), Times.Never);
            _appointmentServiceMock.Verify(a => a.ToAppointmentItems(It.IsAny<int>(), It.IsAny<List<AppointmentRethinkModel>>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetUnbilledAppointmentDetails_FilterByFunder_EarlyEmpty()
        {
            // Arrange
            SetupService(
                new List<ClaimAppointmentLinkEntity> { Link(1, 1101, 10, 501, 601, ClaimStatus.PendingReview, deleted: true) },
                new List<AppointmentRethinkModel> { Appt(1101, 501, 700, 601, 800, 900) });
            var req = new UnbilledAppointmentsRequestModel { AccountInfoId = 10, MemberId = 1, Clients = new(), PayerOrFunder = new() { 999 }, Staff = new(), Location = new(), PlaceOfService = new(), Skip = 0, Take = 0, SortingModels = new() };
            // Act
            var result = await _service.GetUnbilledAppointmentDetails(req);
            // Assert
            Assert.Equal(0, result.totalCount);
            Assert.Empty(result.appointmentModels);
        }

        [Fact]
        public async Task GetUnbilledAppointmentDetails_FilterByStaff_EarlyEmpty()
        {
            // Arrange
            SetupService(
                new List<ClaimAppointmentLinkEntity> { Link(1, 1201, 10, 601, 701, ClaimStatus.PendingReview, deleted: true) },
                new List<AppointmentRethinkModel> { Appt(1201, 601, 800, 701, 900, 1000) });
            var req = new UnbilledAppointmentsRequestModel { AccountInfoId = 10, MemberId = 2, Staff = new() { 999 }, Clients = new(), PayerOrFunder = new(), Location = new(), PlaceOfService = new(), Skip = 0, Take = 0, SortingModels = new() };
            // Act
            var result = await _service.GetUnbilledAppointmentDetails(req);
            // Assert
            Assert.Equal(0, result.totalCount);
        }

        [Fact]
        public async Task GetUnbilledAppointmentDetails_FilterByLocation_EarlyEmpty()
        {
            // Arrange
            SetupService(
                new List<ClaimAppointmentLinkEntity> { Link(1, 1301, 10, 701, 801, ClaimStatus.PendingReview, deleted: true) },
                new List<AppointmentRethinkModel> { Appt(1301, 701, 900, 801, 1000, 1100) });
            var req = new UnbilledAppointmentsRequestModel { AccountInfoId = 10, MemberId = 3, Location = new() { 999 }, Clients = new(), PayerOrFunder = new(), Staff = new(), PlaceOfService = new(), Skip = 0, Take = 0, SortingModels = new() };
            // Act
            var result = await _service.GetUnbilledAppointmentDetails(req);
            // Assert
            Assert.Equal(0, result.totalCount);
        }

        [Fact]
        public async Task GetUnbilledAppointmentDetails_FilterByPlaceOfService_EarlyEmpty()
        {
            // Arrange
            SetupService(
                new List<ClaimAppointmentLinkEntity> { Link(1, 1401, 10, 801, 901, ClaimStatus.PendingReview, deleted: true) },
                new List<AppointmentRethinkModel> { Appt(1401, 801, 1000, 901, 1100, 1200) });
            var req = new UnbilledAppointmentsRequestModel { AccountInfoId = 10, MemberId = 4, PlaceOfService = new() { 999 }, Clients = new(), PayerOrFunder = new(), Staff = new(), Location = new(), Skip = 0, Take = 0, SortingModels = new() };
            // Act
            var result = await _service.GetUnbilledAppointmentDetails(req);
            // Assert
            Assert.Equal(0, result.totalCount);
        }

        [Fact]
        public async Task GetUnbilledAppointmentDetails_DateRange_NoMatch_EarlyEmpty()
        {
            // Arrange
            var start = DateTime.UtcNow.Date.AddDays(-10);
            var end = DateTime.UtcNow.Date.AddDays(-9);
            SetupService(
                new List<ClaimAppointmentLinkEntity> { Link(1, 1501, 10, 901, 1001, ClaimStatus.PendingReview, deleted: true) },
                new List<AppointmentRethinkModel> { Appt(1501, 901, 1100, 1001, 1200, 1300) });
            var req = new UnbilledAppointmentsRequestModel { AccountInfoId = 10, MemberId = 5, StartDate = start, EndDate = end, Clients = new(), PayerOrFunder = new(), Staff = new(), Location = new(), PlaceOfService = new(), Skip = 0, Take = 0, SortingModels = new() };
            // Act
            var result = await _service.GetUnbilledAppointmentDetails(req);
            // Assert
            Assert.Equal(0, result.totalCount);
        }

        [Fact]
        public async Task GetUnbilledAppointmentDetails_SkipAndTake_SecondItemReturned()
        {
            // Arrange
            SetupService(
                new List<ClaimAppointmentLinkEntity>
                {
                    Link(1, 1601, 10, 1001, 1101, ClaimStatus.PendingReview, true),
                    Link(2, 1602, 10, 1002, 1102, ClaimStatus.ReadyToBill, true)
                },
                new List<AppointmentRethinkModel>
                {
                    Appt(400,1001,1201,1101,1301,1401),
                    Appt(700,1002,1202,1102,1302,1402)
                });
            var req = new UnbilledAppointmentsRequestModel { AccountInfoId = 10, MemberId = 6, Skip = 1, Take = 1, Clients = new(), PayerOrFunder = new(), Staff = new(), Location = new(), PlaceOfService = new(), SortingModels = new() };
            // Act
            var result = await _service.GetUnbilledAppointmentDetails(req);
            // Assert
            Assert.Equal(2, result.totalCount);
            Assert.Single(result.appointmentModels);
            Assert.Equal(700, result.appointmentModels[0].Id);
        }

        [Fact]
        public async Task ExportUnbilledAppointmentDataAsync_ToAppointmentItemsThrows_WrapsException()
        {
            // Arrange
            SetupService(
                new List<ClaimAppointmentLinkEntity> { Link(1, 1701, 10, 2001, 3001, ClaimStatus.PendingReview, true) },
                new List<AppointmentRethinkModel> { Appt(1701, 2001, 4001, 3001, 5001, 6001) });
            _appointmentServiceMock
                .Setup(a => a.ToAppointmentItems(It.IsAny<int>(), It.IsAny<List<AppointmentRethinkModel>>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("MapFail"));
            var exportModel = BuildExportModel(10, 99);
            // Act / Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _service.ExportUnbilledAppointmentDataAsync(exportModel));
            Assert.Equal("An error occurred while generating the excel file.", ex.Message);
            Assert.IsType<InvalidOperationException>(ex.InnerException);
        }

        [Fact]
        public async Task GetUnbilledAppointmentDetails_NoFiltersMappingCalledOnce()
        {
            // Arrange
            SetupService(
                new List<ClaimAppointmentLinkEntity> { Link(1, 1801, 10, 3001, 4001, ClaimStatus.PendingReview, true) },
                new List<AppointmentRethinkModel> { Appt(700, 3001, 5001, 4001, 6001, 7001) });
            var req = new UnbilledAppointmentsRequestModel { AccountInfoId = 10, MemberId = 7, Clients = new(), PayerOrFunder = new(), Staff = new(), Location = new(), PlaceOfService = new(), Skip = 0, Take = 0, SortingModels = new() };
            // Act
            var result = await _service.GetUnbilledAppointmentDetails(req);
            // Assert
            Assert.Equal(1, result.totalCount);
            _appointmentServiceMock.Verify(a =>
                a.ToAppointmentItems(10, It.Is<List<AppointmentRethinkModel>>(l => l.Count == 1), 7),
                Times.Once);
        }
    }
}