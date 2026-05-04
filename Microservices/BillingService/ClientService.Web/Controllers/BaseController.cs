using ClientService.Web.Helpers.HttpClients;
using Microsoft.AspNetCore.Mvc;

namespace ClientService.Web.Controllers
{
    public class BaseController : ControllerBase
    {
        private readonly IBaseHttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BaseController> _logger;
        private readonly string baseUrl = "BHUrl";

        public BaseController(IBaseHttpClient httpClient, IConfiguration configuration, ILogger<BaseController> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        protected async Task<IActionResult> SendAsync(string endpoint, object data)
        {
            var baseAddress = _configuration.GetValue<string>(baseUrl);
            _logger.LogInformation("BaseController.SendAsync started | BaseAddress: {BaseAddress}, Endpoint: {Endpoint}", baseAddress, endpoint);
            try
            {
                var responseMsg = await _httpClient.PostAsync(baseAddress, endpoint, data);
                if (responseMsg != null)
                {
                    _logger.LogInformation("BaseController.SendAsync succeeded");
                    return Ok(responseMsg);
                }
                _logger.LogWarning("BaseController.SendAsync returned null response ");
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BaseController.SendAsync failed | BaseAddress: {BaseAddress}, Endpoint: {Endpoint}", baseAddress, endpoint);
                throw;
            }
        }

        protected string GetEndpoint(ControllerContext context)
        {
            var actionDescriptor = context.ActionDescriptor;
            var endpoint = $"{actionDescriptor.ControllerName}/{actionDescriptor.ActionName}";
            _logger.LogDebug("Resolved endpoint | Controller: {Controller}, Action: {Action}, Endpoint: {Endpoint}", actionDescriptor.ControllerName, actionDescriptor.ActionName, endpoint);
            return endpoint;
        }
    }
}