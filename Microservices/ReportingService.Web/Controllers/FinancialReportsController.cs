using Microsoft.AspNetCore.Mvc;
using Rethink.Services.Common.Models.ReportingModels;
using SummationService.Domain.Interfaces;

namespace ReportingService.Web.Controllers
{
    [Area("Reporting")]
    [Route("[controller]/[action]")]
    public class FinancialReportsController : BaseV1Controller
    {
        private readonly IMonthlyFinancialSummaryService _summaryService;
        private readonly ILogger<FinancialReportsController> _logger;
        private readonly IFunderFinancialSummaryService _funderSummaryService;

        public FinancialReportsController(
            IMonthlyFinancialSummaryService summaryService,
            IFunderFinancialSummaryService funderSummaryService,
            ILogger<FinancialReportsController> logger)
        {
            _summaryService = summaryService;
            _funderSummaryService = funderSummaryService;
            _logger = logger;
        }

        /// <summary>
        /// Returns the Monthly Financial Summary report
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetMonthlyFinancialSummary([FromBody] MonthlyFinancialSummaryRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            // Normalize dates (strip time)
            var startDate = request.StartDate.Date;
            var endDate = request.EndDate.Date;

            // Guard: future dates
            if (endDate > DateTime.UtcNow.Date)
                return BadRequest("EndDate cannot be in the future.");

            var jsonResult = await _summaryService.GetMonthlyFinancialSummaryAsync(
                request.AccountInfoId,
                startDate,
                endDate,
                request.DateType ?? "Transaction",
                request.LocationIds,
                request.FunderIds
            );

            return Ok(jsonResult);
        }

        /// <summary>
        /// Returns the Funder Financial Summary report (grouped by Funder/Payer)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetFunderFinancialSummary([FromBody] FunderFinancialSummaryRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            // Normalize dates (strip time)
            var startDate = request.StartDate.Date;
            var endDate = request.EndDate.Date;

            // Guard: future dates
            if (endDate > DateTime.UtcNow.Date)
                return BadRequest("EndDate cannot be in the future.");

            var jsonResult = await _funderSummaryService.GetFunderFinancialSummaryAsync(
                request.AccountInfoId,
                startDate,
                endDate,
                request.DateType ?? "Transaction",
                request.LocationIds,
                request.FunderIds,
                request.RenderingProviderIds,
                request.BillingProviderIds
            );

            return Ok(jsonResult);
        }

        [HttpPost]
        public async Task<IActionResult> ExportToExcel([FromBody] MonthlyFinancialSummaryRequest model, CancellationToken cancellationToken)
        {
            try
            {
                // Normalize dates (strip time)
                var startDate = model.StartDate.Date;
                var endDate = model.EndDate.Date;

                _logger.LogInformation(
                    $"{nameof(MonthlyFinancialSummaryRequest)}: Exporting Financial Summary to Excel from start date = {startDate} to end date = {endDate} and funder ids={string.Join(",", model.FunderIds)}"
                );

                var result = await _summaryService.GetMonthlyFinancialSummaryAsync(
                    model.AccountInfoId,
                    startDate,
                    endDate,
                    model.DateType ?? "Transaction",
                    model.LocationIds,
                    model.FunderIds
                );

                var excelFile = await _summaryService.ExportToExcelAsync(model, result, cancellationToken);

                var base64Excel = Convert.ToBase64String(excelFile);

                _logger.LogInformation($"{nameof(MonthlyFinancialSummaryRequest)}: Successfully Financial Summary exported to Excel.");
                return Ok(new { data = base64Excel });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    $"{nameof(MonthlyFinancialSummaryRequest)}: Error Exporting Financial Summary to Excel.");

                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ExportFunderToExcel([FromBody] FunderFinancialSummaryRequest model, CancellationToken cancellationToken)
        {
            try
            {
                // Normalize dates (strip time)
                var startDate = model.StartDate.Date;
                var endDate = model.EndDate.Date;

                _logger.LogInformation(
                    $"{nameof(FunderFinancialSummaryRequest)}: Exporting Financial Summary to Excel from start date = {startDate} to end date = {endDate} and funder ids={string.Join(",", model.FunderIds)}"
                );

                var result = await _funderSummaryService.GetFunderFinancialSummaryAsync(
                    model.AccountInfoId,
                    startDate,
                    endDate,
                    model.DateType ?? "Transaction",
                    model.LocationIds,
                    model.FunderIds,
                    model.RenderingProviderIds,
                    model.BillingProviderIds
                );

                var excelFile = await _funderSummaryService.ExportToExcelAsync(model, result, cancellationToken);

                var base64Excel = Convert.ToBase64String(excelFile);

                _logger.LogInformation($"{nameof(FunderFinancialSummaryRequest)}: Successfully Financial Summary exported to Excel.");
                return Ok(new { data = base64Excel });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    $"{nameof(FunderFinancialSummaryRequest)}: Error Exporting Financial Summary to Excel.");

                return StatusCode(500, "Internal server error");
            }
        }
    }
}