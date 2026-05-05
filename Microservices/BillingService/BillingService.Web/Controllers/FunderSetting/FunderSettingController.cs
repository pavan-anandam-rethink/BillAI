using BillingService.Domain.Interfaces.FunderSetting;
using BillingService.Domain.Models.BillingSettings;
using BillingService.Web.Helpers.HttpClients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace BillingService.Web.Controllers.FunderSetting;

[Area("FunderSetting")]
[Route("[controller]/[action]")]
public class FunderSettingController : BaseController
{
    private readonly IFunderSettingService _funderSettingService;

    public FunderSettingController(IBaseHttpClient httpClient, 
                                   IConfiguration configuration,
                                   IFunderSettingService funderSettingService
                                  ) : base(httpClient, configuration)
    {
        _funderSettingService = funderSettingService;
    }

    /// <summary>
    /// Updates the funder settings using the specified request model.
    /// </summary>
    /// <remarks>This method requires authentication. A 400 status is returned if the input model is
    /// invalid, a 401 status if the user is unauthorized, and a 500 status for server errors.</remarks>
    /// <param name="model">The request model containing the funder settings to be updated. Cannot be null.</param>
    /// <returns>An IActionResult that indicates the result of the operation. Returns a 204 No Content status if the update
    /// is successful.</returns>
    [HttpPost]
    [ProducesResponseType(statusCode: 204)]
    [ProducesResponseType(statusCode: 400)]
    [ProducesResponseType(statusCode: 401)]
    [ProducesResponseType(statusCode: 500)]
    public async Task<IActionResult> SaveFunderSettings([FromBody] FunderSettingRequest model)
    {
        await _funderSettingService.UpdateFunderSettingsAsync(model);
        return NoContent();
    }
}