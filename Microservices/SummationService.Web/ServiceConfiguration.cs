using Authentication.Interfaces;
using Authentication.Services;
using BillingService.Domain.Services.RethinkMasterDataMicroservices;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Rethink.Services.Common.Infrastructure.Configuration;
using Rethink.Services.Common.Infrastructure.Connection;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Domain.Interfaces;
using SummationService.Domain.Interfaces;
using SummationService.Domain.Services;
using System.Configuration;
using System.Data.SqlClient;
namespace SummationService.Web
{
    public class ServicesConfiguration
    {
        private readonly IServiceCollection _serviceCollection;
        private readonly IConfiguration _configuration;
        private readonly ILoggingBuilder _loggingBuilder;
        public IKeyVaultProviderService _keyVaultProviderService { get; set; }
        public ServicesConfiguration(IServiceCollection serviceCollection, IConfiguration configuration, ILoggingBuilder loggingBuilder)
        {
            _serviceCollection = serviceCollection;
            _configuration = configuration;
            _loggingBuilder = loggingBuilder;
            _keyVaultProviderService = new KeyVaultProviderService(configuration);
        }

        public async Task Configure()
        {
            ConfigureDatabase();

            //Domain services
            _serviceCollection.AddScoped<IChargeTransactionService, ChargeTransactionService>();
            _serviceCollection.AddScoped<IClaimTransactionService, ClaimTransactionService>();
            _serviceCollection.AddScoped<IHelperService, HelperService>();
            _serviceCollection.AddTransient<ITokenService, TokenService>();
            _serviceCollection.AddSingleton<IKeyVaultProviderService, KeyVaultProviderService>();
            _serviceCollection.AddScoped<IRethinkMasterDataMicroServices, RethinkMasterDataMicroServices>();
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

            //_serviceCollection.AddDbContextPool<BhSimpleContext>(options =>
            //    options.UseSqlServer(billingConnectionString, o =>
            //        {
            //            o.EnableRetryOnFailure();
            //        })
            //    );            

            new DbContextConfigurator<BillingDbContext>().Configure(_serviceCollection, billingConnectionString, false, true);
            new DbContextConfigurator<ReportingDbContext>().Configure(_serviceCollection, reportingConnectionString, false, true);
        }


        private ServiceBusConnectionStringBuilder ConfigureServiceBus()
        {
            var connectionString = _configuration["ConnectionStrings:ServiceBus:ConnectionString"];

            var connStringBuilder = new ServiceBusConnectionStringBuilder(connectionString);

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
                var userIdSecret = _keyVaultProviderService.GetSecretAsync(_configuration[$"ConnectionStrings:{DbName}:UserId"]).Result;
                var passwordSecret = _keyVaultProviderService.GetSecretAsync(_configuration[$"ConnectionStrings:{DbName}:Password"]).Result;
                connStringBuilder.UserID = userIdSecret;
                connStringBuilder.Password = passwordSecret;
            }

            return connStringBuilder.ConnectionString;
        }
    }
}