using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using System.Net.Http;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Threading.Tasks;

namespace Rethink.Services.Domain.Configuration
{
    /// <summary>
    /// Registers typed HttpClients for Rethink BH microservices (shared by Billing, Login, and other hosts).
    /// </summary>
    public static class RethinkMicroserviceHttpClientsRegistration
    {
        public static async Task RegisterAsync(IServiceCollection services, IConfiguration configuration, IKeyVaultProviderService keyVaultProviderService, bool includeAppointmentClient = true)
        {
            var keys = await FetchClientServiceKeysAsync(configuration, keyVaultProviderService, includeAppointmentClient).ConfigureAwait(false);
            var timeout = RethinkMicroserviceHttpClientOptions.GetRequestTimeout(configuration);
            var useResilience = RethinkMicroserviceHttpClientOptions.UseResilience(configuration);

            RegisterClient(services, "accountsClient", client =>
            {
                client.BaseAddress = new Uri(configuration["AccountsApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), keys.AccountsKey);
            }, timeout, useResilience);

            RegisterClient(services, "curriculumClient", client =>
            {
                client.BaseAddress = new Uri(configuration["CurriculumApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), keys.CurriculumsKey);
            }, timeout, useResilience);

            RegisterClient(services, "demographicsClient", client =>
            {
                client.BaseAddress = new Uri(configuration["DemographicsApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), keys.DemographicsKey);
            }, timeout, useResilience);

            RegisterClient(services, "healthPlansClient", client =>
            {
                client.BaseAddress = new Uri(configuration["HealthPlansApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), keys.HealthPlansKey);
            }, timeout, useResilience);

            RegisterClient(services, "healthInsuranceClient", client =>
            {
                client.BaseAddress = new Uri(configuration["HealthInsuranceApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), keys.HealthInsuranceKey);
            }, timeout, useResilience);

            RegisterClient(services, "medicalRecordsClient", client =>
            {
                client.BaseAddress = new Uri(configuration["MedicalRecordsApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), keys.MedicalRecordsKey);
            }, timeout, useResilience);

            RegisterClient(services, "praticeOperationsClient", client =>
            {
                client.BaseAddress = new Uri(configuration["PracticeOperationsApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), keys.PracticeOperationsKey);
            }, timeout, useResilience);

            if (includeAppointmentClient)
            {
                RegisterClient(services, "appointmentClient", client =>
                {
                    client.BaseAddress = new Uri(configuration["AppointmentApiUrl"].ToString());
                    client.DefaultRequestHeaders.Add(configuration["ApiKey"].ToString(), keys.AppointmentApiKey);
                    client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), keys.AppointmentApplicationKey);
                }, timeout, useResilience);
            }
        }

        /// <summary>Synchronous wrapper for callers that cannot await during DI registration.</summary>
        public static void Register(IServiceCollection services, IConfiguration configuration, IKeyVaultProviderService keyVaultProviderService, bool includeAppointmentClient = true)
        {
            RegisterAsync(services, configuration, keyVaultProviderService, includeAppointmentClient).GetAwaiter().GetResult();
        }

        private static void RegisterClient(IServiceCollection services, string name, Action<HttpClient> configureClient, TimeSpan timeout, bool useResilience)
        {
            var builder = services.AddHttpClient(name, configureClient);
            builder.ConfigureHttpClient(c => c.Timeout = timeout);
            if (useResilience)
            {
                builder.AddStandardResilienceHandler();
            }
        }

        private static async Task<(
            string AccountsKey,
            string CurriculumsKey,
            string DemographicsKey,
            string HealthPlansKey,
            string HealthInsuranceKey,
            string MedicalRecordsKey,
            string PracticeOperationsKey,
            string AppointmentApiKey,
            string AppointmentApplicationKey)> FetchClientServiceKeysAsync(IConfiguration configuration, IKeyVaultProviderService keyVaultProviderService, bool includeAppointmentClient)
        {
            var accountsTask = keyVaultProviderService.GetSecretAsync(configuration["AccountsKey"]);
            var curriculumsTask = keyVaultProviderService.GetSecretAsync(configuration["CurriculumsKey"]);
            var demographicsTask = keyVaultProviderService.GetSecretAsync(configuration["DemographicsKey"]);
            var healthPlansTask = keyVaultProviderService.GetSecretAsync(configuration["HealthPlansKey"]);
            var healthInsuranceTask = keyVaultProviderService.GetSecretAsync(configuration["HealthInsuranceKey"]);
            var medicalRecordsTask = keyVaultProviderService.GetSecretAsync(configuration["MedicalRecordsKey"]);
            var practiceOpsTask = keyVaultProviderService.GetSecretAsync(configuration["PracticeOperationsKey"]);

            if (includeAppointmentClient)
            {
                var appointmentApiTask = keyVaultProviderService.GetSecretAsync(configuration["AppointmentAPIKey"]);
                var appointmentAppTask = keyVaultProviderService.GetSecretAsync(configuration["AppointmentApplicationKey"]);
                await Task.WhenAll(
                    accountsTask,
                    curriculumsTask,
                    demographicsTask,
                    healthPlansTask,
                    healthInsuranceTask,
                    medicalRecordsTask,
                    practiceOpsTask,
                    appointmentApiTask,
                    appointmentAppTask).ConfigureAwait(false);

                return (
                    await accountsTask.ConfigureAwait(false),
                    await curriculumsTask.ConfigureAwait(false),
                    await demographicsTask.ConfigureAwait(false),
                    await healthPlansTask.ConfigureAwait(false),
                    await healthInsuranceTask.ConfigureAwait(false),
                    await medicalRecordsTask.ConfigureAwait(false),
                    await practiceOpsTask.ConfigureAwait(false),
                    await appointmentApiTask.ConfigureAwait(false),
                    await appointmentAppTask.ConfigureAwait(false));
            }

            await Task.WhenAll(
                accountsTask,
                curriculumsTask,
                demographicsTask,
                healthPlansTask,
                healthInsuranceTask,
                medicalRecordsTask,
                practiceOpsTask).ConfigureAwait(false);

            return (
                await accountsTask.ConfigureAwait(false),
                await curriculumsTask.ConfigureAwait(false),
                await demographicsTask.ConfigureAwait(false),
                await healthPlansTask.ConfigureAwait(false),
                await healthInsuranceTask.ConfigureAwait(false),
                await medicalRecordsTask.ConfigureAwait(false),
                await practiceOpsTask.ConfigureAwait(false),
                string.Empty,
                string.Empty);
        }
    }
}
