using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Common
{
    public interface ICacheService
    {
        Task<T> GetOrSetCacheAsync<T>(string cacheKey,Func<Task<T>> fetchDataFunc, TimeSpan expirationTime);
        Task RemoveAsync(string cacheKey);
    }
}
