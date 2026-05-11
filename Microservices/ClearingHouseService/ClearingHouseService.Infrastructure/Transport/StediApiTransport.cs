using ClearingHouseService.Domain.Entities;
using ClearingHouseService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ClearingHouseService.Infrastructure.Transport
{
    /// <summary>
    /// API-based transport implementation for Stedi clearing house.
    /// Handles direct API communication for eligibility requests (270/271).
    /// Note: SFTP operations for Stedi claim submission still use SftpTransport.
    /// </summary>
    public class StediApiTransport : IClearingHouseTransport
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<StediApiTransport> _logger;

        public StediApiTransport(HttpClient httpClient, ILogger<StediApiTransport> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<TransmissionResult> SendAsync(ClearingHouseConfig config, string fileName, Stream data, CancellationToken cancellationToken = default)
        {
            try
            {
                using var reader = new StreamReader(data);
                var ediContent = await reader.ReadToEndAsync(cancellationToken);

                _logger.LogInformation("Submitting EDI data via Stedi API for file {FileName}", fileName);

                var payload = new { x12 = ediContent };
                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("raw-x12", content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully submitted {FileName} via Stedi API", fileName);
                    return TransmissionResult.Success(fileName);
                }

                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Stedi API call failed. Status={Status}, Response={Response}",
                    response.StatusCode, errorBody);

                return TransmissionResult.Fail(TransmissionErrorType.UploadFailed,
                    $"Stedi API returned {response.StatusCode}: {errorBody}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error communicating with Stedi API");
                return TransmissionResult.Fail(TransmissionErrorType.ConnectionFailure, ex.Message);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Stedi API request timed out");
                return TransmissionResult.Fail(TransmissionErrorType.Timeout, "API request timed out");
            }
        }

        public Task<List<(MemoryStream Data, string FileName)>> ReceiveAsync(ClearingHouseConfig config, CancellationToken cancellationToken = default)
        {
            // Stedi API responses are received synchronously in the Send call.
            // This method is not applicable for API-based transport.
            _logger.LogWarning("ReceiveAsync is not supported for Stedi API transport. Responses are returned inline with SendAsync.");
            return Task.FromResult(new List<(MemoryStream Data, string FileName)>());
        }

        public async Task<TransmissionResult> ValidateConnectionAsync(ClearingHouseConfig config, CancellationToken cancellationToken = default)
        {
            try
            {
                // Simple connectivity check using a lightweight request
                var response = await _httpClient.GetAsync(string.Empty, cancellationToken);
                // Even a 4xx response means the API is reachable
                _logger.LogInformation("Stedi API connection validated successfully");
                return TransmissionResult.Success(string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stedi API connection validation failed");
                return TransmissionResult.Fail(TransmissionErrorType.ConnectionFailure, ex.Message);
            }
        }

        public Task<bool> DeleteAsync(ClearingHouseConfig config, string fileName, CancellationToken cancellationToken = default)
        {
            // Delete is not applicable for API-based transport
            _logger.LogWarning("DeleteAsync is not supported for Stedi API transport");
            return Task.FromResult(true);
        }
    }
}
