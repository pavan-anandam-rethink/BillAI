using BillingService.Web.Helpers.HttpClients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace BillingService.Web.Controllers
{
    public class BaseController : ControllerBase
    {
        private readonly IBaseHttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string baseUrl = "BillingUrl";

        public BaseController(IBaseHttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }
    }
}