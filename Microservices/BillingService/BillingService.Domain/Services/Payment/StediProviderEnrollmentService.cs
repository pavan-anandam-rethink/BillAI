using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Payment
{
    public class StediProviderEnrollmentService : IStediProviderEnrollmentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StediProviderEnrollmentService> _logger;
        private readonly IKeyVaultProviderService _keyVaultProviderService;
        string enrollmentURI = string.Empty;
        string apiKey = string.Empty;

        public StediProviderEnrollmentService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<StediProviderEnrollmentService> logger,
            IKeyVaultProviderService keyVaultProviderService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _keyVaultProviderService = keyVaultProviderService;

            enrollmentURI = _configuration["Clearinghouses:Stedi:EnrollmenUrl"];
            apiKey = _configuration["Clearinghouses:Stedi:ApiKey"];
        }

        public async Task<bool> VerifyProviderEnrollmentAsync(string providerNpi)
        {
            try
            {
                _logger.LogInformation("Verifying provider enrollment for NPI: {Npi}", providerNpi);

                return await CheckProviderEnrollmentasync(providerNpi);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying provider enrollment");

                return false;
            }


        }

        private async Task<bool> CheckProviderEnrollmentasync(string npiNumber)
        {
            var client = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Get, $"{enrollmentURI}?status=LIVE");

            request.Headers.Add("Authorization", apiKey);

            var response = await client.SendAsync(request);

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<EnrollmentResponse>(
             responseContent,
             new JsonSerializerOptions
             {
                 PropertyNameCaseInsensitive = true
             });


            return result?.Items.Any(x => x.Provider.Npi == npiNumber) ?? false;

        }
    }
}