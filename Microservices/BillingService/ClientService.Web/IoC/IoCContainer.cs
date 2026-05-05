using AutoMapper;
using BillingService.Domain;
using BillingService.Domain.Utils;
using ClientService.Web.Helpers.HttpClients;
using Microsoft.Data.SqlClient;
using Rethink.Services.Common.Factories;
using Rethink.Services.Common.Infrastructure.Configuration;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Domain.Configuration;
using Rethink.Services.Domain.Interfaces;
using Rethink.Services.Domain.Services;
using System.Threading.Tasks;

namespace ClientService.Web.IoC
{
    public class IoCContainer
    {
        private static IDbContextConfigurator<BillingDbContext> _billingConfigurator = new DbContextConfigurator<BillingDbContext>();
        private static IDbContextConfigurator<ReportingDbContext> _reportingConfigurator = new DbContextConfigurator<ReportingDbContext>();

        public static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration, IKeyVaultProviderService secretProvider)
        {
            var billingDbConnectionString = GetDBConnectionString(configuration, "Database", secretProvider);
            var reportingConnectionString = GetDBConnectionString(configuration, "ReportingDB", secretProvider);


            //services.AddDbContextPool<ReportingDbContext>(options =>
            // options.UseSqlServer(reportingConnectionString, o =>
            // {
            //     o.EnableRetryOnFailure();
            // })
            // );

            _reportingConfigurator.Configure(services, reportingConnectionString, false, false);
            _billingConfigurator.Configure(services, billingDbConnectionString, false, false);
        }

        public static void RegisterDBContext(IServiceCollection services)
        {
            new DbContextConfigurator<BillingDbContext>().Register(services);
            new DbContextConfigurator<ReportingDbContext>().Register(services);
            services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        }

        public static async Task RegisterHttpClientsAsync(IServiceCollection services, IConfiguration configuration, IKeyVaultProviderService keyVaultProviderService)
        {
            var keys = await FetchClientServiceKeysAsync(configuration, keyVaultProviderService).ConfigureAwait(false);
            var timeout = RethinkMicroserviceHttpClientOptions.GetRequestTimeout(configuration);

            services.AddHttpClient<IBaseHttpClient, BaseHttpClient>().SetHandlerLifetime(TimeSpan.FromMinutes(5));
            services.AddHttpClient("accountsClient", client =>
            {
                client.BaseAddress = new Uri(configuration["AccountsApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), keys.AccountsKey);
                client.Timeout = timeout;
            });
            services.AddHttpClient("curriculumClient", client =>
            {
                client.BaseAddress = new Uri(configuration["CurriculumApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), keys.CurriculumsKey);
                client.Timeout = timeout;
            });
            services.AddHttpClient("demographicsClient", client =>
            {
                client.BaseAddress = new Uri(configuration["DemographicsApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), keys.DemographicsKey);
                client.Timeout = timeout;
            });
            services.AddHttpClient("healthPlansClient", client =>
            {
                client.BaseAddress = new Uri(configuration["HealthPlansApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), keys.HealthPlansKey);
                client.Timeout = timeout;
            });
            services.AddHttpClient("healthInsuranceClient", client =>
            {
                client.BaseAddress = new Uri(configuration["HealthInsuranceApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), keys.HealthInsuranceKey);
                client.Timeout = timeout;
            });
            services.AddHttpClient("medicalRecordsClient", client =>
            {
                client.BaseAddress = new Uri(configuration["MedicalRecordsApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), keys.MedicalRecordsKey);
                client.Timeout = timeout;
            });
            services.AddHttpClient("praticeOperationsClient", client =>
            {
                client.BaseAddress = new Uri(configuration["PracticeOperationsApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), keys.PracticeOperationsKey);
                client.Timeout = timeout;
            });
        }

        private static async Task<(
            string AccountsKey,
            string CurriculumsKey,
            string DemographicsKey,
            string HealthPlansKey,
            string HealthInsuranceKey,
            string MedicalRecordsKey,
            string PracticeOperationsKey)> FetchClientServiceKeysAsync(IConfiguration configuration, IKeyVaultProviderService keyVaultProviderService)
        {
            var accountsTask = keyVaultProviderService.GetSecretAsync(configuration["AccountsKey"]);
            var curriculumsTask = keyVaultProviderService.GetSecretAsync(configuration["CurriculumsKey"]);
            var demographicsTask = keyVaultProviderService.GetSecretAsync(configuration["DemographicsKey"]);
            var healthPlansTask = keyVaultProviderService.GetSecretAsync(configuration["HealthPlansKey"]);
            var healthInsuranceTask = keyVaultProviderService.GetSecretAsync(configuration["HealthInsuranceKey"]);
            var medicalRecordsTask = keyVaultProviderService.GetSecretAsync(configuration["MedicalRecordsKey"]);
            var practiceOpsTask = keyVaultProviderService.GetSecretAsync(configuration["PracticeOperationsKey"]);
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
                await practiceOpsTask.ConfigureAwait(false));
        }

        public static async Task RegisterServices(IServiceCollection services, IConfiguration configuration, IKeyVaultProviderService secretProvider)
        {
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MapperProfile());
            });

            IMapper mapper = mapperConfig.CreateMapper();

            services.AddSingleton(mapper);

            ConfigureBlobStorage(services, configuration, secretProvider);

            //var svcBusConnStrBuilder = ServiceBusConfig.ConfigureServiceBus(services, configuration);

            //for razor engine in services
            services.AddRazorPages();

            ServicesConfigurator.Register(services, configuration);
            Authentication.ServicesConfigurator.Register(services, configuration);
        }

        private static void ConfigureBlobStorage(IServiceCollection services, IConfiguration configuration, IKeyVaultProviderService keyVaultProviderService)
        {
            var connectionString = keyVaultProviderService.GetSecretAsync(configuration["ConnectionStrings:BlobStorage:ConnectionString"]).Result;
            
            var factory = new BlobConnectionFactory(connectionString);

            services.AddScoped<IBlobConnectionFactory>(x => factory);

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
                CommandTimeout = int.Parse(connectionStringSection["CommandTimeout"] ?? "16000"),  //Is this timeout need to be updated?
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