using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Client;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Clients.History;
using BillingService.Web.Helpers.HttpClients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingService.Web.Controllers
{
    [Area("Billing")]
    [Route("[controller]")]
    public class ClientChargeHistoryController : BaseController
    {
        private readonly IClientChargeHistoryService _clientChargeHistoryService;
        private readonly IClientService _clientService;
        private readonly ICommonService _commonService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ClientChargeHistoryController> _logger;

        public ClientChargeHistoryController(
            IBaseHttpClient httpClient,
            IConfiguration configuration,
            IClientService clientService,
            ICommonService commonService,
            IClientChargeHistoryService clientChargeHistoryService,
            ILogger<ClientChargeHistoryController> logger
            )
            : base(httpClient, configuration)
        {
            _configuration = configuration;
            _clientService = clientService;
            _commonService = commonService;
            _clientChargeHistoryService = clientChargeHistoryService;
            _logger = logger;
        }

        [HttpPost("GetAllAuthorizationNumbers")]
        public async Task<ActionResult<string>> GetAllAuthorizationNumbers([FromBody] UserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetAllAuthorizationNumbers called. MemberId={MemberId}",
                nameof(ClientChargeHistoryController),
                nameof(GetAllAuthorizationNumbers),
                model.MemberId);

            try
            {
                var data = await _clientChargeHistoryService.GetAllAuthorizationNumbersAsync(model);

                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClientChargeHistoryController)}.{nameof(GetAllAuthorizationNumbers)} - GetAllAuthorizationNumbers failed. MemberId={model.MemberId}, ErrorMsg ={ex.Message}");

                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("history/{accountInfoId}")]
        public async Task<ActionResult<IEnumerable<int>>> GetClientHistoryClaim(int accountInfoId)
        {
            _logger.LogInformation("{Controller}.{Action} - GetClientHistoryClaim called. AccountInfoId={AccountInfoId}",
                    nameof(ClientChargeHistoryController),
                    nameof(GetClientHistoryClaim),
                    accountInfoId);

            try
            {
                var data = await _clientChargeHistoryService.GetClientHistoryClaimAsync(accountInfoId);

                if (data == null)
                {
                    _logger.LogInformation("{Controller}.{Action} - No client history claims found. AccountInfoId={AccountInfoId}",
                        nameof(ClientChargeHistoryController),
                        nameof(GetClientHistoryClaim),
                        accountInfoId);
                    return NotFound("No claim history found for this account.");
                }

                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClientChargeHistoryController)}.{nameof(GetClientHistoryClaim)} - GetClientHistoryClaim failed. AccountInfoId={accountInfoId}, ErrorMsg ={ex.Message}");

                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("GetClientRecords")]
        public async Task<IActionResult> GetClientRecords([FromBody] ClientHistoryRequestModel clientHistoryRequest)
        {
            _logger.LogInformation("{Controller}.{Action} - GetClientRecords called.",
                nameof(ClientChargeHistoryController),
                nameof(GetClientRecords));

            try
            {
                var result = await _clientChargeHistoryService.GetClientRecordAsync(
                    clientHistoryRequest.clientHistoryRequest,
                    clientHistoryRequest.clientRecordFilterModel);

                return Ok(result);

            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClientChargeHistoryController)}.{nameof(GetClientRecords)} - GetClientRecords failed. ErrorMsg ={ex.Message}");

                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("GetClientChargeHistoryDetails")]
        public async Task<IActionResult> GetClientChargeHistoryDetails([FromBody] ClientHistoryChargeDetailsRequestModel clientHistoryChargeDetailsRequest)
        {
            _logger.LogInformation("{Controller}.{Action} - GetClientChargeHistoryDetails called.",
                nameof(ClientChargeHistoryController),
                nameof(GetClientChargeHistoryDetails));

            try
            {
                var result = await _clientChargeHistoryService.GetClientChargeHistoryDetailsAsync(
                    clientHistoryChargeDetailsRequest.clientHistoryChargeDetailsRequest,
                    clientHistoryChargeDetailsRequest.clientHistoryChargeFilterModel);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClientChargeHistoryController)}.{nameof(GetClientChargeHistoryDetails)} - GetClientChargeHistoryDetails failed. ErrorMsg ={ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("Search")]
        public async Task<IActionResult> SearchInvoices([FromBody] InvoiceHistoryRequestModel request)
        {
            // Validate request first (existing behavior)
            if (request == null ||
                request.InvoiceHistoryRequest == null ||
                request.InvoiceHistoryRequestFilterModel == null)
            {
                _logger.LogError($"{nameof(ClientChargeHistoryController)}.{nameof(SearchInvoices)} - Invalid invoice search request. Request or required properties are null.");
                return BadRequest("Request or required properties are null.");
            }

            _logger.LogInformation("{Controller}.{Action} - SearchInvoices called.",
                nameof(ClientChargeHistoryController),
                nameof(SearchInvoices));

            try
            {
                var result = await _clientChargeHistoryService.InvoicesSearchAsync(
                    request.InvoiceHistoryRequest,
                    request.InvoiceHistoryRequestFilterModel);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClientChargeHistoryController)}.{nameof(SearchInvoices)} - SearchInvoices failed. ErrorMsg ={ex.Message}");

                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
