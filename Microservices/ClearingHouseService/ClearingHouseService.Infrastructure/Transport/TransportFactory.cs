using ClearingHouseService.Domain.Interfaces;
using ClearingHouseService.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClearingHouseService.Infrastructure.Transport
{
    /// <summary>
    /// Factory that resolves the appropriate IClearingHouseTransport implementation
    /// based on the clearing house type and its transport protocol.
    /// </summary>
    public interface ITransportFactory
    {
        /// <summary>
        /// Gets the appropriate transport for the given clearing house type.
        /// </summary>
        IClearingHouseTransport GetTransport(ClearingHouseType clearingHouseType);

        /// <summary>
        /// Gets the appropriate transport for the given transport protocol.
        /// </summary>
        IClearingHouseTransport GetTransport(TransportProtocol protocol);
    }

    public class TransportFactory : ITransportFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TransportFactory> _logger;

        public TransportFactory(IServiceProvider serviceProvider, ILogger<TransportFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public IClearingHouseTransport GetTransport(ClearingHouseType clearingHouseType)
        {
            _logger.LogDebug("Resolving transport for clearing house type: {ClearingHouseType}", clearingHouseType.Name);
            return GetTransport(clearingHouseType.Protocol);
        }

        public IClearingHouseTransport GetTransport(TransportProtocol protocol)
        {
            return protocol switch
            {
                TransportProtocol.Sftp => _serviceProvider.GetRequiredService<SftpTransport>(),
                TransportProtocol.Api => _serviceProvider.GetRequiredService<StediApiTransport>(),
                _ => throw new NotSupportedException($"Transport protocol '{protocol}' is not supported.")
            };
        }
    }
}
