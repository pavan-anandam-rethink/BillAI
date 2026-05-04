using System.Threading.Tasks;

namespace BillingService.Web.Servers
{
    public interface IPusherNotificationServer
    {
        object Authenticate(string channelName, string socketId);
        Task TriggerAsync(string channel, string eventName, object data);
    }
}