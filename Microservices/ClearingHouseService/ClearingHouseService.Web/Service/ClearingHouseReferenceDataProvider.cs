using ClearingHouseService.Web.Interface;
using Rethink.Services.Domain.Interfaces;

namespace ClearingHouseService.Web.Service
{
    public sealed class ClearingHouseReferenceDataProvider : IClearingHouseReferenceDataProvider
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ClearingHouseReferenceDataProvider>? _logger;
        private readonly IKeyVaultProviderService _keyVaultProviderService;

        private readonly string _baseUrl;
        private readonly string _payersUrl;
        private readonly string _enrollmenUrl;
        private readonly string _apiKey;

        public ClearingHouseReferenceDataProvider(IConfiguration configuration, IKeyVaultProviderService keyVaultProviderService)
        {
            _configuration = configuration;
            _keyVaultProviderService = keyVaultProviderService;

            // Initialize fields that depend on async calls
            _baseUrl = Convert.ToString(_keyVaultProviderService.GetSecretAsync(_configuration["Clearinghouses:Stedi:BaseUrl"]).Result)!;
            _apiKey = Convert.ToString(_keyVaultProviderService.GetSecretAsync(_configuration["Clearinghouses:Stedi:ApiKey"]).Result)!;
            _payersUrl = Convert.ToString(_keyVaultProviderService.GetSecretAsync(_configuration["Clearinghouses:Stedi:GetPayersUrl"]).Result)!;
            _enrollmenUrl = Convert.ToString(_keyVaultProviderService.GetSecretAsync(_configuration["Clearinghouses:Stedi:GetEnrollmenUrl"]).Result)!;
        }

        public async Task<string> GetPayersAsync(CancellationToken ct)
        {
            (HttpClient client, HttpResponseMessage response) = await clearingHouseHttpClient(_payersUrl,ct);
            return response.Content.ReadAsStringAsync().Result;
        }        

        public async Task<string> GetEnrollmentsAsync(CancellationToken ct)
        {
            var _enrollmenUrl = string.Format("{0}/_enrollmenUrl/{1}", _baseUrl, ct);
            (HttpClient client, HttpResponseMessage response) = await clearingHouseHttpClient(_enrollmenUrl, ct);
            return await response.Content.ReadAsStringAsync(ct);
        }

        private async Task<(HttpClient client, HttpResponseMessage response)> clearingHouseHttpClient(string clearingUrl, CancellationToken ct)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", _apiKey);
            var response = await client.GetAsync(_baseUrl + clearingUrl, ct);
            return (client, response);
        }

    }

}
