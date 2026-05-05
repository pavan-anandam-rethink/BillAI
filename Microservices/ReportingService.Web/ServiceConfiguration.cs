using Authentication.Interfaces;
using Authentication.Services;
using BillingService.Domain.Services.RethinkMasterDataMicroservices;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.EntityFrameworkCore;
using ReportingService.Web.Helpers.HttpClients;
using Rethink.Services.Common.Infrastructure.Configuration;
using Rethink.Services.Common.Infrastructure.Connection;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Utils;
using Rethink.Services.Domain.Interfaces;
using SummationService.Domain.Interfaces;
using SummationService.Domain.Services;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;

namespace ReportingService.Web
{
    [ExcludeFromCodeCoverage]
    public class ServicesConfiguration
    {
        private readonly IServiceCollection _serviceCollection;
        private readonly IConfiguration _configuration;
        private readonly ILoggingBuilder _loggingBuilder;
        public IKeyVaultProviderService _keyVaultProviderService { get; set; }
        public ServicesConfiguration(IServiceCollection serviceCollection, IConfiguration configuration, ILoggingBuilder loggingBuilder, IKeyVaultProviderService keyVaultProviderService)
        {
            _serviceCollection = serviceCollection;
            _configuration = configuration;
            _loggingBuilder = loggingBuilder;
            _keyVaultProviderService = keyVaultProviderService;

        }

        public async Task Configure()
        {
            ConfigureDatabase();
            RegisterHttpClients(_serviceCollection, _configuration, _keyVaultProviderService);
            _serviceCollection.AddScoped(typeof(IDbHelper<>), typeof(DbHelper<>));
            //Domain services
            _serviceCollection.AddScoped<IAccountsReceivableService, AccountsReceivableService>();
            _serviceCollection.AddScoped<IPaymentAdjustmentService, PaymentAdjustmentService>();
            _serviceCollection.AddScoped<IRethinkMasterDataMicroServices, RethinkMasterDataMicroServices>();
            _serviceCollection.AddScoped<IHelperService, HelperService>();
            _serviceCollection.AddTransient<ITokenService, TokenService>();
            _serviceCollection.AddScoped<IMonthlyFinancialSummaryService, MonthlyFinancialSummaryService>();
            _serviceCollection.AddScoped<IFunderFinancialSummaryService, FunderFinancialSummaryService>();

        }

        public static void RegisterHttpClients(IServiceCollection services, IConfiguration configuration, IKeyVaultProviderService keyVaultProviderService)
        {
            string accountsKey, demographicsKey, healthInsuranceKey;
            FetchClientServiceKeys(configuration, keyVaultProviderService, out accountsKey, out demographicsKey, out healthInsuranceKey);

            services.AddHttpClient<IBaseHttpClient, BaseHttpClient>().SetHandlerLifetime(TimeSpan.FromMinutes(5));

            services.AddHttpClient("accountsClient", client =>
            {
                client.BaseAddress = new Uri(configuration["AccountsApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), accountsKey);
            });
            services.AddHttpClient("demographicsClient", client =>
            {
                client.BaseAddress = new Uri(configuration["DemographicsApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), demographicsKey);
            });
            services.AddHttpClient("healthInsuranceClient", client =>
            {
                client.BaseAddress = new Uri(configuration["HealthInsuranceApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), healthInsuranceKey);
            });
        }

        private static void FetchClientServiceKeys(IConfiguration configuration, IKeyVaultProviderService keyVaultProviderService, out string accountsKey, out string demographicsKey, out string healthInsuranceKey)
        {
            accountsKey = keyVaultProviderService.GetSecretAsync(configuration["AccountsKey"]).Result;
            demographicsKey = keyVaultProviderService.GetSecretAsync(configuration["DemographicsKey"]).Result;
            healthInsuranceKey = keyVaultProviderService.GetSecretAsync(configuration["HealthInsuranceKey"]).Result;
        }

        private void ConfigureDatabase()
        {
            _serviceCollection.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
            var billingConnectionString = GetDBConnectionString("BillingDB");
            var reportingConnectionString = GetDBConnectionString("ReportingDB");

            _serviceCollection.AddDbContextPool<BillingDbContext>(options =>
                options.UseSqlServer(billingConnectionString, o =>
                {
                    o.EnableRetryOnFailure();
                })
                );
            _serviceCollection.AddDbContextPool<ReportingDbContext>(options =>
             options.UseSqlServer(reportingConnectionString, o =>
             {
                 o.EnableRetryOnFailure();
             })
             );
            new DbContextConfigurator<BillingDbContext>().Configure(_serviceCollection, billingConnectionString, false, true);
            new DbContextConfigurator<ReportingDbContext>().Configure(_serviceCollection, reportingConnectionString, false, true);
        }


        private ServiceBusConnectionStringBuilder ConfigureServiceBus()
        {
            var connStringBuilder = new ServiceBusConnectionStringBuilder(_keyVaultProviderService.GetSecretAsync(_configuration["ConnectionStrings:ServiceBus:ConnectionString"]).Result);
            var builder = new ServiceBusConnectionFactory(connStringBuilder);
            _serviceCollection.AddScoped<IServiceBusConnectionFactory>(x => builder);
            return connStringBuilder;

        }

        private static async Task CreateQueueIfNotExists(ManagementClient mgmtClient, string queuePath)
        {
            var queues = await mgmtClient.GetQueuesAsync();

            if (queues.All(q => q.Path != queuePath))
            {
                await mgmtClient.CreateQueueAsync(queuePath);
            }

            var testInfo = await mgmtClient.GetQueueRuntimeInfoAsync(queuePath);
        }

        private string GetDBConnectionString(string DbName)
        {
            var DbSectionName = $"ConnectionStrings:{DbName}";
            var connectionStringSection = _configuration.GetSection(DbSectionName);
            var connStringBuilder = new SqlConnectionStringBuilder
            {
                DataSource = connectionStringSection["DataSource"],
                InitialCatalog = connectionStringSection["InitialCatalog"],
                IntegratedSecurity = bool.Parse(connectionStringSection["IntegratedSecurity"] ?? "false"),
                MultiSubnetFailover = bool.Parse(connectionStringSection["MultiSubnetFailover"] ?? "false"),
                Encrypt = bool.Parse(connectionStringSection["Encrypt"] ?? "false"),
                TrustServerCertificate = bool.Parse(connectionStringSection["TrustServerCertificate"] ?? "true"),
                ConnectTimeout = int.Parse(connectionStringSection["ConnectionTimeout"] ?? "0"),
            };

            if (!connStringBuilder.IntegratedSecurity)
            {
                connStringBuilder.UserID = _keyVaultProviderService.GetSecretAsync(connectionStringSection["UserID"]).Result;
                connStringBuilder.Password = _keyVaultProviderService.GetSecretAsync(connectionStringSection["Password"]).Result;
            }

            return connStringBuilder.ConnectionString;
        }
    }
}