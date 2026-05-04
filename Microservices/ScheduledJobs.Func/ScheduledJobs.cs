using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Models.ReportingModels;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;

namespace ScheduledJobs.Func
{
    [ExcludeFromCodeCoverage]
    public class ScheduledJobs
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        // SFTP Configuration
        private readonly string _sftpApiUrl;
        private readonly string _sftpXApiKey;

        // Report Configuration
        private readonly string _reportApiUrl;
        private readonly string _reportXApiKey;

        public ScheduledJobs(ILoggerFactory loggerFactory, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _logger = loggerFactory.CreateLogger<ScheduledJobs>();
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;

            // SFTP Configuration
            _sftpApiUrl = _configuration["SftpApiUrl"]?.ToString() ?? throw new ArgumentNullException("SftpApiUrl configuration is missing.");
            _sftpXApiKey = _configuration["SftpXApiKey"]?.ToString() ?? throw new ArgumentNullException("SftpXApiKey configuration is missing.");

            // Report Configuration
            _reportApiUrl = _configuration["ReportApiUrl"]?.ToString() ?? throw new ArgumentNullException("ReportApiUrl configuration is missing.");
            _reportXApiKey = _configuration["ReportXApiKey"]?.ToString() ?? throw new ArgumentNullException("ReportXApiKey configuration is missing.");
        }

        #region SFTP Response Download Functions

        [Function("AvailitySftpResponseDownload")]
        public async Task AvailitySftpResponseDownloadRun([TimerTrigger("%AvailityTimerSchedule%")] TimerInfo timer)
        {
            _logger.LogInformation("Availity SftpResponseDownload started at: {Time}", DateTime.UtcNow);

            int clearingHouseId = Convert.ToInt16(_configuration["AvailityClearingHouseId"]);

            try
            {
                _logger.LogInformation("Processing Availity ClearingHouseId: {ClearingHouseId} at: {Time}", clearingHouseId, DateTime.UtcNow);
                await ProcessClearingHouseAsync(clearingHouseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Availity ClearingHouseId: {Id}", clearingHouseId);
            }

            _logger.LogInformation("Availity SftpResponseDownload completed at: {Time}", DateTime.UtcNow);
        }

        [Function("StediSftpResponseDownload")]
        public async Task StediSftpResponseDownloadRun([TimerTrigger("%StediTimerSchedule%")] TimerInfo timer)
        {
            _logger.LogInformation("Stedi SftpResponseDownload started at: {Time}", DateTime.UtcNow);

            int clearingHouseId = Convert.ToInt16(_configuration["StediClearingHouseId"]);

            try
            {
                _logger.LogInformation("Processing Stedi ClearingHouseId: {ClearingHouseId} at: {Time}", clearingHouseId, DateTime.UtcNow);
                await ProcessClearingHouseAsync(clearingHouseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stedi ClearingHouseId: {Id}", clearingHouseId);
            }

            _logger.LogInformation("Stedi SftpResponseDownload completed at: {Time}", DateTime.UtcNow);
        }

        private async Task ProcessClearingHouseAsync(int clearingHouseId)
        {
            _logger.LogInformation("Starting SFTP processing for ClearingHouseId: {Id}", clearingHouseId);

            var httpClient = _httpClientFactory.CreateClient("SftpClient");

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("XApiKey", _sftpXApiKey);
            httpClient.Timeout = TimeSpan.FromMinutes(30);

            var content = new StringContent(JsonConvert.SerializeObject(clearingHouseId), Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(_sftpApiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully processed ClearingHouseId: {Id}", clearingHouseId);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed for ClearingHouseId: {Id}. Status: {Status}. Response: {Response}",
                    clearingHouseId, response.StatusCode, error);
            }
        }

        #endregion

        #region Scheduled Report Functions

        [Function("WeeklyScheduledReport")]
        public async Task WeeklyReportRun([TimerTrigger("%WeeklyTimerSchedule%")] TimerInfo timer)
        {
            _logger.LogInformation("Weekly Report started at: {ExecutionTime}", DateTime.UtcNow);

            if (timer.ScheduleStatus?.Next != null)
            {
                _logger.LogInformation("Next weekly report schedule at: {NextSchedule}", timer.ScheduleStatus.Next);
            }

            try
            {
                await ProcessReportAsync(ReportFrequency.Weekly);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Weekly Report");
            }

            _logger.LogInformation("Weekly Report completed at: {ExecutionTime}", DateTime.UtcNow);
        }

        [Function("MonthlyScheduledReport")]
        public async Task MonthlyReportRun([TimerTrigger("%MonthlyTimerSchedule%")] TimerInfo timer)
        {
            _logger.LogInformation("Monthly Report started at: {ExecutionTime}", DateTime.UtcNow);

            if (timer.ScheduleStatus?.Next != null)
            {
                _logger.LogInformation("Next monthly report schedule at: {NextSchedule}", timer.ScheduleStatus.Next);
            }

            try
            {
                await ProcessReportAsync(ReportFrequency.Monthly);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Monthly Report");
            }

            _logger.LogInformation("Monthly Report completed at: {ExecutionTime}", DateTime.UtcNow);
        }

        private async Task ProcessReportAsync(ReportFrequency reportFrequency)
        {
            var reportTypeName = reportFrequency.ToString();
            _logger.LogInformation("Starting {ReportType} Report processing", reportTypeName);

            var httpClient = _httpClientFactory.CreateClient("ReportClient");

            httpClient.BaseAddress = new Uri(_reportApiUrl);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("XApiKey", _reportXApiKey);

            var reportModel = new ReportQueryModel { ReportFrequency = reportFrequency };
            var content = new StringContent(JsonConvert.SerializeObject(reportModel), Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("ClaimPosting/SendReport", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("{ReportType} Report generated successfully.", reportTypeName);
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("{ReportType} Report failed with status {StatusCode} and message {ResponseMessage}",
                    reportTypeName, response.StatusCode, responseContent);
            }
        }

        #endregion
    }
}
