using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rethink.Services.Common.Handlers;
using System.Net.Http.Headers;
using System.Text;

namespace ProcessClaimCreation.Func
{
    public class ProcessClaimCreation
    {
        private readonly ILogger<ProcessClaimCreation> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _apiUrl;

        public ProcessClaimCreation(ILogger<ProcessClaimCreation> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _apiUrl = _configuration["ApiUrl"].ToString();
        }

        [Function(nameof(ProcessClaimCreation))]
        public async Task Run(
            [ServiceBusTrigger("%QueueName%", Connection = "ServiceBusConnectionString")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(10);
                client.BaseAddress = new Uri(_apiUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("XApiKey", _configuration["XApiKey"].ToString());

                ClaimCreateEnd claimCreateEnd = JsonConvert.DeserializeObject<ClaimCreateEnd>(message.Body.ToString());
                StringContent content = new StringContent(JsonConvert.SerializeObject(claimCreateEnd), Encoding.UTF8, "application/json");
                // HTTP POST
                HttpResponseMessage response = await client.PostAsync("Claim/ProcessClaimCreation", content);
                if (response.IsSuccessStatusCode)
                {
                    await messageActions.CompleteMessageAsync(message);
                }
                else
                {
                    _logger.LogError($"HTTP request failed with status code {response.StatusCode}");
                    _logger.LogInformation($"Request contents : {claimCreateEnd}");
                    throw new ServiceBusException(response.StatusCode.ToString(), ServiceBusFailureReason.ServiceCommunicationProblem);
                }
            }
        }
    }
}
