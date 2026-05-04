using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rethink.Services.Common.Handlers;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace ProcessClaimResponse.Func
{
    public class ProcessClaimResponse
    {
        private readonly ILogger<ProcessClaimResponse> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _ApiUrl;
        private readonly string _XApiKey;
        public ProcessClaimResponse(ILogger<ProcessClaimResponse> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _ApiUrl = _configuration["ApiUrl"].ToString();
            _XApiKey = _configuration["XApiKey"].ToString();
        }

        [Function(nameof(ProcessClaimResponse))]
        public async Task Run(
            [ServiceBusTrigger("%QueueName%", Connection = "ServiceBusConnectionString")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            _logger.LogInformation("Message ID: {id}", message.MessageId);
            _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

            var str = message.Body.ToString();
            EdiDownloadData claimDetails = JsonConvert.DeserializeObject<EdiDownloadData>(str.ToString());
            var httpClient = _httpClientFactory.CreateClient();

            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("XApiKey", _XApiKey);

            using (var content = new StringContent(JsonConvert.SerializeObject(claimDetails), Encoding.UTF8, "application/json"))
            {
                HttpResponseMessage result = await httpClient.PostAsync(_ApiUrl, content);
                if (result.StatusCode == HttpStatusCode.OK)
                {
                    await messageActions.CompleteMessageAsync(message);
                }
                else
                {
                    _logger.LogError("ProcessClaimResponse failed with status {StatusCode} for message {MessageId}", result.StatusCode, message.MessageId);
                    throw new ServiceBusException(result.StatusCode.ToString(), ServiceBusFailureReason.ServiceCommunicationProblem);
                }
            }
        }
    }
}
