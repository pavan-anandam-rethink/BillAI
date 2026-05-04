using AutoMapper;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Billing.FolderStructure.Core.Services;
using BillingService.Domain;
using BillingService.Domain.Interfaces.Provider;
using BillingService.Domain.Services.Payment;
using BillingService.Domain.Utils;
using BillingService.Web.Helpers.HttpClients;
using EdiFabric;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver.Core.Configuration;
using Quartz.Util;
using Rethink.Services.Common.Factories;
using Rethink.Services.Common.Infrastructure.Configuration;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Domain.Interfaces;
using Rethink.Services.Domain.Services;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace BillingService.Web.IoC
{
    public class IoCContainer
    {
        private static IDbContextConfigurator<BillingDbContext> _billingConfigurator = new DbContextConfigurator<BillingDbContext>();
        private static IDbContextConfigurator<ReportingDbContext> _reportingConfigurator = new DbContextConfigurator<ReportingDbContext>();

        public static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration, IKeyVaultProviderService secretProvider)
        {

            var billingDbConnectionString = GetDBConnectionString(configuration, "Database", secretProvider);
            var reportingDbConnectionString = GetDBConnectionString(configuration, "ReportingDB", secretProvider);

            _billingConfigurator.Configure(services, billingDbConnectionString, false, false);
            _reportingConfigurator.Configure(services, reportingDbConnectionString, false, false);
        }


        public static void RegisterDBContext(IServiceCollection services)
        {
            new DbContextConfigurator<BillingDbContext>().Register(services);
            new DbContextConfigurator<ReportingDbContext>().Register(services);
            services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        }

        public static void RegisterRedisCache(IServiceCollection services, IConfiguration configuration, IKeyVaultProviderService keyVaultProviderService)
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var redisConnectionString = keyVaultProviderService.GetSecretAsync(configuration["RedisCache:ConnectionString"]).Result; ;
                return ConnectionMultiplexer.Connect(redisConnectionString);
            });
        }

        public static void RegisterHttpClients(IServiceCollection services, IConfiguration configuration, IKeyVaultProviderService keyVaultProviderService)
        {
            string accountsKey, curriculumsKey, demographicsKey, healthPlansKey, healthInsuranceKey, medicalRecordsKey, practiceOperationsKey, appointmentAPIKey, appointmentApplicationKey;
            FetchClientServiceKeys(configuration, keyVaultProviderService, out accountsKey, out curriculumsKey, out demographicsKey, out healthPlansKey, out healthInsuranceKey, out medicalRecordsKey, out practiceOperationsKey, out appointmentAPIKey, out appointmentApplicationKey);

            services.AddHttpClient<IBaseHttpClient, BaseHttpClient>().SetHandlerLifetime(TimeSpan.FromMinutes(5));
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

        public static async Task RegisterServices(IServiceCollection services, IConfiguration configuration, IKeyVaultProviderService secretProvider)
        {

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MapperProfile());
            });

            IMapper mapper = mapperConfig.CreateMapper();

            services.AddSingleton(mapper);

            await ConfigureBlobStorageAsync(services, configuration, secretProvider);

            var svcBusConnStrBuilder = ServiceBusConfig.ConfigureServiceBus(services, configuration, secretProvider);
            var mgmtClient = new ManagementClient(svcBusConnStrBuilder);

            //for razor engine in services
            services.AddRazorPages();

            ServicesConfigurator.Register(services, configuration);
            Authentication.ServicesConfigurator.Register(services, configuration);

        }

        private static async Task ConfigureBlobStorageAsync(IServiceCollection services, IConfiguration configuration, IKeyVaultProviderService keyVaultProviderService)
        {
            var ediFabricKey = await keyVaultProviderService.GetSecretAsync(configuration["EdiFabric:SerialKey"]);
            SerialKey.Set(ediFabricKey);

            var blobConnectionString = keyVaultProviderService.GetSecretAsync(configuration["ConnectionStrings:BlobStorage:ConnectionString"]).Result;

            var factory = new BlobConnectionFactory(blobConnectionString);
            services.AddScoped<IBlobConnectionFactory>(x => factory);
            services.AddSingleton(x => new BlobServiceClient(blobConnectionString));
            services.AddScoped<IBillingBlobService, BillingBlobService>();
            services.AddScoped<IBlobProcessingService, BlobProcessingService>();

        }
        public static string GetDBConnectionString(IConfiguration configuration, string DbName, IKeyVaultProviderService secretProvider)
        {
            var DbSectionName = $"ConnectionStrings:{DbName}";
            var connectionStringSection = configuration.GetSection(DbSectionName);
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
                var userIdSecret = secretProvider.GetSecretAsync(configuration["ConnectionStrings:Database:UserId"]).Result;
                var passwordSecret = secretProvider.GetSecretAsync(configuration["ConnectionStrings:Database:Password"]).Result;
                connStringBuilder.UserID = userIdSecret;
                connStringBuilder.Password = passwordSecret;
            }

            return connStringBuilder.ConnectionString;
        }

    }
}