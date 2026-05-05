using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Services.Billing;
using BillingService.Domain.Utils;
using BillingService.Test.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using Rethink.Services.Common.Factories;
using Rethink.Services.Common.Helpers;
using Rethink.Services.Common.Infrastructure.Configuration;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Jobs;
using Rethink.Services.Common.Services;
using Rethink.Services.Domain.Interfaces;
using Rethink.Services.Domain.Services;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace BillingService.Test
{
    public class ServicesConfiguration
    {
        private static IDbContextConfigurator<BillingDbContext> _billingConfigurator = new DbContextConfigurator<BillingDbContext>();
        private readonly IServiceCollection _serviceCollection;
        private readonly IConfiguration _configuration;

        public ServicesConfiguration(IServiceCollection serviceCollection, IConfiguration configuration)
        {
            _serviceCollection = serviceCollection;
            _configuration = configuration;
        }

        public async Task Configure()
        {
            //ConfigureDatabase();

            //RegisterDBContext();

            ConfigureMapping(_serviceCollection);


            //Quartz services
            _serviceCollection.AddSingleton<IJobFactory, JobFactory>()
                              .AddSingleton<ISchedulerFactory, StdSchedulerFactory>()
                              .AddHostedService<QuartzHostedService>();

            //Our jobs
            _serviceCollection.AddSingleton<BillingServiceTestJob>()
                              .AddSingleton(new JobScheduler(
                jobType: typeof(BillingServiceTestJob),
                cronExpression: "0/9 * * * * ?"));

            //Domain services
            _serviceCollection.AddScoped<IClaimManagerService, ClaimManagerService>()
                              .AddScoped<IClaimHistoryService, ClaimHistoryService>()
                              .AddSingleton(_serviceCollection)
                              .AddLogging(builder => LogConfigHelper.ConfigureWorkerLogging(builder));

            //_serviceCollection.AddScoped<IEdiUploadService, EdiUploadService>()
            //       .AddSingleton(_serviceCollection)
            //       .AddLogging(builder => LogConfigHelper.ConfigureWorkerLogging(builder));

            //_serviceCollection.AddScoped<ISftpService, SftpService>()
            //                   .AddSingleton(_serviceCollection)
            //                  .AddLogging(builder => LogConfigHelper.ConfigureWorkerLogging(builder));
        }

        private void ConfigureMapping(IServiceCollection serviceCollection)
        {
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MapperProfile());
            });

            IMapper mapper = mapperConfig.CreateMapper();

            serviceCollection.AddSingleton(mapper);



        }

        private void ConfigureDatabase()
        {
            var billingDbConnectionString = GetDBConnectionString("BillingDB");

            _billingConfigurator.Configure(_serviceCollection, billingDbConnectionString, false, false);
        }

        public void RegisterDBContext()
        {
            new DbContextConfigurator<BillingDbContext>().Register(_serviceCollection);
            _serviceCollection.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
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
                connStringBuilder.UserID = connectionStringSection["UserID"];
                connStringBuilder.Password = connectionStringSection["Password"];
            }

            return connStringBuilder.ConnectionString;
        }

    }
}