using Authentication.Interfaces;
using Authentication.Services;
using Azure.Storage.Blobs;
using Billing.FolderStructure.Core.Services;
using Billing.FolderStructure.Core.Utils;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Files;
using BillingService.Domain.Services.Billing;
using BillingService.Domain.Services.Files;
using BillingService.Domain.Services.RethinkMasterDataMicroservices;
using ClearingHouseService.Web.BackgroundWorker;
using ClearingHouseService.Web.Factories;
using ClearingHouseService.Web.Helpers;
using ClearingHouseService.Web.infrastructure;
using ClearingHouseService.Web.Interface;
using ClearingHouseService.Web.Service;
using ClearingHouseService.Web.Service.Handler;
using EdiFabric;
using EraParserService.Domain.Services;
using EraParserService.Domain.Services.EdiExtensionParsers;
using EraParserService.Domain.Services.EdiParsers.Edi277;
using EraParserService.Domain.Services.EdiParsers.Edi999;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Rethink.Services.Common.Factories;
using Rethink.Services.Common.Helpers;
using Rethink.Services.Common.Infrastructure.Configuration;
using Rethink.Services.Common.Infrastructure.Connection;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Jobs;
using Rethink.Services.Common.Models.EligibilityRequest;
using Rethink.Services.Domain.Interfaces;
using Rethink.Services.Domain.Services;
using RethinkCore.Common.Logging.Extensions;
using System.Data.SqlClient;
using System.Net.Http.Headers;

namespace ClearingHouseService.Web
{
    public class ServicesConfiguration
    {
        private readonly IServiceCollection serviceCollection;
        private readonly IConfiguration configuration;
        private readonly ILoggingBuilder loggingBuilder;
        public IKeyVaultProviderService keyVaultProviderService { get; set; }

        public ServicesConfiguration(IServiceCollection serviceCollection, IConfiguration configuration, ILoggingBuilder loggingBuilder, IKeyVaultProviderService keyVaultProviderService)
        {
            this.serviceCollection = serviceCollection;
            this.configuration = configuration;
            this.loggingBuilder = loggingBuilder;
            this.keyVaultProviderService = keyVaultProviderService;
        }

        public void Configure()
        {

            ConfigureBlobStorage();

            ConfigureDatabase();

            ConfigureHttpClients();

            SerialKey.Set(keyVaultProviderService.GetSecretAsync(configuration["EdiFabric:SerialKey"]).Result);

            var svcBusConnStrBuilder = ConfigureServiceBus();

            //Domain services
            serviceCollection.AddScoped<IEdiProcessingService, EdiProcessingService>()
                              .AddSingleton(serviceCollection)
                              .AddLogging(builder => LogConfigHelper.ConfigureWorkerLogging(builder));

            serviceCollection.AddScoped<IEraValidationService, EraValidationService>()
                              .AddSingleton(serviceCollection)
                              .AddLogging(builder => LogConfigHelper.ConfigureWorkerLogging(builder));

            serviceCollection.AddScoped<IRethinkMasterDataMicroServices, RethinkMasterDataMicroServices>()
                              .AddSingleton(serviceCollection)
                              .AddLogging(builder => LogConfigHelper.ConfigureWorkerLogging(builder));

            serviceCollection.AddScoped<IClaimHistoryService, ClaimHistoryService>()
                              .AddSingleton(serviceCollection)
                              .AddLogging(builder => LogConfigHelper.ConfigureWorkerLogging(builder));

            serviceCollection.AddScoped<IPaymentService, PaymentService>()
                              .AddLogging(builder => LogConfigHelper.ConfigureWorkerLogging(builder));

            serviceCollection.AddScoped<IClaimAckParser, ClaimAckParser>()
                              .AddLogging(builder => LogConfigHelper.ConfigureWorkerLogging(builder));

            serviceCollection.AddScoped<IEdi999Parser, Edi999Parser>()
                              .AddLogging(builder => LogConfigHelper.ConfigureWorkerLogging(builder));

            serviceCollection.AddScoped<IClaimsSummaryDataParser, ClaimsSummaryDataParser>()
                               .AddLogging(builder => LogConfigHelper.ConfigureWorkerLogging(builder));

            serviceCollection.AddScoped<IBaseClaimService, BaseClaimService>();

            serviceCollection.AddTransient<ICommon, CommonHelper>();
            serviceCollection.AddScoped<IClearingHouseProcessor, ClearingHouseProcessorService>();
            serviceCollection.AddScoped<IClearingHouseProcessorFor270Edi, ClearingHouseProcessorFor270Edi>();
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            serviceCollection.AddScoped<IEdiFilesDownload, EdiFilesDownload>();
            serviceCollection.AddTransient<IEdiUploadService, EdiUploadService>();
            serviceCollection.AddScoped<IFileService, FileService>();
            serviceCollection.AddScoped<IFileManagerService, BlobManagerService>();
            serviceCollection.AddScoped<IBillingBlobService, BillingBlobService>();
            serviceCollection.AddScoped<IBillingFilePath, BillingFilePath>();

            serviceCollection.AddTransient<IEdiDownloadService, EdiDownloadService>();
            serviceCollection.AddTransient<ITokenService, TokenService>();
            serviceCollection.AddScoped<IMessageBus, MessageBus>();
            serviceCollection.AddScoped<ICHService, CHService>();
            serviceCollection.AddSingleton<IBackgroundJobQueue, BackgroundJobQueue>();
            serviceCollection.AddScoped<StediEligibilityJobHandler>();
            serviceCollection.AddHostedService<StediEligibilityBackgroundWorker>();
            serviceCollection.AddScoped<IEligibility271Repository, Eligibility271Repository>();
            serviceCollection.AddScoped<IX12Parser<Eligibility271ParsedResponse>, X12EligibilityParser>();
            serviceCollection.AddScoped<IEligibility271ResponseService, Eligibility271ResponseService>();
            serviceCollection.AddScoped<IStediEligibilityProcessor, StediEligibilityProcessor>();
            serviceCollection.AddScoped<IClearingHouseUploaderFactory, ClearingHouseUploaderResolver>();
            serviceCollection.AddScoped<IClearingHouseUploader, SftpUploader>();
            serviceCollection.AddScoped<ICredentialResolver, CredentialResolver>();
            serviceCollection.AddScoped<IClaimSubmissionHandler, ClaimSubmissionHandler>();
            serviceCollection.AddScoped<IClaimRepository, ClaimRepository>();

            serviceCollection.AddRethinkLogging(configuration);
            loggingBuilder.AddAppInsightLogger(configuration);

            var mgmtClient = new ManagementClient(svcBusConnStrBuilder);
        }

        private void ConfigureHttpClients()
        {
            serviceCollection.AddHttpClient("accountsClient", client =>
            {
                var accountsKey = keyVaultProviderService.GetSecretAsync(configuration["AccountsKey"]).Result;
                client.BaseAddress = new Uri(configuration["AccountsApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), accountsKey.ToString());
            });
            serviceCollection.AddHttpClient("curriculumClient", client =>
            {
                var curriculumsKey = keyVaultProviderService.GetSecretAsync(configuration["CurriculumsKey"]).Result;
                client.BaseAddress = new Uri(configuration["CurriculumApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), curriculumsKey.ToString());
            });
            serviceCollection.AddHttpClient("demographicsClient", client =>
            {
                var demographicsKey = keyVaultProviderService.GetSecretAsync(configuration["DemographicsKey"]).Result;
                client.BaseAddress = new Uri(configuration["DemographicsApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), demographicsKey.ToString());
            });
            serviceCollection.AddHttpClient("healthPlansClient", client =>
            {
                var healthPlansKey = keyVaultProviderService.GetSecretAsync(configuration["HealthPlansKey"]).Result;
                client.BaseAddress = new Uri(configuration["HealthPlansApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), healthPlansKey.ToString());
            });
            serviceCollection.AddHttpClient("healthInsuranceClient", client =>
            {
                var healthInsuranceKey = keyVaultProviderService.GetSecretAsync(configuration["HealthInsuranceKey"]).Result;
                client.BaseAddress = new Uri(configuration["HealthInsuranceApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), healthInsuranceKey.ToString());
            });
            serviceCollection.AddHttpClient("medicalRecordsClient", client =>
            {
                var medicalRecordsKey = keyVaultProviderService.GetSecretAsync(configuration["MedicalRecordsKey"]).Result;
                client.BaseAddress = new Uri(configuration["MedicalRecordsApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), medicalRecordsKey.ToString());
            });
            serviceCollection.AddHttpClient("praticeOperationsClient", client =>
            {
                var practiceOperationsKey = keyVaultProviderService.GetSecretAsync(configuration["PracticeOperationsKey"]).Result;
                client.BaseAddress = new Uri(configuration["PracticeOperationsApiUrl"].ToString());
                client.DefaultRequestHeaders.Add(configuration["HeaderKey"].ToString(), practiceOperationsKey.ToString());
            });

            //stedi api registration
            serviceCollection.AddHttpClient<IStediEligibilityClient, StediEligibilityClient>(client =>
            {
                var baseUrl = keyVaultProviderService.GetSecretAsync(configuration["Clearinghouses:Stedi:BaseUrl"]).Result;
                var eligibilityUrl = keyVaultProviderService.GetSecretAsync(configuration["Clearinghouses:Stedi:EligibilityUrl"]).Result;
                var apiKey = keyVaultProviderService.GetSecretAsync(configuration["Clearinghouses:Stedi:ApiKey"]).Result;

                if (string.IsNullOrWhiteSpace(baseUrl))
                    throw new InvalidOperationException("STEDI BaseUrl is not configured");

                if (string.IsNullOrWhiteSpace(apiKey))
                    throw new InvalidOperationException("STEDI ApiKey is not configured");

                client.BaseAddress = new Uri(baseUrl + "" + eligibilityUrl);

                // STEDI requires raw API key, NOT Bearer
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(apiKey);

                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });
        }

        private void ConfigureBlobStorage()
        {
            var blobConnectionString = keyVaultProviderService.GetSecretAsync(configuration["ConnectionStrings:BlobStorage:ConnectionString"]).Result;

            var factory = new BlobConnectionFactory(blobConnectionString);
            serviceCollection.AddScoped<IBlobConnectionFactory>(x => factory);
            serviceCollection.AddSingleton(x => new BlobServiceClient(blobConnectionString));
            serviceCollection.AddScoped<IBillingBlobService, BillingBlobService>();
            serviceCollection.AddScoped<IBlobProcessingService, BlobProcessingService>();
            serviceCollection.AddScoped<IClearingHouseReferenceDataProvider, ClearingHouseReferenceDataProvider>();
        }

        private void ConfigureDatabase()
        {
            serviceCollection.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
            var billingConnectionString = GetDBConnectionString("BillingDB");

            serviceCollection.AddDbContextPool<BillingDbContext>(options =>
                options.UseSqlServer(billingConnectionString, o =>
                {
                    o.EnableRetryOnFailure();
                })
                );
            new DbContextConfigurator<BillingDbContext>().Configure(serviceCollection, billingConnectionString, false, true);
        }


        private ServiceBusConnectionStringBuilder ConfigureServiceBus()
        {
            var connStringBuilder = new ServiceBusConnectionStringBuilder(keyVaultProviderService.GetSecretAsync(configuration["ConnectionStrings:ServiceBus:ConnectionString"]).Result);
            var builder = new ServiceBusConnectionFactory(connStringBuilder);
            serviceCollection.AddScoped<IServiceBusConnectionFactory>(x => builder);
            return connStringBuilder;
        }

        private string GetDBConnectionString(string DbName)
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
            };

            if (!connStringBuilder.IntegratedSecurity)
            {
                connStringBuilder.UserID = keyVaultProviderService.GetSecretAsync(connectionStringSection["UserID"]).Result;
                connStringBuilder.Password = keyVaultProviderService.GetSecretAsync(connectionStringSection["Password"]).Result;
            }
            return connStringBuilder.ConnectionString;
        }
    }
}