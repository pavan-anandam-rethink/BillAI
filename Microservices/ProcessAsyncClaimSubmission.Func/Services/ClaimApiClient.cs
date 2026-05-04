using BillingService.Domain.Models.Claims;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Models.Claim;
using System.Text;
using System.Text.Json;

namespace ProcessAsyncClaimSubmission.Func.Services
{
    public class ClaimApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ClaimApiClient> _logger;

        public ClaimApiClient(HttpClient httpClient, ILogger<ClaimApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        // Claim Processing API
        public async Task<HttpResponseMessage> CallClaimProcessingApi(ClaimsSubmitModel claim)
        {
            // Log the claim submission attempt
            _logger.LogInformation("ProcessAsyncClaimSubmission--> Submitting claim with AccountInfoId: {AccountInfoId}, MemberId: {MemberId}, IsSecondary: {IsSecondary}, AdjustmentLevel: {AdjustmentLevel}, Number of SecondaryFunderDetails: {SecondaryFunderDetailsCount}",
                claim.AccountInfoId,
                claim.MemberId,
                claim.IsSecondary,
                claim.AdjustmentLevel,
                claim.SecondaryFunderDetails?.Count ?? 0);

            var content = new StringContent(JsonSerializer.Serialize(claim), Encoding.UTF8, "application/json");
            return await _httpClient.PostAsync("/claim/SubmitClaims", content);
        }

        // Claim Approval API
        public async Task<HttpResponseMessage> CallClaimApprovalApi(IdsWithUserInfo model)
        {
            // Log the claim submission attempt
            _logger.LogInformation("ProcessAsyncClaimSubmission--> Approve claims with AccountInfoId: {AccountInfoId}, MemberId: {MemberId}, Number of Claims: {ClaimIds}",
                model.AccountInfoId,
                model.MemberId,
                model.Ids?.Length ?? 0);

            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            return await _httpClient.PostAsync("/claim/ApproveClaims", content);
        }

        // Pusher Notification API
        public async Task CallPusherNotificationApi(ClaimProcessRequestModel claim, HttpResponseMessage apiResponse)
        {

            var payload = new ClaimStatusUpdate
            {
                AccountId = claim.RequestModel.AccountInfoId,
                UserId = claim.RequestModel.MemberId,
                ClaimId = claim.RequestModel.Ids.FirstOrDefault(),
                BatchId = claim.BatchId,
                Total = claim.TotalClaims,
                Status = claim.ClaimStatus,
                Message = apiResponse.IsSuccessStatusCode ? "Success" : "Failure"
            };

            // Log the notification attempt
            _logger.LogInformation("ProcessAsyncClaimSubmission--> Notifying claim status for BatchId: {BatchId}, AccountId: {AccountId}, UserId: {UserId}, ClaimId: {ClaimId}, Status: {Status}, TotalClaims: {TotalClaims}",
                payload.BatchId,
                payload.AccountId,
                payload.UserId,
                payload.ClaimId,
                payload.Status,
                payload.Total);

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("/NotifyClaimStatus/Notify", content);
        }
    }
}
