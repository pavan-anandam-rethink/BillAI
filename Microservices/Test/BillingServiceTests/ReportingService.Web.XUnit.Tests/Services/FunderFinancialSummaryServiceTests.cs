using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models.ReportingModels;
using SummationService.Domain.Interfaces;
using SummationService.Domain.Services;
using System.Data;
using System.Data.Common;
using BillingService.XUnit.Tests.Common.Mocks;
using Rethink.Services.Common.Entities.Billing.Scheduling;

namespace ReportingService.Web.XUnit.Tests.Services
{
    public class FunderFinancialSummaryServiceTests
    {
        [Fact]
        public async Task GetFunderFinancialSummaryAsync_ReturnsExpectedResult()
        {
            // Arrange
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
            var mockReader = new Mock<DbDataReader>();

            int resultSet = 0;
            int rowIndex = -1;

            // Mock ReadAsync
            mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    if (resultSet == 0) // ResultSet1: Starting AR
                        return ++rowIndex < 1;

                    if (resultSet == 1) // ResultSet2: Funder + TOTAL
                        return ++rowIndex < 2;

                    if (resultSet == 2) // ResultSet3: Unapplied Credits
                        return ++rowIndex < 1;

                    return false;
                });

            // Mock NextResultAsync
            mockReader.Setup(r => r.NextResultAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    resultSet++;
                    rowIndex = -1;
                    return resultSet < 3;
                });

            // Mock column ordinals
            mockReader.Setup(r => r.GetOrdinal("PriorPeriodBalance")).Returns(1);
            mockReader.Setup(r => r.GetOrdinal("Charges")).Returns(2);
            mockReader.Setup(r => r.GetOrdinal("InsurancePay")).Returns(3);
            mockReader.Setup(r => r.GetOrdinal("PatientPay")).Returns(4);
            mockReader.Setup(r => r.GetOrdinal("TotalPay")).Returns(5);
            mockReader.Setup(r => r.GetOrdinal("Adjustments")).Returns(6);
            mockReader.Setup(r => r.GetOrdinal("WriteOffs")).Returns(7);
            mockReader.Setup(r => r.GetOrdinal("PeriodBalance")).Returns(8);
            mockReader.Setup(r => r.GetOrdinal("TotalBalance")).Returns(9);
            mockReader.Setup(r => r.GetOrdinal("InsuranceUnapplied")).Returns(10);
            mockReader.Setup(r => r.GetOrdinal("PatientUnapplied")).Returns(11);
            mockReader.Setup(r => r.GetOrdinal("TotalUnapplied")).Returns(12);

            // Mock values for GetDecimal
            mockReader.Setup(r => r.GetDecimal(It.IsAny<int>())).Returns((int index) =>
            {
                // ResultSet1: Starting AR
                if (resultSet == 0)
                    return 50000m;

                // ResultSet2: Funder + TOTAL
                if (resultSet == 1)
                {
                    return index switch
                    {
                        1 => 15000m,  // PriorPeriodBalance
                        2 => 25000m,  // Charges
                        3 => 18000m,  // InsurancePay
                        4 => 1500m,   // PatientPay
                        5 => 19500m,  // TotalPay
                        6 => 3000m,   // Adjustments
                        7 => 500m,    // WriteOffs
                        8 => 2000m,   // PeriodBalance
                        9 => 17000m,  // TotalBalance
                        _ => 0m
                    };
                }

                // ResultSet3: Unapplied Credits
                if (resultSet == 2)
                {
                    return index switch
                    {
                        10 => 5000m,
                        11 => 1200m,
                        12 => 6200m,
                        _ => 0m
                    };
                }

                return 0m;
            });

            // Mock FunderId for ResultSet2
            mockReader.Setup(r => r["FunderId"]).Returns(() =>
            {
                if (resultSet == 1 && rowIndex == 0)
                    return 4; // Funder row
                if (resultSet == 1 && rowIndex == 1)
                    return DBNull.Value; // TOTAL row
                return DBNull.Value;
            });

            // Mock FunderName for ResultSet2
            mockReader.Setup(r => r["FunderName"]).Returns(() =>
            {
                if (resultSet == 1 && rowIndex == 0)
                    return "Aetna"; // Funder row
                if (resultSet == 1 && rowIndex == 1)
                    return "TOTAL"; // Total row
                return null!;
            });

            // Mock ExecuteReaderAsync
            mockDbHelper.Setup(h => h.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<List<SqlParameter>>(),
                It.IsAny<CommandType>()))
                .ReturnsAsync(mockReader.Object);

            var mockHelperService = new Mock<IHelperService>();
            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();
            var service = new FunderFinancialSummaryService(
                mockDbHelper.Object,
                mockHelperService.Object,
                mockClaimRepository.Object,
                mockFunderSearchRepository.Object
            );

            // Act
            var result = await service.GetFunderFinancialSummaryAsync(
                accountInfoId: 123,
                startDate: new DateTime(2026, 1, 1),
                endDate: new DateTime(2026, 1, 31),
                dateType: "Transaction",
                locationIds: new[] { 3, 4, 5 },
                funderIds: new[] { 4 });

            // Assert
            Assert.Equal("Transaction", result.DateBasis);
            Assert.Equal(50000m, result.StartingAR);

            Assert.Single(result.Rows);
            var row = result.Rows[0];
            Assert.Equal(4, row.FunderId);
            Assert.Equal("Aetna", row.FunderName);
            Assert.Equal(15000m, row.PriorPeriodBalance);
            Assert.Equal(25000m, row.Charges);
            Assert.Equal(18000m, row.InsurancePay);
            Assert.Equal(1500m, row.PatientPay);
            Assert.Equal(19500m, row.TotalPay);
            Assert.Equal(3000m, row.Adjustments);
            Assert.Equal(500m, row.WriteOffs);
            Assert.Equal(2000m, row.PeriodBalance);
            Assert.Equal(17000m, row.TotalBalance);

            Assert.NotNull(result.Total);
            Assert.Null(result.Total.FunderId);
            Assert.Equal("TOTAL", result.Total.FunderName);
            Assert.Equal(17000m, result.Total.TotalBalance);

            Assert.NotNull(result.UnappliedCredits);
            Assert.Equal(5000m, result.UnappliedCredits.InsuranceUnapplied);
            Assert.Equal(1200m, result.UnappliedCredits.PatientUnapplied);
            Assert.Equal(6200m, result.UnappliedCredits.TotalUnapplied);
        }

        [Fact]
        public async Task GetFunderFinancialSummaryAsync_WithMultipleFunders_ReturnsAllFunderRows()
        {
            // Arrange
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
            var mockReader = new Mock<DbDataReader>();

            int resultSet = 0;
            int rowIndex = -1;

            mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    if (resultSet == 0) return ++rowIndex < 1;
                    if (resultSet == 1) return ++rowIndex < 4; // Aetna, Anthem, Medicaid, TOTAL
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

            mockReader.Setup(r => r["FunderId"]).Returns(() =>
            {
                if (resultSet == 1)
                {
                    return rowIndex switch
                    {
                        0 => 4,
                        1 => 5,
                        2 => 596,
                        3 => DBNull.Value,
                        _ => DBNull.Value
                    };
                }
                return DBNull.Value;
            });

            mockReader.Setup(r => r["FunderName"]).Returns(() =>
            {
                if (resultSet == 1)
                {
                    return rowIndex switch
                    {
                        0 => "Aetna",
                        1 => "Anthem",
                        2 => "Medicaid",
                        3 => "TOTAL",
                        _ => ""
                    };
                }
                return "";
            });

            mockReader.Setup(r => r.GetDecimal(It.IsAny<int>())).Returns(1000m);
            mockReader.Setup(r => r.GetOrdinal(It.IsAny<string>())).Returns(1);

            mockDbHelper.Setup(h => h.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<List<SqlParameter>>(),
                It.IsAny<CommandType>()))
                .ReturnsAsync(mockReader.Object);

            var mockHelperService = new Mock<IHelperService>();
            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();
            var service = new FunderFinancialSummaryService(
                mockDbHelper.Object,
                mockHelperService.Object,
                mockClaimRepository.Object,
                mockFunderSearchRepository.Object
            );

            // Act
            var result = await service.GetFunderFinancialSummaryAsync(
                123,
                new DateTime(2026, 1, 1),
                new DateTime(2026, 1, 31));

            // Assert
            Assert.Equal(3, result.Rows.Count);
            Assert.Equal("Aetna", result.Rows[0].FunderName);
            Assert.Equal("Anthem", result.Rows[1].FunderName);
            Assert.Equal("Medicaid", result.Rows[2].FunderName);
            Assert.NotNull(result.Total);
            Assert.Equal("TOTAL", result.Total.FunderName);
        }

        [Fact]
        public async Task GetFunderFinancialSummaryAsync_WithDepositDateBasis_PassesCorrectParameter()
        {
            // Arrange
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
            var mockReader = new Mock<DbDataReader>();

            mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            mockReader.Setup(r => r.NextResultAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

            List<SqlParameter>? capturedParams = null;

            mockDbHelper.Setup(h => h.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<List<SqlParameter>>(),
                It.IsAny<CommandType>()))
                .Callback<string, List<SqlParameter>, CommandType>((proc, parms, cmdType) =>
                {
                    capturedParams = parms;
                })
                .ReturnsAsync(mockReader.Object);

            var mockHelperService = new Mock<IHelperService>();
            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();
            var service = new FunderFinancialSummaryService(
                mockDbHelper.Object,
                mockHelperService.Object,
                mockClaimRepository.Object,
                mockFunderSearchRepository.Object
            );

            // Act
            await service.GetFunderFinancialSummaryAsync(
                123,
                new DateTime(2026, 1, 1),
                new DateTime(2026, 1, 31),
                dateType: "Deposit");

            // Assert
            Assert.NotNull(capturedParams);
            var dateBasisParam = capturedParams.FirstOrDefault(p => p.ParameterName == "@DateBasis");
            Assert.NotNull(dateBasisParam);
            Assert.Equal("Deposit", dateBasisParam.Value);
        }

        [Fact]
        public async Task GetFunderFinancialSummaryAsync_WithRenderingProviderFilter_PassesCorrectParameter()
        {
            // Arrange
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
            var mockReader = new Mock<DbDataReader>();

            mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            mockReader.Setup(r => r.NextResultAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

            List<SqlParameter>? capturedParams = null;

            mockDbHelper.Setup(h => h.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<List<SqlParameter>>(),
                It.IsAny<CommandType>()))
                .Callback<string, List<SqlParameter>, CommandType>((proc, parms, cmdType) =>
                {
                    capturedParams = parms;
                })
                .ReturnsAsync(mockReader.Object);

            var mockHelperService = new Mock<IHelperService>();
            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();
            var service = new FunderFinancialSummaryService(
                mockDbHelper.Object,
                mockHelperService.Object,
                mockClaimRepository.Object,
                mockFunderSearchRepository.Object
            );

            // Act
            await service.GetFunderFinancialSummaryAsync(
                123,
                new DateTime(2026, 1, 1),
                new DateTime(2026, 1, 31),
                renderingProviderIds: new[] { 101, 102, 103 });

            // Assert
            Assert.NotNull(capturedParams);
            var renderingProviderParam = capturedParams.FirstOrDefault(p => p.ParameterName == "@RenderingProviderIds");
            Assert.NotNull(renderingProviderParam);
            Assert.Equal("101,102,103", renderingProviderParam.Value);
        }

        [Fact]
        public async Task GetFunderFinancialSummaryAsync_WithBillingProviderFilter_PassesCorrectParameter()
        {
            // Arrange
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
            var mockReader = new Mock<DbDataReader>();

            mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            mockReader.Setup(r => r.NextResultAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

            List<SqlParameter>? capturedParams = null;

            mockDbHelper.Setup(h => h.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<List<SqlParameter>>(),
                It.IsAny<CommandType>()))
                .Callback<string, List<SqlParameter>, CommandType>((proc, parms, cmdType) =>
                {
                    capturedParams = parms;
                })
                .ReturnsAsync(mockReader.Object);

            var mockHelperService = new Mock<IHelperService>();
            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();
            var service = new FunderFinancialSummaryService(
                mockDbHelper.Object,
                mockHelperService.Object,
                mockClaimRepository.Object,
                mockFunderSearchRepository.Object
            );

            // Act
            await service.GetFunderFinancialSummaryAsync(
                123,
                new DateTime(2026, 1, 1),
                new DateTime(2026, 1, 31),
                billingProviderIds: new[] { 201, 202 });

            // Assert
            Assert.NotNull(capturedParams);
            var billingProviderParam = capturedParams.FirstOrDefault(p => p.ParameterName == "@BillingProviderIds");
            Assert.NotNull(billingProviderParam);
            Assert.Equal("201,202", billingProviderParam.Value);
        }

        [Fact]
        public async Task GetFunderFinancialSummaryAsync_WithNullFilters_PassesDBNullValues()
        {
            // Arrange
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
            var mockReader = new Mock<DbDataReader>();

            mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            mockReader.Setup(r => r.NextResultAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

            List<SqlParameter>? capturedParams = null;

            mockDbHelper.Setup(h => h.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<List<SqlParameter>>(),
                It.IsAny<CommandType>()))
                .Callback<string, List<SqlParameter>, CommandType>((proc, parms, cmdType) =>
                {
                    capturedParams = parms;
                })
                .ReturnsAsync(mockReader.Object);

            var mockHelperService = new Mock<IHelperService>();
            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();
            var service = new FunderFinancialSummaryService(
                mockDbHelper.Object,
                mockHelperService.Object,
                mockClaimRepository.Object,
                mockFunderSearchRepository.Object
            );

            // Act
            await service.GetFunderFinancialSummaryAsync(
                123,
                new DateTime(2026, 1, 1),
                new DateTime(2026, 1, 31),
                locationIds: null,
                funderIds: null,
                renderingProviderIds: null,
                billingProviderIds: null);

            // Assert
            Assert.NotNull(capturedParams);
            
            var locationParam = capturedParams.FirstOrDefault(p => p.ParameterName == "@LocationIds");
            Assert.NotNull(locationParam);
            Assert.Equal(DBNull.Value, locationParam.Value);

            var funderParam = capturedParams.FirstOrDefault(p => p.ParameterName == "@FunderIds");
            Assert.NotNull(funderParam);
            Assert.Equal(DBNull.Value, funderParam.Value);

            var renderingParam = capturedParams.FirstOrDefault(p => p.ParameterName == "@RenderingProviderIds");
            Assert.NotNull(renderingParam);
            Assert.Equal(DBNull.Value, renderingParam.Value);

            var billingParam = capturedParams.FirstOrDefault(p => p.ParameterName == "@BillingProviderIds");
            Assert.NotNull(billingParam);
            Assert.Equal(DBNull.Value, billingParam.Value);
        }

        [Fact]
        public async Task GetFunderFinancialSummaryAsync_WithEmptyFilters_PassesDBNullValues()
        {
            // Arrange
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
            var mockReader = new Mock<DbDataReader>();

            mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            mockReader.Setup(r => r.NextResultAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

            List<SqlParameter>? capturedParams = null;

            mockDbHelper.Setup(h => h.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<List<SqlParameter>>(),
                It.IsAny<CommandType>()))
                .Callback<string, List<SqlParameter>, CommandType>((proc, parms, cmdType) =>
                {
                    capturedParams = parms;
                })
                .ReturnsAsync(mockReader.Object);

            var mockHelperService = new Mock<IHelperService>();
            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();
            var service = new FunderFinancialSummaryService(
                mockDbHelper.Object,
                mockHelperService.Object,
                mockClaimRepository.Object,
                mockFunderSearchRepository.Object
            );

            // Act
            await service.GetFunderFinancialSummaryAsync(
                123,
                new DateTime(2026, 1, 1),
                new DateTime(2026, 1, 31),
                locationIds: Array.Empty<int>(),
                funderIds: Array.Empty<int>(),
                renderingProviderIds: Array.Empty<int>(),
                billingProviderIds: Array.Empty<int>());

            // Assert
            Assert.NotNull(capturedParams);

            var locationParam = capturedParams.FirstOrDefault(p => p.ParameterName == "@LocationIds");
            Assert.NotNull(locationParam);
            Assert.Equal(DBNull.Value, locationParam.Value);

            var funderParam = capturedParams.FirstOrDefault(p => p.ParameterName == "@FunderIds");
            Assert.NotNull(funderParam);
            Assert.Equal(DBNull.Value, funderParam.Value);
        }

        [Fact]
        public async Task GetFunderFinancialSummaryAsync_WhenOnlyTotalRow_ReturnsNoFunderRows()
        {
            // Arrange
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
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
            mockReader.Setup(r => r["FunderId"]).Returns(DBNull.Value);
            mockReader.Setup(r => r["FunderName"]).Returns(() => resultSet == 1 ? "TOTAL" : "");
            mockReader.Setup(r => r.GetOrdinal(It.IsAny<string>())).Returns(1);

            mockDbHelper.Setup(h => h.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<List<SqlParameter>>(),
                It.IsAny<CommandType>()))
                .ReturnsAsync(mockReader.Object);

            var mockHelperService = new Mock<IHelperService>();
            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();
            var service = new FunderFinancialSummaryService(
                mockDbHelper.Object,
                mockHelperService.Object,
                mockClaimRepository.Object,
                mockFunderSearchRepository.Object
            );

            // Act
            var result = await service.GetFunderFinancialSummaryAsync(
                123,
                DateTime.Today.AddDays(-30),
                DateTime.Today);

            // Assert
            Assert.Empty(result.Rows);
            Assert.NotNull(result.Total);
            Assert.Equal("TOTAL", result.Total.FunderName);
            Assert.Equal(500m, result.StartingAR);
        }

        [Fact]
        public async Task GetFunderFinancialSummaryAsync_WhenNoUnappliedCredits_ResultIsNull()
        {
            // Arrange
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
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

            mockReader.Setup(r => r["FunderId"]).Returns(DBNull.Value);
            mockReader.Setup(r => r["FunderName"]).Returns("TOTAL");
            mockReader.Setup(r => r.GetDecimal(It.IsAny<int>())).Returns(100m);
            mockReader.Setup(r => r.GetOrdinal(It.IsAny<string>())).Returns(1);

            mockDbHelper.Setup(h => h.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<List<SqlParameter>>(),
                It.IsAny<CommandType>()))
                .ReturnsAsync(mockReader.Object);

            var mockHelperService = new Mock<IHelperService>();
            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();
            var service = new FunderFinancialSummaryService(
                mockDbHelper.Object,
                mockHelperService.Object,
                mockClaimRepository.Object,
                mockFunderSearchRepository.Object
            );

            // Act
            var result = await service.GetFunderFinancialSummaryAsync(
                123,
                DateTime.Today.AddDays(-7),
                DateTime.Today);

            // Assert
            Assert.NotNull(result.Total);
            Assert.Null(result.UnappliedCredits);
        }

        [Fact]
        public async Task GetFunderFinancialSummaryAsync_WhenNoRows_ReturnsEmptyResult()
        {
            // Arrange
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
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

            var mockHelperService = new Mock<IHelperService>();
            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();
            var service = new FunderFinancialSummaryService(
                mockDbHelper.Object,
                mockHelperService.Object,
                mockClaimRepository.Object,
                mockFunderSearchRepository.Object
            );

            // Act
            var result = await service.GetFunderFinancialSummaryAsync(
                123,
                DateTime.Today.AddDays(-7),
                DateTime.Today);

            // Assert
            Assert.Equal(0, result.Rows.Count);
            Assert.NotNull(result.Total);
            Assert.Null(result.UnappliedCredits);
            Assert.Equal("Transaction", result.DateBasis);
            Assert.Equal(0m, result.StartingAR);
        }

        [Fact]
        public async Task GetFunderFinancialSummaryAsync_WhenExecuteReaderThrows_ThrowsException()
        {
            // Arrange
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();

            mockDbHelper.Setup(h => h.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<List<SqlParameter>>(),
                It.IsAny<CommandType>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            var mockHelperService = new Mock<IHelperService>();
            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();
            var service = new FunderFinancialSummaryService(
                mockDbHelper.Object,
                mockHelperService.Object,
                mockClaimRepository.Object,
                mockFunderSearchRepository.Object
            );

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await service.GetFunderFinancialSummaryAsync(
                    123,
                    DateTime.Today.AddDays(-7),
                    DateTime.Today);
            });
        }

        [Fact]
        public async Task GetFunderFinancialSummaryAsync_WithStartingARZero_ReturnsZeroStartingAR()
        {
            // Arrange
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
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
            mockReader.Setup(r => r["FunderId"]).Returns(DBNull.Value);
            mockReader.Setup(r => r["FunderName"]).Returns("TOTAL");
            mockReader.Setup(r => r.GetOrdinal(It.IsAny<string>())).Returns(1);

            mockDbHelper.Setup(h => h.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<List<SqlParameter>>(),
                It.IsAny<CommandType>()))
                .ReturnsAsync(mockReader.Object);

            var mockHelperService = new Mock<IHelperService>();
            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();
            var service = new FunderFinancialSummaryService(
                mockDbHelper.Object,
                mockHelperService.Object,
                mockClaimRepository.Object,
                mockFunderSearchRepository.Object
            );

            // Act
            var result = await service.GetFunderFinancialSummaryAsync(
                123,
                DateTime.Today.AddDays(-7),
                DateTime.Today);

            // Assert
            Assert.Equal(0m, result.StartingAR);
            Assert.NotNull(result.Total);
        }

        [Fact]
        public async Task GetFunderFinancialSummaryAsync_CallsCorrectStoredProcedure()
        {
            // Arrange
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
            var mockReader = new Mock<DbDataReader>();

            mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            mockReader.Setup(r => r.NextResultAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

            string? capturedProcName = null;

            mockDbHelper.Setup(h => h.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<List<SqlParameter>>(),
                It.IsAny<CommandType>()))
                .Callback<string, List<SqlParameter>, CommandType>((proc, parms, cmdType) =>
                {
                    capturedProcName = proc;
                })
                .ReturnsAsync(mockReader.Object);

            var mockHelperService = new Mock<IHelperService>();
            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();
            var service = new FunderFinancialSummaryService(
                mockDbHelper.Object,
                mockHelperService.Object,
                mockClaimRepository.Object,
                mockFunderSearchRepository.Object
            );

            // Act
            await service.GetFunderFinancialSummaryAsync(
                123,
                new DateTime(2026, 1, 1),
                new DateTime(2026, 1, 31));

            // Assert
            Assert.Equal("dbo.usp_FunderFinancialSummary", capturedProcName);
        }

        [Fact]
        public async Task GetFunderFinancialSummaryAsync_PassesAllRequiredParameters()
        {
            // Arrange
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
            var mockReader = new Mock<DbDataReader>();

            mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            mockReader.Setup(r => r.NextResultAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

            List<SqlParameter>? capturedParams = null;

            mockDbHelper.Setup(h => h.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<List<SqlParameter>>(),
                It.IsAny<CommandType>()))
                .Callback<string, List<SqlParameter>, CommandType>((proc, parms, cmdType) =>
                {
                    capturedParams = parms;
                })
                .ReturnsAsync(mockReader.Object);

            var mockHelperService = new Mock<IHelperService>();
            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();
            var service = new FunderFinancialSummaryService(
                mockDbHelper.Object,
                mockHelperService.Object,
                mockClaimRepository.Object,
                mockFunderSearchRepository.Object
            );

            var accountInfoId = 18421;
            var startDate = new DateTime(2026, 1, 1);
            var endDate = new DateTime(2026, 1, 31);

            // Act
            await service.GetFunderFinancialSummaryAsync(
                accountInfoId,
                startDate,
                endDate);

            // Assert
            Assert.NotNull(capturedParams);
            Assert.Contains(capturedParams, p => p.ParameterName == "@AccountInfoId" && (int)p.Value == accountInfoId);
            Assert.Contains(capturedParams, p => p.ParameterName == "@StartDate" && (DateTime)p.Value == startDate);
            Assert.Contains(capturedParams, p => p.ParameterName == "@EndDate" && (DateTime)p.Value == endDate);
            Assert.Contains(capturedParams, p => p.ParameterName == "@DateBasis" && (string)p.Value == "Transaction");
            Assert.Contains(capturedParams, p => p.ParameterName == "@LocationIds");
            Assert.Contains(capturedParams, p => p.ParameterName == "@FunderIds");
            Assert.Contains(capturedParams, p => p.ParameterName == "@RenderingProviderIds");
            Assert.Contains(capturedParams, p => p.ParameterName == "@BillingProviderIds");
        }

        [Fact]
        public async Task MapFunderFinancialRow_HandlesNullFunderId()
        {
            // Arrange
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
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

            // Mock NULL FunderId for TOTAL row
            mockReader.Setup(r => r["FunderId"]).Returns(DBNull.Value);
            mockReader.Setup(r => r["FunderName"]).Returns("TOTAL");
            mockReader.Setup(r => r.GetDecimal(It.IsAny<int>())).Returns(100m);
            mockReader.Setup(r => r.GetOrdinal(It.IsAny<string>())).Returns(1);

            mockDbHelper.Setup(h => h.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<List<SqlParameter>>(),
                It.IsAny<CommandType>()))
                .ReturnsAsync(mockReader.Object);

            var mockHelperService = new Mock<IHelperService>();
            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();
            var service = new FunderFinancialSummaryService(
                mockDbHelper.Object,
                mockHelperService.Object,
                mockClaimRepository.Object,
                mockFunderSearchRepository.Object
            );

            // Act
            var result = await service.GetFunderFinancialSummaryAsync(
                123,
                DateTime.Today.AddDays(-7),
                DateTime.Today);

            // Assert
            Assert.NotNull(result.Total);
            Assert.Null(result.Total.FunderId);
            Assert.Equal("TOTAL", result.Total.FunderName);
        }

        [Fact]
        public async Task ExportToExcelAsync_WithAllBlocks_CreatesExcelBytes_AndCallsHelperMethods()
        {
            // Arrange
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
            var mockHelperService = new Mock<IHelperService>();

            // DefineStyles is void
            mockHelperService.Setup(h => h.DefineStyles(It.IsAny<WorkbookPart>()));

            // AddCell should return a real OpenXML Cell so document save works
            mockHelperService.Setup(h => h.AddCell(
                    It.IsAny<ExcelCellType>(),
                    It.IsAny<object>(),
                    It.IsAny<bool>()))
                .Returns<ExcelCellType, object, bool>((t, v, c) =>
                    new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(v?.ToString() ?? string.Empty)
                    });

            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();

            // Mock the repository queries to support async operations
            var claimEntities = new List<ClaimEntity>
            {
                new ClaimEntity
                {
                    AccountInfoId = 18421,
                    ClaimStatus = ClaimStatus.PendingReview,
                    PrimaryFunderId = 1,
                    ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity>
                    {
                        new ClaimAppointmentLinkEntity()
                    }
                },
                new ClaimEntity
                {
                    AccountInfoId = 18421,
                    ClaimStatus = ClaimStatus.ReadyToBill,
                    PrimaryFunderId = 2,
                    ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity>
                    {
                        new ClaimAppointmentLinkEntity()
                    }
                }
            };

            var funderSearchEntities = new List<ClaimSearchFunderEntity>
            {
                new ClaimSearchFunderEntity { Id = 1, Name = "Aetna" },
                new ClaimSearchFunderEntity { Id = 2, Name = "Anthem" }
            };

            mockClaimRepository.Setup(r => r.Query()).Returns(QueryMock<ClaimEntity>.Create(claimEntities));
            mockFunderSearchRepository.Setup(r => r.Query()).Returns(QueryMock<ClaimSearchFunderEntity>.Create(funderSearchEntities));

            var service = new FunderFinancialSummaryService(
                mockDbHelper.Object,
                mockHelperService.Object,
                mockClaimRepository.Object,
                mockFunderSearchRepository.Object
            );

            var model = new FunderFinancialSummaryRequest
            {
                AccountInfoId = 18421,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 1, 31),
                DateType = "Transaction",

                // to hit: LocationNames != null
                LocationNames = new List<string> { "Loc-1", "Loc-2" },

                // to hit: FunderIds != null && Any + foreach + funderText joined
                FunderIds = new List<int> { 1, 2 },

                // to hit: RenderingProviderNames != null
                RenderingProviderNames = new List<string> { "Dr A", "Dr B" },

                // to hit: BillingProviderNames != null
                BillingProviderNames = new List<string> { "Bill A" }
            };

            var response = new FunderFinancialSummaryResponse
            {
                DateBasis = "Transaction",
                StartingAR = 123.45m,
                Rows = new List<FunderFinancialRow>
                {
                    new FunderFinancialRow
                    {
                        FunderId = 1,
                        FunderName = "Aetna",
                        PriorPeriodBalance = 10,
                        Charges = 20,
                        InsurancePay = -5,
                        PatientPay = -2,
                        TotalPay = -7,
                        Adjustments = -1,
                        WriteOffs = 0.5m,
                        PeriodBalance = 3,
                        TotalBalance = 13
                    },
                    new FunderFinancialRow
                    {
                        FunderId = 2,
                        FunderName = "Anthem",
                        PriorPeriodBalance = 11,
                        Charges = 21,
                        InsurancePay = -6,
                        PatientPay = -3,
                        TotalPay = -9,
                        Adjustments = -2,
                        WriteOffs = 0.25m,
                        PeriodBalance = 4,
                        TotalBalance = 15
                    }
                },
                // to hit: TOTAL block
                Total = new FunderFinancialRow
                {
                    FunderName = "TOTAL",
                    Charges = 41,
                    InsurancePay = -11,
                    PatientPay = -5,
                    TotalPay = -16,
                    Adjustments = -3,
                    WriteOffs = 0.75m,
                    PeriodBalance = 7,
                    TotalBalance = 28
                },
                // to hit: Unapplied credits block
                UnappliedCredits = new UnappliedCreditsDto
                {
                    InsuranceUnapplied = 100,
                    PatientUnapplied = 50,
                    TotalUnapplied = 150
                }
            };

            // Act
            var bytes = await service.ExportToExcelAsync(model, response, CancellationToken.None);

            // Assert
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);

            // Verify that DefineStyles was called once
            mockHelperService.Verify(h => h.DefineStyles(It.IsAny<WorkbookPart>()), Times.Once);

            // Verify AddCell was called multiple times for building the Excel
            mockHelperService.Verify(h => h.AddCell(It.IsAny<ExcelCellType>(), It.IsAny<object>(), It.IsAny<bool>()),
                Times.AtLeastOnce);
            
            // Verify the repositories were queried to get funder information
            mockClaimRepository.Verify(r => r.Query(), Times.Once);
            mockFunderSearchRepository.Verify(r => r.Query(), Times.Once);
        }

        [Fact]
        public async Task ExportToExcelAsync_WhenGetFunderNameReturnsNullOrWhitespace_FunderTextFallsBackToAll()
        {
            // Arrange
            var mockDbHelper = new Mock<IDbHelper<BillingDbContext>>();
            var mockHelperService = new Mock<IHelperService>();

            mockHelperService.Setup(h => h.DefineStyles(It.IsAny<WorkbookPart>()));

            mockHelperService.Setup(h => h.AddCell(
                    It.IsAny<ExcelCellType>(),
                    It.IsAny<object>(),
                    It.IsAny<bool>()))
                .Returns<ExcelCellType, object, bool>((t, v, c) =>
                    new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(v?.ToString() ?? string.Empty)
                    });

            var mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFunderSearchRepository = new Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>>();

            // Mock the repository queries - return a funder with whitespace name
            // The implementation filters these out when building funderText
            var claimEntities = new List<ClaimEntity>
            {
                new ClaimEntity
                {
                    AccountInfoId = 18421,
                    ClaimStatus = ClaimStatus.PendingReview,
                    PrimaryFunderId = 99,
                    ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity>
                    {
                        new ClaimAppointmentLinkEntity()
                    }
                }
            };

            var funderSearchEntities = new List<ClaimSearchFunderEntity>
            {
                // This funder has a whitespace name, which won't match properly when joined
                new ClaimSearchFunderEntity { Id = 99, Name = "   " }
            };

            mockClaimRepository.Setup(r => r.Query()).Returns(QueryMock<ClaimEntity>.Create(claimEntities));
            mockFunderSearchRepository.Setup(r => r.Query()).Returns(QueryMock<ClaimSearchFunderEntity>.Create(funderSearchEntities));

            var service = new FunderFinancialSummaryService(
                mockDbHelper.Object,
                mockHelperService.Object,
                mockClaimRepository.Object,
                mockFunderSearchRepository.Object
            );

            var model = new FunderFinancialSummaryRequest
            {
                AccountInfoId = 18421,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 1, 31),
                FunderIds = new List<int> { 99 },
                LocationNames = null, // hit: locationText stays "All"
                RenderingProviderNames = null, // hit: "All"
                BillingProviderNames = null // hit: "All"
            };

            var response = new FunderFinancialSummaryResponse
            {
                StartingAR = 0,
                Rows = new List<FunderFinancialRow>()
            };

            // Act
            var bytes = await service.ExportToExcelAsync(model, response, CancellationToken.None);

            // Assert
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);

            // Verify that DefineStyles and AddCell were called
            mockHelperService.Verify(h => h.DefineStyles(It.IsAny<WorkbookPart>()), Times.Once);
            mockHelperService.Verify(h => h.AddCell(It.IsAny<ExcelCellType>(), It.IsAny<object>(), It.IsAny<bool>()),
                Times.AtLeastOnce);
        }
    }
}