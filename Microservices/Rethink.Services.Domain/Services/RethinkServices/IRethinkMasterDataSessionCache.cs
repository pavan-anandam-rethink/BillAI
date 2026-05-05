using System;
using System.Threading.Tasks;

namespace Rethink.Services.Domain.Services.RethinkServices
{
    /// <summary>
    /// Session-scoped cache for Rethink BH microservice GET responses (Redis).
    /// </summary>
    public interface IRethinkMasterDataSessionCache
    {
        Task<T> GetOrFetchAsync<T>(string sessionKey, int accountInfoId, string relativePath, Func<Task<T>> acquire);
    }
}
