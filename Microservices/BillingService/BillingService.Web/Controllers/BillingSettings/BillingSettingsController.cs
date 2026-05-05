using Azure.Storage.Blobs.Models;
using BillingService.Domain.Interfaces.BillingSettings;
using BillingService.Domain.Models.BillingSettings;
using BillingService.Domain.Services.BillingSetting;
using BillingService.Web.Helpers.HttpClients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace BillingService.Web.Controllers.BillingSettings
{
    [Area("Billing")]
    [Route("[controller]/[action]")]
    public class BillingSettingsController : BaseController
    {
        private readonly IBillingSettingsService _billingSettingsService;
        private readonly ILogger<BillingSettingsController> _logger;

        /// <summary>
        /// Initializes a new instance of the BillingSettingsController class to manage billing settings operations.
        /// </summary>
        /// <remarks>This constructor requires valid instances of IBaseHttpClient, IConfiguration, and
        /// IBillingSettingsService to enable billing settings management and communication with external
        /// services.</remarks>
        /// <param name="httpClient">The HTTP client used to send requests to external billing services. Cannot be null.</param>
        /// <param name="configuration">The configuration settings used to access application-specific options. Cannot be null.</param>
        /// <param name="billingSettingsService">The service responsible for handling billing settings operations. Cannot be null.</param>
        public BillingSettingsController(IBaseHttpClient httpClient,
            IConfiguration configuration,
            IBillingSettingsService billingSettingsService, ILogger<BillingSettingsController> logger) : base(httpClient, configuration)

        {
            _billingSettingsService = billingSettingsService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a collection of claim filing indicators available for processing claims.
        /// </summary>
        /// <remarks>This method is asynchronous and should be awaited. It may return a 400 status code
        /// for invalid requests, a 401 status code for unauthorized access, or a 500 status code for server
        /// errors.</remarks>
        /// <returns>An IActionResult containing a model of claim filing indicators. Returns a 200 status code with the model on
        /// success, or an appropriate error code on failure.</returns>
        [HttpGet]
        [ProducesResponseType(statusCode: 200, type: typeof(ClaimFilingIndicatorModel))]
        [ProducesResponseType(statusCode: 400)]
        [ProducesResponseType(statusCode: 401)]
        [ProducesResponseType(statusCode: 500)]
        public async Task<IActionResult> GetClaimFilingIndicators()
        {
            var result = await _billingSettingsService.GetClaimFilingIndicators();
            return Ok(result);
        }


        /// <summary>
        /// Updates the billing funder settings using the specified request model.
        /// </summary>
        /// <remarks>This method requires authentication. A 400 status is returned if the input model is
        /// invalid, a 401 status if the user is unauthorized, and a 500 status for server errors.</remarks>
        /// <param name="model">The request model containing the billing funder settings to be updated. Cannot be null.</param>
        /// <returns>An IActionResult that indicates the result of the operation. Returns a 204 No Content status if the update
        /// is successful.</returns>
        [HttpPost]
        [ProducesResponseType(statusCode: 204)]
        [ProducesResponseType(statusCode: 400)]
        [ProducesResponseType(statusCode: 401)]
        [ProducesResponseType(statusCode: 500)]
        public async Task<IActionResult> SetBillingFunderSettings([FromBody] BillingFunderSettingRequestModel model)
        {
            await _billingSettingsService.SetBillingFunderSettings(model);
            return NoContent();
        }

        /// <summary>
        /// Retrieves a list of billing funder settings based on the specified filtering, sorting, and pagination criteria.
        /// </summary>
        /// <remarks>This method is asynchronous and should be awaited. Returns billing funder settings with associated
        /// timezone information. A 400 status code is returned for invalid requests, 401 for unauthorized access, and 500
        /// for server errors.</remarks>
        /// <param name="model">The request model containing filtering, sorting, and pagination parameters. Cannot be null.</param>
        /// <returns>An IActionResult containing a list of billing funder settings. Returns a 200 status code with the list on
        /// success, or an appropriate error code on failure.</returns>
        [HttpPost]
        [ProducesResponseType(statusCode: 200, type: typeof(List<BillingFunderSettings>))]
        [ProducesResponseType(statusCode: 400)]
        [ProducesResponseType(statusCode: 401)]
        [ProducesResponseType(statusCode: 500)]
        public async Task<IActionResult> GetBillingFunderSettings([FromBody] BillingFunderListRequestModel model)
        {
            var result = await _billingSettingsService.GetBillingFunderSettings(model);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves billing funder settings for a specific funder and account combination.
        /// </summary>
        /// <remarks>This method validates the funderId and accountInfoId parameters and returns the associated billing funder
        /// settings. A 400 status code is returned for invalid parameters, 404 if the setting is not found, 401 for unauthorized
        /// access, and 500 for server errors.</remarks>
        /// <param name="funderId">The unique identifier of the funder. Must be greater than zero.</param>
        /// <param name="accountInfoId">The unique identifier of the account. Must be greater than zero.</param>
        /// <returns>An IActionResult containing the billing funder settings. Returns a 200 status code with the settings on success,
        /// 400 for invalid parameters, 404 if not found, or an appropriate error code on failure.</returns>
        [HttpGet()]
        [ProducesResponseType(statusCode: 200, type: typeof(BillingFunderIdRequestModel))]
        [ProducesResponseType(statusCode: 400)]
        [ProducesResponseType(statusCode: 401)]
        [ProducesResponseType(statusCode: 500)]
        public async Task<IActionResult> GetBillingFunderIdsSetting([FromQuery] int funderId,
                [FromQuery] int accountInfoId)
        {
            if (funderId <= 0 || accountInfoId <= 0)
                return BadRequest("Invalid FunderId or AccountInfoId.");

            var result = await _billingSettingsService.GetBillingFunderIdsSettingAsync(funderId, accountInfoId);

            if (result == null)
                return NotFound("Billing Funder Setting not found.");

            return Ok(result);
        }

        /// <summary>
        /// Performs a soft delete of a funder setting identified by the specified ID.
        /// </summary>
        /// <remarks>This method marks the funder setting as deleted without physically removing it from the database.
        /// Returns 400 for invalid ID, 401 for unauthorized access, 404 if the funder setting is not found, and 500
        /// for server errors.</remarks>
        /// <param name="id">The unique identifier of the funder setting to delete. Must be greater than zero.</param>
        /// <returns>An IActionResult containing the deletion result. Returns a 200 status code with the deletion response on success,
        /// or an appropriate error code on failure.</returns>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(statusCode: 200, type: typeof(BillingFunderSettings))]
        [ProducesResponseType(statusCode: 400)]
        [ProducesResponseType(statusCode: 401)]
        [ProducesResponseType(statusCode: 500)]
        public async Task<IActionResult> DeleteFunder(int id)
        {
            try
            {
                var result = await _billingSettingsService.DeleteFunderSetting(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting FunderSetting Id: {Id}", id);

                return StatusCode(500, new BillingFunderSettingAPIResponse
                {
                    Success = false,
                    Message = "An unexpected error occurred."
                });
            }
        }

        /// <summary>
        /// Retrieves the billing features configured for a specific account.
        /// </summary>
        /// <remarks>This method validates the accountId parameter and returns the list of features with their enabled status.
        /// Returns 400 for invalid or missing accountId, 404 if the account is not found, 401 for unauthorized access, and 500
        /// for server errors.</remarks>
        /// <param name="accountId">The unique identifier of the account. Must be greater than zero.</param>
        /// <returns>An IActionResult containing the list of billing features and their status. Returns a 200 status code with the
        /// feature list on success, 400 for invalid parameters, 404 if account not found, or an appropriate error code on failure.</returns>
        [HttpGet]
        [ProducesResponseType(statusCode: 200, type: typeof(List<FeatureStatusDto>))]
        [ProducesResponseType(statusCode: 400)]
        [ProducesResponseType(statusCode: 401)]
        [ProducesResponseType(statusCode: 404)]
        [ProducesResponseType(statusCode: 500)]
        public async Task<IActionResult> GetBillingFeatures([FromQuery] int? accountId)
        {
            if (accountId is null or <= 0)
                return BadRequest("accountId is required and must be greater than zero.");

            _logger.LogInformation(
                "{Controller}.{Action} called. AccountId={AccountId}",
                nameof(BillingSettingsController),
                nameof(GetBillingFeatures),
                accountId.Value);

            try
            {
                var result = await _billingSettingsService.GetFeaturesForAccountAsync(accountId.Value);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Account not found: {AccountId}. Exception: {Message}", accountId, ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in GetFeatures. AccountId={AccountId}", accountId.Value);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// GET: /BillingSettings/GetBillingSettings/{accountId}
        /// Returns invoicing & statements configuration for the account.
        /// - Returns saved settings if present
        /// - Returns default values if none are present
        /// </summary>
        [HttpGet("{accountId:int}")]
        public async Task<IActionResult> GetBillingSettingInformation([FromRoute] int accountId)
        {
            _logger.LogInformation("{Controller}.{Action} - Getting billing settings. AccountId={AccountId}",
                nameof(BillingSettingsController),
                nameof(GetBillingSettingInformation),
                accountId);

            if (accountId <= 0)
            {
                _logger.LogWarning("{Controller}.{Action} - Invalid accountId supplied: {AccountId}",
                    nameof(BillingSettingsController), nameof(GetBillingSettingInformation), accountId);
                return BadRequest("accountId is required and must be greater than zero.");
            }

            try
            {
                var settings = await _billingSettingsService.GetBillingSettingInformationAsync(accountId);
                return Ok(settings);
            }
            catch (KeyNotFoundException knf)
            {
                _logger.LogWarning(knf, "Billing settings not found for accountId={AccountId}", accountId);
                return NotFound(knf.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Controller}.{Action} - Error retrieving billing settings. AccountId={AccountId}",
                    nameof(BillingSettingsController), nameof(GetBillingSettingInformation), accountId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{accountId:int}")]
        public async Task<IActionResult> GetDefaultBilling(int accountId)
        {
            if (accountId <= 0)
                return BadRequest("Invalid accountId");
            try
            {
                var result = await _billingSettingsService.GetDefaultBillingFromMainLocationAsync(accountId);

                return Ok(result);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving default billing for AccountId: {AccountId}", accountId);

                return StatusCode(500, new BillingFunderSettingAPIResponse
                {
                    Success = false,
                    Message = "An unexpected error occurred."
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveBillingSettings([FromBody] SaveBillingSettingRequest request, int memberId)
        {
            _logger.LogInformation(
                "{Controller}.{Action} called for AccountId={AccountId}",
                nameof(BillingSettingsController),
                nameof(SaveBillingSettings),
                request?.AccountId);
            if (request.AccountId <= 0)
            {
                _logger.LogWarning("{Controller}.{Action} - Invalid accountId supplied: {AccountId}",
                    nameof(BillingSettingsController), nameof(GetBillingSettingInformation), request.AccountId);
                return BadRequest("accountId is required and must be greater than zero.");
            }
            if (request == null)
                return BadRequest("Request cannot be null");
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var response = await _billingSettingsService.SaveBillingSettingInformationAsync(request, memberId);

                if (!response.Success)
                    return BadRequest(response.Error);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Controller}.{Action} - Error saving/updating billing settings. AccountId={AccountId}",
                  nameof(BillingSettingsController), nameof(GetBillingSettingInformation), request.AccountId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}