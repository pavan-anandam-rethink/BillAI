using BillingService.Domain.DataObjects.Base;
using BillingService.Domain.Services.Billing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IClaimSearchService
    {
        Task<List<BaseNameOption>> GetAllClientsForAccount(int accountInfoId);
        Task<List<BaseNameOption>> GetFunderInfoByIds(int accountInfoId);
        Task<List<BaseNameOption>> GetClientHistoryFunderInfoByIds(int accountInfoId,int clientId);
        Task<List<StaffBaseNameOption>> GetStaffInfoByIds(int accountInfoId);
        Task<List<BaseNameOption>> GetPlaceOfServiceInfoByIds(int accountInfoId);
        Task<List<BaseNameOption>> GetLocationInfoByIds(int accountInfoId);
    }
}
