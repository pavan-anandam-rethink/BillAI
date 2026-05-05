using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Client;
using BillingService.Domain.Interfaces.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Rethink.Services.Common.Entities.Billing;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Integration.Tests
{
    public class TestServerFixture : IAsyncLifetime, IDisposable
    {
        private readonly string _relativeTargetProjectParentDir;

        public TestServerFixture()
            : this(Path.Combine("BillingService"))
        {
        }

        protected TestServerFixture(string relativeTargetProjectParentDir)
        {
            _relativeTargetProjectParentDir = relativeTargetProjectParentDir;
        }

        #region "Mock Services"
        public Mock<IRepository<BillingDbContext, ClaimEntity>> ClaimRepository { get; private set; } = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
        public Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>> ClaimAppointmentLinkRepository { get; private set; } = new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>>();
        public Mock<IRepository<BillingDbContext, ClaimAppointmentLinkChargeEntry>> LinkChargeEntryRepository { get; private set; } = new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkChargeEntry>>();
        public Mock<IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity>> ClaimChargeEntryWriteOffRepository { get; private set; } = new Mock<IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity>>();
        public Mock<IRepository<BillingDbContext, ClaimValidationErrorEntity>> ClaimValidationErrorRepository { get; private set; } = new Mock<IRepository<BillingDbContext, ClaimValidationErrorEntity>>();
        public Mock<IRepository<BillingDbContext, ClaimAttachmentEntity>> ClaimAttachmentRepository { get; private set; } = new Mock<IRepository<BillingDbContext, ClaimAttachmentEntity>>();
        public Mock<IRepository<BillingDbContext, ClaimErrorCategoryEntity>> ClaimErrorCategoryRepository { get; private set; } = new Mock<IRepository<BillingDbContext, ClaimErrorCategoryEntity>>();
        public Mock<IRepository<BillingDbContext, ClaimErrorMessageEntity>> ClaimErrorMessageRepository { get; private set; } = new Mock<IRepository<BillingDbContext, ClaimErrorMessageEntity>>();
        public Mock<IRepository<BillingDbContext, ClaimHistoryEntity>> ClaimHistoryRepository { get; private set; } = new Mock<IRepository<BillingDbContext, ClaimHistoryEntity>>();
        public Mock<IRepository<BillingDbContext, MemberViewSettingEntity>> MemberViewSettingsRepository { get; private set; } = new Mock<IRepository<BillingDbContext, MemberViewSettingEntity>>();
        public Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>> ClaimChargeEntryRepository { get; private set; } = new Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>>();
        public Mock<IRepository<BillingDbContext, PaymentClaimEntity>> PaymentClaimRepository { get; private set; } = new Mock<IRepository<BillingDbContext, PaymentClaimEntity>>();
        public Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>> PaymentClaimServiceLineRepository { get; private set; } = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>>();
        public Mock<IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity>> PaymentClaimServiceLineAdjustmentRepository { get; private set; } = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity>>();
        public Mock<IRepository<BillingDbContext, ClaimSubmissionServiceLineEntity>> ClaimSubmissionServiceLineRepository { get; private set; } = new Mock<IRepository<BillingDbContext, ClaimSubmissionServiceLineEntity>>();
        public Mock<IRepository<BillingDbContext, PaymentEntity>> PaymentRepository { get; private set; } = new Mock<IRepository<BillingDbContext, PaymentEntity>>();
        public Mock<IRepository<BillingDbContext, PaymentNoteEntity>> PaymentNoteRepository { get; private set; } = new Mock<IRepository<BillingDbContext, PaymentNoteEntity>>();
        public Mock<IRepository<BillingDbContext, ClaimSubmissionEntity>> ClaimSubmissionRepository { get; private set; } = new Mock<IRepository<BillingDbContext, ClaimSubmissionEntity>>();
        public Mock<IRepository<BillingDbContext, ClaimDiagnosisCodeEntity>> ClaimDiagnosisCodeRepository { get; private set; } = new Mock<IRepository<BillingDbContext, ClaimDiagnosisCodeEntity>>();
        public Mock<IRepository<BillingDbContext, ClaimNoteEntity>> ClaimNoteRepository { get; private set; } = new Mock<IRepository<BillingDbContext, ClaimNoteEntity>>();
        public Mock<IRepository<BillingDbContext, ClaimHistoryActionEntity>> ClaimHistoryActionRepository { get; private set; } = new Mock<IRepository<BillingDbContext, ClaimHistoryActionEntity>>();
        public Mock<IRepository<BillingDbContext, ClaimVersionEntity>> ClaimVersionRepository { get; private set; } = new Mock<IRepository<BillingDbContext, ClaimVersionEntity>>();

        public Mock<IDbHelper<BillingDbContext>> BillingDbHelper { get; private set; } = new Mock<IDbHelper<BillingDbContext>>();

        public Mock<IClaimService> ClaimService { get; private set; } = new Mock<IClaimService>();
        public Mock<IClaimManagerService> ClaimManagerService { get; private set; } = new Mock<IClaimManagerService>();
        public Mock<IClaimValidationService> ClaimValidationService { get; private set; } = new Mock<IClaimValidationService>();
        public Mock<IRethinkMasterDataMicroServices> RethinkServices { get; private set; } = new Mock<IRethinkMasterDataMicroServices>();
        public Mock<IClientService> ClientService { get; private set; } = new Mock<IClientService>();
        public Mock<IProviderLocationService> ProviderLocationService { get; private set; } = new Mock<IProviderLocationService>();
        public Mock<IMemberAccountService> MemberAccountService { get; private set; } = new Mock<IMemberAccountService>();
        public Mock<ICommonService> CommonService { get; private set; } = new Mock<ICommonService>();

        public HttpClient Client { get; private set; }
        public string XApiKey { get; private set; }

        public TestServer Server { get; private set; }
        #endregion

        public Task InitializeAsync()
        {
            var startupAssembly = typeof(Web.Startup).GetTypeInfo().Assembly;
            var contentRoot = GetProjectPath(_relativeTargetProjectParentDir, startupAssembly);
            var config = new ConfigurationBuilder()
                    .SetBasePath(contentRoot)
                    .AddJsonFile("appsettings.json")
                    //.AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
                    .Build();

            var builder = new WebHostBuilder()
                .UseContentRoot(contentRoot)
                .ConfigureServices(InitializeBaseAndRepos)
                .ConfigureTestServices(InitializeTestServices)
                .UseEnvironment("Development")
                .UseConfiguration(config)
                .UseStartup(typeof(Web.Startup));

            Server = new TestServer(builder);
            Client = Server.CreateClient();
            Client.BaseAddress = new Uri($"http://localhost:{5000}");
            XApiKey = "B6E9430A-49E6-4100-AFC2-C37C50CFFB33";
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Server?.Dispose();
            Client?.Dispose();
        }

        protected virtual void InitializeBaseAndRepos(IServiceCollection services)
        {
            var startupAssembly = typeof(Web.Startup).GetTypeInfo().Assembly;

            // Inject a custom application part manager.
            // Overrides AddMvcCore() because it uses TryAdd().
            var manager = new ApplicationPartManager();
            manager.ApplicationParts.Add(new AssemblyPart(startupAssembly));
            manager.FeatureProviders.Add(new ControllerFeatureProvider());
            manager.FeatureProviders.Add(new ViewComponentFeatureProvider());

            services.AddSingleton(manager);

            // REPOSITORIES
            services.AddScoped(c => ClaimRepository.Object);
            services.AddScoped(c => ClaimAppointmentLinkRepository.Object);
            services.AddScoped(c => LinkChargeEntryRepository.Object);
            services.AddScoped(c => ClaimValidationErrorRepository.Object);
            services.AddScoped(c => ClaimAttachmentRepository.Object);
            services.AddScoped(c => ClaimErrorCategoryRepository.Object);
            services.AddScoped(c => ClaimErrorMessageRepository.Object);
            services.AddScoped(c => ClaimHistoryRepository.Object);
            services.AddScoped(c => MemberViewSettingsRepository.Object);
            services.AddScoped(c => ClaimChargeEntryRepository.Object);
            services.AddScoped(c => PaymentClaimRepository.Object);
            services.AddScoped(c => PaymentRepository.Object);
            services.AddScoped(c => PaymentNoteRepository.Object);
            services.AddScoped(c => ClaimSubmissionRepository.Object);
            services.AddScoped(c => ClaimDiagnosisCodeRepository.Object);
            services.AddScoped(c => ClaimNoteRepository.Object);
            services.AddScoped(c => ClaimHistoryActionRepository.Object);
            services.AddScoped(c => PaymentClaimServiceLineRepository.Object);
            services.AddScoped(c => ClaimVersionRepository.Object);
            services.AddScoped(c => ClaimSubmissionServiceLineRepository.Object);

            // DBHELPERS
            services.AddScoped(c => BillingDbHelper.Object);

            //SERVICES
            services.AddScoped(c => RethinkServices.Object);
            services.AddScoped(c => ClientService.Object);
            services.AddScoped(c => ProviderLocationService.Object);
            services.AddScoped(c => MemberAccountService.Object);
            services.AddScoped(c => CommonService.Object);
            services.AddScoped(c => ClaimService.Object);
        }

        protected virtual void InitializeTestServices(IServiceCollection services)
        {
            services.AddScoped(c => RethinkServices.Object);
            services.AddScoped(c => ClaimManagerService.Object);
        }

        /// <summary>
        /// Gets the full path to the target project that we wish to test
        /// </summary>
        /// <param name="projectRelativePath">
        /// The parent directory of the target project.
        /// e.g. src, samples, test, or test/Websites
        /// </param>
        /// <param name="startupAssembly">The target project's assembly.</param>
        /// <returns>The full path to the target project.</returns>
        private static string GetProjectPath(string projectRelativePath, Assembly startupAssembly)
        {
            // Get name of the target project which we want to test
            var projectName = startupAssembly.GetName().Name;

            // Get currently executing test project path
            var applicationBasePath = Assembly.GetExecutingAssembly().Location;

            // Find the path to the target project
            var directoryInfo = new DirectoryInfo(applicationBasePath);
            do
            {
                directoryInfo = directoryInfo.Parent;

                var projectDirectoryInfo = new DirectoryInfo(Path.Combine(directoryInfo.FullName, projectRelativePath));
                if (projectDirectoryInfo.Exists)
                {
                    var projectFileInfo = new FileInfo(Path.Combine(projectDirectoryInfo.FullName, projectName, $"{projectName}.csproj"));
                    if (projectFileInfo.Exists)
                    {
                        return Path.Combine(projectDirectoryInfo.FullName, projectName);
                    }
                }
            }
            while (directoryInfo.Parent != null);

            throw new Exception($"Project root could not be located using the application root {applicationBasePath}.");
        }
    }
}
