using System;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Cache
{
    public interface ICacheManager
    {
        Task<T> GetAsync<T>(string key, Func<Task<T>> acquire, CachingDuration duration);

        Task SetAsync(string key, object data, CachingDuration duration);

        Task Remove(string key);

        Task Clear();
    }
}
