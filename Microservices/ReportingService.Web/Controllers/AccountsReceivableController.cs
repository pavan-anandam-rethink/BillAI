using Microsoft.AspNetCore.Mvc;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.ReportingModels;
using SummationService.Domain.Interfaces;

namespace ReportingService.Web.Controllers;

[Area("Reporting")]
[Route("[controller]/[action]")]
public class AccountsReceivableController(
    IAccountsReceivableService accountsReceivableService,
    ILogger<AccountsReceivableController> logger,
    IRethinkMasterDataMicroServices _rethinkServices) : BaseV1Controller
{
    [HttpPost]
    public async Task<IActionResult> AddOrUpdateAccountsReceivable([FromBody] ClaimTransactionModel model, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation($"{nameof(AccountsReceivableController)}: Adding or updating accounts receivable. claimId={model.TransactionTypeId}, TransactionType={(ClaimTransactionType)model.TransactionType}");
            await accountsReceivableService.AddOrUpdateAccountsReceivableAsync((ClaimTransactionType)model.TransactionType, model.TransactionTypeId, cancellationToken);
            logger.LogInformation($"{nameof(AccountsReceivableController)}: Successfully added or updated accounts receivable. claimId={model.TransactionTypeId}, TransactionType={(ClaimTransactionType)model.TransactionType}");
            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogError($"{nameof(AccountsReceivableController)}:Error adding claim details. claimId={model.TransactionTypeId} and TransactionType = {(ClaimTransactionType)model.TransactionType} \n Error: {ex.Message}");
            logger.LogError(ex, $"Error adding claim details. claimId={model.TransactionTypeId} and TransactionType = {(ClaimTransactionType)model.TransactionType} \n Error: {ex.Message}");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetAccountsReceivables([FromBody] AccountsRecievablesRequestModel model, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation($"{nameof(AccountsReceivableController)}: Getting accounts receivables for closing date = {model.closingDate} and funder ids={model.PayerOrFunder}");
            var result = await accountsReceivableService.GetAccountsReceivablesAsync(model, cancellationToken);

            var clientUsersList = await _rethinkServices.GetChildProfilesForAccount(model.AccountInfoId);
            result.AccountsReceivables.Where(x => x.ClientFirstName.Trim() == "").ToList().ForEach(x =>
            {
                var patientDetail = clientUsersList.FirstOrDefault(p => p.Id == x.ClientId && p.DateDeleted == null);
                x.ClientFirstName = patientDetail?.FirstName ?? string.Empty;
                x.ClientLastName = patientDetail?.LastName ?? string.Empty;
            });

            logger.LogInformation($"{nameof(AccountsReceivableController)}: Successfully retrieved accounts receivables for closing date = {model.closingDate} and funder ids={model.PayerOrFunder}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError($"{nameof(AccountsReceivableController)}:Error getting the details of closing date = {model.closingDate} and funder ids={model.PayerOrFunder}\n Error: {ex.Message}");
            logger.LogError(ex, $"Error getting the details of closing date = {model.closingDate} and funder ids={model.PayerOrFunder}\n Error: {ex.Message}");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetAccountsReceivablesChargeLevel([FromBody] AccountsRecievablesRequestModel model, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation($"{nameof(AccountsReceivableController)}: Getting accounts receivables charge level for closing date = {model.closingDate} and funder ids={model.PayerOrFunder}");
            var result = await accountsReceivableService.GetAccountsReceivablesChargeLevelAsync(model, cancellationToken);

            var clientUsersList = await _rethinkServices.GetChildProfilesForAccount(model.AccountInfoId);
            result.AccountsReceivables.Where(x => string.IsNullOrWhiteSpace(x.ClientFirstName)).ToList().ForEach(x =>
            {
                var patientDetail = clientUsersList.FirstOrDefault(p => p.Id == x.ClientId && p.DateDeleted == null);
                x.ClientFirstName = patientDetail?.FirstName ?? string.Empty;
                x.ClientLastName = patientDetail?.LastName ?? string.Empty;
            });

            logger.LogInformation($"{nameof(AccountsReceivableController)}: Successfully retrieved accounts receivables charge level for closing date = {model.closingDate} and funder ids={model.PayerOrFunder}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError($"{nameof(AccountsReceivableController)}:Error getting the details of closing date = {model.closingDate} and funder ids={model.PayerOrFunder}\n Error: {ex.Message}");
            logger.LogError(ex, $"Error getting the details of closing date = {model.closingDate} and funder ids={model.PayerOrFunder}\n Error: {ex.Message}");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("{accountInfoId}")]
    public async Task<IActionResult> GetFunders(CancellationToken cancellationToken, int accountInfoId = 0)
    {
        try
        {
            logger.LogInformation($"{nameof(AccountsReceivableController)}: Getting funders for accountInfoId={accountInfoId}");
            if (accountInfoId == 0)
            {
                var result = await accountsReceivableService.GetFundersAsync(cancellationToken);
                logger.LogInformation($"{nameof(AccountsReceivableController)}: Successfully retrieved all funders.");
                return Ok(result);
            }

            var data = await _rethinkServices.GetAllFundersForAccount(accountInfoId);
            if (data?.Count > 0)
            {

                var results = new List<FunderDetailsResponseModel>();
                data.Where(x => x.isActive == true).ToList().ForEach(funder =>
                {
                    results.Add(new FunderDetailsResponseModel
                    {
                        FunderId = funder.id,
                        FunderName = funder.funderName,
                    });
                });

                logger.LogInformation($"{nameof(AccountsReceivableController)}: Successfully retrieved funders for account {accountInfoId}.");
                return Ok(results);
            }

            logger.LogInformation($"{nameof(AccountsReceivableController)}:No funders found for the account {accountInfoId}");
            return NotFound($"Fuders not found for the account {accountInfoId}");
        }
        catch (Exception ex)
        {
            logger.LogError($"{nameof(AccountsReceivableController)}:Error getting the details of Funder\n Error: {ex.Message}");
            logger.LogError(ex, $"Error getting the details of Funder\n Error: {ex.Message}");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> ExportToExcel([FromBody] AccountsRecievablesRequestModel model, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation($"{nameof(AccountsReceivableController)}: Exporting accounts receivables to Excel for closing date = {model.closingDate} and funder ids={model.PayerOrFunder}");
            var result = await accountsReceivableService.GetAccountsReceivablesAsync(model, cancellationToken);

            var clientUsersList = await _rethinkServices.GetChildProfilesForAccount(model.AccountInfoId);
            result.AccountsReceivables.Where(x => x.ClientFirstName.Trim() == "").ToList().ForEach(x =>
            {
                var patientDetail = clientUsersList.FirstOrDefault(p => p.Id == x.ClientId && p.DateDeleted == null);
                x.ClientFirstName = patientDetail?.FirstName ?? string.Empty;
                x.ClientLastName = patientDetail?.LastName ?? string.Empty;
            });

            var excelFile = await accountsReceivableService.ExportToExcelAsync(model, result, cancellationToken);

            var base64Excel = Convert.ToBase64String(excelFile);

            logger.LogInformation($"{nameof(AccountsReceivableController)}: Successfully exported accounts receivables to Excel.");
            return Ok(new { data = base64Excel });
        }
        catch (Exception ex)
        {
            logger.LogError($"{nameof(AccountsReceivableController)}:Error getting the details of closing date = {model.closingDate} and funder ids={model.PayerOrFunder}\n Error: {ex.Message}");
            logger.LogError(ex, $"Error getting the details of closing date = {model.closingDate} and funder ids={model.PayerOrFunder}\n Error: {ex.Message}");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<IActionResult> ExportToExcelChargeLevel([FromBody] AccountsRecievablesRequestModel model, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation($"{nameof(AccountsReceivableController)}: Exporting accounts receivables charge level to Excel for closing date = {model.closingDate} and funder ids={model.PayerOrFunder}");
            model.Take = 0;
            var result = await accountsReceivableService.GetAccountsReceivablesChargeLevelAsync(model, cancellationToken);

            var clientUsersList = await _rethinkServices.GetChildProfilesForAccount(model.AccountInfoId);
            result.AccountsReceivables.Where(x => string.IsNullOrWhiteSpace(x.ClientFirstName)).ToList().ForEach(x =>
            {
                var patientDetail = clientUsersList.FirstOrDefault(p => p.Id == x.ClientId && p.DateDeleted == null);
                x.ClientFirstName = patientDetail?.FirstName ?? string.Empty;
                x.ClientLastName = patientDetail?.LastName ?? string.Empty;
            });

            var excelFile = await accountsReceivableService.ExportToExcelChargeLevelAsync(model, result, cancellationToken);

            var base64Excel = Convert.ToBase64String(excelFile);

            logger.LogInformation($"{nameof(AccountsReceivableController)}: Successfully exported accounts receivables charge level to Excel.");
            return Ok(new { data = base64Excel });
        }
        catch (Exception ex)
        {
            logger.LogError($"{nameof(AccountsReceivableController)}:Error getting the details of closing date = {model.closingDate} and funder ids={model.PayerOrFunder}\n Error: {ex.Message}");
            logger.LogError(ex, $"Error getting the details of closing date = {model.closingDate} and funder ids={model.PayerOrFunder}\n Error: {ex.Message}");
            return StatusCode(500, "Internal server error");
        }
    }
}
