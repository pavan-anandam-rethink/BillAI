using Microsoft.AspNetCore.Mvc;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.ReportingModels;
using SummationService.Domain.Interfaces;

namespace ReportingService.Web.Controllers;

[Area("Reporting")]
[Route("[controller]/[action]")]
public class PaymentAdjustmentController(
    IRethinkMasterDataMicroServices _rethinkServices,
    IPaymentAdjustmentService paymentAdjustmentService,
    ILogger<PaymentAdjustmentController> logger) : BaseV1Controller
{
    [HttpPost]
    public async Task<IActionResult> AddOrUpdatePaymentAdjustment([FromBody] ClaimTransactionModel model, CancellationToken cancellationToken)
    {
        logger.LogInformation($"{nameof(PaymentAdjustmentController)}:AddOrUpdatePaymentAdjustment called with claimId={model.TransactionTypeId} and TransactionType={model.TransactionType}", model.TransactionTypeId, (ClaimTransactionType)model.TransactionType);
        try
        {
            await paymentAdjustmentService.AddOrUpdatePaymentAdjustmentAsync((ClaimTransactionType)model.TransactionType, model.TransactionTypeId, cancellationToken);
            logger.LogInformation($"{nameof(PaymentAdjustmentController)}:Successfully added or updated payment adjustment for claimId={model.TransactionTypeId} and TransactionType={model.TransactionType}", model.TransactionTypeId, (ClaimTransactionType)model.TransactionType);
            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogError($"{nameof(PaymentAdjustmentController)}:Error adding claim details. claimId={model.TransactionTypeId} and TransactionType = {(ClaimTransactionType)model.TransactionType} \n Error: {ex.Message}");
            logger.LogError(ex, $"Error adding claim details. claimId={model.TransactionTypeId} and TransactionType = {(ClaimTransactionType)model.TransactionType} \n Error: {ex.Message}");
            return StatusCode(500, "Internal server error");
        }
    }
    [HttpPost]
    public async Task<IActionResult> GetPaymentsAdjustments([FromBody] PaymentsAdjustmentsRequestModel model, CancellationToken cancellationToken)
    {
        logger.LogInformation($"{nameof(PaymentAdjustmentController)}:GetPaymentsAdjustments called for funderId={model.FunderId}, startDate={model.StartDate}, endDate={model.EndDate}", model.FunderId, model.StartDate, model.EndDate);
        try
        {
            var result = await paymentAdjustmentService.GetPaymentsAdjustmentsAsync(model, cancellationToken);

            var clientUsersList = await _rethinkServices.GetChildProfilesForAccount(model.AccountInfoId);
            result.paymentsAdjustments.Where(x => string.IsNullOrWhiteSpace(x.ClientFirst)).ToList().ForEach(x =>
            {
                var patientDetail = clientUsersList.FirstOrDefault(p => p.Id == x.ClientId && p.DateDeleted == null);
                x.ClientFirst = patientDetail?.FirstName ?? string.Empty;
                x.ClientLast = patientDetail?.LastName ?? string.Empty;
            });

            logger.LogInformation($"{nameof(PaymentAdjustmentController)}:Successfully retrieved payments adjustments for funderId={model.FunderId}, startDate={model.StartDate}, endDate={model.EndDate}", model.FunderId, model.StartDate, model.EndDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError($"{nameof(PaymentAdjustmentController)}:Error getting the details of funder ids = {model.FunderId} with startDate = {model.StartDate} and endDate = {model.EndDate} \n Error: {ex.Message}");
            logger.LogError(ex, $"Error getting the details of funder ids = {model.FunderId} with startDate = {model.StartDate} and endDate = {model.EndDate} \n Error: {ex.Message}");
            return StatusCode(500, "Internal server error");
        }
    }
    [HttpPost]
    public async Task<IActionResult> ExportToExcel([FromBody] PaymentsAdjustmentsRequestModel model, CancellationToken cancellationToken)
    {
        logger.LogInformation($"{nameof(PaymentAdjustmentController)}:ExportToExcel called for funderId={model.FunderId}, startDate={model.StartDate}, endDate={model.EndDate}", model.FunderId, model.StartDate, model.EndDate);
        try
        {
            model.IsExport = true;
            var result = await paymentAdjustmentService.GetPaymentsAdjustmentsAsync(model, cancellationToken);

            var clientUsersList = await _rethinkServices.GetChildProfilesForAccount(model.AccountInfoId);
            result.paymentsAdjustments.Where(x => string.IsNullOrWhiteSpace(x.ClientFirst)).ToList().ForEach(x =>
            {
                var patientDetail = clientUsersList.FirstOrDefault(p => p.Id == x.ClientId && p.DateDeleted == null);
                x.ClientFirst = patientDetail?.FirstName ?? string.Empty;
                x.ClientLast = patientDetail?.LastName ?? string.Empty;
            });

            var excelFile = await paymentAdjustmentService.ExportToExcelAsync(model, result, cancellationToken);

            var base64Excel = Convert.ToBase64String(excelFile);

            logger.LogInformation($"{nameof(PaymentAdjustmentController)}:Successfully exported payments adjustments to Excel for funderId={model.FunderId}, startDate={model.StartDate}, endDate={model.EndDate}", model.FunderId, model.StartDate, model.EndDate);
            return Ok(new { data = base64Excel });
        }
        catch (Exception ex)
        {
            logger.LogError($"{nameof(PaymentAdjustmentController)}:Error getting the details of funder ids = {model.FunderId} with startDate = {model.StartDate} and endDate = {model.EndDate} \n Error: {ex.Message}");
            logger.LogError(ex, $"Error getting the details of funder ids = {model.FunderId} with startDate = {model.StartDate} and endDate = {model.EndDate} \n Error: {ex.Message}");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetClaimFollowUpReport([FromBody] ClaimFollowUpRequestModel model, CancellationToken cancellationToken)
    {
        logger.LogInformation($"{nameof(PaymentAdjustmentController)}:GetClaimFollowUpReport called for funderIds={model.FunderIds} , startDate= {model.StartDate}, endDate={model.EndDate}", model.FunderIds, model.StartDate, model.EndDate);
        try
        {
            var result = await paymentAdjustmentService.GetClaimFollowUpReportAsync(model, cancellationToken);

            await MapCreatedByResult(_rethinkServices, model, result);

            logger.LogInformation($"{nameof(PaymentAdjustmentController)}:Successfully retrieved claim follow up report for funderIds={model.FunderIds}, startDate={model.StartDate}, endDate={model.EndDate}", model.FunderIds, model.StartDate, model.EndDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError($"{nameof(PaymentAdjustmentController)}:Error getting the details of funder ids = {model.FunderIds} with startDate = {model.StartDate} and endDate = {model.EndDate} \n Error: {ex.Message}");
            logger.LogError(ex, $"Error getting the details of funder ids = {model.FunderIds} with startDate = {model.StartDate} and endDate = {model.EndDate} \n Error: {ex.Message}");
            return StatusCode(500, ex.Message);
        }
    }

    private static async Task MapCreatedByResult(IRethinkMasterDataMicroServices _rethinkServices, ClaimFollowUpRequestModel model, ClaimFollowUpResponseModel result)
    {
        var memberIdsList = result.claimFollowUps
                    .Where(r => r.NoteCreatedBy.HasValue)
                    .Select(r => r.NoteCreatedBy.Value)
                    .Distinct()
                    .ToList();

        if (memberIdsList.Count > 0)
        {
            var memberIdsQuery = string.Join("&", memberIdsList.Select(id => $"memberIds={id}"));

            var members = await _rethinkServices.GetMembersAsync(model.AccountInfoId, memberIdsQuery);

            result.claimFollowUps.ForEach(r =>
            {
                var creator = members.data.FirstOrDefault(m => m.id == r.NoteCreatedBy);
                r.NoteCreatedByName = $"{creator?.firstName} {creator?.lastName}";
            });
        }
    }


    [HttpPost]
    public async Task<IActionResult> ExportToExcelClaimFollow([FromBody] ClaimFollowUpRequestModel model, CancellationToken cancellationToken)
    {
        logger.LogInformation($"{nameof(PaymentAdjustmentController)}:ExportToExcelClaimFollow called for funderIds={model.FunderIds}, startDate={model.StartDate}, endDate={model.EndDate}", model.FunderIds, model.StartDate, model.EndDate);
        try
        {
            model.Take = 0;
            var claimFollowUpResponses = await paymentAdjustmentService.GetClaimFollowUpReportAsync(model, cancellationToken);
            await MapCreatedByResult(_rethinkServices, model, claimFollowUpResponses);

            var excelFile = await paymentAdjustmentService.ExportToExcelClaimFollowAsync(model, claimFollowUpResponses.claimFollowUps, cancellationToken);
            
            var base64Excel = Convert.ToBase64String(excelFile);

            logger.LogInformation($"{nameof(PaymentAdjustmentController)}:Successfully exported claim follow up report to Excel for funderIds={model.FunderIds}, startDate={model.StartDate}, endDate={model.EndDate}", model.FunderIds, model.StartDate, model.EndDate);
            return Ok(new { data = base64Excel });
        }
        catch (Exception ex)
        {
            logger.LogError($"{nameof(PaymentAdjustmentController)}:Error getting the details of funder ids = {model.FunderIds} with startDate = {model.StartDate} and endDate = {model.EndDate} \n Error: {ex.Message}");
            logger.LogError(ex, $"Error getting the details of funder ids = {model.FunderIds} with startDate = {model.StartDate} and endDate = {model.EndDate} \n Error: {ex.Message}");
            return StatusCode(500, ex.Message);
        }
    }
}
