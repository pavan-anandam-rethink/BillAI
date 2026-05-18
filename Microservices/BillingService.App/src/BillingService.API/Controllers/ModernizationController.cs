using BillingService.App.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BillingService.App.API.Controllers;

[ApiController]
[Authorize]
[Route("internal/[controller]/[action]")]
public sealed class ModernizationController : ControllerBase
{
    private readonly BillingModernizationSettings _settings;

    public ModernizationController(IOptions<BillingModernizationSettings> settings)
    {
        _settings = settings.Value;
    }

    [HttpGet]
    public IActionResult Status()
    {
        if (!_settings.ExposeModernizationStatusEndpoint)
        {
            return NotFound();
        }

        return Ok(new
        {
            _settings.UseLegacyProxyFallback,
            _settings.ClaimHeadersMigrationMode,
            _settings.UseCqrsDashboard,
            _settings.ClaimHeaderTtlSeconds,
            _settings.DashboardSummaryTtlSeconds
        });
    }
}

