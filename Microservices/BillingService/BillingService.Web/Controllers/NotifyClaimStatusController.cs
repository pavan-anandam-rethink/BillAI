using BillingService.Web.Helpers.HttpClients;
using BillingService.Web.Servers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Models.Claim;
using System;
using System.Threading.Tasks;

namespace BillingService.Web.Controllers
{
    [Area("Billing")]
    [Route("[controller]/[action]")]
    public class NotifyClaimStatusController : BaseController
    {
        private readonly IPusherNotificationServer _pusherService;
        private readonly ILogger<NotifyClaimStatusController> _logger;

        public NotifyClaimStatusController(IPusherNotificationServer pusherService,
            IBaseHttpClient httpClient,
            IConfiguration configuration, ILogger<NotifyClaimStatusController> logger)
            : base(httpClient, configuration)
        {
            _pusherService = pusherService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Notify([FromBody] ClaimStatusUpdate update)
        {
            try
            {
                _logger.LogInformation("Sending claim status notification. AccountId={AccountId}, UserId={UserId}, ClaimId={ClaimId}",
                update.AccountId, update.UserId, update.ClaimId);
                var channelName = $"private-account-{update.AccountId}-user-{update.UserId}";

                await _pusherService.TriggerAsync(channelName, "claim-status-updated", new
                {
                    batchId = update.BatchId,
                    total = update.Total,
                    claimId = update.ClaimId,
                    status = update.Status,
                    message = update.Message
                });
               
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(NotifyClaimStatusController)}.{nameof(Notify)} -Failed to send claim status notification. AccountId={update.AccountId}, UserId={update.UserId}, ClaimId={update.ClaimId}, ErrorMsg={ex.Message}");
                 return StatusCode(500, ex.Message);
            }
        }

    }
}
