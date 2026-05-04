using BillingService.Domain.Models.Clients;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Provider
{
    public interface IProviderService
    {
        Task<List<ProviderLocationData>> GetProviderLocationList(int accountInfoId, JsonSerializerSettings settings);
    }
}
