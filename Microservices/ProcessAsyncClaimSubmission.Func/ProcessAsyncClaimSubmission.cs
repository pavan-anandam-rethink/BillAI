using Azure.Messaging.ServiceBus;
using BillingService.Domain.Models.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProcessAsyncClaimSubmission.Func.Services;
using System.Text.Json;

namespace ProcessAsyncClaimSubmission.Func;

public class ProcessAsyncClaimSubmission
{
    private readonly ILogger<ProcessAsyncClaimSubmission> _logger;
    private readonly ClaimApiClient _apiClient;
    private readonly IConfiguration _configuration;
    private readonly int MaxRetryAttempts;
    private readonly int InitialDelayMilliseconds;

    public ProcessAsyncClaimSubmission(ILogger<ProcessAsyncClaimSubmission> logger, ClaimApiClient apiClient, IConfiguration configuration)
    {
        _logger = logger;
        _apiClient = apiClient;
        _configuration = configuration;
        MaxRetryAttempts = int.Parse(_configuration["MaxRetryAttempts"] ?? "3");
        InitialDelayMilliseconds = int.Parse(_configuration["InitialDelayMilliseconds"] ?? "2000");
    }

    [Function(nameof(ProcessAsyncClaimSubmission))]
    public async Task Run(
        [ServiceBusTrigger("%TopicName%", "claim-processing-subscription", Connection = "ServiceBusConnectionString")] ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        await ProcessMessage(message, messageActions);
    }

    [Function("ProcessAsyncClaimApproval")]
    public async Task RunAsyncClaimApproval(
        [ServiceBusTrigger("%ClaimApprovalTopic%", "claim-approval-subscription", Connection = "ServiceBusConnectionString")] ServiceBusReceivedMessage approvalMessage,
        ServiceBusMessageActions messageActions)
    {
        await ProcessClaimApproval(approvalMessage, messageActions);
    }

    private async Task ProcessClaimApproval(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
    {
        var claim = null as ClaimApproveRequestModel;
        var model = new ClaimProcessRequestModel();
        try
        {
            _logger.LogInformation($"ProcessAsyncClaimApproval--> Message ID: {message.MessageId}");
            _logger.LogWarning($"ProcessAsyncClaimApproval--> Message Body: {message.Body.ToString()}"); // Log the message body for debugging

            claim = JsonSerializer.Deserialize<ClaimApproveRequestModel>(message.Body.ToString());
            if (claim == null || claim.RequestModel == null)
            {
                _logger.LogError("Failed to deserialize claim. Message body was null or in an unexpected format.");
                return;  
            }

            // Prepare ClaimProcessRequestModel for Pusher notification
            model = new ClaimProcessRequestModel
            {
                BatchId = claim.BatchId,
                RequestModel = new BillingService.Domain.Models.Claims.ClaimsSubmitModel
                {
                    AccountInfoId = claim.RequestModel.AccountInfoId,
                    MemberId = claim.RequestModel.MemberId,
                    Ids = claim.RequestModel.Ids
                },
                TotalClaims = claim.TotalClaims
            };

            // Call ClaimApproval API with retry
            var apiResponse = await CallWithRetry(() => _apiClient.CallClaimApprovalApi(claim.RequestModel), MaxRetryAttempts, "ProcessAsyncClaimApproval");

            // Log ClaimApproval API response
            _logger.LogInformation($"ProcessAsyncClaimApproval--> ApproveClaim API response for claim {claim.RequestModel.Ids?.FirstOrDefault()}: {apiResponse.StatusCode}");

            // Notify frontend via Pusher API
            model.ClaimStatus = apiResponse.IsSuccessStatusCode ? "Approved" : "Rejected";
            await _apiClient.CallPusherNotificationApi(model, apiResponse);

            // Log Pusher API response
            _logger.LogInformation($"ProcessAsyncClaimApproval--> Pusher API notification sent for claim {claim.RequestModel.Ids?.FirstOrDefault()}");

            // Complete message
            await messageActions.CompleteMessageAsync(message);

            _logger.LogInformation($"ProcessAsyncClaimApproval-- > Approved claim {claim} successfully.");
        }
        catch (Exception ex)
        {
            // In case of failure, log and notify via Pusher API
            if (claim != null)
            {
                model.ClaimStatus = "Rejected";
                await _apiClient.CallPusherNotificationApi(model, new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.BadRequest });
            }

            _logger.LogError(ex, $"ProcessAsyncClaimApproval--> Error processing message {message.MessageId}, with Message : {message.Body.ToString()}");
            // Move to DeadLetter Queue
            await messageActions.DeadLetterMessageAsync(message);
        }
    }

    private async Task ProcessMessage(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
    {
        var claim = null as ClaimProcessRequestModel;
        try
        {
            _logger.LogInformation($"ProcessAsyncClaimSubmission--> Message ID: {message.MessageId}");
            _logger.LogWarning($"ProcessAsyncClaimSubmission--> Message Body: {message.Body.ToString()}"); // Log the message body for debugging

            claim = JsonSerializer.Deserialize<ClaimProcessRequestModel>(message.Body.ToString());

            if (claim == null || claim.RequestModel == null)
            {
                _logger.LogError("ProcessAsyncClaimSubmission--> Failed to deserialize claim. Message body was null or in an unexpected format.");
                await messageActions.DeadLetterMessageAsync(message);
                return;
            }

            // Call Claim Processing API with retry
            var apiResponse = await CallWithRetry(() => _apiClient.CallClaimProcessingApi(claim.RequestModel), MaxRetryAttempts, nameof(ProcessAsyncClaimSubmission));

            // Log ClaimSubmission API response
            _logger.LogInformation($"ProcessAsyncClaimSubmission--> Claim Processing API response for claim {claim.RequestModel.Ids?.FirstOrDefault()}: {apiResponse.StatusCode}");

            // Notify frontend via Pusher API
            claim.ClaimStatus = apiResponse.IsSuccessStatusCode ? "Success" : "Failure";
            await _apiClient.CallPusherNotificationApi(claim, apiResponse);

            // Log Pusher API response
            _logger.LogInformation($"ProcessAsyncClaimSubmission--> Pusher API notification sent for claim {claim.RequestModel.Ids?.FirstOrDefault()}");

            // Complete message
            await messageActions.CompleteMessageAsync(message);

            _logger.LogInformation($"ProcessAsyncClaimSubmission--> Processed claim {claim} successfully.");
        }
        catch (Exception ex)
        {
            // In case of failure, log and notify via Pusher API
            if (claim != null)
            {
                claim.ClaimStatus = "Failure";
                await _apiClient.CallPusherNotificationApi(claim, new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.BadRequest });
            }

            _logger.LogError(ex, $"ProcessAsyncClaimSubmission--> Error processing message {message.MessageId}, with Message : {message.Body.ToString()}");
            // Move to DeadLetter Queue
            await messageActions.DeadLetterMessageAsync(message);
        }
    }

    // Retry with exponential backoff
    private async Task<HttpResponseMessage> CallWithRetry(Func<Task<HttpResponseMessage>> apiCall, int maxRetries, string functionName)
    {
        int retryCount = 0;
        int delay = InitialDelayMilliseconds;

        while (true)
        {
            try
            {
                var response = await apiCall();
                if (response.IsSuccessStatusCode)
                    return response;

                throw new HttpRequestException($"API returned {response.StatusCode}");
            }
            catch (Exception ex) when (retryCount < maxRetries)
            {
                retryCount++;
                _logger.LogWarning($"{functionName}-- > Retry {retryCount}/{maxRetries} due to {ex.Message}");
                await Task.Delay(delay);
                delay *= 2; // exponential backoff
            }
        }
    }
}