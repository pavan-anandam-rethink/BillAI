using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces
{
    public interface IRazorViewService
    {
        Task<string> RenderViewToStringAsync<TModel>(string viewName, TModel model);
    }
}
