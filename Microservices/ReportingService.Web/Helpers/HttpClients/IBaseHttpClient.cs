using System.Threading.Tasks;

namespace ReportingService.Web.Helpers.HttpClients
{
    public interface IBaseHttpClient
    {
        Task<string> PostAsync(string baseAddress, string actionName, object body);
    }
}