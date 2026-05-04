using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IProviderBillingCodeService
    {
        //Task<List<ProviderBillingCodeItem>> GetForFunders(int accountInfoId, List<int> funderIds);
        Task<decimal?> GetServiceRateAsync(int funderId, string serviceCode, int accountInfoId);
    }
}
