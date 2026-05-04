using AutoMapper;
using Azure.Messaging.ServiceBus;
using BillingService.Domain.Models.PaymentClaims;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Domain.Utils;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rethink.Services.Common.Handlers;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace ProcessEraPaymentResponse.Func
{
    public class ProcessEraPaymentResponse
    {
        private readonly ILogger<ProcessEraPaymentResponse> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _ApiUrl;
        private readonly string _XApiKey;
        private readonly string _billingApiUrl;
        private readonly string _billingXApiKey;

        public ProcessEraPaymentResponse(
            ILogger<ProcessEraPaymentResponse> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;

            // ERA API
            _ApiUrl = _configuration["ApiUrl"];
            _XApiKey = _configuration["XApiKey"];

            // Billing API
            _billingApiUrl = _configuration["BillingApiUrl"];
            _billingXApiKey = _configuration["BillingXApiKey"];
        }

        [Function(nameof(ProcessEraPaymentResponse))]
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
                    _logger.LogError("ProcessEraPaymentResponse failed with status {StatusCode} for message {MessageId}", result.StatusCode, message.MessageId);
                    throw new ServiceBusException(result.StatusCode.ToString(), ServiceBusFailureReason.ServiceCommunicationProblem);
                }
            }
        }
        [Function("ProcessUnallocatedPaymentUpdate")]
        public async Task RunUnallocatedPaymentUpdate(
        [ServiceBusTrigger(
            "billing-payment-topic",
            "billing-payment-subscription",
            Connection = "BillingServiceBusConnectionString")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
        {
            _logger.LogInformation("ProcessUnallocatedPaymentUpdate --> Message ID: {id}", message.MessageId);

            try
            {
                var payload = message.Body.ToString();

                // Deserialize Service Bus message
                var billingMessage =
                    JsonConvert.DeserializeObject<BillingPaymentMessage>(payload);

                if (billingMessage == null)
                {
                    _logger.LogError("Invalid message payload.");
                    await messageActions.DeadLetterMessageAsync(message);
                    return;
                }

                var payment = new ManualCreatePaymentModelRequest
                {
                    FunderType = "Patient",
                    PaymentMethod = "RevSpring",
                    PaymentAmount = billingMessage.PaidAmount,
                    ReferenceNumber = billingMessage.ReferenceNo,
                    AccountInfoId = billingMessage.AccountId,
                    MemberId = billingMessage.MemberId,
                    DepositDate = DateTime.Now.Date,
                    PostDate = DateTime.Now.Date
                };

                // First call returns int PaymentId
                int? paymentId = await PostToBillingApiAsync<ManualCreatePaymentModelRequest, int>(
                     payment,
                    _billingApiUrl + "PaymentPosting/ManualCreatePayment"
                );

                if (paymentId == null || paymentId == 0)
                {
                    _logger.LogError("Create payment billing API failed or returned invalid PaymentId.");
                    await messageActions.DeadLetterMessageAsync(message);
                    return;
                }

                var patientClaims = new CreatePatientClaimsModel
                {
                    PaymentId = paymentId.Value,
                    PatientIds = new[] { billingMessage.PatientId },
                    UnAllocatedAmount = new[] { billingMessage.PaidAmount },
                    Notes = new[] { "" },
                    AccountInfoId = billingMessage.AccountId,
                    MemberId = billingMessage.MemberId,
                };

                var patientPaymentResult = await PostToBillingApiAsync<CreatePatientClaimsModel, object>(
                    patientClaims,
                    _billingApiUrl + "ClaimPosting/CreatePaymentPatientClaims"
                );

                if (patientPaymentResult == null)
                {
                    await messageActions.DeadLetterMessageAsync(message);
                    return;
                }

                _logger.LogInformation("Billing processing success.");
                await messageActions.CompleteMessageAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Billing processing failed.");
                await messageActions.DeadLetterMessageAsync(message);
            }
        }

        private async Task<TResponse?> PostToBillingApiAsync<TRequest, TResponse>(TRequest requestObject, string endpointUrl)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("XApiKey", _billingXApiKey);

            var jsonContent = JsonConvert.SerializeObject(requestObject);
            using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(endpointUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("POST to {Endpoint} failed with status {StatusCode}", endpointUrl, response.StatusCode);
                return default;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(responseContent))
                return default;

            // Handle primitive types like int, string, bool
            if (typeof(TResponse).IsPrimitive || typeof(TResponse) == typeof(string))
            {
                try
                {
                    return (TResponse)Convert.ChangeType(responseContent, typeof(TResponse));
                }
                catch
                {
                    _logger.LogError("Failed to convert API response to {TypeName}", typeof(TResponse).Name);
                    return default;
                }
            }

            // For JSON objects, deserialize normally
            try
            {
                return JsonConvert.DeserializeObject<TResponse>(responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize API response to {TypeName}", typeof(TResponse).Name);
                return default;
            }
        }



    }
}
