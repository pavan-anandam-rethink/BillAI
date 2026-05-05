using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models.EligibilityRequest;
using System.Text;
using System.Text.Json;

namespace ClearingHouseService.Web.infrastructure
{
    public sealed class StediEligibilityClient : IStediEligibilityClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<StediEligibilityClient> _logger;

        public StediEligibilityClient(
        HttpClient httpClient,
        ILogger<StediEligibilityClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }


        /// <summary>
        /// Submit 270 edi file to STEDI and get back 271 response
        /// </summary>
        public async Task<Eligibility271ParsedResponse> Submit270Async(string edi270,CancellationToken ct)
        {
            var payload = new
            {
                x12 = edi270
            };
            
            string json = JsonSerializer.Serialize(payload);

            var content = new StringContent(json,Encoding.UTF8,"application/json");

            _logger.LogInformation("STEDI eligibility call. edi270={edi270}, BaseAddress={BaseAddress}", edi270, _httpClient.BaseAddress);
            
            var response = await _httpClient.PostAsync("raw-x12", content);

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("STEDI eligibility call failed. Status={Status}, Response={Response}",response.StatusCode,responseBody);
                return new Eligibility271ParsedResponse
                {
                    IsSuccess = false,
                    FailureResponse = responseBody,
                };
            }

            using var doc = JsonDocument.Parse(responseBody);

            return new Eligibility271ParsedResponse
            {
                IsSuccess = true,
                X12Response = doc.RootElement.GetProperty("x12").GetString(),
            };
        }
    }
}
