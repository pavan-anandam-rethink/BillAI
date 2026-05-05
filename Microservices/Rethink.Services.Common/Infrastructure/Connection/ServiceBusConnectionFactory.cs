using Microsoft.Azure.ServiceBus;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Connection
{
    [ExcludeFromCodeCoverage]
    public class ServiceBusConnectionFactory : IServiceBusConnectionFactory
    {
        public ServiceBusConnectionFactory(ServiceBusConnectionStringBuilder connectionStringBuilder)
        {
            ConnectionStringBuilder = connectionStringBuilder;
        }

        public ServiceBusConnectionStringBuilder ConnectionStringBuilder { get; set; }


        public ServiceBusConnection CreateConnection()
        {
            var connString = ConnectionStringBuilder.GetEntityConnectionString();
            return new ServiceBusConnection(connString);
        }
    }
}