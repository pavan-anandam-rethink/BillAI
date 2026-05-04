using Azure.Storage.Blobs;
using Billing.FolderStructure.Core.Services;
using Microsoft.Azure.ServiceBus;
using Rethink.Services.Common.Infrastructure.Connection;
using StackExchange.Redis;

namespace ClientService.Web.infrastructure
{
  
        public static class InfrastructureServiceCollectionExtensions
        {
            public static IServiceCollection AddInfrastructure(
                this IServiceCollection services,
                IConfiguration configuration)
            {
                services.AddServiceBus(configuration);
                services.AddRedis(configuration);
                services.AddBlobStorage(configuration);

                return services;
            }

            public static IServiceCollection AddServiceBus(
                this IServiceCollection services,
                IConfiguration configuration)
            {
                services.AddSingleton<IServiceBusConnectionFactory>(sp =>
                {
                    var connectionString = configuration["ServiceBus:ConnectionString"];

                    var builder = new ServiceBusConnectionStringBuilder(connectionString)
                    {
                        TransportType = TransportType.Amqp
                    };

                    return new ServiceBusConnectionFactory(builder);
                });

            return services;
        }

            public static IServiceCollection AddRedis(
                this IServiceCollection services,
                IConfiguration configuration)
            {
                services.AddSingleton<IConnectionMultiplexer>(sp =>
                {
                    return ConnectionMultiplexer.Connect(
                        configuration.GetConnectionString("Redis")
                    );
                });

                return services;
            }

            public static IServiceCollection AddBlobStorage(
                this IServiceCollection services,
                IConfiguration configuration)
            {
            services.AddSingleton(sp =>
            {
                var connectionString = configuration["BlobStorage:ConnectionString"];

                return new BlobServiceClient(connectionString);
            });

            services.AddScoped<IBillingBlobService, BillingBlobService>();

            return services;
        }
        }
    }
