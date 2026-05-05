using BillingService.Domain.Models.Funders;
using BillingService.Domain.Models.PaymentPosting;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IFunderService
    {
        //Task<List<FunderItem>> GetForAccount(int accountInfoId);
        Task<FunderDropdownResponseModel> GetFundersAsync(FunderSearchModelWithUserInfo funderSearchModel);
    }
}
