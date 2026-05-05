using BillingService.Web.Servers;
using Microsoft.AspNetCore.Mvc;

namespace BillingService.Web.Controllers
{
    [Route("pusher/auth")]
    public class PusherAuthController : ControllerBase
    {
        private readonly IPusherNotificationServer _pusherServer;

        public PusherAuthController(IPusherNotificationServer pusherService)
        {
            _pusherServer = pusherService;
        }

        [HttpPost]
        public IActionResult Authorize([FromForm] string channel_name, [FromForm] string socket_id)
        {
            var auth = _pusherServer.Authenticate(channel_name, socket_id);
            return new JsonResult(auth);
        }
    }
}
