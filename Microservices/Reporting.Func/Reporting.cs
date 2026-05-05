using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rethink.Services.Common.Models.Claim;
using System.Net.Http.Headers;
using System.Text;

namespace Reporting.Func
{
    public class Reporting
    {
        private readonly ILogger<Reporting> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _apiUrl;

        public Reporting(ILogger<Reporting> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _apiUrl = _configuration["ApiUrl"].ToString();
        }

        [Function(nameof(Reporting))]
        public async Task Run(
            [ServiceBusTrigger("%TopicName%",subscriptionName:"report_txn", Connection = "ServiceBusConnectionString")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_apiUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("XApiKey", _configuration["XApiKey"].ToString());

                ClaimTransactionModel reportRequest = JsonConvert.DeserializeObject<ClaimTransactionModel>(message.Body.ToString());
                StringContent content = new StringContent(JsonConvert.SerializeObject(reportRequest), Encoding.UTF8, "application/json");
                HttpResponseMessage accountReceivableResponse = new HttpResponseMessage();
                HttpResponseMessage paymentAdjustmentResponse = new HttpResponseMessage();
                accountReceivableResponse = await client.PostAsync("/AccountsReceivable/AddOrUpdateAccountsReceivable", content);
                paymentAdjustmentResponse = await client.PostAsync("/PaymentAdjustment/AddOrUpdatePaymentAdjustment", content);
                if (accountReceivableResponse.IsSuccessStatusCode && paymentAdjustmentResponse.IsSuccessStatusCode)
                {
                    await messageActions.CompleteMessageAsync(message);
                }
                else
                {
                    if (!accountReceivableResponse.IsSuccessStatusCode)
                    {
                        _logger.LogError($"AR failed with status {accountReceivableResponse.StatusCode} and message {accountReceivableResponse.Content} " +
                                         $"for Transaction Type: {reportRequest?.TransactionType}, Id: {reportRequest?.TransactionTypeId}");
                        throw new ServiceBusException(accountReceivableResponse.StatusCode.ToString(), ServiceBusFailureReason.ServiceCommunicationProblem);
                    }
                    else
                    {
                        _logger.LogError($"PayAdj failed with status {paymentAdjustmentResponse.StatusCode} and message {paymentAdjustmentResponse.Content} " +
                                         $"for Transaction Type: {reportRequest?.TransactionType}, Id: {reportRequest?.TransactionTypeId}");
                        throw new ServiceBusException(paymentAdjustmentResponse.StatusCode.ToString(), ServiceBusFailureReason.ServiceCommunicationProblem);
                    }
                }
            }
        }
    }
}
