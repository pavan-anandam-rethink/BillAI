using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Models.EligibilityRequest;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ProcessEligibilityRequest.Func
{
    public class ProcessEligibilityRequest
    {
        private readonly ILogger<ProcessEligibilityRequest> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _ApiUrl;
        private readonly string _XApiKey;

        public ProcessEligibilityRequest(ILogger<ProcessEligibilityRequest> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _ApiUrl = _configuration["ApiUrl"].ToString();
            _XApiKey = _configuration["XApiKey"].ToString();
        }

        [Function("ProcessEligibilityRequest")]
        public async Task Run([ServiceBusTrigger("%TopicName%", "billing-eligibility-subscription",Connection = "ServiceBusConnectionString")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            _logger.LogInformation($"ProcessEligibilityRequest--> Message ID: {message.MessageId}");
            _logger.LogInformation($"ProcessEligibilityRequest--> Message Body: {message.Body}");
            _logger.LogInformation($"ProcessEligibilityRequest--> Message Content-Type: {message.ContentType}");

            var eligilibilityModel = null as Eligibility270Request;

            try
            {
                eligilibilityModel = JsonSerializer.Deserialize<Eligibility270Request>(message.Body.ToString());
                _logger.LogInformation("Raw ServiceBus Body: {Body}", message.Body.ToString());
                HttpClient httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("XApiKey", _XApiKey);

                if (eligilibilityModel == null ||
                    string.IsNullOrEmpty(eligilibilityModel.SubscriberId) ||
                    eligilibilityModel.ClientFunderId <= 0 ||
                    eligilibilityModel.AccountInfoId <= 0 ||
                    string.IsNullOrWhiteSpace(eligilibilityModel.ChildProfileReferringProviderId))
                {
                    _logger.LogError("ProcessEligibilityRequest--> Validation failed for message {MessageId}", message.MessageId);
                    await messageActions.DeadLetterMessageAsync(message);
                    return;
                }

                using (var content = new StringContent(JsonSerializer.Serialize(eligilibilityModel), System.Text.Encoding.UTF8, "application/json"))
                {
                    HttpResponseMessage result = await httpClient.PostAsync(_ApiUrl, content);
                   
                    if (!result.IsSuccessStatusCode)
                    {
                        _logger.LogError("ProcessEligibilityRequest--> Failed to process message {MessageId}. Status Code: {StatusCode}", message.MessageId, result.StatusCode);
                        throw new HttpRequestException($"ProcessEligibilityRequest--> Failed to process 270 Edi response. Status Code: {result.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ProcessEligibilityRequest--> Error processing eligibility request: {ex.Message}");
                throw;
            }
        }
    }
}
