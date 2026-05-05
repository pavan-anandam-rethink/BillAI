using ClearingHouseService.Web.Service;
using ClearingHouseService.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClearingHouseService.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClearinghouseValidationController : ControllerBase
    {
        private readonly IEdiUploadService _ediUploadService;
        private readonly ILogger<ClearinghouseValidationController> _logger;

        public ClearinghouseValidationController(
            IEdiUploadService ediUploadService,
            ILogger<ClearinghouseValidationController> logger)
        {
            _ediUploadService = ediUploadService;
            _logger = logger;
        }

        /// <summary>
        /// Validates SFTP credentials for all active clearinghouses (Availity, Stedi)
        /// </summary>
        [HttpGet("validate-credentials")]
        [ProducesResponseType(typeof(ClearinghouseCredentialValidationResponse), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ValidateCredentials()
        {
            _logger.LogInformation(
                "Clearinghouse credentials validation endpoint called. Timestamp={Timestamp}",
                DateTime.UtcNow);

            try
            {
                var result = await _ediUploadService.ValidateAllClearinghousesAsync();

                if (!result.AllValid)
                {
                    _logger.LogWarning(
                        "Clearinghouse validation completed with failures. Failed={FailedCount}/{TotalCount}",
                        result.FailedValidations,
                        result.TotalClearinghouses);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during clearinghouse credentials validation");

                return StatusCode(500, new
                {
                    error = "An error occurred while validating clearinghouse credentials",
                    message = "An unexpected error occurred while validating clearinghouse credentials.",
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
