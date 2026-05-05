using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using EdiFabric;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rethink.Services.Common.Infrastructure.Connection;
using Rethink.Services.Domain.Interfaces;

namespace BillingService.Domain
{
    public class ServiceBusConfig
    {
        public static ServiceBusConnectionStringBuilder ConfigureServiceBus(IServiceCollection serviceCollection, IConfiguration configuration,IKeyVaultProviderService keyVaultProviderService)
        {
            var serviceBusSecret = keyVaultProviderService.GetSecretAsync(configuration["ConnectionStrings:ServiceBus:ConnectionString"]).Result;
            
            // we need a ServiceBusConnectionStringBuilder to construct a ManagementClient
            var connStringBuilder = new ServiceBusConnectionStringBuilder(serviceBusSecret);
            var builder = new ServiceBusConnectionFactory(connStringBuilder);
            serviceCollection.AddScoped<IServiceBusConnectionFactory>(x => builder);

            return connStringBuilder;
        }

        public static async Task CreateQueueIfNotExists(ManagementClient mgmtClient, string queuePath)
        {
            var queues = await mgmtClient.GetQueuesAsync();

            if (queues.All(q => q.Path != queuePath))
            {
                await mgmtClient.CreateQueueAsync(queuePath);
            }
        }
    }
}