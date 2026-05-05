using Microsoft.Extensions.Configuration;
using PusherServer;
using Rethink.Services.Domain.Interfaces;
using System.Threading.Tasks;

namespace BillingService.Web.Servers
{
    public class PusherNotificationServer : IPusherNotificationServer
    {
        private readonly Pusher _pusher;

        public PusherNotificationServer(IConfiguration config, IKeyVaultProviderService keyVaultProviderService)
        {
            var option = new PusherOptions
            {
                Cluster = config["Pusher:Cluster"],
                Encrypted = true
            };
            var appId = keyVaultProviderService.GetSecretAsync(config["Pusher:AppId"]).Result;
            var key = keyVaultProviderService.GetSecretAsync(config["Pusher:Key"]).Result;
            var secret = keyVaultProviderService.GetSecretAsync(config["Pusher:Secret"]).Result;

            _pusher = new Pusher(
                appId,
                key,
                secret,
                option);
        }

        public async Task TriggerAsync(string channel, string eventName, object data)
        {
            await _pusher.TriggerAsync(channel, eventName, data);
        }

        public object Authenticate(string channelName, string socketId)
        {
            return _pusher.Authenticate(channelName, socketId);
        }

    }
}
