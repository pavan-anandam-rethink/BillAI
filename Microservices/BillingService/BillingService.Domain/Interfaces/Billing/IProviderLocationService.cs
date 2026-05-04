using Rethink.Services.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace BillingService.Domain.Interfaces.Billing
{
    public interface IProviderLocationService
    {
        Task<List<ProviderLocations>> GetForAccount(int accountInfoId);
    }
}
