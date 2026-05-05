using Microsoft.Extensions.Configuration;
using PusherServer;
using Rethink.Services.Domain.Interfaces;
using System.Threading.Tasks;

namespace BillingService.Web.Servers
{
    public class PusherNotificationServer : IPusherNotificationServer
    {
        private readonly Pusher _pusher;

        public PusherNotificationServer(IConfiguration config, string appId, string key, string secret)
        {
            var option = new PusherOptions
            {
                Cluster = config["Pusher:Cluster"],
                Encrypted = true
            };

            _pusher = new Pusher(
                appId,
                key,
                secret,
                option);
        }

        public Task TriggerAsync(string channel, string eventName, object data)
        {
            return _pusher.TriggerAsync(channel, eventName, data);
        }

        public object Authenticate(string channelName, string socketId)
        {
            return _pusher.Authenticate(channelName, socketId);
        }
    }
}
