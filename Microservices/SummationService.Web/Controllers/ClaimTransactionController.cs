using Microsoft.AspNetCore.Mvc;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Models.Claim;
using SummationService.Domain.Interfaces;

namespace SummationService.Web.Controllers;

[Area("Summation")]
[Route("[controller]/[action]")]
public class ClaimTransactionController(
    IClaimTransactionService claimTransactionService,
    IChargeTransactionService chargeTransactionService,
    ILogger<ClaimTransactionController> logger) : BaseV1Controller
{

    [HttpPost]
    public async Task<IActionResult> AddOrUpdateClaimTransaction([FromBody] ClaimTransactionModel model, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                $"{nameof(ClaimTransactionController)}:Received AddOrUpdateClaimTransaction request. TransactionType={model.TransactionType}, TransactionTypeId={model.TransactionTypeId}",
                (ClaimTransactionType)model.TransactionType,
                model.TransactionTypeId);

            await claimTransactionService.AddOrUpdateClaimTransactionAsync((ClaimTransactionType)model.TransactionType, model.TransactionTypeId, cancellationToken);

            logger.LogInformation(
               $"{nameof(ClaimTransactionController)}:Successfully processed AddOrUpdateClaimTransaction. TransactionType={model.TransactionType}, TransactionTypeId={model.TransactionTypeId}",
               (ClaimTransactionType)model.TransactionType,
               model.TransactionTypeId);

            return Ok();
        }
        catch (Exception ex) 
        {
            logger.LogError($"{nameof(ClaimTransactionController)}:Error adding claim details. claimId={model.TransactionTypeId} and TransactionType = {(ClaimTransactionType)model.TransactionType} \n Error: {ex.Message}");
            logger.LogError(
                ex,
                "Error occurred while processing AddOrUpdateClaimTransaction. TransactionType={TransactionType}, TransactionTypeId={TransactionTypeId}",
                (ClaimTransactionType)model.TransactionType,
                model.TransactionTypeId);

            return StatusCode(500, "Internal server error");
        }
    }
}
