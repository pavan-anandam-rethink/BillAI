using AutoMapper;
using Azure.Storage.Blobs;
using Billing.FolderStructure.Core.Services;
using BillingService.Domain;
using BillingService.Domain.Interfaces.Provider;
using BillingService.Domain.Services.Payment;
using BillingService.Domain.Utils;
using BillingService.Web.Helpers.HttpClients;
using BillingService.Web.Observability;
using EdiFabric;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rethink.Services.Common.Infrastructure.Configuration;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Domain.Interfaces;
using Rethink.Services.Domain.Services;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Web.IoC
{
    public static class IoCContainer
    {
        private static readonly IDbContextConfigurator<BillingDbContext> BillingConfigurator =
            new DbContextConfigurator<BillingDbContext>();
        private static readonly IDbContextConfigurator<ReportingDbContext> ReportingConfigurator =
            new DbContextConfigurator<ReportingDbContext>();

        public static void ConfigureDatabase(
            IServiceCollection services,
            string billingDbConnectionString,
            string reportingDbConnectionString)
        {
            BillingConfigurator.Configure(services, billingDbConnectionString, false, false);
            ReportingConfigurator.Configure(services, reportingDbConnectionString, false, false);
        }

        public static void RegisterDBContext(IServiceCollection services)
        {
            new DbContextConfigurator<BillingDbContext>().Register(services);
            new DbContextConfigurator<ReportingDbContext>().Register(services);
            services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        }

        public static void RegisterRedisCache(IServiceCollection services, string redisConnectionString)
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnectionString));
        }

        public static void RegisterHttpClients(
            IServiceCollection services,
            IConfiguration configuration,
            IReadOnlyDictionary<string, string> clientApiKeys)
        {
            string Header(string keyName) =>
                configuration[keyName] ??
                throw new InvalidOperationException($"Configuration '{keyName}' is required for HTTP clients.");

            IHttpClientBuilder CreateResilientClient(string name, Action<System.Net.Http.HttpClient> configureClient)
            {
                return services.AddHttpClient(name, configureClient)
                    .AddHttpMessageHandler<ResilienceDelegatingHandler>();
            }

            services.AddHttpClient<IBaseHttpClient, BaseHttpClient>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddHttpMessageHandler<ResilienceDelegatingHandler>();
            CreateResilientClient("accountsClient", client =>
            {
                client.BaseAddress = new Uri(configuration["AccountsApiUrl"]);
                client.DefaultRequestHeaders.Add(Header("HeaderKey"), clientApiKeys["AccountsKey"]);
            });
            CreateResilientClient("curriculumClient", client =>
            {
                client.BaseAddress = new Uri(configuration["CurriculumApiUrl"]);
                client.DefaultRequestHeaders.Add(Header("HeaderKey"), clientApiKeys["CurriculumsKey"]);
            });
            CreateResilientClient("demographicsClient", client =>
            {
                client.BaseAddress = new Uri(configuration["DemographicsApiUrl"]);
                client.DefaultRequestHeaders.Add(Header("HeaderKey"), clientApiKeys["DemographicsKey"]);
            });
            CreateResilientClient("healthPlansClient", client =>
            {
                client.BaseAddress = new Uri(configuration["HealthPlansApiUrl"]);
                client.DefaultRequestHeaders.Add(Header("HeaderKey"), clientApiKeys["HealthPlansKey"]);
            });
            CreateResilientClient("healthInsuranceClient", client =>
            {
                client.BaseAddress = new Uri(configuration["HealthInsuranceApiUrl"]);
                client.DefaultRequestHeaders.Add(Header("HeaderKey"), clientApiKeys["HealthInsuranceKey"]);
            });
            CreateResilientClient("medicalRecordsClient", client =>
            {
                client.BaseAddress = new Uri(configuration["MedicalRecordsApiUrl"]);
                client.DefaultRequestHeaders.Add(Header("HeaderKey"), clientApiKeys["MedicalRecordsKey"]);
            });
            CreateResilientClient("praticeOperationsClient", client =>
            {
                client.BaseAddress = new Uri(configuration["PracticeOperationsApiUrl"]);
                client.DefaultRequestHeaders.Add(Header("HeaderKey"), clientApiKeys["PracticeOperationsKey"]);
            });
            CreateResilientClient("appointmentClient", client =>
            {
                client.BaseAddress = new Uri(configuration["AppointmentApiUrl"]);
                client.DefaultRequestHeaders.Add(configuration["ApiKey"], clientApiKeys["AppointmentAPIKey"]);
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"], clientApiKeys["AppointmentApplicationKey"]);
            });
        }

        private static async Task<IReadOnlyDictionary<string, string>> FetchClientServiceKeysAsync(
            IConfiguration configuration,
            IKeyVaultProviderService keyVaultProviderService)
        {
            var defs = new (string DictKey, string ConfigKey)[]
            {
                ("AccountsKey", "AccountsKey"),
                ("CurriculumsKey", "CurriculumsKey"),
                ("DemographicsKey", "DemographicsKey"),
                ("HealthPlansKey", "HealthPlansKey"),
                ("HealthInsuranceKey", "HealthInsuranceKey"),
                ("MedicalRecordsKey", "MedicalRecordsKey"),
                ("PracticeOperationsKey", "PracticeOperationsKey"),
                ("AppointmentAPIKey", "AppointmentAPIKey"),
                ("AppointmentApplicationKey", "AppointmentApplicationKey"),
            };

            var pairs = await Task.WhenAll(defs.Select(async d =>
            {
                var v = await keyVaultProviderService.GetSecretAsync(configuration[d.ConfigKey]).ConfigureAwait(false);
                return (d.DictKey, Value: v ?? string.Empty);
            })).ConfigureAwait(false);

            return pairs.ToDictionary(p => p.DictKey, p => p.Value);
        }

        public static Task<IReadOnlyDictionary<string, string>> ResolveClientHttpKeysAsync(
            IConfiguration configuration,
            IKeyVaultProviderService keyVaultProviderService) =>
            FetchClientServiceKeysAsync(configuration, keyVaultProviderService);

        /// <param name="blobStorageConnectionString">Pre-resolved so blob storage secret is read once.</param>
        /// <param name="serviceBusConnectionString">Pre-resolved so Service Bus is not queried twice.</param>
        public static async Task RegisterServicesAsync(
            IServiceCollection services,
            IConfiguration configuration,
            IKeyVaultProviderService secretProvider,
            string blobStorageConnectionString,
            string serviceBusConnectionString)
        {
            var mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile(new MapperProfile()); });
            services.AddSingleton(mapperConfig.CreateMapper());

            await ConfigureBlobStorageAsync(services, configuration, secretProvider, blobStorageConnectionString)
                .ConfigureAwait(false);

            _ = ServiceBusConfig.ConfigureServiceBusWithConnectionString(services, serviceBusConnectionString);

            services.AddRazorPages();

            ServicesConfigurator.Register(services, configuration);
            Authentication.ServicesConfigurator.Register(services, configuration);
        }

        private static async Task ConfigureBlobStorageAsync(
            IServiceCollection services,
            IConfiguration configuration,
            IKeyVaultProviderService keyVaultProviderService,
            string blobStorageConnectionString)
        {
            var ediFabricKey = await keyVaultProviderService.GetSecretAsync(configuration["EdiFabric:SerialKey"])
                .ConfigureAwait(false);
            SerialKey.Set(ediFabricKey);

            var blobConnectionString = blobStorageConnectionString ?? string.Empty;
            var factory = new BlobConnectionFactory(blobConnectionString);
            services.AddScoped<IBlobConnectionFactory>(_ => factory);
            services.AddSingleton(_ => new BlobServiceClient(blobConnectionString));
            services.AddScoped<IBillingBlobService, BillingBlobService>();
            services.AddScoped<IBlobProcessingService, BlobProcessingService>();
        }

        public static async Task<string> GetDBConnectionStringAsync(
            IConfiguration configuration,
            string dbName,
            IKeyVaultProviderService secretProvider)
        {
            var dbSectionName = $"ConnectionStrings:{dbName}";
            var connectionStringSection = configuration.GetSection(dbSectionName);
            var connStringBuilder = new SqlConnectionStringBuilder
            {
                DataSource = connectionStringSection["DataSource"],
                InitialCatalog = connectionStringSection["InitialCatalog"],
                IntegratedSecurity = bool.Parse(connectionStringSection["IntegratedSecurity"] ?? "false"),
                MultiSubnetFailover = bool.Parse(connectionStringSection["MultiSubnetFailover"] ?? "false"),
                Encrypt = bool.Parse(connectionStringSection["Encrypt"] ?? "false"),
                TrustServerCertificate = bool.Parse(connectionStringSection["TrustServerCertificate"] ?? "true"),
                ConnectTimeout = int.Parse(connectionStringSection["ConnectionTimeout"] ?? "0"),
                CommandTimeout = int.Parse(connectionStringSection["CommandTimeout"] ?? "16000"),
            };

            if (!connStringBuilder.IntegratedSecurity)
            {
                var userIdKey = connectionStringSection["UserId"];
                var passwordKey = connectionStringSection["Password"];
                var userIdTask = secretProvider.GetSecretAsync(userIdKey);
                var passwordTask = secretProvider.GetSecretAsync(passwordKey);
                await Task.WhenAll(userIdTask, passwordTask).ConfigureAwait(false);
                connStringBuilder.UserID = await userIdTask.ConfigureAwait(false);
                connStringBuilder.Password = await passwordTask.ConfigureAwait(false);
            }

            return connStringBuilder.ConnectionString;
        }
    }
}
