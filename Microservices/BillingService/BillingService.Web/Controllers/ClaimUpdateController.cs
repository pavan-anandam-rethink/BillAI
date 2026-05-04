using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Web.Helpers.HttpClients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BillingService.Web.Controllers
{
    [Area("Billing")]
    [Route("[controller]/[action]")]
    public class ClaimUpdateController : BaseController
    {
        private readonly IBaseHttpClient baseHttpClient;
        private readonly IConfiguration configuration;
        private readonly IClaimUpdateService _claimUpdateService;
        private readonly ILogger<ClaimUpdateController> _logger;
        public ClaimUpdateController(
            IBaseHttpClient httpClient,
            IConfiguration configuration,
            IClaimUpdateService claimUpdateService, ILogger<ClaimUpdateController> logger
            ) : base(httpClient, configuration)
        {
            this.baseHttpClient = httpClient;
            this.configuration = configuration;
            _claimUpdateService = claimUpdateService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> UpdateClaimIfSecondaryFunderPresent([FromBody] IdWithUserInfo model)
        {
            try
            {
                _logger.LogInformation("{Controller}.{Action} - Updating claim secondary funder on refresh. MemberId={MemberId}, ClaimId={ClaimId}",
                    nameof(ClaimUpdateController),
                    nameof(UpdateClaimIfSecondaryFunderPresent),
                    model?.MemberId, model?.Id);
                var result = await _claimUpdateService.UpdateClaimSecondaryFunderOnRefresh(model.AccountInfoId, model.MemberId, model.Id);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimUpdateController)}.{nameof(UpdateClaimIfSecondaryFunderPresent)} -Failed to update claim secondary funder on refresh. MemberId={model?.MemberId}, claimId={model?.Id}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }
    }
}
