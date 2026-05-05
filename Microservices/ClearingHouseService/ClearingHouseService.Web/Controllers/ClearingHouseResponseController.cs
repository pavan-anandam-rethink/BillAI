using BillingService.Domain.Interfaces.Billing;
using Microsoft.AspNetCore.Mvc;
using Rethink.Services.Common.Models.Claim;

namespace ClearingHouseService.Web.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ClearingHouseResponseController : ControllerBase
    {
        private readonly ILogger<ClearingHouseResponseController> _logger;
        private readonly IEligibility271ResponseService _eligibility271ResponseService;

        public ClearingHouseResponseController(ILogger<ClearingHouseResponseController> logger,
             IEligibility271ResponseService eligibility271ResponseService)
        {
            _logger = logger;
            _eligibility271ResponseService = eligibility271ResponseService;
        }
    
        [HttpPost("Get271EligibilityResponse")]
        public async Task<IActionResult> GetEligibilityResponse([FromBody] EligibilityRequest requestModel)
        {
            if (requestModel == null)
            {
                _logger.LogWarning($"{nameof(ClearingHouseResponseController)}: Request body is null.");
                return BadRequest("Request body cannot be null.");
            }

            var response = await _eligibility271ResponseService.GetEligibilityResponse(requestModel);

            if (response == null)
            {
                _logger.LogInformation($"{nameof(ClearingHouseResponseController)}: No eligibility data found for FunderId: {requestModel.FunderId}");
                return NotFound("No matching eligibility data found.");
            }

            _logger.LogInformation($"{nameof(ClearingHouseResponseController)}: Successfully retrieved eligibility response for FunderId: {requestModel.FunderId}");

            return Ok(response);
        }
    }
}
