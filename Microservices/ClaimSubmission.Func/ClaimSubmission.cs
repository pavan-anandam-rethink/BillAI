using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rethink.Services.Common.Models.Claim;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace ClaimSubmission.Func
{
    public class ClaimSubmission
    {
        private readonly ILogger<ClaimSubmission> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _ApiUrl;
        private readonly string _XApiKey;
        public ClaimSubmission(ILogger<ClaimSubmission> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _ApiUrl = _configuration["ApiUrl"].ToString();
            _XApiKey = _configuration["XApiKey"].ToString();

        }

        [Function(nameof(ClaimSubmission))]
        public async Task Run(
            [ServiceBusTrigger("%QueueName%", Connection = "ServiceBusConnectionString")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            _logger.LogInformation("Message ID: {id}", message.MessageId);
            _logger.LogInformation("Message Body: {body}", message.Body);
            _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

            var str = message.Body.ToString();
            ClearingHouseClaimModel claimDetails = JsonConvert.DeserializeObject<ClearingHouseClaimModel>(str.ToString());
            HttpClient httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("XApiKey", _XApiKey);

            using (var content = new StringContent(JsonConvert.SerializeObject(claimDetails), Encoding.UTF8, "application/json"))
            {
                try
                {
                    HttpResponseMessage result = httpClient.PostAsync(_ApiUrl, content).Result;
                    {
                        if (result.StatusCode == HttpStatusCode.OK)
                        {
                            await messageActions.CompleteMessageAsync(message);
                        }
                        else
                        {
                            throw new ServiceBusException(result.StatusCode.ToString(), ServiceBusFailureReason.ServiceCommunicationProblem);
                        }

                    }
                }
                catch (ServiceBusException ex)
                {
                    throw new Exception("Not Found" + ex.Message.ToString());
                }
            }
        }
    }
}
