using System;
using System.Linq;
using System.Threading.Tasks;
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
        /// <summary>Registers <see cref="IServiceBusConnectionFactory"/> without additional Key Vault round-trips.</summary>
        public static ServiceBusConnectionStringBuilder ConfigureServiceBusWithConnectionString(
            IServiceCollection serviceCollection,
            string connectionString)
        {
            var connStringBuilder = new ServiceBusConnectionStringBuilder(connectionString);
            var factory = new ServiceBusConnectionFactory(connStringBuilder);
            serviceCollection.AddScoped<IServiceBusConnectionFactory>(_ => factory);
            return connStringBuilder;
        }

        public static Task<ServiceBusConnectionStringBuilder> ConfigureServiceBusAsync(
            IServiceCollection serviceCollection,
            string serviceBusKvSecretName,
            IKeyVaultProviderService keyVaultProviderService)
        {
            return ConfigureServiceBusFromKeyVaultAsync(serviceCollection, serviceBusKvSecretName, keyVaultProviderService);
        }

        public static async Task<ServiceBusConnectionStringBuilder> ConfigureServiceBusFromKeyVaultAsync(
            IServiceCollection serviceCollection,
            string serviceBusKvSecretName,
            IKeyVaultProviderService keyVaultProviderService)
        {
            var serviceBusSecret = await keyVaultProviderService.GetSecretAsync(serviceBusKvSecretName).ConfigureAwait(false);
            return ConfigureServiceBusWithConnectionString(serviceCollection, serviceBusSecret);
        }

        [Obsolete("Use ConfigureServiceBusWithConnectionString or ConfigureServiceBusFromKeyVaultAsync.")]
        public static ServiceBusConnectionStringBuilder ConfigureServiceBus(IServiceCollection serviceCollection, IConfiguration configuration, IKeyVaultProviderService keyVaultProviderService)
        {
            return ConfigureServiceBusFromKeyVaultAsync(
                serviceCollection,
                configuration["ConnectionStrings:ServiceBus:ConnectionString"],
                keyVaultProviderService).GetAwaiter().GetResult();
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
