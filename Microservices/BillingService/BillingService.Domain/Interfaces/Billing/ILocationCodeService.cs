using BillingService.Domain.DataObjects.Billing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface ILocationCodeService
    {
        Task<List<LocationCodeItem>> GetAll(int accountInfoId);
    }
}
