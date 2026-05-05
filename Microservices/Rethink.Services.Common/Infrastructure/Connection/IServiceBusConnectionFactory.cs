using Microsoft.Azure.ServiceBus;

namespace Rethink.Services.Common.Infrastructure.Connection
{
    public interface IServiceBusConnectionFactory
    {
        ServiceBusConnectionStringBuilder ConnectionStringBuilder { get; }
        ServiceBusConnection CreateConnection();
    }
}