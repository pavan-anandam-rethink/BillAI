using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rethink.Services.Common.Interfaces;
using System;

namespace Rethink.Services.Domain.Configuration
{
    /// <summary>
    /// Registers typed HttpClients for Rethink BH microservices (shared by Billing, Login, and other hosts).
    /// </summary>
    public static class RethinkMicroserviceHttpClientsRegistration
    {
        public static void Register(IServiceCollection services, IConfiguration configuration, IKeyVaultProviderService keyVaultProviderService)
        {
            string accountsKey, curriculumsKey, demographicsKey, healthPlansKey, healthInsuranceKey, medicalRecordsKey, practiceOperationsKey, appointmentAPIKey, appointmentApplicationKey;
            FetchClientServiceKeys(configuration, keyVaultProviderService, out accountsKey, out curriculumsKey, out demographicsKey, out healthPlansKey, out healthInsuranceKey, out medicalRecordsKey, out practiceOperationsKey, out appointmentAPIKey, out appointmentApplicationKey);

            services.AddHttpClient("accountsClient", client =>
            {
                client.BaseAddress = new Uri(configuration["AccountsApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), accountsKey);
            });
            services.AddHttpClient("curriculumClient", client =>
            {
                client.BaseAddress = new Uri(configuration["CurriculumApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), curriculumsKey);
            });
            services.AddHttpClient("demographicsClient", client =>
            {
                client.BaseAddress = new Uri(configuration["DemographicsApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), demographicsKey);
            });
            services.AddHttpClient("healthPlansClient", client =>
            {
                client.BaseAddress = new Uri(configuration["HealthPlansApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), healthPlansKey);
            });
            services.AddHttpClient("healthInsuranceClient", client =>
            {
                client.BaseAddress = new Uri(configuration["HealthInsuranceApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), healthInsuranceKey);
            });
            services.AddHttpClient("medicalRecordsClient", client =>
            {
                client.BaseAddress = new Uri(configuration["MedicalRecordsApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), medicalRecordsKey);
            });
            services.AddHttpClient("praticeOperationsClient", client =>
            {
                client.BaseAddress = new Uri(configuration["PracticeOperationsApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), practiceOperationsKey);
            });

            services.AddHttpClient("appointmentClient", client =>
            {
                client.BaseAddress = new Uri(configuration["AppointmentApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["ApiKey"].ToString(), appointmentAPIKey);
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), appointmentApplicationKey);
            });
        }

        private static void FetchClientServiceKeys(IConfiguration configuration, IKeyVaultProviderService keyVaultProviderService, out string accountsKey, out string curriculumsKey, out string demographicsKey, out string healthPlansKey, out string healthInsuranceKey, out string medicalRecordsKey, out string practiceOperationsKey, out string appointmentAPIKey, out string appointmentApplicationKey)
        {
            accountsKey = keyVaultProviderService.GetSecretAsync(configuration["AccountsKey"]).Result;
            curriculumsKey = keyVaultProviderService.GetSecretAsync(configuration["CurriculumsKey"]).Result;
            demographicsKey = keyVaultProviderService.GetSecretAsync(configuration["DemographicsKey"]).Result;
            healthPlansKey = keyVaultProviderService.GetSecretAsync(configuration["HealthPlansKey"]).Result;
            healthInsuranceKey = keyVaultProviderService.GetSecretAsync(configuration["HealthInsuranceKey"]).Result;
            medicalRecordsKey = keyVaultProviderService.GetSecretAsync(configuration["MedicalRecordsKey"]).Result;
            practiceOperationsKey = keyVaultProviderService.GetSecretAsync(configuration["PracticeOperationsKey"]).Result;
            appointmentAPIKey = keyVaultProviderService.GetSecretAsync(configuration["AppointmentAPIKey"]).Result;
            appointmentApplicationKey = keyVaultProviderService.GetSecretAsync(configuration["AppointmentApplicationKey"]).Result;
        }
    }
}
