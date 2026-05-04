using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rethink.Services.Common.Models.Claim;
using System.Net.Http.Headers;
using System.Text;

namespace AutoClaimCreation.Func
{
    public class AutoClaimCreation
    {
        private readonly ILogger<AutoClaimCreation> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _apiUrl, XApiKey;
        private readonly IHttpClientFactory _httpClientFactory;

        public AutoClaimCreation(ILogger<AutoClaimCreation> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _apiUrl = _configuration["ApiUrl"].ToString();
            _httpClientFactory = httpClientFactory;
            XApiKey = _configuration["XApiKey"].ToString();
        }

        [Function(nameof(AutoClaimCreation))]
        public async Task Run(
            [ServiceBusTrigger("%QueueName%", Connection = "ServiceBusConnectionString")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            _logger.LogInformation("AutoClaimCreation initialized for Appointment with message {MessageId}", message.MessageId);
            var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_apiUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("XApiKey", XApiKey);

                AutoClaimRequestModel autoClaimRequest = JsonConvert.DeserializeObject<AutoClaimRequestModel>(message.Body.ToString());
                StringContent content = new StringContent(JsonConvert.SerializeObject(autoClaimRequest), Encoding.UTF8, "application/json");
                // HTTP POST
                HttpResponseMessage response = await client.PostAsync("Appointment/SyncClaim", content);
                if (response.IsSuccessStatusCode)
                {
                    await messageActions.CompleteMessageAsync(message);
                }
                else
                {
                    //string responseContent = await response.Content.ReadAsStringAsync();
                    //ExceptionResponse exceptionResponse = JsonConvert.DeserializeObject<ExceptionResponse>(responseContent);
                    _logger.LogInformation($"AutoClaimCreation failed with status {response.StatusCode} and message {response.Content} " +
                                     $"for Appointment: {autoClaimRequest?.appointmentId}, Account: {autoClaimRequest?.accountId}");
                    throw new ServiceBusException(response.StatusCode.ToString(), ServiceBusFailureReason.ServiceCommunicationProblem);
                }
        }


        [Function("AutoProcessUnBilledAppointment")]
        public async Task ProcessScheduleAppointments([TimerTrigger("%TimerSchedule%")] TimerInfo myTimer)
        {
            try
            {
                var currentTime = DateTime.UtcNow;
                var cronExpression = _configuration["TimerSchedule"];

                _logger.LogInformation("Timer trigger function executed at: {UtcNow}", DateTime.UtcNow);

                if (myTimer.ScheduleStatus != null)
                {
                    var httpClient = _httpClientFactory.CreateClient("AutoProcessUnBilledAppointmentClient");

                    httpClient.BaseAddress = new Uri(_apiUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Add("XApiKey", XApiKey);

                    httpClient.Timeout = TimeSpan.FromSeconds(90); ;
                    var result = await httpClient.PostAsync("Appointment/AutoProcessUnBilledAppointmentSchedule", null);
                    if (result.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("AutoProcessUnBilledAppointment run successfully.");
                    }
                    else
                    {
                        _logger.LogError($"AutoProcessUnBilledAppointment failed with status code {result.StatusCode}");
                    }

                    _logger.LogInformation("Next timer schedule at: {NextSchedule}", myTimer.ScheduleStatus.Next);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred: {ex.Message}");
                throw;
            }
        }
    }
}
