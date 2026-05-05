using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rethink.Services.Common.Models.Claim;
using System.Net.Http.Headers;
using System.Text;

namespace Summation.Func
{
    public class Summation
    {
        private readonly ILogger<Summation> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _apiUrl;

        public Summation(ILogger<Summation> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _apiUrl = _configuration["ApiUrl"].ToString();
        }

        [Function(nameof(Summation))]
        public async Task Run(
            [ServiceBusTrigger("%TopicName%",subscriptionName:"claim_txn", Connection = "ServiceBusConnectionString")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_apiUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("XApiKey", _configuration["XApiKey"].ToString());

                ClaimTransactionModel summationRequest = JsonConvert.DeserializeObject<ClaimTransactionModel>(message.Body.ToString());
                StringContent content = new StringContent(JsonConvert.SerializeObject(summationRequest), Encoding.UTF8, "application/json");
                HttpResponseMessage response = new HttpResponseMessage();
                response = await client.PostAsync("/ClaimTransaction/AddOrUpdateClaimTransaction", content);
                if (response.IsSuccessStatusCode)
                {
                    await messageActions.CompleteMessageAsync(message);
                }
                else
                {
                    _logger.LogError($"Summation failed with status {response.StatusCode} and message {response.Content} " +
                                     $"for Transaction Type: {summationRequest?.TransactionType}, Id: {summationRequest?.TransactionTypeId}");
                    throw new ServiceBusException(response.StatusCode.ToString(), ServiceBusFailureReason.ServiceCommunicationProblem);
                }
            }
        }
    }
}
