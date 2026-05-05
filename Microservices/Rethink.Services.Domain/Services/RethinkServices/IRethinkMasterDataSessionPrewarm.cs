using System.Threading.Tasks;

namespace Rethink.Services.Domain.Services.RethinkServices
{
    /// <summary>
    /// Populates session-scoped master data cache after login (parallel BH calls).
    /// </summary>
    public interface IRethinkMasterDataSessionPrewarm
    {
        Task WarmAsync(int accountInfoId, string sessionKey);
    }
}
