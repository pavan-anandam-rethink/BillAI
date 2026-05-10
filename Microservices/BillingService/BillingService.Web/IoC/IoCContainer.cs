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
using Rethink.Services.Domain.Configuration;
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

            _billingConfigurator.Configure(services, billingDbConnectionString, true, false);
            _reportingConfigurator.Configure(services, reportingDbConnectionString, true, false);
        }


        public static void RegisterDBContext(IServiceCollection services)
        {
            new DbContextConfigurator<BillingDbContext>().Register(services);
            new DbContextConfigurator<ReportingDbContext>().Register(services);
            services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        }

        public static async Task RegisterRedisCacheAsync(IServiceCollection services, IConfiguration configuration, IKeyVaultProviderService keyVaultProviderService)
        {
            var redisConnectionString = await keyVaultProviderService.GetSecretAsync(configuration["RedisCache:ConnectionString"]).ConfigureAwait(false);
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
        }

        public static async Task RegisterHttpClientsAsync(IServiceCollection services, IConfiguration configuration, IKeyVaultProviderService keyVaultProviderService)
        {
            services.AddHttpClient<IBaseHttpClient, BaseHttpClient>().SetHandlerLifetime(TimeSpan.FromMinutes(5));
            await RethinkMicroserviceHttpClientsRegistration.RegisterAsync(services, configuration, keyVaultProviderService).ConfigureAwait(false);
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

            var blobConnectionString = await keyVaultProviderService.GetSecretAsync(configuration["ConnectionStrings:BlobStorage:ConnectionString"]).ConfigureAwait(false);

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