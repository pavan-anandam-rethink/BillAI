using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using BillingService.Domain.Models.ClearingHouse;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing
{
    /// <summary>
    /// Service for validating clearinghouse SFTP credentials by calling the ClearingHouse API
    /// </summary>
    public class ClearinghouseCredentialValidationService : IClearinghouseCredentialValidationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ClearinghouseCredentialValidationService> _logger;
        private readonly string _clearingHouseBaseUrl;
        private readonly string _validationEndpoint;
        private readonly string _apiKey;
        private const string ApiKeyHeaderName = "XApiKey";

        public ClearinghouseCredentialValidationService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IKeyVaultProviderService keyVaultProviderService,
            ILogger<ClearinghouseCredentialValidationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            
            var baseUrlSecretKey = configuration["Clearinghouses:BaseUrl"] 
                ?? throw new ArgumentNullException(nameof(configuration), "Clearinghouses:BaseUrl configuration is missing");
            var validationEndpointSecretKey = configuration["Clearinghouses:ValidationEndpoint"]
                ?? throw new ArgumentNullException(nameof(configuration), "Clearinghouses:ValidationEndpoint configuration is missing");
            var apiKeySecretKey = configuration["Clearinghouses:ApiKey"]
                ?? throw new ArgumentNullException(nameof(configuration), "Clearinghouses:ApiKey configuration is missing");
            
            _clearingHouseBaseUrl = keyVaultProviderService.GetSecretAsync(baseUrlSecretKey).Result;
            _validationEndpoint = keyVaultProviderService.GetSecretAsync(validationEndpointSecretKey).Result;
            _apiKey = keyVaultProviderService.GetSecretAsync(apiKeySecretKey).Result;
        }

        /// <summary>
        /// Validates SFTP credentials for all active clearinghouses by calling the ClearingHouse API
        /// </summary>
        public async Task<ClearinghouseApiValidationResponse> ValidateAllClearinghousesAsync()
        {
            var result = new ClearinghouseApiValidationResponse();

            try
            {
                _logger.LogInformation("Starting clearinghouse credentials validation via ClearingHouse API _clearingHouseBaseUrl={_clearingHouseBaseUrl},APIKey={_apiKey},ValidationEndpoint={_validationEndpoint}",
                     _clearingHouseBaseUrl, _apiKey, _validationEndpoint);

                var client = _httpClientFactory.CreateClient("ClearingHouseService");
                
                // Set base address if not already configured
                if (client.BaseAddress == null)
                {
                    client.BaseAddress = new Uri(_clearingHouseBaseUrl.TrimEnd('/') + "/");
                }

                // Add API key header
                if (!client.DefaultRequestHeaders.Contains(ApiKeyHeaderName))
                {
                    client.DefaultRequestHeaders.Add(ApiKeyHeaderName, _apiKey);
                }

                var response = await client.GetAsync(_validationEndpoint);
              
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var validationResponse = JsonSerializer.Deserialize<ClearinghouseApiValidationResponse>(content, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    result.AllValid = validationResponse?.AllValid ?? false;
                    result.TotalClearinghouses = validationResponse?.TotalClearinghouses ?? 0;
                    result.SuccessfulValidations = validationResponse?.SuccessfulValidations ?? 0;
                    result.FailedValidations = validationResponse?.FailedValidations ?? 0;
                    result.clearinghouseCredentialValidationResults = validationResponse?.clearinghouseCredentialValidationResults ?? new List<ClearinghouseCredentialValidationResult>();

                    _logger.LogInformation(
                        "Clearinghouse validation completed. AllValid={AllValid}, Total={Total}, Success={Success}, Failed={Failed}",
                        result.AllValid, result.TotalClearinghouses, result.SuccessfulValidations, result.FailedValidations);
                }
                else
                {
                    result.AllValid = false;
                    result.ErrorMessage = $"ClearingHouse API returned status code: {response.StatusCode}";
                    _logger.LogWarning("Clearinghouse validation API call failed with status: {StatusCode}", response.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                result.AllValid = false;
                result.ErrorMessage = $"Failed to connect to ClearingHouse service: {ex.Message}";
                _logger.LogError(ex, "Failed to connect to ClearingHouse service for credential validation");
            }
            catch (Exception ex)
            {
                result.AllValid = false;
                result.ErrorMessage = $"Error during clearinghouse validation: {ex.Message}";
                _logger.LogError(ex, "Error during clearinghouse credential validation");
            }

            return result;
        }

        /// <summary>
        /// Internal class to deserialize the ClearingHouse API response
        /// </summary>
       
    }
}
