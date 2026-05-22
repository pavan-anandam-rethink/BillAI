using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models.RulesEngine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing
{
    public class ClaimRulesEngineClient : IClaimRulesEngineClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ClaimRulesEngineClient> _logger;

        public ClaimRulesEngineClient(HttpClient httpClient, IConfiguration configuration, ILogger<ClaimRulesEngineClient> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IReadOnlyList<ClaimRulesEngineViolation>> EvaluateClaimAsync(
            ClaimRulesEngineRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return Array.Empty<ClaimRulesEngineViolation>();
            }

            try
            {
                var endpoint = _configuration["RulesEngine:ValidationEndpoint"] ?? "api/validation/claim";

                if (_httpClient.BaseAddress == null && !Uri.IsWellFormedUriString(endpoint, UriKind.Absolute))
                {
                    _logger.LogWarning("Rules engine base URL is not configured.");
                    return Array.Empty<ClaimRulesEngineViolation>();
                }

                using var response = await _httpClient.PostAsJsonAsync(endpoint, request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Rules engine returned non-success status {StatusCode}", response.StatusCode);
                    return Array.Empty<ClaimRulesEngineViolation>();
                }

                var result = await response.Content.ReadFromJsonAsync<List<ClaimRulesEngineViolation>>(cancellationToken: cancellationToken);
                return result ?? Array.Empty<ClaimRulesEngineViolation>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to call rules engine");
                return Array.Empty<ClaimRulesEngineViolation>();
            }
        }
    }
}
