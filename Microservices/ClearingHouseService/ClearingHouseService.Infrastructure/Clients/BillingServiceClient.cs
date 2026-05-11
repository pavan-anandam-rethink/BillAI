using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ClearingHouseService.Infrastructure.Clients
{
    /// <summary>
    /// HTTP client for communicating with the BillingService API.
    /// Extracted from CommonHelper to provide clean separation of concerns.
    /// </summary>
    public class BillingServiceClient : IBillingServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BillingServiceClient> _logger;

        public BillingServiceClient(HttpClient httpClient, ILogger<BillingServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<(bool Success, string Result)> GenerateEdiDataAsync(object claimModel, CancellationToken cancellationToken = default)
        {
            return await PostWithRetryAsync("/ClearingHouse/GenerateEDIData", claimModel, cancellationToken);
        }

        public async Task<(bool Success, string Result)> Generate270EdiDataAsync(object eligibilityRequest, CancellationToken cancellationToken = default)
        {
            return await PostWithRetryAsync("/ClearingHouse/Generate270EDIData", eligibilityRequest, cancellationToken);
        }

        public async Task<(bool Success, string Result)> UploadFileToBlobStorageAsync(object fileModel, CancellationToken cancellationToken = default)
        {
            return await PostAsync("/ClearingHouse/UploadFileToBlobStorage", fileModel, cancellationToken);
        }

        public async Task<(bool Success, string Result)> UploadSftpFilesToBlobStorageAsync(object fileData, CancellationToken cancellationToken = default)
        {
            return await PostAsync("/ClearingHouse/UploadEDIResponseFile", fileData, cancellationToken);
        }

        public async Task<bool> ReapplyPrAdjustmentAfterSecondaryBillingAsync(int claimId, CancellationToken cancellationToken = default)
        {
            try
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(claimId),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    "/ServiceLineAdjustment/ReapplyPRAdjustmentAfterSecondaryBilling",
                    content,
                    cancellationToken);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reapplying PR adjustment for claim {ClaimId}", claimId);
                return false;
            }
        }

        private async Task<(bool Success, string Result)> PostAsync(string endpoint, object payload, CancellationToken cancellationToken)
        {
            try
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
                var responseData = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return (true, responseData);
                }

                _logger.LogWarning("BillingService call to {Endpoint} failed with status {Status}: {Response}",
                    endpoint, response.StatusCode, responseData);
                return (false, responseData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling BillingService endpoint {Endpoint}", endpoint);
                return (false, ex.Message);
            }
        }

        private async Task<(bool Success, string Result)> PostWithRetryAsync(string endpoint, object payload, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var content = new StringContent(
                        JsonSerializer.Serialize(payload),
                        Encoding.UTF8,
                        "application/json");

                    var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
                    var responseData = await response.Content.ReadAsStringAsync(cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = Regex.Replace(
                            JsonSerializer.Deserialize<string>(responseData) ?? string.Empty,
                            "[\"""]",
                            string.Empty);
                        return (true, result);
                    }

                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        var result = Regex.Replace(
                            JsonSerializer.Deserialize<string>(responseData) ?? string.Empty,
                            "[\"""]",
                            string.Empty);
                        return (false, result);
                    }

                    // Retry on server errors
                    _logger.LogWarning("BillingService call to {Endpoint} returned {Status}, retrying in 30s",
                        endpoint, response.StatusCode);
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calling BillingService endpoint {Endpoint}, retrying in 30s", endpoint);
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            return (false, "Operation was cancelled");
        }
    }
}
