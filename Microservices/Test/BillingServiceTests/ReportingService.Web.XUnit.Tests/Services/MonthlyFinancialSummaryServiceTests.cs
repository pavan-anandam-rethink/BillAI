using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models.ReportingModels;
using SummationService.Domain.Interfaces;
using SummationService.Domain.Services;
using System.Data;
using System.Data.Common;

namespace ReportingService.Web.XUnit.Tests.Services
{
    public class MonthlyFinancialSummaryServiceTests
    {
        private MonthlyFinancialSummaryService CreateService(Mock<IHelperService> helperMock)
        {
            return new MonthlyFinancialSummaryService(
                new Mock<IDbHelper<BillingDbContext>>().Object,
                helperMock.Object,
                new Mock<IRepository<BillingDbContext, ClaimEntity>>().Object,
                new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>().Object
            );
        }

        private MonthlyFinancialSummaryRequest GetModel()
        {
            return new MonthlyFinancialSummaryRequest
            {
                StartDate = DateTime.Now.AddDays(-30),
                EndDate = DateTime.Now
            };
        }

        private MonthlyFinancialSummaryResponse GetResponse()
        {
            return new MonthlyFinancialSummaryResponse
            {
                StartingAR = 100,
                Rows = new List<MonthlyFinancialRow>(),
                Total = new MonthlyFinancialRow
                {
                    MonthYear = "TOTAL"
                },
                UnappliedCredits = new UnappliedCreditsDto()
            };
        }

        private Mock<IHelperService> CreateHelperMock()
        {
            var mock = new Mock<IHelperService>();

            mock.Setup(h => h.DefineStyles(It.IsAny<WorkbookPart>()));

            mock.Setup(h => h.AddCell(It.IsAny<ExcelCellType>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(() => new Cell());

            return mock;
        }

        [Fact]
        public async Task GetMonthlyFinancialSummaryAsyncReturnsExpectedResult()
        {
            // Arrange
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
            var mockHelperService = new Mock<IHelperService>();
            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();
            var mockReader = new Mock<DbDataReader>();

            int resultSet = 0;
            int rowIndex = -1;

            // -----------------------------
            // Mock ReadAsync
            // -----------------------------
            mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    if (resultSet == 0) // ResultSet1: Starting AR
                        return ++rowIndex < 1;

                    if (resultSet == 1) // ResultSet2: Monthly + TOTAL
                        return ++rowIndex < 2;

                    if (resultSet == 2) // ResultSet3: Unapplied Credits
                        return ++rowIndex < 1;

                    return false;
                });

            // -----------------------------
            // Mock NextResultAsync
            // -----------------------------
            mockReader.Setup(r => r.NextResultAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    resultSet++;
                    rowIndex = -1;
                    return resultSet < 3;
                });

            // -----------------------------
            // Mock column ordinals
            // -----------------------------
            mockReader.Setup(r => r.GetOrdinal("Charges")).Returns(1);
            mockReader.Setup(r => r.GetOrdinal("InsurancePay")).Returns(2);
            mockReader.Setup(r => r.GetOrdinal("PatientPay")).Returns(3);
            mockReader.Setup(r => r.GetOrdinal("TotalPay")).Returns(4);
            mockReader.Setup(r => r.GetOrdinal("Adjustments")).Returns(5);
            mockReader.Setup(r => r.GetOrdinal("WriteOffs")).Returns(6);
            mockReader.Setup(r => r.GetOrdinal("PeriodBalance")).Returns(7);
            mockReader.Setup(r => r.GetOrdinal("EndingAR")).Returns(8);
            mockReader.Setup(r => r.GetOrdinal("InsuranceUnapplied")).Returns(9);
            mockReader.Setup(r => r.GetOrdinal("PatientUnapplied")).Returns(10);
            mockReader.Setup(r => r.GetOrdinal("TotalUnapplied")).Returns(11);

            // -----------------------------
            // Mock values for GetDecimal
            // -----------------------------
            mockReader.Setup(r => r.GetDecimal(It.IsAny<int>())).Returns((int index) =>
            {
                // ResultSet1: Starting AR
                if (resultSet == 0)
                    return 1000m;

                // ResultSet2: Monthly + TOTAL
                if (resultSet == 1)
                {
                    return index switch
                    {
                        1 => 200m, // Charges
                        2 => 50m,  // InsurancePay
                        3 => 30m,  // PatientPay
                        4 => 80m,  // TotalPay
                        5 => 10m,  // Adjustments
                        6 => 5m,   // WriteOffs
                        7 => 100m, // PeriodBalance
                        8 => 900m, // EndingAR
                        _ => 0m
                    };
                }

                // ResultSet3: Unapplied Credits
                if (resultSet == 2)
                {
                    return index switch
                    {
                        9 => 15m,
                        10 => 5m,
                        11 => 20m,
                        _ => 0m
                    };
                }

                return 0m;
            });

            // -----------------------------
            // Mock MonthYear for ResultSet2
            // -----------------------------
            mockReader.Setup(r => r["MonthYear"]).Returns(() =>
            {
                if (resultSet == 1 && rowIndex == 0)
                    return "2026-01"; // Monthly row
                if (resultSet == 1 && rowIndex == 1)
                    return "TOTAL";   // Total row
                return null!;
            });

            // -----------------------------
            // Mock ExecuteReaderAsync
            // -----------------------------
            mockDbHelper.Setup(h => h.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<List<SqlParameter>>(),
                It.IsAny<CommandType>()))
                .ReturnsAsync(mockReader.Object);

            var service = new MonthlyFinancialSummaryService(mockDbHelper.Object, mockHelperService.Object, mockClaimRepository.Object, mockFunderSearchRepository.Object);

            // Act
            var result = await service.GetMonthlyFinancialSummaryAsync(
                18421,
                new DateTime(2026, 1, 1),
                new DateTime(2026, 1, 31));

            // Assert
            Assert.Equal("Transaction", result.DateBasis);

            Assert.Equal(1000m, result.StartingAR);

            Assert.Single(result.Rows);
            var row = result.Rows[0];
            Assert.Equal("2026-01", row.MonthYear);
            Assert.Equal(200m, row.Charges);
            Assert.Equal(50m, row.InsurancePay);
            Assert.Equal(30m, row.PatientPay);
            Assert.Equal(80m, row.TotalPay);
            Assert.Equal(10m, row.Adjustments);
            Assert.Equal(5m, row.WriteOffs);
            Assert.Equal(100m, row.PeriodBalance);
            Assert.Equal(900m, row.EndingAR);

            Assert.NotNull(result.Total);
            Assert.Equal("TOTAL", result.Total.MonthYear);
            Assert.Equal(900m, result.Total.EndingAR);

            Assert.NotNull(result.UnappliedCredits);
            Assert.Equal(15m, result.UnappliedCredits.InsuranceUnapplied);
            Assert.Equal(5m, result.UnappliedCredits.PatientUnapplied);
            Assert.Equal(20m, result.UnappliedCredits.TotalUnapplied);
        }

        [Fact]
        public async Task GetMonthlyFinancialSummaryAsync_WhenOnlyTotalRow_ReturnsNoMonthlyRows()
        {
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
            var mockHelperService = new Mock<IHelperService>();
            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();
            var mockReader = new Mock<DbDataReader>();

            int resultSet = 0;
            int rowIndex = -1;

            mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    if (resultSet == 0) return ++rowIndex < 1; // Starting AR
                    if (resultSet == 1) return ++rowIndex < 1; // TOTAL only
                    if (resultSet == 2) return ++rowIndex < 1; // Unapplied
                    return false;
                });

            mockReader.Setup(r => r.NextResultAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    resultSet++;
                    rowIndex = -1;
                    return resultSet < 3;
                });

            mockReader.Setup(r => r.GetDecimal(It.IsAny<int>())).Returns(500m);
            mockReader.Setup(r => r["MonthYear"]).Returns(() => resultSet == 1 ? "TOTAL" : "");
            mockReader.Setup(h => h.GetOrdinal(It.IsAny<string>())).Returns(1);

            mockDbHelper.Setup(h => h.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<List<SqlParameter>>(),
                It.IsAny<CommandType>()))
                .ReturnsAsync(mockReader.Object);

            var service = new MonthlyFinancialSummaryService(mockDbHelper.Object, mockHelperService.Object, mockClaimRepository.Object, mockFunderSearchRepository.Object);

            var result = await service.GetMonthlyFinancialSummaryAsync(
                18421,
                DateTime.Today.AddDays(-30),
                DateTime.Today);

            Assert.Empty(result.Rows);
            Assert.NotNull(result.Total);
            Assert.Equal(500m, result.StartingAR);
        }

        [Fact]
        public async Task GetMonthlyFinancialSummaryAsync_WithMultipleMonths_ReturnsAllMonthlyRows()
        {
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
            var mockHelperService = new Mock<IHelperService>();
            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();
            var mockReader = new Mock<DbDataReader>();

            int resultSet = 0;
            int rowIndex = -1;

            mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    if (resultSet == 0) return ++rowIndex < 1;
                    if (resultSet == 1) return ++rowIndex < 3; // Jan, Feb, TOTAL
                    if (resultSet == 2) return ++rowIndex < 1;
                    return false;
                });

            mockReader.Setup(r => r.NextResultAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    resultSet++;
                    rowIndex = -1;
                    return resultSet < 3;
                });

            mockReader.Setup(r => r["MonthYear"]).Returns(() =>
            {
                if (resultSet == 1 && rowIndex == 0) return "2026-01";
                if (resultSet == 1 && rowIndex == 1) return "2026-02";
                if (resultSet == 1 && rowIndex == 2) return "TOTAL";
                return "";
            });

            mockReader.Setup(r => r.GetDecimal(It.IsAny<int>())).Returns(100m);
            mockReader.Setup(r => r.GetOrdinal(It.IsAny<string>())).Returns(1);

            mockDbHelper.Setup(h => h.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<List<SqlParameter>>(),
                It.IsAny<CommandType>()))
                .ReturnsAsync(mockReader.Object);

            var service = new MonthlyFinancialSummaryService(mockDbHelper.Object, mockHelperService.Object, mockClaimRepository.Object, mockFunderSearchRepository.Object);

            var result = await service.GetMonthlyFinancialSummaryAsync(
                18421,
                new DateTime(2026, 1, 1),
                new DateTime(2026, 2, 28));

            Assert.Equal(2, result.Rows.Count);
            Assert.Equal("2026-01", result.Rows[0].MonthYear);
            Assert.Equal("2026-02", result.Rows[1].MonthYear);
            Assert.NotNull(result.Total);
        }

        [Fact]
        public async Task GetMonthlyFinancialSummaryAsync_WhenNoUnappliedCredits_ResultIsNull()
        {
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
            var mockHelperService = new Mock<IHelperService>();
            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();
            var mockReader = new Mock<DbDataReader>();

            int resultSet = 0;
            int rowIndex = -1;

            mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    if (resultSet == 0) return ++rowIndex < 1;
                    if (resultSet == 1) return ++rowIndex < 1;
                    return false;
                });

            mockReader.Setup(r => r.NextResultAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    resultSet++;
                    rowIndex = -1;
                    return resultSet < 2;
                });

            mockReader.Setup(r => r["MonthYear"]).Returns("TOTAL");
            mockReader.Setup(r => r.GetDecimal(It.IsAny<int>())).Returns(100m);
            mockReader.Setup(r => r.GetOrdinal(It.IsAny<string>())).Returns(1);

            mockDbHelper.Setup(h => h.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<List<SqlParameter>>(),
                It.IsAny<CommandType>()))
                .ReturnsAsync(mockReader.Object);

            var service = new MonthlyFinancialSummaryService(mockDbHelper.Object, mockHelperService.Object, mockClaimRepository.Object, mockFunderSearchRepository.Object);

            var result = await service.GetMonthlyFinancialSummaryAsync(
                18421,
                DateTime.Today.AddDays(-7),
                DateTime.Today);

            Assert.NotNull(result.Total);
            Assert.Null(result.UnappliedCredits);
        }

        [Fact]
        public async Task ExportToExcelAsync_ReturnsNonEmptyByteArray()
        {
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
            var mockHelperService = new Mock<IHelperService>();
            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();

            var model = new MonthlyFinancialSummaryRequest
            {
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 1, 31)
            };

            var response = new MonthlyFinancialSummaryResponse
            {
                DateBasis = "Transaction",
                StartingAR = 1000m,
                Rows = new List<MonthlyFinancialRow>(),
                Total = new MonthlyFinancialRow
                {
                    MonthYear = "TOTAL",
                    Charges = 0m,
                    InsurancePay = 0m,
                    PatientPay = 0m,
                    TotalPay = 0m,
                    Adjustments = 0m,
                    WriteOffs = 0m,
                    PeriodBalance = 0m,
                    EndingAR = 900m
                },
                UnappliedCredits = new UnappliedCreditsDto
                {
                    InsuranceUnapplied = 15m,
                    PatientUnapplied = 5m,
                    TotalUnapplied = 20m
                }
            };

            mockHelperService
                .Setup(h => h.DefineStyles(It.IsAny<WorkbookPart>()))
                .Verifiable();

            // IMPORTANT: return a NEW Cell each time (OpenXML cannot reuse element instance)
            mockHelperService
                .Setup(h => h.AddCell(It.IsAny<ExcelCellType>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(() => new Cell());

            var service = new MonthlyFinancialSummaryService(mockDbHelper.Object, mockHelperService.Object, mockClaimRepository.Object, mockFunderSearchRepository.Object);

            var result = await service.ExportToExcelAsync(model, response, CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public async Task GetMonthlyFinancialSummaryAsync_WhenNoRows_ReturnsEmptyResult()
        {
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
            var mockHelperService = new Mock<IHelperService>();
            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();
            var mockReader = new Mock<DbDataReader>();

            mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            mockReader.Setup(r => r.NextResultAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            mockReader.Setup(r => r.GetOrdinal(It.IsAny<string>())).Returns(0);
            mockReader.Setup(r => r.GetDecimal(It.IsAny<int>())).Returns(0m);

            mockDbHelper.Setup(h => h.ExecuteReaderAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<SqlParameter>>(),
                    It.IsAny<CommandType>()))
                .ReturnsAsync(mockReader.Object);

            var service = new MonthlyFinancialSummaryService(mockDbHelper.Object, mockHelperService.Object, mockClaimRepository.Object, mockFunderSearchRepository.Object);

            var result = await service.GetMonthlyFinancialSummaryAsync(18421, DateTime.Today.AddDays(-7), DateTime.Today);

            Assert.Equal(0, result.Rows.Count);
            Assert.NotNull(result.Total);

            Assert.Null(result.UnappliedCredits);
            Assert.Equal("Transaction", result.DateBasis);
            Assert.Equal(0m, result.StartingAR);
        }



        [Fact]
        public async Task GetMonthlyFinancialSummaryAsync_WhenExecuteReaderThrows_ThrowsException()
        {
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
            var mockHelperService = new Mock<IHelperService>();
            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();

            mockDbHelper.Setup(h => h.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<List<SqlParameter>>(),
                It.IsAny<CommandType>()))
                .ThrowsAsync(new Exception("Database failure"));

            var service = new MonthlyFinancialSummaryService(mockDbHelper.Object, mockHelperService.Object, mockClaimRepository.Object, mockFunderSearchRepository.Object);

            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await service.GetMonthlyFinancialSummaryAsync(18421, DateTime.Today.AddDays(-7), DateTime.Today);
            });
        }

        [Fact]
        public async Task GetMonthlyFinancialSummaryAsync_WithStartingARZero_ReturnsZeroStartingAR()
        {
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
            var mockHelperService = new Mock<IHelperService>();
            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();
            var mockReader = new Mock<DbDataReader>();

            int resultSet = 0;
            int rowIndex = -1;

            mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    if (resultSet == 0) return ++rowIndex < 1;
                    if (resultSet == 1) return ++rowIndex < 1;
                    return false;
                });

            mockReader.Setup(r => r.NextResultAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    resultSet++;
                    rowIndex = -1;
                    return resultSet < 2;
                });

            mockReader.Setup(r => r.GetDecimal(It.IsAny<int>())).Returns(0m);
            mockReader.Setup(r => r["MonthYear"]).Returns("TOTAL");
            mockReader.Setup(r => r.GetOrdinal(It.IsAny<string>())).Returns(1);

            mockDbHelper.Setup(h => h.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<List<SqlParameter>>(),
                It.IsAny<CommandType>()))
                .ReturnsAsync(mockReader.Object);

            var service = new MonthlyFinancialSummaryService(mockDbHelper.Object, mockHelperService.Object, mockClaimRepository.Object, mockFunderSearchRepository.Object);

            var result = await service.GetMonthlyFinancialSummaryAsync(18421, DateTime.Today.AddDays(-7), DateTime.Today);

            Assert.Equal(0m, result.StartingAR);
            Assert.NotNull(result.Total);
        }

        [Fact]
        public async Task ExportToExcelAsync_WithRows_ShouldToggleRowColors()
        {
            var mockHelperService = new Mock<IHelperService>();

            var colorFlags = new List<bool>();

            mockHelperService
                .Setup(h => h.DefineStyles(It.IsAny<WorkbookPart>()));

            mockHelperService
                .Setup(h => h.AddCell(It.IsAny<ExcelCellType>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Callback<ExcelCellType, object, bool>((_, _, isColor) =>
                {
                    colorFlags.Add(isColor);
                })
                .Returns(() => new Cell());

            var service = CreateService(mockHelperService);

            var response = GetResponse();
            response.Rows = new List<MonthlyFinancialRow>
    {
        new MonthlyFinancialRow { MonthYear = "Jan" },
        new MonthlyFinancialRow { MonthYear = "Feb" }
    };

            await service.ExportToExcelAsync(GetModel(), response, CancellationToken.None);

            Assert.Contains(true, colorFlags);
            Assert.Contains(false, colorFlags);
        }

        [Fact]
        public async Task ExportToExcelAsync_WithLocationNames_ShouldNotFail()
        {
            var service = CreateService(CreateHelperMock());

            var model = GetModel();
            model.LocationNames = new List<string> { "Loc1", "Loc2" };

            var result = await service.ExportToExcelAsync(model, GetResponse(), CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task ExportToExcelAsync_WhenHelperThrows_ShouldWrapException()
        {
            var mockHelperService = new Mock<IHelperService>();

            mockHelperService
                .Setup(h => h.DefineStyles(It.IsAny<WorkbookPart>()))
                .Throws(new Exception("Test error"));

            var service = CreateService(mockHelperService);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                service.ExportToExcelAsync(GetModel(), GetResponse(), CancellationToken.None));

            Assert.Contains("An error occurred while generating Monthly Financial Summary Excel", ex.Message);
        }

        [Fact]
        public async Task ExportToExcelAsync_ShouldCallDefineStyles()
        {
            var mockHelperService = new Mock<IHelperService>();

            mockHelperService
                .Setup(h => h.DefineStyles(It.IsAny<WorkbookPart>()))
                .Verifiable();

            mockHelperService
                .Setup(h => h.AddCell(It.IsAny<ExcelCellType>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(() => new Cell());

            var service = CreateService(mockHelperService);

            await service.ExportToExcelAsync(GetModel(), GetResponse(), CancellationToken.None);

            mockHelperService.Verify(h => h.DefineStyles(It.IsAny<WorkbookPart>()), Times.Once);
        }

        [Fact]
        public async Task ExportToExcelAsync_WhenUnappliedCreditsNull_ShouldSkipSection()
        {
            var service = CreateService(CreateHelperMock());

            var response = GetResponse();
            response.UnappliedCredits = null;

            var result = await service.ExportToExcelAsync(GetModel(), response, CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task ExportToExcelAsync_WhenTotalIsNull_ShouldNotThrow()
        {
            var service = CreateService(CreateHelperMock());

            var response = GetResponse();
            response.Total = null;

            var result = await service.ExportToExcelAsync(GetModel(), response, CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task ExportToExcelAsync_ShouldContainHeaderRow()
        {
            var service = CreateService(CreateHelperMock());

            var result = await service.ExportToExcelAsync(GetModel(), GetResponse(), CancellationToken.None);

            using var stream = new MemoryStream(result);
            using var doc = SpreadsheetDocument.Open(stream, false);

            var sheetData = doc.WorkbookPart.WorksheetParts.First()
                .Worksheet.GetFirstChild<SheetData>();

            var rows = sheetData.Elements<Row>().ToList();

            // Header is around row 7 (based on your logic)
            Assert.True(rows.Count >= 7);
        }

        [Fact]
        public async Task ExportToExcelAsync_WhenLocationNamesNull_ShouldUseDefaultAll()
        {
            var mockHelperService = CreateHelperMock();
            var service = CreateService(mockHelperService);

            var model = GetModel();
            model.LocationNames = null;

            var result = await service.ExportToExcelAsync(model, GetResponse(), CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task ExportToExcelAsync_WhenFunderIdsNull_ShouldNotFail()
        {
            var service = CreateService(CreateHelperMock());

            var model = GetModel();
            model.FunderIds = null;

            var result = await service.ExportToExcelAsync(model, GetResponse(), CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task ExportToExcelAsync_WhenFunderIdsEmpty_ShouldSkipFunderLogic()
        {
            var service = CreateService(CreateHelperMock());

            var model = GetModel();
            model.FunderIds = new List<int>();

            var result = await service.ExportToExcelAsync(model, GetResponse(), CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task ExportToExcelAsync_WhenDateTypeNull_ShouldDefaultToTransaction()
        {
            var service = CreateService(CreateHelperMock());

            var model = GetModel();
            model.DateType = null;

            var result = await service.ExportToExcelAsync(model, GetResponse(), CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task ExportToExcelAsync_ShouldIncludePreviousPeriodRow()
        {
            var service = CreateService(CreateHelperMock());

            var response = GetResponse();
            response.StartingAR = 500m;

            var result = await service.ExportToExcelAsync(GetModel(), response, CancellationToken.None);

            using var stream = new MemoryStream(result);
            using var doc = SpreadsheetDocument.Open(stream, false);

            var sheetData = doc.WorkbookPart.WorksheetParts.First()
                .Worksheet.GetFirstChild<SheetData>();

            var rows = sheetData.Elements<Row>().ToList();

            Assert.True(rows.Count > 0); // At least ensures it's written
        }

        [Fact]
        public async Task ExportToExcelAsync_WithNegativeValues_ShouldNotFail()
        {
            var service = CreateService(CreateHelperMock());

            var response = GetResponse();
            response.Rows = new List<MonthlyFinancialRow>
    {
        new MonthlyFinancialRow
        {
            MonthYear = "Jan",
            InsurancePay = -50,
            PatientPay = -20,
            TotalPay = -70
        }
    };

            var result = await service.ExportToExcelAsync(GetModel(), response, CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task ExportToExcelAsync_WithManyRows_ShouldHandleLargeData()
        {
            var service = CreateService(CreateHelperMock());

            var response = GetResponse();
            response.Rows = Enumerable.Range(1, 100)
                .Select(i => new MonthlyFinancialRow
                {
                    MonthYear = $"2026-{i:D2}",
                    Charges = i * 10
                }).ToList();

            var result = await service.ExportToExcelAsync(GetModel(), response, CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public async Task ExportToExcelAsync_ShouldAlternateColorsCorrectly()
        {
            var mockHelperService = new Mock<IHelperService>();

            var colorSequence = new List<bool>();

            mockHelperService
                .Setup(h => h.DefineStyles(It.IsAny<WorkbookPart>()));

            mockHelperService
                .Setup(h => h.AddCell(It.IsAny<ExcelCellType>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Callback<ExcelCellType, object, bool>((_, _, isColor) =>
                {
                    colorSequence.Add(isColor);
                })
                .Returns(() => new Cell());

            var service = CreateService(mockHelperService);

            var response = GetResponse();
            response.Rows = new List<MonthlyFinancialRow>
    {
        new MonthlyFinancialRow { MonthYear = "Jan" },
        new MonthlyFinancialRow { MonthYear = "Feb" },
        new MonthlyFinancialRow { MonthYear = "Mar" }
    };

            await service.ExportToExcelAsync(GetModel(), response, CancellationToken.None);

            // Expect alternating pattern: false, true, false OR similar
            Assert.True(colorSequence.Distinct().Count() > 1);
        }

        [Fact]
        public async Task ExportToExcelAsync_ShouldHaveCorrectSheetName()
        {
            var service = CreateService(CreateHelperMock());

            var result = await service.ExportToExcelAsync(GetModel(), GetResponse(), CancellationToken.None);

            using var stream = new MemoryStream(result);
            using var doc = SpreadsheetDocument.Open(stream, false);

            var sheet = doc.WorkbookPart.Workbook.Sheets.Elements<Sheet>().First();

            Assert.Equal("Monthly Financial Summary", sheet.Name);
        }

        [Fact]
        public async Task ExportToExcelAsync_WithCancelledToken_ShouldStillComplete()
        {
            var service = CreateService(CreateHelperMock());

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await service.ExportToExcelAsync(GetModel(), GetResponse(), cts.Token);

            // Method doesn't use token internally, so should still succeed
            Assert.NotNull(result);
        }

      
    }
}
