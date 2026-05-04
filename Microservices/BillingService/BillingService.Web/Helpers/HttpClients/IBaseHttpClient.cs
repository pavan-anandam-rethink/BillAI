using System.Threading.Tasks;

namespace BillingService.Web.Helpers.HttpClients
{
    public interface IBaseHttpClient
    {
        Task<string> PostAsync(string baseAddress, string actionName, object body);
    }
}