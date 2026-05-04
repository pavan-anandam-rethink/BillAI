using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.Claims.WriteOff;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Web.Controllers
{
    [Area("Billing")]
    [Route("[controller]/[action]")]
    public class WriteOffController : Controller
    {
        private readonly IWriteOffService _writeOffService;
        private readonly ILogger<WriteOffController> _logger;

        public WriteOffController(IWriteOffService writeOffService, ILogger<WriteOffController> logger)
        {
            _writeOffService = writeOffService;
            _logger = logger;
        }


        [HttpPost]
        public async Task<IActionResult> AddWriteOff([FromBody] WriteOffClaimModelWithUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - AddWriteOff called. ClaimId={ClaimId}, MemberId={MemberId}",
                nameof(WriteOffController),
                nameof(AddWriteOff),
                model?.ClaimId,
                model?.MemberId);

            try
            {
                var result = await _writeOffService.AddAsync(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(WriteOffController)}.{nameof(AddWriteOff)} - AddWriteOff failed. ClaimId={model?.ClaimId}, MemberId={model?.MemberId}, ErrorMsg ={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetChargeEntryWriteOffsByChargeId([FromBody] GetChargeEntryWriteOffModel model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetChargeEntryWriteOffsByChargeId called. ChargeId={ChargeId}",
                nameof(WriteOffController),
                nameof(GetChargeEntryWriteOffsByChargeId),
                model?.Id);

            try
            {
                var result = await _writeOffService.GetChargeEntryWriteOffsByChargeIdAsync(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(WriteOffController)}.{nameof(GetChargeEntryWriteOffsByChargeId)} - GetChargeEntryWriteOffsByChargeId failed. ChargeId={model?.Id}, ErrorMsg ={ex.Message}");

                return BadRequest(ex.Message);
            }

        }

        [HttpPost]
        public async Task<IActionResult> DeleteChargeEntryWriteOffsByCharge([FromBody] IdsWithUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - DeleteChargeEntryWriteOffsByCharge called. IdCount={Count}",
                nameof(WriteOffController),
                nameof(DeleteChargeEntryWriteOffsByCharge),
                model?.Ids?.Count());

            try
            {
                await _writeOffService.DeleteChargeEntryWriteOffsByChargeIdAsync(model);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(WriteOffController)}.{nameof(DeleteChargeEntryWriteOffsByCharge)} - DeleteChargeEntryWriteOffsByCharge failed. Ids={(model?.Ids != null ? string.Join(",", model.Ids) : "null")}, ErrorMsg ={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateChargeEntryWriteOffsByChargeId([FromBody] EditChargeEntryWriteOffModelWithUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - UpdateChargeEntryWriteOffsByChargeId called. ClaimId={ClaimId}",
                nameof(WriteOffController),
                nameof(UpdateChargeEntryWriteOffsByChargeId),
                model?.ClaimId);

            try
            {
                var result = await _writeOffService.UpdateChargeEntryWriteOffsByChargeIdAsync(model);
                return Ok(result.FirstOrDefault());
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(WriteOffController)}.{nameof(UpdateChargeEntryWriteOffsByChargeId)} - UpdateChargeEntryWriteOffsByChargeId failed. ClaimId={model?.ClaimId}, ErrorMsg ={ex.Message}");

                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetReasonCodes()
        {
            _logger.LogInformation("{Controller}.{Action} - GetReasonCodes called.",
                nameof(WriteOffController),
                nameof(GetReasonCodes));

            try
            {
                var result = await _writeOffService.GetReasonCodesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(WriteOffController)}.{nameof(GetReasonCodes)} - GetReasonCodes failed. ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message);
            }
        }

    }
}