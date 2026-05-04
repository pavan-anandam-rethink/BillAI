using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ReportingService.Web.Controllers;
using Rethink.Services.Common.Models.ReportingModels;
using SummationService.Domain.Interfaces;
using Xunit;

namespace ReportingService.Tests.Controllers
{
    public class FinancialReportsControllerTests
    {
        private readonly Mock<IMonthlyFinancialSummaryService> _monthlyServiceMock;
        private readonly Mock<IFunderFinancialSummaryService> _funderServiceMock;
        private readonly Mock<ILogger<FinancialReportsController>> _loggerMock;

        private readonly FinancialReportsController _controller;

        public FinancialReportsControllerTests()
        {
            _monthlyServiceMock = new Mock<IMonthlyFinancialSummaryService>();
            _funderServiceMock = new Mock<IFunderFinancialSummaryService>();
            _loggerMock = new Mock<ILogger<FinancialReportsController>>();

            _controller = new FinancialReportsController(
                _monthlyServiceMock.Object,
                _funderServiceMock.Object,
                _loggerMock.Object
            );
        }

        #region GetMonthlyFinancialSummary

        [Fact]
        public async Task GetMonthlyFinancialSummary_InvalidModel_ReturnsValidationProblem()
        {
            _controller.ModelState.AddModelError("StartDate", "Required");

            var result = await _controller.GetMonthlyFinancialSummary(new MonthlyFinancialSummaryRequest());

            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task GetMonthlyFinancialSummary_FutureEndDate_ReturnsBadRequest()
        {
            var request = new MonthlyFinancialSummaryRequest
            {
                AccountInfoId = 1,
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(1)
            };

            var result = await _controller.GetMonthlyFinancialSummary(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("EndDate cannot be in the future.", badRequest.Value);
        }

        [Fact]
        public async Task GetMonthlyFinancialSummary_ValidRequest_ReturnsOk()
        {
            var request = new MonthlyFinancialSummaryRequest
            {
                AccountInfoId = 1,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(-1),
                DateType = null,
                LocationIds = new List<int> { 1 },
                FunderIds = new List<int> { 2 }
            };

            var fakeResult = new MonthlyFinancialSummaryResponse
            {
                StartingAR = 0m,
                DateBasis = "Transaction",
                Rows = new List<MonthlyFinancialRow>(),
                Total = new MonthlyFinancialRow(),
                UnappliedCredits = new UnappliedCreditsDto()
            };

            _monthlyServiceMock
                .Setup(x => x.GetMonthlyFinancialSummaryAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<string>(),
                    It.IsAny<List<int>>(),
                    It.IsAny<List<int>>()
                ))
                .ReturnsAsync(fakeResult);

            var result = await _controller.GetMonthlyFinancialSummary(request);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(fakeResult, ok.Value);
        }

        #endregion

        #region GetFunderFinancialSummary

        [Fact]
        public async Task GetFunderFinancialSummary_InvalidModel_ReturnsValidationProblem()
        {
            _controller.ModelState.AddModelError("StartDate", "Required");

            var result = await _controller.GetFunderFinancialSummary(new FunderFinancialSummaryRequest());

            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task GetFunderFinancialSummary_FutureEndDate_ReturnsBadRequest()
        {
            var request = new FunderFinancialSummaryRequest
            {
                AccountInfoId = 1,
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(2)
            };

            var result = await _controller.GetFunderFinancialSummary(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("EndDate cannot be in the future.", badRequest.Value);
        }

        [Fact]
        public async Task GetFunderFinancialSummary_ValidRequest_ReturnsOk()
        {
            var request = new FunderFinancialSummaryRequest
            {
                AccountInfoId = 1,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(-1),
                LocationIds = new List<int> { 1 },
                FunderIds = new List<int> { 2 },
                RenderingProviderIds = new List<int> { 3 },
                BillingProviderIds = new List<int> { 4 }
            };

            var fakeResult = new FunderFinancialSummaryResponse
            {
                StartingAR = 0m,
                DateBasis = "Transaction",
                Rows = new List<FunderFinancialRow>(),
                Total = new FunderFinancialRow(),
                UnappliedCredits = new UnappliedCreditsDto()
            };

            _funderServiceMock
                .Setup(x => x.GetFunderFinancialSummaryAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<string>(),
                    It.IsAny<List<int>>(),
                    It.IsAny<List<int>>(),
                    It.IsAny<List<int>>(),
                    It.IsAny<List<int>>()
                ))
                .ReturnsAsync(fakeResult);

            var result = await _controller.GetFunderFinancialSummary(request);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(fakeResult, ok.Value);
        }

        #endregion

        #region ExportToExcel

        [Fact]
        public async Task ExportToExcel_Success_ReturnsBase64()
        {
            var request = new MonthlyFinancialSummaryRequest
            {
                AccountInfoId = 1,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(-1),
                FunderIds = new List<int> { 1, 2 }
            };

            var fakeSummaryResult = new MonthlyFinancialSummaryResponse
            {
                StartingAR = 0m,
                DateBasis = "Transaction",
                Rows = new List<MonthlyFinancialRow>(),
                Total = new MonthlyFinancialRow(),
                UnappliedCredits = new UnappliedCreditsDto()
            };
            var fakeExcel = new byte[] { 1, 2, 3, 4 };

            _monthlyServiceMock
                .Setup(x => x.GetMonthlyFinancialSummaryAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<string>(),
                    It.IsAny<List<int>>(),
                    It.IsAny<List<int>>()
                ))
                .ReturnsAsync(fakeSummaryResult);

            _monthlyServiceMock
                .Setup(x => x.ExportToExcelAsync(
                    It.IsAny<MonthlyFinancialSummaryRequest>(),
                    It.IsAny<MonthlyFinancialSummaryResponse>(),
                    It.IsAny<CancellationToken>()
                ))
                .ReturnsAsync(fakeExcel);

            var result = await _controller.ExportToExcel(request, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);

            // Controller returns the Base64 payload directly (string or byte[]). Validate accordingly.
            if (ok.Value is string base64)
            {
                Assert.False(string.IsNullOrWhiteSpace(base64));
            }
            else if (ok.Value is byte[] bytes)
            {
                Assert.NotEmpty(bytes);
            }
            else
            {
                // If wrapped in an anonymous object with `data`, validate that too.
                var dataProp = ok.Value?.GetType().GetProperty("data");
                Assert.NotNull(dataProp);
                var dataVal = dataProp!.GetValue(ok.Value);
                Assert.NotNull(dataVal);
            }
        }

        [Fact]
        public async Task ExportToExcel_Exception_Returns500()
        {
            var request = new MonthlyFinancialSummaryRequest
            {
                AccountInfoId = 1,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(-1)
            };

            _monthlyServiceMock
                .Setup(x => x.GetMonthlyFinancialSummaryAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<string>(),
                    It.IsAny<List<int>>(),
                    It.IsAny<List<int>>()
                ))
                .ThrowsAsync(new Exception("DB Error"));

            var result = await _controller.ExportToExcel(request, CancellationToken.None);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        #endregion
    }
}