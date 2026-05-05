using Microsoft.Extensions.Configuration;
using PusherServer;
using Rethink.Services.Domain.Interfaces;
using System.Threading.Tasks;
using System;

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
            var appIdTask = keyVaultProviderService.GetSecretAsync(config["Pusher:AppId"]);
            var keyTask = keyVaultProviderService.GetSecretAsync(config["Pusher:Key"]);
            var secretTask = keyVaultProviderService.GetSecretAsync(config["Pusher:Secret"]);
            Task.WhenAll(appIdTask, keyTask, secretTask).GetAwaiter().GetResult();
            var appId = appIdTask.GetAwaiter().GetResult();
            var key = keyTask.GetAwaiter().GetResult();
            var secret = secretTask.GetAwaiter().GetResult();

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
